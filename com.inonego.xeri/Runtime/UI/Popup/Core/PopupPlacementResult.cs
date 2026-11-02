/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : PopupPlacementResult.cs
수정일 : 2026-10-27

# 설명
Popup 배치의 성공 여부, 최종 Rect, 선택된 방향과 보정 내역을 정의한다.

# 특이사항, 제약사항
실패한 결과(default 포함)의 Bounds와 Direction은 의미가 없으며 Succeeded를 먼저 확인한다.
========================================================================= BLOCK_HEADER_END */

using UnityEngine;

namespace inonego.Xeri.UI
{
    // ============================================================
    /// <summary>
    /// Popup 배치가 실패한 이유.
    /// </summary>
    // ============================================================
    public enum PopupPlacementFailure
    {
        None = 0,
        InvalidInput = 1,
        NoSpace = 2,
    }

    // ============================================================
    /// <summary>
    /// Popup 배치 계산 결과.
    /// </summary>
    // ============================================================
    public readonly struct PopupPlacementResult
    {

    #region 필드

        // ------------------------------------------------------------
        /// <summary>
        /// 배치에 성공했는지 여부.
        /// </summary>
        // ------------------------------------------------------------
        public bool Succeeded { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 배치에 실패한 이유. 성공하면 None이다.
        /// </summary>
        // ------------------------------------------------------------
        public PopupPlacementFailure FailureReason { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 안전 영역과 같은 좌표계의 최종 Popup Rect.
        /// </summary>
        // ------------------------------------------------------------
        public Rect Bounds { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 실제로 선택된 열림 방향.
        /// </summary>
        // ------------------------------------------------------------
        public PopupPlacementDirection Direction { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 선호 방향이 아닌 방향이 선택됐는지 여부.
        /// </summary>
        // ------------------------------------------------------------
        public bool WasFlipped { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 교차축 위치가 안전 영역 안으로 이동됐는지 여부.
        /// </summary>
        // ------------------------------------------------------------
        public bool WasShifted { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 교차축 정렬이 Start↔End로 반전됐는지 여부.
        /// </summary>
        // ------------------------------------------------------------
        public bool WasAlignmentFlipped { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 요청한 크기보다 작게 배치됐는지 여부.
        /// </summary>
        // ------------------------------------------------------------
        public bool WasResized { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 최종 Rect가 기준 영역과 면적이 겹치는지 여부.
        /// </summary>
        // ------------------------------------------------------------
        public bool OverlapsAnchor { get; }

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// 성공한 Popup 배치 결과를 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        public PopupPlacementResult
        (
            Rect bounds,
            PopupPlacementDirection direction,
            bool wasFlipped,
            bool wasAlignmentFlipped,
            bool wasShifted,
            bool wasResized,
            bool overlapsAnchor
        ) : this()
        {
            Succeeded = true;
            FailureReason = PopupPlacementFailure.None;
            Bounds = bounds;
            Direction = direction;
            WasFlipped = wasFlipped;
            WasAlignmentFlipped = wasAlignmentFlipped;
            WasShifted = wasShifted;
            WasResized = wasResized;
            OverlapsAnchor = overlapsAnchor;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 실패한 Popup 배치 결과를 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        public PopupPlacementResult(PopupPlacementFailure failureReason) : this()
        {
            FailureReason = failureReason;
        }

    #endregion

    }
}
