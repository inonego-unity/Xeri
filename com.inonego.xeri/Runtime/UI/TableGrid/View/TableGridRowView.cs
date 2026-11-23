/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TableGridRowView.cs
수정일 : 2026-11-23

# 설명
가상화된 행 하나의 화면 요소. 고정 영역 쪽(끌기 손잡이 + 행 선택 칸 + 고정 열)과 스크롤 영역 쪽(나머지 열) 두 부분으로 나뉜다.
행 안의 셀은 flex로 나란히 놓고 폭만 지정한다. 행의 세로 위치만 절대 좌표다.
셀 틀 안의 내용은 열(값 규칙·셀 뷰)이 만들고 연결하며, 셀마다 셀 호스트(Inline 값 변경·상세 편집 요청)를 둔다.

# 특이사항, 제약사항
행 요소는 풀에서 재사용하며 열 구성이 바뀔 때만 셀을 다시 만든다.
선택·활성·호버 같은 시각 상태는 USS 클래스로만 표현하고 색·여백은 USS에 둔다.
그룹 머리 행은 셀을 숨기고 제목(행 Title)을 스크롤 영역의 보이는 왼쪽에 둔다.
Inline 열 셀 안 컨트롤을 누르면 표는 셀만 고르고 포인터·포커스를 빼앗지 않는다(InlinePointerDown).
Inline(Confirm)은 Enter·셀 밖으로 포커스 이탈 때 보류한 값을 요청하고, Escape는 보류 값을 버리고 표로 포커스를 돌린다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;

using UnityEngine;
using UnityEngine.UIElements;

namespace inonego.Xeri.UI.TableGrid
{
    // ============================================================
    /// <summary>
    /// 가상화된 행 하나의 화면 요소.
    /// </summary>
    // ============================================================
    internal sealed class TableGridRowView
    {

    #region 내부 타입

        // ============================================================
        /// <summary>
        /// 셀 하나의 틀과 내용, 셀 호스트.
        /// </summary>
        // ============================================================
        private sealed class CellSlot : ITableGridCellHost
        {
            private readonly TableGridRowView owner;
            private object pending;

            // ------------------------------------------------------------
            /// <summary>
            /// 행 요소에 속한 셀을 만든다.
            /// </summary>
            // ------------------------------------------------------------
            public CellSlot(TableGridRowView owner, TableGridColumn column, VisualElement root, VisualElement content)
            {
                this.owner = owner;
                Column = column;
                Root = root;
                Content = content;
            }

            // ------------------------------------------------------------
            /// <summary>
            /// 셀 틀 요소.
            /// </summary>
            // ------------------------------------------------------------
            public VisualElement Root { get; }

            // ------------------------------------------------------------
            /// <summary>
            /// 열이 만든 내용 요소.
            /// </summary>
            // ------------------------------------------------------------
            public VisualElement Content { get; }

            // ------------------------------------------------------------
            /// <summary>
            /// 셀이 속한 열.
            /// </summary>
            // ------------------------------------------------------------
            public TableGridColumn Column { get; }

            // ------------------------------------------------------------
            /// <summary>
            /// 셀의 현재 선택 표시 상태.
            /// </summary>
            // ------------------------------------------------------------
            public TableGridCellMarks Marks { get; set; }

            // ------------------------------------------------------------
            /// <summary>
            /// Inline(Confirm)에서 보류한 값이 있는지 여부.
            /// </summary>
            // ------------------------------------------------------------
            public bool HasPending => hasPending;
            private bool hasPending;

            // ------------------------------------------------------------
            /// <summary>
            /// 셀 값 변경을 요청하거나(Immediate) 보류한다(Confirm).
            /// </summary>
            // ------------------------------------------------------------
            public void Replace(object value)
            {
                if (owner.DataRowID == null)
                {
                    return;
                }

                if (Column.Edit.InlineCommit == TableGridInlineCommit.Confirm)
                {
                    pending = value;
                    hasPending = true;
                    return;
                }

                owner.commands.RequestInline(owner.DataRowID, Column, value);
            }

            // ------------------------------------------------------------
            /// <summary>
            /// 보류한 값을 요청한다. 없으면 하지 않는다.
            /// </summary>
            // ------------------------------------------------------------
            public void Commit()
            {
                if (!hasPending)
                {
                    return;
                }

                var value = pending;
                Discard();
                owner.commands.RequestInline(owner.DataRowID, Column, value);
            }

            // ------------------------------------------------------------
            /// <summary>
            /// 셀 밖 상세 편집을 요청한다.
            /// </summary>
            // ------------------------------------------------------------
            public void OpenDetail()
            {
                owner.commands.RequestDetail(owner.DataRowID, Column);
            }

            // ------------------------------------------------------------
            /// <summary>
            /// 보류한 값을 버린다.
            /// </summary>
            // ------------------------------------------------------------
            public void Discard()
            {
                hasPending = false;
                pending = null;
            }
        }

    #endregion

    #region 필드

        private static readonly string[] numberTexts = new string[2048];

        private readonly List<CellSlot> cells = new();
        private readonly TableGridCellCommands commands;
        private readonly VisualElement handle;
        private readonly VisualElement selector;
        private readonly Label selectorNumber;
        private readonly Label groupLabel;
        private int editorColumn = -1;
        private StyleEnum<DisplayStyle> editorContentDisplay;
        private bool stale;

        // ------------------------------------------------------------
        /// <summary>
        /// 고정 영역에 놓이는 부분(끌기 손잡이 + 행 선택 칸 + 고정 열).
        /// </summary>
        // ------------------------------------------------------------
        public VisualElement FrozenPart { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 스크롤 영역에 놓이는 부분(나머지 열).
        /// </summary>
        // ------------------------------------------------------------
        public VisualElement ScrollPart { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 연결된 행의 위치. 연결이 없으면 -1이다.
        /// </summary>
        // ------------------------------------------------------------
        public int Index { get; private set; } = -1;

        // ------------------------------------------------------------
        /// <summary>
        /// 연결된 행. 연결이 없으면 null이다.
        /// </summary>
        // ------------------------------------------------------------
        public TableGridRow Row { get; private set; }

        // ------------------------------------------------------------
        /// <summary>
        /// 셀 수. 표시 열 수와 같다.
        /// </summary>
        // ------------------------------------------------------------
        public int CellCount => cells.Count;

        private string DataRowID => Row != null && Row.IsData ? Row.ID : null;

    #endregion

    #region 이벤트

        // ------------------------------------------------------------
        /// <summary>
        /// 셀을 눌렀을 때 행 요소와 표시 열 위치를 알린다.
        /// </summary>
        // ------------------------------------------------------------
        public event Action<TableGridRowView, int, PointerDownEvent> CellPointerDown;

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> Inline 셀 안 컨트롤을 눌렀을 때 알린다.
        /// <br/> 표는 셀만 고르고 포인터·포커스는 컨트롤에 남긴다.
        /// </summary>
        // ------------------------------------------------------------
        public event Action<TableGridRowView, int, PointerDownEvent> InlinePointerDown;

        // ------------------------------------------------------------
        /// <summary>
        /// 행 선택 칸을 눌렀을 때 알린다.
        /// </summary>
        // ------------------------------------------------------------
        public event Action<TableGridRowView, PointerDownEvent> SelectorPointerDown;

        // ------------------------------------------------------------
        /// <summary>
        /// 끌기 손잡이를 눌렀을 때 알린다.
        /// </summary>
        // ------------------------------------------------------------
        public event Action<TableGridRowView, PointerDownEvent> ReorderHandlePointerDown;

    #endregion

    #region 생성

        // ------------------------------------------------------------
        /// <summary>
        /// 행의 두 부분과 끌기 손잡이·행 선택 칸을 만든다.
        /// </summary>
        // ------------------------------------------------------------
        public TableGridRowView(TableGridCellCommands commands)
        {
            this.commands = commands ?? throw new ArgumentNullException(nameof(commands));
            FrozenPart = CreatePart();
            ScrollPart = CreatePart();

            // 끌기 손잡이는 고정 영역 맨 앞, 행 선택 칸 앞에 둔다.
            handle = new VisualElement();
            handle.AddToClassList("xeri-table__handle");
            var grip = new VisualElement
            {
                pickingMode = PickingMode.Ignore,
            };
            grip.AddToClassList("xeri-table__handle-icon");
            handle.Add(grip);
            FrozenPart.Add(handle);

            // 행 선택 칸은 Excel의 행 머리처럼 행 번호를 보인다. 누르거나 끌면 행을 고른다(눌림은 칸이 받는다).
            selector = new VisualElement();
            selector.AddToClassList("xeri-table__selector");
            selectorNumber = new Label
            {
                pickingMode = PickingMode.Ignore,
            };
            selectorNumber.AddToClassList("xeri-table__selector-number");
            selector.Add(selectorNumber);
            FrozenPart.Add(selector);

            groupLabel = new Label();
            groupLabel.AddToClassList("xeri-table__group-title");
            groupLabel.pickingMode = PickingMode.Ignore;
            ScrollPart.Add(groupLabel);

            // 두 부분이 다른 영역에 있으므로 호버를 함께 표시한다.
            RegisterHover(FrozenPart);
            RegisterHover(ScrollPart);

            // Inline 컨트롤은 컨트롤이 누름을 멈춰도 셀을 고를 수 있게 내려가는 단계에서 본다.
            FrozenPart.RegisterCallback<PointerDownEvent>(HandleInlinePointerDown, TrickleDown.TrickleDown);
            ScrollPart.RegisterCallback<PointerDownEvent>(HandleInlinePointerDown, TrickleDown.TrickleDown);
            FrozenPart.RegisterCallback<PointerDownEvent>(HandlePointerDown);
            ScrollPart.RegisterCallback<PointerDownEvent>(HandlePointerDown);
        }

        private void HandlePointerDown(PointerDownEvent evt)
        {
            if (Index < 0)
            {
                return;
            }

            // 눌린 요소에서 행 부분까지 올라가며 끌기 손잡이·행 선택 칸인지, 어느 셀인지 찾는다.
            for (var element = evt.target as VisualElement; element != null; element = element.parent)
            {
                if (element == FrozenPart || element == ScrollPart)
                {
                    return;
                }

                if (element == handle)
                {
                    ReorderHandlePointerDown?.Invoke(this, evt);
                    return;
                }

                if (element == selector)
                {
                    SelectorPointerDown?.Invoke(this, evt);
                    return;
                }

                var column = FindCell(element);
                if (column >= 0)
                {
                    // Inline 컨트롤 누름은 내려가는 단계에서 처리했다.
                    if (!IsInlineControl(column, evt.target as VisualElement))
                    {
                        CellPointerDown?.Invoke(this, column, evt);
                    }

                    return;
                }
            }
        }

        private void HandleInlinePointerDown(PointerDownEvent evt)
        {
            if (Index < 0)
            {
                return;
            }

            var target = evt.target as VisualElement;
            for (var element = target; element != null && element != FrozenPart && element != ScrollPart; element = element.parent)
            {
                var column = FindCell(element);
                if (column >= 0)
                {
                    if (IsInlineControl(column, target))
                    {
                        InlinePointerDown?.Invoke(this, column, evt);
                    }

                    return;
                }
            }
        }

        private int FindCell(VisualElement element)
        {
            for (var column = 0; column < cells.Count; column++)
            {
                if (cells[column].Root == element)
                {
                    return column;
                }
            }

            return -1;
        }

        // --------------------------------------------------------------------------------
        /// <summary>
        /// <br/> target이 Inline 열 셀 안의 컨트롤(포커스를 받는 요소)이나 그 안쪽인지 확인한다.
        /// <br/> 셀 틀이나 내용의 빈 자리는 컨트롤이 아니다.
        /// </summary>
        // --------------------------------------------------------------------------------
        private bool IsInlineControl(int columnIndex, VisualElement target)
        {
            var cell = cells[columnIndex];
            if (cell.Column.Edit.Kind != TableGridCellEditKind.Inline)
            {
                return false;
            }

            for (var element = target; element != null && element != cell.Root; element = element.parent)
            {
                if (element.focusable)
                {
                    return true;
                }
            }

            return false;
        }

        private static VisualElement CreatePart()
        {
            var part = new VisualElement();
            part.AddToClassList("xeri-table__row");
            part.style.position = Position.Absolute;
            part.style.left = 0f;
            return part;
        }

        private void RegisterHover(VisualElement part)
        {
            // 셀 사이를 지날 때 자식에서 올라온 진입·이탈은 무시한다.
            part.RegisterCallback<PointerEnterEvent>(evt => SetHover(evt.target == part, true));
            part.RegisterCallback<PointerLeaveEvent>(evt => SetHover(evt.target == part, false));
        }

        private void SetHover(bool ownEvent, bool hovered)
        {
            if (ownEvent)
            {
                SetRowClass("xeri-table__row--hover", hovered);
            }
        }

    #endregion

    #region 열 구성

        // ------------------------------------------------------------
        /// <summary>
        /// 표시 열에 맞춰 셀을 다시 만든다. 열 구성이 바뀔 때만 부른다.
        /// </summary>
        // ------------------------------------------------------------
        public void BuildCells(IReadOnlyList<TableGridColumn> columns, bool showHandle, bool showSelector)
        {
            Unbind();
            foreach (var cell in cells)
            {
                cell.Root.RemoveFromHierarchy();
            }

            cells.Clear();
            handle.style.display = showHandle ? DisplayStyle.Flex : DisplayStyle.None;
            selector.style.display = showSelector ? DisplayStyle.Flex : DisplayStyle.None;

            foreach (var column in columns)
            {
                var root = new VisualElement();
                root.AddToClassList("xeri-table__cell");
                root.AddToClassList(AlignmentClass(column.Alignment));

                var content = column.MakeCell();
                root.Add(content);
                var slot = new CellSlot(this, column, root, content);
                if (column.Edit.Kind == TableGridCellEditKind.Inline)
                {
                    RegisterInline(slot);
                }

                cells.Add(slot);
            }
        }

        private void RegisterInline(CellSlot slot)
        {
            // Escape는 컨트롤보다 먼저 받아 보류 값을 버리고 표로 돌아간다.
            slot.Root.RegisterCallback<KeyDownEvent>(evt => HandleInlineEscape(slot, evt), TrickleDown.TrickleDown);
            slot.Root.RegisterCallback<KeyDownEvent>(evt => HandleInlineEnter(slot, evt));
            slot.Root.RegisterCallback<FocusOutEvent>(evt => HandleInlineFocusOut(slot, evt));
        }

        private void HandleInlineEscape(CellSlot slot, KeyDownEvent evt)
        {
            if (evt.keyCode != KeyCode.Escape)
            {
                return;
            }

            // 보류 값을 버렸으면 컨트롤이 원래 값을 다시 보이도록 다시 연결한다.
            if (slot.HasPending)
            {
                slot.Discard();
                BindCell(slot);
            }

            evt.StopPropagation();
            commands.FocusTable();
        }

        private static void HandleInlineEnter(CellSlot slot, KeyDownEvent evt)
        {
            // 컨트롤이 Enter를 먼저 처리한 뒤(값 확정) 보류 값을 요청한다.
            if (XeriShortcutKeys.IsSubmit(evt.keyCode))
            {
                slot.Commit();
            }
        }

        private static void HandleInlineFocusOut(CellSlot slot, FocusOutEvent evt)
        {
            // 셀 안에서 포커스가 옮겨 간 경우는 확정하지 않는다.
            for (var next = evt.relatedTarget as VisualElement; next != null; next = next.parent)
            {
                if (next == slot.Root)
                {
                    return;
                }
            }

            slot.Commit();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 셀을 고정·스크롤 영역에 나눠 놓고 폭과 행 폭을 맞춘다.
        /// </summary>
        // ------------------------------------------------------------
        public void ApplyGeometry(TableGridGeometry geometry, float scrollPartWidth)
        {
            // 열 구성이 바뀐 직후 기하가 아직 계산되지 않았으면 다음 배치에서 맞춘다.
            if (geometry.ColumnCount != cells.Count)
            {
                return;
            }

            handle.style.width = geometry.HandleWidth;
            selector.style.width = geometry.SelectorWidth;
            for (var index = 0; index < cells.Count; index++)
            {
                var cell = cells[index];
                var part = geometry.IsFrozen(index) ? FrozenPart : ScrollPart;
                if (cell.Root.parent != part)
                {
                    part.Add(cell.Root);
                }

                cell.Root.style.width = geometry.GetWidth(index);
                cell.Root.EnableInClassList("xeri-table__cell--frozen-edge", index == geometry.FrozenCount - 1);
            }

            // 그룹 제목은 셀 뒤에 두어 셀 위에 그려지게 한다.
            groupLabel.BringToFront();

            FrozenPart.style.width = geometry.FrozenWidth;
            FrozenPart.style.height = geometry.RowHeight;
            ScrollPart.style.width = scrollPartWidth;
            ScrollPart.style.height = geometry.RowHeight;
        }

        private static string AlignmentClass(TableGridAlignment alignment)
        {
            return alignment switch
            {
                TableGridAlignment.Center => "xeri-table__cell--center",
                TableGridAlignment.Right => "xeri-table__cell--right",
                _ => "xeri-table__cell--left",
            };
        }

    #endregion

    #region 행 연결

        // --------------------------------------------------------------------------------
        /// <summary>
        /// <br/> 행 위치와 값을 연결한다. number는 데이터 행 번호(1부터)이고 데이터 행이 아니면 0이다.
        /// <br/> 같은 행을 다시 연결하면 값은 그대로 두고 위치와 번호만 맞춘다.
        /// </summary>
        // --------------------------------------------------------------------------------
        public void Bind(int index, TableGridRow row, int number, float top)
        {
            FrozenPart.style.top = top;
            ScrollPart.style.top = top;

            // 행 번호는 데이터 행만 센다(그룹 머리 행은 번호가 없다). 앞의 머리 행이 바뀌면 같은 행도 번호가 바뀐다.
            selectorNumber.text = number > 0 ? GetNumberText(number) : string.Empty;

            if (!stale && index == Index && ReferenceEquals(row, Row))
            {
                return;
            }

            stale = false;
            Unbind();
            Index = index;
            Row = row;

            var isGroup = row.Kind == TableGridRowKind.GroupHeader;
            SetRowClass("xeri-table__row--alt", index % 2 == 1);
            SetRowClass("xeri-table__row--group", isGroup);
            SetRowClass("xeri-table__row--summary", row.Kind == TableGridRowKind.Summary);

            groupLabel.style.display = isGroup ? DisplayStyle.Flex : DisplayStyle.None;
            if (isGroup)
            {
                groupLabel.text = row.Title;
            }

            foreach (var cell in cells)
            {
                cell.Root.style.visibility = isGroup ? Visibility.Hidden : Visibility.Visible;
                if (!isGroup)
                {
                    BindCell(cell);
                }
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 다음 Bind에서 같은 행이라도 셀 값을 다시 연결하게 한다.
        /// </summary>
        // ------------------------------------------------------------
        public void Invalidate()
        {
            stale = true;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> 행 번호 글자를 돌려준다. 스크롤마다 같은 번호가 다시 쓰이므로
        /// <br/> 앞쪽 번호는 한 번 만든 글자를 재사용해 할당을 줄인다.
        /// </summary>
        // ------------------------------------------------------------
        private static string GetNumberText(int number)
        {
            if (number >= numberTexts.Length)
            {
                return number.ToString(CultureInfo.InvariantCulture);
            }

            return numberTexts[number] ??= number.ToString(CultureInfo.InvariantCulture);
        }

        private void BindCell(CellSlot cell)
        {
            var canEdit = DataRowID != null && commands.CanEdit(cell.Column);
            var context = new TableGridCellContext(Row.ID, Index, Row.Item, cell.Column, canEdit, cell);
            cell.Column.BindCell(cell.Content, Row.Item, in context);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 행 연결을 끊는다. 요소는 다음 행에서 재사용한다.
        /// </summary>
        // ------------------------------------------------------------
        public void Unbind()
        {
            // 행 요소가 다른 행에 다시 쓰이므로 붙어 있던 편집기를 떼고 보류 값을 버린다.
            HideEditor();
            if (Index < 0)
            {
                return;
            }

            if (Row != null && Row.Kind != TableGridRowKind.GroupHeader)
            {
                foreach (var cell in cells)
                {
                    cell.Discard();
                    cell.Column.UnbindCell(cell.Content);
                }
            }

            Index = -1;
            Row = null;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 그룹 제목이 스크롤 영역의 보이는 왼쪽에 머물도록 가로 위치를 맞춘다.
        /// </summary>
        // ------------------------------------------------------------
        public void SetGroupTitleOffset(float scrollX, float viewportWidth)
        {
            groupLabel.style.left = scrollX;
            groupLabel.style.width = Mathf.Max(0f, viewportWidth);
        }

        // --------------------------------------------------------------------------------
        /// <summary>
        /// <br/> 셀 내용의 글자 요소와, 그 글자 둘레에 붙는 여백·테두리 폭을 찾는다.
        /// <br/> 열 내용 맞춤에서 같은 모양의 글자 폭을 잴 때 쓴다. 글자 요소가 없으면 false다.
        /// </summary>
        // --------------------------------------------------------------------------------
        public bool TryGetCellText(int columnIndex, out TextElement text, out float chrome)
        {
            text = null;
            chrome = 0f;
            if (columnIndex < 0 || columnIndex >= cells.Count)
            {
                return false;
            }

            var cell = cells[columnIndex];
            text = cell.Content as TextElement ?? cell.Content.Q<TextElement>();
            if (text == null || text.panel == null)
            {
                return false;
            }

            // 글자 요소부터 셀 틀까지 올라가며 좌우 여백·테두리를 더한다. 셀 틀의 바깥 여백은 셀 폭에 들지 않는다.
            for (var element = (VisualElement)text; element != null; element = element.parent)
            {
                var style = element.resolvedStyle;
                chrome += style.paddingLeft + style.paddingRight + style.borderLeftWidth + style.borderRightWidth;
                if (element == cell.Root)
                {
                    break;
                }

                chrome += style.marginLeft + style.marginRight;
            }

            return true;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 셀 틀의 월드 좌표 경계를 찾는다. 없는 셀이면 false다.
        /// </summary>
        // ------------------------------------------------------------
        public bool TryGetCellBounds(int columnIndex, out Rect bounds)
        {
            bounds = default;
            if (columnIndex < 0 || columnIndex >= cells.Count)
            {
                return false;
            }

            bounds = cells[columnIndex].Root.worldBound;
            return true;
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// 셀 안에서 포커스를 받을 수 있는 첫 컨트롤에 포커스를 준다. 없으면 false다.
        /// </summary>
        // ----------------------------------------------------------------------
        public bool FocusControl(int columnIndex)
        {
            if (columnIndex < 0 || columnIndex >= cells.Count)
            {
                return false;
            }

            var control = FindFocusable(cells[columnIndex].Content);
            if (control == null)
            {
                return false;
            }

            control.Focus();
            return true;
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// 요소 자신이나 그 안에서 포커스를 받을 수 있는 첫 요소를 찾는다. 없으면 null이다.
        /// </summary>
        // ----------------------------------------------------------------------
        public static VisualElement FindFocusable(VisualElement element)
        {
            if (element.focusable && element.canGrabFocus)
            {
                return element;
            }

            foreach (var child in element.Children())
            {
                var found = FindFocusable(child);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

    #endregion

    #region 셀 편집기

        // ------------------------------------------------------------
        /// <summary>
        /// 셀 내용을 가리고 그 자리에 편집기를 놓는다.
        /// </summary>
        // ------------------------------------------------------------
        public void ShowEditor(int columnIndex, VisualElement editor)
        {
            HideEditor();
            var cell = cells[columnIndex];
            editorContentDisplay = cell.Content.style.display;
            cell.Content.style.display = DisplayStyle.None;
            cell.Root.AddToClassList("xeri-table__cell--editing");
            cell.Root.Add(editor);
            editorColumn = columnIndex;
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// 편집기를 떼고 셀 내용을 다시 보인다. 편집기가 없으면 아무것도 하지 않는다.
        /// </summary>
        // ----------------------------------------------------------------------
        public void HideEditor()
        {
            if (editorColumn < 0 || editorColumn >= cells.Count)
            {
                editorColumn = -1;
                return;
            }

            var cell = cells[editorColumn];
            editorColumn = -1;
            cell.Root.RemoveFromClassList("xeri-table__cell--editing");
            for (var index = cell.Root.childCount - 1; index >= 0; index--)
            {
                var child = cell.Root[index];
                if (child != cell.Content)
                {
                    child.RemoveFromHierarchy();
                }
            }

            cell.Content.style.display = editorContentDisplay;
        }

    #endregion

    #region 선택 표시

        // ------------------------------------------------------------
        /// <summary>
        /// 행 선택 여부를 표시한다.
        /// </summary>
        // ------------------------------------------------------------
        public void SetRowSelected(bool selected)
        {
            SetRowClass("xeri-table__row--selected", selected);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 셀 범위가 지나는 행 표시를 켜거나 끈다. 행 번호 칸이 강조된다.
        /// </summary>
        // ------------------------------------------------------------
        public void SetRowInRange(bool inRange)
        {
            SetRowClass("xeri-table__row--range", inRange);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 잘라내어 옮길 행 표시를 켜거나 끈다.
        /// </summary>
        // ------------------------------------------------------------
        public void SetRowCut(bool cut)
        {
            SetRowClass("xeri-table__row--cut", cut);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 끌어서 옮기는 중인 행 표시를 켜거나 끈다.
        /// </summary>
        // ------------------------------------------------------------
        public void SetRowDragging(bool dragging)
        {
            SetRowClass("xeri-table__row--dragging", dragging);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 셀의 범위 바탕·활성·거부 표시를 바꾼다. 바뀐 표시만 클래스를 고친다.
        /// </summary>
        // ------------------------------------------------------------
        public void SetCellMarks(int columnIndex, TableGridCellMarks marks)
        {
            var cell = cells[columnIndex];
            var changed = cell.Marks ^ marks;
            if (changed == TableGridCellMarks.None)
            {
                return;
            }

            cell.Marks = marks;
            var root = cell.Root;
            Toggle(TableGridCellMarks.InRange, "xeri-table__cell--in-range");
            Toggle(TableGridCellMarks.Active, "xeri-table__cell--active");
            Toggle(TableGridCellMarks.Rejected, "xeri-table__cell--rejected");
            Toggle(TableGridCellMarks.Cut, "xeri-table__cell--cut");

            void Toggle(TableGridCellMarks mark, string className)
            {
                if ((changed & mark) != 0)
                {
                    root.EnableInClassList(className, (marks & mark) != 0);
                }
            }
        }

        private void SetRowClass(string className, bool enabled)
        {
            FrozenPart.EnableInClassList(className, enabled);
            ScrollPart.EnableInClassList(className, enabled);
        }

    #endregion

    }
}
