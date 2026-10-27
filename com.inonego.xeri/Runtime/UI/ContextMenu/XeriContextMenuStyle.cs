/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : XeriContextMenuStyle.cs
수정일 : 2026-10-28

# 설명
컨텍스트 메뉴의 테마(색)와 밀도(크기)를 정의한다.
배치와 창 크기 계산에 쓰는 치수는 밀도별로 XeriContextMenuMetrics 한곳에서만 정한다.

# 특이사항, 제약사항
default(XeriContextMenuStyle)은 Auto 테마의 Compact 밀도이며 기본 스타일과 같다.
치수는 메뉴 요소에 직접 적용되므로 USS에서 같은 값을 다시 정하지 않는다.
========================================================================= BLOCK_HEADER_END */

namespace inonego.Xeri.UI
{
    // ============================================================
    /// <summary>
    /// 컨텍스트 메뉴의 색 테마.
    /// </summary>
    // ============================================================
    public enum XeriContextMenuTheme
    {
        Auto = 0,
        Dark = 1,
        Light = 2,
    }

    // ============================================================
    /// <summary>
    /// 컨텍스트 메뉴의 크기 밀도.
    /// </summary>
    // ============================================================
    public enum XeriContextMenuDensity
    {
        Compact = 0,
        Expanded = 1,
    }

    // ============================================================
    /// <summary>
    /// 컨텍스트 메뉴의 테마와 밀도 설정.
    /// </summary>
    // ============================================================
    public readonly struct XeriContextMenuStyle
    {

    #region 프리셋

        // ------------------------------------------------------------
        /// <summary>
        /// 좁은 행과 폭의 작은 메뉴. 테마는 Editor 스킨을 따른다.
        /// </summary>
        // ------------------------------------------------------------
        public static XeriContextMenuStyle Compact => new
        (
            XeriContextMenuTheme.Auto, XeriContextMenuDensity.Compact
        );

        // ------------------------------------------------------------
        /// <summary>
        /// 넓은 행과 폭의 확장 메뉴. 테마는 Editor 스킨을 따른다.
        /// </summary>
        // ------------------------------------------------------------
        public static XeriContextMenuStyle Expanded => new
        (
            XeriContextMenuTheme.Auto, XeriContextMenuDensity.Expanded
        );

    #endregion

    #region 필드

        // ------------------------------------------------------------
        /// <summary>
        /// 메뉴의 색 테마.
        /// </summary>
        // ------------------------------------------------------------
        public XeriContextMenuTheme Theme { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 메뉴의 크기 밀도.
        /// </summary>
        // ------------------------------------------------------------
        public XeriContextMenuDensity Density { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 밀도에 대응하는 배치 치수.
        /// </summary>
        // ------------------------------------------------------------
        public XeriContextMenuMetrics Metrics => XeriContextMenuMetrics.Of(Density);

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// 테마와 밀도를 지정해 스타일을 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        public XeriContextMenuStyle
        (
            XeriContextMenuTheme theme, XeriContextMenuDensity density
        ) : this()
        {
            Theme = theme;
            Density = density;
        }

    #endregion

    }

    // ============================================================
    /// <summary>
    /// 메뉴 창 크기와 배치 계산에 쓰는 치수.
    /// </summary>
    // ============================================================
    public readonly struct XeriContextMenuMetrics
    {

    #region 필드

        // ------------------------------------------------------------
        /// <summary>
        /// 메뉴 한 단계의 폭.
        /// </summary>
        // ------------------------------------------------------------
        public float Width { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 명령 행과 라벨 행의 높이.
        /// </summary>
        // ------------------------------------------------------------
        public float ActionHeight { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 구분선 행의 전체 높이. 구분선 두께와 위아래 여백을 포함한다.
        /// </summary>
        // ------------------------------------------------------------
        public float SeparatorHeight { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 첫 행 위와 마지막 행 아래의 안쪽 여백.
        /// </summary>
        // ------------------------------------------------------------
        public float VerticalPadding { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 메뉴 테두리 두께.
        /// </summary>
        // ------------------------------------------------------------
        public float BorderWidth { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 항목 높이를 제외하고 메뉴 한 단계가 차지하는 위아래 높이.
        /// </summary>
        // ------------------------------------------------------------
        public float ChromeHeight => (VerticalPadding + BorderWidth) * 2f;

        // ------------------------------------------------------------
        /// <summary>
        /// 구분선 한 줄 위와 아래에 각각 두는 여백.
        /// </summary>
        // ------------------------------------------------------------
        public float SeparatorMargin => (SeparatorHeight - SeparatorLineHeight) * 0.5f;

    #endregion

    #region 상수

        // ------------------------------------------------------------
        /// <summary>
        /// 구분선 한 줄의 두께.
        /// </summary>
        // ------------------------------------------------------------
        public const float SeparatorLineHeight = 1f;

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// 치수를 지정해 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        public XeriContextMenuMetrics
        (
            float width, float actionHeight, float separatorHeight,
            float verticalPadding, float borderWidth
        ) : this()
        {
            Width = width;
            ActionHeight = actionHeight;
            SeparatorHeight = separatorHeight;
            VerticalPadding = verticalPadding;
            BorderWidth = borderWidth;
        }

    #endregion

    #region 밀도별 치수

        // ------------------------------------------------------------
        /// <summary>
        /// 밀도에 대응하는 치수를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public static XeriContextMenuMetrics Of(XeriContextMenuDensity density)
        {
            return density == XeriContextMenuDensity.Expanded
                ? new XeriContextMenuMetrics(268f, 29f, 11f, 4f, 1f)
                : new XeriContextMenuMetrics(220f, 22f, 7f, 2f, 1f);
        }

    #endregion

    }
}
