/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : GenerationChildSlot.cs
수정일 : 2026-08-04

# 설명
부모 Planning Pass가 자식 실행 전에 확정하는 안정 Identity와 읽기 전용 입력을 표현한다.

# 제약사항
형제 결과를 다음 자식 입력으로 반영하거나, 도메인별 공간·예산 타입을 이 Core가 소유하지 않는다.
========================================================================= BLOCK_HEADER_END */

using System;

namespace inonego.Xeri.Generation
{
    // ============================================================
    /// <summary>
    /// 하나의 자식 Subtree에 예약된 Identity와 불변 입력을 보관한다.
    /// </summary>
    // ============================================================
    public readonly struct GenerationChildSlot<TInput>
    {
    #region 필드

        // ------------------------------------------------------------
        /// <summary>
        /// 부모가 자식에 할당한 Recipe·Slot·Pass 식별자다.
        /// </summary>
        // ------------------------------------------------------------
        public GenerationIdentity Identity => identity;

        private readonly GenerationIdentity identity;

        // ------------------------------------------------------------
        /// <summary>
        /// 자식 생성 중 변경되지 않는 도메인별 입력이다.
        /// </summary>
        // ------------------------------------------------------------
        public TInput Input => input;

        private readonly TInput input;

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// 자식 Subtree의 안정 Identity와 입력을 예약한다.
        /// </summary>
        // ------------------------------------------------------------
        public GenerationChildSlot(GenerationIdentity identity, TInput input)
        {
            if (!identity.IsDefined)
            {
                throw new ArgumentException("Generation Child Slot에는 정의된 Identity가 필요합니다.", nameof(identity));
            }

            this.identity = identity;
            this.input = input;
        }

    #endregion

    #region 메서드

        // ------------------------------------------------------------
        /// <summary>
        /// 부모 Seed에서 파생한 자식 전용 Context를 만든다.
        /// </summary>
        // ------------------------------------------------------------
        public GenerationContext<TInput> CreateContext(GenerationSeed parentSeed)
        {
            return new GenerationContext<TInput>
            (
                identity,
                parentSeed.Derive(identity),
                input
            );
        }

    #endregion
    }
}
