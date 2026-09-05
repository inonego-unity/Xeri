/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : VariantCue.cs
수정일 : 2026-09-05

# 설명
교체 가능한 Variant 중 이번 재생 대상을 선택하는 runtime Cue의 공통 기반을 선언한다.
선택 이력과 직전 선택 제외 정책은 구체 재생 기술과 분리해 Cue 수명에 유지한다.
========================================================================= BLOCK_HEADER_END */

namespace inonego.Xeri.Playback
{
    // ============================================================
    /// <summary>
    /// Variant 선택 상태를 소유하는 runtime Cue의 공통 기반.
    /// </summary>
    // ============================================================
    public abstract class VariantCue : IPlaybackCue
    {

    #region 필드

        private readonly RandomIndexSelector selector = null;

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// 지정 Variant 선택 정책으로 runtime Cue를 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        protected VariantCue(bool excludePrevious)
        {
            selector = new RandomIndexSelector(excludePrevious);
        }

    #endregion

    #region Variant 선택

        // ------------------------------------------------------------
        /// <summary>
        /// 지정 Variant 개수에서 이번 재생 인덱스를 선택한다.
        /// </summary>
        // ------------------------------------------------------------
        protected int SelectVariantIndex(int variantCount) =>
            selector.Select(variantCount);

    #endregion

    }
}
