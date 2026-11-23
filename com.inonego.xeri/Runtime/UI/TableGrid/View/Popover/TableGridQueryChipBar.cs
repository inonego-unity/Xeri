/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TableGridQueryChipBar.cs
수정일 : 2026-11-23

# 설명
표의 쿼리 표시 상태(정렬·필터·그룹)를 둥근 칩으로 보여 준다. 호스트가 원하는 곳(검색 줄 옆 등)에 놓는다.
칩을 누르면 같은 열 메뉴가 그 구역만 보이게 열리고(필터는 필터 편집), ×를 누르면 그 조건을 푸는 요청을 보낸다.

# 특이사항, 제약사항
칩은 표의 QueryState만 따르고, 바꾸는 일은 표의 QueryRequested 요청으로만 한다.
표 밖에 놓이므로 테마·모양 USS를 직접 붙이고 테마 변수 클래스(xeri-table-chips)를 가진다.
테마 수정 클래스는 자기 패널과 따르는 표의 옵션으로 정한다(표가 없으면 기본 옵션).
새 필터는 열 헤더에서만 추가한다(칩 줄에는 추가 버튼이 없다).
표의 QueryStateChanged는 패널에 붙어 있는 동안만 구독한다(떼어 낸 칩 줄을 표가 붙잡지 않게). 다시 붙으면 칩을 새로 만든다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UIElements;

using inonego;
using inonego.Xeri;
using inonego.Xeri.UI;

namespace inonego.Xeri.UI.TableGrid
{
    // ============================================================
    /// <summary>
    /// 쿼리 칩 줄.
    /// </summary>
    // ============================================================
    [UxmlElement]
    public partial class TableGridQueryChipBar : VisualElement
    {

    #region 필드

        // ------------------------------------------------------------
        /// <summary>
        /// 칩이 따르는 표. null이면 칩을 비운다.
        /// </summary>
        // ------------------------------------------------------------
        public TableGrid Grid
        {
            get => grid;
            set
            {
                if (grid == value)
                {
                    return;
                }

                Unsubscribe();
                grid = value;
                if (panel != null)
                {
                    Subscribe();
                }

                TableGridTheming.Apply(this, grid?.Options);
                Rebuild();
            }
        }

        private TableGrid grid;

    #endregion

    #region 생성

        // ------------------------------------------------------------
        /// <summary>
        /// 빈 칩 줄을 만든다. Grid를 지정하면 칩이 채워진다.
        /// </summary>
        // ------------------------------------------------------------
        public TableGridQueryChipBar()
        {
            AddToClassList("xeri-table-chips");
            styleSheets.Add(TableGridResources.Theme);
            styleSheets.Add(TableGridResources.Style);

            RegisterCallback<AttachToPanelEvent>(HandleAttachToPanel);
            RegisterCallback<DetachFromPanelEvent>(_ => Unsubscribe());
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 표를 지정해 칩 줄을 만든다.
        /// </summary>
        // ------------------------------------------------------------
        public TableGridQueryChipBar(TableGrid grid) : this()
        {
            Grid = grid;
        }

    #endregion

    #region 테마

        private void HandleAttachToPanel(AttachToPanelEvent evt)
        {
            // 패널이 바뀌면 Editor·Player 판정이 달라질 수 있으므로 다시 정한다.
            // 떨어져 있는 동안 바뀐 쿼리를 놓쳤을 수 있으므로 칩도 다시 만든다.
            Subscribe();
            TableGridTheming.Apply(this, grid?.Options);
            Rebuild();
        }

        private void Subscribe()
        {
            if (grid != null)
            {
                grid.QueryStateChanged -= HandleQueryStateChanged;
                grid.QueryStateChanged += HandleQueryStateChanged;
            }
        }

        private void Unsubscribe()
        {
            if (grid != null)
            {
                grid.QueryStateChanged -= HandleQueryStateChanged;
            }
        }

    #endregion

    #region 칩 구성

        private void HandleQueryStateChanged(object sender, EventArgs evt)
        {
            Rebuild();
        }

        private void Rebuild()
        {
            Clear();
            var query = grid?.QueryState;
            if (query == null)
            {
                return;
            }

            if (query.SortDirection.HasValue)
            {
                var columnID = query.SortColumnID;
                var icon = query.SortDirection == TableGridSortDirection.Ascending ? "arrow-up" : "arrow-down";
                AddChip
                (
                    TitleOf(columnID), icon, 0,
                    chip => grid.OpenColumnMenu(columnID, chip.worldBound, TableGridMenuSection.Sort),
                    () => grid.RequestQuery(grid.QueryState.WithSort(null, null))
                );
            }

            foreach (var filter in query.Filters)
            {
                var columnID = filter.ColumnID;
                var multiChoice = filter.ValueKind == TableGridColumnValueKind.Choice && filter.Choices.Count > 1;
                AddChip
                (
                    DescribeFilter(filter), "funnel", multiChoice ? filter.Choices.Count : 0,
                    chip => grid.OpenFilterEditor(columnID, chip.worldBound),
                    () => grid.RequestQuery(grid.QueryState.WithFilter(columnID, null))
                );
            }

            if (!string.IsNullOrEmpty(query.GroupColumnID))
            {
                var columnID = query.GroupColumnID;
                AddChip
                (
                    $"그룹 · {TitleOf(columnID)}", "layers", 0,
                    chip => grid.OpenColumnMenu(columnID, chip.worldBound, TableGridMenuSection.Group),
                    () => grid.RequestQuery(grid.QueryState.WithGroup(null))
                );
            }
        }

        private void AddChip(string text, string icon, int count, Action<VisualElement> open, Action remove)
        {
            var chip = new VisualElement();
            chip.AddToClassList("xeri-table-chips__chip");

            var body = new VisualElement();
            body.AddToClassList("xeri-table-chips__body");
            var iconElement = new VisualElement();
            iconElement.AddToClassList("xeri-table-popover__icon");
            iconElement.AddToClassList($"xeri-table-popover__icon--{icon}");
            body.Add(iconElement);
            var label = new Label(text);
            label.AddToClassList("xeri-table-chips__label");
            body.Add(label);
            if (count > 0)
            {
                // 고른 값 개수는 Xeri Core Badge로 덧붙인다.
                var countBadge = new Badge(count.ToString());
                countBadge.Tone = BadgeTone.Accent;
                countBadge.AddToClassList("xeri-table-chips__count");
                body.Add(countBadge);
            }

            body.AddManipulator(new Clickable(() => open(chip)));
            chip.Add(body);

            var removeButton = new VisualElement();
            removeButton.AddToClassList("xeri-table-chips__remove");
            removeButton.AddManipulator(new Clickable(remove));
            chip.Add(removeButton);

            Add(chip);
        }

    #endregion

    #region 칩 글자

        private string TitleOf(string columnID)
        {
            if (grid != null)
            {
                foreach (var column in grid.Columns)
                {
                    if (string.Equals(column.ID, columnID, StringComparison.Ordinal))
                    {
                        return column.Title;
                    }
                }
            }

            return columnID;
        }

        private string DescribeFilter(TableGridFilterRule filter)
        {
            var title = TitleOf(filter.ColumnID);
            switch (filter.ValueKind)
            {
                case TableGridColumnValueKind.Choice:
                    return filter.Choices.Count == 1 ? $"{title}: {filter.Choices[0]}" : title;
                case TableGridColumnValueKind.Number:
                    return DescribeRange(title, filter.Minimum, filter.Maximum);
                default:
                    return $"{title} {DescribeOperator(filter.Operator)} \"{filter.Value}\"";
            }
        }

        private static string DescribeRange(string title, string minimum, string maximum)
        {
            if (minimum.Length > 0 && maximum.Length > 0)
            {
                return $"{title} {minimum}–{maximum}";
            }

            return minimum.Length > 0 ? $"{title} ≥ {minimum}" : $"{title} ≤ {maximum}";
        }

        private static string DescribeOperator(TableGridFilterOperator filterOperator)
        {
            return filterOperator switch
            {
                TableGridFilterOperator.Equals => "=",
                TableGridFilterOperator.NotEquals => "≠",
                TableGridFilterOperator.StartsWith => "시작",
                TableGridFilterOperator.GreaterThan => ">",
                TableGridFilterOperator.GreaterThanOrEqual => "≥",
                TableGridFilterOperator.LessThan => "<",
                TableGridFilterOperator.LessThanOrEqual => "≤",
                _ => "포함",
            };
        }

    #endregion

    }
}
