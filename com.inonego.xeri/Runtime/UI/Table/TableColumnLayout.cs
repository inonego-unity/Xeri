/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TableColumnLayout.cs
수정일 : 2026-11-19

# 설명
Table column의 폭과 stretch 정책을 표현하는 layout 모델.
Picker와 Collection Editor의 표가 열 선언과 폭 배분에 공통으로 사용한다.
Distribute는 UI 요소 없이 주어진 폭을 열마다 나누는 순수 계산이라 자체 표 구현도 쓸 수 있다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

namespace inonego.Xeri.UI
{
    // ============================================================
    /// <summary>
    /// Table column의 layout mode.
    /// </summary>
    // ============================================================
    public enum TableColumnLayoutMode
    {
        Fixed,
        Flexible,
    }

    // ============================================================
    /// <summary>
    /// Table column의 폭과 stretch 정책.
    /// </summary>
    // ============================================================
    [Serializable]
    public sealed class TableColumnLayout
    {

    #region 필드

        // ------------------------------------------------------------
        /// <summary>
        /// 고정 또는 가변 폭 방식.
        /// </summary>
        // ------------------------------------------------------------
        public readonly TableColumnLayoutMode Mode;

        // ------------------------------------------------------------
        /// <summary>
        /// 기본 폭. 가변 열은 최초 배분의 기준 폭으로 쓴다.
        /// </summary>
        // ------------------------------------------------------------
        public readonly float Width;

        // ------------------------------------------------------------
        /// <summary>
        /// 최소 폭.
        /// </summary>
        // ------------------------------------------------------------
        public readonly float MinWidth;

        // ------------------------------------------------------------
        /// <summary>
        /// 최대 폭. 0은 제한 없음이다.
        /// </summary>
        // ------------------------------------------------------------
        public readonly float MaxWidth;

        // ------------------------------------------------------------
        /// <summary>
        /// 가변 열이 남은 폭을 나누는 가중치. 고정 열은 0이다.
        /// </summary>
        // ------------------------------------------------------------
        public readonly float StretchWeight;

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// column layout 정책을 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        public TableColumnLayout
        (
            TableColumnLayoutMode mode,
            float width,
            float minWidth,
            float maxWidth,
            float stretchWeight
        )
        {
            if (mode == TableColumnLayoutMode.Flexible
                && (stretchWeight <= 0f || float.IsNaN(stretchWeight) || float.IsInfinity(stretchWeight)))
            {
                throw new ArgumentOutOfRangeException(nameof(stretchWeight), "가변 열 가중치는 양의 유한한 값이어야 합니다.");
            }

            Mode          = mode;
            Width         = Math.Max(1f, width);
            MinWidth      = Math.Max(1f, minWidth);
            MaxWidth      = Math.Max(0f, maxWidth);
            StretchWeight = mode == TableColumnLayoutMode.Flexible ? stretchWeight : 0f;
        }

    #endregion

    #region 생성

        // ------------------------------------------------------------
        /// <summary>
        /// 고정 폭 column layout을 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        public static TableColumnLayout Fixed(float width)
        {
            var safeWidth = Math.Max(1f, width);

            return new TableColumnLayout
            (
                TableColumnLayoutMode.Fixed,
                safeWidth,
                safeWidth,
                safeWidth,
                0f
            );
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 가변 폭 column layout을 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        public static TableColumnLayout Flexible
        (
            float width,
            float minWidth,
            float maxWidth,
            float stretchWeight
        )
        {
            return new TableColumnLayout
            (
                TableColumnLayoutMode.Flexible,
                width,
                minWidth,
                maxWidth,
                stretchWeight
            );
        }

    #endregion

    #region 폭 배분

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> 전체 폭을 열마다 나눠 widths에 쓴다. 고정 열은 자기 폭을 갖는다.
        /// <br/> 가변 열은 남은 폭을 가중치 비율로 나눈다.
        /// <br/> 최소·최대 폭에 걸린 열은 그 폭으로 두고 나머지 열이 다시 나눈다.
        /// <br/> 폭이 모자라면 가변 열은 최소 폭이 되어 합계가 전체 폭을 넘을 수 있다.
        /// </summary>
        // ----------------------------------------------------------------------
        public static void Distribute(IReadOnlyList<TableColumnLayout> layouts, float availableWidth, float[] widths)
        {
            if (layouts == null)
            {
                throw new ArgumentNullException(nameof(layouts));
            }

            if (widths == null || widths.Length != layouts.Count)
            {
                throw new ArgumentException("폭 배열 길이는 열 개수와 같아야 합니다.", nameof(widths));
            }

            if (float.IsNaN(availableWidth) || float.IsInfinity(availableWidth))
            {
                throw new ArgumentOutOfRangeException(nameof(availableWidth), "전체 폭은 유한한 값이어야 합니다.");
            }

            var active = new bool[layouts.Count];
            var remainingWidth = availableWidth;
            var remainingWeight = 0f;

            for (var index = 0; index < layouts.Count; index++)
            {
                var layout = layouts[index] ?? throw new ArgumentException("열 레이아웃은 null일 수 없습니다.", nameof(layouts));
                if (layout.Mode == TableColumnLayoutMode.Fixed)
                {
                    widths[index] = layout.Width;
                    remainingWidth -= layout.Width;
                    continue;
                }

                remainingWeight += layout.StretchWeight;
                active[index] = true;
            }

            remainingWidth = Math.Max(0f, remainingWidth);

            // 최소·최대 폭에 걸리는 열을 하나씩 고정하고, 남은 열이 남은 폭을 다시 나눈다.
            var clamped = true;
            while (clamped && remainingWeight > 0f)
            {
                clamped = false;
                for (var index = 0; index < layouts.Count; index++)
                {
                    if (!active[index]) continue;

                    var layout = layouts[index];
                    var share = remainingWidth * layout.StretchWeight / remainingWeight;
                    var limit = share < layout.MinWidth
                        ? layout.MinWidth
                        : layout.MaxWidth > 0f && share > layout.MaxWidth ? layout.MaxWidth : share;
                    if (limit == share) continue;

                    widths[index] = limit;
                    remainingWidth -= limit;
                    remainingWeight -= layout.StretchWeight;
                    active[index] = false;
                    clamped = true;
                }
            }

            for (var index = 0; index < layouts.Count; index++)
            {
                if (!active[index]) continue;

                var layout = layouts[index];
                var share = remainingWeight > 0f
                    ? remainingWidth * layout.StretchWeight / remainingWeight
                    : layout.MinWidth;
                widths[index] = Math.Max(layout.MinWidth, share);
            }
        }

    #endregion

    }
}
