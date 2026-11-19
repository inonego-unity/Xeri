/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : CollectionTable.cs
수정일 : 2026-11-19

# 설명
컬렉션 편집 세션을 Inspector 표로 보여 준다: 검색·선택·페이지·행 메뉴·키보드·드래그·높이 조절.
셀은 열마다 고른 셀 drawer가 그리고, 모든 변경은 CollectionSession.Apply로 요청한다.
행 검증 아이콘은 드래그 핸들 뒤에 두고, 읽기 전용에서도 탐색·복사·상세 보기를 허용한다.

# 특이사항, 제약사항
기본은 지정 행 수 높이(하단 손잡이로 조절)이며, 호스트 USS가 채움 모드를 켜면
부모 높이를 따르고 들어가는 행 수로 페이지를 나눠 내부 스크롤을 만들지 않는다.
화면에 남은 행과 원본이 다르면 이전 표시에서 만든 행 명령을 실행하지 않는다.
접힌 표는 헤더만 만들고 행 목록·행 메뉴·드래그는 처음 펼칠 때 만든다(인스펙터 로드 비용 절감).
표는 객체 전체가 아니라 자기 컬렉션 필드만 감시한다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

using inonego;
using inonego.Xeri;
using inonego.Xeri.UI;

namespace inonego.Xeri.Serializable.Editor
{
    // ============================================================
    /// <summary>
    /// 컬렉션 편집 세션의 Inspector 표.
    /// </summary>
    // ============================================================
    internal sealed class CollectionTable : VisualElement, ICollectionCellHost
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
            internal int PageIndex = -1;
            internal CollectionColumn Column;
            internal ICollectionCellDrawer Drawer;
            internal VisualElement Row;
            internal VisualElement Content;
            internal VisualElement DragHandle;
            internal SeverityIndicator Validation;
            internal Button RowEditButton;
        }

    #endregion

    #region 필드

        // 실제 행·헤더 크기는 CollectionTableStyle.uss의 28px과 일치해야 한다.
        private const float rowHeight = 28f;
        private const float tableHeaderHeight = 28f;
        private const float viewportHeightAllowance = 6f;
        private const int minimumPageSize = 10;
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
        private readonly List<int> sourceRows = new();
        private readonly SearchPagedList<int> searchPagedRows;
        private readonly PageNavigationController<int> navigation;
        private readonly VisualElement rowsHost;
        private readonly VisualElement resizeHandle;
        private readonly Label titleLabel;
        private readonly Button foldoutButton;
        private readonly Label emptyLabel;
        private readonly Label countLabel;
        private readonly ToolbarSearchField searchField;
        private readonly PagedListChrome chrome;
        private readonly XeriContextMenu collectionActionsMenu;
        private readonly IVisualElementScheduledItem refreshTask;

        // 행 목록 본문과 페이지 바는 처음 펼칠 때 EnsureBody가 만든다. 접힌 표는 헤더만 만든다.
        private BaseListView listView;
        private PaginationBar paginationBar;
        private CollectionRowDragController rowDrag;
        private XeriContextMenu rowContextMenu;

        private CollectionValidation validation = new();
        private bool? hasUnlistedFields;
        private int visibleRows;
        private bool fillsParent;
        private float resizeStartY;
        private int resizeStartRows;
        private bool isExpanded;
        private object selectedIdentity;
        private string selectedColumnID;
        private bool restoringSelection;
        private int renderedPageItemCount;
        private bool pendingRefresh;
        private bool pendingStructureRefresh;
        private ulong displayedRevision;

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// 편집 세션으로 표를 구성한다.
        /// </summary>
        // ------------------------------------------------------------
        public CollectionTable(CollectionSession session, CollectionTableOptions? options = null)
        {
            Session = session ?? throw new ArgumentNullException(nameof(session));
            kind = session.Kind;
            source = session.Source;
            isReadOnly = session.IsReadOnly;
            hasFieldColumns = HasFieldColumns(kind);
            addFlow = new CollectionAddFlow(session, () => panel != null, LogCommandFailure, RevealRow, RevealRow, CloseDetailEditors);
            var settings = options ?? CollectionTableOptions.Default();
            label = settings.Label;
            selectedColumnID = kind.Columns[0].ID;
            refreshTask = schedule.Execute(RefreshWhenIdle);
            refreshTask.Pause();
            var stateKey = $"{session.SerializedObject.targetObject.GetEntityId()}.{source.Path}";
            foldoutStateKey = $"UniXeri.CollectionEditor.Foldout.{stateKey}";
            isExpanded = SessionState.GetBool(foldoutStateKey, false);

            // 사용자가 조절한 행 수는 같은 대상과 경로에 대해 편집 세션 동안 유지한다.
            rowsStateKey = $"UniXeri.CollectionEditor.Rows.{stateKey}";
            defaultVisibleRows = settings.VisibleRows;
            visibleRows = ClampVisibleRows(SessionState.GetInt(rowsStateKey, defaultVisibleRows));
            searchPagedRows = new SearchPagedList<int>(sourceRows, GetSearchText, Math.Max(minimumPageSize, visibleRows));
            navigation = new PageNavigationController<int>(searchPagedRows);
            AddToClassList("xeri-collection");
            EnableInClassList("xeri-collection--multi-column", kind.Columns.Count > 1);
            styleSheets.Add(CollectionEditorResources.LoadTableStyle());

            // 구조는 UXML, 스타일은 USS, 데이터 연결과 명령은 표가 담당한다.
            var content = new VisualElement();
            CollectionEditorResources.LoadTableLayout().CloneTree(content);
            chrome = new PagedListChrome();
            Add(chrome);

            var header = content.Q<VisualElement>("collection-header");
            var search = content.Q<VisualElement>("collection-search");
            var add = content.Q<VisualElement>("collection-add");
            var rows = content.Q<VisualElement>("collection-rows-host");
            if (header == null || search == null || add == null || rows == null)
            {
                throw new InvalidOperationException("CollectionTable의 UXML 구성이 잘못되었습니다.");
            }

            chrome.HeaderHost.Add(header);
            chrome.SearchHost.Add(search);
            chrome.ToolbarActionHost.Add(add);
            chrome.ContentHost.Add(rows);

            titleLabel = this.Q<Label>("collection-title");
            titleLabel.tooltip = kind.Description;
            foldoutButton = this.Q<Button>("collection-foldout")
                ?? throw new InvalidOperationException("CollectionTable 접기 버튼이 없습니다.");
            foldoutButton.clicked += () => SetExpanded(!isExpanded);
            countLabel = this.Q<Label>("collection-count");
            emptyLabel = this.Q<Label>("collection-empty");
            searchField = this.Q<ToolbarSearchField>("collection-search");
            searchField.tooltip = "항목 검색";
            searchField.RegisterValueChangedCallback(evt => HandleSearchTextChanged(evt.newValue));

            var actionsButton = this.Q<Button>("collection-actions");
            actionsButton.text = string.Empty;
            actionsButton.iconImage = XeriIcon.LoadBackground("ellipsis");
            actionsButton.tooltip = "컬렉션 관리";
            actionsButton.SetEnabled(!isReadOnly);
            collectionActionsMenu = new XeriContextMenu(actionsButton);
            actionsButton.clicked += ShowCollectionActions;

            // 제목·빈 헤더를 눌러도 접되, 관리 메뉴와 접기 버튼 클릭은 중복 처리하지 않는다. 채움 모드는 항상 펼친다.
            header.RegisterCallback<ClickEvent>
            (
                evt =>
                {
                    if (fillsParent)
                    {
                        return;
                    }

                    if (evt.target is VisualElement target
                        && (target == actionsButton || actionsButton.Contains(target)
                            || target == foldoutButton || foldoutButton.Contains(target)))
                    {
                        return;
                    }

                    SetExpanded(!isExpanded);
                }
            );

            var addButton = this.Q<Button>("collection-add");
            addButton.text = string.Empty;
            addButton.iconImage = XeriIcon.LoadBackground("plus");
            addButton.tooltip = "항목 추가";
            addButton.SetEnabled(!isReadOnly && kind.Supports(CollectionEdits.Add));
            addButton.clicked += () => HandleAdd(addButton);

            rowsHost = rows;

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
            chrome.ContentHost.Add(resizeHandle);

            // 부모를 채우는 배치는 호스트 USS가 --xeri-collection-fill로 지정한다.
            RegisterCallback<CustomStyleResolvedEvent>(HandleCustomStyleResolved);
            rowsHost.RegisterCallback<GeometryChangedEvent>(HandleRowsGeometryChanged);
            emptyLabel.pickingMode = PickingMode.Ignore;

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
        /// 헤더 컨트롤러와 첫 행 바인딩을 보존하며 펼침 상태를 바꾼다.
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

        // ------------------------------------------------------------
        /// <summary>
        /// 행 목록과 행 메뉴·드래그를 처음 필요할 때 한 번 만든다.
        /// </summary>
        // ------------------------------------------------------------
        private void EnsureBody()
        {
            if (listView != null)
            {
                return;
            }

            listView = CreateListView();
            listView.name = "collection-rows";
            rowContextMenu = new XeriContextMenu(listView, HandleRowContextMenuKeyDown);
            listView.selectionType = SelectionType.Single;
            listView.reorderable = false;
            listView.reorderMode = ListViewReorderMode.Simple;
            listView.fixedItemHeight = rowHeight;
            listView.showAlternatingRowBackgrounds = AlternatingRowBackground.ContentOnly;
            listView.focusable = true;
            rowDrag = new CollectionRowDragController(Session, sourceRows, searchPagedRows, listView, SelectDragPageRow, MoveRowToIndex);
            listView.itemsSource = searchPagedRows.PageItemsSource;
            listView.selectionChanged += HandleSelectionChanged;
            renderedPageItemCount = searchPagedRows.PageItems.Count;

            // 기본 DropdownMenu는 "/"를 계층 구분자로 쓰고 단축키 열이 없으므로 자체 메뉴를 쓴다.
            listView.RegisterCallback<MouseUpEvent>(HandleContextMouseUp, TrickleDown.TrickleDown);
            listView.RegisterCallback<KeyDownEvent>(HandleKeyDown, TrickleDown.TrickleDown);
            listView.RegisterCallback<FocusOutEvent>
            (
                _ =>
                {
                    if (pendingRefresh)
                    {
                        refreshTask.ExecuteLater(0);
                    }
                }
            );

            // 빈 상태 안내는 목록 위에 겹쳐 높이를 바꾸지 않는다.
            rowsHost.Insert(0, listView);
            chrome.BindPageListView(listView);
            paginationBar = new PaginationBar(searchPagedRows.Paginator, HandlePageNavigation);
            chrome.PaginationHost.Add(paginationBar);

            // 이미 패널에 붙은 뒤 만들어졌으면 붙을 때 하던 드래그 연결을 여기서 한다.
            if (panel != null && !isReadOnly)
            {
                rowDrag.Bind();
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 열이 하나면 헤더 없는 ListView, 여럿이면 다중 열 표를 만든다.
        /// </summary>
        // ------------------------------------------------------------
        private BaseListView CreateListView()
        {
            if (kind.Columns.Count == 1)
            {
                // 다중 열 표처럼 셀을 항목 안에 둔다. 셀이 곧 항목이면 Unity 항목 스타일이 셀 여백을 덮어 행이 위로 치우친다.
                var column = kind.Columns[0];
                return new ListView
                {
                    makeItem = () => MakeItem(column),
                    bindItem = (item, index) => BindCell(item[0], index),
                    unbindItem = (item, index) => UnbindCell(item[0], index),
                };
            }

            // 고정 열은 유지하고 가변 열은 남는 폭을 채운다. 창 폭에 따라 달라지는 열 폭은 저장하지 않는다.
            var table = new MultiColumnListView();
            table.sortingMode = ColumnSortingMode.None;
            table.columns.resizable = true;
            table.columns.resizePreview = false;
            var layouts = new List<TableColumnLayout>(kind.Columns.Count);
            var lastColumn = kind.Columns[kind.Columns.Count - 1];
            foreach (var column in kind.Columns)
            {
                var spec = column;
                var layout = spec == lastColumn ? ReserveRowActions(spec.Layout) : spec.Layout;
                var tableColumn = TableColumnSizing.CreateColumn(spec.ID, spec.Header, layout);
                tableColumn.resizable = layout.Mode == TableColumnLayoutMode.Flexible;
                tableColumn.sortable = false;
                tableColumn.makeHeader = () => MakeColumnHeader(spec);
                tableColumn.makeCell = () => MakeCell(spec);
                tableColumn.bindCell = BindCell;
                tableColumn.unbindCell = UnbindCell;
                table.columns.Add(tableColumn);
                layouts.Add(layout);
            }

            table.ApplyColumnSizing(layouts);
            return table;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 행 버튼(상세 편집·삭제)이 들어가는 마지막 열에 버튼 폭을 더한다.
        /// </summary>
        // ------------------------------------------------------------
        private TableColumnLayout ReserveRowActions(TableColumnLayout layout)
        {
            // 아이콘 버튼은 20px + 좌우 여백 2px씩이다(CollectionTableStyle.uss).
            const float buttonWidth = 24f;
            var reserved = (hasFieldColumns ? 2 : 1) * buttonWidth;
            if (layout.Mode == TableColumnLayoutMode.Fixed)
            {
                return TableColumnLayout.Fixed(layout.Width + reserved);
            }

            var maxWidth = layout.MaxWidth > 0f ? layout.MaxWidth + reserved : layout.MaxWidth;
            return TableColumnLayout.Flexible(layout.Width + reserved, layout.MinWidth + reserved, maxWidth, layout.StretchWeight);
        }

        private static VisualElement MakeColumnHeader(CollectionColumn column)
        {
            var header = new VisualElement();
            header.AddToClassList("xeri-collection__column-header");

            var label = new Label(column.Header);
            label.AddToClassList("xeri-collection__column-header-label");
            header.Add(label);
            return header;
        }

    #endregion

    #region 셀 구성과 바인딩

        // ------------------------------------------------------------
        /// <summary>
        /// 한 열 표의 항목을 만들고 셀을 그 안에 둔다.
        /// </summary>
        // ------------------------------------------------------------
        private VisualElement MakeItem(CollectionColumn column)
        {
            var item = new VisualElement();
            item.AddToClassList("xeri-collection__item");
            item.Add(MakeCell(column));
            return item;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 셀 UI와 콜백을 한 번 만들고 재사용할 연결 상태를 함께 보관한다.
        /// </summary>
        // ------------------------------------------------------------
        private VisualElement MakeCell(CollectionColumn column)
        {
            var drawer = CollectionCellDrawers.Resolve(column);
            var cell = new VisualElement();
            cell.AddToClassList("xeri-collection__cell");
            var binding = new CellBinding
            {
                Column = column,
                Drawer = drawer,
                Row = new VisualElement(),
                Content = drawer.CreateElement(column),
                Validation = new SeverityIndicator(),
            };
            cell.userData = binding;
            binding.Row.AddToClassList("xeri-collection__row");
            cell.Add(binding.Row);

            var isFirst = column == kind.Columns[0];
            if (!isReadOnly && isFirst && kind.CanReorder)
            {
                binding.DragHandle = rowDrag.CreateReorderHandle();
                binding.Row.Add(binding.DragHandle);
            }

            // 진단 아이콘은 드래그 핸들 뒤, 값 콘텐츠 앞에 둔다.
            binding.Row.Add(binding.Validation);
            binding.Row.RegisterCallback<PointerDownEvent>
            (
                evt =>
                {
                    if (binding.SourceIndex >= 0 && (evt.button == 0 || evt.button == 1))
                    {
                        selectedColumnID = column.ID;
                        listView.SetSelection(binding.PageIndex);
                    }
                },
                TrickleDown.TrickleDown
            );
            binding.Row.Add(binding.Content);

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

            if (column == kind.Columns[kind.Columns.Count - 1])
            {
                binding.Row.Add(CreateRowActions(binding));
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

            var deleteButton = CollectionIconButton.Create(() => HandleRemoveAt(binding.PageIndex, binding.SourceIndex), "trash", "행 삭제");
            deleteButton.AddToClassList("xeri-collection__row-remove");
            deleteButton.SetEnabled(!isReadOnly && kind.Supports(CollectionEdits.Remove));
            rowActions.Add(deleteButton);
            return rowActions;
        }

        private void BindCell(VisualElement element, int pageIndex)
        {
            if (pageIndex < 0 || pageIndex >= searchPagedRows.PageItems.Count)
            {
                UnbindCell(element, pageIndex);
                return;
            }

            var binding = (CellBinding)element.userData;
            binding.PageIndex = pageIndex;
            binding.SourceIndex = searchPagedRows.PageItems[pageIndex];
            binding.Row.userData = binding.SourceIndex;
            BindContent(binding);
            if (binding.DragHandle != null)
            {
                rowDrag.UpdateReorderHandle(binding.DragHandle, pageIndex, binding.SourceIndex);
            }

            if (binding.RowEditButton != null)
            {
                var hasDetails = HasUnlistedFields(binding.SourceIndex);
                binding.RowEditButton.style.display = hasDetails ? DisplayStyle.Flex : DisplayStyle.None;
            }

            RefreshCellValidation(binding);
        }

        private static void UnbindCell(VisualElement element, int _)
        {
            var binding = (CellBinding)element.userData;
            binding.Drawer.Unbind(binding.Content);
            binding.SourceIndex = -1;
            binding.PageIndex = -1;
            binding.Row.userData = null;
            binding.Validation.SetSeverity(null);
            binding.Validation.tooltip = null;
        }

        private void BindContent(CellBinding binding)
        {
            var property = source.GetCell(binding.SourceIndex, binding.Column);
            binding.Drawer.Bind(binding.Content, new CollectionCell(this, binding.SourceIndex, binding.Column, property, isReadOnly));
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

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 표시 순서와 검색 결과를 다시 구성해 표시한다.
        /// </summary>
        // ------------------------------------------------------------
        private void RefreshRows()
        {
            UpdateRows();
            RefreshPageView();
        }

        private void UpdateRows()
        {
            ClearPendingRefresh();
            Session.Observe();
            SaveRevision();
            sourceRows.Clear();
            sourceRows.AddRange(source.GetDisplayRows());
            validation = Session.Validate();
            searchPagedRows.Refresh();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 페이지 조각을 표시하고 여전히 같은 항목인 선택을 복구한다.
        /// </summary>
        // ------------------------------------------------------------
        private void RefreshPageView()
        {
            // 접힌 표에서 컬렉션 관리 명령을 실행해도 목록 갱신이 필요하면 본문을 만든다.
            EnsureBody();
            rowDrag.CancelDrag();
            chrome.CancelPageScroll();
            restoringSelection = true;
            try
            {
                var itemCount = searchPagedRows.PageItems.Count;
                if (renderedPageItemCount != itemCount)
                {
                    listView.Rebuild();
                    renderedPageItemCount = itemCount;
                }
                else
                {
                    listView.RefreshItems();
                }

                var selected = selectedIdentity == null ? -1 : source.Find(selectedIdentity);
                var pageIndex = searchPagedRows.IndexOfPage(selected);
                listView.SetSelection(pageIndex >= 0 ? pageIndex : -1);
            }
            finally
            {
                restoringSelection = false;
            }

            RefreshStatus();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 개수·빈 상태·높이를 현재 상태에 맞게 표시한다.
        /// </summary>
        // ------------------------------------------------------------
        private void RefreshStatus()
        {
            var searching = isExpanded && !string.IsNullOrWhiteSpace(searchPagedRows.SearchText);
            var count = source.Count;
            titleLabel.text = label ?? source.DisplayName;
            countLabel.text = searching ? $"{searchPagedRows.FilteredCount}/{count}" : $"{count}";
            countLabel.tooltip = searching ? "검색 결과 / 전체 항목" : "전체 항목 수";
            EnableInClassList("xeri-collection--searching", searching);
            if (!isExpanded)
            {
                return;
            }

            emptyLabel.text = searching ? "검색 결과가 없습니다." : "항목이 없습니다.";
            emptyLabel.style.display = searchPagedRows.PageItems.Count == 0 ? DisplayStyle.Flex : DisplayStyle.None;
            listView.style.display = DisplayStyle.Flex;
            paginationBar.style.display = DisplayStyle.Flex;
            ApplyListHeight();
        }

    #endregion

    #region 높이 정책

        private static int ClampVisibleRows(int rows)
        {
            return Math.Clamp(rows, 1, CollectionTableOptions.MaxVisibleRows);
        }

        private float HeaderHeight => kind.Columns.Count > 1 ? tableHeaderHeight : 0f;

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> 행 모드는 표시 행 수만큼 높이를 고정하고 초과분만 스크롤한다.
        /// <br/> 채움 모드는 부모 높이를 따르고 들어가는 행 수로 페이지를 나눈다.
        /// </summary>
        // ----------------------------------------------------------------------
        private void ApplyListHeight()
        {
            // 본문이 아직 없으면 처음 펼칠 때 RefreshStatus가 높이를 다시 적용한다.
            if (listView == null)
            {
                return;
            }

            var scroll = listView.Q<ScrollView>();
            if (fillsParent)
            {
                listView.style.height = StyleKeyword.Null;
                if (scroll != null)
                {
                    scroll.verticalScrollerVisibility = ScrollerVisibility.Hidden;
                }

                return;
            }

            // 정확히 표시 행 수일 때 스크롤바가 생기지 않도록 여유를 포함하고 너비 변화 없이 항상 세로 스크롤바를 둔다.
            listView.style.height = HeaderHeight + visibleRows * rowHeight + viewportHeightAllowance;
            if (scroll != null)
            {
                scroll.verticalScrollerVisibility = ScrollerVisibility.AlwaysVisible;
            }
        }

        private void SetPageSize(int size)
        {
            if (!searchPagedRows.SetPageSize(size))
            {
                return;
            }

            if (TryGetSelectedSourceIndex(out var selected))
            {
                searchPagedRows.MoveToItem(selected);
            }

            if (isExpanded)
            {
                RefreshPageView();
            }
        }

        private void SetVisibleRows(int rows)
        {
            rows = ClampVisibleRows(rows);
            if (rows == visibleRows)
            {
                return;
            }

            visibleRows = rows;
            SetPageSize(Math.Max(minimumPageSize, visibleRows));
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
            if (!fill)
            {
                SetPageSize(Math.Max(minimumPageSize, visibleRows));
            }

            // 호스트가 공간을 맡긴 채움 모드에서는 접지 않고 항상 목록을 보인다.
            if (fill && !isExpanded)
            {
                SetExpanded(true);
            }

            ApplyListHeight();
        }

        private void HandleRowsGeometryChanged(GeometryChangedEvent evt)
        {
            if (!fillsParent || !isExpanded)
            {
                return;
            }

            var available = evt.newRect.height - HeaderHeight - viewportHeightAllowance;
            SetPageSize(Math.Max(1, (int)Math.Floor(available / rowHeight)));
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
            if (!isReadOnly)
            {
                rowDrag?.Bind();
            }

            CollectionSession.Committed -= HandleExternalChange;
            CollectionSession.Committed += HandleExternalChange;
            Undo.undoRedoPerformed -= HandleUndoRedo;
            Undo.undoRedoPerformed += HandleUndoRedo;

            // 다시 연결되면 현재 상태를 표시하되 새 변경으로 기록하지 않는다.
            displayedRevision = Session.Revision;
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
            rowDrag?.Unbind();
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

            displayedRevision = Session.Revision;
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
            var currentRevision = Session.Revision;
            if (currentRevision != displayedRevision)
            {
                displayedRevision = currentRevision;
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

            if (!string.IsNullOrWhiteSpace(searchPagedRows.SearchText))
            {
                if (!IsEditingCell())
                {
                    RefreshRows();
                }

                return;
            }

            ClearPendingRefresh();
            validation = Session.Validate();
            listView.Query<VisualElement>(className: "xeri-collection__cell").ForEach
            (
                element =>
                {
                    var binding = (CellBinding)element.userData;
                    if (binding.SourceIndex >= 0 && binding.SourceIndex < source.Count)
                    {
                        BindContent(binding);
                        RefreshCellValidation(binding);
                    }
                }
            );
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
            displayedRevision = Session.Revision;
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
            if (listView == null)
            {
                return false;
            }

            var focused = panel?.focusController?.focusedElement as VisualElement;
            while (focused != null && focused != listView)
            {
                if (focused.ClassListContains("unity-base-field") && listView.Contains(focused))
                {
                    return true;
                }

                focused = focused.parent;
            }

            return false;
        }

    #endregion

    #region 편집 결과 표시

        // ------------------------------------------------------------
        /// <summary>
        /// 편집 결과 행을 검색과 페이지 밖에서도 보이게 한다.
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
            listView.ClearSelection();
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
            var searchText = searchPagedRows.SearchText;
            if (!string.IsNullOrWhiteSpace(searchText) && !SearchTextUtility.Matches(GetSearchText(sourceIndex), searchText))
            {
                searchField.SetValueWithoutNotify(string.Empty);
                searchPagedRows.SetSearchText(string.Empty);
            }

            searchPagedRows.MoveToItem(sourceIndex);
            RefreshPageView();
            var pageIndex = searchPagedRows.IndexOfPage(sourceIndex);
            listView.SetSelection(pageIndex);
            listView.ScrollToItem(pageIndex);
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

    #region 검색·추가·페이지

        private void HandleSearchTextChanged(string value)
        {
            searchPagedRows.SetSearchText(value);
            RefreshPageView();
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

        // ------------------------------------------------------------
        /// <summary>
        /// 페이지 이동 결과를 선택 항목과 목록 스크롤에 반영한다.
        /// </summary>
        // ------------------------------------------------------------
        private void HandlePageNavigation(PageNavigationRequest request)
        {
            var result = navigation.Navigate(request);
            if (!result.ShouldApply)
            {
                return;
            }

            selectedIdentity = result.SelectedIndex >= 0
                ? source.GetIdentity(searchPagedRows.PageItems[result.SelectedIndex])
                : null;
            RefreshPageView();
            if (result.SelectedIndex >= 0)
            {
                chrome.ScrollToPageBoundary(result.ScrollAnchor);
            }
        }

    #endregion

    #region 선택과 키보드 입력

        private void HandleSelectionChanged(IEnumerable<object> selectedItems)
        {
            if (!restoringSelection)
            {
                selectedIdentity = TryGetSelectedSourceIndex(out var selected) ? source.GetIdentity(selected) : null;
            }

            RefreshStatus();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 유효한 드래그 시작 시 첫 열과 페이지 행 선택을 맞춘다.
        /// </summary>
        // ------------------------------------------------------------
        private void SelectDragPageRow(int pageIndex)
        {
            selectedColumnID = kind.Columns[0].ID;
            listView.SetSelection(pageIndex);
        }

        private bool TryGetSelectedSourceIndex(out int sourceIndex)
        {
            sourceIndex = -1;
            var pageIndex = listView.selectedIndex;
            if (pageIndex < 0 || pageIndex >= searchPagedRows.PageItems.Count)
            {
                return false;
            }

            sourceIndex = searchPagedRows.PageItems[pageIndex];
            return true;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 표에 포커스가 있을 때 행 명령 단축키를 처리한다.
        /// </summary>
        // ------------------------------------------------------------
        private void HandleKeyDown(KeyDownEvent evt)
        {
            if (IsEditingFieldTarget(evt.target as VisualElement))
            {
                return;
            }

            // 키보드 사용자도 Shift+F10으로 같은 행 메뉴를 열 수 있다.
            if (evt.shiftKey && evt.keyCode == KeyCode.F10)
            {
                ShowRowContextMenu(listView.worldBound.position + new Vector2(18f, 36f));
                StopShortcutEvent(evt);
                return;
            }

            if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
            {
                OpenSelectedCell();
                StopShortcutEvent(evt);
                return;
            }

            if (evt.keyCode == KeyCode.Escape)
            {
                rowDrag.CancelDrag();
                selectedIdentity = null;
                listView.ClearSelection();
                listView.Focus();
                StopShortcutEvent(evt);
                return;
            }

            var actionKey = evt.ctrlKey || evt.commandKey;
            Action command = actionKey && evt.keyCode == KeyCode.C ? HandleCopySelected
                : actionKey && evt.keyCode == KeyCode.V ? HandlePasteSelected
                : actionKey && evt.keyCode == KeyCode.D ? HandleDuplicateSelected
                : evt.keyCode == KeyCode.Delete
                    || (UnityEngine.Application.platform == RuntimePlatform.OSXEditor && evt.keyCode == KeyCode.Backspace)
                    ? HandleRemove
                    : null;
            if (command != null)
            {
                command();
                StopShortcutEvent(evt);
            }
        }

        private void OpenSelectedCell()
        {
            var column = kind.FindColumn(selectedColumnID);
            if (column != null && TryGetSelectedSourceIndex(out var selected))
            {
                OpenDetail(selected, column);
            }
        }

        private bool IsEditingFieldTarget(VisualElement element)
        {
            while (element != null && element != listView)
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

        private void HandleContextMouseUp(MouseUpEvent evt)
        {
            var contextClick = evt.button == 1
                || (UnityEngine.Application.platform == RuntimePlatform.OSXEditor && evt.button == 0 && evt.ctrlKey);
            if (!contextClick || !TryGetContextRow(evt.target as VisualElement, out var binding))
            {
                return;
            }

            selectedColumnID = binding.Column.ID;
            if (listView.selectedIndex != binding.PageIndex)
            {
                listView.SetSelection(binding.PageIndex);
            }

            ShowRowContextMenu(evt.mousePosition, binding.PageIndex, binding.SourceIndex);
            evt.StopImmediatePropagation();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 스크롤바·헤더를 제외하고 실제 클릭한 행의 연결 상태를 찾는다.
        /// </summary>
        // ------------------------------------------------------------
        private bool TryGetContextRow(VisualElement target, out CellBinding binding)
        {
            binding = null;
            for (var element = target; element != null && element != listView; element = element.parent)
            {
                if (!element.ClassListContains("xeri-collection__cell"))
                {
                    continue;
                }

                binding = element.userData as CellBinding;
                return binding != null && IsCurrentPageRow(binding.PageIndex, binding.SourceIndex);
            }

            return false;
        }

        private bool IsCurrentPageRow(int pageIndex, int sourceIndex)
        {
            return pageIndex >= 0 && pageIndex < searchPagedRows.PageItems.Count
                && sourceIndex >= 0 && sourceIndex < source.Count
                && searchPagedRows.PageItems[pageIndex] == sourceIndex;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 선택 행 메뉴를 단축키 열이 있는 팝업으로 연다.
        /// </summary>
        // ------------------------------------------------------------
        private void ShowRowContextMenu(Vector2 worldPosition, int pageIndex = -1, int sourceIndex = -1)
        {
            CloseRowContextMenu();

            // 화면에 남은 행과 현재 원본이 다르면 이전 표시에서 명령을 만들지 않는다.
            if (Session.Observe() || Session.Revision != displayedRevision || pendingRefresh)
            {
                RequestRefresh();
                return;
            }

            if (pageIndex < 0 && TryGetSelectedSourceIndex(out var selectedSource))
            {
                pageIndex = listView.selectedIndex;
                sourceIndex = selectedSource;
            }

            var hasSelection = IsCurrentPageRow(pageIndex, sourceIndex);
            var rowLocator = hasSelection ? new SerializedPropertyLocator(source.GetRow(sourceIndex)) : null;
            var canEdit = hasSelection && !isReadOnly;
            var canPaste = canEdit && kind.Supports(CollectionEdits.Paste)
                && SerializedPropertyClipboard.CanPasteValue(source.GetRow(sourceIndex), out _);
            var originalRevision = Session.Revision;
            var isMac = UnityEngine.Application.platform == RuntimePlatform.OSXEditor;
            var modifier = isMac ? "Cmd+" : "Ctrl+";
            var deleteShortcut = isMac ? "Backspace" : "Delete";

            void Run(Action execute) => ExecuteContextRowAction(pageIndex, sourceIndex, originalRevision, rowLocator, execute);

            var items = new List<XeriContextMenuItem>
            {
                XeriContextMenuItem.Command
                (
                    "행 복제", () => Run(HandleDuplicateSelected),
                    enabled: canEdit && kind.Supports(CollectionEdits.Duplicate), shortcut: modifier + "D"
                ),
                XeriContextMenuItem.Separator(),
                XeriContextMenuItem.Command("행 전체 복사", () => Run(HandleCopySelected), enabled: hasSelection, shortcut: modifier + "C"),
                XeriContextMenuItem.Command("행 전체 붙여넣기", () => Run(HandlePasteSelected), enabled: canPaste, shortcut: modifier + "V"),
                XeriContextMenuItem.Separator(),
                XeriContextMenuItem.Command
                (
                    "인덱스로 이동...", () => Run(OpenMoveIndex),
                    enabled: canEdit && kind.CanReorder && string.IsNullOrWhiteSpace(searchPagedRows.SearchText)
                ),
                XeriContextMenuItem.Separator(),
                XeriContextMenuItem.Command("위로 이동", () => Run(() => HandleMoveSelected(-1)), enabled: hasSelection && CanMoveSelected(-1, out _)),
                XeriContextMenuItem.Command("아래로 이동", () => Run(() => HandleMoveSelected(1)), enabled: hasSelection && CanMoveSelected(1, out _)),
                XeriContextMenuItem.Separator(),
                XeriContextMenuItem.Command
                (
                    "행 삭제", () => Run(HandleRemove),
                    enabled: canEdit && kind.Supports(CollectionEdits.Remove), shortcut: deleteShortcut
                ),
            };
            rowContextMenu.Open(worldPosition, items);
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// 메뉴를 연 뒤 셀 재사용이나 페이지 변경이 생겨도 다른 행을 고치지 않는다.
        /// </summary>
        // ----------------------------------------------------------------------
        private void ExecuteContextRowAction
        (
            int pageIndex, int sourceIndex, ulong originalRevision,
            SerializedPropertyLocator rowLocator, Action execute
        )
        {
            // UI 관찰 콜백보다 먼저 명령이 실행돼도 원본 변경을 검사한다.
            Session.Observe();
            if (Session.Revision != originalRevision
                || !IsCurrentPageRow(pageIndex, sourceIndex)
                || rowLocator == null
                || !rowLocator.TryResolve(Session.SerializedObject, out _, out _))
            {
                LogCommandFailure("행이 변경되어 명령을 실행하지 않았습니다.");
                RefreshRows();
                return;
            }

            if (listView.selectedIndex != pageIndex)
            {
                listView.SetSelection(pageIndex);
            }

            execute();
        }

        // 메뉴 공용 키 처리를 제외한 컬렉션 명령 단축키만 표로 넘긴다.
        private void HandleRowContextMenuKeyDown(KeyDownEvent evt)
        {
            if (evt.ctrlKey || evt.commandKey || evt.keyCode == KeyCode.Delete
                || (UnityEngine.Application.platform == RuntimePlatform.OSXEditor && evt.keyCode == KeyCode.Backspace))
            {
                CloseRowContextMenu();
                listView.Focus();
                HandleKeyDown(evt);
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
                RemoveRowAndSelectNext(listView.selectedIndex, sourceIndex);
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 재사용된 셀의 오래된 삭제 버튼이 다른 행을 지우지 않게 한다.
        /// </summary>
        // ------------------------------------------------------------
        private void HandleRemoveAt(int pageIndex, int sourceIndex)
        {
            if (isReadOnly)
            {
                return;
            }

            if (!IsCurrentPageRow(pageIndex, sourceIndex))
            {
                RefreshRows();
                return;
            }

            RemoveRowAndSelectNext(pageIndex, sourceIndex);
        }

        private void RemoveRowAndSelectNext(int pageIndex, int sourceIndex)
        {
            var removedIndices = new[]
            {
                sourceIndex
            };
            ApplyRowsEdit(CollectionEdit.Remove(removedIndices));
            selectedIdentity = null;
            var nextPageIndex = Math.Min(pageIndex, searchPagedRows.PageItems.Count - 1);
            if (nextPageIndex >= 0)
            {
                listView.SetSelection(nextPageIndex);
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
            if (isReadOnly || !kind.CanReorder || !string.IsNullOrWhiteSpace(searchPagedRows.SearchText)
                || !TryGetSelectedSourceIndex(out sourceIndex))
            {
                return false;
            }

            var destinationIndex = sourceIndex + offset;
            return destinationIndex >= 0 && destinationIndex < source.Count;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 선택 행의 대상 인덱스를 입력받고 바뀌지 않은 원본에만 적용한다.
        /// </summary>
        // ------------------------------------------------------------
        private void OpenMoveIndex()
        {
            if (isReadOnly || !kind.CanReorder || !string.IsNullOrWhiteSpace(searchPagedRows.SearchText)
                || !TryGetSelectedSourceIndex(out var sourceIndex))
            {
                return;
            }

            if (Session.Observe())
            {
                RefreshRows();
                return;
            }

            var originalCount = source.Count;
            var originalRevision = Session.Revision;
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
                    var unchanged = source.Count == originalCount && Session.Revision == originalRevision
                        && rowLocator.TryResolve(Session.SerializedObject, out _, out _)
                        && TryGetSelectedSourceIndex(out var selected) && selected == sourceIndex
                        && string.IsNullOrWhiteSpace(searchPagedRows.SearchText);
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
        /// 메뉴·드래그·인덱스 입력에 같은 삽입형 이동 편집을 적용한다.
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
        /// 초안 열은 초안 창으로, 그 밖에는 즉시 셀 값을 바꾼다.
        /// </summary>
        // ------------------------------------------------------------
        public void Replace(int sourceIndex, CollectionColumn column, object value)
        {
            if (isReadOnly)
            {
                return;
            }

            if (column.Edit == CollectionCellEdit.Draft)
            {
                CollectionDraftWindow.OpenForRow(Session, sourceIndex, column, value, RevealRow);
                return;
            }

            CloseDetailEditors();
            RevealRow(Session.Apply(CollectionEdit.SetCell(sourceIndex, column.ID, value)));
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

    #endregion

    }
}
