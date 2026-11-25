/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : CollectionTableGrid.cs
수정일 : 2026-11-23

# 설명
컬렉션 편집 세션을 Inspector 표(TableGrid, TableGridOptions.Inspector())로 보여 준다.
머리(접기·제목·개수·관리 메뉴)·검색·추가 버튼과 높이 조절 손잡이는 이 크롬이 맡고,
행 표시·선택·끌기 손잡이·키보드 이동은 TableGrid가 맡는다.
셀은 열마다 고른 셀 drawer를 셀 뷰 어댑터(CellView)가 감싸 그리고, 모든 변경은 CollectionSession.Apply로 요청한다.
행 진단 아이콘은 셀 앞에, 행 상세·삭제 버튼은 마지막 열 셀 끝에 둔다.

# 특이사항, 제약사항
기본은 지정 행 수 높이(하단 손잡이로 조절)이고, 호스트 USS가 채움 모드(--xeri-collection-fill)를 켜면
부모 높이를 채우고 표 안에서 스크롤한다.
행 ID는 직렬화 인덱스다. 구조가 바뀌면 행 식별 값으로 선택을 다시 찾는다.
화면에 남은 행과 원본이 다르면 이전 표시에서 만든 행 명령·편집 요청을 실행하지 않는다.
행 복제·행 전체 복사/붙여넣기·삭제·메뉴 단축키와 Enter는 표 기본 동작보다 먼저 이 크롬이 처리하고, 행 메뉴는 표가 넘긴 위치에 연다.
검색창은 TableGridSearchField이고 표를 직접 검색하지 않는다(SearchesGrid = false, 표는 테마·결과 수에만 쓴다).
검색은 이 크롬이 셀 drawer 요약으로 걸러 결과 행만 표에 넘기고, 검색 중에는 행을 옮기지 않는 소스를 준다.
열 상태(폭)는 TableGridViewStatePrefs(대상 형식 + 속성 경로 키)로, 접힘·행 수는 SessionState로 저장한다.
접힌 표는 머리만 만들고 표는 처음 펼칠 때 만든다(인스펙터 로드 비용 절감).
표는 객체 전체가 아니라 자기 컬렉션 필드만 감시한다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

using inonego;
using inonego.Xeri;
using inonego.Xeri.Editor;
using inonego.Xeri.UI;
using inonego.Xeri.UI.Editor;
using inonego.Xeri.UI.TableGrid;
using inonego.Xeri.UI.TableGrid.Editor;

using TableGridView = inonego.Xeri.UI.TableGrid.TableGrid;

namespace inonego.Xeri.Serializable.Editor
{
    // ============================================================
    /// <summary>
    /// 컬렉션 편집 세션의 Inspector 표.
    /// </summary>
    // ============================================================
    internal sealed class CollectionTableGrid : VisualElement, ICollectionCellHost
    {

    #region 내부 타입

        // ============================================================
        /// <summary>
        /// 재사용하는 셀 UI와 현재 행 연결 상태.
        /// </summary>
        // ============================================================
        private sealed class CellBinding
        {
            internal int SourceIndex = -1;
            internal CollectionColumn Column;
            internal ICollectionCellDrawer Drawer;
            internal VisualElement Root;
            internal VisualElement Content;
            internal SeverityIndicator Validation;
            internal Button RowEditButton;
            internal TableGridCellContext? GridContext;
        }

        // ================================================================================
        /// <summary>
        /// <br/> 셀 drawer(와 그 안의 CollectionCellView)를 표 셀 뷰로 감싸는 어댑터.
        /// <br/> 셀 상태는 셀 요소의 CellBinding에 두고 이 뷰는 열마다 하나를 공유한다.
        /// </summary>
        // ================================================================================
        private sealed class CellView : TableGridItemCellView
        {
            private readonly CollectionTableGrid owner;
            private readonly CollectionColumn column;
            private readonly ICollectionCellDrawer drawer;
            private readonly bool hasRowActions;

            // ------------------------------------------------------------
            /// <summary>
            /// 열과 drawer로 셀 뷰를 만든다.
            /// </summary>
            // ------------------------------------------------------------
            public CellView(CollectionTableGrid owner, CollectionColumn column, ICollectionCellDrawer drawer, bool hasRowActions)
            {
                this.owner = owner;
                this.column = column;
                this.drawer = drawer;
                this.hasRowActions = hasRowActions;
            }

            // ------------------------------------------------------------
            /// <summary>
            /// 행이 바뀌어도 재사용할 셀 UI를 만든다.
            /// </summary>
            // ------------------------------------------------------------
            public override VisualElement MakeCell()
            {
                return owner.MakeCell(column, drawer, hasRowActions);
            }

            // ------------------------------------------------------------
            /// <summary>
            /// 셀 UI를 표 셀 문맥의 행에 연결한다.
            /// </summary>
            // ------------------------------------------------------------
            public override void Bind(VisualElement element, in TableGridCellContext context)
            {
                owner.BindCell((CellBinding)element.userData, in context);
            }

            // ------------------------------------------------------------
            /// <summary>
            /// 셀 UI의 연결을 끊고 UI는 재사용할 수 있게 둔다.
            /// </summary>
            // ------------------------------------------------------------
            public override void Unbind(VisualElement element)
            {
                owner.UnbindCell((CellBinding)element.userData);
            }
        }

    #endregion

    #region 필드

        // 행·헤더 높이는 CollectionTableGridStyle.uss의 28px과 맞춘다.
        private const float rowHeight = 28f;
        private const float tableHeaderHeight = 28f;

        // 행 버튼(.xeri-collection__icon-button) 한 개가 차지하는 폭. USS의 20px + 좌우 여백 2px씩과 맞춘다.
        private const float rowActionButtonWidth = 24f;
        private const float viewportHeightAllowance = 6f;
        private const string emptyText = "항목이 없습니다.";
        private const string noResultText = "검색 결과가 없습니다.";
        private static readonly CustomStyleProperty<float> fillProperty = new("--xeri-collection-fill");

        // ------------------------------------------------------------
        /// <summary>
        /// 표가 편집하는 세션을 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public CollectionSession Session { get; }

        private readonly CollectionKind kind;
        private readonly CollectionSource source;
        private readonly CollectionAddFlow addFlow;
        private readonly bool isReadOnly;
        private readonly bool hasFieldColumns;
        private readonly string label;
        private readonly int defaultVisibleRows;
        private readonly string rowsStateKey;
        private readonly string foldoutStateKey;
        private readonly string viewStateKey;
        private readonly List<int> sourceRows = new();
        private readonly List<int> shownRows = new();
        private readonly HashSet<int> shownRowSet = new();
        private readonly HashSet<CellBinding> boundCells = new();
        private readonly VisualElement contentHost;
        private readonly VisualElement resizeHandle;
        private readonly Label titleLabel;
        private readonly Button foldoutButton;
        private readonly Label countLabel;
        private readonly TableGridSearchField searchField;
        private readonly XeriContextMenu collectionActionsMenu;
        private readonly IVisualElementScheduledItem refreshTask;

        // 표와 행 메뉴는 처음 펼칠 때 EnsureBody가 만든다. 접힌 표는 머리만 만든다.
        private TableGridView grid;
        private XeriContextMenu rowContextMenu;

        private CollectionValidation validation = new();
        private bool? hasUnlistedFields;
        private string searchText = string.Empty;
        private int visibleRows;
        private bool fillsParent;
        private float resizeStartY;
        private int resizeStartRows;
        private bool isExpanded;
        private object selectedIdentity;
        private bool restoringSelection;
        private bool pendingRefresh;
        private bool pendingStructureRefresh;
        private RevisionMark displayed;

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// 편집 세션으로 표를 구성한다.
        /// </summary>
        // ------------------------------------------------------------
        public CollectionTableGrid(CollectionSession session, CollectionTableGridOptions? options = null)
        {
            Session = session ?? throw new ArgumentNullException(nameof(session));
            kind = session.Kind;
            source = session.Source;
            isReadOnly = session.IsReadOnly;
            hasFieldColumns = HasFieldColumns(kind);
            addFlow = new CollectionAddFlow(session, () => panel != null, LogCommandFailure, RevealRow, RevealRow, CloseDetailEditors);
            var settings = options ?? CollectionTableGridOptions.Default();
            label = settings.Label;
            refreshTask = schedule.Execute(RefreshWhenIdle);
            refreshTask.Pause();
            var target = session.SerializedObject.targetObject;
            var stateKey = $"{target.GetEntityId()}.{source.Path}";
            foldoutStateKey = $"UniXeri.CollectionEditor.Foldout.{stateKey}";
            isExpanded = SessionState.GetBool(foldoutStateKey, false);

            // 열 상태는 같은 형식의 같은 필드끼리 공유한다. 에디터를 다시 열면 바뀌는 값은 키에 쓰지 않는다.
            viewStateKey = $"Collection.{target.GetType().FullName}.{source.Path}";

            // 사용자가 조절한 행 수는 같은 대상과 경로에 대해 편집 세션 동안 유지한다.
            rowsStateKey = $"UniXeri.CollectionEditor.Rows.{stateKey}";
            defaultVisibleRows = settings.VisibleRows;
            visibleRows = ClampVisibleRows(SessionState.GetInt(rowsStateKey, defaultVisibleRows));
            AddToClassList("xeri-collection");
            EnableInClassList("xeri-collection--multi-column", kind.Columns.Count > 1);
            styleSheets.Add(CollectionEditorResources.LoadTableGridStyle());

            // 구조는 UXML, 스타일은 USS, 데이터 연결과 명령은 이 크롬이 담당한다.
            CollectionEditorResources.LoadTableGridLayout().CloneTree(this);
            var header = Require<VisualElement>("collection-header");
            contentHost = Require<VisualElement>("collection-content");
            titleLabel = Require<Label>("collection-title");
            titleLabel.tooltip = kind.Description;
            foldoutButton = Require<Button>("collection-foldout");
            foldoutButton.clicked += () => SetExpanded(!isExpanded);
            countLabel = Require<Label>("collection-count");
            searchField = Require<TableGridSearchField>("collection-search");
            searchField.tooltip = "항목 검색";
            searchField.SearchesGrid = false;
            searchField.SearchTextChanged += (_, _) => HandleSearchTextChanged(searchField.Text);

            var actionsButton = Require<Button>("collection-actions");
            actionsButton.text = string.Empty;
            actionsButton.iconImage = XeriIcon.LoadBackground("ellipsis");
            actionsButton.tooltip = "컬렉션 관리";
            actionsButton.SetEnabled(!isReadOnly);
            collectionActionsMenu = new XeriContextMenu(actionsButton);
            actionsButton.clicked += ShowCollectionActions;

            // 제목·빈 머리를 눌러도 접되, 관리 메뉴와 접기 버튼 클릭은 중복 처리하지 않는다. 채움 모드는 항상 펼친다.
            header.RegisterCallback<ClickEvent>
            (
                evt =>
                {
                    if (fillsParent)
                    {
                        return;
                    }

                    if (evt.target is VisualElement clicked
                        && (clicked == actionsButton || actionsButton.Contains(clicked)
                            || clicked == foldoutButton || foldoutButton.Contains(clicked)))
                    {
                        return;
                    }

                    SetExpanded(!isExpanded);
                }
            );

            var addButton = Require<Button>("collection-add");
            addButton.text = string.Empty;
            addButton.iconImage = XeriIcon.LoadBackground("plus");
            addButton.tooltip = "항목 추가";
            addButton.SetEnabled(!isReadOnly && kind.Supports(CollectionEdits.Add));
            addButton.clicked += () => HandleAdd(addButton);

            // 목록 하단 손잡이로 표시 행 수를 조절하고 더블 클릭으로 기본값을 복원한다.
            resizeHandle = new VisualElement
            {
                name = "collection-resize-handle",
                tooltip = "끌어서 목록 높이 조절 · 더블 클릭으로 기본 높이",
            };
            resizeHandle.AddToClassList("xeri-collection__resize-handle");
            resizeHandle.RegisterCallback<PointerDownEvent>(HandleResizePointerDown);
            resizeHandle.RegisterCallback<PointerMoveEvent>(HandleResizePointerMove);
            resizeHandle.RegisterCallback<PointerUpEvent>(HandleResizePointerUp);
            contentHost.Add(resizeHandle);

            // 행 명령 단축키는 표 기본 키 처리보다 먼저 받고, 표가 쓰지 않은 Escape는 올라올 때 받는다.
            RegisterCallback<KeyDownEvent>(HandleShortcutKeyDown, TrickleDown.TrickleDown);
            RegisterCallback<KeyDownEvent>(HandleEscapeKeyDown);
            RegisterCallback<MouseUpEvent>(HandleInlineContextMouseUp, TrickleDown.TrickleDown);

            // 부모를 채우는 배치는 호스트 USS가 --xeri-collection-fill로 지정한다.
            RegisterCallback<CustomStyleResolvedEvent>(HandleCustomStyleResolved);

            // 셀 편집은 바인딩 통지로 확정하고 외부 변경 관찰은 표시 갱신만 한다.
            // 객체 전체가 아니라 이 컬렉션 필드만 감시해 다른 필드 편집에는 반응하지 않는다.
            RegisterCallback<AttachToPanelEvent>(HandleAttach);
            RegisterCallback<DetachFromPanelEvent>(HandleDetach);
            RegisterCallback<SerializedPropertyChangeEvent>(HandlePropertyEdited);
            this.TrackPropertyValue(source.GetProperty(), _ => HandleCollectionChanged());
            SetExpanded(isExpanded);
            if (isExpanded)
            {
                RefreshRows();
            }
            else
            {
                SaveRevision();
            }
        }

        private T Require<T>(string elementName)
        where T : VisualElement
        {
            return XeriUIResources.Require<T>(this, elementName, "CollectionTableGrid");
        }

        private static bool HasFieldColumns(CollectionKind kind)
        {
            foreach (var column in kind.Columns)
            {
                if (column.RelativePath != null)
                {
                    return true;
                }
            }

            return false;
        }

    #endregion

    #region 펼침과 본문 구성

        // ------------------------------------------------------------
        /// <summary>
        /// 머리 컨트롤러와 첫 행 바인딩을 보존하며 펼침 상태를 바꾼다.
        /// </summary>
        // ------------------------------------------------------------
        private void SetExpanded(bool expanded)
        {
            var wasExpanded = isExpanded;
            isExpanded = expanded;
            SessionState.SetBool(foldoutStateKey, expanded);
            EnableInClassList("xeri-collection--collapsed", !expanded);
            foldoutButton.text = string.Empty;
            foldoutButton.iconImage = XeriIcon.LoadBackground(expanded ? "chevron-down" : "chevron-right");
            foldoutButton.tooltip = expanded ? "목록 접기" : "목록 펼치기";
            if (!expanded)
            {
                ClearPendingRefresh();
                CloseRowContextMenu();
                RefreshStatus();
                return;
            }

            EnsureBody();
            if (!wasExpanded)
            {
                RefreshRows();
            }
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> 표와 행 메뉴를 처음 필요할 때 한 번 만든다.
        /// <br/> 열이 하나면 헤더를 숨기고, 행을 옮길 수 있는 종류만 끌기 손잡이를 둔다.
        /// </summary>
        // ----------------------------------------------------------------------
        private void EnsureBody()
        {
            if (grid != null)
            {
                return;
            }

            var options = TableGridOptions.Inspector();
            options.Access = isReadOnly ? TableGridAccess.ReadOnly : TableGridAccess.Editable;
            options.RowHeight = rowHeight;
            options.ShowHeader = kind.Columns.Count > 1;
            options.ShowReorderHandle = !isReadOnly && kind.CanReorder;
            options.EmptyText = emptyText;
            options.NoResultText = noResultText;
            grid = new TableGridView(options)
            {
                name = "collection-rows",
            };
            grid.AddToClassList("xeri-collection__grid");
            grid.SetColumns(CreateGridColumns());

            // 검색창은 표의 테마를 따른다. 검색은 이 크롬이 거르므로 표 검색으로 보내지 않는다.
            searchField.Grid = grid;

            // 열 상태(표시·순서·폭)만 저장하고 쿼리·세션은 저장하지 않는다.
            TableGridViewStatePrefs.Bind(grid, viewStateKey, includeQuery: false);

            // 행 메뉴는 표가 행을 고른 뒤 넘긴 위치에 이 크롬이 연다.
            grid.HostContextMenu = ShowRowContextMenu;
            rowContextMenu = new XeriContextMenu(grid, HandleRowContextMenuKeyDown);
            grid.SelectionChanged += HandleSelectionChanged;
            grid.EditRequested += HandleEditRequested;
            grid.DetailRequested += HandleDetailRequested;
            grid.RowActivated += HandleRowActivated;
            if (!isReadOnly && kind.CanReorder)
            {
                grid.MoveRowsRequested += HandleMoveRowsRequested;
            }

            grid.RegisterCallback<FocusOutEvent>
            (
                _ =>
                {
                    if (pendingRefresh)
                    {
                        refreshTask.ExecuteLater(0);
                    }
                }
            );

            // 높이 조절 손잡이는 표 아래에 둔다.
            contentHost.Insert(0, grid);
        }

        // --------------------------------------------------------------------------------
        /// <summary>
        /// <br/> 종류의 열마다 표 열을 만든다. 값 규칙 글자는 셀 drawer 요약(검색 글자)이다.
        /// <br/> 보기 정렬·필터·그룹·표 검색은 쓰지 않는다(표시 순서는 종류가 정한다).
        /// </summary>
        // --------------------------------------------------------------------------------
        private IReadOnlyList<TableGridColumn> CreateGridColumns()
        {
            var columns = new List<TableGridColumn>(kind.Columns.Count);
            var lastColumn = kind.Columns[kind.Columns.Count - 1];
            foreach (var column in kind.Columns)
            {
                var spec = column;
                var drawer = CollectionCellDrawers.Resolve(spec);
                var isLast = spec == lastColumn;
                var layout = isLast ? ReserveRowActions(spec.Layout) : spec.Layout;
                var edit = CreateGridEdit(spec);
                columns.Add
                (
                    new TableGridColumn<int, object>
                    (
                        spec.ID, spec.Header, sourceIndex => GetCellText(sourceIndex, spec, drawer),
                        TableGridFormats.Default<object>(), layout, TableGridAlignment.Left, edit,
                        filterable: false, sortable: false, groupable: false, searchable: false,
                        view: new CellView(this, spec, drawer, isLast)
                    )
                );
            }

            return columns;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> 열 편집 방식을 표 편집 방식으로 바꾼다.
        /// <br/> ReadOnly → None, Inline → Inline(Immediate),
        /// <br/> Delayed → Inline(Confirm), 복합 값 열 → Detail.
        /// <br/> 초안 열은 셀 입력칸으로 고칠 수 있으면 Inline(Confirm),
        /// <br/> 아니면 Detail(초안 창)이다.
        /// </summary>
        // ------------------------------------------------------------
        private TableGridCellEdit CreateGridEdit(CollectionColumn column)
        {
            if (isReadOnly || column.Edit == CollectionCellEdit.ReadOnly)
            {
                return TableGridCellEdit.None;
            }

            if (IsComplexColumn(column))
            {
                return TableGridCellEdit.Detail;
            }

            return column.Edit switch
            {
                CollectionCellEdit.Inline => TableGridCellEdit.Inline(TableGridInlineCommit.Immediate),
                CollectionCellEdit.Delayed => TableGridCellEdit.Inline(TableGridInlineCommit.Confirm),
                _ => CollectionInlineInput.Supports(column)
                    ? TableGridCellEdit.Inline(TableGridInlineCommit.Confirm)
                    : TableGridCellEdit.Detail,
            };
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> 셀 안에서 바로 고칠 수 없는 복합 값 열인지 반환한다.
        /// <br/> 관리 참조·목록·일반 class/struct가 복합 값이다.
        /// <br/> 형식을 모르면 단순 값으로 본다(셀 안 컨트롤의 포커스를 지키기 위해서다).
        /// </summary>
        // ----------------------------------------------------------------------
        private static bool IsComplexColumn(CollectionColumn column)
        {
            if (column.IsManagedReference)
            {
                return true;
            }

            var type = column.ValueType;
            if (type == null || type == typeof(string) || type.IsPrimitive || type.IsEnum || type == typeof(decimal))
            {
                return false;
            }

            if (typeof(UnityEngine.Object).IsAssignableFrom(type))
            {
                return false;
            }

            // 배열·목록은 복합 값이고, UnityEngine의 기본 값 형식(Vector3, Color 등)은 단순 값이다.
            return typeof(IList).IsAssignableFrom(type) || type.Namespace != "UnityEngine";
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// 표 열이 가진 편집 방식을 읽는다(CreateGridColumns가 열마다 정한 값).
        /// </summary>
        // ----------------------------------------------------------------------
        private TableGridCellEdit GetGridEdit(CollectionColumn column)
        {
            foreach (var gridColumn in grid.Columns)
            {
                if (string.Equals(gridColumn.ID, column.ID, StringComparison.Ordinal))
                {
                    return gridColumn.Edit;
                }
            }

            throw new InvalidOperationException($"표에 없는 열입니다: {column.ID}");
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 행 버튼(상세 편집·삭제)이 들어가는 마지막 열에 버튼 폭을 더한다.
        /// </summary>
        // ------------------------------------------------------------
        private TableGridColumnLayout ReserveRowActions(TableGridColumnLayout layout)
        {
            var reserved = (hasFieldColumns ? 2 : 1) * rowActionButtonWidth;
            if (layout.Mode == TableGridColumnLayoutMode.Fixed)
            {
                return TableGridColumnLayout.Fixed(layout.Width + reserved);
            }

            var maxWidth = layout.MaxWidth > 0f ? layout.MaxWidth + reserved : layout.MaxWidth;
            return TableGridColumnLayout.Flexible(layout.Width + reserved, layout.MinWidth + reserved, maxWidth, layout.StretchWeight);
        }

        private string GetCellText(int sourceIndex, CollectionColumn column, ICollectionCellDrawer drawer)
        {
            if (sourceIndex < 0 || sourceIndex >= source.Count)
            {
                return string.Empty;
            }

            var property = source.GetCell(sourceIndex, column);
            return property == null ? string.Empty : drawer.GetSummary(property, column) ?? string.Empty;
        }

    #endregion

    #region 셀 구성과 바인딩

        // ------------------------------------------------------------
        /// <summary>
        /// 셀 UI와 콜백을 한 번 만들고 재사용할 연결 상태를 함께 보관한다.
        /// </summary>
        // ------------------------------------------------------------
        private VisualElement MakeCell(CollectionColumn column, ICollectionCellDrawer drawer, bool hasRowActions)
        {
            var cell = new VisualElement();
            cell.AddToClassList("xeri-collection__cell");
            var binding = new CellBinding
            {
                Column = column,
                Drawer = drawer,
                Root = cell,
                Content = drawer.CreateElement(column),
                Validation = new SeverityIndicator(),
            };
            cell.userData = binding;

            // 진단 아이콘은 값 콘텐츠 앞에 둔다. 끌기 손잡이는 표가 행 앞에 둔다.
            cell.Add(binding.Validation);
            cell.Add(binding.Content);

            // 셀을 더블 클릭하면 상세 편집을 연다. 셀 안의 입력 필드는 더블 클릭을 단어 선택에 쓰므로 제외한다.
            binding.Content.RegisterCallback<PointerDownEvent>
            (
                evt =>
                {
                    if (evt.button != 0 || evt.clickCount != 2 || binding.SourceIndex < 0
                        || IsEditingFieldTarget(evt.target as VisualElement))
                    {
                        return;
                    }

                    OpenDetail(binding.SourceIndex, binding.Column);
                    evt.StopPropagation();
                }
            );

            if (hasRowActions)
            {
                cell.Add(CreateRowActions(binding));
            }

            return cell;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 행 전체 명령(상세 편집·삭제)을 마지막 열 끝에 둔다.
        /// </summary>
        // ------------------------------------------------------------
        private VisualElement CreateRowActions(CellBinding binding)
        {
            var rowActions = new VisualElement();
            rowActions.AddToClassList("xeri-collection__row-actions");

            // 열로 나열하지 않은 필드가 있는 행은 행 전체 상세 창을 연다.
            if (hasFieldColumns)
            {
                binding.RowEditButton = CollectionIconButton.Create
                (
                    () => OpenRowEditor(binding),
                    isReadOnly ? "eye" : "pencil",
                    isReadOnly ? "항목 상세 보기" : "항목 상세 편집"
                );
                rowActions.Add(binding.RowEditButton);
            }

            var deleteButton = CollectionIconButton.Create(() => HandleRemoveAt(binding.SourceIndex), "trash", "행 삭제");
            deleteButton.AddToClassList("xeri-collection__row-remove");
            deleteButton.SetEnabled(!isReadOnly && kind.Supports(CollectionEdits.Remove));
            rowActions.Add(deleteButton);
            return rowActions;
        }

        private void BindCell(CellBinding binding, in TableGridCellContext context)
        {
            if (!(context.Item is int sourceIndex) || sourceIndex < 0 || sourceIndex >= source.Count)
            {
                UnbindCell(binding);
                return;
            }

            binding.SourceIndex = sourceIndex;
            binding.GridContext = context;
            boundCells.Add(binding);
            BindContent(binding);
            if (binding.RowEditButton != null)
            {
                var hasDetails = HasUnlistedFields(sourceIndex);
                binding.RowEditButton.style.display = hasDetails ? DisplayStyle.Flex : DisplayStyle.None;
            }

            RefreshCellValidation(binding);
        }

        private void UnbindCell(CellBinding binding)
        {
            boundCells.Remove(binding);
            binding.Drawer.Unbind(binding.Content);
            binding.SourceIndex = -1;
            binding.GridContext = null;
            binding.Validation.SetSeverity(null);
            binding.Validation.tooltip = null;
        }

        private void BindContent(CellBinding binding)
        {
            var property = source.GetCell(binding.SourceIndex, binding.Column);
            var cell = new CollectionCell(this, binding.SourceIndex, binding.Column, property, isReadOnly, binding.GridContext);
            binding.Drawer.Bind(binding.Content, cell);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 셀 진단 가운데 가장 높은 심각도를 보이고 전체 내용은 툴팁에 둔다.
        /// </summary>
        // ------------------------------------------------------------
        private void RefreshCellValidation(CellBinding binding)
        {
            CollectionCellDiagnostic? primary = null;
            var messages = new List<string>();
            foreach (var diagnostic in validation.GetDiagnostics(binding.SourceIndex))
            {
                // 행 전체 메시지는 첫 번째 열에만 표시한다.
                var columnID = diagnostic.ColumnID ?? kind.Columns[0].ID;
                if (columnID != binding.Column.ID)
                {
                    continue;
                }

                messages.Add(diagnostic.Message);
                if (!primary.HasValue || diagnostic.Severity > primary.Value.Severity)
                {
                    primary = diagnostic;
                }
            }

            binding.Validation.SetSeverity(primary?.Severity);
            binding.Validation.tooltip = primary.HasValue ? string.Join("\n", messages) : null;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 열로 나열하지 않은 직렬화 필드가 행에 남아 있는지 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        private bool HasUnlistedFields(int sourceIndex)
        {
            if (hasUnlistedFields.HasValue)
            {
                return hasUnlistedFields.Value;
            }

            // 행 형식은 고정이므로 첫 확인 결과를 모든 행에 재사용한다.
            var listed = new HashSet<string>(StringComparer.Ordinal);
            foreach (var column in kind.Columns)
            {
                if (column.RelativePath != null)
                {
                    listed.Add(column.RelativePath);
                }
            }

            var found = false;
            foreach (var child in SerializedPropertyHelper.EnumerateVisibleChildren(source.GetRow(sourceIndex)))
            {
                if (!listed.Contains(child.name))
                {
                    found = true;
                    break;
                }
            }

            hasUnlistedFields = found;
            return found;
        }

        private void OpenRowEditor(CellBinding binding)
        {
            if (binding.SourceIndex < 0 || binding.SourceIndex >= source.Count)
            {
                RefreshRows();
                return;
            }

            CollectionDetailEditors.OpenRow(Session, binding.SourceIndex);
        }

    #endregion

    #region 행 표시와 선택 복원

        private bool IsSearching => !string.IsNullOrWhiteSpace(searchText);

        private static string GetRowID(int sourceIndex)
        {
            return sourceIndex.ToString(CultureInfo.InvariantCulture);
        }

        private static bool TryGetRowIndex(string rowID, out int sourceIndex)
        {
            return int.TryParse(rowID, NumberStyles.None, CultureInfo.InvariantCulture, out sourceIndex);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 표시 순서와 검색 결과를 다시 구성해 표시한다.
        /// </summary>
        // ------------------------------------------------------------
        private void RefreshRows()
        {
            UpdateRows();
            RefreshView(false);
        }

        private void UpdateRows()
        {
            ClearPendingRefresh();
            Session.Observe();
            SaveRevision();
            sourceRows.Clear();
            sourceRows.AddRange(source.GetDisplayRows());
            validation = Session.Validate();
            RebuildShownRows();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 표시 순서의 행 가운데 검색어에 맞는 행만 보일 행으로 고른다.
        /// </summary>
        // ------------------------------------------------------------
        private void RebuildShownRows()
        {
            shownRows.Clear();
            shownRowSet.Clear();
            var searching = IsSearching;
            foreach (var sourceIndex in sourceRows)
            {
                if (searching && !SearchTextUtility.Matches(GetSearchText(sourceIndex), searchText))
                {
                    continue;
                }

                shownRows.Add(sourceIndex);
                shownRowSet.Add(sourceIndex);
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> 보일 행을 표에 넘기고 여전히 같은 항목인 선택을 복구한다.
        /// <br/> resetScroll이면 맨 위로 스크롤한다(검색어 변경).
        /// </summary>
        // ------------------------------------------------------------
        private void RefreshView(bool resetScroll)
        {
            // 접힌 표에서 컬렉션 관리 명령을 실행해도 목록 갱신이 필요하면 본문을 만든다.
            EnsureBody();
            restoringSelection = true;
            try
            {
                // 검색은 이 크롬이 걸러 결과 행만 넘기므로 걸러진 상태를 표에 알린다.
                // 검색 중에는 표시 순서가 원본과 달라지므로 행을 옮기지 않는 소스를 준다.
                var searching = IsSearching;
                var activeColumnID = grid.ActiveColumnID;
                grid.IsSourceNarrowed = searching;
                var rows = new List<int>(shownRows);
                var canReorder = !isReadOnly && kind.CanReorder && !searching;
                grid.SetSource(new TableGridListSource<int>(rows, GetRowID, canReorder), !resetScroll);

                var selected = selectedIdentity == null ? -1 : source.Find(selectedIdentity);
                grid.SelectRow(shownRowSet.Contains(selected) ? GetRowID(selected) : null, activeColumnID);
            }
            finally
            {
                restoringSelection = false;
            }

            RefreshStatus();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 개수·검색 상태·높이를 현재 상태에 맞게 표시한다.
        /// </summary>
        // ------------------------------------------------------------
        private void RefreshStatus()
        {
            var searching = isExpanded && IsSearching;
            var count = source.Count;
            titleLabel.text = label ?? source.DisplayName;
            countLabel.text = searching ? $"{shownRows.Count}/{count}" : $"{count}";
            countLabel.tooltip = searching ? "검색 결과 / 전체 항목" : "전체 항목 수";
            EnableInClassList("xeri-collection--searching", searching);
            if (!isExpanded)
            {
                return;
            }

            ApplyListHeight();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 행을 고르고 표의 키보드 이동 기준으로 둔다.
        /// </summary>
        // ------------------------------------------------------------
        private void SelectSourceRow(int sourceIndex)
        {
            if (grid == null)
            {
                return;
            }

            var shown = shownRowSet.Contains(sourceIndex);
            selectedIdentity = shown ? source.GetIdentity(sourceIndex) : null;
            grid.SelectRow(shown ? GetRowID(sourceIndex) : null, grid.ActiveColumnID);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 고른 행이 보이도록 스크롤한다. 배치 전이면 표가 배치 뒤에 맞춘다.
        /// </summary>
        // ------------------------------------------------------------
        private void ScrollToSelection()
        {
            grid?.ScrollToSelection();
        }

    #endregion

    #region 높이 정책

        private static int ClampVisibleRows(int rows)
        {
            return Math.Clamp(rows, 1, CollectionTableGridOptions.MaxVisibleRows);
        }

        private float HeaderHeight => kind.Columns.Count > 1 ? tableHeaderHeight : 0f;

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> 행 모드는 표시 행 수만큼 높이를 고정하고 초과분은 표 안에서 스크롤한다.
        /// <br/> 채움 모드는 부모 높이를 따르고 표 안에서 스크롤한다.
        /// </summary>
        // ----------------------------------------------------------------------
        private void ApplyListHeight()
        {
            // 본문이 아직 없으면 처음 펼칠 때 RefreshStatus가 높이를 다시 적용한다.
            if (grid == null)
            {
                return;
            }

            if (fillsParent)
            {
                grid.style.height = StyleKeyword.Null;
                grid.style.flexGrow = 1f;
                grid.style.flexShrink = 1f;
                return;
            }

            // 정확히 표시 행 수일 때 스크롤바가 생기지 않도록 여유를 포함한다.
            grid.style.flexGrow = 0f;
            grid.style.flexShrink = 0f;
            grid.style.height = HeaderHeight + visibleRows * rowHeight + viewportHeightAllowance;
        }

        private void SetVisibleRows(int rows)
        {
            rows = ClampVisibleRows(rows);
            if (rows == visibleRows)
            {
                return;
            }

            visibleRows = rows;
            ApplyListHeight();
        }

        private void HandleCustomStyleResolved(CustomStyleResolvedEvent evt)
        {
            var fill = evt.customStyle.TryGetValue(fillProperty, out var value) && value > 0f;
            if (fill == fillsParent)
            {
                return;
            }

            fillsParent = fill;
            EnableInClassList("xeri-collection--fill", fill);

            // 호스트가 공간을 맡긴 채움 모드에서는 접지 않고 항상 목록을 보인다.
            if (fill && !isExpanded)
            {
                SetExpanded(true);
            }

            ApplyListHeight();
        }

        private void HandleResizePointerDown(PointerDownEvent evt)
        {
            if (evt.button != 0 || fillsParent)
            {
                return;
            }

            if (evt.clickCount == 2)
            {
                SessionState.EraseInt(rowsStateKey);
                SetVisibleRows(defaultVisibleRows);
                evt.StopPropagation();
                return;
            }

            resizeStartY = evt.position.y;
            resizeStartRows = visibleRows;
            resizeHandle.CapturePointer(evt.pointerId);
            resizeHandle.AddToClassList("xeri-collection__resize-handle--active");
            evt.StopPropagation();
        }

        private void HandleResizePointerMove(PointerMoveEvent evt)
        {
            if (!resizeHandle.HasPointerCapture(evt.pointerId))
            {
                return;
            }

            var delta = evt.position.y - resizeStartY;
            SetVisibleRows(resizeStartRows + (int)Math.Round(delta / rowHeight));
            evt.StopPropagation();
        }

        private void HandleResizePointerUp(PointerUpEvent evt)
        {
            if (!resizeHandle.HasPointerCapture(evt.pointerId))
            {
                return;
            }

            resizeHandle.ReleasePointer(evt.pointerId);
            resizeHandle.RemoveFromClassList("xeri-collection__resize-handle--active");
            SessionState.SetInt(rowsStateKey, visibleRows);
            evt.StopPropagation();
        }

    #endregion

    #region 패널 연결과 구독

        private void HandleAttach(AttachToPanelEvent _)
        {
            CollectionSession.Committed -= HandleExternalChange;
            CollectionSession.Committed += HandleExternalChange;
            Undo.undoRedoPerformed -= HandleUndoRedo;
            Undo.undoRedoPerformed += HandleUndoRedo;

            // 다시 연결되면 현재 상태를 표시하되 새 변경으로 기록하지 않는다.
            displayed = Session.CaptureRevision();
            RequestRefresh();
            RefreshStatus();
            if (pendingRefresh)
            {
                refreshTask.ExecuteLater(0);
            }
        }

        private void HandleDetach(DetachFromPanelEvent _)
        {
            StopListening();
            CloseRowContextMenu();
        }

        private void StopListening()
        {
            CollectionSession.Committed -= HandleExternalChange;
            Undo.undoRedoPerformed -= HandleUndoRedo;
            refreshTask.Pause();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> 세션의 SerializedObject가 해제됐으면 알림 구독을 끊는다.
        /// <br/> 해제된 표가 다른 편집기의 확정을 실패시키지 않게 한다.
        /// </summary>
        // ------------------------------------------------------------
        private bool EnsureAlive()
        {
            if (Session.IsAlive)
            {
                return true;
            }

            StopListening();
            return false;
        }

    #endregion

    #region 변경 반영

        // ----------------------------------------------------------------------
        /// <summary>
        /// 컬렉션 필드 변경 알림을 받으면 세션이 새 세대를 확인한 경우에만 표시 갱신한다.
        /// </summary>
        // ----------------------------------------------------------------------
        private void HandleCollectionChanged()
        {
            if (panel == null || !EnsureAlive() || !Session.Observe())
            {
                return;
            }

            displayed = Session.CaptureRevision();
            RequestRefresh(source.Path);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 다른 편집기가 확정한 같은 컬렉션 변경을 표시에 반영한다.
        /// </summary>
        // ------------------------------------------------------------
        private void HandleExternalChange(UnityEngine.Object target, string path, string propertyPath)
        {
            if (!EnsureAlive() || target != Session.SerializedObject.targetObject || !SerializedPropertyHelper.IsRelatedPath(source.Path, path))
            {
                return;
            }

            // 상세 창은 별도 SerializedObject를 쓰므로 이 표의 스냅샷을 먼저 갱신한다.
            Session.SerializedObject.Update();
            var hasSelectedSource = TryGetSelectedSourceIndex(out var selectedSource);
            Session.Observe();
            SaveRevision();
            if (propertyPath != null && hasSelectedSource && selectedSource < source.Count)
            {
                selectedIdentity = source.GetIdentity(selectedSource);
            }

            // 접힌 목록은 펼칠 때 동기화하고 입력 중인 셀은 갱신을 미룬다.
            if (!isExpanded)
            {
                RefreshStatus();
            }
            else if (IsEditingCell())
            {
                RequestRefresh();
            }
            else
            {
                RefreshRows();
            }
        }

        private void HandlePropertyEdited(SerializedPropertyChangeEvent evt)
        {
            if (!isReadOnly && evt.changedProperty.serializedObject == Session.SerializedObject && EnsureAlive())
            {
                Session.Apply(CollectionEdit.CommitBound(evt.changedProperty.propertyPath));
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Undo/Redo는 사용자 편집이 아닌 확정된 외부 상태로 받아들인다.
        /// </summary>
        // ------------------------------------------------------------
        private void HandleUndoRedo()
        {
            if (panel == null || !EnsureAlive())
            {
                return;
            }

            Session.Restore();
            if (!displayed.IsCurrent)
            {
                displayed = Session.CaptureRevision();
                RequestRefresh();
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 구조 변경은 우선하고 알려진 값 변경들은 한 번의 표시 갱신으로 합친다.
        /// </summary>
        // ------------------------------------------------------------
        private void RequestRefresh(string propertyPath = null)
        {
            if (!isExpanded)
            {
                ClearPendingRefresh();
                RefreshStatus();
                return;
            }

            pendingStructureRefresh |= propertyPath == null;
            if (!pendingRefresh)
            {
                pendingRefresh = true;
                refreshTask.ExecuteLater(0);
            }
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// 값 편집은 UI를 유지하고 검색 결과나 행 구조가 바뀔 때만 다시 바인드한다.
        /// </summary>
        // ----------------------------------------------------------------------
        private void RefreshWhenIdle()
        {
            if (!pendingRefresh || !EnsureAlive())
            {
                return;
            }

            if (!isExpanded)
            {
                ClearPendingRefresh();
                RefreshStatus();
                return;
            }

            if (pendingStructureRefresh || !HasSameSourceRows())
            {
                RefreshRows();
                return;
            }

            if (IsSearching)
            {
                if (!IsEditingCell())
                {
                    RefreshRows();
                }

                return;
            }

            // 보이는 셀만 값과 진단을 다시 연결한다. 표 행은 다시 읽지 않아 입력 중인 컨트롤을 유지한다.
            ClearPendingRefresh();
            validation = Session.Validate();
            foreach (var binding in boundCells)
            {
                if (binding.SourceIndex >= 0 && binding.SourceIndex < source.Count)
                {
                    BindContent(binding);
                    RefreshCellValidation(binding);
                }
            }

            RefreshStatus();
        }

        private void ClearPendingRefresh()
        {
            pendingRefresh = false;
            pendingStructureRefresh = false;
            refreshTask.Pause();
        }

        private void SaveRevision()
        {
            displayed = Session.CaptureRevision();
        }

        private bool HasSameSourceRows()
        {
            var rows = source.GetDisplayRows();
            if (rows.Count != sourceRows.Count)
            {
                return false;
            }

            for (var index = 0; index < rows.Count; index++)
            {
                if (rows[index] != sourceRows[index])
                {
                    return false;
                }
            }

            return true;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 입력 컨트롤이 셀 값 편집을 소유하는지 확인한다.
        /// </summary>
        // ------------------------------------------------------------
        private bool IsEditingCell()
        {
            if (grid == null)
            {
                return false;
            }

            var focused = panel?.focusController?.focusedElement as VisualElement;
            while (focused != null && focused != grid)
            {
                if (focused.ClassListContains("unity-base-field") && grid.Contains(focused))
                {
                    return true;
                }

                focused = focused.parent;
            }

            return false;
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> 표에 보인 행이 원본과 달라졌는지 확인한다.
        /// <br/> 관찰하지 않은 외부 변경이나 보류 중인 구조 갱신이 있으면 달라진 것이다.
        /// <br/> 달라졌으면 이전 표시의 행 ID로 만든 요청을 실행하지 않는다.
        /// </summary>
        // ----------------------------------------------------------------------
        private bool IsDisplayStale()
        {
            return Session.Observe() || !displayed.IsCurrent || pendingStructureRefresh;
        }

    #endregion

    #region 편집 결과 표시

        // ------------------------------------------------------------
        /// <summary>
        /// 편집 결과 행을 검색 밖에서도 보이게 한다.
        /// </summary>
        // ------------------------------------------------------------
        private void RevealRow(CollectionEditResult result)
        {
            if (!result.Succeeded)
            {
                if (!string.IsNullOrEmpty(result.Reason))
                {
                    LogCommandFailure(result.Reason);
                }

                RefreshRows();
                return;
            }

            if (result.RowIndex >= 0)
            {
                RevealRow(result.RowIndex);
                return;
            }

            // 바뀐 행을 유일하게 알 수 없으면 다른 행을 임의로 선택하지 않는다.
            selectedIdentity = null;
            RefreshRows();
        }

        private void RevealRow(int sourceIndex)
        {
            if (sourceIndex < 0 || sourceIndex >= source.Count)
            {
                LogCommandFailure("항목을 만들지 못했습니다. 콘솔에서 오류를 확인하세요.");
                return;
            }

            selectedIdentity = source.GetIdentity(sourceIndex);
            UpdateRows();

            // 결과가 검색 밖이면 검색을 해제해 성공 결과를 숨기지 않는다.
            if (IsSearching && !SearchTextUtility.Matches(GetSearchText(sourceIndex), searchText))
            {
                searchField.SetTextWithoutNotify(string.Empty);
                searchText = string.Empty;
                RebuildShownRows();
            }

            RefreshView(false);

            // 값 형식 행은 같은 값이 여럿일 수 있으므로 결과 행을 직접 고른다.
            SelectSourceRow(sourceIndex);
            ScrollToSelection();
        }

        private void CloseDetailEditors()
        {
            CollectionDetailEditors.Close(Session);
        }

        private void LogCommandFailure(string message)
        {
            Debug.LogWarning(message);
        }

    #endregion

    #region 검색·추가

        private void HandleSearchTextChanged(string value)
        {
            searchText = value ?? string.Empty;
            RebuildShownRows();
            RefreshView(true);
        }

        private string GetSearchText(int sourceIndex)
        {
            var builder = new StringBuilder();
            foreach (var column in kind.Columns)
            {
                var property = source.GetCell(sourceIndex, column);
                SearchTextUtility.Append(builder, CollectionCellDrawers.Resolve(column).GetSummary(property, column));
            }

            return builder.ToString();
        }

        private void HandleAdd(VisualElement anchor)
        {
            if (!isReadOnly)
            {
                addFlow.Start(anchor);
            }
        }

    #endregion

    #region 선택과 표 요청

        private void HandleSelectionChanged(object sender, TableGridSelectionChanged evt)
        {
            if (!restoringSelection)
            {
                selectedIdentity = TryGetSelectedSourceIndex(out var selected) ? source.GetIdentity(selected) : null;
            }

            RefreshStatus();
        }

        private bool TryGetSelectedSourceIndex(out int sourceIndex)
        {
            sourceIndex = -1;
            if (grid == null)
            {
                return false;
            }

            var selection = grid.Selection;
            if (selection.Kind != TableGridSelectionKind.Row || selection.RowIDs.Count == 0)
            {
                return false;
            }

            return TryGetRowIndex(selection.RowIDs[0], out sourceIndex) && IsCurrentRow(sourceIndex);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 직렬화 인덱스가 지금 표에 보이는 행인지 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        private bool IsCurrentRow(int sourceIndex)
        {
            return sourceIndex >= 0 && sourceIndex < source.Count && shownRowSet.Contains(sourceIndex);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> 셀 입력칸(Inline Confirm)이 확정한 값을 세션에 쓴다.
        /// <br/> 초안 열은 초안 검증(키 중복 등)을 통과했을 때만 쓴다.
        /// <br/> 거부하면 원래 값을 다시 보인다.
        /// </summary>
        // ------------------------------------------------------------
        private void HandleEditRequested(object sender, TableGridEditRequest request)
        {
            if (isReadOnly || request.Edits.Count == 0)
            {
                return;
            }

            // 화면에 남은 행과 원본이 다르면 이전 표시의 행 ID로 쓰지 않는다.
            if (IsDisplayStale())
            {
                LogCommandFailure("행이 변경되어 편집을 반영하지 않았습니다.");
                RefreshRows();
                return;
            }

            // 셀 하나씩 확정하는 입력만 받는다. 쓰면 행 순서가 바뀔 수 있어 이어지는 행 ID를 믿을 수 없다.
            if (request.Edits.Count != 1)
            {
                LogCommandFailure("여러 셀을 한 번에 바꿀 수 없습니다.");
                RefreshRows();
                return;
            }

            var edit = request.Edits[0];
            var column = kind.FindColumn(edit.ColumnID);
            if (column == null || !TryGetRowIndex(edit.RowID, out var sourceIndex) || !IsCurrentRow(sourceIndex))
            {
                RefreshRows();
                return;
            }

            ApplyCellEdit(sourceIndex, column, edit.Value);
        }

        private void HandleDetailRequested(object sender, TableGridDetailRequest request)
        {
            OpenRequestedCell(request.RowID, request.ColumnID);
        }

        private void HandleRowActivated(object sender, TableGridRowActivated evt)
        {
            OpenRequestedCell(evt.RowID, evt.ColumnID);
        }

        private void OpenRequestedCell(string rowID, string columnID)
        {
            var column = kind.FindColumn(columnID) ?? kind.Columns[0];
            if (TryGetRowIndex(rowID, out var sourceIndex) && IsCurrentRow(sourceIndex))
            {
                OpenDetail(sourceIndex, column);
            }
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> 끌기 손잡이·Alt+방향키의 행 이동 요청을 삽입형 이동 편집으로 바꾼다.
        /// <br/> 행 선택은 한 행이므로 한 행만 옮긴다. 검색 중에는 표가 요청하지 않는다.
        /// </summary>
        // ----------------------------------------------------------------------
        private void HandleMoveRowsRequested(object sender, TableGridMoveRowsRequest request)
        {
            if (isReadOnly || !kind.CanReorder || IsSearching || request.RowIDs.Count != 1)
            {
                return;
            }

            if (IsDisplayStale())
            {
                LogCommandFailure("행이 변경되어 이동하지 않았습니다.");
                RefreshRows();
                return;
            }

            if (!TryGetRowIndex(request.RowIDs[0], out var sourceIndex) || !IsCurrentRow(sourceIndex)
                || !TryGetRowIndex(request.TargetRowID, out var targetIndex) || !IsCurrentRow(targetIndex))
            {
                RefreshRows();
                return;
            }

            // 옮길 행을 빼면 그 뒤 행이 한 칸 당겨지므로 대상보다 앞에서 오면 한 칸 줄인다.
            var destinationIndex = request.Placement == TableGridMovePlacement.Before
                ? (sourceIndex < targetIndex ? targetIndex - 1 : targetIndex)
                : (sourceIndex < targetIndex ? targetIndex : targetIndex + 1);
            MoveRowToIndex(sourceIndex, destinationIndex);
        }

    #endregion

    #region 키보드 명령

        // ------------------------------------------------------------
        /// <summary>
        /// 표에 포커스가 있을 때 행 명령 단축키를 표보다 먼저 처리한다.
        /// </summary>
        // ------------------------------------------------------------
        private void HandleShortcutKeyDown(KeyDownEvent evt)
        {
            // 셀 안 입력 컨트롤의 키는 그 컨트롤이 쓴다.
            if (grid == null || evt.target != grid)
            {
                return;
            }

            if (HandleShortcut(evt))
            {
                StopShortcutEvent(evt);
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> Shift+F10 행 메뉴, Enter 상세 편집, Delete 행 삭제와
        /// <br/> Ctrl·Cmd+C/V/D 행 복사·붙여넣기·복제를 처리한다.
        /// <br/> 행 복사와 섞이는 표 기본 잘라내기(Ctrl·Cmd+X)와
        /// <br/> Windows Backspace 지우기는 막는다.
        /// </summary>
        // ------------------------------------------------------------
        private bool HandleShortcut(KeyDownEvent evt)
        {
            // 키보드 사용자도 Shift+F10으로 같은 행 메뉴를 열 수 있다. 활성 셀 아래에 열고, 보이지 않으면 표 왼쪽 위다.
            if (evt.shiftKey && evt.keyCode == KeyCode.F10)
            {
                var position = grid.TryGetActiveCellBounds(out var cell) ? new Vector2(cell.xMin, cell.yMax) : grid.worldBound.position;
                ShowRowContextMenu(position);
                return true;
            }

            if (XeriShortcutKeys.IsSubmit(evt.keyCode))
            {
                OpenSelectedCell();
                return true;
            }

            var actionKey = evt.ctrlKey || evt.commandKey;
            Action command = actionKey && evt.keyCode == KeyCode.C ? HandleCopySelected
                : actionKey && evt.keyCode == KeyCode.V ? HandlePasteSelected
                : actionKey && evt.keyCode == KeyCode.D ? HandleDuplicateSelected
                : evt.keyCode == KeyCode.Delete || (XeriShortcutKeys.IsMac && evt.keyCode == KeyCode.Backspace) ? HandleRemove
                : null;
            if (command != null)
            {
                command();
                return true;
            }

            return (actionKey && evt.keyCode == KeyCode.X) || evt.keyCode == KeyCode.Backspace;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 표가 쓰지 않은 Escape로 선택을 해제한다.
        /// </summary>
        // ------------------------------------------------------------
        private void HandleEscapeKeyDown(KeyDownEvent evt)
        {
            if (grid == null || evt.target != grid || evt.keyCode != KeyCode.Escape)
            {
                return;
            }

            selectedIdentity = null;
            grid.SelectRow(null, null);
            grid.Focus();
            StopShortcutEvent(evt);
        }

        private void OpenSelectedCell()
        {
            var column = kind.FindColumn(grid.ActiveColumnID) ?? kind.Columns[0];
            if (TryGetSelectedSourceIndex(out var selected))
            {
                OpenDetail(selected, column);
            }
        }

        private bool IsEditingFieldTarget(VisualElement element)
        {
            while (element != null && element != grid)
            {
                if (element is TextField || element.ClassListContains("unity-base-field"))
                {
                    return true;
                }

                element = element.parent;
            }

            return false;
        }

        private static void StopShortcutEvent(KeyDownEvent evt)
        {
            evt.StopPropagation();
            evt.StopImmediatePropagation();
        }

    #endregion

    #region 행 메뉴

        // --------------------------------------------------------------------------------
        /// <summary>
        /// <br/> 셀 안 입력 컨트롤의 우클릭과 macOS Ctrl+클릭은 표가 행 메뉴를 열지 않으므로
        /// <br/> 여기서 연다. 그 밖의 우클릭은 표가 행을 고른 뒤 HostContextMenu로 넘긴다.
        /// </summary>
        // --------------------------------------------------------------------------------
        private void HandleInlineContextMouseUp(MouseUpEvent evt)
        {
            if (grid == null)
            {
                return;
            }

            var target = evt.target as VisualElement;
            var macContextClick = XeriShortcutKeys.IsMac && evt.button == 0 && evt.ctrlKey;
            if (!(evt.button == 1 || macContextClick) || !TryGetCellBinding(target, out var binding) || !IsCurrentRow(binding.SourceIndex))
            {
                return;
            }

            if (!macContextClick && !IsInlineControlTarget(binding, target))
            {
                return;
            }

            SelectSourceRow(binding.SourceIndex);
            ShowRowContextMenu(evt.mousePosition);
            evt.StopImmediatePropagation();
        }

        private bool TryGetCellBinding(VisualElement target, out CellBinding binding)
        {
            binding = null;
            for (var element = target; element != null && element != grid; element = element.parent)
            {
                if (element.ClassListContains("xeri-collection__cell") && element.userData is CellBinding found)
                {
                    binding = found;
                    return found.SourceIndex >= 0;
                }
            }

            return false;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 표가 포인터를 양보하는 Inline 셀 안 컨트롤을 눌렀는지 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        private bool IsInlineControlTarget(CellBinding binding, VisualElement target)
        {
            if (GetGridEdit(binding.Column).Kind != TableGridCellEditKind.Inline)
            {
                return false;
            }

            for (var element = target; element != null && element != binding.Root; element = element.parent)
            {
                if (element.focusable)
                {
                    return true;
                }
            }

            return false;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 고른 행 메뉴를 단축키 열이 있는 팝업으로 연다.
        /// </summary>
        // ------------------------------------------------------------
        private void ShowRowContextMenu(Vector2 worldPosition)
        {
            CloseRowContextMenu();

            // 화면에 남은 행과 현재 원본이 다르면 이전 표시에서 명령을 만들지 않는다(편집 요청과 같은 판정).
            if (IsDisplayStale())
            {
                RequestRefresh();
                return;
            }

            var hasSelection = TryGetSelectedSourceIndex(out var sourceIndex);
            var rowLocator = hasSelection ? new SerializedPropertyLocator(source.GetRow(sourceIndex)) : null;
            var canEdit = hasSelection && !isReadOnly;
            var canPaste = canEdit && kind.Supports(CollectionEdits.Paste)
                && SerializedPropertyClipboard.CanPasteValue(source.GetRow(sourceIndex), out _);
            var originalMark = Session.CaptureRevision();
            var deleteShortcut = XeriShortcutKeys.Format(XeriShortcutKeys.IsMac ? KeyCode.Backspace : KeyCode.Delete);

            void Run(Action execute) => ExecuteContextRowAction(sourceIndex, originalMark, rowLocator, execute);

            var items = new List<XeriContextMenuItem>
            {
                XeriContextMenuItem.Command
                (
                    "행 복제", () => Run(HandleDuplicateSelected),
                    enabled: canEdit && kind.Supports(CollectionEdits.Duplicate), shortcut: XeriShortcutKeys.Format(KeyCode.D, action: true)
                ),
                XeriContextMenuItem.Separator(),
                XeriContextMenuItem.Command("행 전체 복사", () => Run(HandleCopySelected), enabled: hasSelection, shortcut: XeriShortcutKeys.Format(KeyCode.C, action: true)),
                XeriContextMenuItem.Command("행 전체 붙여넣기", () => Run(HandlePasteSelected), enabled: canPaste, shortcut: XeriShortcutKeys.Format(KeyCode.V, action: true)),
                XeriContextMenuItem.Separator(),
                XeriContextMenuItem.Command
                (
                    "위로 이동", () => Run(() => HandleMoveSelected(-1)),
                    enabled: hasSelection && CanMoveSelected(-1, out _), shortcut: XeriShortcutKeys.Format(KeyCode.UpArrow, alt: true)
                ),
                XeriContextMenuItem.Command
                (
                    "아래로 이동", () => Run(() => HandleMoveSelected(1)),
                    enabled: hasSelection && CanMoveSelected(1, out _), shortcut: XeriShortcutKeys.Format(KeyCode.DownArrow, alt: true)
                ),
                XeriContextMenuItem.Command
                (
                    "위치로 이동...", () => Run(OpenMoveIndex),
                    enabled: canEdit && kind.CanReorder && !IsSearching
                ),
                XeriContextMenuItem.Separator(),
                XeriContextMenuItem.Command
                (
                    "행 삭제", () => Run(HandleRemove),
                    enabled: canEdit && kind.Supports(CollectionEdits.Remove), shortcut: deleteShortcut
                ),
            };
            rowContextMenu.Open(worldPosition, items);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 메뉴를 연 뒤 셀 재사용이나 원본 변경이 생겨도 다른 행을 고치지 않는다.
        /// </summary>
        // ------------------------------------------------------------
        private void ExecuteContextRowAction
        (
            int sourceIndex, RevisionMark originalMark, SerializedPropertyLocator rowLocator, Action execute
        )
        {
            // UI 관찰 콜백보다 먼저 명령이 실행돼도 원본 변경을 검사한다.
            Session.Observe();
            if (!originalMark.IsCurrent
                || !IsCurrentRow(sourceIndex)
                || rowLocator == null
                || !rowLocator.TryResolve(Session.SerializedObject, out _, out _))
            {
                LogCommandFailure("행이 변경되어 명령을 실행하지 않았습니다.");
                RefreshRows();
                return;
            }

            if (!TryGetSelectedSourceIndex(out var selected) || selected != sourceIndex)
            {
                SelectSourceRow(sourceIndex);
            }

            execute();
        }

        // 메뉴 공용 키 처리를 제외한 컬렉션 명령 단축키만 표로 넘긴다.
        private void HandleRowContextMenuKeyDown(KeyDownEvent evt)
        {
            if (evt.ctrlKey || evt.commandKey || evt.keyCode == KeyCode.Delete
                || (XeriShortcutKeys.IsMac && evt.keyCode == KeyCode.Backspace))
            {
                CloseRowContextMenu();
                grid.Focus();
                if (HandleShortcut(evt))
                {
                    StopShortcutEvent(evt);
                }
            }
        }

        private void CloseRowContextMenu()
        {
            rowContextMenu?.Close();
        }

    #endregion

    #region 행 명령

        private void HandleCopySelected()
        {
            if (!TryGetSelectedSourceIndex(out var sourceIndex))
            {
                return;
            }

            Session.SerializedObject.UpdateIfRequiredOrScript();
            if (!SerializedPropertyClipboard.TryCopyValue(source.GetRow(sourceIndex), out var reason))
            {
                LogCommandFailure($"행 복사 실패: {reason}");
            }
        }

        private void HandlePasteSelected()
        {
            if (isReadOnly || !TryGetSelectedSourceIndex(out var sourceIndex))
            {
                return;
            }

            CloseDetailEditors();
            var result = Session.Apply(CollectionEdit.Paste(sourceIndex));
            if (!result.Succeeded)
            {
                LogCommandFailure($"행 붙여넣기 실패: {result.Reason}");
                return;
            }

            RevealRow(result);
        }

        private void HandleDuplicateSelected()
        {
            if (isReadOnly || !TryGetSelectedSourceIndex(out var sourceIndex))
            {
                return;
            }

            CloseDetailEditors();
            RevealRow(Session.Apply(CollectionEdit.Duplicate(sourceIndex)));
        }

        private void HandleRemove()
        {
            if (!isReadOnly && TryGetSelectedSourceIndex(out var sourceIndex))
            {
                RemoveRowAndSelectNext(sourceIndex);
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 재사용된 셀의 오래된 삭제 버튼이 다른 행을 지우지 않게 한다.
        /// </summary>
        // ------------------------------------------------------------
        private void HandleRemoveAt(int sourceIndex)
        {
            if (isReadOnly)
            {
                return;
            }

            if (!IsCurrentRow(sourceIndex))
            {
                RefreshRows();
                return;
            }

            RemoveRowAndSelectNext(sourceIndex);
        }

        private void RemoveRowAndSelectNext(int sourceIndex)
        {
            var position = shownRows.IndexOf(sourceIndex);
            var removedIndices = new[]
            {
                sourceIndex
            };
            selectedIdentity = null;
            ApplyRowsEdit(CollectionEdit.Remove(removedIndices));

            // 지운 자리(없으면 마지막 행)를 이어서 고른다.
            var nextPosition = Math.Min(position, shownRows.Count - 1);
            if (position >= 0 && nextPosition >= 0)
            {
                SelectSourceRow(shownRows[nextPosition]);
            }
        }

        private void HandleMoveSelected(int offset)
        {
            if (CanMoveSelected(offset, out var sourceIndex))
            {
                MoveRowToIndex(sourceIndex, sourceIndex + offset);
            }
        }

        private bool CanMoveSelected(int offset, out int sourceIndex)
        {
            sourceIndex = -1;
            if (isReadOnly || !kind.CanReorder || IsSearching || !TryGetSelectedSourceIndex(out sourceIndex))
            {
                return false;
            }

            var destinationIndex = sourceIndex + offset;
            return destinationIndex >= 0 && destinationIndex < source.Count;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 선택 행의 대상 위치를 입력받고 바뀌지 않은 원본에만 적용한다.
        /// </summary>
        // ------------------------------------------------------------
        private void OpenMoveIndex()
        {
            if (isReadOnly || !kind.CanReorder || IsSearching || !TryGetSelectedSourceIndex(out var sourceIndex))
            {
                return;
            }

            if (Session.Observe())
            {
                RefreshRows();
                return;
            }

            var originalCount = source.Count;
            var originalMark = Session.CaptureRevision();
            var rowLocator = new SerializedPropertyLocator(source.GetRow(sourceIndex));
            CollectionRowMoveWindow.Open
            (
                sourceIndex, originalCount,
                destinationIndex =>
                {
                    if (panel == null)
                    {
                        return;
                    }

                    Session.Observe();
                    var unchanged = source.Count == originalCount && originalMark.IsCurrent
                        && rowLocator.TryResolve(Session.SerializedObject, out _, out _)
                        && TryGetSelectedSourceIndex(out var selected) && selected == sourceIndex
                        && !IsSearching;
                    if (!unchanged)
                    {
                        LogCommandFailure("선택 또는 컬렉션이 변경되어 이동하지 않았습니다.");
                        return;
                    }

                    MoveRowToIndex(sourceIndex, destinationIndex);
                }
            );
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 메뉴·끌기·키보드·위치 입력에 같은 삽입형 이동 편집을 적용한다.
        /// </summary>
        // ------------------------------------------------------------
        private void MoveRowToIndex(int sourceIndex, int destinationIndex)
        {
            if (isReadOnly || !kind.CanReorder || sourceIndex == destinationIndex)
            {
                return;
            }

            CloseDetailEditors();
            RevealRow(Session.Apply(CollectionEdit.Move(sourceIndex, destinationIndex)));
        }

    #endregion

    #region 컬렉션 관리 메뉴

        // ------------------------------------------------------------
        /// <summary>
        /// 컬렉션 전체 명령(모두 삭제, 무효 정리, 키 자동 채우기) 메뉴를 연다.
        /// </summary>
        // ------------------------------------------------------------
        private void ShowCollectionActions()
        {
            var current = Session.Validate();
            var canRemove = !isReadOnly && kind.Supports(CollectionEdits.Remove);
            var items = new List<XeriContextMenuItem>
            {
                XeriContextMenuItem.Command("모든 항목 삭제", HandleClear, enabled: canRemove && source.Count > 0),
                XeriContextMenuItem.Command
                (
                    "무효한 항목 정리", HandleRemoveInvalid, enabled: canRemove && current.InvalidRows.Count > 0
                ),
            };
            AppendPopulateItems(items);
            collectionActionsMenu.OpenBelow(items);
        }

        private void HandleClear()
        {
            var rows = new List<int>(source.Count);
            for (var index = 0; index < source.Count; index++)
            {
                rows.Add(index);
            }

            ApplyRowsEdit(CollectionEdit.Remove(rows, "컬렉션 전체 삭제"));
        }

        private void HandleRemoveInvalid()
        {
            // 메뉴를 연 뒤 바뀐 상태를 반영하도록 실행 시점에 다시 검증한다.
            Session.Observe();
            ApplyRowsEdit(CollectionEdit.Remove(Session.Validate().InvalidRows, "무효한 컬렉션 항목 정리"));
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 정수·열거형 키 컬렉션에 키 자동 채우기 명령을 추가한다.
        /// </summary>
        // ------------------------------------------------------------
        private void AppendPopulateItems(List<XeriContextMenuItem> items)
        {
            var keyType = kind.KeyColumn?.ValueType;
            if (isReadOnly || !kind.Supports(CollectionEdits.KeySet) || keyType == null)
            {
                return;
            }

            Action<CollectionKeySetOperation> execute;
            if (keyType.IsEnum)
            {
                execute = operation => ApplyRowsEdit(CollectionEdit.ApplyKeySet(GetEnumKeys(keyType), operation));
            }
            else if (keyType == typeof(int))
            {
                execute = operation => CollectionKeyRangeWindow.Open(Session, operation);
            }
            else
            {
                return;
            }

            items.Add(XeriContextMenuItem.Separator());
            items.Add(XeriContextMenuItem.Command("키 자동 채우기/추가", () => execute(CollectionKeySetOperation.Add)));
            items.Add(XeriContextMenuItem.Command("키 자동 채우기/제거", () => execute(CollectionKeySetOperation.Remove)));
            items.Add(XeriContextMenuItem.Command("키 자동 채우기/지정 범위만 유지", () => execute(CollectionKeySetOperation.Confine)));
        }

        private static IReadOnlyList<object> GetEnumKeys(Type enumType)
        {
            var values = Enum.GetValues(enumType);
            var keys = new List<object>(values.Length);
            foreach (var value in values)
            {
                keys.Add(value);
            }

            return keys;
        }

        private void ApplyRowsEdit(CollectionEdit edit)
        {
            if (isReadOnly)
            {
                return;
            }

            CloseDetailEditors();
            var result = Session.Apply(edit);
            if (!result.Succeeded && !string.IsNullOrEmpty(result.Reason))
            {
                LogCommandFailure(result.Reason);
            }

            RefreshRows();
        }

    #endregion

    #region 셀 요청 처리

        // ------------------------------------------------------------
        /// <summary>
        /// 셀이 아직 같은 직렬화 위치를 가리키면 현재 속성을 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public bool TryResolve
        (
            int sourceIndex, CollectionColumn column, string propertyPath, out SerializedProperty property
        )
        {
            property = null;
            if (panel == null || sourceIndex < 0 || sourceIndex >= source.Count)
            {
                return false;
            }

            property = source.GetCell(sourceIndex, column);
            return property != null && property.propertyPath == propertyPath;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> 셀 입력칸으로 고칠 수 없는 초안 열(복합 값 등)은 초안 창으로,
        /// <br/> 그 밖에는 셀 값을 바꾼다(초안 열은 세션이 검증한 뒤 쓴다).
        /// </summary>
        // ------------------------------------------------------------
        public void Replace(int sourceIndex, CollectionColumn column, object value)
        {
            if (isReadOnly)
            {
                return;
            }

            if (column.Edit == CollectionCellEdit.Draft && GetGridEdit(column).Kind != TableGridCellEditKind.Inline)
            {
                CollectionDraftWindow.OpenForRow(Session, sourceIndex, column, value, RevealRow);
                return;
            }

            ApplyCellEdit(sourceIndex, column, value);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 셀 안에서 이미 쓴 값을 확정한다.
        /// </summary>
        // ------------------------------------------------------------
        public void Commit(string propertyPath)
        {
            if (!isReadOnly)
            {
                Session.Apply(CollectionEdit.CommitBound(propertyPath));
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 셀의 상세 편집 또는 초안 편집을 연다.
        /// </summary>
        // ------------------------------------------------------------
        public void OpenDetail(int sourceIndex, CollectionColumn column)
        {
            if (!CollectionDetailEditors.OpenCell(Session, sourceIndex, column, RevealRow))
            {
                LogCommandFailure("목록에서 직접 수정하세요. 참조가 없으면 형식을 선택하세요.");
            }
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// 셀 값을 세션으로 쓰고 결과 행을 보인다. 거부되면 원래 값을 다시 보인다.
        /// </summary>
        // ----------------------------------------------------------------------
        private void ApplyCellEdit(int sourceIndex, CollectionColumn column, object value)
        {
            CloseDetailEditors();
            RevealRow(Session.Apply(CollectionEdit.SetCell(sourceIndex, column.ID, value)));
        }

    #endregion

    }
}
