/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : MValue.cs
수정일 : 2026-05-02

# 설명
Order 순서로 적용되는 IModifier<T> 목록을 가지는 Modifiable Value.
Base 또는 modifiers 변경 시 Modified(캐시값)를 재계산하고 OnModifiedChange를 발행한다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections.Generic;

using UnityEngine;

namespace inonego.Xeri.Serializable
{
    // ============================================================
    /// <summary>
    /// 수정자가 적용되는 Value.
    /// </summary>
    // ============================================================
    [Serializable]
    public class MValue<T> : Value<T>, IReadOnlyMValue<T>, IDeepCloneable<MValue<T>>
    {

    #region 필드

        // ------------------------------------------------------------
        /// <summary>
        /// Order 오름차순으로 정렬된 수정자 목록.
        /// </summary>
        // ------------------------------------------------------------
        [SerializeField, HideInInspector]
        private XOrdered<int, string, IModifier<T>> modifiers = new();

        // ----------------------------------------------------------------------------
        /// <summary>
        /// (Modifier, Order) 튜플 리스트로 변환된 수정자 목록(Order 오름차순).
        /// </summary>
        // ----------------------------------------------------------------------------
        public IReadOnlyList<(IModifier<T> Modifier, int Order)> Modifiers
        {
            get
            {
                var list = new List<(IModifier<T>, int)>(modifiers.Count);

                foreach (var pair in modifiers)
                {
                    list.Add((pair.Value, pair.Order));
                }

                return list;
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 수정자 적용 후 캐시된 값.
        /// </summary>
        // ------------------------------------------------------------
        [SerializeField, HideInInspector]
        private T cached;

        // ------------------------------------------------------------
        /// <summary>
        /// 수정자가 적용된 현재 값.
        /// </summary>
        // ------------------------------------------------------------
        public T Modified => cached;

    #endregion

    #region 이벤트

        // ------------------------------------------------------------
        /// <summary>
        /// Modified 가 변경될 때 발생하는 이벤트.
        /// </summary>
        // ------------------------------------------------------------
        public event ValueChangeEventHandler<T> OnModifiedChange = null;

    #endregion

    #region 생성자

        public MValue() : this(default) {}

        public MValue(T value) : base(value)
        {
            Refresh(invokeEvent: false);
        }

    #endregion

    #region 메서드

        // -----------------------------------------------------------------------
        /// <summary>
        /// 수정자를 모두 적용한 값을 다시 계산해 cached 에 반영한다.
        /// </summary>
        // -----------------------------------------------------------------------
        private void Refresh(bool invokeEvent = true)
        {
            var (prev, next) = (cached, Modify(Base));

            if (comparer.Equals(prev, next)) return;

            cached = next;

            if (invokeEvent)
            {
                OnModifiedChange?.Invoke(this, new(prev, cached));
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// modifiers 를 Order 순서대로 순차 적용한 값을 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        private T Modify(T value)
        {
            foreach (var pair in modifiers)
            {
                value = pair.Value.Modify(value);
            }

            return value;
        }

        // -------------------------------------------------------------------
        /// <summary>
        /// Base 값을 설정한 뒤 Modified 캐시를 갱신한다.
        /// </summary>
        // -------------------------------------------------------------------
        public override void Set(T value, bool invokeEvent = true)
        {
            base.Set(value, invokeEvent);

            Refresh(invokeEvent);
        }

    #endregion

    #region 수정자 관리

        // -------------------------------------------------------------------
        /// <summary>
        /// 키를 명시하여 수정자를 추가한다.
        /// </summary>
        // -------------------------------------------------------------------
        public void AddModifier(string key, IModifier<T> modifier, int order = 0, bool invokeEvent = true)
        {
            if (modifier == null)
            {
                throw new ArgumentNullException(nameof(modifier), "추가하려는 수정자가 null입니다.");
            }

            modifiers.Add(order, key, modifier);

            Refresh(invokeEvent);
        }

        // ----------------------------------------------------------------------------------
        /// <summary>
        /// IKeyable<string> 을 구현한 수정자를 자기 키로 추가한다.
        /// </summary>
        // ----------------------------------------------------------------------------------
        public void AddModifier<TModifier>(TModifier modifier, int order = 0, bool invokeEvent = true)
        where TModifier : IModifier<T>, IKeyable<string>
        {
            if (modifier == null)
            {
                throw new ArgumentNullException(nameof(modifier), "추가하려는 수정자가 null입니다.");
            }

            AddModifier(modifier.Key, modifier, order, invokeEvent);
        }

        // ----------------------------------------------------------------------------------
        /// <summary>
        /// 수정자가 IKeyable<string> 을 구현하면 자기 키로 추가한다. 아니면 예외.
        /// </summary>
        // ----------------------------------------------------------------------------------
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
                throw new ArgumentException($"수정자({modifier.GetType().Name})가 IKeyable<string>을 구현하지 않아 키를 추출할 수 없습니다.");
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 키로 수정자를 제거한다.
        /// </summary>
        // ------------------------------------------------------------
        public bool RemoveModifier(string key, bool invokeEvent = true)
        {
            bool removed = modifiers.Remove(key);

            if (removed)
            {
                Refresh(invokeEvent);
            }

            return removed;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 모든 수정자를 제거한다.
        /// </summary>
        // ------------------------------------------------------------
        public void ClearModifiers(bool invokeEvent = true)
        {
            if (modifiers.Count > 0)
            {
                modifiers.Clear();

                Refresh(invokeEvent);
            }
        }

    #endregion

    #region 복제

        // ------------------------------------------------------------
        /// <summary>
        /// 빈 새 인스턴스를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public new MValue<T> @new() => new MValue<T>();

        // ----------------------------------------------------------------------------------
        /// <summary>
        /// source 의 Base, cached, modifiers 를 모두 깊은 복제하여 this 에 채운다.
        /// </summary>
        // ----------------------------------------------------------------------------------
        public void CloneFrom(MValue<T> source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source), "MValue<T>.CloneFrom()의 인자가 null입니다.");
            }

            base.CloneFrom(source);

            cached = source.cached;

            modifiers.CloneFrom(source.modifiers);
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
