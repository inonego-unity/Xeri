/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TableGrid.cs
수정일 : 2026-11-23

# 설명
UI Toolkit 범용 표의 공개 진입점. 상태(TableGridModel), 기하(TableGridGeometry),
헤더(TableGridHeaderView), 본문(TableGridViewport), 상태 표시(TableGridStatusBar),
입력(TableGridPointerInput, TableGridKeyboardInput), 팝오버(TableGridPopover),
본문 우클릭 메뉴(Xeri Core XeriContextMenu), 휠 클릭 자동 스크롤(Xeri Core UITKAutoScrollManipulator)을
조립하고 이벤트를 중계한다.
조회·필터·정렬·저장은 호스트가 맡고, 표는 표시·입력과 요청 이벤트만 맡는다.
셀 구성(값 규칙·셀 뷰·편집 방식)은 열(TableGridColumn)이 맡는다. Editor 편집은 TableGridCellEditing이,
Inline 값 변경·Detail 요청은 셀 호스트가 TableGridCellCommands를 거쳐 EditRequested·DetailRequested로 보낸다.
복사는 TSV와 함께 셀 값 묶음을 보관하고, 같은 TSV를 붙여넣으면 값 묶음으로 요청한다.
붙여넣기(Ctrl+V)는 늘 덮어쓴다. 잘라내기는 복사에 잘라낸 표시를 더한 것이고, 같은 표에 붙여넣으면
덮어쓰기와 원본 비우기를 한 요청(EditSource.Cut)으로 보내 값을 옮긴다. 다른 곳에 붙여넣으면 복사처럼 원본은 그대로다.
삽입 붙여넣기(우클릭)는 클립보드 행을 고른 행 위에 넣는다. 복사한 행은 새 행(AddRowRequested, 행 값 포함),
같은 표에서 잘라낸 행은 행 이동(MoveRowsRequested)이다.
헤더 왼쪽 클릭은 List에서 정렬, 열을 고를 수 있는 표(Workspace + Cell)에서 열 선택이고, 열 메뉴는 Workspace 헤더 우클릭으로만 연다.
열 이동(고른 헤더 끌기·Ctrl+Alt+←/→)은 보기 순서만 바꾸며 열 패널과 같이 SetVisibleColumns 경로로 보기 상태에 남는다.

# 특이사항, 제약사항
기하(열 폭·위치, 보이는 행 범위)는 TableGridGeometry 한곳에서 계산하고 헤더·본문이 같은 결과를 쓴다.
모양(색·여백·구분선·선택 표시)은 USS가 정하고, C#은 폭·세로 위치와 상태 클래스만 지정한다.
옵션은 생성 시 검증하며, 바꾼 옵션은 Refresh 때 반영한다.
Core 안 호스트(Collection Editor)용 연결부(HostContextMenu·SelectRow·ActiveColumnID)는 internal로만 둔다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UIElements;

using inonego;
using inonego.Xeri;
using inonego.Xeri.UI;
using inonego.Xeri.UI.AutoScroll;
using inonego.Xeri.Utility;

namespace inonego.Xeri.UI.TableGrid
{
    // ============================================================
    /// <summary>
    /// UI Toolkit 범용 표.
    /// </summary>
    // ============================================================
    [UxmlElement]
    public partial class TableGrid : VisualElement
    {

    #region 상수

        // ------------------------------------------------------------
        /// <summary>
        /// 표 루트 요소의 USS 클래스.
        /// </summary>
        // ------------------------------------------------------------
        public static readonly string UssClassName = "xeri-table";

        private const float HANDLE_WIDTH = 20f;
        private const float SELECTOR_MIN_WIDTH = 28f;
        private const float SELECTOR_DIGIT_WIDTH = 8f;
        private const float SELECTOR_PADDING = 14f;
        private const int AUTO_FIT_SAMPLE_ROWS = 500;

    #endregion

    #region 필드

        private readonly TableGridModel model = new();
        private readonly TableGridGeometry geometry = new();
        private readonly List<TableGridColumnLayout> effectiveLayouts = new();

        private readonly VisualElement headerRow;
        private readonly VisualElement statusRow;
        private readonly VisualElement body;
        private readonly VisualElement headerScrollContent;
        private readonly VisualElement headerGap;
        private readonly TableGridHeaderView header;
        private readonly TableGridViewport viewport;
        private readonly TableGridStatusBar statusBar;
        private readonly TableGridPopover popover;
        private readonly XeriContextMenu bodyMenu;
        private readonly TableGridKeyboardInput keyboardInput;
        private readonly TableGridCellCommands cellCommands;
        private readonly TableGridCellEditing cellEditing;
        private int rowNumberDigits = 1;

        private ITableGridSource source;
        private bool columnsDirty = true;
        private bool showsReorderHandle;
        private bool applyingViewState;

        // 헤더 경계 끌기 한 번 동안 열 폭이 실제로 바뀌었는지. 끌기가 끝날 때 보기 상태 변경을 알릴지 정한다.
        private bool resizeChanged;
        private readonly Paginator paginator = new(1);
        private readonly PageNavigator pageNavigator;

    #endregion

    #region 속성

        // ------------------------------------------------------------
        /// <summary>
        /// 표시·조회 옵션. 바꾼 값은 Refresh 때 반영한다.
        /// </summary>
        // ------------------------------------------------------------
        public TableGridOptions Options { get; }

        // ----------------------------------------------------------------------
        /// <summary>
        /// 지금 옵션과 연결된 요청 이벤트에서 파생한 능력. 읽을 때마다 새로 계산한다.
        /// </summary>
        // ----------------------------------------------------------------------
        public TableGridCapabilities Capabilities => new TableGridCapabilities
        (
            canEdit: CanEdit,
            canDeleteRows: CanDeleteRows,
            canMoveRows: CanReorderRows,
            canSelectCellRange: Options.AllowsCellRange,
            canSelectRows: Options.AllowsRowSelection,
            canSortFromHeader: !Options.IsWorkspace && QueryRequested != null,
            hasColumnMenu: Options.IsWorkspace,
            canQueryFromMenu: Options.IsWorkspace && QueryRequested != null,
            hasJump: Options.IsWorkspace,
            canApplySortOrder: CanApplySortOrder,
            canSelectColumns: Options.AllowsColumnSelection,
            canAddRows: CanAddRows
        );

        // 능력 조건은 여기서 한 번만 정한다. 능력 스냅샷과 메뉴·요청이 같은 값을 읽는다.
        private bool CanEdit => Options.IsEditable && EditRequested != null;

        private bool CanDeleteRows => Options.IsEditable && DeleteRowsRequested != null;

        private bool CanAddRows => Options.IsEditable && AddRowRequested != null;

        // ------------------------------------------------------------
        /// <summary>
        /// 원본 순서의 열 목록.
        /// </summary>
        // ------------------------------------------------------------
        public IReadOnlyList<TableGridColumn> Columns => model.Columns.SourceColumns;

        // ------------------------------------------------------------
        /// <summary>
        /// 화면 순서의 표시 열 목록.
        /// </summary>
        // ------------------------------------------------------------
        public IReadOnlyList<TableGridColumn> VisibleColumns => model.Columns.VisibleColumns;

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> 왼쪽에 고정한 표시 열 수. 지정한 수가 표시 열 수를 넘으면
        /// <br/> 읽을 때 표시 열 수로 줄어든다.
        /// <br/> 0 미만은 지정할 수 없다.
        /// </summary>
        // ------------------------------------------------------------
        public int FrozenColumnCount
        {
            get => model.Columns.FrozenCount;
            set
            {
                model.Columns.FrozenCount = value;
                RaiseViewStateChanged();
            }
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> 현재 선택. 선택이 없으면 TableGridSelection.None이다.
        /// <br/> 지정하면 표시를 갱신하고 바뀌었을 때 SelectionChanged를 알린다.
        /// <br/> 행 선택이면 키보드 이동이 그 선택에서 이어지도록 기준 행을,
        /// <br/> 열 선택이면 기준 열과 활성 행을 맞춘다.
        /// </summary>
        // ----------------------------------------------------------------------
        public TableGridSelection Selection
        {
            get => model.Selection.Current;
            set
            {
                model.AlignAnchors(value);
                ApplySelection(value);
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> 헤더·칩에 보이는 쿼리 표시 상태. 실제 조회는 호스트가 한다.
        /// <br/> 지정해도 조회하지 않고 표시만 바꾼다. null이면 빈 상태다.
        /// </summary>
        // ------------------------------------------------------------
        public TableGridQueryState QueryState
        {
            get => model.Query;
            set
            {
                model.Query = value;
                header.ApplyQuery(model.Query);
                ApplyReorderHandleState();
                QueryStateChanged?.Invoke(this, EventArgs.Empty);
                RaiseViewStateChanged();
            }
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// 표가 다루는 행 수(소스 결과 행 수, 그룹 머리 행 포함). 위치의 범위다.
        /// </summary>
        // ----------------------------------------------------------------------
        public int RowCount => model.Rows.RowCount;

        // ------------------------------------------------------------
        /// <summary>
        /// 데이터 행 수(그룹 머리 행은 세지 않는다). 사용자에게 보이는 개수다.
        /// </summary>
        // ------------------------------------------------------------
        public int DataRowCount => model.Rows.DataRowCount;

    #endregion

    #region 이벤트

        // ------------------------------------------------------------
        /// <summary>
        /// 선택이 바뀌었을 때 알린다.
        /// </summary>
        // ------------------------------------------------------------
        public event EventHandler<TableGridSelectionChanged> SelectionChanged;

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> 열 메뉴·쿼리 칩에서 정렬·필터·그룹 변경을 요청했을 때 알린다.
        /// <br/> 표는 조회하지 않는다. 호스트가 조회한 뒤 QueryState로 반영한다.
        /// </summary>
        // ----------------------------------------------------------------------
        public event EventHandler<TableGridQueryRequest> QueryRequested;

        // ------------------------------------------------------------
        /// <summary>
        /// QueryState로 쿼리 표시 상태가 바뀌었을 때 알린다.
        /// </summary>
        // ------------------------------------------------------------
        public event EventHandler QueryStateChanged;

        // --------------------------------------------------------------------------------
        /// <summary>
        /// <br/> 셀 편집·붙여넣기·지우기·잘라낸 셀 옮기기로 셀 값을 바꿔 달라고 할 때 알린다.
        /// <br/> 표는 데이터를 바꾸지 않는다. 반영한 변경은 ApplyEditResults나 Refresh로 보인다.
        /// </summary>
        // --------------------------------------------------------------------------------
        public event EventHandler<TableGridEditRequest> EditRequested;

        // ------------------------------------------------------------
        /// <summary>
        /// 선택한 행을 지워 달라고 할 때 알린다(행 선택에서 Delete).
        /// </summary>
        // ------------------------------------------------------------
        public event EventHandler<TableGridDeleteRowsRequest> DeleteRowsRequested;

        // ----------------------------------------------------------------------
        /// <summary>
        /// 행을 옮겨 달라고 할 때 알린다(끌기 손잡이·Alt+방향키·메뉴·잘라낸 행 삽입).
        /// </summary>
        // ----------------------------------------------------------------------
        public event EventHandler<TableGridMoveRowsRequest> MoveRowsRequested;

        // --------------------------------------------------------------------------------
        /// <summary>
        /// <br/> 행을 확정했을 때 알린다(더블클릭·Enter). ReadOnly 표에서는 늘, Editable 표에서는
        /// <br/> 활성 셀을 편집할 수 없을 때만 알린다(편집할 수 있으면 편집이 우선).
        /// </summary>
        // --------------------------------------------------------------------------------
        public event EventHandler<TableGridRowActivated> RowActivated;

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> 새 행을 넣어 달라고 할 때 알린다
        /// <br/> (우클릭 "위에·아래에 행 삽입"·"복사한 행 삽입", RequestAddRow).
        /// <br/> 복사한 행 삽입은 행 값(RowValues)을 함께 담는다.
        /// <br/> 끝에 추가하는 버튼과 추가 흐름은 호스트가 맡는다.
        /// </summary>
        // ----------------------------------------------------------------------
        public event EventHandler<TableGridAddRowRequest> AddRowRequested;

        // --------------------------------------------------------------------------------
        /// <summary>
        /// <br/> Detail 열 셀의 상세 편집을 열어 달라고 할 때 알린다.
        /// <br/> Enter·더블클릭이나 셀 호스트의 OpenDetail로 보낸다.
        /// <br/> 편집 화면과 반영은 호스트가 맡는다. 연결하지 않으면 Detail 셀은 편집할 수 없다.
        /// </summary>
        // --------------------------------------------------------------------------------
        public event EventHandler<TableGridDetailRequest> DetailRequested;

        // ------------------------------------------------------------
        /// <summary>
        /// Paged 모드에서 페이지가 바뀌었을 때 알린다.
        /// </summary>
        // ------------------------------------------------------------
        public event EventHandler PageChanged;

        // --------------------------------------------------------------------------------
        /// <summary>
        /// <br/> 열 표시·순서·폭·고정 수나 쿼리 표시 상태가 바뀐 뒤 한 번 알린다.
        /// <br/> 열 경계를 끄는 동안에는 알리지 않고 끌기를 마쳤을 때 알린다. 저장은 호스트가 한다.
        /// </summary>
        // --------------------------------------------------------------------------------
        public event EventHandler ViewStateChanged;

        // ----------------------------------------------------------------------
        /// <summary>
        /// 행을 다시 읽었거나 더 읽어 행 수가 바뀔 수 있을 때 알린다(검색창 결과 수).
        /// </summary>
        // ----------------------------------------------------------------------
        internal event Action RowsChanged;

    #endregion

    #region 생성과 수명

        // ------------------------------------------------------------
        /// <summary>
        /// 기본 옵션으로 표를 만든다. UXML에서 쓰는 생성자다.
        /// </summary>
        // ------------------------------------------------------------
        public TableGrid() : this(null)
        {
            // NONE
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 옵션을 검증해 표를 만든다. null이면 기본 옵션이다.
        /// </summary>
        // ------------------------------------------------------------
        public TableGrid(TableGridOptions options)
        {
            Options = options ?? new TableGridOptions();
            Options.Validate();

            AddToClassList(UssClassName);
            TableGridTheming.Attach(this, Options);
            styleSheets.Add(TableGridResources.Theme);
            styleSheets.Add(TableGridResources.Style);
            TableGridResources.Layout.CloneTree(this);

            // 셀 호스트(Inline·Detail)와 편집 처리기가 표에 요청을 보내는 통로. 본문보다 먼저 만든다.
            cellCommands = new TableGridCellCommands
            (
                CanEditColumn,
                edit => DispatchEdit(edit, TableGridEditSource.Inline),
                request => DetailRequested?.Invoke(this, request),
                error => statusBar?.ShowMessage(error, true),
                () => Focus()
            );

            headerRow = Require<VisualElement>("header");
            statusRow = Require<VisualElement>("status");

            // Paged 모드의 페이지 이동 컨트롤(Xeri Core PageNavigator, 모양 시트는 컨트롤이 직접 붙인다). 상태 줄 가운데 슬롯에 둔다.
            pageNavigator = new PageNavigator(paginator, TurnPage);
            Require<VisualElement>("page-slot").Add(pageNavigator);
            body = Require<VisualElement>("body");
            headerScrollContent = Require<VisualElement>("header-scroll-content");
            headerGap = Require<VisualElement>("header-gap");
            header = new TableGridHeaderView(Require<VisualElement>("header-frozen"), headerScrollContent, geometry);
            viewport = new TableGridViewport
            (
                Require<VisualElement>("frozen-pane"), Require<VisualElement>("frozen-rows"),
                Require<ScrollView>("scroll-pane"), Require<VisualElement>("scroll-rows"),
                model, geometry, Options, cellCommands
            );
            statusBar = new TableGridStatusBar
            (
                Require<Label>("state-label"), Require<Label>("range-label"),
                Require<Label>("message-label"), Require<Label>("selection-label")
            );

            // 입력은 표 요소의 이벤트에 붙어 표와 수명을 같이 하므로 따로 보관하지 않는다.
            var pointerInput = new TableGridPointerInput(this, model, viewport, header, Options, ApplySelection, PrepareRowDrag);
            pointerInput.ContextMenuRequested += OpenBodyMenu;
            pointerInput.CellDoubleClicked += BeginEditAfterPointer;
            pointerInput.RowsDropped += HandleRowsDropped;
            pointerInput.ColumnsDropped += HandleColumnsDropped;
            pointerInput.PageStepRequested += step => PageIndex += step;
            keyboardInput = new TableGridKeyboardInput(this, model, viewport, Options, ApplySelection, CancelCut);
            keyboardInput.CopyRequested += () => CopySelection(false);
            keyboardInput.CutRequested += () => CutSelection();
            keyboardInput.PasteRequested += () => PasteClipboard();
            keyboardInput.ClearRequested += HandleClearKey;
            keyboardInput.JumpRequested += OpenJump;
            keyboardInput.EditRequested += BeginEditOrActivate;
            keyboardInput.RowMoveRequested += move => RequestRowMove(move, TableGridMoveSource.Keyboard);
            keyboardInput.ColumnMoveRequested += MoveSelectedColumns;
            keyboardInput.PageTurnRequested += step => TurnPage
            (
                new PageNavigationRequest(step < 0 ? PageNavigationKind.Previous : PageNavigationKind.Next)
            );
            bodyMenu = new XeriContextMenu(this, null, XeriContextMenuStyle.Compact);

            // 휠 클릭 자동 스크롤(Xeri Core): 고정 열을 포함한 본문 어디서 눌러도 스크롤 영역을 움직인다.
            var scrollTarget = new UITKScrollViewTarget(Require<ScrollView>("scroll-pane"));
            body.AddManipulator(new UITKAutoScrollManipulator(scrollTarget));

            cellEditing = new TableGridCellEditing(this, model, viewport, Options, cellCommands);
            cellEditing.EditCommitted += HandleEditCommitted;
            cellEditing.Finished += HandleEditFinished;

            body.RegisterCallback<GeometryChangedEvent>(_ => RelayoutColumns());
            viewport.Scrolled += offset => headerScrollContent.style.translate = new Translate(-offset.x, 0f);

            // 스크롤하면 행 요소가 다른 행에 다시 쓰이므로 편집을 확정한다.
            viewport.Scrolled += _ => cellEditing.Commit(null);
            viewport.RowsBound += ShowRowStatus;

            // 페이지 구간은 행을 연결하기 전에 맞춘다(행 수는 HandleRowsChanged, 보이는 높이는 여기).
            viewport.Resized += ApplyPageWindow;
            viewport.WindowChangeRequested += ShowPageOf;

            header.ColumnResizing += (column, width) => resizeChanged |= ApplyColumnWidth(column.ID, width);
            header.ColumnResized += EndColumnResize;
            header.ColumnAutoFitRequested += column => AutoFitColumn(column.ID);
            header.HeaderClicked += HandleHeaderClicked;
            header.HeaderContextClicked += HandleHeaderContextClicked;

            popover = new TableGridPopover(this);
            popover.Closed += () => header.SetOpenColumn(null);
            popover.SortRequested += (column, direction) =>
                RequestQuery(model.Query.WithSort(column.ID, direction));
            popover.FilterApplied += (column, rule) =>
                RequestQuery(model.Query.WithFilter(column.ID, rule));
            popover.GroupRequested += (column, grouped) =>
                RequestQuery(model.Query.WithGroup(grouped ? column.ID : null));
            popover.FreezeRequested += (column, frozen) =>
                FrozenColumnCount = frozen ? model.Columns.GetVisibleIndex(column.ID) + 1 : 0;
            popover.HideRequested += HideColumn;
            popover.VisibleColumnsChanged += SetVisibleColumns;

            model.ColumnsChanged += HandleColumnsChanged;
            model.ColumnLayoutChanged += RelayoutColumns;
            model.RowsChanged += HandleRowsChanged;
            model.SelectionAdjusted += HandleSelectionAdjusted;

            RegisterCallback<AttachToPanelEvent>(HandleAttach);
            void HandleDetach(DetachFromPanelEvent evt)
            {
                cellEditing.Cancel(false);
                popover.Close();
                bodyMenu.Close();
            }

            RegisterCallback<DetachFromPanelEvent>(HandleDetach);

            ApplyDisplayOptions();
            ShowRowStatus();
        }

        private T Require<T>(string elementName) where T : VisualElement
        {
            return XeriUIResources.Require<T>(this, elementName, "TableGrid");
        }

        private void ApplyDisplayOptions()
        {
            headerRow.style.display = Options.ShowHeader ? DisplayStyle.Flex : DisplayStyle.None;
            statusRow.style.display = Options.ShowsStatusBar ? DisplayStyle.Flex : DisplayStyle.None;
            EnableInClassList("xeri-table--column-select", Options.AllowsColumnSelection);
        }

        private void HandleAttach(AttachToPanelEvent evt)
        {
            // 떨어져 있는 동안 바뀐 행을 다시 붙을 때 연결한다.
            if (source != null)
            {
                viewport.Refresh();
            }
        }

    #endregion

    #region 열과 데이터 연결

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> 원본 열을 지정한다. 셀 값은 이 순서로 들어온다.
        /// <br/> 남아 있는 열 ID의 표시 순서·사용자 폭은 유지한다.
        /// </summary>
        // ------------------------------------------------------------
        public void SetColumns(IReadOnlyList<TableGridColumn> columns)
        {
            model.Columns.SetColumns(columns);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 표시할 열과 순서를 지정한다. 숨긴 열의 폭은 유지한다.
        /// </summary>
        // ------------------------------------------------------------
        public void SetVisibleColumns(IReadOnlyList<string> columnIDs)
        {
            model.Columns.SetVisibleColumns(columnIDs);
            RaiseViewStateChanged();
        }

        // --------------------------------------------------------------------------------
        /// <summary>
        /// <br/> 데이터 소스를 바꾸고 다시 연결한다.
        /// <br/> 기본은 맨 위로 스크롤하고 선택을 해제한다.
        /// <br/> keepView면 스크롤 위치를 유지하고 선택은 선택 규칙대로 남긴다
        /// <br/> (행은 ID, 셀은 같은 자리). 행 삭제·되살리기처럼 같은 쿼리를 다시 조회할 때 쓴다.
        /// </summary>
        // --------------------------------------------------------------------------------
        public void SetSource(ITableGridSource newSource, bool keepView = false)
        {
            source = newSource;
            if (source is ITableGridSearchableSource searchable && !string.IsNullOrWhiteSpace(SearchText))
            {
                searchable.SetSearch(SearchText, CollectSearchColumns());
            }

            if (!keepView)
            {
                viewport.ResetScroll();
                CancelCut();
                Selection = TableGridSelection.None;
            }

            Refresh();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> 같은 소스의 결과를 다시 읽고 옵션 변경을 반영한다.
        /// <br/> 호스트가 데이터를 바꾼 뒤 부른다. 선택은 선택 규칙대로 남긴다.
        /// </summary>
        // ------------------------------------------------------------
        public void Refresh()
        {
            Options.Validate();
            columnsDirty = true;
            ApplyDisplayOptions();
            model.Rows.SetSource(source);
            RelayoutColumns();
            TableGridTheming.Apply(this, Options);
        }

    #endregion

    #region 열 폭

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> 열 폭을 지정한다. 고정 열은 그 px 폭이 되고,
        /// <br/> 가변 열은 지금 표 폭에서 그 폭이 되도록 비율(가중치)을 바꾼다.
        /// <br/> 이후에도 비율로 동작한다. null이면 열의 폭 정책으로 되돌린다.
        /// </summary>
        // ------------------------------------------------------------
        public void SetColumnWidth(string columnID, float? width)
        {
            // 실제로 바뀐 때만 보기 상태 변경을 알린다(가변 열 비율을 못 구했거나 같은 값이면 알리지 않는다).
            if (ApplyColumnWidth(columnID, width))
            {
                RaiseViewStateChanged();
            }
        }

        private void EndColumnResize()
        {
            // 끄는 동안 폭이 한 번도 바뀌지 않았으면(최소 폭·같은 값·비율을 못 구함) 알리지 않는다.
            if (resizeChanged)
            {
                resizeChanged = false;
                RaiseViewStateChanged();
            }
        }

        private bool ApplyColumnWidth(string columnID, float? width)
        {
            var column = model.Columns.Find(columnID) ?? throw new ArgumentException($"알 수 없는 열 ID입니다: {columnID}", nameof(columnID));
            if (!width.HasValue)
            {
                return model.Columns.ClearUserSize(columnID);
            }

            if (column.Layout.Mode == TableGridColumnLayoutMode.Fixed)
            {
                return model.Columns.SetUserWidth(columnID, width);
            }

            return TryGetFlexibleWeight(columnID, width.Value, out var weight) && model.Columns.SetUserWeight(columnID, weight);
        }

        // --------------------------------------------------------------------------------
        /// <summary>
        /// <br/> 가변 열이 지금 표 폭에서 requested가 되는 가중치를 구한다.
        /// <br/> 가변 열들이 나눠 갖는 폭 W, 나머지 가변 열 가중치 합 S이면 x / (W - x) × S이고,
        /// <br/> 나머지 가변 열끼리의 비율은 그대로 남는다. 다른 가변 열이 없거나 배치 전이면 false다.
        /// </summary>
        // --------------------------------------------------------------------------------
        private bool TryGetFlexibleWeight(string columnID, float requested, out float weight)
        {
            weight = 0f;
            var target = model.Columns.GetVisibleIndex(columnID);
            var bodyWidth = body.resolvedStyle.width;
            if (target < 0 || float.IsNaN(bodyWidth) || bodyWidth <= 0f || float.IsNaN(requested) || float.IsInfinity(requested))
            {
                return false;
            }

            var layouts = new List<TableGridColumnLayout>();
            model.Columns.FillEffectiveLayouts(layouts);
            var flexibleWidth = 0f;
            var otherWeight = 0f;
            var otherMinimum = 0f;
            for (var index = 0; index < layouts.Count; index++)
            {
                var layout = layouts[index];
                if (layout.Mode != TableGridColumnLayoutMode.Flexible)
                {
                    continue;
                }

                flexibleWidth += geometry.GetWidth(index);
                if (index != target)
                {
                    otherWeight += layout.StretchWeight;
                    otherMinimum += layout.MinWidth;
                }
            }

            var own = layouts[target];
            var minimum = TableGridColumnSet.GetMinUserWidth(own);
            var maximum = flexibleWidth - otherMinimum;
            if (own.MaxWidth > 0f)
            {
                maximum = Math.Min(maximum, own.MaxWidth);
            }

            if (otherWeight <= 0f || maximum < minimum)
            {
                return false;
            }

            var width = Math.Clamp(requested, minimum, maximum);
            var rest = flexibleWidth - width;
            if (rest <= 0f)
            {
                return false;
            }

            weight = width / rest * otherWeight;
            return weight > 0f && !float.IsInfinity(weight);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> 열 폭을 제목과 행 값이 잘리지 않는 폭으로 맞춘다.
        /// <br/> 값은 보이는 구간부터 데이터 행 최대 500개를 잰다.
        /// <br/> 데이터 행이 없으면 제목만 기준으로 한다.
        /// </summary>
        // ------------------------------------------------------------
        public void AutoFitColumn(string columnID)
        {
            var columnIndex = model.Columns.GetVisibleIndex(columnID);
            if (columnIndex < 0)
            {
                return;
            }

            var width = header.MeasureTitleWidth(columnIndex);
            var column = model.Columns.VisibleColumns[columnIndex];
            viewport.TryGetCellMeasurer(columnIndex, out var measurer, out var chrome);
            width = Math.Max(width, MeasureValues(column, measurer, chrome));

            if (width > 0f)
            {
                // 글자 폭의 소수점 때문에 말줄임이 생기지 않도록 올려서 1px 여유를 둔다.
                SetColumnWidth(columnID, Mathf.Ceil(width) + 1f);
            }
        }

        // --------------------------------------------------------------------------------
        /// <summary>
        /// <br/> 보이는 구간부터 데이터 행의 셀 폭 중 가장 넓은 폭을 잰다(셀 틀 여백 포함).
        /// <br/> 셀 뷰가 MeasureWidth로 폭을 주면 그 폭을, 아니면 값 규칙 글자를 measurer로 잰다.
        /// </summary>
        // --------------------------------------------------------------------------------
        private float MeasureValues(TableGridColumn column, TextElement measurer, float chrome)
        {
            var rows = model.Rows;
            var widest = 0f;
            var sampled = 0;
            var first = Math.Max(0, Math.Min(viewport.FirstVisibleRow, rows.RowCount - AUTO_FIT_SAMPLE_ROWS));
            for (var index = first; index < rows.RowCount && sampled < AUTO_FIT_SAMPLE_ROWS; index++)
            {
                if (!rows.TryGetDataRow(index, out var row))
                {
                    continue;
                }

                // 셀 뷰가 정한 폭이 있으면 글자 대신 쓴다.
                var context = new TableGridCellContext(row.ID, index, row.Item, column, false, null);
                var viewWidth = column.MeasureCell(row.Item, in context);
                if (viewWidth.HasValue)
                {
                    sampled++;
                    widest = Math.Max(widest, viewWidth.Value + chrome);
                    continue;
                }

                var text = column.GetText(row.Item);
                if (measurer == null || string.IsNullOrEmpty(text))
                {
                    continue;
                }

                sampled++;
                var size = measurer.MeasureTextSize(text, 0f, MeasureMode.Undefined, 0f, MeasureMode.Undefined);
                if (!float.IsNaN(size.x))
                {
                    widest = Math.Max(widest, size.x + chrome);
                }
            }

            return widest;
        }

    #endregion

    #region 보기 상태

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> 보기 상태(열 표시·순서·사용자 폭·고정 열 수)를 꺼낸다.
        /// <br/> includeQuery이면 정렬·필터·그룹(원래 값)을,
        /// <br/> includeSession이면 스크롤·선택 행을 함께 담는다.
        /// </summary>
        // ------------------------------------------------------------
        public TableGridViewState CaptureViewState(bool includeQuery = true, bool includeSession = false)
        {
            var state = new TableGridViewState
            {
                FrozenColumnCount = model.Columns.FrozenCount,
            };

            foreach (var column in model.Columns.SourceColumns)
            {
                var order = model.Columns.GetVisibleIndex(column.ID);
                var columnState = new TableGridColumnViewState
                {
                    ID = column.ID,
                    Visible = order >= 0,
                    Order = order,
                };

                if (model.Columns.TryGetUserWidth(column.ID, out var width))
                {
                    columnState.HasWidth = true;
                    columnState.Width = width;
                }

                if (model.Columns.TryGetUserWeight(column.ID, out var weight))
                {
                    columnState.HasWeight = true;
                    columnState.Weight = weight;
                }

                state.Columns.Add(columnState);
            }

            if (includeQuery)
            {
                state.HasQuery = true;
                state.Query = TableGridQueryViewState.From(model.Query);
            }

            if (includeSession)
            {
                state.HasSession = true;
                state.Session.ScrollX = viewport.ScrollOffset.x;
                state.Session.ScrollY = viewport.ScrollOffset.y;
                var current = model.Selection.Current;
                if (current.Kind == TableGridSelectionKind.Row)
                {
                    state.Session.SelectedRowIDs.AddRange(current.RowIDs);
                }
            }

            return state;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> 보기 상태를 적용한다. 열은 ID로 맞추고,
        /// <br/> 없어진 열은 무시하고 상태에 없는 새 열은 뒤에 보인다.
        /// <br/> 고정 열은 Width, 가변 열은 Weight만 받는다.
        /// <br/> 쿼리는 QueryRequested로 요청한다.
        /// <br/> 버전이 다르면 열 부분만 적용한다.
        /// <br/> 적용하는 동안에는 ViewStateChanged를 알리지 않는다.
        /// </summary>
        // ------------------------------------------------------------
        public void ApplyViewState(TableGridViewState state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            applyingViewState = true;
            try
            {
                ApplyColumnViewState(state);
                if (state.Version != TableGridViewState.CURRENT_VERSION)
                {
                    return;
                }

                if (state.HasQuery && state.Query != null && QueryRequested != null)
                {
                    RequestQuery(state.Query.ToQuery());
                }

                if (state.HasSession && state.Session != null)
                {
                    if (state.Session.SelectedRowIDs != null && state.Session.SelectedRowIDs.Count > 0)
                    {
                        Selection = TableGridSelection.Rows(state.Session.SelectedRowIDs);
                    }

                    viewport.SetScrollOffset(new Vector2(state.Session.ScrollX, state.Session.ScrollY));
                }
            }
            finally
            {
                applyingViewState = false;
            }
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> 열 표시·순서·사용자 폭을 열 정책으로 되돌린다(모든 열을 원본 순서로 보임).
        /// <br/> 고정 열 수와 쿼리는 바꾸지 않는다.
        /// </summary>
        // ----------------------------------------------------------------------
        public void ResetViewState()
        {
            var columnIDs = new List<string>();
            foreach (var column in model.Columns.SourceColumns)
            {
                columnIDs.Add(column.ID);
            }

            applyingViewState = true;
            try
            {
                if (columnIDs.Count > 0)
                {
                    model.Columns.SetVisibleColumns(columnIDs);
                }

                model.Columns.ClearUserSizes();
            }
            finally
            {
                applyingViewState = false;
            }

            RaiseViewStateChanged();
        }

        private void ApplyColumnViewState(TableGridViewState state)
        {
            var columns = state.Columns ?? new List<TableGridColumnViewState>();
            var ordered = new List<TableGridColumnViewState>();
            var known = new HashSet<string>(StringComparer.Ordinal);
            foreach (var columnState in columns)
            {
                if (columnState == null || model.Columns.Find(columnState.ID) == null || !known.Add(columnState.ID))
                {
                    continue;
                }

                if (columnState.Visible)
                {
                    ordered.Add(columnState);
                }
            }

            ordered.Sort((left, right) => left.Order.CompareTo(right.Order));
            var visibleIDs = new List<string>();
            foreach (var columnState in ordered)
            {
                visibleIDs.Add(columnState.ID);
            }

            // 상태에 없는 새 열은 뒤에 보인다.
            foreach (var column in model.Columns.SourceColumns)
            {
                if (!known.Contains(column.ID))
                {
                    visibleIDs.Add(column.ID);
                }
            }

            if (visibleIDs.Count > 0)
            {
                model.Columns.SetVisibleColumns(visibleIDs);
            }

            model.Columns.ClearUserSizes();
            foreach (var columnState in columns)
            {
                var column = columnState == null ? null : model.Columns.Find(columnState.ID);
                if (column == null)
                {
                    continue;
                }

                // 저장된 보기 상태는 읽는 쪽이 관대하다(모르는 열은 건너뛴다). 비유한 폭·비율도 예외로 중단하지 않고 건너뛴다.
                if (column.Layout.Mode == TableGridColumnLayoutMode.Fixed && columnState.HasWidth
                    && !float.IsNaN(columnState.Width) && !float.IsInfinity(columnState.Width))
                {
                    model.Columns.SetUserWidth(column.ID, columnState.Width);
                }
                else if (column.Layout.Mode == TableGridColumnLayoutMode.Flexible && columnState.HasWeight
                    && columnState.Weight > 0f && !float.IsInfinity(columnState.Weight))
                {
                    model.Columns.SetUserWeight(column.ID, columnState.Weight);
                }
            }

            model.Columns.FrozenCount = Math.Max(0, state.FrozenColumnCount);
        }

        private void RaiseViewStateChanged()
        {
            if (!applyingViewState)
            {
                ViewStateChanged?.Invoke(this, EventArgs.Empty);
            }
        }

    #endregion

    #region 열 메뉴와 쿼리 요청

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> 헤더 칸을 왼쪽 클릭하면 목록 수준에서는 정렬할 수 있는 열의 정렬을
        /// <br/> 오름차순 → 내림차순 → 해제 순으로 바꿔 달라고 요청한다.
        /// <br/> 작업공간의 왼쪽 클릭은 열 선택(포인터 입력)이고,
        /// <br/> 열을 고를 수 없는 작업공간 표에서는 아무것도 하지 않는다.
        /// </summary>
        // ----------------------------------------------------------------------
        private void HandleHeaderClicked(TableGridColumn column, VisualElement cell)
        {
            if (Options.IsWorkspace || !column.Sortable || QueryRequested == null)
            {
                return;
            }

            TableGridSortDirection? next = TableGridSortDirection.Ascending;
            if (model.Query.SortColumnID == column.ID)
            {
                next = model.Query.SortDirection == TableGridSortDirection.Ascending
                    ? TableGridSortDirection.Descending
                    : null;
            }

            RequestQuery(model.Query.WithSort(column.ID, next));
        }

        // --------------------------------------------------------------------------------
        /// <summary>
        /// <br/> 헤더 칸을 우클릭하면 작업공간에서 열 메뉴를 연다. 목록 수준에서는 아무것도 하지 않는다.
        /// <br/> 열을 고를 수 있는 표에서 그 열이 열 선택 밖이면 그 열을 먼저 고른다.
        /// </summary>
        // --------------------------------------------------------------------------------
        private void HandleHeaderContextClicked(TableGridColumn column, VisualElement cell)
        {
            if (!Options.IsWorkspace)
            {
                return;
            }

            var selection = model.Selection;
            var selected = selection.Current.Kind == TableGridSelectionKind.Column && selection.IsColumnSelected(column.ID);
            if (Options.AllowsColumnSelection && !selected)
            {
                var columnIDs = new[]
                {
                    column.ID,
                };
                model.SetColumnAnchor(column.ID);
                ApplySelection(TableGridSelection.Columns(columnIDs));
            }

            OpenColumnMenu(column.ID, cell.worldBound, TableGridMenuSection.All);
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> 열 메뉴를 기준 영역(월드 좌표) 아래에 연다.
        /// <br/> section으로 정렬·그룹 구역만 보이게 할 수 있다(쿼리 칩에서 연 경우).
        /// </summary>
        // ----------------------------------------------------------------------
        internal void OpenColumnMenu(string columnID, Rect worldAnchor, TableGridMenuSection section)
        {
            var column = model.Columns.Find(columnID);
            if (column == null || panel == null)
            {
                return;
            }

            popover.OpenMenu(column, worldAnchor, section, model, Options);
            header.SetOpenColumn(columnID);
        }

        private void HideColumn(TableGridColumn column)
        {
            var columnIDs = new List<string>();
            foreach (var visible in model.Columns.VisibleColumns)
            {
                if (visible != column)
                {
                    columnIDs.Add(visible.ID);
                }
            }

            // 표시 열은 하나 이상이어야 한다.
            if (columnIDs.Count > 0)
            {
                SetVisibleColumns(columnIDs);
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 열의 필터 편집을 기준 영역(월드 좌표) 아래에 바로 연다.
        /// </summary>
        // ------------------------------------------------------------
        internal void OpenFilterEditor(string columnID, Rect worldAnchor)
        {
            var column = model.Columns.Find(columnID);
            if (column == null || panel == null)
            {
                return;
            }

            popover.OpenFilter(column, worldAnchor, model.Query.FindFilter(columnID));
            header.SetOpenColumn(columnID);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 요청한 쿼리 상태를 호스트에 알린다. 표시는 바꾸지 않는다.
        /// </summary>
        // ------------------------------------------------------------
        internal void RequestQuery(TableGridQueryState requested)
        {
            QueryRequested?.Invoke(this, new TableGridQueryRequest(model.Query, requested));
        }

    #endregion

    #region 선택

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> 선택을 바꾸고 표시를 갱신한다. 바뀌었으면 SelectionChanged를 알린다.
        /// <br/> 입력이 기준 행과 활성 열을 직접 정한 선택이므로 기준 행을 맞추지 않는다.
        /// </summary>
        // ----------------------------------------------------------------------
        private void ApplySelection(TableGridSelection newSelection)
        {
            if (!model.SetSelection(newSelection, out var previous))
            {
                return;
            }

            ShowSelection();
            SelectionChanged?.Invoke(this, new TableGridSelectionChanged(previous, model.Selection.Current));
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 열·행이 바뀌어 선택 규칙이 선택을 바꿨을 때 알린다.
        /// </summary>
        // ------------------------------------------------------------
        private void HandleSelectionAdjusted(TableGridSelection previous)
        {
            ShowSelection();
            SelectionChanged?.Invoke(this, new TableGridSelectionChanged(previous, model.Selection.Current));
        }

        // --------------------------------------------------------------------------------
        /// <summary>
        /// <br/> 행 하나를 고르고 키보드 이동의 기준 행·활성 열로 둔다(Core 안 호스트용).
        /// <br/> rowID가 null이면 선택을 해제한다. columnID가 null이면 활성 열을 두지 않는다.
        /// </summary>
        // --------------------------------------------------------------------------------
        internal void SelectRow(string rowID, string columnID)
        {
            model.Selection.RowAnchorID = rowID;
            model.Selection.RowActiveColumnID = rowID == null ? null : columnID;
            if (rowID == null)
            {
                Selection = TableGridSelection.None;
                return;
            }

            var rowIDs = new[]
            {
                rowID,
            };
            Selection = TableGridSelection.Rows(rowIDs);
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> 활성 열 ID(Core 안 호스트용). 행 선택이면 기준 행에서 고른 열,
        /// <br/> 셀 선택이면 포커스 셀의 열이다. 없으면 null이다.
        /// </summary>
        // ----------------------------------------------------------------------
        internal string ActiveColumnID
        {
            get
            {
                var current = model.Selection.Current;
                if (current.Kind == TableGridSelectionKind.Cell)
                {
                    return current.Focus.ColumnID;
                }

                return current.Kind == TableGridSelectionKind.Row ? model.Selection.RowActiveColumnID : null;
            }
        }

        // --------------------------------------------------------------------------------
        /// <summary>
        /// <br/> 활성 셀(행 선택이면 처음 고른 행)이 보이도록 스크롤한다.
        /// <br/> 실행 취소로 선택을 되돌린 뒤 바뀐 자리를 보일 때 쓴다. 결과에 없는 행이면 하지 않는다.
        /// </summary>
        // --------------------------------------------------------------------------------
        public void ScrollToSelection()
        {
            var selection = model.Selection;
            if (selection.ActiveRow >= 0)
            {
                viewport.ScrollIntoView(selection.ActiveRow, selection.ActiveColumn);
                return;
            }

            var current = selection.Current;
            if (current.Kind != TableGridSelectionKind.Row || current.RowIDs.Count == 0)
            {
                return;
            }

            var rowIndex = model.Rows.IndexOf(current.RowIDs[0]);
            if (rowIndex >= 0)
            {
                viewport.ScrollIntoView(rowIndex, -1);
            }
        }

        private void ShowSelection()
        {
            viewport.ApplySelectionMarks();
            header.ApplySelection(model.Selection);
            statusBar.ShowSelection(model.Selection);
        }

    #endregion

    #region 편집

        // ------------------------------------------------------------
        /// <summary>
        /// 셀 편집기가 열려 있는지 여부.
        /// </summary>
        // ------------------------------------------------------------
        public bool IsEditing => cellEditing.IsEditing;

        // --------------------------------------------------------------------------------
        /// <summary>
        /// <br/> 활성 셀 편집을 시작한다(Enter·더블클릭과 같다). 편집할 수 없으면 false다.
        /// <br/> Editor 열은 편집기를 열고(replacement가 있으면 그 글자로 시작),
        /// <br/> Inline 열은 셀 안 첫 컨트롤에 포커스를, Detail 열은 DetailRequested를 보낸다.
        /// </summary>
        // --------------------------------------------------------------------------------
        public bool BeginEdit(string replacement = null)
        {
            return cellEditing.Begin(replacement);
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> Enter·더블클릭(replacement null)이면 활성 셀 편집을 시도한다.
        /// <br/> 편집할 수 없으면 행 확정을 알린다.
        /// <br/> 글자 입력(replacement 있음)은 편집만 시도한다.
        /// </summary>
        // ----------------------------------------------------------------------
        private void BeginEditOrActivate(string replacement)
        {
            if (BeginEdit(replacement) || replacement != null)
            {
                return;
            }

            var selection = model.Selection;
            if (!model.TryGetDataRowID(selection.ActiveRow, out var rowID))
            {
                var current = selection.Current;
                rowID = current.Kind == TableGridSelectionKind.Row ? selection.RowAnchorID : null;
                if (rowID == null || !selection.IsRowSelected(rowID))
                {
                    return;
                }
            }

            var columnID = selection.ActiveColumn >= 0 && selection.ActiveColumn < model.Columns.VisibleColumns.Count
                ? model.Columns.VisibleColumns[selection.ActiveColumn].ID
                : null;
            RowActivated?.Invoke(this, new TableGridRowActivated(rowID, columnID));
        }

        private void BeginEditAfterPointer()
        {
            // 누름 이벤트가 끝날 때 포커스가 표 루트로 돌아가 입력칸이 바로 확정·닫히므로, 이벤트가 끝난 다음 틱에 연다.
            schedule.Execute(() => BeginEditOrActivate(null));
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 편집 중인 입력을 버리고 편집을 끝낸다.
        /// </summary>
        // ------------------------------------------------------------
        public void CancelEdit()
        {
            cellEditing.Cancel(true);
        }

        // --------------------------------------------------------------------------------
        /// <summary>
        /// <br/> 호스트가 데이터에 반영한 셀 변경을 표에 보인다. 표는 항목에 쓰지 않는다.
        /// <br/> 변경된 행 중 결과에 든 행이 있으면 보이는 셀을 모두 다시 연결한다(스크롤·선택 유지).
        /// <br/> 호스트는 먼저 데이터(행 항목)를 바꿔 둔다. 결과에 든 행이 없으면 하지 않는다.
        /// <br/> 정렬·필터가 바뀌면 다시 조회한 뒤 SetSource를 쓴다.
        /// </summary>
        // --------------------------------------------------------------------------------
        public void ApplyEditResults(IReadOnlyList<TableGridEditIntent> accepted)
        {
            if (accepted == null)
            {
                throw new ArgumentNullException(nameof(accepted));
            }

            if (source == null || !HasRowInResult(accepted))
            {
                return;
            }

            // 같은 행이어도 값이 바뀌었을 수 있으므로 연결을 끊어 둔 뒤 다시 연결한다.
            viewport.InvalidateBindings();
            viewport.Refresh();
        }

        private bool HasRowInResult(IReadOnlyList<TableGridEditIntent> edits)
        {
            foreach (var edit in edits)
            {
                if (edit != null && model.Rows.IndexOf(edit.RowID) >= 0)
                {
                    return true;
                }
            }

            return false;
        }

        private bool CanEditColumn(TableGridColumn column)
        {
            if (!Options.IsEditable)
            {
                return false;
            }

            // Detail 열은 상세 편집 요청을, 그 밖의 열은 셀 변경 요청을 받을 호스트가 있어야 한다.
            return column.Edit.Kind == TableGridCellEditKind.Detail ? DetailRequested != null : EditRequested != null;
        }

        private bool CanRequestEdits()
        {
            if (CanEdit)
            {
                return true;
            }

            // 편집할 수 있는 표인데 받을 호스트가 없을 때만 이유를 보인다.
            if (Options.IsEditable)
            {
                statusBar.ShowMessage("편집을 처리할 호스트가 연결되지 않았습니다", true);
            }

            return false;
        }

        private bool DispatchEdits
        (
            IReadOnlyList<TableGridEditIntent> edits, TableGridEditSource source, TableGridPasteFeedback? feedback
        )
        {
            if (edits.Count == 0)
            {
                if (source != TableGridEditSource.Paste)
                {
                    statusBar.ShowMessage("바꿀 수 있는 셀이 없습니다", true);
                }

                return false;
            }

            if (EditRequested == null)
            {
                statusBar.ShowMessage("편집을 처리할 호스트가 연결되지 않았습니다", true);
                return false;
            }

            // 호스트가 실행 취소 단계를 만들 수 있게 지금 셀 값과 선택을 함께 담는다.
            var requested = new List<TableGridEditIntent>(edits.Count);
            foreach (var edit in edits)
            {
                model.TryGetCellValue(edit.RowID, edit.ColumnID, out object previous);
                requested.Add(new TableGridEditIntent(edit.RowID, edit.ColumnID, edit.Value, previous));
            }

            // 표에서 값을 바꾸는 요청을 보내면 잘라내기를 푼다(잘라낸 셀 옮기기도 한 번으로 끝난다).
            CancelCut();
            var request = new TableGridEditRequest(requested, source, feedback, model.Selection.Current);
            EditRequested.Invoke(this, request);
            return true;
        }

        private void ShowRejected(IEnumerable<TableGridCellAddress> cells)
        {
            const long rejectedDurationMs = 900;
            var generation = model.Edit.MarkRejected(cells);
            viewport.ApplySelectionMarks();

            void Expire()
            {
                if (model.Edit.ClearRejected(generation))
                {
                    viewport.ApplySelectionMarks();
                }
            }

            schedule.Execute(Expire).StartingIn(rejectedDurationMs);
        }

        private void HandleEditCommitted(TableGridEditIntent edit)
        {
            DispatchEdit(edit, TableGridEditSource.Typing);
        }

        private void DispatchEdit(TableGridEditIntent edit, TableGridEditSource source)
        {
            var edits = new[]
            {
                edit,
            };
            DispatchEdits(edits, source, null);
        }

        private void HandleEditFinished(TableGridMove? next)
        {
            Focus();
            if (next.HasValue)
            {
                keyboardInput.MoveActive(next.Value);
            }
        }

    #endregion

    #region 클립보드

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> 선택의 표시 값을 TSV로 클립보드에 넣는다. 넣었으면 true다.
        /// <br/> includeHeaders이면 첫 줄에 열 이름을 넣는다.
        /// </summary>
        // ------------------------------------------------------------
        public bool CopySelection(bool includeHeaders)
        {
            return CopyToClipboard(includeHeaders, false);
        }

        // --------------------------------------------------------------------------------
        /// <summary>
        /// <br/> 선택을 시스템 클립보드에 TSV로 넣고 값 묶음을 보관한다. 복사할 값이 없으면 false다.
        /// <br/> 열 이름 줄은 TSV에만 들어가므로 같은 표에 되붙이면 그 줄은 빠진다.
        /// <br/> cut이면 잘라낸 것으로 표시하고, 아니면 앞의 잘라내기 표시를 지운다.
        /// <br/> 열 선택이 셀 수 상한을 넘으면 상태 줄에 이유를 보이고 복사하지 않는다.
        /// </summary>
        // --------------------------------------------------------------------------------
        private bool CopyToClipboard(bool includeHeaders, bool cut)
        {
            var error = TableGridClipboard.CheckCopy(model);
            if (error != null)
            {
                statusBar.ShowMessage(error, true);
                return false;
            }

            var bundle = TableGridClipboard.BuildSelection(model, includeHeaders);
            if (bundle == null || string.IsNullOrEmpty(bundle.Text))
            {
                return false;
            }

            GUIUtility.systemCopyBuffer = bundle.Text;
            var hadCut = model.Edit.IsCut;
            model.Edit.SetCopied(bundle, cut);
            if (hadCut || cut)
            {
                viewport.ApplySelectionMarks();
            }

            return true;
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> 선택을 잘라낸다(Ctrl·Cmd+X). 복사와 같고 원본을 흐리게 표시한다.
        /// <br/> 같은 표에 붙여넣으면 덮어쓰고 원본을 비운다(값 옮기기).
        /// <br/> 잘라낸 행은 "잘라낸 행 삽입"으로 옮길 수도 있다.
        /// <br/> 다른 곳에 붙여넣으면 복사처럼 원본은 그대로다.
        /// <br/> 데이터는 붙여넣기 전까지 바뀌지 않는다. 잘라냈으면 true다.
        /// </summary>
        // ----------------------------------------------------------------------
        public bool CutSelection()
        {
            // 잘라낸 셀은 덮어쓰기(편집)로, 잘라낸 행은 덮어쓰기나 잘라낸 행 삽입(행 이동)으로 쓰인다.
            var rows = model.Selection.Current.Kind == TableGridSelectionKind.Row;
            if (rows ? !CanCutRows : !CanRequestEdits())
            {
                return false;
            }

            var error = TableGridClipboard.CheckCut(model);
            if (error != null)
            {
                statusBar.ShowMessage(error, true);
                return false;
            }

            if (!CopyToClipboard(false, true))
            {
                return false;
            }

            var bundle = model.Edit.LastCopied;
            var message = rows
                ? $"{bundle.SourceRowIDs.Count:N0}개 행을 잘라냈습니다"
                : $"{(long)bundle.SourceRowIDs.Count * bundle.SourceColumnIDs.Count:N0}개 셀을 잘라냈습니다";
            statusBar.ShowMessage(message, false);
            return true;
        }

        private bool CanCutRows => CanEdit || CanReorderRows;

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> 시스템 클립보드의 TSV를 선택 위치에 덮어쓰기를 요청한다(Ctrl·Cmd+V).
        /// <br/> 같은 표에서 잘라낸 것이면 원본도 비워 값을 옮긴다.
        /// </summary>
        // ----------------------------------------------------------------------
        public bool PasteClipboard()
        {
            // 클립보드가 잘라낸 때와 다르면 잘라내기를 풀고 그 글자를 붙여넣는다.
            var text = GUIUtility.systemCopyBuffer;
            if (IsOwnCut(text))
            {
                return MoveCutValues();
            }

            CancelCut();
            return PasteText(text);
        }

        // --------------------------------------------------------------------------------
        /// <summary>
        /// <br/> TSV를 셀 범위의 좌상단(행 선택이면 고른 행의 첫 열)부터 붙여넣기를 요청한다.
        /// <br/> 값이 하나면 선택을 채운다. text가 마지막 복사의 TSV와 같으면 값 묶음을,
        /// <br/> 다르면 열마다 값 규칙 TryParse로 읽은 값을 요청한다.
        /// <br/> 읽기 전용 셀은 그대로 두고, 잘못된 값은 건너뛰며 잠깐 강조한다.
        /// <br/> 결과 요약은 상태 줄과 요청에 담긴다.
        /// </summary>
        // --------------------------------------------------------------------------------
        public bool PasteText(string text)
        {
            if (!CanRequestEdits() || string.IsNullOrEmpty(text))
            {
                return false;
            }

            var plan = TableGridClipboard.BuildPaste(model, model.Edit.ResolveClip(text));
            return SendPaste(plan, TableGridEditSource.Paste);
        }

        // --------------------------------------------------------------------------------
        /// <summary>
        /// <br/> 클립보드 행을 고른 행 위에 넣는다(우클릭 "복사한 행 삽입"·"잘라낸 행 삽입").
        /// <br/> 같은 표에서 잘라낸 행이면 그 행을 옮기고(MoveRowsRequested),
        /// <br/> 아니면 클립보드 행 값으로 새 행을 넣어 달라고 한다(AddRowRequested).
        /// <br/> 셀·열을 복사한 클립보드는 넣지 않는다. 요청했으면 true다.
        /// </summary>
        // --------------------------------------------------------------------------------
        public bool InsertClipboardRows()
        {
            var text = GUIUtility.systemCopyBuffer;
            if (IsCutRows(text))
            {
                return MoveCutRows();
            }

            if (!CanInsertCopiedRows(text))
            {
                return false;
            }

            var targetRowID = FindTopRowID();
            if (targetRowID == null)
            {
                statusBar.ShowMessage("넣을 자리의 행을 고르세요", true);
                return false;
            }

            var plan = TableGridClipboard.BuildInsert(model, model.Edit.ResolveClip(text));
            if (plan.Error != null)
            {
                statusBar.ShowMessage(plan.Error, true);
                return false;
            }

            SendAddRow(TableGridAddPosition.Before, targetRowID, plan.Rows);
            statusBar.ShowMessage(plan.Describe($"{plan.Rows.Count:N0}개 행 삽입을 요청했습니다"), false);
            return true;
        }

        // --------------------------------------------------------------------------------
        /// <summary>
        /// <br/> 클립보드에 맞는 삽입 붙여넣기 메뉴 항목을 만든다. 넣을 수 없으면 null이다.
        /// <br/> 같은 표에서 잘라낸 행은 "잘라낸 행 삽입", 복사한 행·밖의 글자는 "복사한 행 삽입"이다.
        /// </summary>
        // --------------------------------------------------------------------------------
        private XeriContextMenuItem CreateInsertPasteItem()
        {
            var text = GUIUtility.systemCopyBuffer;
            var hasTarget = FindTopRowID() != null;
            if (IsCutRows(text))
            {
                return CanReorderRows
                    ? XeriContextMenuItem.Command("잘라낸 행 삽입", () => InsertClipboardRows(), enabled: hasTarget)
                    : null;
            }

            return CanInsertCopiedRows(text)
                ? XeriContextMenuItem.Command("복사한 행 삽입", () => InsertClipboardRows(), enabled: hasTarget)
                : null;
        }

        private bool IsOwnCut(string text)
        {
            return model.Edit.IsCut && model.Edit.LastCopied.Matches(text);
        }

        private bool IsCutRows(string text)
        {
            return IsOwnCut(text) && model.Edit.LastCopied.SourceKind == TableGridSelectionKind.Row;
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> 복사한 행으로 넣을 수 있는 클립보드인지 확인한다.
        /// <br/> 이 표에서 복사한 행이거나 밖에서 온 글자다(셀·열 복사는 아니다).
        /// </summary>
        // ----------------------------------------------------------------------
        private bool CanInsertCopiedRows(string text)
        {
            if (!CanAddRows || string.IsNullOrEmpty(text) || IsCutRows(text))
            {
                return false;
            }

            var copied = model.Edit.LastCopied;
            return copied == null || !copied.Matches(text) || copied.SourceKind == TableGridSelectionKind.Row;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> 잘라낸 행을 고른 행 위로 옮기기를 요청한다(잘라낸 행 삽입).
        /// <br/> 요청하면 잘라낸 표시를 지운다(SendRowMove).
        /// </summary>
        // ------------------------------------------------------------
        private bool MoveCutRows()
        {
            var targetRowID = FindTopRowID();
            if (targetRowID == null)
            {
                statusBar.ShowMessage("옮길 자리의 행을 고르세요", true);
                return false;
            }

            if (model.Edit.IsCutRow(targetRowID))
            {
                statusBar.ShowMessage("잘라낸 행 자리로는 옮길 수 없습니다", true);
                return false;
            }

            if (!CheckRowMove())
            {
                return false;
            }

            var rowIDs = new List<string>(model.Edit.LastCopied.SourceRowIDs);
            SendRowMove(rowIDs, targetRowID, TableGridMovePlacement.Before, TableGridMoveSource.Cut);
            return true;
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> 잘라낸 셀·행 값을 선택 위치로 옮기기를 요청한다(덮어쓰기와 원본 비우기).
        /// <br/> 둘을 한 요청(Cut)에 담아 실행 취소 한 번에 되돌아가게 한다.
        /// <br/> 요청하면 잘라낸 표시를 지운다(DispatchEdits).
        /// </summary>
        // ----------------------------------------------------------------------
        private bool MoveCutValues()
        {
            if (!CanRequestEdits())
            {
                return false;
            }

            return SendPaste(TableGridClipboard.BuildMove(model, model.Edit.LastCopied), TableGridEditSource.Cut);
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> 고른 행 중 맨 위 데이터 행 ID(행 삽입·옮기기 자리).
        /// <br/> 행 선택이면 고른 행, 셀 선택이면 범위(없으면 활성 셀)의 맨 위다.
        /// </summary>
        // ----------------------------------------------------------------------
        private string FindTopRowID()
        {
            // 셀 범위는 모두 모으지 않고 첫 데이터 행에서 멈춘다.
            List<int> positions;
            if (model.Selection.Current.Kind == TableGridSelectionKind.Row)
            {
                positions = model.CollectSelectedRowPositions();
            }
            else
            {
                var (first, last) = TableGridRowMoves.GetCellSelectionRows(model);
                positions = model.CollectDataRowPositions(first, last, 1);
            }

            return positions.Count > 0 && model.TryGetDataRowID(positions[0], out var rowID) ? rowID : null;
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> 잘라내기 표시를 지우고 화면 표시를 고친다(표시를 지우는 유일한 경로).
        /// <br/> 지울 것이 있었으면 true다.
        /// </summary>
        // ----------------------------------------------------------------------
        private bool CancelCut()
        {
            if (!model.Edit.ClearCut())
            {
                return false;
            }

            viewport.ApplySelectionMarks();
            return true;
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> 붙여넣기(또는 잘라낸 셀 옮기기) 계획을 요청하고 결과를 상태 줄에 보인다.
        /// <br/> 거부된 셀은 잠깐 강조한다. 요청했으면 true다.
        /// </summary>
        // ----------------------------------------------------------------------
        private bool SendPaste(TableGridEditPlan plan, TableGridEditSource source)
        {
            if (plan.Error != null)
            {
                statusBar.ShowMessage(plan.Error, true);
                return false;
            }

            ShowRejected(plan.Rejected);
            var sent = plan.Edits.Count > 0;
            if (sent)
            {
                DispatchEdits(plan.Edits, source, plan.ToFeedback());
            }

            statusBar.ShowMessage(plan.Describe($"{plan.Edits.Count:N0}셀 변경 요청"), !sent);
            return sent;
        }

    #endregion

    #region 지우기와 행 삭제

        // ------------------------------------------------------------
        /// <summary>
        /// 선택 범위의 편집 가능한 셀을 비우도록 요청한다(Delete).
        /// </summary>
        // ------------------------------------------------------------
        public bool ClearSelectedValues()
        {
            if (!CanRequestEdits())
            {
                return false;
            }

            var plan = TableGridClipboard.BuildClear(model);
            if (plan.Error != null)
            {
                statusBar.ShowMessage(plan.Error, true);
                return false;
            }

            if (!DispatchEdits(plan.Edits, TableGridEditSource.Clear, null))
            {
                return false;
            }

            statusBar.ShowMessage($"{plan.Edits.Count:N0}개 셀 비우기를 요청했습니다", false);
            return true;
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> 선택한 데이터 행을 지워 달라고 요청한다(행 선택에서 Delete).
        /// <br/> 결과에서 빠진 행은 선택 규칙이 이미 뺐으므로 고른 행은 모두 보이는 행이다.
        /// </summary>
        // ----------------------------------------------------------------------
        public bool RequestDeleteSelectedRows()
        {
            var current = model.Selection.Current;
            if (current.Kind != TableGridSelectionKind.Row || current.RowIDs.Count == 0)
            {
                return false;
            }

            if (!CanDeleteRows)
            {
                // 편집할 수 있는 표인데 받을 호스트가 없을 때만 이유를 보인다.
                if (Options.IsEditable)
                {
                    statusBar.ShowMessage("행 삭제를 처리할 호스트가 연결되지 않았습니다", true);
                }

                return false;
            }

            CancelCut();
            DeleteRowsRequested.Invoke(this, new TableGridDeleteRowsRequest(current.RowIDs, current));
            statusBar.ShowMessage($"{current.RowIDs.Count:N0}개 행 삭제를 요청했습니다", false);
            return true;
        }

        private void HandleClearKey()
        {
            // 행 선택이면 행 삭제, 셀 선택이면 셀 비우기다.
            if (model.Selection.Current.Kind == TableGridSelectionKind.Row)
            {
                RequestDeleteSelectedRows();
                return;
            }

            ClearSelectedValues();
        }

    #endregion

    #region 본문 메뉴

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> 본문 우클릭 메뉴를 표 대신 여는 호스트 처리(Core 안 사용처 전용).
        /// <br/> 지정하면 표 기본 메뉴를 열지 않고 누른 위치(패널 좌표)를 넘긴다.
        /// <br/> 선택은 표가 먼저 맞춘다.
        /// </summary>
        // ----------------------------------------------------------------------
        internal Action<Vector2> HostContextMenu { get; set; }

        private void OpenBodyMenu(Vector2 position)
        {
            // 호스트가 자기 행 메뉴를 쓰면 표 기본 메뉴 대신 호스트가 연다.
            if (HostContextMenu != null)
            {
                HostContextMenu(position);
                return;
            }

            var current = model.Selection.Current;
            var hasSelection = current.Kind != TableGridSelectionKind.None;
            var editing = CanEdit;

            // 열 선택은 그 열 전체의 셀이 대상이므로 셀 범위와 같이 잘라내기·붙여넣기·지우기를 연다.
            var cellTarget = editing && (model.Selection.HasCellRange || current.Kind == TableGridSelectionKind.Column);
            var rows = CanDeleteRows && current.Kind == TableGridSelectionKind.Row;
            var pasteTarget = cellTarget || (editing && current.Kind == TableGridSelectionKind.Row);
            var rowCut = current.Kind == TableGridSelectionKind.Row && CanCutRows;

            var items = new List<XeriContextMenuItem>
            {
                XeriContextMenuItem.Command
                (
                    "복사", () => CopySelection(false), enabled: hasSelection, shortcut: XeriShortcutKeys.Format(KeyCode.C, action: true)
                ),
            };

            // 열 이름 포함 복사·빠른 이동은 작업공간 도구다.
            if (Options.IsWorkspace)
            {
                items.Add(XeriContextMenuItem.Command("열 이름 포함 복사", () => CopySelection(true), enabled: hasSelection));
            }

            items.Add(XeriContextMenuItem.Command("잘라내기", () => CutSelection(), enabled: cellTarget || rowCut, shortcut: XeriShortcutKeys.Format(KeyCode.X, action: true)));
            items.Add(XeriContextMenuItem.Command("붙여넣기", () => PasteClipboard(), enabled: pasteTarget, shortcut: XeriShortcutKeys.Format(KeyCode.V, action: true)));
            var insertPaste = CreateInsertPasteItem();
            if (insertPaste != null)
            {
                items.Add(insertPaste);
            }

            items.Add(XeriContextMenuItem.Separator());
            items.Add
            (
                rows
                    ? XeriContextMenuItem.Command("선택한 행 삭제", () => RequestDeleteSelectedRows(), shortcut: XeriShortcutKeys.Format(KeyCode.Delete))
                    : XeriContextMenuItem.Command("선택한 셀 지우기", () => ClearSelectedValues(), enabled: cellTarget, shortcut: XeriShortcutKeys.Format(KeyCode.Delete))
            );

            AddRowInsertItems(items);
            AddRowMoveItems(items);

            if (Options.IsWorkspace)
            {
                items.Add(XeriContextMenuItem.Separator());
                items.Add(XeriContextMenuItem.Command("빠른 이동", OpenJump, shortcut: XeriShortcutKeys.Format(TableGridKeyboardInput.JUMP_KEY, action: true)));
            }

            bodyMenu.Open(position, items);
        }

        private void AddRowInsertItems(List<XeriContextMenuItem> items)
        {
            if (!CanAddRows || FindTopRowID() == null)
            {
                return;
            }

            items.Add(XeriContextMenuItem.Separator());
            items.Add(XeriContextMenuItem.Command("위에 행 삽입", () => RequestAddRow(TableGridAddPosition.Before)));
            items.Add(XeriContextMenuItem.Command("아래에 행 삽입", () => RequestAddRow(TableGridAddPosition.After)));
        }

        private void AddRowMoveItems(List<XeriContextMenuItem> items)
        {
            var current = model.Selection.Current;
            var hasRows = current.Kind == TableGridSelectionKind.Row || model.Selection.ActiveRow >= 0;
            if (CanReorderRows && hasRows)
            {
                items.Add(XeriContextMenuItem.Separator());
                items.Add(XeriContextMenuItem.Command("위로 이동", () => RequestRowMove(TableGridRowMove.Up, TableGridMoveSource.Menu), shortcut: XeriShortcutKeys.Format(KeyCode.UpArrow, alt: true)));
                items.Add(XeriContextMenuItem.Command("아래로 이동", () => RequestRowMove(TableGridRowMove.Down, TableGridMoveSource.Menu), shortcut: XeriShortcutKeys.Format(KeyCode.DownArrow, alt: true)));
                items.Add(XeriContextMenuItem.Command("맨 위로 이동", () => RequestRowMove(TableGridRowMove.Top, TableGridMoveSource.Menu), shortcut: XeriShortcutKeys.Format(KeyCode.Home, alt: true)));
                items.Add(XeriContextMenuItem.Command("맨 아래로 이동", () => RequestRowMove(TableGridRowMove.Bottom, TableGridMoveSource.Menu), shortcut: XeriShortcutKeys.Format(KeyCode.End, alt: true)));
                items.Add(XeriContextMenuItem.Command("위치로 이동...", PromptRowMovePosition));
            }

            if (CanApplySortOrder)
            {
                items.Add(XeriContextMenuItem.Separator());
                items.Add(XeriContextMenuItem.Command("정렬 순서를 데이터에 적용", RequestApplySortOrder));
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> 활성 셀의 경계(월드 좌표)를 찾는다. 보이지 않으면 false다.
        /// <br/> 키보드로 여는 메뉴 위치처럼 Core 안 사용처가 쓴다.
        /// </summary>
        // ------------------------------------------------------------
        internal bool TryGetActiveCellBounds(out Rect bounds)
        {
            var selection = model.Selection;
            return viewport.TryGetCellBounds(selection.ActiveRow, Math.Max(0, selection.ActiveColumn), out bounds);
        }

    #endregion

    #region 빠른 이동

        // ------------------------------------------------------------
        /// <summary>
        /// 빠른 이동 창을 표 위쪽에 연다(Ctrl·Cmd+G와 같다).
        /// </summary>
        // ------------------------------------------------------------
        public void OpenJump()
        {
            if (panel == null || !Options.IsWorkspace)
            {
                return;
            }

            popover.OpenJump(GetPromptAnchor(), Jump);
        }

        // 입력칸 하나짜리 창(빠른 이동·위치로 이동)은 본문 위쪽 가운데에 연다.
        private Rect GetPromptAnchor()
        {
            var bounds = body.worldBound;
            var width = TableGridPopoverMetrics.PROMPT_WIDTH;
            return new Rect(bounds.center.x - width * 0.5f, bounds.yMin, width, 0f);
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> 데이터 행 번호(행 번호 칸과 같은 번호, 1부터) 또는 행 ID로 이동한다.
        /// <br/> 성공하면 null이다.
        /// <br/> 소스 결과 전체에서 찾아 그 셀을 고른다. 그룹 머리 행이면 스크롤만 한다.
        /// </summary>
        // ----------------------------------------------------------------------
        private string Jump(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return "행 번호나 ID를 입력하세요";
            }

            int rowIndex;
            if (int.TryParse(text, out var number))
            {
                // 번호는 행 번호 칸과 같은 데이터 행 번호다(그룹 머리 행은 세지 않는다).
                rowIndex = model.Rows.FindDataRowByNumber(number);
                if (rowIndex < 0)
                {
                    return $"1–{model.Rows.DataRowCount:N0} 사이 번호를 입력하세요";
                }
            }
            else
            {
                rowIndex = model.Rows.IndexOf(text);
                if (rowIndex < 0)
                {
                    return "그 ID의 행이 없습니다";
                }
            }

            var columnIndex = Math.Max(0, model.Selection.ActiveColumn);
            viewport.ScrollIntoView(rowIndex, columnIndex);
            if (model.TryGetCellAddress(rowIndex, columnIndex, out var address))
            {
                Selection = TableGridSelection.SingleCell(address);
            }

            return null;
        }

    #endregion

    #region 페이지

        // --------------------------------------------------------------------------------
        /// <summary>
        /// <br/> 지금 페이지(0부터). Scroll 모드는 0이다.
        /// <br/> Paged 모드에서 지정하면 그 페이지로 이동하고, 범위 밖이면 가장 가까운 페이지다.
        /// <br/> Scroll 모드에서는 지정해도 아무 일도 없다.
        /// </summary>
        // --------------------------------------------------------------------------------
        public int PageIndex
        {
            get => Options.IsPaged ? paginator.PageIndex : 0;
            set
            {
                if (!Options.IsPaged)
                {
                    return;
                }

                var before = paginator.PageIndex;
                paginator.MoveTo(Math.Max(0, value));
                if (paginator.PageIndex == before)
                {
                    return;
                }

                ApplyPageWindow();
                viewport.SetScrollOffset(new Vector2(viewport.ScrollOffset.x, 0f));
                viewport.Refresh();
                PageChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 전체 페이지 수. Scroll 모드는 1이다.
        /// </summary>
        // ------------------------------------------------------------
        public int PageCount => Options.IsPaged ? Math.Max(1, paginator.PageCount) : 1;

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> 페이지 크기·전체 행 수를 다시 계산해 본문이 보일 행 구간을 맞춘다.
        /// <br/> 행을 연결하기 전에 부른다(행 수·보이는 높이·페이지·열 배치가 바뀔 때).
        /// <br/> Scroll 모드는 전체 행이고 페이지 이동 컨트롤을 숨긴다.
        /// </summary>
        // ----------------------------------------------------------------------
        private void ApplyPageWindow()
        {
            if (!Options.IsPaged)
            {
                pageNavigator.style.display = DisplayStyle.None;
                viewport.SetRowWindow(0, -1);
                return;
            }

            // PageSize 0이면 보이는 높이에 들어가는 행 수다(본문과 같은 계산).
            paginator.PerPage = Options.PageSize > 0 ? Options.PageSize : viewport.PageRowCount;
            paginator.TotalCount = Math.Max(0, model.Rows.RowCount);
            pageNavigator.style.display = DisplayStyle.Flex;

            var range = paginator.Range;
            viewport.SetRowWindow(range.BeginIndex, range.Count);

            // 페이지 수·크기 계산을 마친 상태를 컨트롤 표시에 맞춘다.
            pageNavigator.Refresh();
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> 페이지 이동 컨트롤·좌우 키로 페이지를 넘긴다.
        /// <br/> 행 선택 표에서 고른 행이 새 페이지에 없으면, 이전·마지막으로 넘겼을 때는
        /// <br/> 페이지 마지막, 그 밖(다음·처음·직접)은 페이지 처음 데이터 행을 고른다.
        /// </summary>
        // ----------------------------------------------------------------------
        private void TurnPage(PageNavigationRequest request)
        {
            var before = paginator.PageIndex;
            switch (request.Kind)
            {
                case PageNavigationKind.First:
                    PageIndex = 0;
                    break;
                case PageNavigationKind.Previous:
                    PageIndex = paginator.PageIndex - 1;
                    break;
                case PageNavigationKind.Next:
                    PageIndex = paginator.PageIndex + 1;
                    break;
                case PageNavigationKind.Last:
                    PageIndex = paginator.PageCount - 1;
                    break;
                default:
                    PageIndex = request.PageIndex;
                    break;
            }

            var selectsRows = Options.Selection == TableGridSelectionMode.Row || Options.Selection == TableGridSelectionMode.MultiRow;
            if (paginator.PageIndex == before || !selectsRows || IsSelectionOnPage())
            {
                return;
            }

            var toEnd = request.Kind == PageNavigationKind.Previous || request.Kind == PageNavigationKind.Last;
            SelectPageEdge(toEnd);
        }

        private bool IsSelectionOnPage()
        {
            var current = model.Selection.Current;
            if (current.Kind != TableGridSelectionKind.Row)
            {
                return false;
            }

            var range = paginator.Range;
            foreach (var rowID in current.RowIDs)
            {
                var index = model.Rows.IndexOf(rowID);
                if (index >= range.BeginIndex && index < range.EndIndex)
                {
                    return true;
                }
            }

            return false;
        }

        // 지금 페이지의 처음(또는 마지막) 데이터 행 하나를 고르고 보이게 한다.
        private void SelectPageEdge(bool toEnd)
        {
            var range = paginator.Range;
            var step = toEnd ? -1 : 1;
            var index = toEnd ? range.EndIndex - 1 : range.BeginIndex;
            for (; index >= range.BeginIndex && index < range.EndIndex; index += step)
            {
                if (model.Rows.TryGetDataRow(index, out var row))
                {
                    var rowIDs = new[]
                    {
                        row.ID,
                    };
                    Selection = TableGridSelection.Rows(rowIDs);
                    ScrollToSelection();
                    return;
                }
            }
        }

        // --------------------------------------------------------------------------------
        /// <summary>
        /// <br/> 표 밖(호스트의 검색창 등)에서 받은 이동 키(방향키·PageUp/Down·Home/End)를
        /// <br/> 표에 포커스가 있을 때처럼 처리한다. 처리했으면 true다.
        /// </summary>
        // --------------------------------------------------------------------------------
        internal bool HandleForwardedKey(KeyCode keyCode, bool shift)
        {
            return keyboardInput.HandleForwardedKey(keyCode, shift);
        }

        // --------------------------------------------------------------------------------
        /// <summary>
        /// <br/> 결과가 바뀐 뒤(검색·필터·정렬) 보이는 자리를 맞춘다.
        /// <br/> 고른 행이 결과에 남아 있으면 그 행을, 없으면 맨 위(Paged는 첫 페이지)를 보인다.
        /// </summary>
        // --------------------------------------------------------------------------------
        internal void RevealSelectionOrFirstPage()
        {
            var current = model.Selection.Current;
            var rowIndex = current.Kind == TableGridSelectionKind.Row && current.RowIDs.Count > 0
                ? model.Rows.IndexOf(current.RowIDs[0])
                : -1;
            if (rowIndex >= 0)
            {
                ScrollToSelection();
                return;
            }

            if (Options.IsPaged)
            {
                PageIndex = 0;
            }

            viewport.SetScrollOffset(new Vector2(viewport.ScrollOffset.x, 0f));
        }

        private void ShowPageOf(int rowIndex)
        {
            if (Options.IsPaged && paginator.PerPage > 0)
            {
                PageIndex = rowIndex / paginator.PerPage;
            }
        }

    #endregion

    #region 행 추가 요청

        // --------------------------------------------------------------------------------
        /// <summary>
        /// <br/> 새 행을 넣어 달라고 요청한다.
        /// <br/> Before·After는 행 선택이면 처음·마지막 고른 행, 셀 선택이면 활성 행이 기준이다.
        /// <br/> 넣을 수 없으면 false다(ReadOnly, 호스트 미연결, 기준 행 없음).
        /// </summary>
        // --------------------------------------------------------------------------------
        public bool RequestAddRow(TableGridAddPosition position)
        {
            if (!CanAddRows)
            {
                return false;
            }

            string rowID = null;
            if (position != TableGridAddPosition.End)
            {
                var rowIDs = TableGridRowMoves.CollectMoving(model);
                if (rowIDs.Count == 0)
                {
                    return false;
                }

                rowID = position == TableGridAddPosition.Before ? rowIDs[0] : rowIDs[rowIDs.Count - 1];
            }

            SendAddRow(position, rowID, null);
            return true;
        }

        private void SendAddRow
        (
            TableGridAddPosition position, string rowID, IReadOnlyList<IReadOnlyDictionary<string, object>> rowValues
        )
        {
            // 행을 넣어 달라고 하면 잘라내기를 푼다(값 편집·행 이동·행 삭제 요청과 같다).
            CancelCut();
            AddRowRequested.Invoke(this, new TableGridAddRowRequest(position, rowID, model.Selection.Current, rowValues));
        }

    #endregion

    #region 행 이동

        private bool IsReorderable => Options.IsEditable && MoveRowsRequested != null && source != null && source.Capabilities.CanReorder;

        private bool HasViewOrder => model.Query.SortDirection.HasValue || !string.IsNullOrEmpty(model.Query.GroupColumnID);

        private bool CanReorderRows => IsReorderable && !HasViewOrder;

        private bool CanApplySortOrder => IsReorderable && Options.IsWorkspace
            && model.Query.SortDirection.HasValue && string.IsNullOrEmpty(model.Query.GroupColumnID);

        // --------------------------------------------------------------------------------
        /// <summary>
        /// <br/> 행 이동을 할 수 있는지 확인한다. 정렬·그룹 때문에 막혔으면 상태 줄에 이유를 보인다.
        /// <br/> 데이터가 행 이동을 지원하지 않거나 호스트가 연결되지 않았으면 조용히 false다.
        /// </summary>
        // --------------------------------------------------------------------------------
        private bool CheckRowMove()
        {
            if (CanReorderRows)
            {
                return true;
            }

            if (IsReorderable && HasViewOrder)
            {
                statusBar.ShowMessage("정렬·그룹을 풀어야 행을 옮길 수 있습니다", true);
            }

            return false;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 고른 행을 한 칸 위·아래나 맨 위·아래로 옮겨 달라고 요청한다.
        /// </summary>
        // ------------------------------------------------------------
        private void RequestRowMove(TableGridRowMove move, TableGridMoveSource moveSource)
        {
            if (!CheckRowMove())
            {
                return;
            }

            var rowIDs = TableGridRowMoves.CollectMoving(model);
            if (rowIDs.Count == 0)
            {
                return;
            }

            if (TableGridRowMoves.TryResolve(model, rowIDs, move, out var targetRowID, out var placement))
            {
                SendRowMove(rowIDs, targetRowID, placement, moveSource);
            }
        }

        // --------------------------------------------------------------------------------
        /// <summary>
        /// 고른 행을 입력한 위치(행 번호 칸과 같은 데이터 행 번호, 1부터)로 옮기는 창을 연다.
        /// </summary>
        // --------------------------------------------------------------------------------
        private void PromptRowMovePosition()
        {
            if (panel == null || !CheckRowMove())
            {
                return;
            }

            var rowIDs = TableGridRowMoves.CollectMoving(model);
            if (rowIDs.Count == 0)
            {
                return;
            }

            string Submit(string text)
            {
                var error = TableGridRowMoves.TryResolveNumber(model, rowIDs, text, out var targetRowID, out var placement);
                if (error != null)
                {
                    return error;
                }

                SendRowMove(rowIDs, targetRowID, placement, TableGridMoveSource.Menu);
                return null;
            }

            popover.OpenPrompt(GetPromptAnchor(), "위치로 이동", "행 번호", "옮길 자리의 행 번호(1부터)를 입력합니다", Submit);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 지금 보기 정렬 순서를 데이터에 적용해 달라고 요청한다.
        /// </summary>
        // ------------------------------------------------------------
        private void RequestApplySortOrder()
        {
            if (!CanApplySortOrder)
            {
                return;
            }

            var request = new TableGridMoveRowsRequest
            (
                Array.Empty<string>(), null, TableGridMovePlacement.Before,
                TableGridMoveSource.ApplySort, model.Selection.Current
            );
            MoveRowsRequested.Invoke(this, request);
            statusBar.ShowMessage("정렬 순서를 데이터에 적용하도록 요청했습니다", false);
        }

        private void SendRowMove
        (
            IReadOnlyList<string> rowIDs, string targetRowID, TableGridMovePlacement placement, TableGridMoveSource moveSource
        )
        {
            // 호스트가 반영하면 셀 선택이 같은 자리 대신 옮긴 행을 따라가게 한다.
            // 행을 옮기면 잘라내기를 푼다(잘라낸 행 옮기기도 한 번으로 끝난다).
            CancelCut();
            var request = new TableGridMoveRowsRequest(rowIDs, targetRowID, placement, moveSource, model.Selection.Current);
            model.FollowSelectionOnce();
            MoveRowsRequested.Invoke(this, request);
            statusBar.ShowMessage($"{rowIDs.Count:N0}개 행 이동을 요청했습니다", false);
        }

        // --------------------------------------------------------------------------------
        /// <summary>
        /// <br/> 끌기 손잡이를 누른 행으로 옮길 행을 정한다. 옮길 수 없으면 null이다.
        /// <br/> 누른 행이 옮길 행(선택)에 들어 있으면 그 선택을, 아니면 그 행 하나를 골라 옮긴다.
        /// <br/> 선택을 끈 표(None)는 선택을 바꾸지 않고 그 행만 옮긴다.
        /// </summary>
        // --------------------------------------------------------------------------------
        private IReadOnlyList<string> PrepareRowDrag(int rowIndex)
        {
            if (!CheckRowMove() || !model.TryGetDataRowID(rowIndex, out var rowID))
            {
                return null;
            }

            var rowIDs = TableGridRowMoves.CollectMoving(model);
            if (rowIDs.Contains(rowID))
            {
                return rowIDs;
            }

            var single = new List<string>
            {
                rowID,
            };
            if (Options.Selection != TableGridSelectionMode.None)
            {
                model.Selection.RowAnchorID = rowID;
                Selection = TableGridSelection.Rows(single);
            }

            return single;
        }

        private void HandleRowsDropped(IReadOnlyList<string> rowIDs, string targetRowID, TableGridMovePlacement placement)
        {
            // 끄는 동안 정렬·그룹이 걸렸을 수 있으므로 놓을 때 다시 확인한다.
            if (CheckRowMove())
            {
                SendRowMove(rowIDs, targetRowID, placement, TableGridMoveSource.Drag);
            }
        }

        private void ApplyReorderHandleState()
        {
            // 손잡이를 보이되 지금 옮길 수 없으면(정렬·그룹 등) 흐리게 보인다. 누르면 CheckRowMove가 이유를 보인다.
            EnableInClassList("xeri-table--reorder-locked", showsReorderHandle && !CanReorderRows);
        }

    #endregion

    #region 열 이동

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> 고른 열을 왼쪽(-1)·오른쪽(1)으로 한 칸 옮긴다(Ctrl+Alt+←/→).
        /// <br/> 옮긴 쪽 끝 열이 보이도록 가로로 스크롤한다.
        /// </summary>
        // ----------------------------------------------------------------------
        private void MoveSelectedColumns(int direction)
        {
            var current = model.Selection.Current;
            if (!Options.AllowsColumnSelection || current.Kind != TableGridSelectionKind.Column)
            {
                return;
            }

            var order = CollectVisibleColumnIDs();
            var moving = new HashSet<string>(current.ColumnIDs, StringComparer.Ordinal);
            var index = TableGridColumnSet.GetStepIndex(order, moving, direction);
            if (!ApplyColumnOrder(order, TableGridColumnSet.MoveColumns(order, moving, index)))
            {
                return;
            }

            var positions = model.Selection.ColumnPositions;
            viewport.ScrollIntoView(-1, direction < 0 ? positions[0] : positions[positions.Count - 1]);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> 끈 열을 놓은 열 경계로 옮긴다. 고정 열 수는 그대로이고,
        /// <br/> 옮긴 열은 놓은 구역(고정·스크롤)에 들어간다.
        /// </summary>
        // ------------------------------------------------------------
        private void HandleColumnsDropped(IReadOnlyCollection<string> columnIDs, int gap, bool frozenZone)
        {
            var order = CollectVisibleColumnIDs();
            var moving = new HashSet<string>(columnIDs, StringComparer.Ordinal);
            var index = TableGridColumnSet.GetDropIndex(order, moving, gap, model.Columns.FrozenCount, frozenZone);
            ApplyColumnOrder(order, TableGridColumnSet.MoveColumns(order, moving, index));
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> 새 보기 순서를 열 패널과 같은 경로(SetVisibleColumns)로 적용한다.
        /// <br/> 순서가 그대로면 하지 않고 false다. 열 선택은 ID라 옮긴 열을 따라간다.
        /// </summary>
        // ----------------------------------------------------------------------
        private bool ApplyColumnOrder(List<string> order, List<string> moved)
        {
            for (var index = 0; index < order.Count; index++)
            {
                if (!string.Equals(order[index], moved[index], StringComparison.Ordinal))
                {
                    SetVisibleColumns(moved);
                    return true;
                }
            }

            return false;
        }

        private List<string> CollectVisibleColumnIDs()
        {
            var columns = model.Columns.VisibleColumns;
            var columnIDs = new List<string>(columns.Count);
            foreach (var column in columns)
            {
                columnIDs.Add(column.ID);
            }

            return columnIDs;
        }

    #endregion

    #region 빈 상태와 검색

        // --------------------------------------------------------------------------------
        /// <summary>
        /// <br/> 검색 글자. 거르기는 소스가 맡는다(TableGridListSource).
        /// <br/> 지정하면 소스에 검색을 알리고 다시 읽는다. 결과가 없으면 NoResultText를 보인다.
        /// <br/> Paged면 고른 행이 남은 페이지, 없으면 첫 페이지를 보인다.
        /// <br/> null은 빈 글자로 다루고, 같은 글자를 지정하면 아무 일도 없다.
        /// </summary>
        // --------------------------------------------------------------------------------
        public string SearchText
        {
            get => searchText;
            set
            {
                value ??= string.Empty;
                if (value == searchText)
                {
                    return;
                }

                searchText = value;
                if (source is ITableGridSearchableSource searchable)
                {
                    searchable.SetSearch(value, CollectSearchColumns());
                }

                Refresh();
                RevealSelectionOrFirstPage();
            }
        }
        private string searchText = string.Empty;

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> 행이 없을 때 글자 대신 보일 요소. 지정하지 않았으면 null이다.
        /// <br/> null로 지정하면 글자(EmptyText·NoResultText)로 돌아간다.
        /// <br/> 요소는 본문 위를 덮도록 배치된다(xeri-table__empty).
        /// </summary>
        // ----------------------------------------------------------------------
        public VisualElement EmptyContent
        {
            get => emptyContent;
            set
            {
                emptyContent?.RemoveFromHierarchy();
                emptyContent = value;
                if (emptyContent != null)
                {
                    emptyContent.AddToClassList("xeri-table__empty");
                    body.Add(emptyContent);
                }

                ShowRowStatus();
            }
        }
        private VisualElement emptyContent;

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> 호스트가 소스를 직접 걸러 결과 행만 넘기는 표에서, 지금 걸러진 상태인지 알린다.
        /// <br/> true면 행이 없을 때 EmptyText 대신 NoResultText를 보인다.
        /// <br/> 표 자신의 필터·검색이 걸린 경우는 이 값과 상관없이 NoResultText를 보인다.
        /// </summary>
        // ----------------------------------------------------------------------
        public bool IsSourceNarrowed
        {
            get => isSourceNarrowed;
            set
            {
                if (isSourceNarrowed == value)
                {
                    return;
                }

                isSourceNarrowed = value;
                ShowRowStatus();
            }
        }
        private bool isSourceNarrowed;

        // 검색 글자가 공백뿐이면 검색하지 않는다(SearchTextUtility 규칙).
        private bool IsNarrowed => isSourceNarrowed || model.Query.Filters.Count > 0 || !string.IsNullOrWhiteSpace(SearchText);

        private List<TableGridColumn> CollectSearchColumns()
        {
            var searchable = new List<TableGridColumn>();
            foreach (var column in model.Columns.SourceColumns)
            {
                if (column.Searchable)
                {
                    searchable.Add(column);
                }
            }

            return searchable;
        }

    #endregion

    #region 배치

        private void HandleColumnsChanged()
        {
            // 열 구성이 바뀌면 셀을 다시 만들므로 편집 중이던 입력은 버린다.
            cellEditing.Cancel(false);
            columnsDirty = true;
            RelayoutColumns();
            statusBar.ShowSelection(model.Selection);
        }

        private void HandleRowsChanged()
        {
            // 행 번호 자릿수가 바뀌면 번호 칸 폭이 달라지므로 열 배치를 다시 한다(배치가 끝에 본문도 다시 그린다).
            if (Options.ShowsRowSelector && CountDigits(model.Rows.DataRowCount) != rowNumberDigits)
            {
                RelayoutColumns();
            }
            else
            {
                // 행 수가 바뀌면 페이지 구간이 달라질 수 있으므로 연결하기 전에 맞춘다.
                ApplyPageWindow();
                viewport.Refresh();
            }

            statusBar.ShowSelection(model.Selection);
            RowsChanged?.Invoke();
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> 행 번호 칸의 폭. 행 수의 자릿수에 맞춰 넓어지며
        /// <br/> SELECTOR_MIN_WIDTH보다 좁아지지 않는다.
        /// <br/> 계산한 자릿수를 보관해 행 수가 바뀔 때 폭이 달라지는지 비교한다.
        /// </summary>
        // ----------------------------------------------------------------------
        private float GetSelectorWidth()
        {
            rowNumberDigits = CountDigits(model.Rows.DataRowCount);

            return Math.Max(SELECTOR_MIN_WIDTH, rowNumberDigits * SELECTOR_DIGIT_WIDTH + SELECTOR_PADDING);
        }

        private static int CountDigits(int value)
        {
            var digits = 1;
            for (var rest = value / 10; rest > 0; rest /= 10)
            {
                digits++;
            }

            return digits;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> 열 폭과 위치를 다시 계산해 헤더·본문에 반영한다.
        /// <br/> 표시 열이 바뀌었으면 헤더 칸과 행의 셀을 다시 만든다.
        /// </summary>
        // ------------------------------------------------------------
        private void RelayoutColumns()
        {
            var bodyWidth = body.resolvedStyle.width;
            if (float.IsNaN(bodyWidth) || bodyWidth <= 0f)
            {
                return;
            }

            var showSelector = Options.ShowsRowSelector;
            if (columnsDirty)
            {
                // 손잡이는 옵션이 정하지 않으면 데이터를 옮길 수 있는 표에서 보인다. 정렬·그룹으로 잠시 막힌 동안에도
                // 자리를 지켜 열이 흔들리지 않고 흐린 모양으로 남는다(ApplyReorderHandleState). Refresh 때 다시 정한다.
                columnsDirty = false;
                showsReorderHandle = Options.ShowReorderHandle ?? IsReorderable;
                header.Build(model.Columns.VisibleColumns, showsReorderHandle, showSelector);
                header.ApplyQuery(model.Query);
                header.ApplySelection(model.Selection);
                viewport.BuildCells(showsReorderHandle, showSelector);
            }

            ApplyReorderHandleState();
            var scrollbarWidth = viewport.ScrollbarWidth;
            model.Columns.FillEffectiveLayouts(effectiveLayouts);
            geometry.LayoutColumns
            (
                effectiveLayouts, model.Columns.FrozenCount,
                showsReorderHandle ? HANDLE_WIDTH : 0f, showSelector ? GetSelectorWidth() : 0f, bodyWidth - scrollbarWidth
            );

            header.ApplyGeometry();
            headerGap.style.width = scrollbarWidth;
            viewport.ApplyGeometry();
            ApplyPageWindow();
            viewport.Refresh();
        }

        private void ShowRowStatus()
        {
            var isEmpty = model.Rows.IsEmpty;
            if (emptyContent != null)
            {
                emptyContent.style.display = isEmpty ? DisplayStyle.Flex : DisplayStyle.None;
            }

            var emptyText = emptyContent != null ? string.Empty : IsNarrowed ? Options.NoResultText : Options.EmptyText;
            // 보이는 행 위치 구간은 본문 연결과 같은 계산(페이지 구간 안으로 자름)을 쓴다.
            var (first, end) = geometry.GetVisibleRows(viewport.ScrollOffset.y, viewport.ViewportHeight, 0);
            statusBar.ShowRows(model.Rows, first, end, emptyText);
        }

    #endregion

    }
}
