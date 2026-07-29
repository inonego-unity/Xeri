/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : IPresentationTimeSource.cs
수정일 : 2026-07-29

# 설명
Presentation Transition이 scaled 또는 unscaled 시간을 선택하는 계약을 정의한다.
========================================================================= BLOCK_HEADER_END */

namespace inonego.Xeri.UI.Game
{
    // ============================================================
    /// <summary>
    /// Presentation Transition 시간 공급 정책.
    /// </summary>
    // ============================================================
    public interface IPresentationTimeSource
    {
        // ------------------------------------------------------------
        /// <summary>
        /// unscaled 시간을 사용할지 여부.
        /// </summary>
        // ------------------------------------------------------------
        bool UseUnscaledTime { get; }
    }
}
