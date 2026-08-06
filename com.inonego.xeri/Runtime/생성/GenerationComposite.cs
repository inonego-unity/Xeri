/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : GenerationComposite.cs
수정일 : 2026-08-04

# 설명
부모가 미리 계획한 Child Slot들을 독립 Context로 실행하도록 돕는 Composite 보조 계약이다.

# 제약사항
자식 Manifest의 병합 규칙, 도메인별 실패 처리와 실제 결과 기반 보정은 부모 Recipe가 소유한다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections.Generic;

namespace inonego.Xeri.Generation
{
    // ============================================================
    /// <summary>
    /// 하나의 예약 Child Slot과 그 생성·검증 결과를 함께 보관한다.
    /// </summary>
    // ============================================================
    public readonly struct GenerationChildExecution<TInput, TManifest>
    where TManifest : IGenerationManifest
    {
    #region 필드

        // ------------------------------------------------------------
        /// <summary>
        /// 실행 전에 부모가 확정한 자식 Slot이다.
        /// </summary>
        // ------------------------------------------------------------
        public GenerationChildSlot<TInput> Slot => slot;

        private readonly GenerationChildSlot<TInput> slot;

        // ------------------------------------------------------------
        /// <summary>
        /// 이 Slot의 순수 Manifest와 검증 결과다.
        /// </summary>
        // ------------------------------------------------------------
        public GenerationExecution<TManifest> Execution => execution;

        private readonly GenerationExecution<TManifest> execution;

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// 계획 Slot과 실행 결과를 함께 보관한다.
        /// </summary>
        // ------------------------------------------------------------
        public GenerationChildExecution
        (
            GenerationChildSlot<TInput> slot,
            GenerationExecution<TManifest> execution
        )
        {
            this.slot = slot;
            this.execution = execution;
        }

    #endregion
    }

    // ============================================================
    /// <summary>
    /// 계획 완료된 형제 Slot을 서로 독립적인 Context로 실행하는 도우미다.
    /// </summary>
    // ============================================================
    public static class GenerationComposite
    {
    #region 메서드

        // ------------------------------------------------------------
        /// <summary>
        /// 모든 Child Slot을 실행 시작 전에 고정하고, 각각의 독립 Context로 Pipeline을 실행한다.
        /// </summary>
        // ------------------------------------------------------------
        public static IReadOnlyList<GenerationChildExecution<TInput, TManifest>> GenerateChildren<TInput, TManifest>
        (
            GenerationPipeline<TInput, TManifest> pipeline,
            GenerationSeed parentSeed,
            IReadOnlyList<GenerationChildSlot<TInput>> slots
        )
        where TManifest : IGenerationManifest
        {
            if (pipeline == null)
            {
                throw new ArgumentNullException(nameof(pipeline));
            }

            if (slots == null)
            {
                throw new ArgumentNullException(nameof(slots));
            }

            var identities = new HashSet<GenerationIdentity>();
            var executions = new GenerationChildExecution<TInput, TManifest>[slots.Count];

            // 모든 Identity를 먼저 점검해, 앞선 자식 결과가 뒤 자식 입력에 영향을 주지 않게 한다.
            for (var index = 0; index < slots.Count; index++)
            {
                if (!identities.Add(slots[index].Identity))
                {
                    throw new ArgumentException("Composite의 Child Slot Identity는 중복될 수 없습니다.", nameof(slots));
                }
            }

            for (var index = 0; index < slots.Count; index++)
            {
                var slot = slots[index];
                var context = slot.CreateContext(parentSeed);
                var execution = pipeline.Generate(context);
                executions[index] = new GenerationChildExecution<TInput, TManifest>(slot, execution);
            }

            return executions;
        }

    #endregion
    }
}
