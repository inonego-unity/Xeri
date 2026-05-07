/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : Value.cs
수정일 : 2026-04-29

# 설명
단일 값을 관리하며 변경 이벤트를 발행하는 제네릭 클래스.
ProcessBase 오버라이드로 값 전처리를 확장할 수 있다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace inonego.Xeri.Serializable
{
    using Primitive; // inonego.Xeri.Primitive

    // ============================================================
    /// <summary>
    /// 값을 관리하는 클래스.
    /// </summary>
    // ============================================================
    [Serializable]
    public class Value<T> : IValue<T>, IDeepCloneable<Value<T>>
    {
        protected static readonly EqualityComparer<T> comparer = EqualityComparer<T>.Default;

    #region 필드

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 값.
        /// </summary>
        // ------------------------------------------------------------
        [SerializeField]
        protected T @base;
        public T Base
        {
            get => @base;
            set => Set(value);
        }

    #endregion

    #region 이벤트

        // ------------------------------------------------------------
        /// <summary>
        /// 값이 변경될 때 발생하는 이벤트.
        /// </summary>
        // ------------------------------------------------------------
        public event ValueChangeEventHandler<T> OnBaseChange = null;

    #endregion

    #region 생성자

        public Value() : this(default) {}

        public Value(T value)
        {
            @base = value;
            ProcessBase(default, ref @base);
        }

    #endregion

    #region 메서드

        // ------------------------------------------------------------
        /// <summary>
        /// 값을 설정한다. invokeEvent 가 false 이면 이벤트를 발행하지 않는다.
        /// </summary>
        // ------------------------------------------------------------
        public virtual void Set(T value, bool invokeEvent = true)
        {
            var (prev, next) = (@base, value);

            ProcessBase(prev, ref next);

            if (comparer.Equals(prev, next)) return;

            @base = next;

            if (invokeEvent)
            {
                InvokeOnBaseChange(prev);
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 값을 설정하기 전에 처리하는 훅. 서브클래스에서 오버라이드한다.
        /// </summary>
        // ------------------------------------------------------------
        protected virtual void ProcessBase(in T prev, ref T next) { }

        // ----------------------------------------------------------------------
        /// <summary>
        /// equality check 없이 OnBaseChange를 강제 발화한다.
        /// Undo 복원 후 backing field가 이미 복원된 상태에서 이벤트를 트리거할 때 사용한다.
        /// </summary>
        // ----------------------------------------------------------------------
        public void InvokeOnBaseChange(T previousValue)
        {
            OnBaseChange?.Invoke(this, new(previousValue, @base));
        }

    #endregion

    #region 복제

        // ------------------------------------------------------------
        /// <summary>
        /// 빈 새 인스턴스를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public Value<T> @new() => new Value<T>();

        // ------------------------------------------------------------
        /// <summary>
        /// source의 값을 this에 복사한다.
        /// </summary>
        // ------------------------------------------------------------
        public void CloneFrom(Value<T> source)
        {
            if (source == null)
            {
                throw new ArgumentNullException($"Value<T>.CloneFrom()의 인자가 null입니다.");
            }

            @base = source.@base;
        }

    #endregion

    #region 암시적 변환

        // ------------------------------------------------------------
        /// <summary>
        /// Value&lt;T&gt;에서 T로의 암시적 변환.
        /// </summary>
        // ------------------------------------------------------------
        public static implicit operator T(Value<T> wrapper)
        {
            return wrapper != null ? wrapper.@base : default;
        }

    #endregion

    #region Object 오버라이드

        public override bool Equals(object obj)
        {
            if (obj is Value<T> other)
                return comparer.Equals(@base, other.@base);
            if (obj is T directValue)
                return comparer.Equals(@base, directValue);
            return false;
        }

        public override int GetHashCode() => @base.GetHashCode();

        public override string ToString() => @base.ToString();

    #endregion

    }
}
