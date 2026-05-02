/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : RangeValue.cs
수정일 : 2026-04-29

# 설명
값을 [Min, Max] 범위로 제한해 관리하는 클래스.
Range(MinMax<T>)가 변경되면 현재값을 즉시 재조정한다.
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
    /// 범위 제한이 있는 값을 관리하는 클래스.
    /// </summary>
    // ============================================================
    [Serializable]
    public class RangeValue<T> : Value<T>, IReadOnlyRangeValue<T>, IDeepCloneable<RangeValue<T>>
    where T : struct, IComparable<T>
    {

    #region 필드

        IReadOnlyValue<MinMax<T>> IReadOnlyRangeValue<T>.Range => range;

        // ------------------------------------------------------------
        /// <summary>
        /// 값을 제한하는 범위.
        /// </summary>
        // ------------------------------------------------------------
        [SerializeField]
        protected Value<MinMax<T>> range = new();
        public Value<MinMax<T>> Range => range;

        // ------------------------------------------------------------
        /// <summary>
        /// 최솟값.
        /// </summary>
        // ------------------------------------------------------------
        public T Min => range.Base.Min;

        // ------------------------------------------------------------
        /// <summary>
        /// 최댓값.
        /// </summary>
        // ------------------------------------------------------------
        public T Max => range.Base.Max;

    #endregion

    #region 생성자

        public RangeValue() : this(default, (default, default)) {}

        // ------------------------------------------------------------
        /// <summary>
        /// 초기 값과 범위를 지정해 초기화한다.
        /// </summary>
        // ------------------------------------------------------------
        public RangeValue(T @base, MinMax<T> range)
        {
            this.range.Base = range;

            this.@base = @base;
            ProcessBase(default, ref this.@base);

            this.range.OnBaseChange += OnRangeChange;
        }

    #endregion

    #region 이벤트 핸들러

        // ------------------------------------------------------------
        /// <summary>
        /// 범위가 변경될 때 현재값을 재조정한다.
        /// </summary>
        // ------------------------------------------------------------
        private void OnRangeChange(object sender, ValueChangeEventArgs<MinMax<T>> e)
        {
            Base = Base;
        }

    #endregion

    #region 메서드

        // ------------------------------------------------------------
        /// <summary>
        /// 값을 범위 내로 제한하는 전처리 훅.
        /// </summary>
        // ------------------------------------------------------------
        protected override void ProcessBase(in T prev, ref T next)
        {
            next = range.Base.Clamp(next);
        }

        // -----------------------------------------------------------------------
        /// <summary>
        /// equality check 없이 Range.OnBaseChange를 강제 발화한다.
        /// Undo 복원 후 backing field가 이미 복원된 상태에서 이벤트를 트리거할 때 사용한다.
        /// </summary>
        // -----------------------------------------------------------------------
        public void InvokeOnRangeChange(MinMax<T> previousRange)
        {
            Range.InvokeOnBaseChange(previousRange);
        }

    #endregion

    #region 복제

        // ------------------------------------------------------------
        /// <summary>
        /// 빈 새 인스턴스를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public new RangeValue<T> @new() => new RangeValue<T>();

        // ------------------------------------------------------------
        /// <summary>
        /// source의 값과 범위를 this에 복사한다.
        /// </summary>
        // ------------------------------------------------------------
        public void CloneFrom(RangeValue<T> source)
        {
            if (source == null)
            {
                throw new ArgumentNullException($"RangeValue<T>.CloneFrom()의 인자가 null입니다.");
            }

            base.CloneFrom(source);

            range.CloneFrom(source.range);
        }

    #endregion

    #region 암시적 변환

        // ------------------------------------------------------------
        /// <summary>
        /// RangeValue&lt;T&gt;에서 T로의 암시적 변환.
        /// </summary>
        // ------------------------------------------------------------
        public static implicit operator T(RangeValue<T> wrapper)
        {
            return wrapper != null ? wrapper.@base : default;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// IComparable&lt;T&gt; 구현.
        /// </summary>
        // ------------------------------------------------------------
        public int CompareTo(T other) => @base.CompareTo(other);

    #endregion

    #region Object 오버라이드

        public override bool Equals(object obj)
        {
            if (obj is RangeValue<T> other)
                return comparer.Equals(@base, other.@base);
            if (obj is T directValue)
                return comparer.Equals(@base, directValue);
            return false;
        }

        public override int GetHashCode() => @base.GetHashCode();

        public override string ToString() => $"{@base} {range.Base}";

    #endregion

    }
}
