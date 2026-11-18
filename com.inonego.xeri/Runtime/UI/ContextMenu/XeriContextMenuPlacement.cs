/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : XeriContextMenuPlacement.cs
수정일 : 2026-11-18

# 설명
컨텍스트 메뉴 단계의 화면 경계와 하위 메뉴 방향을 공통 Popup 배치 계산기로 정한다.

# 특이사항, 제약사항
이 파일은 선호 방향·간격·여백 정책만 정하며 실제 배치 계산은 PopupPlacementSolver가 한다.
모든 좌표는 호스트가 변환한 Y-down 팝업 좌표계이고 배치 영역은 호스트가 제공하는 작업 영역이다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UIElements;

using inonego;
using inonego.Xeri;

namespace inonego.Xeri.UI
{
    // ============================================================
    /// <summary>
    /// 컨텍스트 메뉴 단계 배치 정책.
    /// </summary>
    // ============================================================
    internal static class XeriContextMenuPlacement
    {

    #region 상수

        // 화면 가장자리 여백과 서브메뉴 간격은 밀도와 무관한 배치 정책이다.
        private const float menuMargin = 4f;
        private const float submenuGap = 1f;

    #endregion

    #region 배치

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> 공통 Popup 배치 계산기로 메뉴 단계의 화면 경계와
        /// <br/> 하위 메뉴 방향을 정하고 단계 요소에 적용한다.
        /// </summary>
        // ------------------------------------------------------------
        internal static void Place
        (
            PopupHost host, XeriContextMenuMetrics metrics,
            XeriContextMenuLevel level, XeriContextMenuLevel parent,
            float contentHeight, Rect anchorBounds, float crossOffset
        )
        {
            var isRoot = level.Depth == 0;
            var options = isRoot
                ? CreateRootOptions(metrics)
                : CreateSubmenuOptions(parent.Direction, crossOffset);
            var result = host.Solve
            (
                anchorBounds, new Vector2(metrics.Width, contentHeight), options
            );

            // 루트는 좌우의 남은 공간으로, 하위 메뉴는 실제 선택된 방향으로 다음 단계의 선호 방향을 정한다.
            var bounds = result.Bounds;
            level.Direction = isRoot
                ? GetRootChildDirection(host.WorkArea, bounds, metrics) : result.Direction;

            // 높이를 줄였을 때만 스크롤을 허용한다. 줄이지 않았다면 내용이 들어가므로 배율에 따른
            // 픽셀 반올림 오차로 스크롤바가 생기지 않게 숨긴다.
            level.Scroll.verticalScrollerVisibility = result.WasResized
                ? ScrollerVisibility.Auto : ScrollerVisibility.Hidden;
            level.Element.style.height = bounds.height;
            level.Bounds = bounds;
            level.Element.style.left = 0f;
            level.Element.style.top = 0f;
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> 루트 메뉴는 기준 영역 아래를 우선하고 부족하면 위로 열며
        /// <br/> 높이는 마지막에만 줄인다.
        /// <br/> 왼쪽 정렬이 화면을 넘으면 오른쪽 모서리를 기준 영역에 맞춰 연다.
        /// </summary>
        // ----------------------------------------------------------------------
        private static PopupPlacementOptions CreateRootOptions(XeriContextMenuMetrics metrics)
        {
            return new PopupPlacementOptions
            (
                PopupPlacementDirection.Below,
                padding: menuMargin,
                minMainSize: metrics.ActionHeight + metrics.ChromeHeight,
                allowAnchorOverlap: true,
                allowAlignmentFlip: true
            );
        }

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> 하위 메뉴는 부모가 열린 쪽을 우선하고
        /// <br/> 항목 높이에 맞춰 부모 옆에 붙인다.
        /// </summary>
        // ------------------------------------------------------------
        private static PopupPlacementOptions CreateSubmenuOptions
        (
            PopupPlacementDirection parentDirection, float crossOffset
        )
        {
            // 메뉴 폭은 고정이므로 폭을 줄이지 않고, 양쪽이 모자라면 부모와 덜 겹치는 쪽에 놓는다.
            return new PopupPlacementOptions
            (
                parentDirection,
                gap: submenuGap,
                padding: menuMargin,
                allowMainShrink: false,
                allowAnchorOverlap: true,
                crossOffset: crossOffset
            );
        }

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> 루트 메뉴 좌우의 남은 공간을 비교해
        /// <br/> 하위 메뉴가 우선 열릴 방향을 구한다.
        /// </summary>
        // ------------------------------------------------------------
        private static PopupPlacementDirection GetRootChildDirection
        (
            Rect area, Rect rootBounds, XeriContextMenuMetrics metrics
        )
        {
            var rightSpace = area.xMax - rootBounds.xMax;
            var leftSpace = rootBounds.xMin - area.xMin;

            return rightSpace >= metrics.Width || rightSpace >= leftSpace
                ? PopupPlacementDirection.Right : PopupPlacementDirection.Left;
        }

    #endregion

    }
}
