/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TableGridHeaderView.cs
수정일 : 2026-11-22

# 설명
열 헤더 줄. 행과 같이 고정 영역 쪽과 스크롤 영역 쪽으로 나뉘고 같은 기하로 폭을 맞춘다.
헤더 칸은 제목, 정렬·필터·그룹 상태 표시, 호버 때만 보이는 메뉴 아이콘을 가진다.
고정 영역 맨 앞에는 행의 끌기 손잡이·행 선택 칸과 폭을 맞추는 빈 자리를 둔다.
열 선택 표시(고른 열 강조), 열 끌기 중의 흐린 표시와 열 경계 삽입선도 여기서 그린다.

# 특이사항, 제약사항
헤더 칸 누름은 HeaderPointerDown, 왼쪽 클릭은 HeaderClicked, 우클릭은 HeaderContextClicked로 알린다.
열 선택·열 이동은 TableGridPointerInput이, 정렬·열 메뉴는 TableGrid가 정한다. 열 경계 핸들 조작은 칸 누름으로 올라가지 않는다.
패널 좌표의 열·열 경계 찾기는 본문과 같은 기하(TableGridGeometry)로 계산한다.
상태 표시는 호스트가 준 쿼리 표시 상태(TableGridQueryState)를 그대로 따른다.
아이콘 모양은 USS가 resource()로 지정한다.
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
    /// 열 헤더 줄.
    /// </summary>
    // ============================================================
    internal sealed class TableGridHeaderView
    {

    #region 내부 타입

        // ============================================================
        /// <summary>
        /// 헤더 칸 하나.
        /// </summary>
        // ============================================================
        private sealed class HeaderCell
        {
            // ------------------------------------------------------------
            /// <summary>
            /// 칸이 나타내는 열.
            /// </summary>
            // ------------------------------------------------------------
            public TableGridColumn Column;

            // ------------------------------------------------------------
            /// <summary>
            /// 칸 틀 요소.
            /// </summary>
            // ------------------------------------------------------------
            public VisualElement Root;

            // ------------------------------------------------------------
            /// <summary>
            /// 열 제목.
            /// </summary>
            // ------------------------------------------------------------
            public Label Title;

            // ------------------------------------------------------------
            /// <summary>
            /// 정렬 방향 표시 요소.
            /// </summary>
            // ------------------------------------------------------------
            public VisualElement Sort;

            // ------------------------------------------------------------
            /// <summary>
            /// 필터 표시 요소.
            /// </summary>
            // ------------------------------------------------------------
            public VisualElement Filter;

            // ------------------------------------------------------------
            /// <summary>
            /// 그룹 표시 요소.
            /// </summary>
            // ------------------------------------------------------------
            public VisualElement Group;
        }

    #endregion

    #region 필드

        private readonly List<HeaderCell> cells = new();
        private readonly TableGridGeometry geometry;
        private readonly VisualElement handleSpacer;
        private readonly VisualElement selectorSpacer;
        private readonly VisualElement dropLine;

        private VisualElement resizeHandle;
        private TableGridColumn resizeColumn;
        private float resizeStartX;
        private float resizeStartWidth;
        private bool resizeMoved;

        // ------------------------------------------------------------
        /// <summary>
        /// 고정 영역 쪽 헤더(끌기 손잡이·행 선택 칸 자리 + 고정 열).
        /// </summary>
        // ------------------------------------------------------------
        public VisualElement FrozenPart { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 스크롤 영역 쪽 헤더 콘텐츠. 가로 스크롤만큼 옮겨 그린다.
        /// </summary>
        // ------------------------------------------------------------
        public VisualElement ScrollContent { get; }

    #endregion

    #region 이벤트

        // ------------------------------------------------------------
        /// <summary>
        /// 헤더 칸을 눌렀을 때(모든 버튼) 표시 열 위치와 누름 이벤트를 알린다.
        /// </summary>
        // ------------------------------------------------------------
        public event Action<int, PointerDownEvent> HeaderPointerDown;

        // ------------------------------------------------------------
        /// <summary>
        /// 헤더 칸을 왼쪽 클릭했을 때(누른 뒤 놓음) 열과 헤더 칸 요소를 알린다.
        /// </summary>
        // ------------------------------------------------------------
        public event Action<TableGridColumn, VisualElement> HeaderClicked;

        // ------------------------------------------------------------
        /// <summary>
        /// 헤더 칸을 우클릭했을 때 열과 헤더 칸 요소를 알린다.
        /// </summary>
        // ------------------------------------------------------------
        public event Action<TableGridColumn, VisualElement> HeaderContextClicked;

        // ------------------------------------------------------------
        /// <summary>
        /// 열 경계를 끄는 동안 열과 새 폭을 알린다.
        /// </summary>
        // ------------------------------------------------------------
        public event Action<TableGridColumn, float> ColumnResizing;

        // ------------------------------------------------------------
        /// <summary>
        /// 열 경계 끌기를 마쳤을 때 알린다. 끄는 동안에는 알리지 않는다.
        /// </summary>
        // ------------------------------------------------------------
        public event Action ColumnResized;

        // ------------------------------------------------------------
        /// <summary>
        /// 열 경계를 두 번 눌러 내용 맞춤을 요청했을 때 알린다.
        /// </summary>
        // ------------------------------------------------------------
        public event Action<TableGridColumn> ColumnAutoFitRequested;

    #endregion

    #region 생성

        // ------------------------------------------------------------
        /// <summary>
        /// UXML이 만든 두 헤더 컨테이너와 표의 기하에 연결한다.
        /// </summary>
        // ------------------------------------------------------------
        public TableGridHeaderView(VisualElement frozenPart, VisualElement scrollContent, TableGridGeometry geometry)
        {
            FrozenPart = frozenPart ?? throw new ArgumentNullException(nameof(frozenPart));
            ScrollContent = scrollContent ?? throw new ArgumentNullException(nameof(scrollContent));
            this.geometry = geometry ?? throw new ArgumentNullException(nameof(geometry));

            handleSpacer = new VisualElement();
            handleSpacer.AddToClassList("xeri-table__header-handle");
            FrozenPart.Add(handleSpacer);

            selectorSpacer = new VisualElement();
            selectorSpacer.AddToClassList("xeri-table__header-selector");
            FrozenPart.Add(selectorSpacer);

            // 열 이동 삽입선은 늘 만들어 두고 놓을 구역으로 옮겨 보인다. 처음 보일 때도 두께를 바로 쓸 수 있다.
            dropLine = new VisualElement
            {
                pickingMode = PickingMode.Ignore,
            };
            dropLine.AddToClassList("xeri-table__header-drop-line");
            dropLine.style.visibility = Visibility.Hidden;
            FrozenPart.Add(dropLine);
        }

    #endregion

    #region 열 구성

        // ------------------------------------------------------------
        /// <summary>
        /// 표시 열에 맞춰 헤더 칸을 다시 만든다.
        /// </summary>
        // ------------------------------------------------------------
        public void Build(IReadOnlyList<TableGridColumn> columns, bool showHandle, bool showSelector)
        {
            EndResize(-1);
            foreach (var cell in cells)
            {
                cell.Root.RemoveFromHierarchy();
            }

            cells.Clear();
            handleSpacer.style.display = showHandle ? DisplayStyle.Flex : DisplayStyle.None;
            selectorSpacer.style.display = showSelector ? DisplayStyle.Flex : DisplayStyle.None;

            for (var index = 0; index < columns.Count; index++)
            {
                cells.Add(CreateCell(columns[index], index));
            }
        }

        private HeaderCell CreateCell(TableGridColumn column, int index)
        {
            var root = new VisualElement();
            root.AddToClassList("xeri-table__header-cell");
            root.EnableInClassList("xeri-table__header-cell--right", column.Alignment == TableGridAlignment.Right);

            var title = new Label(column.Title);
            title.AddToClassList("xeri-table__header-title");
            root.Add(title);

            var cell = new HeaderCell
            {
                Column = column,
                Root = root,
                Title = title,
                Sort = CreateIndicator("xeri-table__header-sort"),
                Filter = CreateIndicator("xeri-table__header-filter"),
                Group = CreateIndicator("xeri-table__header-group"),
            };

            root.Add(cell.Sort);
            root.Add(cell.Filter);
            root.Add(cell.Group);

            // 누름은 열 선택·열 이동(포인터 입력)이, 왼쪽 클릭은 정렬, 우클릭은 열 메뉴(TableGrid)가 받는다.
            // 열 선택이 누름에서 포인터를 점유하면 놓음은 표 루트로 가므로 왼쪽 클릭으로 알리지 않는다.
            void HandlePointerDown(PointerDownEvent evt)
            {
                HeaderPointerDown?.Invoke(index, evt);
            }

            void HandlePointerUp(PointerUpEvent evt)
            {
                if (evt.button == 0)
                {
                    HeaderClicked?.Invoke(column, root);
                }
                else if (evt.button == 1)
                {
                    HeaderContextClicked?.Invoke(column, root);
                }
                else
                {
                    return;
                }

                evt.StopPropagation();
            }

            root.RegisterCallback<PointerDownEvent>(HandlePointerDown);
            root.RegisterCallback<PointerUpEvent>(HandlePointerUp);
            root.Add(CreateResizeHandle(column, root));

            return cell;
        }

        private VisualElement CreateResizeHandle(TableGridColumn column, VisualElement root)
        {
            var handle = new VisualElement();
            handle.AddToClassList("xeri-table__header-resize");

            // 경계 조작은 헤더 칸 누름·클릭(열 선택·정렬·열 메뉴)으로 올라가지 않게 막는다.
            void HandlePointerDown(PointerDownEvent evt)
            {
                if (evt.button == 0)
                {
                    BeginResize(evt, handle, column, root);
                }

                evt.StopPropagation();
            }

            void HandlePointerMove(PointerMoveEvent evt)
            {
                if (resizeHandle == handle)
                {
                    resizeMoved = true;
                    ColumnResizing?.Invoke(resizeColumn, resizeStartWidth + evt.position.x - resizeStartX);
                    evt.StopPropagation();
                }
            }

            void HandlePointerUp(PointerUpEvent evt)
            {
                EndResize(evt.pointerId);
                evt.StopPropagation();
            }

            handle.RegisterCallback<PointerDownEvent>(HandlePointerDown);
            handle.RegisterCallback<PointerMoveEvent>(HandlePointerMove);
            handle.RegisterCallback<PointerUpEvent>(HandlePointerUp);
            handle.RegisterCallback<PointerCaptureOutEvent>(_ => EndResize(-1));

            return handle;
        }

        private void BeginResize(PointerDownEvent evt, VisualElement handle, TableGridColumn column, VisualElement root)
        {
            EndResize(-1);
            if (evt.clickCount >= 2)
            {
                ColumnAutoFitRequested?.Invoke(column);
                return;
            }

            resizeHandle = handle;
            resizeColumn = column;
            resizeMoved = false;
            resizeStartX = evt.position.x;
            resizeStartWidth = root.resolvedStyle.width;
            handle.AddToClassList("xeri-table__header-resize--active");
            handle.CapturePointer(evt.pointerId);
        }

        private void EndResize(int pointerID)
        {
            var handle = resizeHandle;
            if (handle == null)
            {
                return;
            }

            var column = resizeColumn;
            var moved = resizeMoved;
            resizeHandle = null;
            resizeColumn = null;
            resizeMoved = false;
            handle.RemoveFromClassList("xeri-table__header-resize--active");
            if (pointerID >= 0 && handle.HasPointerCapture(pointerID))
            {
                handle.ReleasePointer(pointerID);
            }

            if (moved)
            {
                ColumnResized?.Invoke();
            }
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> 헤더 칸이 제목을 자르지 않고 보이는 데 필요한 폭을 잰다.
        /// <br/> 제목 글자 폭에 칸 여백·테두리와 자리를 차지하는 상태 표시 폭을 더한다.
        /// </summary>
        // ----------------------------------------------------------------------
        public float MeasureTitleWidth(int index)
        {
            if (index < 0 || index >= cells.Count)
            {
                return 0f;
            }

            var cell = cells[index];
            var root = cell.Root;
            if (root.panel == null)
            {
                return 0f;
            }

            var size = cell.Title.MeasureTextSize(cell.Column.Title, 0f, VisualElement.MeasureMode.Undefined, 0f, VisualElement.MeasureMode.Undefined);
            var width = float.IsNaN(size.x) ? 0f : size.x;

            var style = root.resolvedStyle;
            width += style.paddingLeft + style.paddingRight + style.borderLeftWidth + style.borderRightWidth;
            foreach (var child in root.Children())
            {
                var childStyle = child.resolvedStyle;
                if (child == cell.Title || childStyle.display == DisplayStyle.None || childStyle.position == Position.Absolute)
                {
                    continue;
                }

                width += childStyle.width + childStyle.marginLeft + childStyle.marginRight;
            }

            return width;
        }

        private static VisualElement CreateIndicator(string className)
        {
            var element = new VisualElement();
            element.AddToClassList(className);
            element.pickingMode = PickingMode.Ignore;
            return element;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 헤더 칸을 고정·스크롤 영역에 나눠 놓고 폭을 맞춘다.
        /// </summary>
        // ------------------------------------------------------------
        public void ApplyGeometry()
        {
            if (geometry.ColumnCount != cells.Count)
            {
                return;
            }

            handleSpacer.style.width = geometry.HandleWidth;
            selectorSpacer.style.width = geometry.SelectorWidth;
            for (var index = 0; index < cells.Count; index++)
            {
                var root = cells[index].Root;
                var part = geometry.IsFrozen(index) ? FrozenPart : ScrollContent;
                if (root.parent != part)
                {
                    part.Add(root);
                }

                root.style.width = geometry.GetWidth(index);
                root.EnableInClassList("xeri-table__header-cell--frozen-edge", index == geometry.FrozenCount - 1);
            }

            FrozenPart.style.width = geometry.FrozenWidth;
            ScrollContent.style.width = geometry.ScrollContentWidth;
        }

    #endregion

    #region 상태 표시

        // ------------------------------------------------------------
        /// <summary>
        /// 쿼리 표시 상태에 맞춰 정렬 방향·필터·그룹 표시를 바꾼다.
        /// </summary>
        // ------------------------------------------------------------
        public void ApplyQuery(TableGridQueryState query)
        {
            foreach (var cell in cells)
            {
                var id = cell.Column.ID;
                var sorted = query != null && query.SortDirection.HasValue
                    && string.Equals(query.SortColumnID, id, StringComparison.Ordinal);
                var descending = sorted && query.SortDirection == TableGridSortDirection.Descending;

                cell.Sort.EnableInClassList("xeri-table__header-sort--asc", sorted && !descending);
                cell.Sort.EnableInClassList("xeri-table__header-sort--desc", descending);
                cell.Filter.EnableInClassList("xeri-table__header-filter--on", HasFilter(query, id));
                cell.Group.EnableInClassList
                (
                    "xeri-table__header-group--on",
                    query != null && string.Equals(query.GroupColumnID, id, StringComparison.Ordinal)
                );
                cell.Root.EnableInClassList
                (
                    "xeri-table__header-cell--active",
                    sorted || HasFilter(query, id) || (query != null && string.Equals(query.GroupColumnID, id, StringComparison.Ordinal))
                );
            }
        }

        private static bool HasFilter(TableGridQueryState query, string columnID)
        {
            if (query == null)
            {
                return false;
            }

            foreach (var filter in query.Filters)
            {
                if (string.Equals(filter.ColumnID, columnID, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 열 메뉴가 열린 헤더 칸을 표시한다. null이면 모두 해제한다.
        /// </summary>
        // ------------------------------------------------------------
        public void SetOpenColumn(string columnID)
        {
            foreach (var cell in cells)
            {
                cell.Root.EnableInClassList
                (
                    "xeri-table__header-cell--open",
                    string.Equals(cell.Column.ID, columnID, StringComparison.Ordinal)
                );
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 열 선택에 든 열의 헤더 칸을 강조한다.
        /// </summary>
        // ------------------------------------------------------------
        public void ApplySelection(TableGridSelectionState selection)
        {
            var columnSelection = selection.Current.Kind == TableGridSelectionKind.Column;
            foreach (var cell in cells)
            {
                cell.Root.EnableInClassList
                (
                    "xeri-table__header-cell--selected",
                    columnSelection && selection.IsColumnSelected(cell.Column.ID)
                );
            }
        }

    #endregion

    #region 열 끌기 표시

        // ------------------------------------------------------------
        /// <summary>
        /// 끌어서 옮기는 중인 열의 헤더 칸을 흐리게 한다. null이면 지운다.
        /// </summary>
        // ------------------------------------------------------------
        public void SetDraggingColumns(ISet<string> columnIDs)
        {
            foreach (var cell in cells)
            {
                cell.Root.EnableInClassList
                (
                    "xeri-table__header-cell--dragging",
                    columnIDs != null && columnIDs.Contains(cell.Column.ID)
                );
            }
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> 열 경계 gap에 삽입선을 보인다. 음수면 숨긴다.
        /// <br/> frozen이면 고정 영역 쪽, 아니면 스크롤 영역 쪽 경계에 놓는다.
        /// </summary>
        // ----------------------------------------------------------------------
        public void ShowDropLine(int gap, bool frozen)
        {
            if (gap < 0)
            {
                dropLine.style.visibility = Visibility.Hidden;
                return;
            }

            var part = frozen ? FrozenPart : ScrollContent;
            if (dropLine.parent != part)
            {
                part.Add(dropLine);
            }

            // 경계를 선 가운데에 두되, 영역 양 끝 경계의 선도 잘리지 않게 영역 안으로 맞춘다.
            var thickness = dropLine.resolvedStyle.width;
            thickness = float.IsNaN(thickness) ? 0f : thickness;
            var partWidth = frozen ? geometry.FrozenWidth : geometry.ScrollContentWidth;
            var left = geometry.GetGapOffset(gap, frozen) - thickness * 0.5f;
            dropLine.style.left = Mathf.Clamp(left, 0f, Mathf.Max(0f, partWidth - thickness));
            dropLine.style.visibility = Visibility.Visible;
            dropLine.BringToFront();
        }

    #endregion

    #region 위치 찾기

        // --------------------------------------------------------------------------------
        /// <summary>
        /// <br/> 패널 x에 있는 헤더 칸의 표시 열 위치를 찾는다. 헤더 밖이면 가장 가까운 열이다.
        /// <br/> 끌기 중 포인터가 표 밖으로 나가도 가장자리 열로 범위를 잇는 데 쓴다.
        /// </summary>
        // --------------------------------------------------------------------------------
        public bool TryFindColumn(float x, out int columnIndex)
        {
            var inFrozen = geometry.HasFrozenColumns && x < ScrollContent.parent.worldBound.xMin;
            columnIndex = inFrozen
                ? geometry.FindColumnAt(x - FrozenPart.worldBound.xMin, true)
                : geometry.FindColumnAt(x - ScrollContent.worldBound.xMin, false);

            return columnIndex >= 0;
        }

        // --------------------------------------------------------------------------------
        /// <summary>
        /// <br/> 패널 x에 가장 가까운 열 경계(열 이동의 삽입 자리)와 그 경계의 구역을 찾는다.
        /// <br/> 고정 열이 있고 고정 영역 위면 고정 구역, 아니면 스크롤 구역의 경계다.
        /// <br/> 표시 열이 없으면 false다.
        /// </summary>
        // --------------------------------------------------------------------------------
        public bool TryFindColumnGap(float x, out int gap, out bool frozen)
        {
            frozen = geometry.HasFrozenColumns && x < ScrollContent.parent.worldBound.xMin;
            gap = frozen
                ? geometry.FindColumnGapAt(x - FrozenPart.worldBound.xMin, true)
                : geometry.FindColumnGapAt(x - ScrollContent.worldBound.xMin, false);

            return gap >= 0;
        }

    #endregion

    }
}
