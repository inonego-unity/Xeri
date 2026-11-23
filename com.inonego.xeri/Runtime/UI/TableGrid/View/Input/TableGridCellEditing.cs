/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TableGridCellEditing.cs
수정일 : 2026-11-23

# 설명
활성 셀 편집 처리기. 열의 편집 방식(TableGridCellEdit)에 따라 편집을 시작한다.
- Editor: 열이 정한 편집기(지정·등록·기본 입력칸)를 셀 안에 놓는다. 편집기가 없는 선택형 열은
  Xeri Core 컨텍스트 메뉴로 값을 고른다(현재 값 체크). Enter는 확정하고 그 셀에 머문다(Enter로 편집을 시작하므로 토글).
  Tab·Shift+Tab은 확정 후 옆으로 옮기고, Escape는 취소한다. 편집기 밖으로 포커스가 나가면 확정한다.
- Inline: Enter·더블클릭이면 셀 안 첫 컨트롤에 포커스를 준다. 글자 입력으로는 시작하지 않는다.
- Detail: Enter·더블클릭이면 상세 편집을 요청한다(DetailRequested). 글자 입력으로는 시작하지 않는다.

# 특이사항, 제약사항
확정한 값은 EditCommitted로만 알리고 데이터는 바꾸지 않는다(호스트가 반영한다). 값이 그대로면 알리지 않는다.
편집기 값은 TryGetValue로 읽고 값 규칙으로 검증한다. 읽지 못하거나 틀린 값이면 편집기를 열어 둔 채 오류를 보인다.
편집기 위치(Placement)는 지금 InCell만 구현했다. Popup 편집기도 셀 안에 놓는다.
편집 중인 셀은 행·열 ID로 기억한다(TableGridEditState). 스크롤하면 행 요소가 다른 행에 다시 쓰이므로 TableGrid가 확정한다.
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
    /// 활성 셀 편집 처리기.
    /// </summary>
    // ============================================================
    internal sealed class TableGridCellEditing
    {

    #region 필드

        private const string INVALID_CLASS = "xeri-table__editor--invalid";

        private readonly VisualElement owner;
        private readonly TableGridModel model;
        private readonly TableGridViewport viewport;
        private readonly TableGridOptions options;
        private readonly TableGridCellCommands commands;
        private readonly XeriContextMenu choiceMenu;

        private VisualElement editorElement;
        private TableGridCellEditor editor;
        private TableGridColumn column;
        private bool closing;

        // ------------------------------------------------------------
        /// <summary>
        /// 셀 안 편집기로 편집 중인지 여부.
        /// </summary>
        // ------------------------------------------------------------
        public bool IsEditing => editorElement != null;

    #endregion

    #region 이벤트

        // ------------------------------------------------------------
        /// <summary>
        /// 값이 바뀐 편집을 확정했을 때 변경을 알린다.
        /// </summary>
        // ------------------------------------------------------------
        public event Action<TableGridEditIntent> EditCommitted;

        // ------------------------------------------------------------
        /// <summary>
        /// 편집이 끝났을 때 다음에 옮길 방향을 알린다. 옮기지 않으면 null이다.
        /// </summary>
        // ------------------------------------------------------------
        public event Action<TableGridMove?> Finished;

    #endregion

    #region 생성

        // ------------------------------------------------------------
        /// <summary>
        /// 편집에 쓸 표 요소와 상태를 연결한다.
        /// </summary>
        // ------------------------------------------------------------
        public TableGridCellEditing
        (
            VisualElement owner, TableGridModel model, TableGridViewport viewport, TableGridOptions options,
            TableGridCellCommands commands
        )
        {
            this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
            this.model = model ?? throw new ArgumentNullException(nameof(model));
            this.viewport = viewport ?? throw new ArgumentNullException(nameof(viewport));
            this.options = options ?? throw new ArgumentNullException(nameof(options));
            this.commands = commands ?? throw new ArgumentNullException(nameof(commands));
            choiceMenu = new XeriContextMenu(owner, null, XeriContextMenuStyle.Compact);
        }

    #endregion

    #region 시작

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> 활성 셀 편집을 시작한다. 편집할 수 없으면 false다.
        /// <br/> replacement가 있으면 글자를 쳐서 시작한 경우다.
        /// <br/> Editor 열만 그 글자로 편집기를 연다.
        /// </summary>
        // ------------------------------------------------------------
        public bool Begin(string replacement)
        {
            var selection = model.Selection;
            var rowIndex = selection.ActiveRow;
            var columnIndex = selection.ActiveColumn;
            if (!options.IsEditable || !model.TryGetCell(rowIndex, columnIndex, out var target, out var row) || !commands.CanEdit(target))
            {
                return false;
            }

            switch (target.Edit.Kind)
            {
                case TableGridCellEditKind.Editor:
                    return BeginEditor(rowIndex, columnIndex, target, row, replacement);
                case TableGridCellEditKind.Inline:
                    // 셀 안 컨트롤이 편집기이므로 글자 입력으로는 시작하지 않고, Enter는 첫 컨트롤로 간다.
                    if (replacement != null)
                    {
                        return false;
                    }

                    viewport.ScrollIntoView(rowIndex, columnIndex);
                    return viewport.FocusCellControl(rowIndex, columnIndex);
                case TableGridCellEditKind.Detail:
                    return replacement == null && commands.RequestDetail(row.ID, target);
                default:
                    return false;
            }
        }

        private bool BeginEditor(int rowIndex, int columnIndex, TableGridColumn target, TableGridRow row, string replacement)
        {
            Cancel(false);
            viewport.ScrollIntoView(rowIndex, columnIndex);
            var address = new TableGridCellAddress(row.ID, target.ID);
            target.TryGetValue(row.Item, out var value);

            // 편집기가 없는 선택형 열은 선택 메뉴로 고른다. 글자도 못 읽으면 편집할 수 없다.
            var cellEditor = target.ResolveEditor();
            if (cellEditor == null)
            {
                var isChoice = target.ValueKind == TableGridColumnValueKind.Choice && target.Choices.Count > 0;
                return isChoice && OpenChoiceMenu(rowIndex, columnIndex, address, target, row.Item, value);
            }

            var context = new TableGridCellContext(row.ID, rowIndex, row.Item, target, true, null);
            var element = cellEditor.BeginBoxed(value, in context, replacement);
            if (element == null)
            {
                return false;
            }

            element.AddToClassList("xeri-table__editor");
            element.RegisterCallback<KeyDownEvent>(HandleKeyDown, TrickleDown.TrickleDown);
            element.RegisterCallback<NavigationMoveEvent>(HandleNavigationMove, TrickleDown.TrickleDown);
            element.RegisterCallback<FocusOutEvent>(HandleFocusOut);
            element.RegisterCallback<ChangeEvent<string>>(_ => MarkInvalid(false));

            if (!viewport.AttachEditor(rowIndex, columnIndex, element))
            {
                cellEditor.EndBoxed(element);
                return false;
            }

            editorElement = element;
            editor = cellEditor;
            column = target;
            model.Edit.Begin(address, value);
            FocusEditor(element, replacement);
            return true;
        }

        private static void FocusEditor(VisualElement element, string replacement)
        {
            var focusTarget = TableGridRowView.FindFocusable(element) ?? element;
            focusTarget.Focus();
            if (!(element is TextField field))
            {
                return;
            }

            // Enter·더블클릭이면 글자를 모두 고르고, 글자를 쳐서 시작했으면 끝에 커서를 둔다.
            var length = field.value?.Length ?? 0;
            if (replacement == null)
            {
                field.SelectAll();
            }
            else
            {
                field.SelectRange(length, length);
            }
        }

        private bool OpenChoiceMenu
        (
            int rowIndex, int columnIndex, TableGridCellAddress address, TableGridColumn target, object item, object original
        )
        {
            if (!viewport.TryGetCellBounds(rowIndex, columnIndex, out var bounds))
            {
                return false;
            }

            var current = target.GetText(item);
            var items = new List<XeriContextMenuItem>();
            foreach (var choice in target.Choices)
            {
                var picked = choice;
                var isCurrent = string.Equals(choice, current, StringComparison.Ordinal);
                // 선택지는 호스트가 정한 글자라 '/'가 있어도 하위 메뉴 경로로 풀지 않는다.
                items.Add(XeriContextMenuItem.Command
                (
                    new[] { choice }, () => CommitChoice(address, target, original, picked), isChecked: isCurrent
                ));
            }

            if (!string.IsNullOrEmpty(current) && target.Format.CanClear)
            {
                items.Add(XeriContextMenuItem.Separator());
                items.Add(XeriContextMenuItem.Command("값 비우기", () => CommitValue(address, original, target.ClearValue)));
            }

            choiceMenu.Open(new Vector2(bounds.xMin, bounds.yMax), items);
            return true;
        }

        private void CommitChoice(TableGridCellAddress address, TableGridColumn target, object original, string picked)
        {
            // 후보 글자를 값 규칙으로 읽어 열 값 형식으로 요청한다.
            if (!target.TryParseText(picked, out var value, out var error))
            {
                commands.ShowError(error);
                Finished?.Invoke(null);
                return;
            }

            CommitValue(address, original, value);
        }

        private void CommitValue(TableGridCellAddress address, object original, object value)
        {
            if (!Equals(original, value))
            {
                EditCommitted?.Invoke(new TableGridEditIntent(address.RowID, address.ColumnID, value));
            }

            Finished?.Invoke(null);
        }

    #endregion

    #region 확정과 취소

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> 편집기의 값을 확정하고 next 방향으로 옮기게 알린다.
        /// <br/> 값을 읽지 못하거나 검증에 실패하면 확정하지 않고 false다.
        /// </summary>
        // ------------------------------------------------------------
        public bool Commit(TableGridMove? next)
        {
            if (editorElement == null || closing)
            {
                return true;
            }

            if (!editor.TryGetValueBoxed(editorElement, out var value, out var error) || !column.TryValidateValue(value, out error))
            {
                MarkInvalid(true);
                commands.ShowError(error);
                (TableGridRowView.FindFocusable(editorElement) ?? editorElement).Focus();
                return false;
            }

            var address = model.Edit.Editing;
            var original = model.Edit.OriginalValue;
            Close();

            if (address.HasValue && !Equals(original, value))
            {
                EditCommitted?.Invoke(new TableGridEditIntent(address.Value.RowID, address.Value.ColumnID, value));
            }

            Finished?.Invoke(next);
            return true;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 입력을 버리고 편집을 끝낸다. notify면 끝났음을 알린다.
        /// </summary>
        // ------------------------------------------------------------
        public void Cancel(bool notify)
        {
            choiceMenu.Close();
            if (editorElement == null)
            {
                return;
            }

            Close();
            if (notify)
            {
                Finished?.Invoke(null);
            }
        }

        private void Close()
        {
            // 편집기를 떼면 포커스가 나가 FocusOut이 다시 오므로 그동안은 확정하지 않는다.
            closing = true;
            var element = editorElement;
            var endedEditor = editor;
            viewport.DetachEditor();
            editorElement = null;
            editor = null;
            column = null;
            model.Edit.End();
            endedEditor?.EndBoxed(element);
            closing = false;
        }

        private void MarkInvalid(bool invalid)
        {
            editorElement?.EnableInClassList(INVALID_CLASS, invalid);
        }

    #endregion

    #region 편집기 이벤트

        private void HandleKeyDown(KeyDownEvent evt)
        {
            switch (evt.keyCode)
            {
                case KeyCode.Escape:
                    Cancel(true);
                    break;
                case var key when XeriShortcutKeys.IsSubmit(key):
                    // Enter는 편집 시작과 짝을 이루는 토글이라 확정만 하고 그 셀에 머문다.
                    Commit(null);
                    break;
                case KeyCode.Tab:
                    Commit(evt.shiftKey ? TableGridMove.Previous : TableGridMove.Next);
                    break;
                default:
                    // 글자 입력은 편집기 안쪽 요소가 받아야 하므로 내려가는 단계에서 막지 않는다.
                    // 표의 키 처리는 대상이 표 자신일 때만 동작하므로 따로 막을 필요가 없다.
                    return;
            }

            evt.StopPropagation();
        }

        private void HandleNavigationMove(NavigationMoveEvent evt)
        {
            // Tab·방향키로 포커스가 편집기 밖으로 옮겨지지 않게 한다. Tab은 KeyDown에서 처리한다.
            evt.StopPropagation();
            owner.focusController?.IgnoreEvent(evt);
        }

        private void HandleFocusOut(FocusOutEvent evt)
        {
            if (closing || editorElement == null)
            {
                return;
            }

            // 편집기 안에서 포커스가 옮겨 간 경우는 무시한다.
            for (var next = evt.relatedTarget as VisualElement; next != null; next = next.parent)
            {
                if (next == editorElement)
                {
                    return;
                }
            }

            if (!Commit(null))
            {
                // 틀린 값으로 포커스를 잃으면 확정할 수 없으므로 입력을 버린다.
                Cancel(true);
            }
        }

    #endregion

    }
}
