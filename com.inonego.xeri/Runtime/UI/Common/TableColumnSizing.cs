/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TableColumnSizing.cs
수정일 : 2026-10-31

# 설명
UI Toolkit MultiColumnListView의 열 기준 폭을 가중치로 배분한다.
첫 레이아웃 이후 GrowAndFill로 전환하여 컨테이너 변화와 수동 리사이즈를 Unity에 위임한다.
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
        /// <br/> 고정 열은 0, 가변 열은 양수인 순서별 가중치를 적용한다.
        /// <br/> 첫 헤더 레이아웃 이후의 공간 배분은 Unity가 처리한다.
        /// </summary>
        // ------------------------------------------------------------
        public static void ApplyColumnSizing(this MultiColumnListView view, IReadOnlyList<float> weights)
        {
            if (view == null)
            {
                throw new ArgumentNullException(nameof(view));
            }

            if (view.panel != null)
            {
                throw new InvalidOperationException("컬럼 너비 정책은 테이블을 패널에 연결하기 전에 구성해야 합니다.");
            }

            if (weights == null || weights.Count != view.columns.Count)
            {
                throw new ArgumentException("열 개수와 가중치 개수가 일치해야 합니다.", nameof(weights));
            }

            var columns = view.columns;
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
                        throw new ArgumentException("고정 열의 가중치는 0이어야 합니다.", nameof(weights));
                    }

                    continue;
                }

                if (weight <= 0f || float.IsNaN(weight) || float.IsInfinity(weight))
                {
                    throw new ArgumentException("가변 열에는 양의 유한한 가중치가 필요합니다.", nameof(weights));
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

            // Unity의 GrowAndFill은 첫 레이아웃에서 가변 열을 균등 배분한다.
            // 헤더가 기준 폭으로 첫 계산을 마친 시점에만 자동 채우기로 전환한다.
            view.RegisterCallback<AttachToPanelEvent>(HandleAttach);

            void HandleAttach(AttachToPanelEvent evt)
            {
                var header = view.Q<VisualElement>(className: "unity-multi-column-header");
                if (header == null)
                {
                    throw new InvalidOperationException("MultiColumnListView 헤더를 찾을 수 없습니다.");
                }

                view.UnregisterCallback<AttachToPanelEvent>(HandleAttach);
                // 헤더 자체의 GeometryChangedEvent는 내부 ColumnLayout.DoLayout 뒤에 호출된다.
                header.RegisterCallback<GeometryChangedEvent>(HandleHeaderLayout);

                void HandleHeaderLayout(GeometryChangedEvent geometry)
                {
                    if (geometry.target != header || geometry.newRect.width <= 0f) return;

                    header.UnregisterCallback<GeometryChangedEvent>(HandleHeaderLayout);
                    columns.stretchMode = Columns.StretchMode.GrowAndFill;
                }
            }
        }

    #endregion

    }
}
