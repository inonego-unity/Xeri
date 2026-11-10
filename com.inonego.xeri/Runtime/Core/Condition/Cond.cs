/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : Cond.cs
수정일 : 2026-11-10

# 설명
ICond<TContext> 판정을 AND / OR / NOT 논리 연산으로 조합한다.
# 특이사항, 제약사항
정적 조합 API는 직렬화 가능 객체 Cond_AND/OR/NOT을 생성한다. Unity의 제네릭 managed reference 직렬화는 별도 검증 대상이다.
========================================================================= BLOCK_HEADER_END */

using System;

using UnityEngine;

using inonego.Xeri.Serializable;

namespace inonego.Xeri
{
    // ============================================================
    /// <summary>
    /// 범용 Condition을 논리 연산으로 조합한다.
    /// </summary>
    // ============================================================
    public static class Cond
    {

    #region 논리 조합

        // ------------------------------------------------------------
        /// <summary>
        /// 양쪽 조건을 모두 만족할 때만 참을 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public static ICond<TContext> AND<TContext>(
            ICond<TContext> left, ICond<TContext> right
        )
        {
            if (left == null)
            {
                throw new ArgumentNullException(nameof(left));
            }

            if (right == null)
            {
                throw new ArgumentNullException(nameof(right));
            }

            return new Cond_AND<TContext>(left, right);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 어느 한쪽 조건이라도 만족하면 참을 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public static ICond<TContext> OR<TContext>(
            ICond<TContext> left, ICond<TContext> right
        )
        {
            if (left == null)
            {
                throw new ArgumentNullException(nameof(left));
            }

            if (right == null)
            {
                throw new ArgumentNullException(nameof(right));
            }

            return new Cond_OR<TContext>(left, right);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 지정 조건의 판정 결과를 반전한다.
        /// </summary>
        // ------------------------------------------------------------
        public static ICond<TContext> NOT<TContext>(ICond<TContext> condition)
        {
            if (condition == null)
            {
                throw new ArgumentNullException(nameof(condition));
            }

            return new Cond_NOT<TContext>(condition);
        }

    #endregion

    }

    // ============================================================
    /// <summary>
    /// 두 판정을 모두 만족해야 하는 Cond 노드.
    /// </summary>
    // ============================================================
    [Serializable]
    public sealed class Cond_AND<TContext> : ICond<TContext>
    {

    #region 필드

        [SerializeReference, SerializeReferencePicker]
        private ICond<TContext> left;

        [SerializeReference, SerializeReferencePicker]
        private ICond<TContext> right;

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// Unity managed reference 생성용 생성자.
        /// </summary>
        // ------------------------------------------------------------
        public Cond_AND()
        {
            // NONE
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 조합할 두 판정을 지정한다.
        /// </summary>
        // ------------------------------------------------------------
        public Cond_AND(ICond<TContext> left, ICond<TContext> right)
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
        public bool Check(TContext context)
        {
            if (left == null || right == null)
            {
                throw new InvalidOperationException("Cond_AND requires both operands.");
            }

            return left.Check(context) && right.Check(context);
        }

    #endregion

    }

    // ============================================================
    /// <summary>
    /// 두 판정 중 하나를 만족하면 되는 Cond 노드.
    /// </summary>
    // ============================================================
    [Serializable]
    public sealed class Cond_OR<TContext> : ICond<TContext>
    {

    #region 필드

        [SerializeReference, SerializeReferencePicker]
        private ICond<TContext> left;

        [SerializeReference, SerializeReferencePicker]
        private ICond<TContext> right;

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// Unity managed reference 생성용 생성자.
        /// </summary>
        // ------------------------------------------------------------
        public Cond_OR()
        {
            // NONE
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 조합할 두 판정을 지정한다.
        /// </summary>
        // ------------------------------------------------------------
        public Cond_OR(ICond<TContext> left, ICond<TContext> right)
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
        public bool Check(TContext context)
        {
            if (left == null || right == null)
            {
                throw new InvalidOperationException("Cond_OR requires both operands.");
            }

            return left.Check(context) || right.Check(context);
        }

    #endregion

    }

    // ============================================================
    /// <summary>
    /// 단일 판정을 반전하는 Cond 노드.
    /// </summary>
    // ============================================================
    [Serializable]
    public sealed class Cond_NOT<TContext> : ICond<TContext>
    {

    #region 필드

        [SerializeReference, SerializeReferencePicker]
        private ICond<TContext> condition;

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// Unity managed reference 생성용 생성자.
        /// </summary>
        // ------------------------------------------------------------
        public Cond_NOT()
        {
            // NONE
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 조합할 하위 판정을 지정한다.
        /// </summary>
        // ------------------------------------------------------------
        public Cond_NOT(ICond<TContext> condition)
        {
            this.condition = condition ?? throw new ArgumentNullException(nameof(condition));
        }

    #endregion

    #region 메서드

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 입력을 사용해 하위 판정을 평가한다.
        /// </summary>
        // ------------------------------------------------------------
        public bool Check(TContext context)
        {
            if (condition == null)
            {
                throw new InvalidOperationException("Cond_NOT has no assigned condition.");
            }

            return !condition.Check(context);
        }

    #endregion

    }
}
