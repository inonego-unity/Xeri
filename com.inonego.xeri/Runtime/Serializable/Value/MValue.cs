/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : MValue.cs
수정일 : 2026-09-18

# 설명
Order 순서로 적용되는 IModifier<T> 목록을 가지는 Modifiable Value.
Modifier의 내부 상태 변경을 구독해 Modified 캐시를 자동 갱신한다.

# 특이사항, 제약사항
Modified 캐시는 runtime derived state이며 Base와 Modifier 변경 시 내부에서 즉시 갱신한다.
동일 Modifier 인스턴스가 여러 Key로 등록되어도 변경 이벤트는 한 번만 구독한다.
========================================================================= BLOCK_HEADER_END */

using System;

using UnityEngine;

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

        [SerializeField, HideInInspector]
        private XOrdered<int, string, IModifier<T>> modifiers = new();

        // ------------------------------------------------------------
        /// <summary>
        /// 수정자가 적용된 현재 값.
        /// </summary>
        // ------------------------------------------------------------
        public T Modified => cached;

        [NonSerialized]
        private T cached;

    #endregion

    #region 이벤트

        // ------------------------------------------------------------
        /// <summary>
        /// Modified 가 변경될 때 발생하는 이벤트.
        /// </summary>
        // ------------------------------------------------------------
        [field: NonSerialized]
        public event ValueChangeEventHandler<T> OnModifiedChange = null;

    #endregion

    #region 생성자

        public MValue() : this(default)
        {
            // NONE
        }

        public MValue(T value) : base(value)
        {
            UpdateModified(invokeEvent: false);
        }

    #endregion

    #region 값 평가

        // ------------------------------------------------------------
        /// <summary>
        /// 수정자를 모두 적용한 값을 다시 계산해 cached 에 반영한다.
        /// </summary>
        // ------------------------------------------------------------
        private void UpdateModified(bool invokeEvent = true)
        {
            var prev = cached;
            var next = Modify(Base);

            if (comparer.Equals(prev, next)) return;

            cached = next;

            if (invokeEvent)
            {
                InvokeOnModifiedChange(prev);
            }
        }

        // --------------------------------------------------------------------------------
        /// <summary>
        /// <br/> equality check 없이 OnModifiedChange를 강제 발화한다.
        /// <br/> Undo 복원 후 backing field가 이미 복원된 상태에서 이벤트를 트리거할 때 사용한다.
        /// </summary>
        // --------------------------------------------------------------------------------
        public void InvokeOnModifiedChange(T previousValue)
        {
            OnModifiedChange?.Invoke(this, new(previousValue, cached));
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
            base.Set(value, invokeEvent);

            UpdateModified(invokeEvent);
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

            modifiers.Add(order, key, modifier);
            SubscribeModifier(modifier);

            UpdateModified(invokeEvent);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// IKeyable<string> 을 구현한 수정자를 자기 키로 추가한다.
        /// </summary>
        // ------------------------------------------------------------
        public void AddModifier<TModifier>(TModifier modifier, int order = 0, bool invokeEvent = true)
        where TModifier : IModifier<T>, IKeyable<string>
        {
            if (modifier == null)
            {
                throw new ArgumentNullException(nameof(modifier), "추가하려는 수정자가 null입니다.");
            }

            AddModifier(modifier.Key, modifier, order, invokeEvent);
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// 수정자가 IKeyable<string> 을 구현하면 자기 키로 추가한다. 아니면 예외.
        /// </summary>
        // ----------------------------------------------------------------------
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
            if (!modifiers.TryGetEntry(key, out var entry)) return false;
            if (!modifiers.Remove(key)) return false;

            entry.Value.OnChange -= HandleModifierChange;
            ResubscribeModifiers();

            UpdateModified(invokeEvent);

            return true;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 모든 수정자를 제거한다.
        /// </summary>
        // ------------------------------------------------------------
        public void ClearModifiers(bool invokeEvent = true)
        {
            if (modifiers.Count == 0) return;

            UnsubscribeModifiers();
            modifiers.Clear();

            UpdateModified(invokeEvent);
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
            foreach (var entry in modifiers)
            {
                entry.Value.OnChange -= HandleModifierChange;
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 Modifier 목록을 기준으로 변경 이벤트를 다시 구독한다.
        /// </summary>
        // ------------------------------------------------------------
        private void ResubscribeModifiers()
        {
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
            UpdateModified();
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
            UpdateModified(invokeEvent: false);
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

        public override bool Equals(object obj)
        {
            if (obj is MValue<T> other)
                return comparer.Equals(Modified, other.Modified);
            if (obj is T directValue)
                return comparer.Equals(Modified, directValue);
            return false;
        }

        public override int GetHashCode() => Modified.GetHashCode();

        public override string ToString() => $"{Modified}({Base})";

    #endregion

    }
}
