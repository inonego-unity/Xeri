/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TableColumnSizing.cs
수정일 : 2026-11-18

# 설명
UI Toolkit MultiColumnListView의 열 생성과 기준 폭 배분을 TableColumnLayout으로 공통 처리한다.
가변 열만 있는 표는 가중치를 퍼센트 폭으로 지정해 Unity가 첫 레이아웃부터 비율대로 배치하고 폭 변화도 따라가게 한다.
고정 폭 열이 섞이면 퍼센트가 헤더 전체 폭 기준이라 남은 폭의 비율을 표현할 수 없으므로,
헤더 폭이 바뀔 때마다 가변 열을 가중치 비율로 다시 맞춘다.
Unity는 열 폭을 바꾼 갱신에서 셀을 다시 배치하지 않고 다음 갱신에서 반영하므로(측정으로 확인),
이 경로의 표는 첫 폭을 맞출 때까지 숨겨 맞추기 전 폭이 한 프레임 그려지지 않게 한다.
Unity의 GrowAndFill은 px 폭의 가변 열을 균등 배분하고 이후 열 폭 변경을 화면에 반영하지 않으므로 Grow 모드를 유지한다.
수동 리사이즈는 Unity에 위임한다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UIElements;

namespace inonego.Xeri.UI
{
    // ============================================================
    /// <summary>
    /// 열의 기준 폭을 가중치로 구성하고 Unity의 자동 확장 정책을 연결한다.
    /// </summary>
    // ============================================================
    internal static class TableColumnSizing
    {

    #region 메서드

        // ------------------------------------------------------------
        /// <summary>
        /// 레이아웃의 기본·최소·최대 폭과 가변 여부를 적용한 열을 만든다.
        /// 셀 생성·바인딩과 정렬 같은 표시 동작은 호출한 쪽이 이어서 지정한다.
        /// </summary>
        // ------------------------------------------------------------
        public static Column CreateColumn(string id, string title, TableColumnLayout layout)
        {
            if (layout == null)
            {
                throw new ArgumentNullException(nameof(layout));
            }

            var column = new Column
            {
                name = id,
                title = title ?? string.Empty,
                width = layout.Width,
                minWidth = layout.MinWidth,
                stretchable = layout.Mode == TableColumnLayoutMode.Flexible,
            };

            // 최대 폭 0은 제한 없음이다.
            if (layout.MaxWidth > 0f)
            {
                column.maxWidth = layout.MaxWidth;
            }

            return column;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> 열 순서와 같은 레이아웃 목록의 가중치로 가변 열의 폭을 배분하고 헤더 폭에 맞춘다.
        /// <br/> 가변 열만 있으면 퍼센트 폭으로, 고정 열이 섞이면 헤더 폭이 바뀔 때마다 다시 맞추는 방식으로 처리한다.
        /// </summary>
        // ------------------------------------------------------------
        public static void ApplyColumnSizing(this MultiColumnListView view, IReadOnlyList<TableColumnLayout> layouts)
        {
            if (view == null)
            {
                throw new ArgumentNullException(nameof(view));
            }

            if (view.panel != null)
            {
                throw new InvalidOperationException("컬럼 너비 정책은 테이블을 패널에 연결하기 전에 구성해야 합니다.");
            }

            if (layouts == null || layouts.Count != view.columns.Count)
            {
                throw new ArgumentException("열 개수와 레이아웃 개수가 일치해야 합니다.", nameof(layouts));
            }

            var weights = new float[layouts.Count];
            for (var index = 0; index < layouts.Count; index++)
            {
                weights[index] = layouts[index].StretchWeight;
            }

            var columns = view.columns;

            // 가변 열만 있으면 퍼센트 폭으로 첫 레이아웃부터 가중치 비율을 적용한다.
            if (!HasFixedColumn(layouts))
            {
                ApplyPercentWidths(columns, weights);
                return;
            }

            var widths = new float[columns.Count];
            var active = new bool[columns.Count];
            var totalWidth = 0f;
            var totalWeight = 0f;

            for (var index = 0; index < columns.Count; index++)
            {
                var column = columns[index];
                var weight = weights[index];

                if (!column.stretchable)
                {
                    if (weight != 0f)
                    {
                        throw new ArgumentException("고정 열의 가중치는 0이어야 합니다.", nameof(layouts));
                    }

                    continue;
                }

                if (weight <= 0f || float.IsNaN(weight) || float.IsInfinity(weight))
                {
                    throw new ArgumentException("가변 열에는 양의 유한한 가중치가 필요합니다.", nameof(layouts));
                }

                totalWidth += Math.Max(column.width.value, column.minWidth.value);
                totalWeight += weight;
                active[index] = true;
            }

            if (totalWeight <= 0f) return;

            // 첫 레이아웃에서 Unity가 열별 기준 폭을 먼저 확정하도록 한다.
            columns.stretchMode = Columns.StretchMode.Grow;

            // 최소·최대 폭을 지키면서 최초 레이아웃에 사용할 가중 기준 폭을 계산한다.
            var remainingWidth = totalWidth;
            var remainingWeight = totalWeight;
            var clamped = true;

            while (clamped && remainingWeight > 0f)
            {
                clamped = false;
                for (var index = 0; index < columns.Count; index++)
                {
                    if (!active[index]) continue;

                    var column = columns[index];
                    var share = remainingWidth * weights[index] / remainingWeight;
                    var min = column.minWidth.value;
                    var max = column.maxWidth.value;
                    var limit = share < min ? min : max > 0f && share > max ? max : share;
                    if (limit == share) continue;

                    widths[index] = limit;
                    remainingWidth -= limit;
                    remainingWeight -= weights[index];
                    active[index] = false;
                    clamped = true;
                }
            }

            for (var index = 0; index < columns.Count; index++)
            {
                var column = columns[index];
                if (!column.stretchable) continue;

                // 최소·최대 폭에 걸리지 않은 열만 남은 공간을 공유한다.
                if (active[index])
                {
                    widths[index] = remainingWeight > 0f
                        ? remainingWidth * weights[index] / remainingWeight
                        : column.minWidth.value;
                }

                column.width = Math.Max(column.minWidth.value, widths[index]);
            }

            // 헤더가 레이아웃될 때마다 가변 열을 헤더 폭에 맞춘다.
            // 첫 폭 맞춤은 다음 갱신에서 반영되므로 그때까지 표를 숨겨 기준 폭이 그려지지 않게 한다.
            view.style.visibility = Visibility.Hidden;
            view.RegisterCallback<AttachToPanelEvent>(HandleAttach);

            void HandleAttach(AttachToPanelEvent evt)
            {
                var header = view.Q<VisualElement>(className: "unity-multi-column-header");
                if (header == null)
                {
                    throw new InvalidOperationException("MultiColumnListView 헤더를 찾을 수 없습니다.");
                }

                view.UnregisterCallback<AttachToPanelEvent>(HandleAttach);
                var revealed = false;
                // 헤더 자체의 GeometryChangedEvent는 내부 ColumnLayout.DoLayout 뒤에 호출된다.
                // 분할 뷰나 스크롤바처럼 첫 레이아웃 뒤에 헤더 폭이 바뀌어도 Unity는 열을 다시 맞추지 않는다.
                header.RegisterCallback<GeometryChangedEvent>(HandleHeaderLayout);

                void HandleHeaderLayout(GeometryChangedEvent geometry)
                {
                    if (geometry.target != header || geometry.newRect.width <= 0f) return;

                    FitStretchWidth(columns, geometry.newRect.width);
                    if (revealed) return;

                    // 변경한 열 폭이 레이아웃에 반영되는 다음 갱신에서 표시한다.
                    revealed = true;
                    header.schedule.Execute(() => view.style.visibility = StyleKeyword.Null);
                }
            }
        }

        private static bool HasFixedColumn(IReadOnlyList<TableColumnLayout> layouts)
        {
            foreach (var layout in layouts)
            {
                if (layout.Mode == TableColumnLayoutMode.Fixed)
                {
                    return true;
                }
            }

            return false;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 가중치 합이 100%가 되도록 각 열의 폭을 퍼센트로 지정한다.
        /// 퍼센트는 헤더 폭에 대해 계산되어 첫 레이아웃과 이후 폭 변화에 Unity가 직접 맞춘다.
        /// </summary>
        // ------------------------------------------------------------
        private static void ApplyPercentWidths(Columns columns, float[] weights)
        {
            var total = 0f;
            foreach (var weight in weights)
            {
                total += weight;
            }

            columns.stretchMode = Columns.StretchMode.Grow;
            for (var index = 0; index < columns.Count; index++)
            {
                columns[index].width = Length.Percent(100f * weights[index] / total);
            }
        }

        // --------------------------------------------------------------------------------
        /// <summary>
        /// <br/> 가변 열 합계가 헤더 폭과 다르면 비율을 유지해 헤더 폭에 맞춘다. 최소 폭 아래로는 줄이지 않는다.
        /// <br/> 첫 레이아웃 이후 헤더 폭이 바뀌어도 Unity가 열을 다시 배분하지 않는 경우가 있기 때문이다.
        /// </summary>
        // --------------------------------------------------------------------------------
        private static void FitStretchWidth(Columns columns, float headerWidth)
        {
            var fixedWidth = 0f;
            var stretchWidth = 0f;
            foreach (var column in columns)
            {
                if (!column.visible) continue;

                if (column.stretchable)
                {
                    stretchWidth += column.width.value;
                }
                else
                {
                    fixedWidth += column.width.value;
                }
            }

            var available = headerWidth - fixedWidth;
            if (stretchWidth <= 0f || available <= 0f || Math.Abs(available - stretchWidth) <= 1f) return;

            var scale = available / stretchWidth;
            foreach (var column in columns)
            {
                if (column.visible && column.stretchable)
                {
                    column.width = Math.Max(column.minWidth.value, column.width.value * scale);
                }
            }
        }

    #endregion

    }
}
