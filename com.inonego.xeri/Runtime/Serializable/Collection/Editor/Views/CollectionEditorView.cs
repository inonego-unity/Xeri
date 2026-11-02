/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : CollectionEditorView.cs
수정일 : 2026-11-03

# 설명
Collection Model의 표시 순서와 변경 명령을 Inspector의 검색·선택·페이지에 연결한다.
행 검증 아이콘은 드래그 핸들 뒤에 배치하고, ReadOnly에서도 탐색·복사·상세보기를 허용한다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

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
    /// Collection authoring의 표시·선택·검색과 명령 피드백을 제공한다.
    /// </summary>
    // ============================================================
    internal sealed class CollectionEditorView : VisualElement
    {

    #region 필드

        // 실제 테이블 크기는 CollectionEditorViewStyle.uss의 28px 행·헤더와 일치해야 한다.
        private const float rowHeight = 28f;
        private const float tableHeaderHeight = 28f;
        private const float viewportHeightAllowance = 6f;
        private const int visibleRowCount = 5;
        private const int pageSize = 10;
        private readonly ICollectionEditorModel model;
        private readonly bool isReadOnly;
        private readonly List<int> sourceRows = new();
        private readonly SearchPagedList<int> searchPagedRows;
        private readonly PageNavigationController<int> navigation;
        private readonly BaseListView listView;
        private readonly CollectionRowDragController rowDrag;
        private readonly Label titleLabel;
        private readonly Button foldoutButton;
        private readonly string foldoutStateKey;
        private bool isExpanded;
        private readonly Label emptyLabel;
        private readonly Label countLabel;
        private readonly ToolbarSearchField searchField;
        private readonly PaginationBar paginationBar;
        private readonly PagedListChrome chrome;
        private readonly XeriContextMenu rowContextMenu;
        private readonly XeriContextMenu collectionActionsMenu;
        private object selectedIdentity;
        private string selectedColumnID;
        private bool restoringSelection;
        private int renderedPageItemCount;
        private bool pendingRefresh;
        private ulong displayedRevision;
        private bool pendingStructureRefresh;
        private readonly IVisualElementScheduledItem refreshTask;

        private sealed class CellBinding
        {
            public int SourceIndex = -1;
            public int PageIndex = -1;

            // ------------------------------------------------------------
            /// <summary>
            /// 이 셀에 대응하는 열 설정을 보관한다.
            /// </summary>
            // ------------------------------------------------------------
            public CollectionColumnSpec Column;

            // ------------------------------------------------------------
            /// <summary>
            /// 현재 행의 UI 요소를 보관한다.
            /// </summary>
            // ------------------------------------------------------------
            public VisualElement Row;

            // ------------------------------------------------------------
            /// <summary>
            /// 현재 행의 셀 렌더러를 보관한다.
            /// </summary>
            // ------------------------------------------------------------
            public CollectionCellRenderer Content;

            // ------------------------------------------------------------
            /// <summary>
            /// 현재 행의 드래그 핸들을 보관한다.
            /// </summary>
            // ------------------------------------------------------------
            public VisualElement DragHandle;

            // ------------------------------------------------------------
            /// <summary>
            /// 현재 행의 검증 메시지 요소를 보관한다.
            /// </summary>
            // ------------------------------------------------------------
            public SeverityIndicator Validation;
        }

    #endregion

    #region 생성과 셀 구성

        // ------------------------------------------------------------
        /// <summary>
        /// collection 의미 Model에서 독립된 authoring View를 구성한다.
        /// </summary>
        // ------------------------------------------------------------
        public CollectionEditorView(ICollectionEditorModel model, bool isReadOnly = false)
        {
            this.model = model ?? throw new ArgumentNullException(nameof(model));
            this.isReadOnly = isReadOnly;
            selectedColumnID = model.Columns[0].ID;
            refreshTask = schedule.Execute(RefreshWhenIdle);
            refreshTask.Pause();
            foldoutStateKey = $"UniXeri.CollectionEditor.Foldout.{model.SerializedObject.targetObject.GetEntityId()}.{model.CollectionPropertyPath}";
            isExpanded = SessionState.GetBool(foldoutStateKey, false);
            searchPagedRows = new SearchPagedList<int>
            (
                sourceRows,
                index => CollectionCellSummary.GetSearchText(model, index),
                pageSize
            );
            navigation = new PageNavigationController<int>(searchPagedRows);
            AddToClassList("xeri-collection");
            EnableInClassList("xeri-collection--multi-column", model.Columns.Count > 1);
            styleSheets.Add(CollectionEditorResources.LoadViewStyle());

            // 구조는 UXML, 스타일은 USS, 데이터 연결과 명령은 View가 담당한다.
            var content = new VisualElement();
            CollectionEditorResources.LoadViewLayout().CloneTree(content);
            chrome = new PagedListChrome();
            Add(chrome);
            paginationBar = new PaginationBar
            (
                searchPagedRows.Paginator,
                HandlePageNavigation
            );
            chrome.PaginationHost.Add(paginationBar);

            var header = content.Q<VisualElement>("collection-header");
            var search = content.Q<VisualElement>("collection-search");
            var add = content.Q<VisualElement>("collection-add");
            var rows = content.Q<VisualElement>("collection-rows-host");
            if (header == null || search == null || add == null || rows == null)
            {
                throw new InvalidOperationException("CollectionEditorView의 UXML 구성이 잘못되었습니다.");
            }

            chrome.HeaderHost.Add(header);
            chrome.SearchHost.Add(search);
            chrome.ToolbarActionHost.Add(add);
            chrome.ContentHost.Add(rows);

            titleLabel = this.Q<Label>("collection-title");
            titleLabel.tooltip = model.Description;
            foldoutButton = this.Q<Button>("collection-foldout")
                ?? throw new InvalidOperationException("CollectionEditorView 접기 버튼이 없습니다.");
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
            // 제목/빈 헤더를 눌러도 접되, ⋮ 메뉴와 접기 버튼 클릭은 중복 처리하지 않는다.
            header.RegisterCallback<ClickEvent>
            (
                evt =>
                {
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
            addButton.SetEnabled(!isReadOnly);
            addButton.clicked += () => HandleAdd(addButton);

            // 단일 값은 table header가 필요 없는 ListView를 사용한다.
            if (model.Columns.Count == 1)
            {
                listView = new ListView
                {
                    makeItem = () => MakeCell(model.Columns[0]),
                    bindItem = BindCell,
                    unbindItem = UnbindCell,
                };
            }
            else
            {
                var table = new MultiColumnListView();
                table.sortingMode = ColumnSortingMode.None;
                // 고정 열은 유지하고 가변 열은 공통 정책에 따라 남는 폭을 자동으로 채운다.
                // 사용자의 수동 열 조절은 UI Toolkit에 맡긴다.
                var target = model.SerializedObject.targetObject;
                var globalID = GlobalObjectId.GetGlobalObjectIdSlow(target);
                var ownerID = globalID.identifierType != 0
                    ? globalID.ToString()
                    : target.GetEntityId().ToString();
                table.viewDataKey = $"UniXeri.CollectionEditor.Columns.{ownerID}.{model.CollectionPropertyPath}";
                table.columns.resizable = true;
                table.columns.resizePreview = false;
                var stretchWeights = new List<float>(model.Columns.Count);
                foreach (var column in model.Columns)
                {
                    var spec = column;
                    table.columns.Add
                    (
                        new Column
                        {
                            name = spec.ID,
                            width = spec.Width,
                            minWidth = spec.MinWidth,
                            stretchable = spec.Stretchable,
                            resizable = spec.Stretchable,
                            sortable = false,
                            makeHeader = () => MakeColumnHeader(spec),
                            makeCell = () => MakeCell(spec),
                            bindCell = BindCell,
                            unbindCell = UnbindCell,
                        }
                    );
                    stretchWeights.Add(spec.StretchWeight);
                }

                table.ApplyColumnSizing(stretchWeights);
                listView = table;
            }

            listView.name = "collection-rows";
            rowContextMenu = new XeriContextMenu(listView, HandleRowContextMenuKeyDown);
            listView.selectionType = SelectionType.Single;
            listView.reorderable = false;
            listView.reorderMode = ListViewReorderMode.Simple;
            listView.fixedItemHeight = rowHeight;
            listView.showAlternatingRowBackgrounds = AlternatingRowBackground.ContentOnly;
            listView.focusable = true;
            rowDrag = new CollectionRowDragController
            (
                model,
                sourceRows,
                searchPagedRows,
                listView,
                SelectDragPageRow,
                MoveDraggedRow
            );
            listView.itemsSource = searchPagedRows.PageItemsSource;
            listView.selectionChanged += HandleSelectionChanged;
            // 기본 DropdownMenu는 "/"를 계층 구분자로 사용하고 단축키 오른쪽 열을 제공하지 않는다.
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
            // 빈 상태 안내는 목록의 위에 오버레이하여 높이를 변화시키지 않는다.
            var rowsHost = this.Q<VisualElement>("collection-rows-host");
            rowsHost.Insert(0, listView);
            chrome.BindPageListView(listView);
            renderedPageItemCount = searchPagedRows.PageItems.Count;
            emptyLabel.pickingMode = PickingMode.Ignore;
            // 실제 field 편집만 commit한다. 외부 변경 관찰은 표시 갱신만 담당한다.
            RegisterCallback<AttachToPanelEvent>(HandleAttach);
            RegisterCallback<DetachFromPanelEvent>(HandleDetach);
            RegisterCallback<SerializedPropertyChangeEvent>(HandlePropertyEdited);
            // 중첩 필드를 포함한 외부 직렬화 변경을 이벤트로 관찰한다.
            this.TrackSerializedObjectValue
            (
                model.SerializedObject, _ => HandleCollectionChanged()
            );
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

        // ------------------------------------------------------------
        /// <summary>
        /// 컬렉션 헤더만 남기고 목록과 도구 모음·페이지 영역을 접는다.
        /// </summary>
        // ------------------------------------------------------------
        private void SetExpanded(bool expanded)
        {
            var wasExpanded = isExpanded;
            isExpanded = expanded;
            SessionState.SetBool(foldoutStateKey, expanded);
            EnableInClassList("xeri-collection--collapsed", !expanded);
            foldoutButton.text = string.Empty;
            foldoutButton.iconImage = XeriIcon.LoadBackground
            (
                expanded ? "chevron-down" : "chevron-right"
            );
            foldoutButton.tooltip = expanded ? "목록 접기" : "목록 펼치기";
            if (!expanded)
            {
                ClearPendingRefresh();
                CloseRowContextMenu();
                RefreshStatus();
            }
            else if (!wasExpanded)
            {
                RefreshRows();
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 컬럼 헤더의 표시 텍스트를 왼쪽 정렬로 직접 구성한다.
        /// </summary>
        // ------------------------------------------------------------
        private static VisualElement MakeColumnHeader(CollectionColumnSpec column)
        {
            var header = new VisualElement();
            header.AddToClassList("xeri-collection__column-header");

            var label = new Label(column.Header);
            label.AddToClassList("xeri-collection__column-header-label");
            header.Add(label);
            return header;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 행의 UI와 콜백을 한 번 만들고 재사용할 연결 상태를 함께 보관한다.
        /// </summary>
        // ------------------------------------------------------------
        private VisualElement MakeCell(CollectionColumnSpec column)
        {
            var cell = new VisualElement();
            cell.AddToClassList("xeri-collection__cell");
            var binding = new CellBinding
            {
                Column = column,
                Row = new VisualElement(),
                Content = new CollectionCellRenderer(model, column.ID, RevealRow, isReadOnly),
                Validation = new SeverityIndicator(),
            };
            cell.userData = binding;
            binding.Row.AddToClassList("xeri-collection__row");
            cell.Add(binding.Row);

            if (!isReadOnly && column.ID == model.Columns[0].ID && model.CanReorder)
            {
                binding.DragHandle = rowDrag.CreateReorderHandle();
                binding.Row.Add(binding.DragHandle);
            }

            // 진단 아이콘은 드래그 핸들 뒤, 값 콘텐츠 앞에 고정한다.
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
            if (column.ID == model.Columns[model.Columns.Count - 1].ID)
            {
                var rowActions = new VisualElement();
                rowActions.AddToClassList("xeri-collection__row-actions");
                rowActions.Add(binding.Content.ActionToolbar);

                var deleteButton = new ToolbarButton
                (
                    () =>
                    {
                        if (binding.Content.IsCurrent)
                        {
                            HandleRemoveAt(binding.PageIndex, binding.SourceIndex);
                        }
                        else
                        {
                            RefreshRows();
                        }
                    }
                )
                {
                    iconImage = XeriIcon.LoadBackground("trash"),
                    tooltip = "행 삭제",
                };
                deleteButton.AddToClassList("xeri-collection__icon-button");
                deleteButton.AddToClassList("xeri-collection__row-remove");
                deleteButton.SetEnabled(!isReadOnly);
                rowActions.Add(deleteButton);
                binding.Row.Add(rowActions);
            }

            return cell;
        }

        private static void UnbindCell(VisualElement element, int _)
        {
            var binding = (CellBinding)element.userData;
            binding.Content.ReleaseBinding();
            binding.SourceIndex = -1;
            binding.PageIndex = -1;
            binding.Row.userData = null;
            binding.Validation.SetSeverity(null);
            binding.Validation.tooltip = null;
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
            binding.Content.Bind(binding.SourceIndex);
            if (binding.DragHandle != null)
            {
                rowDrag.UpdateReorderHandle(binding.DragHandle, pageIndex, binding.SourceIndex);
            }

            RefreshCellValidation(binding);
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// 셀에 속한 진단 중 최고 심각도 하나를 표시하고 전체 내용을 툴팁으로 제공한다.
        /// </summary>
        // ----------------------------------------------------------------------
        private void RefreshCellValidation(CellBinding binding)
        {
            var result = model.Validate
            (
                CollectionValidationContext.ForRow(binding.SourceIndex)
            );
            CollectionCellDiagnostic? primary = null;
            var messages = new List<string>();
            foreach (var diagnostic in result.Diagnostics)
            {
                // 행 전체 메시지는 첫 번째 열에서만 표시한다.
                var columnID = diagnostic.ColumnID ?? model.Columns[0].ID;
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

            // 셀별 가장 높은 심각도 하나만 표시하고 모든 설명은 툴팁에 보관한다.
            binding.Validation.SetSeverity(primary?.Severity);
            binding.Validation.tooltip = primary.HasValue ? string.Join("\n", messages) : null;
        }

    #endregion

    #region 행 표시와 선택 복원

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 source 표시 순서와 검색 결과를 다시 구성한다.
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
            model.Synchronize();
            SaveRevision();
            sourceRows.Clear();
            sourceRows.AddRange(model.GetSourceRows());
            searchPagedRows.Refresh();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// page slice를 표시하고 여전히 같은 항목인 선택을 복구한다.
        /// </summary>
        // ------------------------------------------------------------
        private void RefreshPageView()
        {
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

                var source = selectedIdentity == null ? -1 : model.FindRow(selectedIdentity);
                var pageIndex = searchPagedRows.IndexOfPage(source);
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
        /// 개수·빈 상태·현재 선택의 validation을 일관되게 표시한다.
        /// </summary>
        // ------------------------------------------------------------
        private void RefreshStatus()
        {
            var searching = isExpanded && !string.IsNullOrWhiteSpace(searchPagedRows.SearchText);
            titleLabel.text = model.Label;
            countLabel.text = searching
                ? $"{searchPagedRows.FilteredCount}/{model.Count}"
                : $"{model.Count}";
            countLabel.tooltip = searching ? "검색 결과 / 전체 항목" : "전체 항목 수";
            EnableInClassList("xeri-collection--searching", searching);
            if (!isExpanded)
            {
                return;
            }

            var count = searchPagedRows.PageItems.Count;
            emptyLabel.text = searching
                ? "검색 결과가 없습니다."
                : "항목이 없습니다.";
            emptyLabel.style.display = count == 0 ? DisplayStyle.Flex : DisplayStyle.None;
            listView.style.display = DisplayStyle.Flex;
            paginationBar.style.display = DisplayStyle.Flex;
            // 페이지당 최대 10개지만 표시 영역은 5행으로 고정하고 초과분만 스크롤한다.
            // 정확히 5행일 때 스크롤바가 생기지 않도록 Viewport 여유를 포함한다.
            var headerHeight = model.Columns.Count > 1 ? tableHeaderHeight : 0f;
            listView.style.height = headerHeight + visibleRowCount * rowHeight
                + viewportHeightAllowance;

            var scroll = listView.Q<ScrollView>();
            if (scroll != null)
            {
                // 행 개수에 따른 너비 변화 없이 세로 스크롤바를 항상 표시한다.
                scroll.verticalScrollerVisibility = ScrollerVisibility.AlwaysVisible;
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 명시적인 command 결과 항목을 검색과 page 밖에서도 보여준다.
        /// </summary>
        // ------------------------------------------------------------
        private void RevealRow(CollectionRowCommandResult result)
        {
            if (!result.Succeeded)
            {
                LogCommandFailure("컬렉션 행 변경에 실패했습니다.");
                RefreshRows();
                return;
            }

            if (result.RowIndex >= 0)
            {
                RevealRow(result.RowIndex);
                return;
            }

            // 행을 변경했어도 값이나 참조가 중복되면 선택할 대상을 유일하게 알 수 없다.
            selectedIdentity = null;
            RefreshRows();
            listView.ClearSelection();
        }

        private void RevealRow(int sourceIndex)
        {
            if (sourceIndex < 0 || sourceIndex >= model.Count)
            {
                LogCommandFailure("항목을 만들지 못했습니다. 콘솔에서 오류를 확인하세요.");
                return;
            }

            selectedIdentity = model.GetRowIdentity(sourceIndex);
            UpdateRows();
            // 생성 결과가 필터 밖이면 검색을 해제해 성공 결과를 숨기지 않는다.
            var searchText = searchPagedRows.SearchText;
            if (!string.IsNullOrWhiteSpace(searchText)
                && !SearchTextUtility.Matches(CollectionCellSummary.GetSearchText(model, sourceIndex), searchText))
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

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 입력 control이 collection 값 편집을 소유하는지 확인한다.
        /// </summary>
        // ------------------------------------------------------------
        private bool IsEditingCell()
        {
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

    #region 상태 동기화와 이벤트

        // ------------------------------------------------------------
        /// <summary>
        /// 변경 관찰은 Model의 확정 기준을 바꾸지 않고 표시 갱신만 예약한다.
        /// </summary>
        // ------------------------------------------------------------
        private void HandleCollectionChanged()
        {
            if (panel == null)
            {
                return;
            }

            // 다른 Inspector나 외부 직렬화 변경도 같은 컬렉션의 세대에 반영한다.
            model.ObserveExternalChange();
            displayedRevision = model.Revision;
            RequestRefresh(model.CollectionPropertyPath);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 구조 변경은 우선하고, 알려진 값 변경들은 하나의 표시 갱신으로 합친다.
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

        // ------------------------------------------------------------
        /// <summary>
        /// Undo/Redo는 사용자 편집 통지가 아닌 확정된 외부 상태로 받아들인다.
        /// </summary>
        // ------------------------------------------------------------
        private void HandleUndoRedo()
        {
            if (panel == null)
            {
                return;
            }

            model.RestoreSerializedState();
            var currentRevision = model.Revision;
            if (currentRevision != displayedRevision)
            {
                displayedRevision = currentRevision;
                RequestRefresh();
            }
        }

        private void HandleAttach(AttachToPanelEvent _)
        {
            if (!isReadOnly)
            {
                rowDrag.Bind();
            }

            CollectionEditorModelBase.Committed -= HandleExternalChange;
            CollectionEditorModelBase.Committed += HandleExternalChange;
            Undo.undoRedoPerformed -= HandleUndoRedo;
            Undo.undoRedoPerformed += HandleUndoRedo;
            // 재연결 시 현재 상태를 다시 표시하되 새 변경으로 기록하지 않는다.
            displayedRevision = model.Revision;
            RequestRefresh();
            RefreshStatus();
            if (pendingRefresh)
            {
                refreshTask.ExecuteLater(0);
            }
        }

        private void HandleDetach(DetachFromPanelEvent _)
        {
            CollectionEditorModelBase.Committed -= HandleExternalChange;
            Undo.undoRedoPerformed -= HandleUndoRedo;
            refreshTask.Pause();
            rowDrag.Unbind();
            CloseRowContextMenu();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 컬렉션의 상위·하위 경로에서 발생한 변경인지 확인한다.
        /// </summary>
        // ------------------------------------------------------------
        private static bool IsRelatedCollectionPath(string watchedPath, string changedPath)
        {
            if (string.IsNullOrEmpty(watchedPath) || string.IsNullOrEmpty(changedPath))
            {
                return false;
            }

            return watchedPath == changedPath
                || watchedPath.StartsWith(changedPath + ".", StringComparison.Ordinal)
                || changedPath.StartsWith(watchedPath + ".", StringComparison.Ordinal);
        }

        private void HandleExternalChange
        (
            UnityEngine.Object target, string path, string propertyPath
        )
        {
            if (target != model.SerializedObject.targetObject
                || !IsRelatedCollectionPath(model.CollectionPropertyPath, path))
            {
                return;
            }

            // 상세 편집창은 별도의 SerializedObject를 사용한다.
            // 외부 Commit 뒤 원본 Inspector의 스냅샷을 강제로 갱신해야
            // CollectionCellSummary가 최신 SerializedProperty를 읽는다.
            model.SerializedObject.Update();
            var hasSelectedSource = TryGetSelectedSourceIndex(out var selectedSource);
            model.Synchronize();
            SaveRevision();
            if (propertyPath != null && hasSelectedSource && selectedSource < model.Count)
            {
                selectedIdentity = model.GetRowIdentity(selectedSource);
            }

            // 접힌 목록은 다시 펼칠 때 동기화하고 항목 수만 갱신한다.
            // 펼친 목록의 입력 포커스는 기존 갱신 예약 정책을 유지한다.
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
            if (!isReadOnly)
            {
                model.CommitPropertyChange(evt.changedProperty);
            }
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// 값 편집은 UI를 유지하고, 검색 결과나 행 구조가 바뀔 때만 재바인딩한다.
        /// </summary>
        // ----------------------------------------------------------------------
        private void RefreshWhenIdle()
        {
            if (!pendingRefresh)
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
            listView.Query<VisualElement>(className: "xeri-collection__cell").ForEach
            (
                element =>
                {
                    var binding = (CellBinding)element.userData;
                    if (binding.SourceIndex >= 0 && binding.SourceIndex < model.Count)
                    {
                        binding.Content.RefreshDisplay();
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
            displayedRevision = model.Revision;
        }

        private bool HasSameSourceRows()
        {
            if (model.Count != sourceRows.Count)
            {
                return false;
            }

            var index = 0;
            foreach (var row in model.GetSourceRows())
            {
                if (index >= sourceRows.Count || sourceRows[index] != row)
                {
                    return false;
                }

                index++;
            }

            return index == sourceRows.Count;
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// command 실패나 제한 사유를 Inspector 하단 UI 대신 Console에 기록한다.
        /// </summary>
        // ----------------------------------------------------------------------
        private void LogCommandFailure(string message)
        {
            Debug.LogWarning(message);
        }

    #endregion

    #region 검색과 추가·삭제

        // ------------------------------------------------------------
        /// <summary>
        /// 검색어를 적용하되 source row 순서를 변경하지 않는다.
        /// </summary>
        // ------------------------------------------------------------
        private void HandleSearchTextChanged(string value)
        {
            searchPagedRows.SetSearchText(value);
            RefreshPageView();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// type-aware Add의 생성 후보와 취소를 처리한다.
        /// </summary>
        // ------------------------------------------------------------
        private void HandleAdd(VisualElement anchor)
        {
            if (isReadOnly)
            {
                return;
            }

            if (!model.TryGetManagedReferenceAddTarget(out var columnID, out var declaredType))
            {
                CloseDetailEditors();
                RevealRow(model.Add());
                return;
            }

            void AddType(Type type)
            {
                if (panel == null)
                {
                    return;
                }

                if (model is XDictionaryEditorModel dictionary && columnID == dictionary.ValidationColumnID)
                {
                    CollectionStructuralEditorWindow.Open(dictionary, null, type, RevealRow);
                    return;
                }

                AddManagedReference(columnID, type);
            }

            if (ManagedReferenceTypeCatalog.CanCreate(declaredType))
            {
                AddType(declaredType);
                return;
            }

            if (ManagedReferenceTypeCatalog.GetCreationCandidates(declaredType).Count == 0)
            {
                LogCommandFailure("생성 가능한 타입이 없습니다. 행을 추가하지 않았습니다.");
                return;
            }

            // Picker를 연 뒤 상위 배열 요소가 바뀌면 다른 collection에 추가하지 않는다.
            var locator = new SerializedPropertyLocator(model.GetCollectionProperty());
            void ChooseType(Type type)
            {
                if (locator.TryResolve(model.SerializedObject, out _, out var reason))
                {
                    AddType(type);
                }
                else
                {
                    LogCommandFailure(reason);
                }
            }

            ManagedReferenceEditorAction.ShowCreationTypePicker(declaredType, anchor, ChooseType);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 새 managed reference를 생성하고 추가된 row를 보여준다.
        /// </summary>
        // ------------------------------------------------------------
        private void AddManagedReference(string columnID, Type type)
        {
            if (!ManagedReferenceEditorAction.TryCreateReference(type, out var reference, out var reason))
            {
                LogCommandFailure(reason);
                return;
            }

            CloseDetailEditors();
            RevealRow(model.Add(index => model.GetCellProperty(index, columnID).managedReferenceValue = reference));
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 선택 행을 삭제하고 같은 page의 인접 항목을 선택한다.
        /// </summary>
        // ------------------------------------------------------------
        private void HandleRemove()
        {
            if (isReadOnly)
            {
                return;
            }

            if (!TryGetSelectedSourceIndex(out var sourceIndex))
            {
                return;
            }

            RemoveRowAndSelectNext(listView.selectedIndex, sourceIndex);
        }

    #endregion

    #region 페이지 이동

        // ------------------------------------------------------------
        /// <summary>
        /// 공통 페이지 이동 결과를 선택 항목과 목록 스크롤에 반영한다.
        /// </summary>
        // ------------------------------------------------------------
        private void HandlePageNavigation(PageNavigationRequest request)
        {
            var result = navigation.Navigate(request);
            if (!result.ShouldApply) return;

            selectedIdentity = result.SelectedIndex >= 0
                ? model.GetRowIdentity(searchPagedRows.PageItems[result.SelectedIndex])
                : null;

            RefreshPageView();

            if (result.SelectedIndex >= 0)
            {
                chrome.ScrollToPageBoundary(result.ScrollAnchor);
            }
        }

    #endregion

    #region 컨텍스트 메뉴

        // ------------------------------------------------------------
        /// <summary>
        /// 선택 row용 Copy/Paste/Duplicate/Delete 메뉴를 구성한다.
        /// </summary>
        // ------------------------------------------------------------
        private void HandleContextMouseUp(MouseUpEvent evt)
        {
            var contextClick = evt.button == 1
                || (UnityEngine.Application.platform == RuntimePlatform.OSXEditor
                    && evt.button == 0 && evt.ctrlKey);
            if (!contextClick || !TryGetContextRow(evt.target as VisualElement, out var binding))
            {
                return;
            }

            var pageIndex = binding.PageIndex;
            var sourceIndex = binding.SourceIndex;
            selectedColumnID = binding.Column.ID;
            if (listView.selectedIndex != pageIndex)
            {
                listView.SetSelection(pageIndex);
            }

            ShowRowContextMenu(evt.mousePosition, pageIndex, sourceIndex);
            evt.StopImmediatePropagation();
            evt.PreventDefault();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 스크롤바·헤더를 제외하고 실제 클릭한 행의 바인딩을 찾는다.
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
                && sourceIndex >= 0 && sourceIndex < model.Count
                && searchPagedRows.PageItems[pageIndex] == sourceIndex;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 선택 행 메뉴를 단축키 열을 가진 UI Toolkit 팝업으로 표시한다.
        /// </summary>
        // ------------------------------------------------------------
        private void ShowRowContextMenu
        (
            Vector2 worldPosition, int pageIndex = -1, int sourceIndex = -1
        )
        {
            CloseRowContextMenu();

            if (pageIndex < 0 && TryGetSelectedSourceIndex(out var selectedSource))
            {
                pageIndex = listView.selectedIndex;
                sourceIndex = selectedSource;
            }

            var hasSelection = IsCurrentPageRow(pageIndex, sourceIndex);
            var canPaste = false;
            if (hasSelection && !isReadOnly)
            {
                model.SerializedObject.UpdateIfRequiredOrScript();
                canPaste = SerializedPropertyClipboard.CanPasteValue
                (
                    model.GetRowProperty(sourceIndex),
                    out _
                );
            }

            var originalRevision = model.Revision;
            var modifier = UnityEngine.Application.platform == RuntimePlatform.OSXEditor ? "Cmd+" : "Ctrl+";
            rowContextMenu.Clear();
            rowContextMenu.AddAction
            (
                "행 복제",
                () => ExecuteContextRowAction(pageIndex, sourceIndex, originalRevision, HandleDuplicateSelected),
                enabled: hasSelection && !isReadOnly, shortcut: modifier + "D"
            );
            rowContextMenu.AddSeparator();
            rowContextMenu.AddAction
            (
                "행 전체 복사",
                () => ExecuteContextRowAction(pageIndex, sourceIndex, originalRevision, HandleCopySelected),
                enabled: hasSelection, shortcut: modifier + "C"
            );
            rowContextMenu.AddAction
            (
                "행 전체 붙여넣기",
                () => ExecuteContextRowAction(pageIndex, sourceIndex, originalRevision, HandlePasteSelected),
                enabled: canPaste, shortcut: modifier + "V"
            );
            rowContextMenu.AddSeparator();
            rowContextMenu.AddAction
            (
                "인덱스로 이동...",
                () => ExecuteContextRowAction(pageIndex, sourceIndex, originalRevision, OpenMoveIndex),
                enabled: hasSelection && !isReadOnly && model.CanReorder
                    && string.IsNullOrWhiteSpace(searchPagedRows.SearchText)
            );
            rowContextMenu.AddSeparator();
            rowContextMenu.AddAction
            (
                "위로 이동",
                () => ExecuteContextRowAction(pageIndex, sourceIndex, originalRevision, () => HandleMoveSelected(-1)),
                enabled: hasSelection && CanMoveSelected(-1, out _)
            );
            rowContextMenu.AddAction
            (
                "아래로 이동",
                () => ExecuteContextRowAction(pageIndex, sourceIndex, originalRevision, () => HandleMoveSelected(1)),
                enabled: hasSelection && CanMoveSelected(1, out _)
            );
            rowContextMenu.AddSeparator();
            var deleteShortcut = UnityEngine.Application.platform == RuntimePlatform.OSXEditor
                ? "Backspace" : "Delete";
            rowContextMenu.AddAction
            (
                "행 삭제",
                () => ExecuteContextRowAction(pageIndex, sourceIndex, originalRevision, HandleRemove),
                enabled: hasSelection && !isReadOnly, shortcut: deleteShortcut
            );
            rowContextMenu.Open(worldPosition);
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// 메뉴를 연 뒤 셀 재사용이나 페이지 변경이 발생해도 다른 행을 수정하지 않는다.
        /// </summary>
        // ----------------------------------------------------------------------
        private void ExecuteContextRowAction
        (
            int pageIndex, int sourceIndex, ulong originalRevision, Action execute
        )
        {
            model.SerializedObject.UpdateIfRequiredOrScript();
            if (model.Revision != originalRevision
                || !IsCurrentPageRow(pageIndex, sourceIndex))
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

        // 메뉴 공용 키 처리를 제외한 Collection 명령 단축키만 연결한다.
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

    #region 선택과 키보드 입력

        // ------------------------------------------------------------
        /// <summary>
        /// table selection 유무를 Remove command 활성 상태에 반영한다.
        /// </summary>
        // ------------------------------------------------------------
        private void HandleSelectionChanged(IEnumerable<object> selectedItems)
        {
            if (!restoringSelection)
            {
                selectedIdentity = TryGetSelectedSourceIndex(out var source)
                    ? model.GetRowIdentity(source)
                    : null;
            }

            RefreshStatus();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// table focus 상태에서 row clipboard shortcut을 처리한다.
        /// </summary>
        // ------------------------------------------------------------
        private void HandleKeyDown(KeyDownEvent evt)
        {
            if (IsEditingFieldEvent(evt))
            {
                return;
            }

            // 키보드 사용자도 Shift+F10으로 같은 컨텍스트 메뉴를 열 수 있다.
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
            if (actionKey && evt.keyCode == KeyCode.C)
            {
                HandleCopySelected();
                StopShortcutEvent(evt);
                return;
            }

            if (actionKey && evt.keyCode == KeyCode.V)
            {
                HandlePasteSelected();
                StopShortcutEvent(evt);
                return;
            }

            if (actionKey && evt.keyCode == KeyCode.D)
            {
                HandleDuplicateSelected();
                StopShortcutEvent(evt);
                return;
            }

            if (evt.keyCode == KeyCode.Delete
                || (UnityEngine.Application.platform == RuntimePlatform.OSXEditor && evt.keyCode == KeyCode.Backspace))
            {
                HandleRemove();
                StopShortcutEvent(evt);
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 column의 편집 정책에 맞게 Enter 상세 진입을 처리한다.
        /// </summary>
        // ------------------------------------------------------------
        private void OpenSelectedCell()
        {
            if (!TryGetSelectedSourceIndex(out var source))
            {
                return;
            }

            var property = model.GetCellProperty(source, selectedColumnID);
            var mode = model.GetCellEditMode(source, selectedColumnID);
            var opened = CollectionCellRenderer.OpenEditor
            (
                model,
                source,
                property,
                mode,
                RevealRow,
                isReadOnly
            );
            if (!opened)
            {
                LogCommandFailure
                (
                    mode == CollectionCellEditMode.ReadOnly
                        ? "이 키는 수정할 수 없습니다."
                        : "목록에서 직접 수정하세요. 참조가 없으면 형식을 선택하세요."
                );
            }
        }

        private bool IsEditingFieldEvent(KeyDownEvent evt)
        {
            return IsEditingFieldTarget(evt.target as VisualElement);
        }

        private bool IsEditingFieldTarget(VisualElement element)
        {
            while (element != null && element != listView)
            {
                if (element is TextField
                    || element.ClassListContains("unity-base-field"))
                {
                    return true;
                }

                element = element.parent;
            }

            return false;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Collection Editor가 소비한 key event 전파를 중단한다.
        /// </summary>
        // ------------------------------------------------------------
        private static void StopShortcutEvent(KeyDownEvent evt)
        {
            evt.StopPropagation();
            evt.StopImmediatePropagation();
            evt.PreventDefault();
        }

    #endregion

    #region 컬렉션 작업

        // ------------------------------------------------------------
        /// <summary>
        /// collection 전체 수준의 action menu를 표시한다.
        /// </summary>
        // ------------------------------------------------------------
        private void ShowCollectionActions()
        {
            collectionActionsMenu.Clear();
            collectionActionsMenu.AddAction("모든 항목 삭제", HandleClear, enabled: !isReadOnly && model.Count > 0);
            collectionActionsMenu.AddAction("무효한 항목 정리", HandleRemoveInvalid, enabled: !isReadOnly && HasInvalidRows());
            AppendPopulateActions(collectionActionsMenu);
            collectionActionsMenu.OpenBelow();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 모든 row를 하나의 Undo group으로 제거한다.
        /// </summary>
        // ------------------------------------------------------------
        private void HandleClear()
        {
            if (isReadOnly)
            {
                return;
            }

            var rows = new List<int>(model.Count);
            for (var sourceIndex = 0; sourceIndex < model.Count; sourceIndex++)
            {
                rows.Add(sourceIndex);
            }

            if (rows.Count == 0)
            {
                return;
            }

            CloseDetailEditors();
            model.RemoveMany(rows, "컬렉션 전체 삭제");
            RefreshRows();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// validation 오류가 있는 row를 하나의 Undo group으로 제거한다.
        /// </summary>
        // ------------------------------------------------------------
        private void HandleRemoveInvalid()
        {
            if (isReadOnly)
            {
                return;
            }

            var invalidRows = model.GetInvalidRowIndices();

            if (invalidRows.Count == 0)
            {
                return;
            }

            CloseDetailEditors();
            model.RemoveMany(invalidRows, "무효한 컬렉션 항목 정리");
            RefreshRows();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 collection에 validation 오류 row가 있는지 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        private bool HasInvalidRows()
        {
            return model.GetInvalidRowIndices().Count > 0;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 키 집합 편집이 가능한 컬렉션에 자동 채우기 명령을 추가한다.
        /// </summary>
        // ------------------------------------------------------------
        private void AppendPopulateActions(XeriContextMenu menu)
        {
            if (model is not XDictionaryEditorModel dictionaryModel
                || dictionaryModel.KeyType == null)
            {
                return;
            }

            if (!dictionaryModel.KeyType.IsEnum && dictionaryModel.KeyType != typeof(int))
            {
                return;
            }

            Action<CollectionKeySetOperation> execute;
            if (dictionaryModel.KeyType.IsEnum)
            {
                execute = operation => HandlePopulateEnum(dictionaryModel, operation);
            }
            else
            {
                execute = operation => CollectionKeyRangeWindow.Open(dictionaryModel, operation);
            }

            menu.AddSeparator();
            AddPopulateOperationMenu(menu.AddSubmenu("키 자동 채우기"), execute);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 키 추가·제거·범위 제한 명령을 하위 메뉴에 추가한다.
        /// </summary>
        // ------------------------------------------------------------
        private static void AddPopulateOperationMenu
        (
            XeriContextMenuItem menu, Action<CollectionKeySetOperation> execute
        )
        {
            menu.AddAction("추가", () => execute(CollectionKeySetOperation.Add));
            menu.AddAction("제거", () => execute(CollectionKeySetOperation.Remove));
            menu.AddAction("지정 범위만 유지", () => execute(CollectionKeySetOperation.Confine));
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 열거형 전체 값을 키 집합으로 만들어 지정 작업을 실행한다.
        /// </summary>
        // ------------------------------------------------------------
        private void HandlePopulateEnum
        (
            XDictionaryEditorModel dictionaryModel, CollectionKeySetOperation operation
        )
        {
            if (isReadOnly)
            {
                return;
            }

            var enumValues = Enum.GetValues(dictionaryModel.KeyType);
            var keys = new List<object>(enumValues.Length);

            foreach (var value in enumValues)
            {
                keys.Add(value);
            }

            CloseDetailEditors();
            dictionaryModel.ApplyKeySet(keys, operation);
            RefreshRows();
        }

    #endregion

    #region 항목 클립보드

        // ------------------------------------------------------------
        /// <summary>
        /// 선택 row를 Editor-session clipboard에 복사한다.
        /// </summary>
        // ------------------------------------------------------------
        private void HandleCopySelected()
        {
            if (!TryGetSelectedSourceIndex(out var sourceIndex))
            {
                return;
            }

            model.SerializedObject.UpdateIfRequiredOrScript();

            var rowProperty = model.GetRowProperty(sourceIndex);
            if (SerializedPropertyClipboard.TryCopyValue(rowProperty, out var reason))
            {
                return;
            }

            LogCommandFailure($"행 복사 실패: {reason}");
        }

        // ------------------------------------------------------------
        /// <summary>
        /// clipboard payload를 선택 row에 붙여 넣는다.
        /// </summary>
        // ------------------------------------------------------------
        private void HandlePasteSelected()
        {
            if (isReadOnly)
            {
                return;
            }

            if (!TryGetSelectedSourceIndex(out var sourceIndex))
            {
                return;
            }

            CloseDetailEditors();
            if (!model.TryPasteAt(sourceIndex, out var resolved, out var reason))
            {
                LogCommandFailure($"행 붙여넣기 실패: {reason}");
                return;
            }

            if (resolved >= 0)
            {
                RevealRow(resolved);
            }
            else
            {
                selectedIdentity = null;
                RefreshRows();
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 선택 row의 독립 snapshot으로 새 항목을 추가한다.
        /// </summary>
        // ------------------------------------------------------------
        private void HandleDuplicateSelected()
        {
            if (isReadOnly)
            {
                return;
            }

            if (!TryGetSelectedSourceIndex(out var sourceIndex))
            {
                return;
            }

            CloseDetailEditors();
            RevealRow(model.DuplicateAt(sourceIndex));
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 page의 명시적 source row 삭제 버튼을 처리한다.
        /// </summary>
        // ------------------------------------------------------------
        private void HandleRemoveAt(int pageIndex, int sourceIndex)
        {
            if (isReadOnly)
            {
                return;
            }

            // virtualized cell이 갱신되었다면 오래된 버튼이 다른 행을 삭제하지 않는다.
            if (pageIndex < 0 || pageIndex >= searchPagedRows.PageItems.Count
                || searchPagedRows.PageItems[pageIndex] != sourceIndex
                || sourceIndex < 0 || sourceIndex >= model.Count)
            {
                RefreshRows();
                return;
            }

            RemoveRowAndSelectNext(pageIndex, sourceIndex);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 행 삭제 후 상세 창·목록 상태를 갱신하고 다음 인접 항목을 선택한다.
        /// </summary>
        // ------------------------------------------------------------
        private void RemoveRowAndSelectNext(int pageIndex, int sourceIndex)
        {
            if (isReadOnly)
            {
                return;
            }

            CloseDetailEditors();
            model.RemoveAt(sourceIndex);
            selectedIdentity = null;
            RefreshRows();

            var nextPageIndex = Math.Min(pageIndex, searchPagedRows.PageItems.Count - 1);
            if (nextPageIndex >= 0)
            {
                listView.SetSelection(nextPageIndex);
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 선택 row를 지정 방향으로 이동한다.
        /// </summary>
        // ------------------------------------------------------------
        private void HandleMoveSelected(int offset)
        {
            if (!CanMoveSelected(offset, out var sourceIndex))
            {
                return;
            }

            MoveRowToIndex(sourceIndex, sourceIndex + offset);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 선택한 행의 전체 인덱스를 입력받고 변경되지 않은 원본에만 적용한다.
        /// </summary>
        // ------------------------------------------------------------
        private void OpenMoveIndex()
        {
            if (isReadOnly || !model.CanReorder
                || !string.IsNullOrWhiteSpace(searchPagedRows.SearchText)
                || !TryGetSelectedSourceIndex(out var sourceIndex))
            {
                return;
            }

            var originalCount = model.Count;
            var originalRevision = model.Revision;
            CollectionRowMoveWindow.Open
            (
                sourceIndex, originalCount,
                destinationIndex =>
                {
                    if (panel == null
                        || isReadOnly || !model.CanReorder
                        || !string.IsNullOrWhiteSpace(searchPagedRows.SearchText)
                        || model.Count != originalCount
                        || model.Revision != originalRevision
                        || !TryGetSelectedSourceIndex(out var selectedIndex)
                        || selectedIndex != sourceIndex)
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
        /// 메뉴·드래그·인덱스 입력에 동일한 삽입형 이동 명령을 적용한다.
        /// </summary>
        // ------------------------------------------------------------
        private void MoveRowToIndex(int sourceIndex, int destinationIndex)
        {
            if (isReadOnly || !model.CanReorder
                || sourceIndex < 0 || sourceIndex >= model.Count
                || destinationIndex < 0 || destinationIndex >= model.Count
                || sourceIndex == destinationIndex)
            {
                return;
            }

            CloseDetailEditors();
            var resolvedIndex = model.Move(sourceIndex, destinationIndex);
            if (resolvedIndex >= 0)
            {
                RevealRow(resolvedIndex);
                return;
            }

            // 정규화 후 행 identity가 모호하면 다른 행을 임의로 선택하지 않는다.
            selectedIdentity = null;
            UpdateRows();
            RefreshPageView();
            listView.ClearSelection();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 선택 row를 지정 방향으로 이동할 수 있는지 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        private bool CanMoveSelected(int offset, out int sourceIndex)
        {
            sourceIndex = -1;

            if (isReadOnly || !model.CanReorder
                || !string.IsNullOrWhiteSpace(searchPagedRows.SearchText))
            {
                return false;
            }

            if (!TryGetSelectedSourceIndex(out sourceIndex))
            {
                return false;
            }

            var destinationIndex = sourceIndex + offset;
            return destinationIndex >= 0 && destinationIndex < model.Count;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 table selection의 source row index를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
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
        /// 구조 또는 payload 변경 전에 열린 상세 편집 창을 닫는다.
        /// </summary>
        // ------------------------------------------------------------
        private void CloseDetailEditors()
        {
            CollectionValueEditorWindow.CloseForCollection(model.SerializedObject.targetObject, model.CollectionPropertyPath);
        }

    #endregion

    #region 드래그 명령 연결

        // ------------------------------------------------------------
        /// <summary>
        /// 유효한 드래그 시작 시 원본 열 선택과 페이지 행 선택을 동기화한다.
        /// </summary>
        // ------------------------------------------------------------
        private void SelectDragPageRow(int pageIndex)
        {
            selectedColumnID = model.Columns[0].ID;
            listView.SetSelection(pageIndex);
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// 유효한 드롭 요청을 Model mutation으로 실행하고 이동한 행을 표시한다.
        /// </summary>
        // ----------------------------------------------------------------------
        private void MoveDraggedRow(int sourceIndex, int destinationIndex)
        {
            MoveRowToIndex(sourceIndex, destinationIndex);
        }

    #endregion

    }
}
