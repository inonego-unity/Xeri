/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TableGridViewport.cs
수정일 : 2026-11-23

# 설명
표 본문. 고정 영역과 가로 스크롤 영역에 행을 그리고, 보이는 행만 요소로 만들어 재사용한다(세로 가상화).
보이는 범위의 행을 소스에서 바로 읽어 연결하고, 연결한 행에 선택 바탕을 적용한다.
셀 범위 외곽선과 행 끌기의 삽입선은 본문 맨 위 선택 층에 그려 행 가로선·고정선보다 위에 온다.
열 선택은 고른 열의 셀에 범위 바탕을 주고, 이어진 열이면 모든 행에 걸친 셀 범위와 같은 외곽선을 그린다.
행 끌기 중에는 옮기는 행을 흐리게 표시하고 삽입선을 행 경계에 놓는다. 놓을 자리 판단은 TableGridPointerInput이 한다.

# 특이사항, 제약사항
세로 스크롤은 스크롤 영역의 ScrollView가 소유하고 고정 영역은 같은 양만큼 옮겨 그린다.
행 요소는 행 인덱스를 풀 크기로 나눈 나머지 자리에 두어, 스크롤 중에도 같은 행은 같은 요소를 쓴다.
열 기하는 TableGrid가 TableGridGeometry에 계산해 두고, 본문은 그 결과만 쓴다.
셀로 스크롤할 때 배치 전이거나 스크롤 범위가 새 행 수를 아직 반영하지 않았으면 다음 배치 뒤에 한 번 더 맞춘다.
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
    /// 표 본문(세로 가상화).
    /// </summary>
    // ============================================================
    internal sealed class TableGridViewport
    {

    #region 필드

        // 본문 가로선·고정선의 두께. 선택 외곽선을 이 선 위에 겹쳐 놓을 때 쓴다(USS의 1px 선과 맞춘다).
        private const float GRID_LINE_WIDTH = 1f;

        private readonly List<TableGridRowView> rowPool = new();
        private readonly TableGridModel model;
        private readonly TableGridGeometry geometry;
        private readonly TableGridOptions options;
        private readonly TableGridCellCommands commands;
        private readonly HashSet<string> draggingRowIDs = new(StringComparer.Ordinal);
        private bool showHandle;
        private bool showSelector;
        private int dropGap = -1;
        private int windowStart;
        private int windowCount = -1;

        private readonly VisualElement frozenPane;
        private readonly VisualElement frozenRows;
        private readonly ScrollView scrollPane;
        private readonly VisualElement scrollRows;

        private readonly VisualElement selectionLayer;
        private readonly VisualElement scrollClip;
        private readonly VisualElement dropLine;

        // 데이터 행이 이어진 구간마다 외곽선을 한 조각씩 쓴다(그룹 머리·요약 행에서 끊긴다). 모자라면 늘리고 남는 것은 숨긴다.
        private readonly List<VisualElement> frozenOutlines = new();
        private readonly List<VisualElement> scrollOutlines = new();
        private readonly List<(int First, int Last)> outlineRuns = new();

        private int revealRow = -1;
        private int revealColumn = -1;
        private bool hasPendingReveal;
        private TableGridRowView editorView;

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 스크롤 위치.
        /// </summary>
        // ------------------------------------------------------------
        public Vector2 ScrollOffset => scrollPane.scrollOffset;

        // ------------------------------------------------------------
        /// <summary>
        /// 본문이 보이는 행 구간의 첫 행 위치.
        /// </summary>
        // ------------------------------------------------------------
        public int WindowStart => geometry.WindowStart;

        // ------------------------------------------------------------
        /// <summary>
        /// 스크롤 위치를 지정한다. 내용 범위를 넘으면 스크롤 영역이 맞춘다.
        /// </summary>
        // ------------------------------------------------------------
        public void SetScrollOffset(Vector2 offset)
        {
            scrollPane.scrollOffset = offset;
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> 본문이 보일 행 구간을 지정한다. count가 음수면 전체 행이다(Scroll).
        /// <br/> 다시 그리는 것은 호출자가 Refresh로 한다.
        /// </summary>
        // ----------------------------------------------------------------------
        public void SetRowWindow(int start, int count)
        {
            windowStart = start;
            windowCount = count;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 행이 보이는 영역의 높이. 배치 전이면 0이다.
        /// </summary>
        // ------------------------------------------------------------
        public float ViewportHeight
        {
            get
            {
                var height = scrollPane.contentViewport.layout.height;
                return float.IsNaN(height) ? 0f : height;
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 세로 스크롤바 폭. 헤더 오른쪽 여백을 맞출 때 쓴다.
        /// </summary>
        // ------------------------------------------------------------
        public float ScrollbarWidth
        {
            get
            {
                var width = scrollPane.verticalScroller.resolvedStyle.width;
                return float.IsNaN(width) ? 0f : width;
            }
        }

    #endregion

    #region 이벤트

        // ------------------------------------------------------------
        /// <summary>
        /// 스크롤 위치가 바뀌었을 때 알린다. 헤더가 가로 위치를 따라간다.
        /// </summary>
        // ------------------------------------------------------------
        public event Action<Vector2> Scrolled;

        // ------------------------------------------------------------
        /// <summary>
        /// 보이는 행을 다시 연결했을 때 알린다. 상태 줄이 범위를 갱신한다.
        /// </summary>
        // ------------------------------------------------------------
        public event Action RowsBound;

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> 보이는 높이가 바뀌어 다시 연결하기 직전에 알린다.
        /// <br/> 표가 높이에 따른 페이지 구간(PageSize 0)을 먼저 맞춘다.
        /// </summary>
        // ------------------------------------------------------------
        public event Action Resized;

        // --------------------------------------------------------------------------------
        /// <summary>
        /// 본문이 보이는 구간 밖의 행을 보여야 할 때 그 행을 알린다(Paged에서 페이지를 바꿀 때).
        /// </summary>
        // --------------------------------------------------------------------------------
        public event Action<int> WindowChangeRequested;

        // ------------------------------------------------------------
        /// <summary>
        /// 셀을 눌렀을 때 행 위치·표시 열 위치를 알린다.
        /// </summary>
        // ------------------------------------------------------------
        public event Action<int, int, PointerDownEvent> CellPointerDown;

        // ------------------------------------------------------------
        /// <summary>
        /// Inline 셀 안 컨트롤을 눌렀을 때 행 위치·표시 열 위치를 알린다.
        /// </summary>
        // ------------------------------------------------------------
        public event Action<int, int, PointerDownEvent> InlinePointerDown;

        // ------------------------------------------------------------
        /// <summary>
        /// 행 선택 칸을 눌렀을 때 행 위치를 알린다.
        /// </summary>
        // ------------------------------------------------------------
        public event Action<int, PointerDownEvent> SelectorPointerDown;

        // ------------------------------------------------------------
        /// <summary>
        /// 끌기 손잡이를 눌렀을 때 행 위치를 알린다.
        /// </summary>
        // ------------------------------------------------------------
        public event Action<int, PointerDownEvent> ReorderHandlePointerDown;

    #endregion

    #region 생성

        // ------------------------------------------------------------
        /// <summary>
        /// UXML이 만든 본문 요소에 연결한다.
        /// </summary>
        // ------------------------------------------------------------
        public TableGridViewport
        (
            VisualElement frozenPane, VisualElement frozenRows,
            ScrollView scrollPane, VisualElement scrollRows,
            TableGridModel model, TableGridGeometry geometry, TableGridOptions options, TableGridCellCommands commands
        )
        {
            this.frozenPane = frozenPane ?? throw new ArgumentNullException(nameof(frozenPane));
            this.frozenRows = frozenRows ?? throw new ArgumentNullException(nameof(frozenRows));
            this.scrollPane = scrollPane ?? throw new ArgumentNullException(nameof(scrollPane));
            this.scrollRows = scrollRows ?? throw new ArgumentNullException(nameof(scrollRows));
            this.model = model ?? throw new ArgumentNullException(nameof(model));
            this.geometry = geometry ?? throw new ArgumentNullException(nameof(geometry));
            this.options = options ?? throw new ArgumentNullException(nameof(options));
            this.commands = commands ?? throw new ArgumentNullException(nameof(commands));
            showSelector = options.ShowsRowSelector;

            // 세로 스크롤바를 늘 두어 헤더와 본문의 오른쪽 끝이 바뀌지 않게 한다.
            scrollPane.verticalScrollerVisibility = ScrollerVisibility.AlwaysVisible;
            scrollPane.horizontalScrollerVisibility = ScrollerVisibility.Auto;
            scrollPane.verticalScroller.valueChanged += _ => HandleScroll();
            scrollPane.horizontalScroller.valueChanged += _ => HandleScroll();
            scrollPane.contentViewport.RegisterCallback<GeometryChangedEvent>(_ => HandleViewportResized());

            // ScrollView가 같은 요소의 배치 콜백에서 스크롤 범위를 먼저 갱신하므로, 그 뒤에 닿지 못한 스크롤을 맞춘다.
            scrollPane.contentContainer.RegisterCallback<GeometryChangedEvent>(_ => ApplyPendingReveal());
            frozenPane.RegisterCallback<WheelEvent>(HandleFrozenWheel);

            // 선택 외곽선 층: 본문 맨 위에 두어 행 가로선·고정선보다 위에 그린다. 스크롤 쪽 조각은 보이는 영역으로 자른다.
            var body = scrollPane.parent ?? throw new ArgumentException("스크롤 영역은 본문 요소 안에 있어야 합니다.", nameof(scrollPane));
            selectionLayer = CreateOverlayPart("xeri-table__selection-layer");
            scrollClip = CreateOverlayPart("xeri-table__selection-clip");
            selectionLayer.Add(scrollClip);

            // 삽입선은 늘 배치해 두고 보임만 바꾼다. 처음 보일 때도 두께(resolvedStyle)를 바로 쓸 수 있다.
            dropLine = CreateOverlayPart("xeri-table__drop-line");
            dropLine.style.visibility = Visibility.Hidden;
            selectionLayer.Add(dropLine);
            body.Add(selectionLayer);
        }

    #endregion

    #region 열 구성

        // ------------------------------------------------------------
        /// <summary>
        /// 표시 열이 바뀌었을 때 모든 행 요소의 셀을 다시 만든다.
        /// </summary>
        // ------------------------------------------------------------
        public void BuildCells(bool showHandle, bool showSelector)
        {
            this.showHandle = showHandle;
            this.showSelector = showSelector;
            foreach (var view in rowPool)
            {
                view.BuildCells(model.Columns.VisibleColumns, showHandle, showSelector);
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 계산된 열 기하를 고정·스크롤 영역과 행 요소에 반영한다.
        /// </summary>
        // ------------------------------------------------------------
        public void ApplyGeometry()
        {
            frozenPane.style.width = geometry.FrozenWidth;
            frozenPane.style.display = geometry.FrozenWidth > 0f ? DisplayStyle.Flex : DisplayStyle.None;
            scrollRows.style.width = geometry.ScrollContentWidth;

            foreach (var view in rowPool)
            {
                view.ApplyGeometry(geometry, geometry.ScrollContentWidth);
            }

            UpdateSelectionOutline();
            UpdateDropLine();
        }

    #endregion

    #region 스크롤

        // ------------------------------------------------------------
        /// <summary>
        /// 스크롤을 처음 위치로 되돌린다.
        /// </summary>
        // ------------------------------------------------------------
        public void ResetScroll()
        {
            scrollPane.scrollOffset = Vector2.zero;
        }

        private void HandleScroll()
        {
            var offset = scrollPane.scrollOffset;
            frozenRows.style.translate = new Translate(0f, -offset.y);
            Scrolled?.Invoke(offset);
            Refresh();
        }

        private void HandleViewportResized()
        {
            var height = scrollPane.contentViewport.layout.height;
            if (float.IsNaN(height))
            {
                return;
            }

            // 가로 스크롤바가 생기면 보이는 높이가 줄어드므로 고정 영역도 같은 높이로 자른다.
            frozenPane.style.height = height;
            Resized?.Invoke();
            Refresh();
            ApplyPendingReveal();
        }

        private void HandleFrozenWheel(WheelEvent evt)
        {
            // 고정 영역은 스크롤을 갖지 않으므로 휠을 스크롤 영역으로 넘긴다.
            // 맨 위·맨 아래에서 더 갈 수 없으면 부모(Inspector 등)가 스크롤하도록 넘긴다.
            var before = scrollPane.scrollOffset;
            var offset = before;
            offset.y += evt.delta.y * scrollPane.mouseWheelScrollSize;
            scrollPane.scrollOffset = offset;
            if (scrollPane.scrollOffset != before)
            {
                evt.StopPropagation();
            }
        }

        // --------------------------------------------------------------------------------
        /// <summary>
        /// <br/> 셀이 보이도록 가로·세로 스크롤을 옮긴다.
        /// <br/> 배치 전이거나 스크롤 범위가 아직 새 행 수를 반영하지 않아 목표에 닿지 못하면,
        /// <br/> 다음 배치(스크롤 범위 갱신) 뒤에 한 번 더 맞춘다. 호스트가 틱을 미룰 필요가 없다.
        /// </summary>
        // --------------------------------------------------------------------------------
        public void ScrollIntoView(int rowIndex, int columnIndex)
        {
            if (rowIndex >= 0 && !geometry.IsInWindow(rowIndex))
            {
                WindowChangeRequested?.Invoke(rowIndex);
            }

            revealRow = rowIndex;
            revealColumn = columnIndex;
            hasPendingReveal = !TryScrollTo(rowIndex, columnIndex);
        }

        private bool TryScrollTo(int rowIndex, int columnIndex)
        {
            var viewport = scrollPane.contentViewport.layout;
            if (float.IsNaN(viewport.width) || float.IsNaN(viewport.height))
            {
                return false;
            }

            var offset = scrollPane.scrollOffset;
            var target = new Vector2
            (
                geometry.GetScrollXToShow(columnIndex, offset.x, viewport.width),
                geometry.GetScrollYToShow(rowIndex, offset.y, viewport.height)
            );

            if (target != offset)
            {
                scrollPane.scrollOffset = target;
            }

            return scrollPane.scrollOffset == target;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 배치 뒤 스크롤 범위가 갱신되면 닿지 못한 스크롤을 한 번 더 맞춘다.
        /// </summary>
        // ------------------------------------------------------------
        private void ApplyPendingReveal()
        {
            if (!hasPendingReveal)
            {
                return;
            }

            hasPendingReveal = false;
            TryScrollTo(revealRow, revealColumn);
        }

        // --------------------------------------------------------------------------------
        /// <summary>
        /// <br/> 끌기 중 포인터가 행 영역 밖이면 그 방향으로 조금 스크롤한다. 스크롤했으면 true다.
        /// <br/> 왼쪽은 고정 영역까지 벗어났을 때만 스크롤한다.
        /// <br/> 그래서 고정 열을 고르는 동안 화면이 밀리지 않는다.
        /// </summary>
        // --------------------------------------------------------------------------------
        public bool AutoScroll(Vector2 position)
        {
            var area = scrollPane.contentViewport.worldBound;
            var delta = new Vector2(GetHorizontalAutoScrollDelta(position.x), 0f);
            if (position.y > area.yMax)
            {
                delta.y = AutoScrollStep(position.y - area.yMax);
            }
            else if (position.y < area.yMin)
            {
                delta.y = -AutoScrollStep(area.yMin - position.y);
            }

            if (delta == Vector2.zero)
            {
                return false;
            }

            var before = scrollPane.scrollOffset;
            scrollPane.scrollOffset = before + delta;
            return scrollPane.scrollOffset != before;
        }

        // --------------------------------------------------------------------------------
        /// <summary>
        /// <br/> 패널 y가 행 영역 위·아래 가장자리 띠(inset) 안이나 그 밖이면 방향(-1·1)을 준다.
        /// <br/> 아니면 0이다. 행 끌기의 자동 스크롤·페이지 넘김 판단에 쓴다.
        /// </summary>
        // --------------------------------------------------------------------------------
        public int GetVerticalEdge(float y, float inset)
        {
            var area = scrollPane.contentViewport.worldBound;
            if (y < area.yMin + inset)
            {
                return -1;
            }

            return y > area.yMax - inset ? 1 : 0;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> 패널 y가 행 영역 위·아래 가장자리 띠(inset) 안이나 그 밖이면
        /// <br/> 그 방향으로 조금 스크롤한다. 가로는 움직이지 않는다.
        /// <br/> 스크롤했으면 true다(끝에 닿았으면 false).
        /// </summary>
        // ------------------------------------------------------------
        public bool AutoScrollVertical(float y, float inset)
        {
            var area = scrollPane.contentViewport.worldBound;
            var delta = 0f;
            if (y < area.yMin + inset)
            {
                delta = -AutoScrollStep(area.yMin + inset - y);
            }
            else if (y > area.yMax - inset)
            {
                delta = AutoScrollStep(y - (area.yMax - inset));
            }

            if (delta == 0f)
            {
                return false;
            }

            var before = scrollPane.scrollOffset;
            scrollPane.scrollOffset = new Vector2(before.x, before.y + delta);
            return scrollPane.scrollOffset != before;
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> 패널 x가 행 영역 왼쪽·오른쪽 밖이면 그 방향으로 조금 스크롤한다.
        /// <br/> 세로는 움직이지 않는다. 헤더 끌기(열 범위 선택·열 이동)에 쓴다.
        /// <br/> 스크롤했으면 true다.
        /// </summary>
        // ----------------------------------------------------------------------
        public bool AutoScrollHorizontal(float x)
        {
            var deltaX = GetHorizontalAutoScrollDelta(x);
            if (deltaX == 0f)
            {
                return false;
            }

            var before = scrollPane.scrollOffset;
            scrollPane.scrollOffset = new Vector2(before.x + deltaX, before.y);
            return scrollPane.scrollOffset != before;
        }

        private float GetHorizontalAutoScrollDelta(float x)
        {
            // 왼쪽은 고정 영역까지 벗어났을 때만 스크롤해 고정 열 위에서는 화면이 밀리지 않는다.
            var area = scrollPane.contentViewport.worldBound;
            var left = geometry.FrozenWidth > 0f ? frozenPane.worldBound.xMin : area.xMin;
            if (x > area.xMax)
            {
                return AutoScrollStep(x - area.xMax);
            }

            return x < left ? -AutoScrollStep(left - x) : 0f;
        }

        private static float AutoScrollStep(float distance)
        {
            // 많이 벗어날수록 빨리 움직이되 한 번에 너무 멀리 가지 않게 한다.
            return Mathf.Clamp(distance * 0.5f, 4f, 40f);
        }

    #endregion

    #region 위치 찾기

        // --------------------------------------------------------------------------------
        /// <summary>
        /// <br/> 본문 영역에 온전히 들어가는 행 수(최소 1). PageUp·PageDown 이동량이고
        /// <br/> PageSize 0인 Paged의 페이지 크기다. 연결 전에도 맞도록 옵션의 행 높이로 센다.
        /// <br/> 스크롤 영역(ScrollView)이 아니라 본문(body) 높이에서 가로 스크롤바만 뺀 값으로 센다.
        /// <br/> Paged의 스크롤 영역은 페이지 행 수만큼의 내용 높이로 줄어들어 있으므로, 그 높이를 기준으로 하면
        /// <br/> 창을 키워도 이전 페이지 크기에서 늘어나지 못한다.
        /// </summary>
        // --------------------------------------------------------------------------------
        public int PageRowCount => Math.Max(1, (int)Math.Floor(BodyHeight / options.RowHeight));

        // ------------------------------------------------------------
        /// <summary>
        /// 행이 놓일 본문 영역의 높이(가로 스크롤바 제외). 배치 전이면 0이다.
        /// </summary>
        // ------------------------------------------------------------
        private float BodyHeight
        {
            get
            {
                var height = scrollPane.parent.layout.height;
                if (float.IsNaN(height))
                {
                    return 0f;
                }

                var scroller = scrollPane.horizontalScroller;
                var scrollerHeight = scroller.layout.height;
                if (scroller.resolvedStyle.display != DisplayStyle.None && !float.IsNaN(scrollerHeight))
                {
                    height -= scrollerHeight;
                }

                return Math.Max(0f, height);
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 화면 맨 위에 보이는 행 위치. 행이 없으면 -1이다.
        /// </summary>
        // ------------------------------------------------------------
        public int FirstVisibleRow => geometry.FindRowAt(scrollPane.scrollOffset.y);

        // --------------------------------------------------------------------------------
        /// <summary>
        /// <br/> 패널 좌표에 있는 셀의 행·표시 열 위치를 찾는다. 영역 밖이면 가장 가까운 셀이다.
        /// <br/> 끌기 중 포인터가 표 밖으로 나가도 가장자리 셀로 범위를 잇는 데 쓴다.
        /// </summary>
        // --------------------------------------------------------------------------------
        public bool TryFindCell(Vector2 position, out int rowIndex, out int columnIndex)
        {
            var area = scrollPane.contentViewport.worldBound;
            var offset = scrollPane.scrollOffset;
            var inFrozen = geometry.HasFrozenColumns && position.x < area.xMin;

            columnIndex = inFrozen
                ? geometry.FindColumnAt(position.x - frozenPane.worldBound.xMin, true)
                : geometry.FindColumnAt(position.x - area.xMin + offset.x, false);
            rowIndex = geometry.FindRowAt(position.y - area.yMin + offset.y);

            return rowIndex >= 0 && columnIndex >= 0;
        }

        // --------------------------------------------------------------------------------
        /// <summary>
        /// <br/> 패널 y에 가장 가까운 행 경계(행 끌기의 삽입 자리)를 찾는다. 경계 k는 행 k 바로 위다.
        /// <br/> 영역 밖이면 보이는 구간의 맨 위·맨 아래 경계다. 구간이 비었으면 false다.
        /// </summary>
        // --------------------------------------------------------------------------------
        public bool TryFindRowGap(float y, out int gap)
        {
            var area = scrollPane.contentViewport.worldBound;
            gap = geometry.FindGapAt(y - area.yMin + scrollPane.scrollOffset.y);
            return gap >= 0;
        }

    #endregion

    #region 행 연결

        // ------------------------------------------------------------
        /// <summary>
        /// 보이는 행을 다시 연결한다.
        /// </summary>
        // ------------------------------------------------------------
        public void Refresh()
        {
            BindVisibleRows();
            UpdateSelectionOutline();
            UpdateDropLine();
            RowsBound?.Invoke();
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> 다음 Refresh에서 같은 행 스냅샷이라도 셀 값을 다시 연결하게 한다.
        /// <br/> 항목 값이 바뀌었을 수 있을 때 쓴다(ApplyEditResults).
        /// <br/> 요소를 떼거나 숨기지 않으므로 Inline 컨트롤의 포커스가 유지된다.
        /// </summary>
        // ----------------------------------------------------------------------
        public void InvalidateBindings()
        {
            foreach (var view in rowPool)
            {
                view.Invalidate();
            }
        }

        private void BindVisibleRows()
        {
            var rows = model.Rows;
            geometry.SetRows(options.RowHeight, rows.RowCount, windowStart, windowCount);
            scrollRows.style.height = geometry.ContentHeight;
            frozenRows.style.height = geometry.ContentHeight;

            var viewport = scrollPane.contentViewport.layout;
            var viewportHeight = float.IsNaN(viewport.height) ? 0f : viewport.height;
            var viewportWidth = float.IsNaN(viewport.width) ? 0f : viewport.width;
            var offset = scrollPane.scrollOffset;

            var capacity = geometry.GetViewportCapacity(viewportHeight);
            var poolSize = Math.Min(capacity + options.OverscanRows * 2, options.MaxRenderedRows);
            EnsurePool(poolSize);

            var (first, last) = geometry.GetVisibleRows(offset.y, viewportHeight, options.OverscanRows);
            last = Math.Min(last, first + poolSize);

            // 연결이 풀린 요소(열을 다시 만들면 Index가 -1)도 이번에 연결하지 않으면 숨긴다.
            // 남겨 두면 행 수가 줄었을 때 끝 너머 자리에 빈 행이 보인다.
            foreach (var view in rowPool)
            {
                if (view.Index < first || view.Index >= last)
                {
                    view.Unbind();
                    SetViewVisible(view, false);
                }
            }

            for (var index = first; index < last && poolSize > 0; index++)
            {
                var view = rowPool[index % poolSize];
                var row = rows.GetRow(index);
                SetViewVisible(view, true);
                view.Bind(index, row, rows.GetDataRowNumber(index, row), geometry.GetRowTop(index));
                if (row.Kind == TableGridRowKind.GroupHeader)
                {
                    view.SetGroupTitleOffset(offset.x, viewportWidth);
                }

                ApplySelectionMarks(view);
            }
        }

        private void EnsurePool(int size)
        {
            while (rowPool.Count < size)
            {
                var view = new TableGridRowView(commands);
                view.CellPointerDown += (row, column, evt) => CellPointerDown?.Invoke(row.Index, column, evt);
                view.InlinePointerDown += (row, column, evt) => InlinePointerDown?.Invoke(row.Index, column, evt);
                view.SelectorPointerDown += (row, evt) => SelectorPointerDown?.Invoke(row.Index, evt);
                view.ReorderHandlePointerDown += (row, evt) => ReorderHandlePointerDown?.Invoke(row.Index, evt);
                view.BuildCells(model.Columns.VisibleColumns, showHandle, showSelector);
                view.ApplyGeometry(geometry, geometry.ScrollContentWidth);
                frozenRows.Add(view.FrozenPart);
                scrollRows.Add(view.ScrollPart);
                SetViewVisible(view, false);
                rowPool.Add(view);
            }

            // 풀이 줄면 나머지 자리의 인덱스 배정이 바뀌므로 모두 다시 연결한다.
            if (rowPool.Count > size)
            {
                for (var index = rowPool.Count - 1; index >= size; index--)
                {
                    var view = rowPool[index];
                    view.Unbind();
                    view.FrozenPart.RemoveFromHierarchy();
                    view.ScrollPart.RemoveFromHierarchy();
                    rowPool.RemoveAt(index);
                }

                foreach (var view in rowPool)
                {
                    view.Unbind();
                    SetViewVisible(view, false);
                }
            }
        }

        private static void SetViewVisible(TableGridRowView view, bool visible)
        {
            var display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            view.FrozenPart.style.display = display;
            view.ScrollPart.style.display = display;
        }

    #endregion

    #region 셀 편집기

        // ------------------------------------------------------------
        /// <summary>
        /// 연결된 행의 셀에 편집기를 놓는다. 그 행이 보이지 않으면 false다.
        /// </summary>
        // ------------------------------------------------------------
        public bool AttachEditor(int rowIndex, int columnIndex, VisualElement editor)
        {
            DetachEditor();
            var view = FindBoundView(rowIndex);
            if (view == null || columnIndex < 0 || columnIndex >= view.CellCount)
            {
                return false;
            }

            view.ShowEditor(columnIndex, editor);
            editorView = view;
            return true;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 놓아 둔 편집기를 뗀다.
        /// </summary>
        // ------------------------------------------------------------
        public void DetachEditor()
        {
            editorView?.HideEditor();
            editorView = null;
        }

        // --------------------------------------------------------------------------------
        /// <summary>
        /// 연결된 행의 셀 안 첫 컨트롤에 포커스를 준다. 그 행이 보이지 않거나 없으면 false다.
        /// </summary>
        // --------------------------------------------------------------------------------
        public bool FocusCellControl(int rowIndex, int columnIndex)
        {
            var view = FindBoundView(rowIndex);
            return view != null && view.FocusControl(columnIndex);
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// 연결된 행의 셀 경계(월드 좌표)를 찾는다. 그 행이 보이지 않으면 false다.
        /// </summary>
        // ----------------------------------------------------------------------
        public bool TryGetCellBounds(int rowIndex, int columnIndex, out Rect bounds)
        {
            bounds = default;
            var view = FindBoundView(rowIndex);
            return view != null && view.TryGetCellBounds(columnIndex, out bounds);
        }

        private TableGridRowView FindBoundView(int rowIndex)
        {
            if (rowIndex < 0)
            {
                return null;
            }

            foreach (var view in rowPool)
            {
                if (view.Index == rowIndex)
                {
                    return view;
                }
            }

            return null;
        }

    #endregion

    #region 측정

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> 열 내용 맞춤에 쓸 셀 글자 요소와 둘레 폭을 찾는다.
        /// <br/> 연결된 데이터 행 중 하나의 셀을 쓰며, 보이는 데이터 행이 없으면 false다.
        /// </summary>
        // ----------------------------------------------------------------------
        public bool TryGetCellMeasurer(int columnIndex, out TextElement text, out float chrome)
        {
            foreach (var view in rowPool)
            {
                if (view.Index >= 0 && view.Row != null && view.Row.IsData
                    && view.TryGetCellText(columnIndex, out text, out chrome))
                {
                    return true;
                }
            }

            text = null;
            chrome = 0f;
            return false;
        }

    #endregion

    #region 선택 표시

        // ------------------------------------------------------------
        /// <summary>
        /// 연결된 모든 행에 현재 선택 표시를 적용한다.
        /// </summary>
        // ------------------------------------------------------------
        public void ApplySelectionMarks()
        {
            foreach (var view in rowPool)
            {
                if (view.Index >= 0)
                {
                    ApplySelectionMarks(view);
                }
            }

            UpdateSelectionOutline();
        }

        private void ApplySelectionMarks(TableGridRowView view)
        {
            var selection = model.Selection;
            var isData = view.Row != null && view.Row.IsData;
            view.SetRowSelected(isData && selection.IsRowSelected(view.Row.ID));
            view.SetRowInRange(isData && selection.HasCellRange && view.Index >= selection.RowMin && view.Index <= selection.RowMax);
            view.SetRowCut(isData && model.Edit.IsCutRow(view.Row.ID));
            view.SetRowDragging(isData && draggingRowIDs.Contains(view.Row.ID));

            var columns = model.Columns.VisibleColumns;
            var edit = model.Edit;
            for (var column = 0; column < view.CellCount; column++)
            {
                var marks = isData ? selection.GetCellMarks(view.Index, column) : TableGridCellMarks.None;

                // 붙여넣기에서 거부된 셀은 잠깐 강조하고, 잘라낸 셀은 옮길 때까지 흐리게 한다.
                var columnID = isData && column < columns.Count ? columns[column].ID : null;
                if (columnID != null && edit.HasRejected && edit.IsRejected(view.Row.ID, columnID))
                {
                    marks |= TableGridCellMarks.Rejected;
                }

                if (columnID != null && edit.IsCutCell(view.Row.ID, columnID))
                {
                    marks |= TableGridCellMarks.Cut;
                }

                view.SetCellMarks(column, marks);
            }
        }

        // --------------------------------------------------------------------------------
        /// <summary>
        /// <br/> 셀 범위(또는 이어진 열 선택) 외곽선을 행·고정선 위 층에 그린다.
        /// <br/> 데이터 행이 이어진 구간마다 따로 그려, 그룹 머리·요약 행처럼 선택되지 않는 행에서는 끊긴다.
        /// <br/> 각 구간은 고정 영역과 스크롤 영역에 한 조각씩 두고, 범위가 두 영역에 걸치면 맞닿는 변을 비운다.
        /// <br/> 위·왼쪽 변은 앞 행의 가로선과 고정선 위에 겹쳐 놓아 선이 두 겹으로 보이지 않게 한다.
        /// </summary>
        // --------------------------------------------------------------------------------
        private void UpdateSelectionOutline()
        {
            var viewport = scrollPane.contentViewport.layout;
            var viewportWidth = float.IsNaN(viewport.width) ? 0f : viewport.width;
            var viewportHeight = float.IsNaN(viewport.height) ? 0f : viewport.height;
            var frozenWidth = geometry.FrozenWidth;
            var clipLeft = Math.Max(0f, frozenWidth - GRID_LINE_WIDTH);

            selectionLayer.style.width = frozenWidth + viewportWidth;
            selectionLayer.style.height = viewportHeight;
            scrollClip.style.left = clipLeft;
            scrollClip.style.width = frozenWidth + viewportWidth - clipLeft;

            if (!TryGetOutlineRange(out var rangeRowMin, out var rangeRowMax, out var first, out var last)
                || last >= geometry.ColumnCount)
            {
                HideOutlines(0, 0);
                return;
            }

            // 페이지 구간 밖으로 나간 부분은 그리지 않는다.
            var rowMin = Math.Max(rangeRowMin, geometry.WindowStart);
            var rowMax = Math.Min(rangeRowMax, geometry.WindowStart + geometry.WindowCount - 1);
            if (rowMin > rowMax)
            {
                HideOutlines(0, 0);
                return;
            }

            model.Rows.CollectDataRuns(rowMin, rowMax, outlineRuns);
            var offset = scrollPane.scrollOffset;
            var frozenCount = geometry.FrozenCount;
            var spansBoth = first < frozenCount && last >= frozenCount;
            var frozenUsed = 0;
            var scrollUsed = 0;

            foreach (var (runFirst, runLast) in outlineRuns)
            {
                var lineAbove = runFirst > geometry.WindowStart ? GRID_LINE_WIDTH : 0f;
                var top = geometry.GetRowTop(runFirst) - offset.y - lineAbove;
                var height = geometry.GetRowTop(runLast + 1) - offset.y - top;

                // 화면 밖 구간은 조각을 만들지 않는다(배치 전에는 높이를 모르므로 거르지 않는다).
                if (viewportHeight > 0f && (top + height < 0f || top > viewportHeight))
                {
                    continue;
                }

                if (first < frozenCount)
                {
                    // 마지막 고정 열까지 가면 오른쪽 변을 고정선 위에 둔다.
                    var end = Math.Min(last, frozenCount - 1);
                    var left = geometry.GetOffset(first);
                    var right = end == frozenCount - 1 ? frozenWidth : geometry.GetOffset(end) + geometry.GetWidth(end);
                    var outline = GetOutline(frozenOutlines, frozenUsed++, selectionLayer);
                    PlaceOutline(outline, left, top, right - left, height);
                    outline.style.borderRightWidth = spansBoth ? new StyleFloat(0f) : new StyleFloat(StyleKeyword.Null);
                }

                if (last >= frozenCount)
                {
                    // 첫 스크롤 열에서 시작하면 왼쪽 변을 고정선 위로 한 칸 당긴다.
                    var start = Math.Max(first, frozenCount);
                    var left = geometry.GetOffset(start) - offset.x;
                    var right = geometry.GetOffset(last) + geometry.GetWidth(last) - offset.x;
                    if (start == frozenCount && frozenWidth > 0f)
                    {
                        left -= GRID_LINE_WIDTH;
                    }

                    var shift = frozenWidth - clipLeft;
                    var outline = GetOutline(scrollOutlines, scrollUsed++, scrollClip);
                    PlaceOutline(outline, left + shift, top, right - left, height);
                    outline.style.borderLeftWidth = spansBoth ? new StyleFloat(0f) : new StyleFloat(StyleKeyword.Null);
                }
            }

            HideOutlines(frozenUsed, scrollUsed);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 쓰는 조각 수(frozenUsed·scrollUsed) 뒤의 외곽선 조각을 숨긴다.
        /// </summary>
        // ------------------------------------------------------------
        private void HideOutlines(int frozenUsed, int scrollUsed)
        {
            for (var index = frozenUsed; index < frozenOutlines.Count; index++)
            {
                frozenOutlines[index].style.display = DisplayStyle.None;
            }

            for (var index = scrollUsed; index < scrollOutlines.Count; index++)
            {
                scrollOutlines[index].style.display = DisplayStyle.None;
            }
        }

        // --------------------------------------------------------------------------------
        /// <summary>
        /// <br/> 외곽선 조각 목록에서 index번째 조각을 돌려준다. 모자라면 만들어 parent에 단다.
        /// <br/> 고정 영역 조각은 스크롤 영역 덮개(scrollClip)보다 아래에 둔다.
        /// </summary>
        // --------------------------------------------------------------------------------
        private VisualElement GetOutline(List<VisualElement> pool, int index, VisualElement parent)
        {
            while (pool.Count <= index)
            {
                var outline = CreateOverlayPart("xeri-table__selection-outline");
                if (parent == selectionLayer)
                {
                    parent.Insert(parent.IndexOf(scrollClip), outline);
                }
                else
                {
                    parent.Add(outline);
                }

                pool.Add(outline);
            }

            return pool[index];
        }

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> 외곽선을 그릴 행·표시 열 범위를 구한다. 셀 범위는 그 범위,
        /// <br/> 이어진 열 선택은 모든 행에 걸친 범위다. 없으면 false다.
        /// </summary>
        // ------------------------------------------------------------
        private bool TryGetOutlineRange(out int rowMin, out int rowMax, out int columnMin, out int columnMax)
        {
            var selection = model.Selection;
            if (selection.HasCellRange)
            {
                rowMin = selection.RowMin;
                rowMax = selection.RowMax;
                columnMin = selection.ColumnMin;
                columnMax = selection.ColumnMax;
                return true;
            }

            var positions = selection.ColumnPositions;
            rowMin = 0;
            rowMax = model.Rows.RowCount - 1;
            columnMin = positions.Count > 0 ? positions[0] : -1;
            columnMax = positions.Count > 0 ? positions[positions.Count - 1] : -1;
            return selection.IsColumnRun && rowMax >= 0;
        }

        private static void PlaceOutline(VisualElement outline, float left, float top, float width, float height)
        {
            outline.style.display = DisplayStyle.Flex;
            outline.style.left = left;
            outline.style.top = top;
            outline.style.width = Math.Max(0f, width);
            outline.style.height = Math.Max(0f, height);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 끌어서 옮기는 중인 행을 흐리게 표시한다. null이면 표시를 지운다.
        /// </summary>
        // ------------------------------------------------------------
        public void SetDraggingRows(IEnumerable<string> rowIDs)
        {
            draggingRowIDs.Clear();
            if (rowIDs != null)
            {
                draggingRowIDs.UnionWith(rowIDs);
            }

            ApplySelectionMarks();
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> 행 경계 gap(행 gap 바로 위)에 삽입선을 보인다. 음수면 숨긴다.
        /// <br/> 스크롤·배치가 바뀌면 같은 경계에 다시 놓는다.
        /// </summary>
        // ----------------------------------------------------------------------
        public void ShowDropLine(int gap)
        {
            dropGap = gap;
            UpdateDropLine();
        }

        private void UpdateDropLine()
        {
            var inWindow = dropGap >= geometry.WindowStart && dropGap <= geometry.WindowStart + geometry.WindowCount;
            var viewport = scrollPane.contentViewport.layout;
            var viewportHeight = float.IsNaN(viewport.height) ? 0f : viewport.height;
            var thickness = dropLine.resolvedStyle.height;
            thickness = float.IsNaN(thickness) ? 0f : thickness;

            // 경계를 선 가운데에 두되, 맨 위·맨 아래 경계의 선도 잘리지 않게 보이는 영역 안으로 맞춘다.
            var top = geometry.GetRowTop(dropGap) - scrollPane.scrollOffset.y - thickness * 0.5f;
            var visible = dropGap >= 0 && inWindow && top > -thickness && top < viewportHeight;
            dropLine.style.visibility = visible ? Visibility.Visible : Visibility.Hidden;
            if (visible)
            {
                dropLine.style.top = Mathf.Clamp(top, 0f, Mathf.Max(0f, viewportHeight - thickness));
            }
        }

        private static VisualElement CreateOverlayPart(string className)
        {
            var element = new VisualElement
            {
                pickingMode = PickingMode.Ignore,
            };
            element.AddToClassList(className);
            return element;
        }

    #endregion

    }
}
