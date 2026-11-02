/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : IPopupPlacementSolver.cs
수정일 : 2026-10-28

# 설명
Popup의 안전 영역과 앵커를 입력받아 최종 배치를 계산하는 Solver 계약.

# 특이사항, 제약사항
모든 Rect는 같은 Y-down Popup 좌표계이며 좌표 변환은 호출자가 책임진다.
구현체는 UI 요소를 조작하지 않고 성공·실패를 PopupPlacementResult로 반환한다.
========================================================================= BLOCK_HEADER_END */

using UnityEngine;

namespace inonego.Xeri.UI
{
    // ============================================================
    /// <summary>
    /// 교체 가능한 Popup 배치 계산 계약.
    /// </summary>
    // ============================================================
    public interface IPopupPlacementSolver
    {
        // ------------------------------------------------------------
        /// <summary>
        /// 기준 영역 주변의 Popup 배치를 계산한다.
        /// </summary>
        // ------------------------------------------------------------
        PopupPlacementResult Solve
        (
            Rect safeRect, Rect anchor,
            Vector2 popupSize, PopupPlacementOptions options
        );
    }
}
