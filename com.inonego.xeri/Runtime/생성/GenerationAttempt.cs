/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : GenerationAttempt.cs
수정일 : 2026-08-04

# 설명
제한된 생성 재시도에 사용할 시도 번호·Seed·실패 결과 계약을 정의한다.

# 제약사항
대체 Recipe 선택, Backtrack 범위와 실패 복구 정책은 소비 도메인이 소유한다.
========================================================================= BLOCK_HEADER_END */

using System;

namespace inonego.Xeri.Generation
{
    // ============================================================
    /// <summary>
    /// 하나의 결정적 생성 시도 번호와 전용 Seed를 표현한다.
    /// </summary>
    // ============================================================
    public readonly struct GenerationAttempt
    {
    #region 필드

        // ------------------------------------------------------------
        /// <summary>
        /// 0부터 시작하는 현재 시도 번호다.
        /// </summary>
        // ------------------------------------------------------------
        public int Index => index;

        private readonly int index;

        // ------------------------------------------------------------
        /// <summary>
        /// 해당 시도에서만 사용할 결정적 Seed다.
        /// </summary>
        // ------------------------------------------------------------
        public GenerationSeed Seed => seed;

        private readonly GenerationSeed seed;

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// 시도 번호와 전용 Seed를 만든다.
        /// </summary>
        // ------------------------------------------------------------
        public GenerationAttempt(int index, GenerationSeed seed)
        {
            if (index < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            this.index = index;
            this.seed = seed;
        }

    #endregion
    }

    // ============================================================
    /// <summary>
    /// Node가 성공한 Manifest 또는 실패 원인 중 하나를 반환하는 결과다.
    /// </summary>
    // ============================================================
    public readonly struct GenerationNodeResult<TManifest>
    where TManifest : IGenerationManifest
    {
    #region 필드

        // ------------------------------------------------------------
        /// <summary>
        /// Node가 Manifest 생성에 성공했는지 나타낸다.
        /// </summary>
        // ------------------------------------------------------------
        public bool IsSuccess => isSuccess;

        private readonly bool isSuccess;

        // ------------------------------------------------------------
        /// <summary>
        /// 성공 시 반환된 순수 Manifest다.
        /// </summary>
        // ------------------------------------------------------------
        public TManifest Manifest => manifest;

        private readonly TManifest manifest;

        // ------------------------------------------------------------
        /// <summary>
        /// 실패 시 상위로 전달할 구조화된 원인이다.
        /// </summary>
        // ------------------------------------------------------------
        public GenerationFailure Failure => failure;

        private readonly GenerationFailure failure;

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// 성공한 Manifest 결과를 만든다.
        /// </summary>
        // ------------------------------------------------------------
        private GenerationNodeResult(TManifest manifest)
        {
            if (manifest == null)
            {
                throw new ArgumentNullException(nameof(manifest));
            }

            isSuccess = true;
            this.manifest = manifest;
            failure = default;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 실패 원인을 보관한 결과를 만든다.
        /// </summary>
        // ------------------------------------------------------------
        private GenerationNodeResult(GenerationFailure failure)
        {
            isSuccess = false;
            manifest = default;
            this.failure = failure;
        }

    #endregion

    #region 메서드

        // ------------------------------------------------------------
        /// <summary>
        /// 성공 Manifest 결과를 만든다.
        /// </summary>
        // ------------------------------------------------------------
        public static GenerationNodeResult<TManifest> Succeeded(TManifest manifest)
        {
            return new GenerationNodeResult<TManifest>(manifest);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 구조화된 실패 결과를 만든다.
        /// </summary>
        // ------------------------------------------------------------
        public static GenerationNodeResult<TManifest> Failed(GenerationFailure failure)
        {
            return new GenerationNodeResult<TManifest>(failure);
        }

    #endregion
    }

    // ============================================================
    /// <summary>
    /// 재시도·상위 실패 판단에 필요한 생성 실패 원인을 표현한다.
    /// </summary>
    // ============================================================
    public readonly struct GenerationFailure
    {
    #region 필드

        // ------------------------------------------------------------
        /// <summary>
        /// 실패한 Node 또는 Pass의 안정 Identity다.
        /// </summary>
        // ------------------------------------------------------------
        public GenerationIdentity Identity => identity;

        private readonly GenerationIdentity identity;

        // ------------------------------------------------------------
        /// <summary>
        /// 도메인 Validator 또는 Node가 부여한 안정 원인 Key다.
        /// </summary>
        // ------------------------------------------------------------
        public GenerationKey ReasonKey => reasonKey;

        private readonly GenerationKey reasonKey;

        // ------------------------------------------------------------
        /// <summary>
        /// 실패가 발생한 시도 번호다.
        /// </summary>
        // ------------------------------------------------------------
        public int AttemptIndex => attemptIndex;

        private readonly int attemptIndex;

        // ------------------------------------------------------------
        /// <summary>
        /// 제작자 진단과 로그에 표시할 설명이다.
        /// </summary>
        // ------------------------------------------------------------
        public string Message => message;

        private readonly string message;

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// 실패 Node, 원인, 시도 번호와 설명으로 Failure를 만든다.
        /// </summary>
        // ------------------------------------------------------------
        public GenerationFailure
        (
            GenerationIdentity identity,
            GenerationKey reasonKey,
            int attemptIndex,
            string message
        )
        {
            if (!identity.IsDefined)
            {
                throw new ArgumentException("Generation Failure에는 정의된 Identity가 필요합니다.", nameof(identity));
            }

            if (!reasonKey.IsDefined)
            {
                throw new ArgumentException("Generation Failure에는 정의된 Reason Key가 필요합니다.", nameof(reasonKey));
            }

            if (attemptIndex < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(attemptIndex));
            }

            if (string.IsNullOrWhiteSpace(message))
            {
                throw new ArgumentException("Generation Failure의 Message를 비워 둘 수 없습니다.", nameof(message));
            }

            this.identity = identity;
            this.reasonKey = reasonKey;
            this.attemptIndex = attemptIndex;
            this.message = message;
        }

    #endregion
    }

    // ============================================================
    /// <summary>
    /// 시도별 Seed를 사용해 Manifest를 생성하고 실패를 값으로 반환하는 Node 계약이다.
    /// </summary>
    // ============================================================
    public interface IGenerationAttemptNode<TInput, TManifest>
    where TManifest : IGenerationManifest
    {
        // ------------------------------------------------------------
        /// <summary>
        /// 지정한 Context와 Attempt로 순수 Manifest 또는 Failure를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        GenerationNodeResult<TManifest> Generate(GenerationContext<TInput> context, GenerationAttempt attempt);
    }

    // ============================================================
    /// <summary>
    /// Node 실패 뒤 다음 시도를 허용할지 결정하는 도메인별 정책 계약이다.
    /// </summary>
    // ============================================================
    public interface IGenerationRetryPolicy
    {
        // ------------------------------------------------------------
        /// <summary>
        /// Pipeline이 넘을 수 없는 전체 시도 횟수 상한이다.
        /// </summary>
        // ------------------------------------------------------------
        int MaxAttempts { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 지정 Failure 뒤 다음 시도를 허용할지 판정한다.
        /// </summary>
        // ------------------------------------------------------------
        bool ShouldRetry(GenerationFailure failure, GenerationAttempt attempt);
    }

    // ============================================================
    /// <summary>
    /// 지정한 횟수까지 모든 Failure의 재시도를 허용하는 기본 정책이다.
    /// </summary>
    // ============================================================
    public sealed class GenerationFixedRetryPolicy : IGenerationRetryPolicy
    {
    #region 필드

        // ------------------------------------------------------------
        /// <summary>
        /// 전체 시도 횟수 상한이다.
        /// </summary>
        // ------------------------------------------------------------
        public int MaxAttempts => maxAttempts;

        private readonly int maxAttempts;

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// 전체 시도 횟수 상한으로 기본 정책을 만든다.
        /// </summary>
        // ------------------------------------------------------------
        public GenerationFixedRetryPolicy(int maxAttempts)
        {
            if (maxAttempts < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(maxAttempts));
            }

            this.maxAttempts = maxAttempts;
        }

    #endregion

    #region 메서드

        // ------------------------------------------------------------
        /// <summary>
        /// Pipeline 상한은 Pipeline이 보장하므로 여기서는 다음 시도를 항상 허용한다.
        /// </summary>
        // ------------------------------------------------------------
        public bool ShouldRetry(GenerationFailure failure, GenerationAttempt attempt)
        {
            return true;
        }

    #endregion
    }
}
