/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : PopupPlacementSolver.cs
수정일 : 2026-10-27

# 설명
기준 Rect(또는 점) 주변에 Popup을 놓을 위치를 계산하는 기본 Solver 구현.
선호·대체 방향 평가, 반대 방향 Flip, 교차축 정렬 반전과 Shift, 크기 축소와 겹침 정책을 적용한다.
겹침을 감수할 때는 후보 방향 중 기준 영역과 가장 적게 겹치는 방향을 고른다.

# 특이사항, 제약사항
모든 Rect는 같은 Y-down 좌표계(yMin이 위쪽)여야 한다.
EditorWindow, VisualElement, RectTransform을 참조하지 않으며 좌표 변환은 호출자가 책임진다.
성공한 결과의 Bounds는 항상 Padding을 뺀 안전 영역 안에 포함된다.
========================================================================= BLOCK_HEADER_END */

using System;

using UnityEngine;

namespace inonego.Xeri.UI
{
    // ============================================================
    /// <summary>
    /// 기준 영역과 안전 영역에서 Popup의 방향과 최종 Rect를 계산한다.
    /// </summary>
    // ============================================================
    public sealed class PopupPlacementSolver : IPopupPlacementSolver
    {

    #region 상수 및 기본 인스턴스

        // ------------------------------------------------------------
        /// <summary>
        /// 크기 적합 여부와 보정 여부를 판정할 때 허용하는 부동소수점 오차.
        /// </summary>
        // ------------------------------------------------------------
        public const float Tolerance = 0.01f;

        // ------------------------------------------------------------
        /// <summary>
        /// 상태가 없는 기본 Popup 배치 Solver의 공유 인스턴스.
        /// </summary>
        // ------------------------------------------------------------
        public static PopupPlacementSolver Default { get; } = new();

    #endregion

    #region 생성

        private PopupPlacementSolver()
        {
            // NONE
        }

    #endregion

    #region 배치 계산

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> 안전 영역 안에서 기준 영역 주변의 Popup 배치를 계산한다.
        /// <br/> 선호·대체·반대 방향 순으로 전체 크기가 들어가는 첫 방향을 고른다.
        /// <br/> 모두 부족하면 가장 넓은 방향으로 크기를 줄인다.
        /// <br/> 그것도 불가능하면 정책에 따라 가장 덜 겹치는 방향으로 겹치거나 실패한다.
        /// </summary>
        // ----------------------------------------------------------------------
        public PopupPlacementResult Solve
        (
            Rect safeRect,
            Rect anchor,
            Vector2 popupSize,
            PopupPlacementOptions options
        )
        {
            if (!IsValidInput(safeRect, anchor, popupSize, options))
            {
                return new PopupPlacementResult(PopupPlacementFailure.InvalidInput);
            }

            var padding = Mathf.Max(0f, options.Padding);
            var gap = Mathf.Max(0f, options.Gap);
            var safe = Rect.MinMaxRect
            (
                safeRect.xMin + padding, safeRect.yMin + padding,
                safeRect.xMax - padding, safeRect.yMax - padding
            );

            if (safe.width <= 0f || safe.height <= 0f)
            {
                return new PopupPlacementResult(PopupPlacementFailure.InvalidInput);
            }

            Span<PopupPlacementDirection> order = stackalloc PopupPlacementDirection[3];
            var count = BuildDirectionOrder(options, order);
            var bestIndex = 0;
            var bestSpace = -1f;

            // 전체 크기가 들어가는 첫 방향을 우선하고, 없으면 공간이 가장 큰 방향을 기억한다.
            for (var index = 0; index < count; index++)
            {
                var space = GetMainSpace(safe, anchor, order[index], gap);

                if (GetMainSize(popupSize, order[index]) <= space + Tolerance)
                {
                    return Build(safe, anchor, popupSize, popupSize, order[index], options, gap);
                }

                if (space > bestSpace)
                {
                    bestSpace = space;
                    bestIndex = index;
                }
            }

            // 기준 영역을 피하는 방향 중 가장 넓은 공간으로 열림 방향 길이를 줄인다.
            if (options.AllowMainShrink && bestSpace >= Mathf.Max(options.MinMainSize, Tolerance))
            {
                var shrunk = SetMainSize(popupSize, order[bestIndex], bestSpace);
                return Build(safe, anchor, popupSize, shrunk, order[bestIndex], options, gap);
            }

            // 피할 공간이 없으면 정책이 허용할 때만 가장 덜 겹치는 방향으로 겹쳐 놓는다.
            if (options.AllowAnchorOverlap)
            {
                return BuildLeastOverlapping(safe, anchor, popupSize, order, count, options, gap);
            }

            return new PopupPlacementResult(PopupPlacementFailure.NoSpace);
        }

        // --------------------------------------------------------------------------------
        /// <summary>
        /// <br/> 각 방향 후보를 안전 영역 안으로 당긴 뒤 기준 영역과 가장 적게 겹치는 후보를 고른다.
        /// <br/> 겹침이 같으면 앞선 방향을 유지한다.
        /// <br/> 열림 방향 축소가 불가능하면 안전 영역보다 긴 방향은 후보에서 제외한다.
        /// <br/> 남는 후보가 없으면 실패한다.
        /// </summary>
        // --------------------------------------------------------------------------------
        private static PopupPlacementResult BuildLeastOverlapping
        (
            Rect safe,
            Rect anchor,
            Vector2 popupSize,
            ReadOnlySpan<PopupPlacementDirection> order,
            int count,
            PopupPlacementOptions options,
            float gap
        )
        {
            var best = new PopupPlacementResult(PopupPlacementFailure.NoSpace);
            var bestArea = float.MaxValue;

            for (var index = 0; index < count; index++)
            {
                if (!options.AllowMainShrink
                    && GetMainSize(popupSize, order[index]) > GetMainExtent(safe, order[index]) + Tolerance)
                {
                    continue;
                }

                var candidate = Build(safe, anchor, popupSize, popupSize, order[index], options, gap);
                var overlap = GetOverlap(candidate.Bounds, anchor);
                var area = overlap.x * overlap.y;

                if (area < bestArea - Tolerance)
                {
                    best = candidate;
                    bestArea = area;
                }
            }

            return best;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 확정된 방향과 크기로 최종 Rect를 만들고 안전 영역 안으로 보정한다.
        /// </summary>
        // ------------------------------------------------------------
        private static PopupPlacementResult Build
        (
            Rect safe,
            Rect anchor,
            Vector2 requestedSize,
            Vector2 size,
            PopupPlacementDirection direction,
            PopupPlacementOptions options,
            float gap
        )
        {
            // 교차축은 항상 안전 영역 크기로 제한한다. 열림 방향은 축소가 허용된 경우에만 더 길 수 있다.
            size = Vector2.Min(size, safe.size);

            var isVertical = IsVertical(direction);
            var mainPosition = direction switch
            {
                PopupPlacementDirection.Below => anchor.yMax + gap,
                PopupPlacementDirection.Above => anchor.yMin - gap - size.y,
                PopupPlacementDirection.Right => anchor.xMax + gap,
                _ => anchor.xMin - gap - size.x,
            };
            var alignment = options.Alignment;
            var crossStart = GetCrossStart(anchor, size, isVertical, alignment, options.CrossOffset);
            var alignmentFlipped = false;

            // 정렬한 교차축이 안전 영역을 넘으면 Shift 전에 반대 정렬이 안에 들어가는지 먼저 본다.
            if (options.AllowAlignmentFlip && alignment != PopupPlacementAlignment.Center
                && !IsCrossInside(crossStart, size, safe, isVertical))
            {
                var mirrored = alignment == PopupPlacementAlignment.Start
                    ? PopupPlacementAlignment.End : PopupPlacementAlignment.Start;
                var mirroredStart = GetCrossStart
                (
                    anchor, size, isVertical, mirrored, options.CrossOffset
                );

                if (IsCrossInside(mirroredStart, size, safe, isVertical))
                {
                    crossStart = mirroredStart;
                    alignmentFlipped = true;
                }
            }

            var natural = isVertical
                ? new Vector2(crossStart, mainPosition)
                : new Vector2(mainPosition, crossStart);

            // 교차축은 안전 영역으로 밀어 넣고, 열림 방향은 부동소수점 오차만 정리한다.
            var position = new Vector2
            (
                FitStart(natural.x, safe.xMin, safe.xMax, size.x),
                FitStart(natural.y, safe.yMin, safe.yMax, size.y)
            );
            var crossShift = isVertical ? position.x - natural.x : position.y - natural.y;
            var bounds = new Rect(position, size);

            return new PopupPlacementResult
            (
                bounds,
                direction,
                direction != options.PreferredDirection,
                alignmentFlipped,
                Mathf.Abs(crossShift) > Tolerance,
                Mathf.Abs(size.x - requestedSize.x) > Tolerance
                    || Mathf.Abs(size.y - requestedSize.y) > Tolerance,
                IsOverlapping(bounds, anchor)
            );
        }

    #endregion

    #region 방향 평가

        // ------------------------------------------------------------
        /// <summary>
        /// 선호·대체·반대 방향을 중복 없이 시도 순서대로 채우고 개수를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        private static int BuildDirectionOrder
        (
            PopupPlacementOptions options, Span<PopupPlacementDirection> order
        )
        {
            var count = 0;
            AppendDirection(order, ref count, options.PreferredDirection);

            if (options.FallbackDirection.HasValue)
            {
                AppendDirection(order, ref count, options.FallbackDirection.Value);
            }

            if (options.AllowFlip)
            {
                AppendDirection(order, ref count, GetOpposite(options.PreferredDirection));
            }

            return count;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 이미 시도 순서에 있는 방향이 아니면 뒤에 추가한다.
        /// </summary>
        // ------------------------------------------------------------
        private static void AppendDirection
        (
            Span<PopupPlacementDirection> order, ref int count, PopupPlacementDirection direction
        )
        {
            for (var index = 0; index < count; index++)
            {
                if (order[index] == direction)
                {
                    return;
                }
            }

            order[count++] = direction;
        }

        // --------------------------------------------------------------------------------
        /// <summary>
        /// 안전 영역 안에서 기준 영역과 간격을 제외하고 열림 방향으로 쓸 수 있는 길이를 구한다.
        /// </summary>
        // --------------------------------------------------------------------------------
        private static float GetMainSpace
        (
            Rect safe, Rect anchor, PopupPlacementDirection direction, float gap
        )
        {
            var space = direction switch
            {
                PopupPlacementDirection.Below => safe.yMax - anchor.yMax,
                PopupPlacementDirection.Above => anchor.yMin - safe.yMin,
                PopupPlacementDirection.Right => safe.xMax - anchor.xMax,
                _ => anchor.xMin - safe.xMin,
            };

            return Mathf.Max(0f, space - gap);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 반대 열림 방향을 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        private static PopupPlacementDirection GetOpposite(PopupPlacementDirection direction)
        {
            return direction switch
            {
                PopupPlacementDirection.Below => PopupPlacementDirection.Above,
                PopupPlacementDirection.Above => PopupPlacementDirection.Below,
                PopupPlacementDirection.Right => PopupPlacementDirection.Left,
                _ => PopupPlacementDirection.Right,
            };
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 열림 방향이 세로 축인지 확인한다.
        /// </summary>
        // ------------------------------------------------------------
        private static bool IsVertical(PopupPlacementDirection direction)
        {
            return direction == PopupPlacementDirection.Below
                || direction == PopupPlacementDirection.Above;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 열림 방향 축의 Popup 길이를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        private static float GetMainSize(Vector2 size, PopupPlacementDirection direction)
        {
            return IsVertical(direction) ? size.y : size.x;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 열림 방향 축에서 안전 영역이 가진 전체 길이를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        private static float GetMainExtent(Rect safe, PopupPlacementDirection direction)
        {
            return IsVertical(direction) ? safe.height : safe.width;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 열림 방향 축의 길이만 바꾼 크기를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        private static Vector2 SetMainSize
        (
            Vector2 size, PopupPlacementDirection direction, float mainSize
        )
        {
            return IsVertical(direction)
                ? new Vector2(size.x, mainSize)
                : new Vector2(mainSize, size.y);
        }

    #endregion

    #region 기하 유틸

        // ------------------------------------------------------------
        /// <summary>
        /// 교차축에서 기준 영역 구간에 맞춘 Popup 시작 좌표를 구한다.
        /// </summary>
        // ------------------------------------------------------------
        private static float AlignCross
        (
            float anchorMin, float anchorMax, float size, PopupPlacementAlignment alignment
        )
        {
            return alignment switch
            {
                PopupPlacementAlignment.Center => (anchorMin + anchorMax) * 0.5f - size * 0.5f,
                PopupPlacementAlignment.End => anchorMax - size,
                _ => anchorMin,
            };
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// 열림 방향의 교차축에서 정렬과 오프셋을 반영한 Popup 시작 좌표를 구한다.
        /// </summary>
        // ----------------------------------------------------------------------
        private static float GetCrossStart
        (
            Rect anchor,
            Vector2 size,
            bool isVertical,
            PopupPlacementAlignment alignment,
            float crossOffset
        )
        {
            return isVertical
                ? AlignCross(anchor.xMin, anchor.xMax, size.x, alignment) + crossOffset
                : AlignCross(anchor.yMin, anchor.yMax, size.y, alignment) + crossOffset;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 교차축 시작 좌표의 Popup 구간이 안전 영역 안에 들어가는지 확인한다.
        /// </summary>
        // ------------------------------------------------------------
        private static bool IsCrossInside
        (
            float crossStart, Vector2 size, Rect safe, bool isVertical
        )
        {
            var min = isVertical ? safe.xMin : safe.yMin;
            var max = isVertical ? safe.xMax : safe.yMax;
            var length = isVertical ? size.x : size.y;

            return crossStart >= min - Tolerance && crossStart + length <= max + Tolerance;
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// 구간 안에 들어오도록 시작 좌표를 제한하며 구간이 모자라면 최솟값을 우선한다.
        /// </summary>
        // ----------------------------------------------------------------------
        private static float FitStart(float start, float min, float max, float size)
        {
            return Mathf.Max(min, Mathf.Min(start, max - size));
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 두 Rect가 오차를 넘는 면적으로 겹치는지 확인한다.
        /// </summary>
        // ------------------------------------------------------------
        private static bool IsOverlapping(Rect bounds, Rect anchor)
        {
            var overlap = GetOverlap(bounds, anchor);

            return overlap.x > Tolerance && overlap.y > Tolerance;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 두 Rect가 각 축에서 겹치는 길이를 구한다. 겹치지 않는 축은 0이다.
        /// </summary>
        // ------------------------------------------------------------
        private static Vector2 GetOverlap(Rect bounds, Rect anchor)
        {
            return new Vector2
            (
                Mathf.Max(0f, Mathf.Min(bounds.xMax, anchor.xMax) - Mathf.Max(bounds.xMin, anchor.xMin)),
                Mathf.Max(0f, Mathf.Min(bounds.yMax, anchor.yMax) - Mathf.Max(bounds.yMin, anchor.yMin))
            );
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 계산에 쓰는 모든 입력이 유한하고 크기 계약을 만족하는지 확인한다.
        /// </summary>
        // ------------------------------------------------------------
        private static bool IsValidInput
        (
            Rect safeRect, Rect anchor, Vector2 popupSize, PopupPlacementOptions options
        )
        {
            return IsFinite(safeRect) && IsFinite(anchor)
                && float.IsFinite(popupSize.x) && float.IsFinite(popupSize.y)
                && float.IsFinite(options.Gap) && float.IsFinite(options.Padding)
                && float.IsFinite(options.MinMainSize) && float.IsFinite(options.CrossOffset)
                && anchor.width >= 0f && anchor.height >= 0f
                && popupSize.x > 0f && popupSize.y > 0f;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Rect의 위치와 크기가 모두 유한한 값인지 확인한다.
        /// </summary>
        // ------------------------------------------------------------
        private static bool IsFinite(Rect rect)
        {
            return float.IsFinite(rect.x) && float.IsFinite(rect.y)
                && float.IsFinite(rect.width) && float.IsFinite(rect.height);
        }

    #endregion

    }
}
