/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : GenerationRetryPipeline.cs
수정일 : 2026-08-04

# 설명
값 기반 Failure와 유한 Retry Policy를 사용해 생성·검증을 수행한다.

# 제약사항
대체 Recipe 선택과 부모 Backtrack은 이 Pipeline이 아니라 도메인 Composite가 결정한다.
========================================================================= BLOCK_HEADER_END */

using System;

namespace inonego.Xeri.Generation
{
    // ============================================================
    /// <summary>
    /// 유한 재시도 생성의 성공 Execution 또는 마지막 Failure를 보관한다.
    /// </summary>
    // ============================================================
    public readonly struct GenerationRetryExecution<TManifest>
    where TManifest : IGenerationManifest
    {
    #region 필드

        // ------------------------------------------------------------
        /// <summary>
        /// Node가 성공한 뒤 Validator까지 수행했는지 나타낸다.
        /// </summary>
        // ------------------------------------------------------------
        public bool HasExecution => hasExecution;

        private readonly bool hasExecution;

        // ------------------------------------------------------------
        /// <summary>
        /// 성공 시 Manifest와 Validator 결과다.
        /// </summary>
        // ------------------------------------------------------------
        public GenerationExecution<TManifest> Execution => execution;

        private readonly GenerationExecution<TManifest> execution;

        // ------------------------------------------------------------
        /// <summary>
        /// 모든 시도가 실패했거나 Policy가 중단한 경우의 마지막 Failure다.
        /// </summary>
        // ------------------------------------------------------------
        public GenerationFailure Failure => failure;

        private readonly GenerationFailure failure;

        // ------------------------------------------------------------
        /// <summary>
        /// 수행한 실제 시도 횟수다.
        /// </summary>
        // ------------------------------------------------------------
        public int AttemptCount => attemptCount;

        private readonly int attemptCount;

        // ------------------------------------------------------------
        /// <summary>
        /// Node 생성과 Validator 검증이 모두 성공했는지 나타낸다.
        /// </summary>
        // ------------------------------------------------------------
        public bool IsValid => hasExecution && execution.IsValid;

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// 생성과 검증까지 끝난 성공 결과를 만든다.
        /// </summary>
        // ------------------------------------------------------------
        public GenerationRetryExecution(GenerationExecution<TManifest> execution, int attemptCount)
        {
            if (attemptCount < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(attemptCount));
            }

            hasExecution = true;
            this.execution = execution;
            failure = default;
            this.attemptCount = attemptCount;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Manifest를 만들지 못한 실패 결과를 만든다.
        /// </summary>
        // ------------------------------------------------------------
        public GenerationRetryExecution(GenerationFailure failure, int attemptCount)
        {
            if (attemptCount < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(attemptCount));
            }

            hasExecution = false;
            execution = default;
            this.failure = failure;
            this.attemptCount = attemptCount;
        }

    #endregion
    }

    // ============================================================
    /// <summary>
    /// 무한 재시도 없이 값 기반 Failure를 상위로 전달하는 생성 Pipeline이다.
    /// </summary>
    // ============================================================
    public sealed class GenerationRetryPipeline<TInput, TManifest>
    where TManifest : IGenerationManifest
    {
    #region 필드

        private readonly IGenerationAttemptNode<TInput, TManifest> node;
        private readonly IGenerationValidator<TManifest> validator;
        private readonly IGenerationRetryPolicy retryPolicy;

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// 재시도 Node, Validator, 유한 Retry Policy로 Pipeline을 만든다.
        /// </summary>
        // ------------------------------------------------------------
        public GenerationRetryPipeline
        (
            IGenerationAttemptNode<TInput, TManifest> node,
            IGenerationValidator<TManifest> validator,
            IGenerationRetryPolicy retryPolicy
        )
        {
            this.node = node ?? throw new ArgumentNullException(nameof(node));
            this.validator = validator ?? throw new ArgumentNullException(nameof(validator));
            this.retryPolicy = retryPolicy ?? throw new ArgumentNullException(nameof(retryPolicy));

            if (retryPolicy.MaxAttempts < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(retryPolicy));
            }
        }

    #endregion

    #region 메서드

        // ------------------------------------------------------------
        /// <summary>
        /// Policy의 상한 안에서 Node를 재시도하고, 성공 시 Manifest 검증 결과를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public GenerationRetryExecution<TManifest> Generate(GenerationContext<TInput> context)
        {
            for (var index = 0; index < retryPolicy.MaxAttempts; index++)
            {
                var attempt = new GenerationAttempt(index, context.Seed.DeriveAttempt(index));
                var result = node.Generate(context, attempt);

                if (result.IsSuccess)
                {
                    var validation = validator.Validate(result.Manifest);
                    var execution = new GenerationExecution<TManifest>(result.Manifest, validation);
                    return new GenerationRetryExecution<TManifest>(execution, index + 1);
                }

                if (index + 1 >= retryPolicy.MaxAttempts || !retryPolicy.ShouldRetry(result.Failure, attempt))
                {
                    return new GenerationRetryExecution<TManifest>(result.Failure, index + 1);
                }
            }

            throw new InvalidOperationException("Generation Retry Pipeline이 유효한 종료 결과를 만들지 못했습니다.");
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 유효한 Retry Execution만 Instantiator에 전달해 Runtime 결과로 바꾼다.
        /// </summary>
        // ------------------------------------------------------------
        public TResult Instantiate<TResult>
        (
            GenerationRetryExecution<TManifest> execution,
            IGenerationInstantiator<TManifest, TResult> instantiator
        )
        {
            if (!execution.IsValid)
            {
                throw new InvalidOperationException("성공하지 않았거나 검증 오류가 있는 Generation 결과는 인스턴스화할 수 없습니다.");
            }

            if (instantiator == null)
            {
                throw new ArgumentNullException(nameof(instantiator));
            }

            var manifest = new ValidatedGenerationManifest<TManifest>(execution.Execution.Manifest);
            return instantiator.Instantiate(manifest);
        }

    #endregion
    }
}
