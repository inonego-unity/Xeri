/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TableGridKeyboardInput.cs
수정일 : 2026-11-23

# 설명
키보드로 움직이는 선택. 방향키·Tab·Home/End·PageUp/Down·Ctrl+Home/End로 활성 셀을 옮기고,
Shift를 함께 누르면 범위를 늘린다. Escape는 잘라내기 표시를 지우거나 범위를 활성 셀 하나로 줄이고,
Shift+Space는 활성 행을 고른다. Selection이 Row·MultiRow이면 위/아래는 행을 옮기고(MultiRow는 Shift로 범위),
좌/우·Tab은 행 안의 활성 열을 옮긴다. 단 Paged인 행 선택 표에서 좌/우는 이전·다음 페이지를 요청한다.
None이면 선택을 옮기지 않는다.
열을 고를 수 있는 표(Workspace + Cell)에서 Ctrl+Space는 활성 셀의 열을 고르고, Ctrl+Alt+←/→는 고른 열을 한 칸 옮겨 달라고 알린다.
열 선택에서 방향키를 누르면 활성 셀에서 셀 선택으로 이어진다.
Ctrl(Cmd)+C·X·V·G, Enter·글자 입력(편집 시작), Delete·Backspace(지우기)는 요청으로 알리고 처리는 TableGrid가 한다.
편집 중의 Enter(확정)는 입력칸이 받으므로 Enter는 편집 시작과 확정을 오가는 토글처럼 동작한다.

# 특이사항, 제약사항
표 루트에 포커스가 있을 때만 처리한다. 표 안의 입력 필드에 포커스가 있으면 키를 가로채지 않는다.
처리한 키는 포커스 이동(Tab·방향키 내비게이션)이 일어나지 않게 막는다.
이동 규칙은 TableGridNavigator가 정하고, 옮긴 셀은 화면에 보이도록 스크롤한다.
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
    /// 키보드·메뉴의 행 이동 방향.
    /// </summary>
    // ============================================================
    internal enum TableGridRowMove
    {
        Up,
        Down,
        Top,
        Bottom
    }

    // ============================================================
    /// <summary>
    /// 키보드 선택 입력.
    /// </summary>
    // ============================================================
    internal sealed class TableGridKeyboardInput
    {

    #region 필드

        // ------------------------------------------------------------
        /// <summary>
        /// 빠른 이동을 여는 키(명령 키와 함께). 안내 표기도 이 값을 쓴다.
        /// </summary>
        // ------------------------------------------------------------
        public const KeyCode JUMP_KEY = KeyCode.G;

        private readonly VisualElement owner;
        private readonly TableGridModel model;
        private readonly TableGridViewport viewport;
        private readonly TableGridOptions options;
        private readonly Action<TableGridSelection> select;
        private readonly Func<bool> cancelCut;

    #endregion

    #region 이벤트

        // ------------------------------------------------------------
        /// <summary>
        /// Ctrl(Cmd)+C로 선택 복사를 요청했을 때 알린다.
        /// </summary>
        // ------------------------------------------------------------
        public event Action CopyRequested;

        // ------------------------------------------------------------
        /// <summary>
        /// Ctrl(Cmd)+G로 빠른 이동을 요청했을 때 알린다.
        /// </summary>
        // ------------------------------------------------------------
        public event Action JumpRequested;

        // ------------------------------------------------------------
        /// <summary>
        /// Ctrl(Cmd)+X로 잘라내기를 요청했을 때 알린다.
        /// </summary>
        // ------------------------------------------------------------
        public event Action CutRequested;

        // ------------------------------------------------------------
        /// <summary>
        /// Ctrl(Cmd)+V로 붙여넣기를 요청했을 때 알린다.
        /// </summary>
        // ------------------------------------------------------------
        public event Action PasteRequested;

        // ----------------------------------------------------------------------
        /// <summary>
        /// Delete·Backspace로 지우기(행 선택이면 행 삭제)를 요청했을 때 알린다.
        /// </summary>
        // ----------------------------------------------------------------------
        public event Action ClearRequested;

        // ----------------------------------------------------------------------
        /// <summary>
        /// Enter(null) 또는 글자 입력(그 글자)으로 셀 편집을 요청했을 때 알린다.
        /// </summary>
        // ----------------------------------------------------------------------
        public event Action<string> EditRequested;

        // ------------------------------------------------------------
        /// <summary>
        /// Alt+↑·↓·Home·End로 행 이동을 요청했을 때 알린다.
        /// </summary>
        // ------------------------------------------------------------
        public event Action<TableGridRowMove> RowMoveRequested;

        // --------------------------------------------------------------------------------
        /// <summary>
        /// Ctrl(Cmd)+Alt+←·→로 고른 열을 왼쪽(-1)·오른쪽(1)으로 옮겨 달라고 할 때 알린다.
        /// </summary>
        // --------------------------------------------------------------------------------
        public event Action<int> ColumnMoveRequested;

        // ----------------------------------------------------------------------
        /// <summary>
        /// Paged인 행 선택 표에서 ←·→로 이전(-1)·다음(1) 페이지를 요청할 때 알린다.
        /// </summary>
        // ----------------------------------------------------------------------
        public event Action<int> PageTurnRequested;

    #endregion

    #region 생성

        // ------------------------------------------------------------
        /// <summary>
        /// 표 루트를 포커스 가능하게 하고 키 입력을 연결한다.
        /// </summary>
        // ------------------------------------------------------------
        public TableGridKeyboardInput
        (
            VisualElement owner, TableGridModel model, TableGridViewport viewport,
            TableGridOptions options, Action<TableGridSelection> select, Func<bool> cancelCut
        )
        {
            this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
            this.model = model ?? throw new ArgumentNullException(nameof(model));
            this.viewport = viewport ?? throw new ArgumentNullException(nameof(viewport));
            this.options = options ?? throw new ArgumentNullException(nameof(options));
            this.select = select ?? throw new ArgumentNullException(nameof(select));
            this.cancelCut = cancelCut ?? throw new ArgumentNullException(nameof(cancelCut));

            owner.focusable = true;
            owner.RegisterCallback<KeyDownEvent>(HandleKeyDown);
            owner.RegisterCallback<NavigationMoveEvent>(HandleNavigationMove);
        }

    #endregion

    #region 키 처리

        private void HandleKeyDown(KeyDownEvent evt)
        {
            if (evt.target != owner)
            {
                return;
            }

            // 글자 키는 keyCode 없이 글자만 담긴 KeyDown으로 한 번 더 온다. 그 글자로 편집을 시작한다.
            if (evt.keyCode == KeyCode.None)
            {
                var character = evt.character;
                if (!evt.actionKey && !char.IsControl(character) && !char.IsWhiteSpace(character))
                {
                    EditRequested?.Invoke(character.ToString());
                    evt.StopPropagation();
                }

                return;
            }

            if (evt.altKey && evt.actionKey && options.AllowsColumnSelection && HandleColumnMoveKey(evt.keyCode))
            {
                evt.StopPropagation();
                return;
            }

            if (evt.altKey && HandleRowMoveKey(evt.keyCode))
            {
                evt.StopPropagation();
                return;
            }

            if (HandleKey(evt.keyCode, evt.shiftKey, evt.actionKey))
            {
                evt.StopPropagation();
            }
        }

        private void HandleNavigationMove(NavigationMoveEvent evt)
        {
            // Tab·방향키로 포커스가 표 밖으로 나가지 않게 한다. 실제 이동은 KeyDown에서 처리한다.
            if (evt.target == owner)
            {
                evt.StopPropagation();
                owner.focusController?.IgnoreEvent(evt);
            }
        }

        private bool HandleRowMoveKey(KeyCode keyCode)
        {
            TableGridRowMove move;
            switch (keyCode)
            {
                case KeyCode.UpArrow:
                    move = TableGridRowMove.Up;
                    break;
                case KeyCode.DownArrow:
                    move = TableGridRowMove.Down;
                    break;
                case KeyCode.Home:
                    move = TableGridRowMove.Top;
                    break;
                case KeyCode.End:
                    move = TableGridRowMove.Bottom;
                    break;
                default:
                    return false;
            }

            RowMoveRequested?.Invoke(move);
            return true;
        }

        private bool HandleColumnMoveKey(KeyCode keyCode)
        {
            switch (keyCode)
            {
                case KeyCode.LeftArrow:
                    ColumnMoveRequested?.Invoke(-1);
                    return true;
                case KeyCode.RightArrow:
                    ColumnMoveRequested?.Invoke(1);
                    return true;
                default:
                    return false;
            }
        }

        private bool HandleKey(KeyCode keyCode, bool shift, bool action)
        {
            switch (keyCode)
            {
                case KeyCode.UpArrow:
                    return Move(TableGridMove.Up, shift);
                case KeyCode.DownArrow:
                    return Move(TableGridMove.Down, shift);
                case KeyCode.LeftArrow:
                    return TurnPage(-1, shift) || Move(TableGridMove.Left, shift);
                case KeyCode.RightArrow:
                    return TurnPage(1, shift) || Move(TableGridMove.Right, shift);
                case KeyCode.Tab:
                    return Move(shift ? TableGridMove.Previous : TableGridMove.Next, false);
                case KeyCode.Home:
                    return Move(action ? TableGridMove.First : TableGridMove.RowStart, shift);
                case KeyCode.End:
                    return Move(action ? TableGridMove.Last : TableGridMove.RowEnd, shift);
                case KeyCode.PageUp:
                    return Move(TableGridMove.PageUp, shift);
                case KeyCode.PageDown:
                    return Move(TableGridMove.PageDown, shift);
                case KeyCode.Escape:
                    // 잘라내기 표시가 있으면 Escape는 먼저 그 표시를 지운다.
                    return cancelCut() || CollapseRange();
                case KeyCode.Space:
                    return action ? SelectActiveColumn() : shift && SelectActiveRow();
                case KeyCode.C when action:
                    CopyRequested?.Invoke();
                    return true;
                case KeyCode.X when action:
                    CutRequested?.Invoke();
                    return true;
                case KeyCode.V when action:
                    PasteRequested?.Invoke();
                    return true;
                case JUMP_KEY when action:
                    JumpRequested?.Invoke();
                    return true;
                case var key when XeriShortcutKeys.IsSubmit(key):
                    EditRequested?.Invoke(null);
                    return true;
                case KeyCode.Delete:
                case KeyCode.Backspace:
                    ClearRequested?.Invoke();
                    return true;
                default:
                    return false;
            }
        }

        // --------------------------------------------------------------------------------
        /// <summary>
        /// <br/> 표 밖(호스트의 검색창 등)에서 받은 이동 키를 표 루트에서 받은 것처럼 처리한다.
        /// <br/> 방향키·PageUp/Down·Home/End만 받는다. 처리했으면 true다.
        /// </summary>
        // --------------------------------------------------------------------------------
        public bool HandleForwardedKey(KeyCode keyCode, bool shift)
        {
            switch (keyCode)
            {
                case KeyCode.UpArrow:
                case KeyCode.DownArrow:
                case KeyCode.LeftArrow:
                case KeyCode.RightArrow:
                case KeyCode.PageUp:
                case KeyCode.PageDown:
                case KeyCode.Home:
                case KeyCode.End:
                    return HandleKey(keyCode, shift, false);
                default:
                    return false;
            }
        }

        // Paged인 행 선택 표의 좌우 키는 열 대신 페이지를 넘긴다(행 선택은 활성 열을 쓰지 않는다).
        private bool TurnPage(int step, bool shift)
        {
            if (shift || !options.IsPaged || options.Selection == TableGridSelectionMode.Cell
                || options.Selection == TableGridSelectionMode.None)
            {
                return false;
            }

            PageTurnRequested?.Invoke(step);
            return true;
        }

    #endregion

    #region 이동

        // ------------------------------------------------------------
        /// <summary>
        /// 활성 셀을 옮긴다. 편집을 확정한 뒤 다음 셀로 갈 때 쓴다.
        /// </summary>
        // ------------------------------------------------------------
        public void MoveActive(TableGridMove move)
        {
            Move(move, false);
        }

        private bool Move(TableGridMove move, bool extend)
        {
            if (options.Selection == TableGridSelectionMode.None)
            {
                return false;
            }

            if (!TryGetActiveCell(out var rowIndex, out var columnIndex))
            {
                return SelectFirstVisibleCell();
            }

            var columnCount = model.Columns.VisibleColumns.Count;
            var moved = TableGridNavigator.TryMove
            (
                model.Rows, columnCount, rowIndex, columnIndex, move, viewport.PageRowCount,
                out var targetRow, out var targetColumn
            );
            if (!moved)
            {
                // 더 갈 곳이 없어도 키는 표가 처리한 것으로 본다(Tab으로 포커스가 빠지지 않게).
                return true;
            }

            if (!model.TryGetCellAddress(targetRow, targetColumn, out var address))
            {
                return true;
            }

            if (options.Selection != TableGridSelectionMode.Cell)
            {
                MoveRowSelection(targetRow, address, extend);
                viewport.ScrollIntoView(targetRow, targetColumn);
                return true;
            }

            var current = model.Selection.Current;
            var anchor = extend && options.AllowsCellRange && current.Kind == TableGridSelectionKind.Cell
                ? current.Anchor
                : address;
            select(TableGridSelection.CellRange(anchor, address));
            viewport.ScrollIntoView(targetRow, targetColumn);
            return true;
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> Row·MultiRow 표에서 행 선택을 옮긴다. 활성 열은 이동한 셀의 열이다.
        /// <br/> MultiRow에서 Shift이면 기준 행을 두고 그 행까지 범위로 고른다.
        /// </summary>
        // ----------------------------------------------------------------------
        private void MoveRowSelection(int targetRow, TableGridCellAddress address, bool extend)
        {
            var state = model.Selection;
            state.RowActiveColumnID = address.ColumnID;
            var anchorIndex = model.Rows.IndexOf(state.RowAnchorID);
            List<string> rowIDs;
            if (extend && options.Selection == TableGridSelectionMode.MultiRow && anchorIndex >= 0)
            {
                rowIDs = model.ToRowIDs(model.CollectDataRowPositions(anchorIndex, targetRow));
            }
            else
            {
                state.RowAnchorID = address.RowID;
                rowIDs = new List<string>
                {
                    address.RowID,
                };
            }

            // 같은 행에서 열만 옮겨도 활성 셀 표시가 바뀌도록 새 선택으로 알린다.
            select(TableGridSelection.Rows(rowIDs));
        }

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> 키보드 이동의 출발 셀을 찾는다.
        /// <br/> 셀·열 선택이면 활성 셀, 행 선택이면 기준 행의 첫 열이다.
        /// </summary>
        // ------------------------------------------------------------
        private bool TryGetActiveCell(out int rowIndex, out int columnIndex)
        {
            var state = model.Selection;
            var current = state.Current;
            if (current.Kind == TableGridSelectionKind.Cell)
            {
                rowIndex = model.Rows.IndexOf(current.Focus.RowID);
                columnIndex = model.Columns.GetVisibleIndex(current.Focus.ColumnID);
                return rowIndex >= 0 && columnIndex >= 0;
            }

            if (current.Kind == TableGridSelectionKind.Column)
            {
                rowIndex = state.ActiveRow;
                columnIndex = state.ActiveColumn;
                return rowIndex >= 0 && columnIndex >= 0;
            }

            rowIndex = current.Kind == TableGridSelectionKind.Row ? model.Rows.IndexOf(state.RowAnchorID) : -1;
            columnIndex = string.IsNullOrEmpty(state.RowActiveColumnID) ? 0 : Math.Max(0, model.Columns.GetVisibleIndex(state.RowActiveColumnID));
            return rowIndex >= 0 && model.Columns.VisibleColumns.Count > 0;
        }

        private bool SelectFirstVisibleCell()
        {
            // 선택이 없으면 화면 맨 위의 첫 데이터 행 첫 셀에서 시작한다.
            var first = Math.Max(0, viewport.FirstVisibleRow);
            var last = Math.Min(model.Rows.RowCount, first + viewport.PageRowCount + 1);
            for (var index = first; index < last; index++)
            {
                if (model.TryGetCellAddress(index, 0, out var address))
                {
                    if (options.Selection != TableGridSelectionMode.Cell)
                    {
                        MoveRowSelection(index, address, false);
                        viewport.ScrollIntoView(index, 0);
                        return true;
                    }

                    select(TableGridSelection.SingleCell(address));
                    viewport.ScrollIntoView(index, 0);
                    return true;
                }
            }

            return true;
        }

        private bool CollapseRange()
        {
            var current = model.Selection.Current;
            if (current.Kind != TableGridSelectionKind.Cell || current.Anchor == current.Focus)
            {
                return false;
            }

            select(TableGridSelection.SingleCell(current.Focus));
            return true;
        }

        private bool SelectActiveRow()
        {
            if (!options.AllowsRowSelection || !TryGetActiveCell(out var rowIndex, out _)
                || !model.TryGetDataRowID(rowIndex, out var rowID))
            {
                return false;
            }

            var rowIDs = new[]
            {
                rowID,
            };
            model.Selection.RowAnchorID = rowID;
            select(TableGridSelection.Rows(rowIDs));
            return true;
        }

        private bool SelectActiveColumn()
        {
            if (!options.AllowsColumnSelection || !TryGetActiveCell(out _, out var columnIndex))
            {
                return false;
            }

            // 활성 셀의 열 하나를 고른다. 활성 행은 그 셀의 행을 잇는다.
            var columnIDs = new[]
            {
                model.Columns.VisibleColumns[columnIndex].ID,
            };
            model.SetColumnAnchor(columnIDs[0]);
            select(TableGridSelection.Columns(columnIDs));
            return true;
        }

    #endregion

    }
}
