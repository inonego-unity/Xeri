/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : MValue.cs
수정일 : 2026-10-29

# 설명
Order 순서로 적용되는 IModifier<T> 목록을 가지는 Modifiable Value.
Modifier의 내부 상태 변경을 구독해 Modified 캐시를 자동 갱신한다.

# 특이사항, 제약사항
Modified 캐시는 runtime derived state이며 Base와 Modifier 변경 시 내부에서 즉시 갱신한다.
동일 Modifier 인스턴스가 여러 Key로 등록되어도 변경 이벤트는 한 번만 구독한다.
invokeEvent는 외부 이벤트만 제어하며 derived type의 Modified 갱신 hook은 항상 실행한다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using inonego;
using inonego.Xeri;

namespace inonego.Xeri.Serializable
{
    // ============================================================
    /// <summary>
    /// 수정자가 적용되는 Value.
    /// </summary>
    // ============================================================
    [Serializable]
    public class MValue<T> :
        Value<T>,
        IMValue<T>,
        ISerializationCallbackReceiver
    {

    #region 필드

        // ------------------------------------------------------------
        /// <summary>
        /// Order 오름차순의 수정자 등록 목록을 읽기 전용으로 노출한다.
        /// </summary>
        // ------------------------------------------------------------
        public IReadOnlyXOrdered<int, string, IModifier<T>> Modifiers => modifiers;

        [SerializeField]
        private XOrdered<int, string, IModifier<T>> modifiers = new();

        // ------------------------------------------------------------
        /// <summary>
        /// 수정자가 적용된 현재 값.
        /// </summary>
        // ------------------------------------------------------------
        public T Modified => cached;

        [NonSerialized]
        private T cached;

        private HashSet<IModifier<T>> SubscribedModifiers
        {
            get
            {
                subscribedModifiers ??= new
                (
                    ReferenceEqualityComparer<IModifier<T>>.Instance
                );

                return subscribedModifiers;
            }
        }

        [NonSerialized]
        private HashSet<IModifier<T>> subscribedModifiers = new
        (
            ReferenceEqualityComparer<IModifier<T>>.Instance
        );

    #endregion

    #region 이벤트

        // ------------------------------------------------------------
        /// <summary>
        /// Modified 가 변경될 때 발생하는 이벤트.
        /// </summary>
        // ------------------------------------------------------------
        [field: NonSerialized]
        public event ValueChangeEventHandler<T> OnModifiedChange = null;

        // ------------------------------------------------------------
        /// <summary>
        /// 외부 이벤트와 관계없이 Modified 캐시의 실제 변경을 전달한다.
        /// </summary>
        // ------------------------------------------------------------
        [field: NonSerialized]
        internal event Action<T, T> OnModifiedApplied;

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// MValue 인스턴스를 초기화한다.
        /// </summary>
        // ------------------------------------------------------------
        public MValue() : this(default)
        {
            // NONE
        }

        // ------------------------------------------------------------
        /// <summary>
        /// MValue 인스턴스를 초기화한다.
        /// </summary>
        // ------------------------------------------------------------
        public MValue(T value) : base(value)
        {
            UpdateModified(cached, invokeHook: false);
        }

    #endregion

    #region 값 평가

        // ------------------------------------------------------------
        /// <summary>
        /// 이전 Modified와 재계산 결과를 비교해 캐시와 내부 hook을 갱신한다.
        /// </summary>
        // ------------------------------------------------------------
        private void UpdateModified(T previous, bool invokeHook = true)
        {
            var current = Modify(Base);
            cached = current;

            if (comparer.Equals(previous, current))
            {
                return;
            }

            OnModifiedApplied?.Invoke(previous, current);

            if (invokeHook)
            {
                OnModifiedUpdated(in previous, in current);
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 내부 상태를 갱신한 뒤 변경 전 값과 비교해 외부 이벤트를 전달한다.
        /// </summary>
        // ------------------------------------------------------------
        private void Change(Action mutation, bool invokeEvent)
        {
            var previousBase = Base;
            var previousModified = Modified;

            mutation();
            UpdateModified(previousModified);

            if (invokeEvent)
            {
                RaiseChanges(previousBase, previousModified);
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 확정된 Base와 Modified를 이전 값과 비교해 통지한다.
        /// </summary>
        // ------------------------------------------------------------
        private void RaiseChanges(T previousBase, T previousModified)
        {
            var currentBase = Base;
            var currentModified = Modified;

            if (!comparer.Equals(previousBase, currentBase))
            {
                RaiseBaseChange(previousBase, currentBase);
            }

            if (!comparer.Equals(previousModified, currentModified))
            {
                RaiseModifiedChange(previousModified, currentModified);
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 확정된 Modified 변경을 외부에 전달한다.
        /// </summary>
        // ------------------------------------------------------------
        private void RaiseModifiedChange(T previous, T current)
        {
            OnModifiedChange?.Invoke(this, new(previous, current));
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Modified가 갱신된 뒤 외부 이벤트와 무관하게 호출되는 내부 hook.
        /// </summary>
        // ------------------------------------------------------------
        protected virtual void OnModifiedUpdated(in T prev, in T next)
        {
            // NONE
        }


        // ------------------------------------------------------------
        /// <summary>
        /// modifiers 를 Order 순서대로 순차 적용한 값을 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        private T Modify(T value)
        {
            foreach (var entry in modifiers)
            {
                if (entry.Value == null)
                {
                    continue;
                }

                value = entry.Value.Modify(value);
            }

            return value;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Base 값을 설정한 뒤 Modified 캐시를 갱신한다.
        /// </summary>
        // ------------------------------------------------------------
        public override void Set(T value, bool invokeEvent = true)
        {
            void ApplyChange()
            {
                base.Set(value, invokeEvent: false);
            }

            Change
            (
                ApplyChange,
                invokeEvent
            );
        }

    #endregion

    #region 수정자 관리

        // ------------------------------------------------------------
        /// <summary>
        /// 키를 명시하여 수정자를 추가한다.
        /// </summary>
        // ------------------------------------------------------------
        public void AddModifier(string key, IModifier<T> modifier, int order = 0, bool invokeEvent = true)
        {
            if (modifier == null)
            {
                throw new ArgumentNullException(nameof(modifier), "추가하려는 수정자가 null입니다.");
            }

            void ApplyChange()
            {
                modifiers.Add(order, key, modifier);
                SubscribeModifier(modifier);
            }

            Change
            (
                ApplyChange,
                invokeEvent
            );
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// <c>IKeyable&lt;string&gt;</c> 을 구현한 수정자를 자기 키로 추가한다.
        /// </summary>
        // ----------------------------------------------------------------------
        public void AddModifier<TModifier>(TModifier modifier, int order = 0, bool invokeEvent = true)
        where TModifier : IModifier<T>, IKeyable<string>
        {
            if (modifier == null)
            {
                throw new ArgumentNullException(nameof(modifier), "추가하려는 수정자가 null입니다.");
            }

            AddModifier(modifier.Key, modifier, order, invokeEvent);
        }

        // --------------------------------------------------------------------------------
        /// <summary>
        /// 수정자가 <c>IKeyable&lt;string&gt;</c> 을 구현하면 자기 키로 추가한다. 아니면 예외.
        /// </summary>
        // --------------------------------------------------------------------------------
        public void AddModifier(IModifier<T> modifier, int order = 0, bool invokeEvent = true)
        {
            if (modifier == null)
            {
                throw new ArgumentNullException(nameof(modifier), "추가하려는 수정자가 null입니다.");
            }

            if (modifier is IKeyable<string> keyable)
            {
                AddModifier(keyable.Key, modifier, order, invokeEvent);
            }
            else
            {
                throw new ArgumentException
                (
                    $"수정자({modifier.GetType().Name})가 IKeyable<string>을 구현하지 않아 키를 추출할 수 없습니다."
                );
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 키로 수정자를 제거한다.
        /// </summary>
        // ------------------------------------------------------------
        public bool RemoveModifier(string key, bool invokeEvent = true)
        {
            var removed = false;

            void ApplyChange()
            {
                removed = modifiers.Remove(key);
                if (removed)
                {
                    ResubscribeModifiers();
                }
            }

            Change
            (
                ApplyChange,
                invokeEvent
            );

            return removed;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 모든 수정자를 제거한다.
        /// </summary>
        // ------------------------------------------------------------
        public void ClearModifiers(bool invokeEvent = true)
        {
            if (modifiers.Count == 0) return;

            void ApplyChange()
            {
                UnsubscribeModifiers();
                modifiers.Clear();
            }

            Change
            (
                ApplyChange,
                invokeEvent
            );
        }

    #endregion

    #region Modifier 구독

        // ----------------------------------------------------------------------
        /// <summary>
        /// 이 MValue의 변경 handler가 Modifier에 한 번만 등록되도록 구독한다.
        /// </summary>
        // ----------------------------------------------------------------------
        private void SubscribeModifier(IModifier<T> modifier)
        {
            if (modifier == null || !SubscribedModifiers.Add(modifier))
            {
                return;
            }

            modifier.OnChange -= HandleModifierChange;
            modifier.OnChange += HandleModifierChange;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 등록된 Modifier에서 이 MValue의 변경 구독을 모두 해제한다.
        /// </summary>
        // ------------------------------------------------------------
        private void UnsubscribeModifiers()
        {
            foreach (var modifier in SubscribedModifiers)
            {
                modifier.OnChange -= HandleModifierChange;
            }

            SubscribedModifiers.Clear();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 Modifier 목록을 기준으로 변경 이벤트를 다시 구독한다.
        /// </summary>
        // ------------------------------------------------------------
        private void ResubscribeModifiers()
        {
            UnsubscribeModifiers();

            foreach (var entry in modifiers)
            {
                SubscribeModifier(entry.Value);
            }
        }

    #endregion

    #region 이벤트 핸들러

        // ------------------------------------------------------------
        /// <summary>
        /// Modifier 상태 변경 시 Modified 캐시를 갱신한다.
        /// </summary>
        // ------------------------------------------------------------
        private void HandleModifierChange()
        {
            var previousModified = Modified;
            UpdateModified(previousModified);

            if (!comparer.Equals(previousModified, Modified))
            {
                RaiseModifiedChange(previousModified, Modified);
            }
        }

    #endregion

    #region 직렬화 변경 확정

        // ------------------------------------------------------------
        /// <summary>
        /// 직렬화로 적용된 Base·Modified 변경과 Modifier 구독을 확정한다.
        /// </summary>
        // ------------------------------------------------------------
        internal void FinalizeSerializedChange(T previousBase, T previousModified)
        {
            ResubscribeModifiers();

            // 현재 캐시를 되돌리지 않고 직렬화 변경 전 값으로 차이를 판단한다.
            UpdateModified(previousModified);

            RaiseChanges(previousBase, previousModified);
        }

    #endregion

    #region ISerializationCallbackReceiver

        // ------------------------------------------------------------
        /// <summary>
        /// 직렬화 전에 추가 동기화는 수행하지 않는다.
        /// </summary>
        // ------------------------------------------------------------
        public void OnBeforeSerialize()
        {
            // NONE
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// 역직렬화된 authoritative state에서 runtime state를 다시 구성한다.
        /// </summary>
        // ----------------------------------------------------------------------
        public void OnAfterDeserialize()
        {
            ResubscribeModifiers();
            cached = Modify(Base);
        }

    #endregion

    #region 암시적 변환

        // ------------------------------------------------------------
        /// <summary>
        /// MValue&lt;T&gt;에서 T로의 암시적 변환(Modified 값).
        /// </summary>
        // ------------------------------------------------------------
        public static implicit operator T(MValue<T> wrapper)
        {
            return wrapper != null ? wrapper.Modified : default;
        }

    #endregion

    #region Object 오버라이드

        // ------------------------------------------------------------
        /// <inheritdoc />
        // ------------------------------------------------------------
        public override bool Equals(object obj)
        {
            if (obj is MValue<T> other && other.GetType() == GetType())
                return comparer.Equals(Modified, other.Modified);
            if (obj is T directValue)
                return comparer.Equals(Modified, directValue);
            return false;
        }

        // ------------------------------------------------------------
        /// <inheritdoc />
        // ------------------------------------------------------------
        public override int GetHashCode() => comparer.GetHashCode(Modified);

        // ------------------------------------------------------------
        /// <inheritdoc />
        // ------------------------------------------------------------
        public override string ToString() => $"{Modified}({Base})";

    #endregion

    }
}
