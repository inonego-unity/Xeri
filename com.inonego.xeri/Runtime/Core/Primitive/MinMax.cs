/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : MinMax.cs
수정일 : 2026-04-29

# 설명
최솟값·최댓값 쌍을 보관하는 직렬화 가능한 제네릭 구조체.
Min ≤ Max 불변 조건을 setter에서 강제하며, Clamp 유틸리티를 제공한다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace inonego.Xeri.Primitive
{
    // ============================================================
    /// <summary>
    /// Min ≤ Max 조건을 강제하는 범위 구조체.
    /// </summary>
    // ============================================================
    [Serializable]
    public struct MinMax<T> : IEquatable<MinMax<T>>
    where T : struct, IComparable<T>
    {

    #region 필드

        private static readonly EqualityComparer<T> comparer = EqualityComparer<T>.Default;

        // ------------------------------------------------------------
        /// <summary>
        /// 최솟값.
        /// </summary>
        // ------------------------------------------------------------
        [SerializeField]
        private T min;
        public T Min
        {
            get => min;
            set
            {
                if (value.CompareTo(max) > 0)
                {
                    throw new InvalidOperationException($"잘못된 범위입니다. ({value} - {max})");
                }

                min = value;
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 최댓값.
        /// </summary>
        // ------------------------------------------------------------
        [SerializeField]
        private T max;
        public T Max
        {
            get => max;
            set
            {
                if (value.CompareTo(min) < 0)
                {
                    throw new InvalidOperationException($"잘못된 범위입니다. ({min} - {value})");
                }

                max = value;
            }
        }

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// Min과 Max를 지정해 초기화한다. Min ≤ Max여야 한다.
        /// </summary>
        // ------------------------------------------------------------
        public MinMax(T min, T max)
        {
            if (min.CompareTo(max) > 0)
            {
                throw new InvalidOperationException($"잘못된 범위입니다. ({min} - {max})");
            }

            this.min = min;
            this.max = max;
        }

    #endregion

    #region 메서드

        // ------------------------------------------------------------
        /// <summary>
        /// value를 [Min, Max] 범위로 제한해 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public T Clamp(T value)
        {
            if (value.CompareTo(Min) < 0) return Min;
            if (value.CompareTo(Max) > 0) return Max;

            return value;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 동등성 비교.
        /// </summary>
        // ------------------------------------------------------------
        public bool Equals(MinMax<T> other)
        {
            var minEquals = comparer.Equals(min, other.min);
            var maxEquals = comparer.Equals(max, other.max);

            return minEquals && maxEquals;
        }

        public override bool Equals(object obj)
        {
            if (obj is MinMax<T> other)
                return Equals(other);
            return false;
        }

        public override int GetHashCode() => HashCode.Combine(min, max);

        public override string ToString() => $"({min} - {max})";

    #endregion

    #region 암시적 변환

        public static implicit operator MinMax<T>((T Min, T Max) lTuple) => new(lTuple.Min, lTuple.Max);
        public static implicit operator (T Min, T Max)(MinMax<T> minMax) => (minMax.Min, minMax.Max);

    #endregion

    }
}
