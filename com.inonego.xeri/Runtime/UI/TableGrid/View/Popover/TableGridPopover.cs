/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TableGridPopover.cs
수정일 : 2026-11-23

# 설명
표의 팝오버 화면 요소(열 메뉴, 필터 편집, 열 표시·순서, 빠른 이동)를 Xeri Core PopupHost로 띄운다.
열 메뉴는 0단계로 헤더 아래에, 필터 편집과 열 표시·순서는 메뉴의 진입 행 옆 1단계로 연다(컨텍스트 메뉴의 하위 메뉴와 같은 배치).
쿼리 칩에서는 필터 편집을 0단계로 바로 열고, 빠른 이동은 표 위쪽에 0단계로 연다.

# 특이사항, 제약사항
Editor에서는 단계마다 테두리 없는 팝업 창, Runtime에서는 패널 위 프레임으로 뜬다(PopupHost의 표시 구현).
팝업 창 크기는 열기 전에 정하므로 화면 요소가 알려 준 PreferredHeight로 위치를 계산한다.
표 하나에 팝오버는 하나만 연다. 새로 열면 이전 것을 닫는다.
동작을 고르면 먼저 닫은 뒤 이벤트를 알리고, 표로 포커스를 돌려준다. 바깥을 눌러 닫힐 때는 포커스를 옮기지 않는다.
열 표시·순서만은 바꿀 때마다 알리고 창을 열어 둔다(여러 열을 이어서 바꾸므로).
팝오버 화면 요소는 표 밖 창·프레임에 놓이므로 소유 표의 옵션으로 자기 패널 기준 테마 클래스를 붙인다.
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
    /// 표의 팝오버(열 메뉴·필터 편집·열 표시·빠른 이동).
    /// </summary>
    // ============================================================
    internal sealed class TableGridPopover
    {

    #region 필드

        private readonly TableGrid owner;
        private PopupHost host;
        private TableGridColumnMenu menu;
        private TableGridColumn column;

    #endregion

    #region 이벤트

        // ------------------------------------------------------------
        /// <summary>
        /// 팝오버가 닫혔을 때 알린다.
        /// </summary>
        // ------------------------------------------------------------
        public event Action Closed;

        // ------------------------------------------------------------
        /// <summary>
        /// 정렬을 골랐을 때 열과 방향을 알린다. null 방향은 정렬 해제다.
        /// </summary>
        // ------------------------------------------------------------
        public event Action<TableGridColumn, TableGridSortDirection?> SortRequested;

        // ------------------------------------------------------------
        /// <summary>
        /// 필터를 적용했을 때 열과 규칙을 알린다. null 규칙은 필터 해제다.
        /// </summary>
        // ------------------------------------------------------------
        public event Action<TableGridColumn, TableGridFilterRule> FilterApplied;

        // ------------------------------------------------------------
        /// <summary>
        /// 그룹을 걸거나(true) 풀 때(false) 열을 알린다.
        /// </summary>
        // ------------------------------------------------------------
        public event Action<TableGridColumn, bool> GroupRequested;

        // ------------------------------------------------------------
        /// <summary>
        /// 열까지 고정하거나(true) 고정을 풀 때(false) 열을 알린다.
        /// </summary>
        // ------------------------------------------------------------
        public event Action<TableGridColumn, bool> FreezeRequested;

        // ------------------------------------------------------------
        /// <summary>
        /// 열 숨기기를 골랐을 때 열을 알린다.
        /// </summary>
        // ------------------------------------------------------------
        public event Action<TableGridColumn> HideRequested;

        // ------------------------------------------------------------
        /// <summary>
        /// 열 표시·순서 창에서 표시 열 순서를 바꿨을 때 알린다.
        /// </summary>
        // ------------------------------------------------------------
        public event Action<IReadOnlyList<string>> VisibleColumnsChanged;

    #endregion

    #region 생성

        // ------------------------------------------------------------
        /// <summary>
        /// 팝오버를 소유하는 표를 지정한다. 표의 옵션으로 팝오버 테마를 정한다.
        /// </summary>
        // ------------------------------------------------------------
        public TableGridPopover(TableGrid owner)
        {
            this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
        }

    #endregion

    #region 열기

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> 기준 영역(월드 좌표) 아래에 열 메뉴를 연다.
        /// <br/> 쿼리·고정·표시 열 상태는 모델에서 읽는다.
        /// </summary>
        // ------------------------------------------------------------
        public void OpenMenu
        (
            TableGridColumn column, Rect worldAnchor, TableGridMenuSection section,
            TableGridModel model, TableGridOptions options
        )
        {
            Begin(column, worldAnchor);
            try
            {
                var columns = model.Columns;
                var query = model.Query;
                menu = new TableGridColumnMenu();
                menu.Build
                (
                    column, query, section, options,
                    columns.GetVisibleIndex(column.ID), columns.FrozenCount, columns.VisibleColumns.Count
                );
                menu.SortRequested += direction => Finish(() => SortRequested?.Invoke(column, direction));
                menu.GroupRequested += grouped => Finish(() => GroupRequested?.Invoke(column, grouped));
                menu.FreezeRequested += frozen => Finish(() => FreezeRequested?.Invoke(column, frozen));
                menu.HideRequested += () => Finish(() => HideRequested?.Invoke(column));
                menu.FilterRequested += () => OpenFilterBesideMenu(query.FindFilter(column.ID));
                menu.ColumnsRequested += () => OpenColumnsBesideMenu(columns);
                menu.CloseRequested += CloseAndFocusOwner;

                var anchor = host.ToPopupRect(worldAnchor);
                OpenPanel(0, menu, TableGridPopoverMetrics.MENU_WIDTH, anchor, CreateRootOptions());
            }
            catch
            {
                Close();
                throw;
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 기준 영역(월드 좌표) 아래에 필터 편집을 바로 연다.
        /// </summary>
        // ------------------------------------------------------------
        public void OpenFilter(TableGridColumn column, Rect worldAnchor, TableGridFilterRule rule)
        {
            Begin(column, worldAnchor);
            try
            {
                var editor = CreateFilterEditor(rule);
                var anchor = host.ToPopupRect(worldAnchor);
                OpenPanel(0, editor, TableGridPopoverMetrics.FILTER_WIDTH, anchor, CreateRootOptions());
            }
            catch
            {
                Close();
                throw;
            }
        }

        private void Begin(TableGridColumn target, Rect worldAnchor)
        {
            OpenHost(target ?? throw new ArgumentNullException(nameof(target)), worldAnchor);
        }

        // 열 없는 창(입력칸 하나짜리)은 target이 null이다.
        private void OpenHost(TableGridColumn target, Rect worldAnchor)
        {
            Close();
            column = target;
            host = PopupHost.Open(owner, worldAnchor, null, HandleDismissed);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> 기준 영역(월드 좌표) 아래에 빠른 이동 창을 연다.
        /// <br/> jump는 입력을 받아 이동하고 실패하면 보일 문구를 돌려준다.
        /// </summary>
        // ------------------------------------------------------------
        public void OpenJump(Rect worldAnchor, Func<string, string> jump)
        {
            var hint = "전체 행에서 번호나 ID로 찾습니다";
            OpenPrompt(worldAnchor, panel => panel.Build("빠른 이동", XeriShortcutKeys.Format(TableGridKeyboardInput.JUMP_KEY, action: true), "행 번호 또는 ID", hint, jump));
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> 기준 영역(월드 좌표) 아래에 입력칸 하나짜리 창(위치로 이동 등)을 연다.
        /// <br/> submit은 입력을 받아 처리하고 실패하면 보일 문구를 돌려준다.
        /// </summary>
        // ----------------------------------------------------------------------
        public void OpenPrompt
        (
            Rect worldAnchor, string title, string placeholder, string hint, Func<string, string> submit
        )
        {
            OpenPrompt(worldAnchor, panel => panel.Build(title, null, placeholder, hint, submit));
        }

        private void OpenPrompt(Rect worldAnchor, Action<TableGridPromptPanel> build)
        {
            OpenHost(null, worldAnchor);
            try
            {
                var panel = new TableGridPromptPanel();
                build(panel);
                panel.Submitted += CloseAndFocusOwner;
                panel.CloseRequested += CloseAndFocusOwner;

                var anchor = host.ToPopupRect(worldAnchor);
                OpenPanel(0, panel, TableGridPopoverMetrics.PROMPT_WIDTH, anchor, CreateRootOptions());
            }
            catch
            {
                Close();
                throw;
            }
        }

        private void OpenFilterBesideMenu(TableGridFilterRule rule)
        {
            if (host == null || menu?.FilterAction == null)
            {
                return;
            }

            host.CloseFrom(1);
            var editor = CreateFilterEditor(rule);
            OpenBesideMenu(menu.FilterAction, editor, TableGridPopoverMetrics.FILTER_WIDTH);
        }

        private void OpenColumnsBesideMenu(TableGridColumnSet columns)
        {
            if (host == null || menu?.ColumnsAction == null)
            {
                return;
            }

            host.CloseFrom(1);
            var panel = new TableGridColumnPanel();
            panel.Build(columns.SourceColumns, columns.VisibleColumns, columns.FrozenCount);

            // 표시·순서는 바꾸는 즉시 표에 반영하고 창은 열어 둔다.
            panel.VisibleColumnsChanged += ids => VisibleColumnsChanged?.Invoke(ids);
            panel.CloseRequested += CloseAndFocusOwner;
            OpenBesideMenu(menu.ColumnsAction, panel, TableGridPopoverMetrics.MENU_WIDTH);
        }

        private void OpenBesideMenu(VisualElement row, TableGridPopoverPanel panel, float width)
        {
            // 컨텍스트 메뉴의 하위 메뉴처럼 메뉴가 실제로 놓인 경계 옆에, 진입 행 높이에 맞춰 붙인다.
            var menuBounds = host.GetBounds(0);
            var crossOffset = row.worldBound.y - menu.worldBound.y - TableGridPopoverMetrics.PADDING;
            var options = new PopupPlacementOptions
            (
                PopupPlacementDirection.Right,
                gap: TableGridPopoverMetrics.SUBMENU_GAP,
                padding: TableGridPopoverMetrics.EDGE_MARGIN,
                allowAnchorOverlap: true,
                crossOffset: crossOffset
            );
            OpenPanel(1, panel, width, menuBounds, options);
        }

        private TableGridFilterEditor CreateFilterEditor(TableGridFilterRule rule)
        {
            var editor = new TableGridFilterEditor();
            editor.Build(column, rule);
            var target = column;
            editor.Applied += applied => Finish(() => FilterApplied?.Invoke(target, applied));
            editor.CloseRequested += CloseAndFocusOwner;
            return editor;
        }

        private void OpenPanel
        (
            int depth, TableGridPopoverPanel panel, float width, Rect anchor, PopupPlacementOptions options
        )
        {
            var result = host.Solve(anchor, new Vector2(width, panel.PreferredHeight), options);
            panel.style.width = result.Bounds.width;
            panel.style.height = result.Bounds.height;

            // 팝오버가 놓일 창·프레임의 패널 기준으로 표와 같은 테마 클래스를 붙인다.
            TableGridTheming.Attach(panel, owner.Options);
            host.OpenLevel(depth, result.Bounds).Add(panel);
            panel.FocusFirst();
        }

        private static PopupPlacementOptions CreateRootOptions()
        {
            // 헤더 아래를 우선하고, 모자라면 위로 열며, 오른쪽을 넘으면 오른쪽 끝을 헤더에 맞춘다.
            return new PopupPlacementOptions
            (
                PopupPlacementDirection.Below,
                padding: TableGridPopoverMetrics.EDGE_MARGIN,
                minMainSize: TableGridPopoverMetrics.HEADER_HEIGHT + TableGridPopoverMetrics.ROW_HEIGHT,
                allowAnchorOverlap: true,
                allowAlignmentFlip: true
            );
        }

    #endregion

    #region 닫기

        // ------------------------------------------------------------
        /// <summary>
        /// 열린 팝오버를 닫는다. 닫혀 있으면 아무것도 하지 않는다.
        /// </summary>
        // ------------------------------------------------------------
        public void Close()
        {
            if (host == null)
            {
                return;
            }

            var closing = host;
            host = null;
            menu = null;
            closing.Close();
            Closed?.Invoke();
        }

        private void CloseAndFocusOwner()
        {
            Close();
            owner.Focus();
        }

        private void Finish(Action notify)
        {
            // 쿼리 변경이 표를 다시 그리기 전에 팝오버를 먼저 닫는다.
            CloseAndFocusOwner();
            notify();
        }

        private void HandleDismissed()
        {
            // 바깥 클릭·포커스 이탈로 호스트가 스스로 닫혔다.
            host = null;
            menu = null;
            Closed?.Invoke();
        }

    #endregion

    }
}
