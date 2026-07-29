/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : PresentationTimeSource.cs
수정일 : 2026-07-29

# 설명
scaled 또는 unscaled 시간 사용 여부를 나타내는 기본 Presentation 시간 정책을 제공한다.
========================================================================= BLOCK_HEADER_END */

namespace inonego.Xeri.UI.Game
{
    // ============================================================
    /// <summary>
    /// 기본 Presentation 시간 공급 정책.
    /// </summary>
    // ============================================================
    public sealed class PresentationTimeSource : IPresentationTimeSource
    {
    #region 필드

        // ------------------------------------------------------------
        /// <summary>
        /// scaled 시간 정책.
        /// </summary>
        // ------------------------------------------------------------
        public static PresentationTimeSource Scaled { get; } = new PresentationTimeSource(false);

        // ------------------------------------------------------------
        /// <summary>
        /// unscaled 시간 정책.
        /// </summary>
        // ------------------------------------------------------------
        public static PresentationTimeSource Unscaled { get; } = new PresentationTimeSource(true);

        // ------------------------------------------------------------
        /// <summary>
        /// unscaled 시간을 사용할지 여부.
        /// </summary>
        // ------------------------------------------------------------
        public bool UseUnscaledTime { get; }

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// 시간 공급 정책을 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        public PresentationTimeSource(bool useUnscaledTime) : base()
        {
            UseUnscaledTime = useUnscaledTime;
        }

    #endregion

    }
}
