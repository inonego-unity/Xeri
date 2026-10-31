/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : Filter.cs
수정일 : 2026-10-31

# 설명
IFilter<T> 조건을 AND / OR / NOT 논리 연산으로 조합한다.
각 후보를 평가할 때 조합된 필터를 호출하며 결과를 별도로 저장하지 않는다.
========================================================================= BLOCK_HEADER_END */

using System;

namespace inonego.Xeri
{
    // ============================================================
    /// <summary>
    /// 범용 필터를 논리 연산으로 조합한다.
    /// </summary>
    // ============================================================
    public static class Filter
    {

    #region 논리 조합

        // ------------------------------------------------------------
        /// <summary>
        /// 두 필터가 모두 일치할 때만 후보를 허용한다.
        /// </summary>
        // ------------------------------------------------------------
        public static IFilter<T> AND<T>(IFilter<T> left, IFilter<T> right)
        {
            if (left == null)
            {
                throw new ArgumentNullException(nameof(left));
            }

            if (right == null)
            {
                throw new ArgumentNullException(nameof(right));
            }

            return new PredicateFilter<T>
            (
                candidate => left.IsMatch(candidate) && right.IsMatch(candidate)
            );
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 두 필터 중 하나 이상 일치하면 후보를 허용한다.
        /// </summary>
        // ------------------------------------------------------------
        public static IFilter<T> OR<T>(IFilter<T> left, IFilter<T> right)
        {
            if (left == null)
            {
                throw new ArgumentNullException(nameof(left));
            }

            if (right == null)
            {
                throw new ArgumentNullException(nameof(right));
            }

            return new PredicateFilter<T>
            (
                candidate => left.IsMatch(candidate) || right.IsMatch(candidate)
            );
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 필터의 판정 결과를 반전한다.
        /// </summary>
        // ------------------------------------------------------------
        public static IFilter<T> NOT<T>(IFilter<T> filter)
        {
            if (filter == null)
            {
                throw new ArgumentNullException(nameof(filter));
            }

            return new PredicateFilter<T>
            (
                candidate => !filter.IsMatch(candidate)
            );
        }

    #endregion

    }
}
