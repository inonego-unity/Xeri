/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : Filter.cs
수정일 : 2026-11-10

# 설명
IFilter<T> 조건을 AND / OR / NOT 논리 연산으로 조합한다.
Filter_AND/OR/NOT을 객체로 생성하고 각 후보 평가 시 자식 필터를 호출한다.
========================================================================= BLOCK_HEADER_END */

using System;

using UnityEngine;

using inonego.Xeri.Serializable;

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

            return new Filter_AND<T>(left, right);
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

            return new Filter_OR<T>(left, right);
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

            return new Filter_NOT<T>(filter);
        }

    #endregion

    }

    // ============================================================
    /// <summary>
    /// 두 판정을 모두 만족해야 하는 Filter 노드.
    /// </summary>
    // ============================================================
    [Serializable]
    public sealed class Filter_AND<T> : IFilter<T>
    {

    #region 필드

        [SerializeReference, SerializeReferencePicker]
        private IFilter<T> left;

        [SerializeReference, SerializeReferencePicker]
        private IFilter<T> right;

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// Unity managed reference 생성용 생성자.
        /// </summary>
        // ------------------------------------------------------------
        public Filter_AND()
        {
            // NONE
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 조합할 두 판정을 지정한다.
        /// </summary>
        // ------------------------------------------------------------
        public Filter_AND(IFilter<T> left, IFilter<T> right)
        {
            this.left = left ?? throw new ArgumentNullException(nameof(left));
            this.right = right ?? throw new ArgumentNullException(nameof(right));
        }

    #endregion

    #region 메서드

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 입력을 사용해 하위 판정을 평가한다.
        /// </summary>
        // ------------------------------------------------------------
        public bool IsMatch(T value)
        {
            if (left == null || right == null)
            {
                throw new InvalidOperationException("Filter_AND requires both operands.");
            }

            return left.IsMatch(value) && right.IsMatch(value);
        }

    #endregion

    }

    // ============================================================
    /// <summary>
    /// 두 판정 중 하나를 만족하면 되는 Filter 노드.
    /// </summary>
    // ============================================================
    [Serializable]
    public sealed class Filter_OR<T> : IFilter<T>
    {

    #region 필드

        [SerializeReference, SerializeReferencePicker]
        private IFilter<T> left;

        [SerializeReference, SerializeReferencePicker]
        private IFilter<T> right;

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// Unity managed reference 생성용 생성자.
        /// </summary>
        // ------------------------------------------------------------
        public Filter_OR()
        {
            // NONE
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 조합할 두 판정을 지정한다.
        /// </summary>
        // ------------------------------------------------------------
        public Filter_OR(IFilter<T> left, IFilter<T> right)
        {
            this.left = left ?? throw new ArgumentNullException(nameof(left));
            this.right = right ?? throw new ArgumentNullException(nameof(right));
        }

    #endregion

    #region 메서드

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 입력을 사용해 하위 판정을 평가한다.
        /// </summary>
        // ------------------------------------------------------------
        public bool IsMatch(T value)
        {
            if (left == null || right == null)
            {
                throw new InvalidOperationException("Filter_OR requires both operands.");
            }

            return left.IsMatch(value) || right.IsMatch(value);
        }

    #endregion

    }

    // ============================================================
    /// <summary>
    /// 단일 판정을 반전하는 Filter 노드.
    /// </summary>
    // ============================================================
    [Serializable]
    public sealed class Filter_NOT<T> : IFilter<T>
    {

    #region 필드

        [SerializeReference, SerializeReferencePicker]
        private IFilter<T> filter;

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// Unity managed reference 생성용 생성자.
        /// </summary>
        // ------------------------------------------------------------
        public Filter_NOT()
        {
            // NONE
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 조합할 하위 판정을 지정한다.
        /// </summary>
        // ------------------------------------------------------------
        public Filter_NOT(IFilter<T> filter)
        {
            this.filter = filter ?? throw new ArgumentNullException(nameof(filter));
        }

    #endregion

    #region 메서드

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 입력을 사용해 하위 판정을 평가한다.
        /// </summary>
        // ------------------------------------------------------------
        public bool IsMatch(T value)
        {
            if (filter == null)
            {
                throw new InvalidOperationException("Filter_NOT has no assigned filter.");
            }

            return !filter.IsMatch(value);
        }

    #endregion

    }
}
