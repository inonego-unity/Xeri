/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TableGridColumnMenu.cs
수정일 : 2026-11-21

# 설명
열 헤더에 붙는 열 메뉴의 화면 요소. 머리(열 이름 · 열 설정) 아래에 행 정렬, 필터, 그룹, 고정, 표시 구역을 둔다.
표시 구역은 열 숨기기와, 옆에 열 표시·순서 창을 여는 진입 행이다.
쿼리 칩에서 열면 해당 구역(정렬 또는 그룹)만 보인다.

# 특이사항, 제약사항
고른 동작은 이벤트로만 알리고 쿼리를 직접 바꾸지 않는다. 팝업에 띄우고 닫는 일은 TableGridPopover가 맡는다.
열이 허용하지 않는 구역(Sortable·Filterable·Groupable)과 옵션이 끈 구역은 만들지 않는다.
필터 행은 옆에 필터 편집 창을 여는 진입 행이다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UIElements;

namespace inonego.Xeri.UI.TableGrid
{
    // ============================================================
    /// <summary>
    /// 열 메뉴에서 보일 구역.
    /// </summary>
    // ============================================================
    internal enum TableGridMenuSection
    {
        All,
        Sort,
        Group,
    }

    // ============================================================
    /// <summary>
    /// 열 메뉴 화면 요소.
    /// </summary>
    // ============================================================
    internal sealed class TableGridColumnMenu : TableGridPopoverPanel
    {

    #region 필드

        private int sectionCount;

        // ------------------------------------------------------------
        /// <summary>
        /// 필터 진입 행. 필터 구역이 없으면 null이다.
        /// </summary>
        // ------------------------------------------------------------
        public VisualElement FilterAction { get; private set; }

        // ------------------------------------------------------------
        /// <summary>
        /// 열 표시·순서 진입 행. 표시 구역이 없으면 null이다.
        /// </summary>
        // ------------------------------------------------------------
        public VisualElement ColumnsAction { get; private set; }

    #endregion

    #region 이벤트

        // ------------------------------------------------------------
        /// <summary>
        /// 정렬 방향을 골랐을 때 알린다. null이면 정렬 해제다.
        /// </summary>
        // ------------------------------------------------------------
        public event Action<TableGridSortDirection?> SortRequested;

        // ------------------------------------------------------------
        /// <summary>
        /// 필터 편집을 열어 달라고 할 때 알린다.
        /// </summary>
        // ------------------------------------------------------------
        public event Action FilterRequested;

        // ------------------------------------------------------------
        /// <summary>
        /// 그룹을 걸거나(true) 풀 때(false) 알린다.
        /// </summary>
        // ------------------------------------------------------------
        public event Action<bool> GroupRequested;

        // ------------------------------------------------------------
        /// <summary>
        /// 이 열까지 고정하거나(true) 고정을 풀 때(false) 알린다.
        /// </summary>
        // ------------------------------------------------------------
        public event Action<bool> FreezeRequested;

        // ------------------------------------------------------------
        /// <summary>
        /// 이 열을 숨겨 달라고 할 때 알린다.
        /// </summary>
        // ------------------------------------------------------------
        public event Action HideRequested;

        // ------------------------------------------------------------
        /// <summary>
        /// 열 표시·순서 창을 열어 달라고 할 때 알린다.
        /// </summary>
        // ------------------------------------------------------------
        public event Action ColumnsRequested;

    #endregion

    #region 생성과 구성

        // ------------------------------------------------------------
        /// <summary>
        /// 빈 열 메뉴를 만든다. 내용은 Build로 채운다.
        /// </summary>
        // ------------------------------------------------------------
        public TableGridColumnMenu()
        {
            // NONE
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> 열과 현재 쿼리·고정 상태에 맞춰 메뉴 행을 만든다.
        /// <br/> columnIndex는 표시 열 위치, frozenCount는 고정한 열 수다.
        /// <br/> visibleCount는 표시 열 수다.
        /// </summary>
        // ----------------------------------------------------------------------
        public void Build
        (
            TableGridColumn column, TableGridQueryState query, TableGridMenuSection section,
            TableGridOptions options, int columnIndex, int frozenCount, int visibleCount
        )
        {
            ResetContent();
            sectionCount = 0;
            FilterAction = null;
            ColumnsAction = null;
            query ??= TableGridQueryState.Empty;

            var headerText = section switch
            {
                TableGridMenuSection.Sort => "정렬 수정",
                TableGridMenuSection.Group => "그룹 수정",
                _ => "열 설정",
            };
            AddHeader(column.Title, headerText);

            var all = section == TableGridMenuSection.All;
            if ((all || section == TableGridMenuSection.Sort) && column.Sortable && options.IsWorkspace)
            {
                BuildSort(column, query);
            }

            if (all && column.Filterable && options.IsWorkspace)
            {
                BuildFilter(column, query);
            }

            if ((all || section == TableGridMenuSection.Group) && column.Groupable && options.IsWorkspace)
            {
                BuildGroup(column, query);
            }

            if (all)
            {
                BuildFreeze(columnIndex, frozenCount);
                BuildDisplay(visibleCount);
            }
        }

        private void BeginSection(string title)
        {
            if (sectionCount++ > 0)
            {
                AddSeparator();
            }

            AddSectionLabel(title);
        }

        private void BuildSort(TableGridColumn column, TableGridQueryState query)
        {
            var sorted = string.Equals(query.SortColumnID, column.ID, StringComparison.Ordinal);
            var direction = sorted ? query.SortDirection : null;

            BeginSection("행 정렬");
            AddAction
            (
                "오름차순", "arrow-up", direction == TableGridSortDirection.Ascending,
                () => SortRequested?.Invoke(TableGridSortDirection.Ascending)
            );
            AddAction
            (
                "내림차순", "arrow-down", direction == TableGridSortDirection.Descending,
                () => SortRequested?.Invoke(TableGridSortDirection.Descending)
            );

            if (direction.HasValue)
            {
                AddAction("정렬 해제", "x", false, () => SortRequested?.Invoke(null));
            }
        }

        private void BuildFilter(TableGridColumn column, TableGridQueryState query)
        {
            var filtered = query.FindFilter(column.ID) != null;

            BeginSection("필터");
            FilterAction = AddSubmenuAction
            (
                filtered ? "필터 수정" : "필터 추가", "funnel", filtered, () => FilterRequested?.Invoke()
            );
        }

        private VisualElement AddSubmenuAction(string text, string icon, bool active, Action open)
        {
            var row = AddAction(text, icon, active, open);
            row.AddToClassList("xeri-table-popover__action--submenu");

            // 하위 메뉴처럼 오른쪽 방향키로도 옆 창을 연다.
            void HandleSubmenuKeyDown(KeyDownEvent evt)
            {
                if (evt.keyCode == KeyCode.RightArrow)
                {
                    open();
                    evt.StopPropagation();
                }
            }

            row.RegisterCallback<KeyDownEvent>(HandleSubmenuKeyDown);
            return row;
        }

        private void BuildGroup(TableGridColumn column, TableGridQueryState query)
        {
            BeginSection("그룹");
            if (string.Equals(query.GroupColumnID, column.ID, StringComparison.Ordinal))
            {
                AddAction("그룹 해제", "x", false, () => GroupRequested?.Invoke(false));
            }
            else
            {
                AddAction("이 속성으로 그룹", "layers", false, () => GroupRequested?.Invoke(true));
            }
        }

        private void BuildFreeze(int columnIndex, int frozenCount)
        {
            BeginSection("고정");
            AddAction("여기까지 고정", "pin", columnIndex == frozenCount - 1, () => FreezeRequested?.Invoke(true));
            if (frozenCount > 0)
            {
                AddAction("열 고정 해제", "x", false, () => FreezeRequested?.Invoke(false));
            }
        }

        private void BuildDisplay(int visibleCount)
        {
            BeginSection("표시");

            // 표시 열은 하나 이상이어야 하므로 마지막 남은 열은 숨길 수 없다.
            var hide = AddAction("열 숨기기", "eye", false, () => HideRequested?.Invoke());
            hide.SetEnabled(visibleCount > 1);
            ColumnsAction = AddSubmenuAction("열 표시 · 순서", "columns-3", false, () => ColumnsRequested?.Invoke());
        }

    #endregion

    }
}
