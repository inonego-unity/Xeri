/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : PickerView.cs
수정일 : 2026-06-04

# 설명
Unixeri 커스텀 picker의 최종 목표 레이아웃을 확인하기 위한 UI Toolkit 목업 View.
미리보기, 검색/필터, 컬럼형 Entry 목록, 페이징, 단일 선택/더블클릭 확정 흐름을 포함한다.

# 특이사항
실제 데이터 provider, IFilter, DataPackage 연동은 구현하지 않고 임의 Entry 데이터만 사용한다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace inonego.Xeri.Editor.Picker
{
    // ============================================================
    /// <summary>
    /// Picker mockup root view.
    /// </summary>
    // ============================================================
    public sealed class PickerMockupView : VisualElement
    {
        #region 스타일 상수

        private static readonly Color UnityWindow = Hex(0x2B, 0x2B, 0x2B);
        private static readonly Color UnityPanel = Hex(0x32, 0x32, 0x32);
        private static readonly Color UnityPanelDarker = Hex(0x1F, 0x1F, 0x1F);
        private static readonly Color UnityRowEven = Hex(0x20, 0x20, 0x20);
        private static readonly Color UnityRowOdd = Hex(0x25, 0x25, 0x25);
        private static readonly Color UnityBorder = Hex(0x3F, 0x3F, 0x3F);
        private static readonly Color UnityBorderLight = Hex(0x4A, 0x4A, 0x4A);
        private static readonly Color UnityText = Hex(0xD6, 0xD6, 0xD6);
        private static readonly Color UnityTextMuted = Hex(0xA5, 0xA5, 0xA5);
        private static readonly Color UnityTextDim = Hex(0x7E, 0x7E, 0x7E);
        private static readonly Color UnityControl = Hex(0x38, 0x38, 0x38);
        private static readonly Color UnityControlActive = Hex(0x4B, 0x4B, 0x4B);
        private static readonly Color UnityControlPressed = Hex(0x27, 0x27, 0x27);
        private static readonly Color UnityAccent = Hex(0x36, 0x5F, 0x86);
        private static readonly Color UnityAccentText = Hex(0xD8, 0xE6, 0xF2);
        private static readonly Color UnityAccentBorder = Hex(0x55, 0x75, 0x95);
        private static readonly Color UnityGreen = Hex(0x3E, 0x69, 0x4F);
        private static readonly Color UnityGreenBorder = Hex(0x5C, 0x82, 0x65);
        private static readonly Color UnityOrange = Hex(0x71, 0x5C, 0x3C);
        private static readonly Color UnityOrangeBorder = Hex(0x8C, 0x74, 0x4F);
        private static readonly Color UnityGrayBlue = Hex(0x54, 0x59, 0x62);
        private static readonly Color ImagePanel = Hex(0x26, 0x28, 0x2C);

        private const float PreviewSummaryHeight = 54f;
        private const float PreviewSummaryScrollRightGutter = 3f;
        private const float PreviewSummaryTextHorizontalPadding = 10f;
        private const float PreviewSummaryTextRightPadding = 22f;
        private const float PreviewSummaryTextVerticalPadding = 6f;

        #endregion

        #region 내부 데이터

        // ============================================================
        /// <summary>
        /// Mockup entry data.
        /// </summary>
        // ============================================================
        private sealed class Entry
        {
            public string ID;
            public string Name;
            public int Age;
            public string StudentID;
            public string Status;
            public string Source;
            public string Summary;
            public Texture2D Thumbnail;
            public bool IsRecent;
            public bool IsValid;
        }

        private enum ColumnCellType
        {
            Text,
            Image,
        }

        // ============================================================
        /// <summary>
        /// Table column definition.
        /// </summary>
        // ============================================================
        private sealed class Column
        {
            public string ID;
            public string Header;
            public float Width;
            public float MinWidth = 48;
            public bool Stretchable = true;
            public bool Sortable = true;
            public ColumnCellType CellType = ColumnCellType.Text;
            public Func<Entry, string> GetText;
            public Func<Entry, Texture2D> GetImage;
        }

        // ============================================================
        /// <summary>
        /// Button state colors.
        /// </summary>
        // ============================================================
        private readonly struct ButtonPalette
        {
            public readonly Color Background;
            public readonly Color Border;
            public readonly Color Text;

            public ButtonPalette(Color background, Color border, Color text)
            {
                Background = background;
                Border = border;
                Text = text;
            }
        }

        // ============================================================
        /// <summary>
        /// Runtime visual state for mockup buttons.
        /// </summary>
        // ============================================================
        private sealed class ButtonVisualState
        {
            public ButtonPalette Normal;
            public ButtonPalette Hover;
            public ButtonPalette Pressed;
            public ButtonPalette Active;
            public ButtonPalette ActiveHover;
            public ButtonPalette ActivePressed;
            public ButtonPalette Disabled;
            public bool IsActive;
            public bool IsHovered;
            public bool IsPressed;
        }

        #endregion

        #region 이벤트

        // ------------------------------------------------------------
        /// <summary>
        /// Entry double click selection event.
        /// </summary>
        // ------------------------------------------------------------
        public event Action<string> OnEntryConfirmed = null;

        #endregion

        #region 필드

        private readonly List<Entry> allEntries = new();
        private readonly List<Entry> filteredEntries = new();
        private readonly List<Entry> pageEntries = new();
        private readonly List<Column> columns = new();
        private readonly List<Texture2D> mockTextures = new();

        private Entry selectedEntry = null;

        private ToolbarSearchField searchField = null;
        private Button activeFilterButton = null;
        private Button recentFilterButton = null;
        private Button validFilterButton = null;
        private Label resultLabel = null;
        private Label pageLabel = null;
        private Button firstPageButton = null;
        private Button previousPageButton = null;
        private Button nextPageButton = null;
        private Button lastPageButton = null;
        private MultiColumnListView listView = null;
        private Label emptyStateLabel = null;

        private Label previewTitle = null;
        private Label previewSubtitle = null;
        private ScrollView previewSummaryScroll = null;
        private Label previewSummary = null;
        private Label previewMeta = null;
        private Image previewImage = null;
        private Button selectButton = null;

        private bool activeOnly = false;
        private bool recentOnly = false;
        private bool validOnly = false;
        private int pageIndex = 0;
        private const int PAGE_SIZE = 8;

        private string sortColumnID = "name";
        private bool sortAscending = true;

        #endregion

        #region 생성자

        public PickerMockupView() : base()
        {
            name = "xeri-picker-view";
            AddToClassList("xeri-picker-view");
            BuildMockData();
            BuildColumns();
            BuildLayout();
            RegisterCallback<KeyDownEvent>(HandleKeyDown);
            RegisterCallback<DetachFromPanelEvent>(_ => ReleaseMockTextures());
            Refresh();
        }

        #endregion

        #region UI 구성

        private void BuildLayout()
        {
            ApplyRootStyle();

            Add(BuildPreviewPane());
            Add(BuildToolbar());
            Add(BuildEntryList());
            Add(BuildFooter());
        }

        private VisualElement BuildPreviewPane()
        {
            var preview = new VisualElement();
            preview.AddToClassList("picker-preview");
            preview.style.flexShrink = 0;
            preview.style.paddingLeft = 14;
            preview.style.paddingRight = 14;
            preview.style.paddingTop = 10;
            preview.style.paddingBottom = 8;
            preview.style.backgroundColor = UnityPanel;
            preview.style.borderBottomWidth = 1;
            preview.style.borderBottomColor = UnityBorder;

            var topRow = new VisualElement();
            topRow.style.flexDirection = FlexDirection.Row;
            topRow.style.alignItems = Align.Center;
            preview.Add(topRow);

            previewImage = new Image();
            previewImage.scaleMode = ScaleMode.ScaleAndCrop;
            previewImage.style.width = 56;
            previewImage.style.height = 56;
            previewImage.style.borderTopLeftRadius = 4;
            previewImage.style.borderTopRightRadius = 4;
            previewImage.style.borderBottomLeftRadius = 4;
            previewImage.style.borderBottomRightRadius = 4;
            previewImage.style.backgroundColor = ImagePanel;
            previewImage.style.marginRight = 10;
            topRow.Add(previewImage);

            var titleColumn = new VisualElement();
            titleColumn.style.flexGrow = 1;
            topRow.Add(titleColumn);

            previewTitle = new Label("선택 없음");
            previewTitle.style.fontSize = 16;
            previewTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
            previewTitle.style.color = UnityText;
            titleColumn.Add(previewTitle);

            previewSubtitle = new Label("목록에서 항목을 선택하세요.");
            previewSubtitle.style.fontSize = 11;
            previewSubtitle.style.color = UnityTextMuted;
            previewSubtitle.style.marginTop = 2;
            titleColumn.Add(previewSubtitle);

            var actionRow = new VisualElement();
            actionRow.style.flexDirection = FlexDirection.Row;
            actionRow.style.alignItems = Align.Center;
            topRow.Add(actionRow);

            selectButton = CreateToolButton("선택", UnityAccent, UnityAccentBorder);
            selectButton.clicked += ConfirmSelectedEntry;
            actionRow.Add(selectButton);

            var summaryFrame = new VisualElement();
            summaryFrame.style.height = PreviewSummaryHeight;
            summaryFrame.style.marginTop = 8;
            summaryFrame.style.backgroundColor = UnityPanelDarker;
            summaryFrame.style.borderTopLeftRadius = 4;
            summaryFrame.style.borderTopRightRadius = 4;
            summaryFrame.style.borderBottomLeftRadius = 4;
            summaryFrame.style.borderBottomRightRadius = 4;
            summaryFrame.style.position = Position.Relative;
            summaryFrame.style.overflow = Overflow.Hidden;
            preview.Add(summaryFrame);

            previewSummaryScroll = new ScrollView(ScrollViewMode.Vertical);
            previewSummaryScroll.style.flexGrow = 1;
            previewSummaryScroll.style.alignSelf = Align.Stretch;
            previewSummaryScroll.style.marginRight = PreviewSummaryScrollRightGutter;
            previewSummaryScroll.style.paddingLeft = 0;
            previewSummaryScroll.style.paddingRight = 0;
            previewSummaryScroll.style.paddingTop = 0;
            previewSummaryScroll.style.paddingBottom = 0;
            previewSummaryScroll.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            previewSummaryScroll.verticalScrollerVisibility = ScrollerVisibility.Auto;
            previewSummaryScroll.contentContainer.style.alignItems = Align.Stretch;
            previewSummaryScroll.contentContainer.style.justifyContent = Justify.FlexStart;
            previewSummaryScroll.contentContainer.style.paddingLeft = PreviewSummaryTextHorizontalPadding;
            previewSummaryScroll.contentContainer.style.paddingRight = PreviewSummaryTextRightPadding;
            previewSummaryScroll.contentContainer.style.paddingTop = PreviewSummaryTextVerticalPadding;
            previewSummaryScroll.contentContainer.style.paddingBottom = PreviewSummaryTextVerticalPadding;
            StyleDescriptionScroller(previewSummaryScroll);
            summaryFrame.Add(previewSummaryScroll);

            previewSummary = new Label("선택한 항목의 미리보기가 여기에 표시됩니다.");
            previewSummary.style.whiteSpace = WhiteSpace.Normal;
            previewSummary.style.color = UnityText;
            previewSummary.style.fontSize = 11;
            previewSummary.style.width = Length.Percent(100);
            previewSummary.style.marginTop = 0;
            previewSummary.style.marginBottom = 0;
            previewSummary.style.unityTextAlign = TextAnchor.UpperLeft;
            previewSummaryScroll.Add(previewSummary);

            previewMeta = new Label("요약 | 원본 | 참조 | 검증");
            previewMeta.style.fontSize = 10;
            previewMeta.style.color = UnityTextDim;
            previewMeta.style.marginTop = 5;
            preview.Add(previewMeta);

            return preview;
        }

        private VisualElement BuildToolbar()
        {
            var toolbar = new VisualElement();
            toolbar.style.flexDirection = FlexDirection.Column;
            toolbar.style.paddingLeft = 10;
            toolbar.style.paddingRight = 10;
            toolbar.style.paddingTop = 6;
            toolbar.style.paddingBottom = 6;
            toolbar.style.backgroundColor = UnityPanel;
            toolbar.style.borderBottomWidth = 1;
            toolbar.style.borderBottomColor = UnityBorder;

            var searchRow = new VisualElement();
            searchRow.style.flexDirection = FlexDirection.Row;
            searchRow.style.alignItems = Align.Center;
            searchRow.style.height = 24;
            toolbar.Add(searchRow);

            searchField = new ToolbarSearchField();
            searchField.value = string.Empty;
            searchField.tooltip = "검색";
            searchField.style.flexGrow = 1;
            searchField.style.height = 22;
            searchField.style.marginLeft = 0;
            searchField.style.marginRight = 0;
            searchField.style.marginTop = 0;
            searchField.style.marginBottom = 0;
            searchField.RegisterValueChangedCallback(_ =>
            {
                pageIndex = 0;
                Refresh();
            });
            searchRow.Add(searchField);

            var chipRow = new VisualElement();
            chipRow.style.flexDirection = FlexDirection.Row;
            chipRow.style.marginTop = 6;
            toolbar.Add(chipRow);

            activeFilterButton = CreateChipButton("활성", UnityAccent, UnityAccentBorder);
            activeFilterButton.clicked += () =>
            {
                activeOnly = !activeOnly;
                pageIndex = 0;
                Refresh();
            };
            chipRow.Add(activeFilterButton);

            recentFilterButton = CreateChipButton("최근", UnityOrange, UnityOrangeBorder);
            recentFilterButton.clicked += () =>
            {
                recentOnly = !recentOnly;
                pageIndex = 0;
                Refresh();
            };
            chipRow.Add(recentFilterButton);

            validFilterButton = CreateChipButton("유효", UnityGreen, UnityGreenBorder);
            validFilterButton.clicked += () =>
            {
                validOnly = !validOnly;
                pageIndex = 0;
                Refresh();
            };
            chipRow.Add(validFilterButton);

            return toolbar;
        }

        private VisualElement BuildEntryList()
        {
            var listContainer = new VisualElement();
            listContainer.style.flexGrow = 1;
            listContainer.style.position = Position.Relative;
            listContainer.style.backgroundColor = UnityPanelDarker;

            listView = new MultiColumnListView
            {
                fixedItemHeight = 30,
                virtualizationMethod = CollectionVirtualizationMethod.FixedHeight,
                selectionType = SelectionType.Single,
                sortingMode = ColumnSortingMode.Default,
                showAlternatingRowBackgrounds = AlternatingRowBackground.None,
                itemsSource = pageEntries,
            };
            listView.focusable = true;
            listView.style.flexGrow = 1;
            listView.style.backgroundColor = UnityPanelDarker;
            listView.style.borderTopWidth = 0;
            listView.style.borderBottomWidth = 0;
            listView.style.borderLeftWidth = 0;
            listView.style.borderRightWidth = 0;
            listView.selectionChanged += HandleSelectionChanged;
            listView.itemsChosen += HandleItemsChosen;
            listView.columnSortingChanged += HandleColumnSortingChanged;
            listView.RegisterCallback<KeyDownEvent>(HandleKeyDown);
            ConfigureListColumns(listView);
            listContainer.Add(listView);

            emptyStateLabel = new Label("비어 있음");
            emptyStateLabel.pickingMode = PickingMode.Ignore;
            emptyStateLabel.style.position = Position.Absolute;
            emptyStateLabel.style.left = 0;
            emptyStateLabel.style.right = 0;
            emptyStateLabel.style.top = 30;
            emptyStateLabel.style.bottom = 0;
            emptyStateLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            emptyStateLabel.style.color = UnityTextDim;
            emptyStateLabel.style.fontSize = 12;
            emptyStateLabel.style.display = DisplayStyle.None;
            listContainer.Add(emptyStateLabel);

            return listContainer;
        }

        private VisualElement BuildFooter()
        {
            var footer = new VisualElement();
            footer.style.flexDirection = FlexDirection.Row;
            footer.style.alignItems = Align.Stretch;
            footer.style.height = 28;
            footer.style.paddingLeft = 10;
            footer.style.paddingRight = 10;
            footer.style.backgroundColor = UnityPanel;
            footer.style.borderTopWidth = 1;
            footer.style.borderTopColor = UnityBorder;

            resultLabel = new Label();
            resultLabel.style.flexGrow = 1;
            resultLabel.style.height = Length.Percent(100);
            resultLabel.style.unityTextAlign = TextAnchor.MiddleLeft;
            resultLabel.style.color = UnityTextMuted;
            resultLabel.style.fontSize = 10;
            footer.Add(resultLabel);

            firstPageButton = CreatePagerButton("첫 페이지", "◀◀", () => SetPage(0));
            previousPageButton = CreatePagerButton("이전 페이지", "◀", () => SetPage(pageIndex - 1));
            pageLabel = new Label();
            pageLabel.style.width = 52;
            pageLabel.style.height = Length.Percent(100);
            pageLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            pageLabel.style.color = UnityText;
            pageLabel.style.fontSize = 10;
            nextPageButton = CreatePagerButton("다음 페이지", "▶", () => SetPage(pageIndex + 1));
            lastPageButton = CreatePagerButton("마지막 페이지", "▶▶", () => SetPage(GetPageCount() - 1));

            footer.Add(firstPageButton);
            footer.Add(previousPageButton);
            footer.Add(pageLabel);
            footer.Add(nextPageButton);
            footer.Add(lastPageButton);

            return footer;
        }

        private void ConfigureListColumns(MultiColumnListView target)
        {
            target.columns.Clear();

            foreach (var pickerColumn in columns)
            {
                var column = pickerColumn;
                target.columns.Add(new UnityEngine.UIElements.Column
                {
                    name = column.ID,
                    title = column.Header,
                    width = column.Width,
                    minWidth = column.MinWidth,
                    stretchable = column.Stretchable,
                    sortable = column.Sortable,
                    makeCell = column.CellType == ColumnCellType.Image ? MakeImageColumnCell : MakeTextColumnCell,
                    bindCell = (element, index) => BindColumnCell(element, index, column),
                });
            }
        }

        private static VisualElement MakeTextColumnCell()
        {
            var label = new Label();
            label.style.height = Length.Percent(100);
            label.style.width = Length.Percent(100);
            label.style.flexGrow = 1;
            label.style.paddingLeft = 10;
            label.style.unityTextAlign = TextAnchor.MiddleLeft;
            label.style.overflow = Overflow.Hidden;
            return label;
        }

        private static VisualElement MakeImageColumnCell()
        {
            var container = new VisualElement();
            container.style.height = Length.Percent(100);
            container.style.width = Length.Percent(100);
            container.style.alignItems = Align.Center;
            container.style.justifyContent = Justify.Center;

            var image = new Image();
            image.name = "entry-thumbnail";
            image.scaleMode = ScaleMode.ScaleAndCrop;
            image.style.width = 22;
            image.style.height = 22;
            image.style.borderTopLeftRadius = 3;
            image.style.borderTopRightRadius = 3;
            image.style.borderBottomLeftRadius = 3;
            image.style.borderBottomRightRadius = 3;
            image.style.backgroundColor = ImagePanel;
            container.Add(image);

            return container;
        }

        private void BindColumnCell(VisualElement element, int index, Column column)
        {
            if (column.CellType == ColumnCellType.Image)
            {
                BindImageColumnCell(element, index, column);
                return;
            }

            BindTextColumnCell(element, index, column);
        }

        private void BindTextColumnCell(VisualElement element, int index, Column column)
        {
            if (element is not Label label) return;

            label.style.color = UnityText;
            label.style.backgroundColor = ShouldUseSelectionBackground(index)
                ? Color.clear
                : GetRowBackground(index);
            label.text = index >= 0 && index < pageEntries.Count
                ? column.GetText(pageEntries[index])
                : string.Empty;
        }

        private void BindImageColumnCell(VisualElement element, int index, Column column)
        {
            element.style.backgroundColor = ShouldUseSelectionBackground(index)
                ? Color.clear
                : GetRowBackground(index);

            var image = element.Q<Image>("entry-thumbnail");
            if (image == null)
            {
                return;
            }

            image.image = index >= 0 && index < pageEntries.Count
                ? column.GetImage(pageEntries[index])
                : null;
        }

        #endregion

        #region 갱신

        private void Refresh()
        {
            ApplyFilters();
            ClearSelectionWhenFilteredOut();
            ApplySort();
            ClampPage();
            BuildPage();
            RefreshList();
            RefreshPreview();
            RefreshFooter();
            RefreshChipState();
        }

        private void ApplyFilters()
        {
            filteredEntries.Clear();

            var search = searchField?.value?.Trim() ?? string.Empty;

            foreach (var entry in allEntries)
            {
                if (activeOnly && entry.Status != "활성") continue;
                if (recentOnly && !entry.IsRecent) continue;
                if (validOnly && !entry.IsValid) continue;
                if (!MatchesSearch(entry, search)) continue;

                filteredEntries.Add(entry);
            }
        }

        private void ApplySort()
        {
            Comparison<Entry> comparison = sortColumnID switch
            {
                "age" => (a, b) => a.Age.CompareTo(b.Age),
                "student" => (a, b) => string.CompareOrdinal(a.StudentID, b.StudentID),
                "status" => (a, b) => string.CompareOrdinal(a.Status, b.Status),
                "source" => (a, b) => string.CompareOrdinal(a.Source, b.Source),
                _ => (a, b) => string.CompareOrdinal(a.Name, b.Name),
            };

            filteredEntries.Sort((a, b) => sortAscending ? comparison(a, b) : comparison(b, a));
        }

        private void BuildPage()
        {
            pageEntries.Clear();

            var start = pageIndex * PAGE_SIZE;
            for (int i = start; i < filteredEntries.Count && i < start + PAGE_SIZE; i++)
            {
                pageEntries.Add(filteredEntries[i]);
            }
        }

        private void RefreshList()
        {
            if (listView == null) return;

            listView.itemsSource = pageEntries;
            listView.Rebuild();
            RefreshEmptyState();

            if (selectedEntry != null)
            {
                var selectedIndex = pageEntries.IndexOf(selectedEntry);
                if (selectedIndex >= 0)
                {
                    listView.SetSelectionWithoutNotify(new[] { selectedIndex });
                    return;
                }
            }

            listView.ClearSelection();
        }

        private void RefreshEmptyState()
        {
            if (emptyStateLabel == null || listView == null)
            {
                return;
            }

            emptyStateLabel.style.display = pageEntries.Count == 0
                ? DisplayStyle.Flex
                : DisplayStyle.None;

            listView.schedule.Execute(HideBuiltInEmptyLabel).ExecuteLater(0);
        }

        private void HideBuiltInEmptyLabel()
        {
            if (listView == null)
            {
                return;
            }

            listView.Query<Label>().ForEach(label =>
            {
                if (label.text == "List is empty")
                {
                    label.style.display = DisplayStyle.None;
                }
            });
        }

        private void RefreshPreview()
        {
            if (selectedEntry == null)
            {
                previewTitle.text = "선택 없음";
                previewSubtitle.text = "목록에서 항목을 선택하세요. 더블 클릭하면 선택됩니다.";
                previewSummary.text = "데이터 종류에 따라 이미지, 요약 정보, 참조, 검증 결과 등을 표시할 수 있습니다.";
                previewMeta.text = "요약 | 원본 | 참조 | 검증";
                previewImage.image = null;
                previewImage.style.backgroundColor = ImagePanel;
                ResetDescriptionScroll();
                return;
            }

            previewTitle.text = selectedEntry.Name;
            previewSubtitle.text = $"{selectedEntry.StudentID} | {selectedEntry.Source} 이미지 데이터 | {selectedEntry.Status}";
            previewSummary.text = selectedEntry.Summary;
            previewMeta.text = $"나이 {selectedEntry.Age} | 식별자 {selectedEntry.ID} | {(selectedEntry.IsValid ? "유효" : "확인 필요")}";
            previewImage.image = selectedEntry.Thumbnail;
            previewImage.style.backgroundColor = ImagePanel;
            ResetDescriptionScroll();
        }

        private void RefreshFooter()
        {
            var total = allEntries.Count;
            var count = filteredEntries.Count;
            var pageCount = GetPageCount();
            var start = count == 0 ? 0 : pageIndex * PAGE_SIZE + 1;
            var end = Mathf.Min(count, (pageIndex + 1) * PAGE_SIZE);

            resultLabel.text = $"{start}-{end} / {count}개 (전체 {total}개)";
            pageLabel.text = $"{pageIndex + 1}/{pageCount}";

            firstPageButton.SetEnabled(pageIndex > 0);
            previousPageButton.SetEnabled(pageIndex > 0);
            nextPageButton.SetEnabled(pageIndex < pageCount - 1);
            lastPageButton.SetEnabled(pageIndex < pageCount - 1);

            RefreshButtonVisualState(firstPageButton);
            RefreshButtonVisualState(previousPageButton);
            RefreshButtonVisualState(nextPageButton);
            RefreshButtonVisualState(lastPageButton);
        }

        private void RefreshChipState()
        {
            ApplyChipState(activeFilterButton, activeOnly);
            ApplyChipState(recentFilterButton, recentOnly);
            ApplyChipState(validFilterButton, validOnly);
            UpdateSelectButtonState();
        }

        #endregion

        #region 이벤트 처리

        private void HandleSelectionChanged(IEnumerable<object> selection)
        {
            var entry = selection.OfType<Entry>().FirstOrDefault();

            selectedEntry = entry;
            RefreshPreview();
            UpdateSelectButtonState();
            listView.RefreshItems();
        }

        private void HandleColumnSortingChanged()
        {
            if (listView == null)
            {
                return;
            }

            var sortedColumn = listView.sortedColumns.FirstOrDefault();
            var columnID = sortedColumn?.columnName;
            if (string.IsNullOrEmpty(columnID))
            {
                columnID = sortedColumn?.column?.name;
            }

            if (string.IsNullOrEmpty(columnID))
            {
                return;
            }

            sortColumnID = columnID;
            sortAscending = sortedColumn.direction == SortDirection.Ascending;
            pageIndex = 0;
            Refresh();
        }

        private void HandleKeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode != KeyCode.Escape)
            {
                return;
            }

            ClearSelection();
            evt.StopPropagation();
        }

        private void HandleItemsChosen(IEnumerable<object> chosenItems)
        {
            var entry = chosenItems.OfType<Entry>().FirstOrDefault();
            if (entry == null) return;

            selectedEntry = entry;
            RefreshPreview();
            UpdateSelectButtonState();
            OnEntryConfirmed?.Invoke(entry.ID);
        }

        private void SetPage(int newPageIndex)
        {
            pageIndex = Mathf.Clamp(newPageIndex, 0, GetPageCount() - 1);
            Refresh();
        }

        private void ConfirmSelectedEntry()
        {
            if (selectedEntry == null)
            {
                return;
            }

            OnEntryConfirmed?.Invoke(selectedEntry.ID);
        }

        private void UpdateSelectButtonState()
        {
            if (selectButton == null)
            {
                return;
            }

            var hasSelection = selectedEntry != null;
            selectButton.SetEnabled(hasSelection);
            if (selectButton.userData is ButtonVisualState state)
            {
                state.IsActive = hasSelection;
            }

            RefreshButtonVisualState(selectButton);
        }

        #endregion

        #region 데이터/헬퍼

        private void BuildMockData()
        {
            var names = new[]
            {
                "김아린", "서민우", "박준호", "이하나", "최도윤", "한유리", "문이안", "정소라",
                "권미나", "신레오", "백유나", "강재현", "임나리", "유오원", "조은", "홍노엘",
                "송아라", "남태오", "장린", "하준", "안노라", "윤카이", "류모아", "고이든",
                "오세린", "차도겸", "민주원", "도하린", "배서진", "라온",
            };

            for (int i = 0; i < names.Length; i++)
            {
                var status = i % 7 == 0 ? "잠김" : i % 5 == 0 ? "주의" : "활성";
                var source = i % 3 == 0 ? "데이터" : i % 3 == 1 ? "사전" : "외부";
                var thumbnail = CreateMockTexture(i);
                var summary = names[i] == "강재현"
                    ? $"{names[i]} 항목의 이미지형 예시 미리보기입니다. 이 항목은 설명 스크롤 테스트를 위한 긴 목업 데이터입니다.\n썸네일은 대표 이미지나 Unity Object preview, Addressable asset preview, 혹은 외부 API에서 받은 미니어처 이미지를 표시하는 슬롯으로 사용할 수 있습니다.\n설명에는 원본 이미지 경로, Addressable key, 데이터 패키지 이름, 테이블 row ID, 검증 메시지, 해상도, 압축 포맷, 태그, 참조 중인 리소스, 마지막 갱신 시간 같은 정보가 길게 들어올 수 있습니다.\n이 텍스트는 preview 전체가 아니라 설명 박스 안에서만 스크롤되므로, 선택을 바꿔도 상단 preview의 이미지, 이름, 태그 영역은 같은 위치를 유지합니다.\n실제 구현에서는 PreviewModel.Description에 들어온 긴 문자열을 이 영역에 넣고, 더 자세한 정보는 inspector나 별도 detail window로 넘기는 방식이 적합합니다.\n스크롤이 보이는지 확인하려면 이 항목을 선택한 뒤 설명 박스 안쪽을 드래그하거나 마우스 휠로 내려보면 됩니다."
                    : $"{names[i]} 항목의 이미지형 예시 미리보기입니다. 실제 구현에서는 썸네일, 원본 이미지, 참조 경로, 검증 메시지, 해상도나 태그 같은 시각 데이터 정보를 함께 표시할 수 있습니다.";
                allEntries.Add(new Entry
                {
                    ID = $"항목-{i + 1:000}",
                    Name = names[i],
                    Age = 18 + i % 9,
                    StudentID = $"학번-{1001 + i}",
                    Status = status,
                    Source = source,
                    Summary = summary,
                    Thumbnail = thumbnail,
                    IsRecent = i % 4 == 0,
                    IsValid = status != "주의",
                });
            }
        }

        private void BuildColumns()
        {
            columns.Add(new Column
            {
                ID = "thumbnail",
                Header = string.Empty,
                Width = 42,
                MinWidth = 42,
                Stretchable = false,
                Sortable = false,
                CellType = ColumnCellType.Image,
                GetImage = entry => entry.Thumbnail,
            });
            columns.Add(new Column { ID = "name", Header = "이름", Width = 190, GetText = entry => entry.Name });
            columns.Add(new Column { ID = "age", Header = "나이", Width = 70, GetText = entry => entry.Age.ToString() });
            columns.Add(new Column { ID = "student", Header = "학번", Width = 130, GetText = entry => entry.StudentID });
            columns.Add(new Column { ID = "status", Header = "상태", Width = 100, GetText = entry => entry.Status });
            columns.Add(new Column { ID = "source", Header = "출처", Width = 130, GetText = entry => entry.Source });
        }

        private static bool MatchesSearch(Entry entry, string search)
        {
            if (string.IsNullOrEmpty(search)) return true;

            return entry.Name.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0
                   || entry.StudentID.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0
                   || entry.Status.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0
                   || entry.Source.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private Texture2D CreateMockTexture(int index)
        {
            var baseColor = (index % 4) switch
            {
                0 => UnityAccent,
                1 => UnityGreen,
                2 => UnityOrange,
                _ => UnityGrayBlue,
            };
            var texture = new Texture2D(48, 48, TextureFormat.RGBA32, false)
            {
                name = $"PickerMockThumbnail_{index:00}",
                hideFlags = HideFlags.HideAndDontSave,
                filterMode = FilterMode.Point,
            };

            for (int y = 0; y < texture.height; y++)
            {
                for (int x = 0; x < texture.width; x++)
                {
                    var gradient = (x + y) / 96f;
                    var color = Blend(baseColor, UnityPanelDarker, gradient * 0.35f);
                    if ((x / 8 + y / 8 + index) % 2 == 0)
                    {
                        color = Blend(color, UnityTextMuted, 0.08f);
                    }

                    if (x > 30 && y < 16)
                    {
                        color = Blend(color, UnityText, 0.16f);
                    }

                    texture.SetPixel(x, y, color);
                }
            }

            texture.Apply();
            mockTextures.Add(texture);
            return texture;
        }

        private void ReleaseMockTextures()
        {
            foreach (var texture in mockTextures)
            {
                if (texture != null)
                {
                    UnityEngine.Object.DestroyImmediate(texture);
                }
            }

            mockTextures.Clear();
        }

        private void ClampPage()
        {
            pageIndex = Mathf.Clamp(pageIndex, 0, GetPageCount() - 1);
        }

        private void ClearSelectionWhenFilteredOut()
        {
            if (selectedEntry != null && !filteredEntries.Contains(selectedEntry))
            {
                selectedEntry = null;
            }
        }

        private void ClearSelection()
        {
            selectedEntry = null;
            listView?.ClearSelection();
            RefreshPreview();
            UpdateSelectButtonState();
        }

        private Color GetRowBackground(int index)
        {
            if (index < 0 || index >= pageEntries.Count)
            {
                return UnityPanelDarker;
            }

            return index % 2 == 0 ? UnityRowEven : UnityRowOdd;
        }

        private bool ShouldUseSelectionBackground(int index)
        {
            return index >= 0
                   && index < pageEntries.Count
                   && ReferenceEquals(pageEntries[index], selectedEntry);
        }

        private int GetPageCount()
        {
            return Mathf.Max(1, Mathf.CeilToInt(filteredEntries.Count / (float)PAGE_SIZE));
        }

        private static Button CreateToolButton(string text, Color activeColor, Color activeBorder)
        {
            var button = new Button { text = text };
            button.style.height = 24;
            button.style.paddingLeft = 10;
            button.style.paddingRight = 10;
            button.style.borderTopLeftRadius = 4;
            button.style.borderTopRightRadius = 4;
            button.style.borderBottomLeftRadius = 4;
            button.style.borderBottomRightRadius = 4;
            button.style.borderTopWidth = 1;
            button.style.borderBottomWidth = 1;
            button.style.borderLeftWidth = 1;
            button.style.borderRightWidth = 1;
            RegisterButtonVisualState(button, CreateAccentButtonState(activeColor, activeBorder));
            return button;
        }

        private static Button CreateChipButton(string text, Color accent, Color accentBorder)
        {
            var button = CreateToolButton(text, accent, accentBorder);
            button.style.height = 22;
            button.style.marginRight = 6;
            button.style.fontSize = 11;
            return button;
        }

        private static Button CreatePagerButton(string tooltip, string text, Action onClick)
        {
            var button = new Button { text = text };
            button.tooltip = tooltip;
            button.style.height = Length.Percent(100);
            button.style.width = 32;
            button.style.minWidth = 32;
            button.style.paddingLeft = 0;
            button.style.paddingRight = 0;
            button.style.marginLeft = 0;
            button.style.fontSize = 11;
            button.style.borderTopLeftRadius = 0;
            button.style.borderTopRightRadius = 0;
            button.style.borderBottomLeftRadius = 0;
            button.style.borderBottomRightRadius = 0;
            button.style.borderTopWidth = 0;
            button.style.borderBottomWidth = 0;
            button.style.borderLeftWidth = 1;
            button.style.borderRightWidth = 1;
            button.style.unityTextAlign = TextAnchor.MiddleCenter;
            button.style.unityFontStyleAndWeight = FontStyle.Bold;
            RegisterButtonVisualState(button, CreatePagerButtonState());
            button.clicked += onClick;
            return button;
        }

        private static void ApplyChipState(Button button, bool enabled)
        {
            if (button == null)
            {
                return;
            }

            if (button.userData is ButtonVisualState state)
            {
                state.IsActive = enabled;
            }

            RefreshButtonVisualState(button);
        }

        private static ButtonVisualState CreateAccentButtonState(Color activeColor, Color activeBorder)
        {
            return new ButtonVisualState
            {
                Normal = new ButtonPalette(UnityControl, UnityBorderLight, UnityTextMuted),
                Hover = new ButtonPalette(UnityControlActive, UnityBorderLight, UnityText),
                Pressed = new ButtonPalette(UnityControlPressed, UnityAccentBorder, UnityAccentText),
                Active = new ButtonPalette(activeColor, activeBorder, UnityAccentText),
                ActiveHover = new ButtonPalette(Blend(activeColor, UnityAccentText, 0.16f), activeBorder, UnityAccentText),
                ActivePressed = new ButtonPalette(Blend(activeColor, UnityPanelDarker, 0.28f), activeBorder, UnityAccentText),
                Disabled = new ButtonPalette(UnityPanel, UnityBorder, UnityTextDim),
            };
        }

        private void ResetDescriptionScroll()
        {
            if (previewSummaryScroll == null)
            {
                return;
            }

            previewSummaryScroll.scrollOffset = Vector2.zero;
        }

        private static void StyleDescriptionScroller(ScrollView scrollView)
        {
            var scroller = scrollView.verticalScroller;
            scroller.style.backgroundColor = Color.clear;
            scroller.lowButton.style.display = DisplayStyle.None;
            scroller.highButton.style.display = DisplayStyle.None;
            scroller.slider.style.backgroundColor = Color.clear;
            scroller.slider.style.marginTop = 5;
            scroller.slider.style.marginBottom = 5;
            scroller.slider.style.marginLeft = 0;
            scroller.slider.style.marginRight = 0;
            scroller.slider.style.borderTopWidth = 0;
            scroller.slider.style.borderBottomWidth = 0;
            scroller.slider.style.borderLeftWidth = 0;
            scroller.slider.style.borderRightWidth = 0;

            scrollView.schedule.Execute(() => StyleDescriptionScrollerParts(scroller)).ExecuteLater(0);
        }

        private static void StyleDescriptionScrollerParts(Scroller scroller)
        {
            var tracker = scroller.slider.Q<VisualElement>("unity-tracker");
            if (tracker != null)
            {
                tracker.style.backgroundColor = Color.clear;
                tracker.style.borderTopWidth = 0;
                tracker.style.borderBottomWidth = 0;
                tracker.style.borderLeftWidth = 0;
                tracker.style.borderRightWidth = 0;
            }

            var draggerBorder = scroller.slider.Q<VisualElement>("unity-dragger-border");
            if (draggerBorder != null)
            {
                draggerBorder.style.backgroundColor = Color.clear;
                draggerBorder.style.borderTopWidth = 0;
                draggerBorder.style.borderBottomWidth = 0;
                draggerBorder.style.borderLeftWidth = 0;
                draggerBorder.style.borderRightWidth = 0;
                draggerBorder.style.marginLeft = 0;
                draggerBorder.style.marginRight = 0;
            }

            var dragger = scroller.slider.Q<VisualElement>(className: "unity-dragger");
            if (dragger == null)
            {
                return;
            }

            dragger.style.backgroundColor = UnityTextDim;
            dragger.style.minHeight = 8;
            dragger.style.borderTopLeftRadius = 3;
            dragger.style.borderTopRightRadius = 3;
            dragger.style.borderBottomLeftRadius = 3;
            dragger.style.borderBottomRightRadius = 3;
            dragger.style.marginLeft = 0;
            dragger.style.marginRight = 0;
        }

        private static ButtonVisualState CreatePagerButtonState()
        {
            return new ButtonVisualState
            {
                Normal = new ButtonPalette(UnityPanel, UnityBorder, UnityTextMuted),
                Hover = new ButtonPalette(UnityControl, UnityBorderLight, UnityText),
                Pressed = new ButtonPalette(UnityAccent, UnityAccentBorder, UnityAccentText),
                Active = new ButtonPalette(UnityAccent, UnityAccentBorder, UnityAccentText),
                ActiveHover = new ButtonPalette(Blend(UnityAccent, UnityAccentText, 0.12f), UnityAccentBorder, UnityAccentText),
                ActivePressed = new ButtonPalette(Blend(UnityAccent, UnityPanelDarker, 0.25f), UnityAccentBorder, UnityAccentText),
                Disabled = new ButtonPalette(UnityPanel, UnityBorder, UnityTextDim),
            };
        }

        private static void RegisterButtonVisualState(Button button, ButtonVisualState state)
        {
            button.userData = state;
            button.RegisterCallback<PointerEnterEvent>(_ =>
            {
                state.IsHovered = true;
                RefreshButtonVisualState(button);
            });
            button.RegisterCallback<PointerLeaveEvent>(_ =>
            {
                state.IsHovered = false;
                state.IsPressed = false;
                RefreshButtonVisualState(button);
            });
            button.RegisterCallback<PointerDownEvent>(_ =>
            {
                state.IsPressed = true;
                RefreshButtonVisualState(button);
            });
            button.RegisterCallback<PointerUpEvent>(_ =>
            {
                state.IsPressed = false;
                RefreshButtonVisualState(button);
            });
            RefreshButtonVisualState(button);
        }

        private static void RefreshButtonVisualState(Button button)
        {
            if (button?.userData is not ButtonVisualState state)
            {
                return;
            }

            var palette = !button.enabledSelf
                ? state.Disabled
                : state.IsActive && state.IsPressed
                    ? state.ActivePressed
                    : state.IsActive && state.IsHovered
                        ? state.ActiveHover
                        : state.IsActive
                            ? state.Active
                            : state.IsPressed
                                ? state.Pressed
                                : state.IsHovered
                                    ? state.Hover
                                    : state.Normal;

            ApplyButtonPalette(button, palette);
        }

        private static void ApplyButtonPalette(Button button, ButtonPalette palette)
        {
            button.style.backgroundColor = palette.Background;
            button.style.borderTopColor = palette.Border;
            button.style.borderBottomColor = palette.Border;
            button.style.borderLeftColor = palette.Border;
            button.style.borderRightColor = palette.Border;
            button.style.color = palette.Text;
        }

        private void ApplyRootStyle()
        {
            style.flexGrow = 1;
            style.width = Length.Percent(100);
            style.height = Length.Percent(100);
            style.minWidth = 640;
            style.minHeight = 520;
            style.backgroundColor = UnityWindow;
            style.borderTopWidth = 1;
            style.borderBottomWidth = 1;
            style.borderLeftWidth = 1;
            style.borderRightWidth = 1;
            style.borderTopColor = UnityBorder;
            style.borderBottomColor = UnityBorder;
            style.borderLeftColor = UnityBorder;
            style.borderRightColor = UnityBorder;
        }

        private static Color Hex(byte red, byte green, byte blue)
        {
            return new Color(red / 255f, green / 255f, blue / 255f);
        }

        private static Color Blend(Color from, Color to, float weight)
        {
            return Color.Lerp(from, to, Mathf.Clamp01(weight));
        }

        #endregion
    }
}
