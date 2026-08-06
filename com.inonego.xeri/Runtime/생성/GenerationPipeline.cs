/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : GenerationPipeline.cs
수정일 : 2026-08-04

# 설명
순수 Manifest 생성과 검증을 묶되, Runtime 인스턴스화는 호출자에게 남긴다.

# 제약사항
실패 재시도·Backtrack·Prefab 생성과 Unity 수명 관리는 이 Pipeline에 포함하지 않는다.
========================================================================= BLOCK_HEADER_END */

using System;

namespace inonego.Xeri.Generation
{
    // ============================================================
    /// <summary>
    /// Pipeline 검증을 통과한 Manifest만 인스턴스화 경계로 전달하는 값이다.
    /// </summary>
    // ============================================================
    public readonly struct ValidatedGenerationManifest<TManifest>
    where TManifest : IGenerationManifest
    {
    #region 필드

        // ------------------------------------------------------------
        /// <summary>
        /// 검증이 끝난 순수 Manifest다.
        /// </summary>
        // ------------------------------------------------------------
        public TManifest Manifest => manifest;

        private readonly TManifest manifest;

        // ------------------------------------------------------------
        /// <summary>
        /// 이 값이 Pipeline에서 만들어졌는지 나타낸다.
        /// </summary>
        // ------------------------------------------------------------
        public bool IsValidated => isValidated;

        private readonly bool isValidated;

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// Pipeline 내부에서만 검증 완료 Manifest를 만든다.
        /// </summary>
        // ------------------------------------------------------------
        internal ValidatedGenerationManifest(TManifest manifest)
        {
            this.manifest = manifest;
            isValidated = true;
        }

    #endregion
    }

    // ============================================================
    /// <summary>
    /// 하나의 Manifest와 검증 결과를 함께 반환하는 생성 실행 결과다.
    /// </summary>
    // ============================================================
    public readonly struct GenerationExecution<TManifest>
    where TManifest : IGenerationManifest
    {
    #region 필드

        // ------------------------------------------------------------
        /// <summary>
        /// Node가 만든 순수 Manifest다.
        /// </summary>
        // ------------------------------------------------------------
        public TManifest Manifest => manifest;

        private readonly TManifest manifest;

        // ------------------------------------------------------------
        /// <summary>
        /// Manifest에 대한 도메인 Validator 결과다.
        /// </summary>
        // ------------------------------------------------------------
        public GenerationValidationResult Validation => validation;

        private readonly GenerationValidationResult validation;

        // ------------------------------------------------------------
        /// <summary>
        /// 호출자가 인스턴스화를 진행할 수 있는지 나타낸다.
        /// </summary>
        // ------------------------------------------------------------
        public bool IsValid => validation.IsValid;

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// 생성 Manifest와 검증 결과를 함께 보관한다.
        /// </summary>
        // ------------------------------------------------------------
        public GenerationExecution
        (
            TManifest manifest,
            GenerationValidationResult validation
        )
        {
            this.manifest = manifest;
            this.validation = validation;
        }

    #endregion
    }

    // ============================================================
    /// <summary>
    /// Node 생성 후 Validator 검증까지만 수행하는 도메인 독립 Pipeline이다.
    /// </summary>
    // ============================================================
    public sealed class GenerationPipeline<TInput, TManifest>
    where TManifest : IGenerationManifest
    {
    #region 필드

        private readonly IGenerationNode<TInput, TManifest> node;
        private readonly IGenerationValidator<TManifest> validator;

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// 생성 Node와 Manifest Validator로 Pipeline을 만든다.
        /// </summary>
        // ------------------------------------------------------------
        public GenerationPipeline
        (
            IGenerationNode<TInput, TManifest> node,
            IGenerationValidator<TManifest> validator
        )
        {
            this.node = node ?? throw new ArgumentNullException(nameof(node));
            this.validator = validator ?? throw new ArgumentNullException(nameof(validator));
        }

    #endregion

    #region 메서드

        // ------------------------------------------------------------
        /// <summary>
        /// 순수 Manifest를 생성하고 인스턴스화 전에 Validator 결과를 함께 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public GenerationExecution<TManifest> Generate(GenerationContext<TInput> context)
        {
            var manifest = node.Generate(context);

            if (manifest == null)
            {
                throw new InvalidOperationException("Generation Node가 null Manifest를 반환했습니다.");
            }

            // Runtime Object를 만들기 전에 도메인 Validator가 인스턴스화 가능 여부를 확정한다.
            var validation = validator.Validate(manifest);
            return new GenerationExecution<TManifest>(manifest, validation);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 유효한 Execution만 Instantiator에 전달해 Runtime 결과로 바꾼다.
        /// </summary>
        // ------------------------------------------------------------
        public TResult Instantiate<TResult>
        (
            GenerationExecution<TManifest> execution,
            IGenerationInstantiator<TManifest, TResult> instantiator
        )
        {
            if (!execution.IsValid)
            {
                throw new InvalidOperationException("검증 오류가 있는 Generation Manifest는 인스턴스화할 수 없습니다.");
            }

            if (instantiator == null)
            {
                throw new ArgumentNullException(nameof(instantiator));
            }

            var manifest = new ValidatedGenerationManifest<TManifest>(execution.Manifest);
            return instantiator.Instantiate(manifest);
        }

    #endregion
    }
}
