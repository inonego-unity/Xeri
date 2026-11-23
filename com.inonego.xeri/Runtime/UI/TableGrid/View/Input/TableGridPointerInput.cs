/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TableGridPointerInput.cs
수정일 : 2026-11-22

# 설명
포인터로 고르는 선택과 끌기 손잡이로 하는 행 이동.
Selection이 Cell이면 셀 클릭·Shift 클릭·끌기는 셀 범위, 행 선택 칸 클릭·Ctrl·Shift·끌기는 행 선택을 만든다.
Row이면 셀 클릭이 그 행 하나를, MultiRow이면 셀 클릭·Ctrl·Shift·끌기가 행 선택을 만들고 누른 열이 활성 열이 된다. None이면 고르지 않는다.
행 선택 칸을 끌면 누른 행부터 포인터 아래 행까지 고르고, Ctrl로 시작하면 기존 선택에 그 범위를 더하거나 뺀다.
끄는 동안 포인터가 행 영역 밖에 있으면 그 방향으로 자동 스크롤하며 범위를 잇는다.
끌기 손잡이를 눌러 끌면 옮길 행(누른 행이 선택에 들면 그 선택, 아니면 그 행 하나)을 흐리게 하고 가장 가까운 행 경계에 삽입선을 보인다.
행 영역 위·아래 가장자리에서는 자동 스크롤하고, Paged에서 더 갈 수 없으면 가장자리에 머문 지 500ms마다 앞·뒤 페이지로 넘긴다.
놓으면 대상 행과 앞·뒤를 RowsDropped로 알리고, 순서가 그대로인 자리(옮길 덩어리 안이나 바로 앞뒤)는 알리지 않는다. Escape·포인터 점유 상실은 취소다.
Inline 셀 안 컨트롤을 누르면 그 셀(행 선택 표이면 그 행)만 고르고 포커스·포인터 점유·끌기는 하지 않아 컨트롤에 남긴다.
열을 고를 수 있는 표(Workspace + Cell)에서 헤더를 누르면 그 열을 고르고, Ctrl은 더하거나 빼고, Shift는 기준 열부터 잇고,
끌면 누른 열부터 포인터 아래 열까지 고른다(행 선택 칸과 같은 규칙). 고른 헤더를 Ctrl·Shift 없이 눌러 끌면 고른 열을 흐리게 하고
가장 가까운 열 경계에 삽입선을 보이며, 놓으면 경계와 구역(고정·스크롤)을 ColumnsDropped로 알린다. 끌지 않고 놓으면 그 열 하나를 고른다.
헤더 끌기는 왼쪽·오른쪽 가장자리 밖에서 가로로만 자동 스크롤한다. 열 끌기도 Escape·포인터 점유 상실은 취소다.

# 특이사항, 제약사항
선택은 TableGrid.Selection으로만 바꿔 SelectionChanged가 한 경로로 나간다.
행 끌기는 Core DragDrop 대신 표 루트의 포인터 점유로 처리한다. 손잡이 요소는 스크롤·페이지 넘김 중 다른 행에 다시 쓰이고,
놓을 자리는 영역 진입이 아니라 행 경계 계산이어서다. 이동 가능 여부·옮길 행·실제 요청은 TableGrid(prepareRowDrag, RowsDropped)가 정한다.
열 끌기도 같은 포인터 점유로 처리하고, 새 보기 순서 계산과 적용은 TableGrid(ColumnsDropped)가 한다.
그룹 머리·요약 행은 고를 수 없다. 끌기 중 그런 행 위에 있으면 직전 범위를 유지한다.
범위는 소스 결과의 위치로 정하므로 기준 행이 화면 밖 멀리 있어도 끊기지 않는다.
옵션의 Selection이 셀 범위·행 선택을 허용하지 않으면 해당 선택을 만들지 않는다.
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
    /// 포인터 선택·행 끌기 입력.
    /// </summary>
    // ============================================================
    internal sealed class TableGridPointerInput
    {

    #region 필드

        private const long AUTO_SCROLL_INTERVAL_MS = 16;

        // 손잡이·고른 헤더를 이만큼 움직여야 행·열 끌기로 본다(그 전에 놓으면 선택만 바뀐다).
        private const float MOVE_DRAG_THRESHOLD = 4f;

        // 행 끌기에서 자동 스크롤·페이지 넘김을 시작하는 행 영역 위·아래 가장자리 띠의 두께.
        private const float ROW_DRAG_EDGE = 16f;

        // Paged에서 가장자리에 이만큼 머물면 앞·뒤 페이지로 넘긴다.
        private const long PAGE_FLIP_DELAY_MS = 500;

        private readonly VisualElement owner;
        private readonly TableGridModel model;
        private readonly TableGridViewport viewport;
        private readonly TableGridHeaderView header;
        private readonly TableGridOptions options;
        private readonly Action<TableGridSelection> select;
        private readonly Func<int, IReadOnlyList<string>> prepareRowDrag;
        private readonly IVisualElementScheduledItem autoScroll;

        private readonly List<string> rowDragBase = new();

        private bool dragging;
        private bool draggingRows;
        private int dragPointerID = -1;
        private TableGridCellAddress dragAnchor;
        private Vector2 lastPosition;

        private int rowDragAnchor = -1;
        private int rowDragLast = -1;
        private bool rowDragAdds;

        private readonly List<string> movingRowIDs = new();
        private readonly HashSet<string> movingRowSet = new(StringComparer.Ordinal);
        private bool movingRows;
        private bool movingStarted;
        private bool movingContiguous;
        private int movingFirst;
        private int movingLast;
        private Vector2 movingPressPosition;
        private int edgeDirection;
        private long edgeHoldMs;

        private readonly List<string> columnDragBase = new();
        private bool draggingColumns;
        private bool columnDragAdds;
        private int columnDragAnchor = -1;
        private int columnDragLast = -1;

        private readonly HashSet<string> movingColumnSet = new(StringComparer.Ordinal);
        private bool movingColumns;
        private bool movingColumnsStarted;
        private string movingColumnPressID;

    #endregion

    #region 이벤트

        // ------------------------------------------------------------
        /// <summary>
        /// 셀·행 선택 칸을 우클릭했을 때 패널 좌표를 알린다.
        /// </summary>
        // ------------------------------------------------------------
        public event Action<Vector2> ContextMenuRequested;

        // ------------------------------------------------------------
        /// <summary>
        /// 셀을 두 번 눌렀을 때 알린다. 그 셀은 이미 활성 셀이다.
        /// </summary>
        // ------------------------------------------------------------
        public event Action CellDoubleClicked;

        // --------------------------------------------------------------------------------
        /// <summary>
        /// <br/> 끌기 손잡이로 끈 행을 놓았을 때 옮길 행(화면 순서), 대상 행, 대상의 앞·뒤를 알린다.
        /// <br/> 순서가 바뀌는 자리에서만 알린다.
        /// </summary>
        // --------------------------------------------------------------------------------
        public event Action<IReadOnlyList<string>, string, TableGridMovePlacement> RowsDropped;

        // ----------------------------------------------------------------------
        /// <summary>
        /// Paged 행 끌기 중 가장자리에 머물러 앞(-1)·뒤(1) 페이지로 넘겨야 할 때 알린다.
        /// </summary>
        // ----------------------------------------------------------------------
        public event Action<int> PageStepRequested;

        // --------------------------------------------------------------------------------
        /// <summary>
        /// <br/> 고른 헤더를 끌어 놓았을 때 옮길 열, 열 경계(지금 보기 순서에서 열 gap 바로 왼쪽),
        /// <br/> 경계가 고정 구역인지를 알린다. 순서가 그대로인지는 TableGrid가 판단한다.
        /// </summary>
        // --------------------------------------------------------------------------------
        public event Action<IReadOnlyCollection<string>, int, bool> ColumnsDropped;

    #endregion

    #region 생성

        // --------------------------------------------------------------------------------
        /// <summary>
        /// <br/> 본문의 셀·행 선택 칸·끌기 손잡이 입력, 헤더 칸 누름과 표 루트의 끌기 입력을 연결한다.
        /// <br/> prepareRowDrag는 손잡이를 누른 행으로 옮길 행을 정한다(옮길 수 없으면 null).
        /// </summary>
        // --------------------------------------------------------------------------------
        public TableGridPointerInput
        (
            VisualElement owner, TableGridModel model, TableGridViewport viewport, TableGridHeaderView header,
            TableGridOptions options, Action<TableGridSelection> select,
            Func<int, IReadOnlyList<string>> prepareRowDrag
        )
        {
            this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
            this.model = model ?? throw new ArgumentNullException(nameof(model));
            this.viewport = viewport ?? throw new ArgumentNullException(nameof(viewport));
            this.header = header ?? throw new ArgumentNullException(nameof(header));
            this.options = options ?? throw new ArgumentNullException(nameof(options));
            this.select = select ?? throw new ArgumentNullException(nameof(select));
            this.prepareRowDrag = prepareRowDrag ?? throw new ArgumentNullException(nameof(prepareRowDrag));

            viewport.CellPointerDown += HandleCellPointerDown;
            viewport.InlinePointerDown += HandleInlinePointerDown;
            viewport.SelectorPointerDown += HandleSelectorPointerDown;
            viewport.ReorderHandlePointerDown += HandleReorderHandlePointerDown;
            header.HeaderPointerDown += HandleHeaderPointerDown;
            owner.RegisterCallback<PointerMoveEvent>(HandlePointerMove);
            owner.RegisterCallback<PointerUpEvent>(HandlePointerUp);
            owner.RegisterCallback<PointerCaptureOutEvent>(_ => EndDrag());

            // 행·열 끌기 중 Escape는 키보드 입력(선택 줄이기·잘라낸 표시 지우기)보다 먼저 받아 끌기만 취소한다.
            owner.RegisterCallback<KeyDownEvent>(HandleKeyDown, TrickleDown.TrickleDown);

            autoScroll = owner.schedule.Execute(HandleAutoScroll).Every(AUTO_SCROLL_INTERVAL_MS);
            autoScroll.Pause();
        }

    #endregion

    #region 셀 범위

        private void HandleCellPointerDown(int rowIndex, int columnIndex, PointerDownEvent evt)
        {
            if (evt.button == 1)
            {
                HandleCellContextClick(rowIndex, columnIndex, evt);
                return;
            }

            if (evt.button != 0)
            {
                return;
            }

            owner.Focus();
            evt.StopPropagation();
            if (!model.TryGetCellAddress(rowIndex, columnIndex, out var address))
            {
                return;
            }

            if (options.Selection != TableGridSelectionMode.Cell)
            {
                HandleRowModeCellDown(rowIndex, address, evt);
                return;
            }

            var current = model.Selection.Current;
            var extend = evt.shiftKey && options.AllowsCellRange && current.Kind == TableGridSelectionKind.Cell;
            var anchor = extend ? current.Anchor : address;
            select(TableGridSelection.CellRange(anchor, address));

            // 두 번 누르면 끌기 대신 그 셀 편집을 연다.
            if (evt.clickCount == 2)
            {
                CellDoubleClicked?.Invoke();
                return;
            }

            if (!options.AllowsCellRange)
            {
                return;
            }

            dragAnchor = anchor;
            BeginDrag(evt, false);
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> Inline 셀 안 컨트롤 누름. 그 셀(Row·MultiRow면 그 행)만 고르고
        /// <br/> 포커스·포인터 점유·끌기·두 번 누름 처리는 하지 않아 컨트롤에 남긴다.
        /// </summary>
        // ----------------------------------------------------------------------
        private void HandleInlinePointerDown(int rowIndex, int columnIndex, PointerDownEvent evt)
        {
            if (evt.button != 0 || !model.TryGetCellAddress(rowIndex, columnIndex, out var address))
            {
                return;
            }

            switch (options.Selection)
            {
                case TableGridSelectionMode.Cell:
                    select(TableGridSelection.SingleCell(address));
                    break;
                case TableGridSelectionMode.Row:
                case TableGridSelectionMode.MultiRow:
                    SelectSingleRow(address.RowID, address.ColumnID);
                    break;
            }
        }

        // --------------------------------------------------------------------------------
        /// <summary>
        /// <br/> Row·MultiRow·None 표의 셀 누름.
        /// <br/> Row는 그 행 하나를 고른다.
        /// <br/> MultiRow는 행 선택 칸과 같은 규칙(Ctrl·Shift·끌기)으로 행 선택을 만든다.
        /// <br/> 누른 열은 활성 열이 된다. None은 선택을 바꾸지 않는다.
        /// <br/> 두 번 누르면 CellDoubleClicked를 알린다.
        /// </summary>
        // --------------------------------------------------------------------------------
        private void HandleRowModeCellDown(int rowIndex, TableGridCellAddress address, PointerDownEvent evt)
        {
            if (options.Selection == TableGridSelectionMode.Row)
            {
                SelectSingleRow(address.RowID, address.ColumnID);
            }
            else if (options.Selection == TableGridSelectionMode.MultiRow && evt.clickCount < 2)
            {
                model.Selection.RowActiveColumnID = address.ColumnID;
                BeginRowSelection(rowIndex, address.RowID, evt);
            }

            if (evt.clickCount == 2)
            {
                CellDoubleClicked?.Invoke();
            }
        }

        private void SelectSingleRow(string rowID, string columnID)
        {
            var single = new[]
            {
                rowID,
            };
            model.Selection.RowAnchorID = rowID;
            model.Selection.RowActiveColumnID = columnID;
            select(TableGridSelection.Rows(single));
        }

        private void BeginDrag(PointerDownEvent evt, bool rows)
        {
            dragging = true;
            draggingRows = rows;
            dragPointerID = evt.pointerId;
            lastPosition = evt.position;
            owner.CapturePointer(evt.pointerId);
            autoScroll.Resume();
        }

        private void HandleCellContextClick(int rowIndex, int columnIndex, PointerDownEvent evt)
        {
            owner.Focus();
            evt.StopPropagation();
            if (!model.TryGetCellAddress(rowIndex, columnIndex, out var address))
            {
                return;
            }

            // 선택 밖을 우클릭하면 그 셀(행 선택 표는 그 행)을 고르고, 선택 안이면 선택을 그대로 두고 메뉴를 연다.
            var inSelection = model.Selection.Current.Kind == TableGridSelectionKind.Row
                ? model.Selection.IsRowSelected(address.RowID)
                : model.Selection.GetCellMarks(rowIndex, columnIndex) != TableGridCellMarks.None;
            if (!inSelection && options.Selection == TableGridSelectionMode.Cell)
            {
                select(TableGridSelection.SingleCell(address));
            }
            else if (!inSelection && options.Selection != TableGridSelectionMode.None)
            {
                SelectSingleRow(address.RowID, address.ColumnID);
            }

            ContextMenuRequested?.Invoke(evt.position);
        }

        private void HandlePointerMove(PointerMoveEvent evt)
        {
            if (!dragging || evt.pointerId != dragPointerID)
            {
                return;
            }

            lastPosition = evt.position;
            if (movingRows)
            {
                UpdateRowMove(lastPosition);
            }
            else if (movingColumns)
            {
                UpdateColumnMove(lastPosition);
            }
            else if (draggingColumns)
            {
                ExtendColumnsTo(lastPosition.x);
            }
            else
            {
                ExtendTo(lastPosition);
            }

            evt.StopPropagation();
        }

        private void HandlePointerUp(PointerUpEvent evt)
        {
            if (!dragging || evt.pointerId != dragPointerID)
            {
                return;
            }

            if (movingRows)
            {
                DropRows(evt.position);
            }
            else if (movingColumns)
            {
                DropColumns(evt.position);
            }
            else
            {
                EndDrag();
            }

            evt.StopPropagation();
        }

        private void HandleAutoScroll(TimerState state)
        {
            if (!dragging)
            {
                return;
            }

            if (movingRows)
            {
                TickRowMove(state.deltaTime);
                return;
            }

            if (movingColumns || draggingColumns)
            {
                TickHeaderDrag();
                return;
            }

            // 스크롤하면 포인터 아래 셀이 바뀌므로 범위도 다시 잇는다.
            if (viewport.AutoScroll(lastPosition))
            {
                ExtendTo(lastPosition);
            }
        }

        private void ExtendTo(Vector2 position)
        {
            if (!viewport.TryFindCell(position, out var rowIndex, out var columnIndex))
            {
                return;
            }

            if (draggingRows)
            {
                if (rowIndex != rowDragLast)
                {
                    rowDragLast = rowIndex;
                    ApplyRowRange(rowIndex);
                }

                return;
            }

            if (!model.TryGetCellAddress(rowIndex, columnIndex, out var address))
            {
                return;
            }

            var current = model.Selection.Current;
            if (current.Kind == TableGridSelectionKind.Cell && current.Focus == address && current.Anchor == dragAnchor)
            {
                return;
            }

            select(TableGridSelection.CellRange(dragAnchor, address));
        }

        private void EndDrag()
        {
            if (!dragging)
            {
                return;
            }

            dragging = false;
            draggingRows = false;
            draggingColumns = false;
            rowDragBase.Clear();
            columnDragBase.Clear();
            EndRowMove();
            EndColumnMove();
            autoScroll.Pause();
            if (owner.HasPointerCapture(dragPointerID))
            {
                owner.ReleasePointer(dragPointerID);
            }

            dragPointerID = -1;
        }

    #endregion

    #region 행 선택

        private void HandleSelectorPointerDown(int rowIndex, PointerDownEvent evt)
        {
            if (evt.button == 1)
            {
                HandleSelectorContextClick(rowIndex, evt);
                return;
            }

            if (evt.button != 0)
            {
                return;
            }

            owner.Focus();
            evt.StopPropagation();
            if (!options.AllowsRowSelection || !model.TryGetDataRowID(rowIndex, out var rowID))
            {
                return;
            }

            model.Selection.RowActiveColumnID = null;
            BeginRowSelection(rowIndex, rowID, evt);
        }

        // --------------------------------------------------------------------------------
        /// <summary>
        /// <br/> 행 하나를 눌러 행 선택을 시작한다(행 선택 칸, MultiRow 표의 셀).
        /// <br/> Shift는 기준 행부터 잇고, Ctrl은 기존 선택에 더하거나 빼며, 끌면 그 범위로 넓힌다.
        /// </summary>
        // --------------------------------------------------------------------------------
        private void BeginRowSelection(int rowIndex, string rowID, PointerDownEvent evt)
        {
            var state = model.Selection;
            var current = state.Current;
            var anchorIndex = model.Rows.IndexOf(state.RowAnchorID);
            rowDragBase.Clear();
            rowDragAdds = true;

            // Shift는 기준 행을 그대로 두고 기준부터 잇는다. 그 밖에는 누른 행이 새 기준이다.
            // Ctrl은 기존 행 선택을 바탕으로, 누른 행이 골라져 있었으면 빼고 아니면 더한다(끌면 그 범위 전체에 같은 동작).
            if (!evt.shiftKey || anchorIndex < 0)
            {
                if (evt.actionKey && current.Kind == TableGridSelectionKind.Row)
                {
                    rowDragBase.AddRange(current.RowIDs);
                    rowDragAdds = !state.IsRowSelected(rowID);
                }

                state.RowAnchorID = rowID;
                anchorIndex = rowIndex;
            }

            rowDragAnchor = anchorIndex;
            rowDragLast = rowIndex;
            ApplyRowRange(rowIndex);
            BeginDrag(evt, true);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> 끌기 기준 행부터 to 행까지의 범위를 행 선택에 반영한다.
        /// <br/> Ctrl로 시작했으면 시작 전 선택에 범위를 더하거나 뺀다.
        /// </summary>
        // ------------------------------------------------------------
        private void ApplyRowRange(int to)
        {
            var rowIDs = MergeRange(rowDragBase, rowDragAdds, model.ToRowIDs(model.CollectDataRowPositions(rowDragAnchor, to)));
            select(rowIDs.Count > 0 ? TableGridSelection.Rows(rowIDs) : TableGridSelection.None);
        }

        private void HandleSelectorContextClick(int rowIndex, PointerDownEvent evt)
        {
            owner.Focus();
            evt.StopPropagation();
            if (!model.TryGetDataRowID(rowIndex, out var rowID))
            {
                return;
            }

            // 고르지 않은 행을 우클릭하면 그 행만 고른다. 행 선택을 끈 표에서는 선택을 바꾸지 않는다.
            if (options.AllowsRowSelection && !model.Selection.IsRowSelected(rowID))
            {
                var single = new[]
                {
                    rowID,
                };
                model.Selection.RowAnchorID = rowID;
                select(TableGridSelection.Rows(single));
            }

            ContextMenuRequested?.Invoke(evt.position);
        }

    #endregion

    #region 행 끌기

        private void HandleReorderHandlePointerDown(int rowIndex, PointerDownEvent evt)
        {
            // 손잡이 우클릭은 행 선택 칸과 같이 그 행 메뉴를 연다.
            if (evt.button == 1)
            {
                HandleSelectorContextClick(rowIndex, evt);
                return;
            }

            if (evt.button != 0 || dragging)
            {
                return;
            }

            owner.Focus();
            evt.StopPropagation();

            // 옮길 수 있는지·무엇을 옮길지는 표가 정한다(정렬·그룹 중이면 이유를 상태 줄에 보이고 null).
            var rowIDs = prepareRowDrag(rowIndex);
            if (rowIDs == null || rowIDs.Count == 0)
            {
                return;
            }

            BeginRowMove(rowIDs, evt.position);
            BeginDrag(evt, false);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> 옮길 행을 기억하고 한 덩어리(이어진 위치)인지 계산해 둔다.
        /// <br/> 놓을 때 순서가 그대로인 자리를 거르는 데 쓴다.
        /// <br/> 흐린 표시·삽입선은 문턱을 넘은 뒤 보인다.
        /// </summary>
        // ------------------------------------------------------------
        private void BeginRowMove(IReadOnlyList<string> rowIDs, Vector2 position)
        {
            movingRowIDs.Clear();
            movingRowIDs.AddRange(rowIDs);
            movingRowSet.Clear();
            movingRowSet.UnionWith(rowIDs);

            movingFirst = int.MaxValue;
            movingLast = -1;
            foreach (var rowID in movingRowIDs)
            {
                var index = model.Rows.IndexOf(rowID);
                movingFirst = Math.Min(movingFirst, index);
                movingLast = Math.Max(movingLast, index);
            }

            movingContiguous = movingFirst >= 0 && movingLast - movingFirst + 1 == movingRowSet.Count;
            movingRows = true;
            movingStarted = false;
            movingPressPosition = position;
            edgeDirection = 0;
            edgeHoldMs = 0;
        }

        private void UpdateRowMove(Vector2 position)
        {
            if (!movingStarted)
            {
                if (Vector2.Distance(movingPressPosition, position) < MOVE_DRAG_THRESHOLD)
                {
                    return;
                }

                movingStarted = true;
                viewport.SetDraggingRows(movingRowIDs);
            }

            var found = TryFindDrop(position.y, out var gap, out _, out _);
            viewport.ShowDropLine(found ? gap : -1);
        }

        // --------------------------------------------------------------------------------
        /// <summary>
        /// <br/> 끄는 동안 주기적으로 가장자리를 본다. 그 방향으로 스크롤할 수 있으면 스크롤하고,
        /// <br/> 더 갈 수 없을 때 Paged면 가장자리에 머문 시간을 재서 앞·뒤 페이지를 요청한다.
        /// </summary>
        // --------------------------------------------------------------------------------
        private void TickRowMove(long deltaMs)
        {
            if (!movingStarted)
            {
                return;
            }

            // 가장자리에 막 들어온 주기는 머문 시간에 넣지 않는다(쉬었다 재개한 타이머의 긴 간격을 세지 않는다).
            var edge = viewport.GetVerticalEdge(lastPosition.y, ROW_DRAG_EDGE);
            var entered = edge != edgeDirection;
            if (entered)
            {
                edgeDirection = edge;
                edgeHoldMs = 0;
            }

            if (edge == 0)
            {
                return;
            }

            // 스크롤하면 포인터 아래 경계가 바뀌므로 삽입선도 다시 놓는다.
            if (viewport.AutoScrollVertical(lastPosition.y, ROW_DRAG_EDGE))
            {
                edgeHoldMs = 0;
                UpdateRowMove(lastPosition);
                return;
            }

            if (!options.IsPaged || entered)
            {
                return;
            }

            edgeHoldMs += deltaMs;
            if (edgeHoldMs < PAGE_FLIP_DELAY_MS)
            {
                return;
            }

            // 계속 머물면 다음 500ms 뒤에 한 페이지 더 넘긴다.
            edgeHoldMs = 0;
            PageStepRequested?.Invoke(edge);
            UpdateRowMove(lastPosition);
        }

        private void DropRows(Vector2 position)
        {
            // 끌기를 끝내면 옮길 행 목록을 비우므로 알릴 값을 먼저 정한다.
            var targetRowID = default(string);
            var placement = TableGridMovePlacement.Before;
            var dropped = movingStarted && TryFindDrop(position.y, out _, out targetRowID, out placement);
            var rowIDs = dropped ? movingRowIDs.ToArray() : null;
            EndDrag();

            if (dropped)
            {
                RowsDropped?.Invoke(rowIDs, targetRowID, placement);
            }
        }

        // --------------------------------------------------------------------------------
        /// <summary>
        /// <br/> 패널 y의 행 경계(gap)를 놓을 자리로 바꾼다. 옮길 행을 빼고 그 경계에 끼우는 자리다.
        /// <br/> 경계 뒤의 첫 남는 행 앞으로 정하고, 그 행이 없거나 데이터 행이 아닌 행에 막히면
        /// <br/> 경계 앞의 마지막 남는 행 뒤로 정한다(끝에 요약 행이 있는 경우 등).
        /// <br/> 순서가 그대로인 자리(한 덩어리 안·바로 앞뒤)이거나 대상을 못 찾으면 false다.
        /// </summary>
        // --------------------------------------------------------------------------------
        private bool TryFindDrop(float y, out int gap, out string targetRowID, out TableGridMovePlacement placement)
        {
            targetRowID = null;
            placement = TableGridMovePlacement.Before;
            if (!viewport.TryFindRowGap(y, out gap))
            {
                return false;
            }

            if (movingContiguous && gap >= movingFirst && gap <= movingLast + 1)
            {
                return false;
            }

            if (TryFindRemainingRow(gap, 1, out targetRowID))
            {
                return true;
            }

            placement = TableGridMovePlacement.After;
            return TryFindRemainingRow(gap - 1, -1, out targetRowID);
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> start부터 step 방향으로 옮길 행이 아닌 첫 데이터 행을 찾는다.
        /// <br/> 그 전에 데이터 행이 아닌 행(그룹 머리·요약)을 만나면 false다.
        /// </summary>
        // ----------------------------------------------------------------------
        private bool TryFindRemainingRow(int start, int step, out string rowID)
        {
            for (var index = start; index >= 0 && index < model.Rows.RowCount; index += step)
            {
                if (!model.TryGetDataRowID(index, out rowID))
                {
                    break;
                }

                if (!movingRowSet.Contains(rowID))
                {
                    return true;
                }
            }

            rowID = null;
            return false;
        }

        private void HandleKeyDown(KeyDownEvent evt)
        {
            if ((!movingRows && !movingColumns) || evt.keyCode != KeyCode.Escape)
            {
                return;
            }

            EndDrag();
            evt.StopImmediatePropagation();
        }

        private void EndRowMove()
        {
            if (!movingRows)
            {
                return;
            }

            // 흐린 표시·삽입선은 문턱을 넘었을 때만 켰으므로 그때만 지운다.
            if (movingStarted)
            {
                viewport.ShowDropLine(-1);
                viewport.SetDraggingRows(null);
            }

            movingRows = false;
            movingStarted = false;
            movingRowIDs.Clear();
            movingRowSet.Clear();
        }

    #endregion

    #region 열 선택과 열 끌기

        // --------------------------------------------------------------------------------
        /// <summary>
        /// <br/> 헤더 칸 누름. 열을 고를 수 있는 표에서 왼쪽 버튼만 받는다.
        /// <br/> 그 밖의 표(List·Cell이 아닌 Workspace)는 넘겨 HeaderClicked(정렬)로 가게 한다.
        /// <br/> 고른 헤더를 Ctrl·Shift 없이 누르면 열 끌기를, 아니면 열 범위 선택을 시작한다.
        /// </summary>
        // --------------------------------------------------------------------------------
        private void HandleHeaderPointerDown(int columnIndex, PointerDownEvent evt)
        {
            var columns = model.Columns.VisibleColumns;
            if (evt.button != 0 || dragging || !options.AllowsColumnSelection || columnIndex >= columns.Count)
            {
                return;
            }

            owner.Focus();
            evt.StopPropagation();

            var columnID = columns[columnIndex].ID;
            var state = model.Selection;
            var selected = state.Current.Kind == TableGridSelectionKind.Column && state.IsColumnSelected(columnID);
            if (selected && !evt.shiftKey && !evt.actionKey)
            {
                BeginColumnMove(columnID, evt.position);
                BeginDrag(evt, false);
                return;
            }

            BeginColumnSelection(columnIndex, evt);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> 헤더 하나를 눌러 열 선택을 시작한다(행 선택 칸과 같은 규칙).
        /// <br/> Shift는 기준 열(열 선택이 아니면 활성 셀의 열)부터 잇고,
        /// <br/> Ctrl은 기존 열 선택에 더하거나 빼며, 끌면 그 범위로 넓힌다.
        /// <br/> 기준 열이 바뀌면 활성 행은 지금 활성 셀의 행을 잇는다.
        /// </summary>
        // ------------------------------------------------------------
        private void BeginColumnSelection(int columnIndex, PointerDownEvent evt)
        {
            var state = model.Selection;
            var current = state.Current;
            var isColumnSelection = current.Kind == TableGridSelectionKind.Column;
            var anchorIndex = isColumnSelection ? model.Columns.GetVisibleIndex(state.ColumnAnchorID) : state.ActiveColumn;
            var columnID = model.Columns.VisibleColumns[columnIndex].ID;
            columnDragBase.Clear();
            columnDragAdds = true;

            if (!evt.shiftKey || anchorIndex < 0)
            {
                if (evt.actionKey && isColumnSelection)
                {
                    columnDragBase.AddRange(current.ColumnIDs);
                    columnDragAdds = !state.IsColumnSelected(columnID);
                }

                anchorIndex = columnIndex;
            }

            // 열 선택을 새로 시작하거나 기준 열이 바뀌면 기준 열과 활성 행을 정한다(선택을 바꾸기 전에 활성 행을 읽는다).
            var anchorID = model.Columns.VisibleColumns[anchorIndex].ID;
            if (!isColumnSelection || !string.Equals(state.ColumnAnchorID, anchorID, StringComparison.Ordinal))
            {
                model.SetColumnAnchor(anchorID);
            }

            columnDragAnchor = anchorIndex;
            columnDragLast = columnIndex;
            ApplyColumnRange(columnIndex);
            BeginDrag(evt, false);
            draggingColumns = true;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> 끌기 기준 열부터 to 열까지의 범위를 열 선택에 반영한다.
        /// <br/> Ctrl로 시작했으면 시작 전 선택에 범위를 더하거나 뺀다.
        /// </summary>
        // ------------------------------------------------------------
        private void ApplyColumnRange(int to)
        {
            var columns = model.Columns.VisibleColumns;
            var range = new List<string>();
            var last = Math.Max(columnDragAnchor, to);
            for (var index = Math.Min(columnDragAnchor, to); index <= last; index++)
            {
                range.Add(columns[index].ID);
            }

            var columnIDs = MergeRange(columnDragBase, columnDragAdds, range);
            select(columnIDs.Count > 0 ? TableGridSelection.Columns(columnIDs) : TableGridSelection.None);
        }

        private void ExtendColumnsTo(float x)
        {
            if (!header.TryFindColumn(x, out var columnIndex) || columnIndex == columnDragLast)
            {
                return;
            }

            columnDragLast = columnIndex;
            ApplyColumnRange(columnIndex);
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> 헤더 끌기 중 포인터가 행 영역 왼쪽·오른쪽 밖이면 가로로 스크롤한다.
        /// <br/> 스크롤하면 포인터 아래 열·경계가 바뀌므로 범위나 삽입선을 다시 잇는다.
        /// </summary>
        // ----------------------------------------------------------------------
        private void TickHeaderDrag()
        {
            // 열 끌기는 문턱을 넘은 뒤에만 스크롤한다(그 전에는 클릭일 수 있다).
            if (movingColumns && !movingColumnsStarted)
            {
                return;
            }

            if (!viewport.AutoScrollHorizontal(lastPosition.x))
            {
                return;
            }

            if (movingColumns)
            {
                UpdateColumnMove(lastPosition);
            }
            else
            {
                ExtendColumnsTo(lastPosition.x);
            }
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> 고른 열을 옮길 열로 기억한다. 누른 열은 끌지 않고 놓으면 고를 열이다.
        /// <br/> 흐린 표시·삽입선은 문턱을 넘은 뒤 보인다.
        /// </summary>
        // ----------------------------------------------------------------------
        private void BeginColumnMove(string pressedColumnID, Vector2 position)
        {
            movingColumnSet.Clear();
            movingColumnSet.UnionWith(model.Selection.Current.ColumnIDs);
            movingColumns = true;
            movingColumnsStarted = false;
            movingColumnPressID = pressedColumnID;
            movingPressPosition = position;
        }

        private void UpdateColumnMove(Vector2 position)
        {
            if (!movingColumnsStarted)
            {
                if (Vector2.Distance(movingPressPosition, position) < MOVE_DRAG_THRESHOLD)
                {
                    return;
                }

                movingColumnsStarted = true;
                header.SetDraggingColumns(movingColumnSet);
            }

            var found = header.TryFindColumnGap(position.x, out var gap, out var frozen);
            header.ShowDropLine(found ? gap : -1, frozen);
        }

        private void DropColumns(Vector2 position)
        {
            // 끌기를 끝내면 옮길 열을 비우므로 알릴 값을 먼저 정한다.
            var started = movingColumnsStarted;
            var gap = -1;
            var frozen = false;
            var dropped = started && header.TryFindColumnGap(position.x, out gap, out frozen);
            var columnIDs = dropped ? new List<string>(movingColumnSet) : null;
            var pressedColumnID = movingColumnPressID;
            EndDrag();

            if (dropped)
            {
                ColumnsDropped?.Invoke(columnIDs, gap, frozen);
                return;
            }

            // 끌지 않고 놓았으면 누른 열 하나를 고른다.
            if (!started)
            {
                SelectSingleColumn(pressedColumnID);
            }
        }

        private void SelectSingleColumn(string columnID)
        {
            var columnIDs = new[]
            {
                columnID,
            };
            model.SetColumnAnchor(columnID);
            select(TableGridSelection.Columns(columnIDs));
        }

        private void EndColumnMove()
        {
            if (!movingColumns)
            {
                return;
            }

            // 흐린 표시·삽입선은 문턱을 넘었을 때만 켰으므로 그때만 지운다.
            if (movingColumnsStarted)
            {
                header.ShowDropLine(-1, false);
                header.SetDraggingColumns(null);
            }

            movingColumns = false;
            movingColumnsStarted = false;
            movingColumnPressID = null;
            movingColumnSet.Clear();
        }

    #endregion

    #region 끌기 범위 공용

        // --------------------------------------------------------------------------------
        /// <summary>
        /// <br/> 끌기 범위를 Ctrl로 시작하기 전 선택(baseIDs)에 더하거나 뺀다(행·열 공용).
        /// <br/> 시작 전 선택이 없으면 범위 그대로다.
        /// </summary>
        // --------------------------------------------------------------------------------
        private static List<string> MergeRange(List<string> baseIDs, bool adds, List<string> range)
        {
            if (baseIDs.Count == 0)
            {
                return range;
            }

            // 시작 전 선택 순서를 지키고, 더할 때는 범위의 새 ID를 뒤에 붙인다.
            var picked = new HashSet<string>(range, StringComparer.Ordinal);
            var kept = new HashSet<string>(StringComparer.Ordinal);
            var merged = new List<string>();
            foreach (var id in baseIDs)
            {
                if ((adds || !picked.Contains(id)) && kept.Add(id))
                {
                    merged.Add(id);
                }
            }

            if (adds)
            {
                foreach (var id in range)
                {
                    if (kept.Add(id))
                    {
                        merged.Add(id);
                    }
                }
            }

            return merged;
        }

    #endregion

    }
}
