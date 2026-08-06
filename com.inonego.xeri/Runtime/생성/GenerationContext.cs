/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : GenerationContext.cs
수정일 : 2026-08-04

# 설명
부모가 확정한 Seed·Identity와 도메인별 읽기 전용 입력을 생성 Node에 전달한다.

# 제약사항
Bounds, Field, Budget 같은 실제 입력 타입은 이 Core가 아니라 소비 도메인이 정의한다.
========================================================================= BLOCK_HEADER_END */

using System;

namespace inonego.Xeri.Generation
{
    // ============================================================
    /// <summary>
    /// 부모가 확정한 샘플 입력에서 값을 계산하는 생성 Field 계약이다.
    /// </summary>
    // ============================================================
    public interface IGenerationField<in TSample, out TValue>
    {
        // ------------------------------------------------------------
        /// <summary>
        /// Field를 식별하는 안정 Key다.
        /// </summary>
        // ------------------------------------------------------------
        GenerationKey Key { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 부모가 전달한 샘플 입력에서 읽기 전용 값을 계산한다.
        /// </summary>
        // ------------------------------------------------------------
        TValue Sample(TSample sample);
    }

    // ============================================================
    /// <summary>
    /// 어느 샘플에서도 같은 값을 반환하는 가장 단순한 Generation Field다.
    /// </summary>
    // ============================================================
    public sealed class GenerationConstantField<TSample, TValue> : IGenerationField<TSample, TValue>
    {
    #region 필드

        // ------------------------------------------------------------
        /// <summary>
        /// Field를 식별하는 안정 Key다.
        /// </summary>
        // ------------------------------------------------------------
        public GenerationKey Key => key;

        private readonly GenerationKey key;

        // ------------------------------------------------------------
        /// <summary>
        /// 모든 샘플에서 반환할 읽기 전용 값이다.
        /// </summary>
        // ------------------------------------------------------------
        public TValue Value => value;

        private readonly TValue value;

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// 안정 Key와 상수 값으로 Field를 만든다.
        /// </summary>
        // ------------------------------------------------------------
        public GenerationConstantField(GenerationKey key, TValue value)
        {
            if (!key.IsDefined)
            {
                throw new ArgumentException("Generation Field에는 정의된 Key가 필요합니다.", nameof(key));
            }

            this.key = key;
            this.value = value;
        }

    #endregion

    #region 메서드

        // ------------------------------------------------------------
        /// <summary>
        /// 샘플 위치와 무관하게 상수 값을 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public TValue Sample(TSample sample)
        {
            return value;
        }

    #endregion
    }

    // ============================================================
    /// <summary>
    /// 하나의 Node가 생성에 필요한 Identity·Seed와 도메인 입력을 읽기 전용으로 전달한다.
    /// </summary>
    // ============================================================
    public readonly struct GenerationContext<TInput>
    {
    #region 필드

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 Node 또는 Pass의 안정 식별자다.
        /// </summary>
        // ------------------------------------------------------------
        public GenerationIdentity Identity => identity;

        private readonly GenerationIdentity identity;

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 Subtree 전용 Seed다.
        /// </summary>
        // ------------------------------------------------------------
        public GenerationSeed Seed => seed;

        private readonly GenerationSeed seed;

        // ------------------------------------------------------------
        /// <summary>
        /// 부모 또는 호출자가 확정한 도메인별 읽기 전용 입력이다.
        /// </summary>
        // ------------------------------------------------------------
        public TInput Input => input;

        private readonly TInput input;

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// 하나의 생성 Node가 사용할 Context를 만든다.
        /// </summary>
        // ------------------------------------------------------------
        public GenerationContext
        (
            GenerationIdentity identity,
            GenerationSeed seed,
            TInput input
        )
        {
            if (!identity.IsDefined)
            {
                throw new ArgumentException("Generation Context에는 정의된 Identity가 필요합니다.", nameof(identity));
            }

            this.identity = identity;
            this.seed = seed;
            this.input = input;
        }

    #endregion

    #region 메서드

        // ------------------------------------------------------------
        /// <summary>
        /// 부모 Seed에서 파생한 독립 Seed와 함께 자식 Context를 만든다.
        /// </summary>
        // ------------------------------------------------------------
        public GenerationContext<TChildInput> CreateChild<TChildInput>
        (
            GenerationIdentity childIdentity,
            TChildInput childInput
        )
        {
            return new GenerationContext<TChildInput>
            (
                childIdentity,
                seed.Derive(childIdentity),
                childInput
            );
        }

    #endregion
    }
}
