/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : PredicateCond.cs
수정일 : 2026-11-10

# 설명
런타임의 함수 기반 판정을 범용 ICond 계약에 연결한다.
# 특이사항, 제약사항
null 판정 함수는 설정 오류로 거부하며 Func 자체는 Unity 직렬화 대상이 아니다.
========================================================================= BLOCK_HEADER_END */

using System;

namespace inonego.Xeri
{
    // ============================================================
    /// <summary>
    /// Func<TContext, bool> 판정 함수를 ICond<TContext> 계약으로 연결한다.
    /// </summary>
    // ============================================================
    public sealed class PredicateCond<TContext> : ICond<TContext>
    {

    #region 필드

        private readonly Func<TContext, bool> predicate;

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// 유효한 판정 함수를 보관한다.
        /// </summary>
        // ------------------------------------------------------------
        public PredicateCond(Func<TContext, bool> predicate)
        {
            this.predicate = predicate ?? throw new ArgumentNullException(nameof(predicate));
        }

    #endregion

    #region 메서드

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 Context가 요구사항을 충족하는지 검사한다.
        /// </summary>
        // ------------------------------------------------------------
        public bool Check(TContext context) => predicate(context);

    #endregion

    }
}
