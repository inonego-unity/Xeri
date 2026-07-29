/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : PlacementOptions.cs
수정일 : 2026-07-29

# 설명
UI Placement 정렬, offset, padding과 Safe Area clamp 정책을 정의한다.
========================================================================= BLOCK_HEADER_END */

using UnityEngine;

namespace inonego.Xeri.UI.Game
{
    // ============================================================
    /// <summary>
    /// UI Placement 계산 옵션.
    /// </summary>
    // ============================================================
    public readonly struct PlacementOptions
    {
    #region 필드

        // ------------------------------------------------------------
        /// <summary>
        /// Anchor 기준 정렬 방향.
        /// </summary>
        // ------------------------------------------------------------
        public PlacementAlignment Alignment { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 정렬 결과에 더할 로컬 offset.
        /// </summary>
        // ------------------------------------------------------------
        public Vector2 Offset { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 배치 영역 안쪽 여백.
        /// </summary>
        // ------------------------------------------------------------
        public Vector2 Padding { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 배치 결과를 현재 영역 안으로 제한할지 여부.
        /// </summary>
        // ------------------------------------------------------------
        public bool ClampToBounds { get; }

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// UI Placement 옵션을 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        public PlacementOptions
        (
            PlacementAlignment alignment,
            Vector2 offset,
            Vector2 padding,
            bool clampToBounds = true
        ) : this()
        {
            Alignment = alignment;
            Offset = offset;
            Padding = padding;
            ClampToBounds = clampToBounds;
        }

    #endregion

    }
}
