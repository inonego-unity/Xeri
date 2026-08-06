/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : GenerationContracts.cs
수정일 : 2026-08-04

# 설명
도메인 생성 Node, Manifest, Validator와 인스턴스화 경계가 공유할 최소 계약을 정의한다.

# 제약사항
Composite 병합 규칙, Runtime Object 생성, 재시도·Backtrack 정책은 소비 도메인이 소유한다.
========================================================================= BLOCK_HEADER_END */

namespace inonego.Xeri.Generation
{
    // ============================================================
    /// <summary>
    /// 검증 후 인스턴스화할 순수 생성 결과가 제공해야 하는 공통 식별 정보다.
    /// </summary>
    // ============================================================
    public interface IGenerationManifest
    {
    #region 필드

        // ------------------------------------------------------------
        /// <summary>
        /// Manifest를 만든 생성 Node 또는 Pass의 안정 식별자다.
        /// </summary>
        // ------------------------------------------------------------
        public GenerationIdentity Identity { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 이 Manifest를 만든 Subtree Seed다.
        /// </summary>
        // ------------------------------------------------------------
        public GenerationSeed Seed { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// Generator 또는 Recipe의 호환성 판단용 버전 문자열이다.
        /// </summary>
        // ------------------------------------------------------------
        public string GeneratorVersion { get; }

    #endregion
    }

    // ============================================================
    /// <summary>
    /// 도메인 입력 Context로부터 순수 Manifest를 만드는 Composite 또는 Leaf Node 계약이다.
    /// </summary>
    // ============================================================
    public interface IGenerationNode<TInput, TManifest>
    where TManifest : IGenerationManifest
    {
    #region 메서드

        // ------------------------------------------------------------
        /// <summary>
        /// Unity Runtime Object를 만들지 않고 Context에 대응하는 Manifest를 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        public TManifest Generate(GenerationContext<TInput> context);

    #endregion
    }

    // ============================================================
    /// <summary>
    /// 생성된 Manifest가 도메인 제약을 만족하는지 판정하는 계약이다.
    /// </summary>
    // ============================================================
    public interface IGenerationValidator<TManifest>
    where TManifest : IGenerationManifest
    {
    #region 메서드

        // ------------------------------------------------------------
        /// <summary>
        /// Manifest의 유효성과 진단 목록을 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public GenerationValidationResult Validate(TManifest manifest);

    #endregion
    }

    // ============================================================
    /// <summary>
    /// 검증된 Manifest를 소비 도메인의 Runtime 결과로 변환하는 경계다.
    /// </summary>
    // ============================================================
    public interface IGenerationInstantiator<TManifest, TResult>
    where TManifest : IGenerationManifest
    {
    #region 메서드

        // ------------------------------------------------------------
        /// <summary>
        /// Pipeline이 검증한 Manifest만 도메인 Runtime 결과로 변환한다.
        /// </summary>
        // ------------------------------------------------------------
        public TResult Instantiate(ValidatedGenerationManifest<TManifest> manifest);

    #endregion
    }
}
