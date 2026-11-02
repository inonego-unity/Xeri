/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : RangeValue.cs
수정일 : 2026-10-29

# 설명
값을 [Min, Max] 범위로 제한해 관리하는 클래스.
Range(Range<T>)가 변경되면 현재값을 즉시 재조정한다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using inonego;
using inonego.Xeri;
using inonego.Xeri.Primitive;

namespace inonego.Xeri.Serializable
{

    // ============================================================
    /// <summary>
    /// 범위 제한이 있는 값을 관리하는 클래스.
    /// </summary>
    // ============================================================
    [Serializable]
    public class RangeValue<T> : Value<T>, IRangeValue<T>, ISerializationCallbackReceiver
    where T : struct, IComparable<T>
    {

    #region 필드

        IReadOnlyValue<Range<T>> IReadOnlyRangeValue<T>.Range => range;
        IValue<Range<T>>         IRangeValue<T>        .Range => range;

        // ------------------------------------------------------------
        /// <summary>
        /// 값을 제한하는 범위.
        /// </summary>
        // ------------------------------------------------------------
        [SerializeField]
        protected Value<Range<T>> range = new();
        public Value<Range<T>> Range => range;

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

        [NonSerialized]
        private Value<Range<T>> subscribedRange;

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// 기본 범위를 초기화한다.
        /// </summary>
        // ------------------------------------------------------------
        public RangeValue() : this(default, (default, default))
        {
            // NONE
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 초기 값과 범위를 지정해 초기화한다.
        /// </summary>
        // ------------------------------------------------------------
        public RangeValue(T @base, Range<T> range) : base(@base)
        {
            this.range.Set(range, invokeEvent: false);
            InitializeBase(@base);
            BindRange();
        }

    #endregion

    #region Range 내부 연동

        // ------------------------------------------------------------
        /// <summary>
        /// Range 공개 이벤트 이전에 Base를 새 범위로 제한한다.
        /// </summary>
        // ------------------------------------------------------------
        private void OnRangeApplied(Range<T> previous, Range<T> current, bool invokeEvent)
        {
            Set(Base, invokeEvent);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Range 교체와 역직렬화 후에도 내부 구독을 하나만 유지한다.
        /// </summary>
        // ------------------------------------------------------------
        private void BindRange()
        {
            range ??= new Value<Range<T>>();
            if (subscribedRange != null)
            {
                subscribedRange.OnBaseApplied -= OnRangeApplied;
            }

            subscribedRange = range;
            subscribedRange.OnBaseApplied += OnRangeApplied;
        }

    #endregion

    #region 직렬화 변경 확정 및 상태 복구

        // ------------------------------------------------------------
        /// <summary>
        /// 직렬화로 적용된 Base·Range 변경과 Base 제한을 확정한다.
        /// </summary>
        // ------------------------------------------------------------
        internal void FinalizeSerializedChange(T previousBase, Range<T> previousRange)
        {
            BindRange();
            var currentRange = Range.Base;
            if (currentRange.Min.CompareTo(currentRange.Max) > 0)
            {
                return;
            }

            var currentBase = currentRange.Clamp(Base);
            @base = currentBase;

            if (!comparer.Equals(previousBase, currentBase))
            {
                PublishSerializedChange(previousBase, currentBase);
            }

            if (!previousRange.Equals(currentRange))
            {
                Range.PublishSerializedChange(previousRange, currentRange);
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Unity 역직렬화에서 내부 구독과 Base 제한을 이벤트 없이 복원한다.
        /// </summary>
        // ------------------------------------------------------------
        public void OnAfterDeserialize()
        {
            BindRange();
            if (range.Base.Min.CompareTo(range.Base.Max) <= 0)
            {
                @base = range.Base.Clamp(Base);
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 직렬화 전에 수행할 추가 작업은 없다.
        /// </summary>
        // ------------------------------------------------------------
        public void OnBeforeSerialize()
        {
            // NONE
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
            if (obj is RangeValue<T> other && other.GetType() == GetType())
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
