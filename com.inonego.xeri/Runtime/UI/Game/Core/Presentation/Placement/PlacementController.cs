/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : PlacementController.cs
수정일 : 2026-07-29

# 설명
현재 Safe Area 로컬 Rect를 기준으로 UI 요소의 정렬 위치와 clamp를 계산한다.
========================================================================= BLOCK_HEADER_END */

using System;

using UnityEngine;

namespace inonego.Xeri.UI.Game
{
    // ============================================================
    /// <summary>
    /// UI 요소의 현재 영역 배치 위치를 계산한다.
    /// </summary>
    // ============================================================
    public sealed class PlacementController
    {
    #region 메서드

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 Safe Area RectTransform을 읽어 UI 요소 배치 위치를 계산한다.
        /// </summary>
        // ------------------------------------------------------------
        public PlacementResult Place
        (
            RectTransform safeAreaRoot,
            Vector2 anchor,
            Vector2 elementSize,
            PlacementOptions options
        )
        {
            if (safeAreaRoot == null)
            {
                throw new ArgumentNullException(nameof(safeAreaRoot));
            }

            return Place(safeAreaRoot.rect, anchor, elementSize, options);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 지정 로컬 Rect 안에서 UI 요소 배치 위치를 계산한다.
        /// </summary>
        // ------------------------------------------------------------
        public PlacementResult Place
        (
            Rect bounds,
            Vector2 anchor,
            Vector2 elementSize,
            PlacementOptions options
        )
        {
            var half = elementSize * 0.5f;
            var alignmentOffset = GetAlignmentOffset(options.Alignment, half);
            var position = anchor + alignmentOffset + options.Offset;
            var original = position;

            if (options.ClampToBounds)
            {
                var min = bounds.min + options.Padding + half;
                var max = bounds.max - options.Padding - half;
                position.x = min.x <= max.x
                    ? Mathf.Clamp(position.x, min.x, max.x)
                    : bounds.center.x;
                position.y = min.y <= max.y
                    ? Mathf.Clamp(position.y, min.y, max.y)
                    : bounds.center.y;
            }

            return new PlacementResult(position, position != original);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 정렬 방향을 요소 반크기 기준 offset으로 변환한다.
        /// </summary>
        // ------------------------------------------------------------
        private Vector2 GetAlignmentOffset
        (
            PlacementAlignment alignment,
            Vector2 half
        )
        {
            switch (alignment)
            {
                case PlacementAlignment.Top:
                    return new Vector2(0.0f, half.y);
                case PlacementAlignment.Bottom:
                    return new Vector2(0.0f, -half.y);
                case PlacementAlignment.Left:
                    return new Vector2(-half.x, 0.0f);
                case PlacementAlignment.Right:
                    return new Vector2(half.x, 0.0f);
                case PlacementAlignment.TopLeft:
                    return new Vector2(-half.x, half.y);
                case PlacementAlignment.TopRight:
                    return new Vector2(half.x, half.y);
                case PlacementAlignment.BottomLeft:
                    return new Vector2(-half.x, -half.y);
                case PlacementAlignment.BottomRight:
                    return new Vector2(half.x, -half.y);
                default:
                    return Vector2.zero;
            }
        }

    #endregion

    }
}
