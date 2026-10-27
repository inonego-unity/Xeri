/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : PopupPlacementDirection.cs
수정일 : 2026-10-27

# 설명
Popup이 기준 영역에서 열리는 방향과 교차축 정렬 방식을 정의한다.

# 특이사항, 제약사항
Y-down 좌표계(UI Toolkit·Editor 화면 좌표)를 전제로 하며 Below는 yMax 쪽, Above는 yMin 쪽이다.
========================================================================= BLOCK_HEADER_END */

namespace inonego.Xeri.UI
{
    // ============================================================
    /// <summary>
    /// 기준 영역을 기준으로 Popup이 열리는 방향.
    /// </summary>
    // ============================================================
    public enum PopupPlacementDirection
    {
        Below = 0,
        Above = 1,
        Right = 2,
        Left = 3,
    }

    // ============================================================
    /// <summary>
    /// 열림 방향의 교차축에서 Popup을 기준 영역에 맞추는 방식.
    /// </summary>
    // ============================================================
    public enum PopupPlacementAlignment
    {
        Start = 0,
        Center = 1,
        End = 2,
    }
}
