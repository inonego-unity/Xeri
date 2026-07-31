/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : PlacementAlignment.cs
수정일 : 2026-07-31

# 설명
UI 배치 대상이 Anchor를 기준으로 정렬되는 방향을 정의한다.
========================================================================= BLOCK_HEADER_END */

namespace inonego.Xeri.UI.Game
{
    // ============================================================
    /// <summary>
    /// UI Placement 정렬 방향.
    /// </summary>
    // ============================================================
    public enum PlacementAlignment
    {
        Center = 0,
        Top = 1,
        Bottom = 2,
        Left = 3,
        Right = 4,
        TopLeft = 5,
        TopRight = 6,
        BottomLeft = 7,
        BottomRight = 8,
    }
}
