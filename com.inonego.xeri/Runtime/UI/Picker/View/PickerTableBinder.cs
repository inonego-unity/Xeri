/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : PickerTableBinder.cs
수정일 : 2026-10-31

# 설명
Picker MultiColumnListView의 열 폭 정책, 행 바인딩, 선택·정렬 입력을 관리한다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UIElements;

using inonego;
using inonego.Xeri;
using inonego.Xeri.UI;

namespace inonego.Xeri.UI.Picker
{
    // ============================================================
    /// <summary>
    /// Picker 테이블의 행·열 렌더링과 선택·정렬 입력을 연결한다.
    /// </summary>
    // ============================================================
    internal sealed class PickerTableBinder<TEntry, TValue>
    {

    #region 필드

        private const string imageColumnID = "__xeri_picker_image";

        private readonly PickerSession<TEntry, TValue> session;
        private readonly VisualElement root;
        private readonly MultiColumnListView listView;
        private readonly Label emptyLabel;
        private readonly List<PickerEntry<TEntry, TValue>> pageItems = new();

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// table binder를 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        public PickerTableBinder(PickerSession<TEntry, TValue> session, VisualElement root)
        {
            this.session = session;
            this.root    = root;
            listView     = root.Q<MultiColumnListView>("entry-table");
            emptyLabel   = root.Q<Label>("empty-label");

            ConfigureListView();
        }

    #endregion

    #region 테이블 구성

        // ------------------------------------------------------------
        /// <summary>
        /// 테이블의 기본 동작을 설정하고 열과 입력 이벤트를 연결한다.
        /// </summary>
        // ------------------------------------------------------------
        private void ConfigureListView()
        {
            listView.selectionType = SelectionType.Single;
            listView.focusable = false;
            listView.fixedItemHeight = 30f;
            listView.showAlternatingRowBackgrounds = AlternatingRowBackground.None;
            listView.sortingMode = ColumnSortingMode.Custom;
            ConfigureColumns();

            listView.RegisterCallback<PointerDownEvent>(HandlePointerDown, TrickleDown.TrickleDown);
            listView.selectionChanged += HandleSelectionChanged;
            listView.itemsChosen += HandleItemsChosen;
            listView.columnSortingChanged += HandleColumnSortingChanged;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 표시 열을 생성하고 열 순서에 맞는 초기 가중치를 적용한다.
        /// </summary>
        // ------------------------------------------------------------
        private void ConfigureColumns()
        {
            listView.columns.Add
            (
                new Column
                {
                    name = imageColumnID,
                    title = string.Empty,
                    width = 42f,
                    minWidth = 42f,
                    maxWidth = 42f,
                    stretchable = false,
                    sortable = false,
                    makeCell = MakeImageCell,
                    bindCell = BindImageCell,
                }
            );

            var stretchWeights = new List<float>(session.Columns.Count + 1);
            stretchWeights.Add(0f);
            foreach (var pickerColumn in session.Columns)
            {
                var column = pickerColumn;
                if (column.Options.Visibility == PickerColumnVisibility.Hidden) continue;

                var listColumn = new Column
                {
                    name = column.ID,
                    title = column.Header,
                    width = column.Options.Layout.Width,
                    minWidth = column.Options.Layout.MinWidth,
                    stretchable = column.Options.Layout.Mode == PickerColumnLayoutMode.Flexible,
                    sortable = column.Options.Sortable,
                    makeCell = MakeTextCell,
                    bindCell = (element, index) => BindTextCell(element, index, column),
                };

                if (column.Options.Layout.MaxWidth > 0f)
                {
                    listColumn.maxWidth = column.Options.Layout.MaxWidth;
                }

                listView.columns.Add(listColumn);
                stretchWeights.Add(column.Options.Layout.StretchWeight);
            }

            listView.ApplyColumnSizing(stretchWeights);
        }

    #endregion

    #region 페이지 동기화

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 페이지의 표시 목록과 선택 상태를 갱신한다.
        /// </summary>
        // ------------------------------------------------------------
        public void Refresh()
        {
            var pageChanged = RefreshPageItems();

            if (!ReferenceEquals(listView.itemsSource, pageItems))
            {
                listView.itemsSource = pageItems;
                pageChanged = true;
            }

            // 선택만 바뀌었다면 가상화된 cell을 다시 생성하지 않는다.
            if (pageChanged)
            {
                listView.Rebuild();
            }

            emptyLabel.style.display = pageItems.Count == 0 ? DisplayStyle.Flex : DisplayStyle.None;

            var index = session.CurrentEntry == null
                ? -1
                : pageItems.IndexOf(session.CurrentEntry);
            if (listView.selectedIndex == index)
            {
                return;
            }

            if (index >= 0)
            {
                listView.SetSelection(index);
            }
            else
            {
                listView.ClearSelection();
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 실제 페이지 구성이나 순서가 바뀐 경우에만 table buffer를 갱신한다.
        /// </summary>
        // ------------------------------------------------------------
        private bool RefreshPageItems()
        {
            var entries = session.PageEntries;
            var changed = pageItems.Count != entries.Count;
            if (!changed)
            {
                for (var index = 0; index < entries.Count; index++)
                {
                    if (!ReferenceEquals(pageItems[index], entries[index]))
                    {
                        changed = true;
                        break;
                    }
                }
            }

            if (!changed)
            {
                return false;
            }

            pageItems.Clear();
            foreach (var entry in entries)
            {
                pageItems.Add(entry);
            }

            return true;
        }

    #endregion

    #region 셀 생성과 바인딩

        // ------------------------------------------------------------
        /// <summary>
        /// image column cell을 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        private static VisualElement MakeImageCell()
        {
            var cell = new VisualElement();
            cell.AddToClassList("xeri-picker__table-cell");
            cell.AddToClassList("xeri-picker__table-cell-image-frame");

            var image = new Image
            {
                scaleMode = ScaleMode.ScaleAndCrop,
            };

            image.AddToClassList("xeri-picker__table-cell-image");
            cell.Add(image);

            return cell;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// image column cell에 현재 page entry 이미지를 바인딩한다.
        /// </summary>
        // ------------------------------------------------------------
        private void BindImageCell(VisualElement element, int index)
        {
            ApplyRowTone(element, index);

            var image = element.Q<Image>();
            if (image == null) return;

            if (index < 0 || index >= pageItems.Count)
            {
                image.image = PickerDefaultTextures.GrayChecker;
                return;
            }

            image.image = pageItems[index].Image ?? PickerDefaultTextures.GrayChecker;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// text column cell을 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        private static VisualElement MakeTextCell()
        {
            var label = new Label();
            label.AddToClassList("xeri-picker__table-cell");
            label.AddToClassList("xeri-picker__table-cell-label");

            return label;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// column cell에 현재 page entry 값을 바인딩한다.
        /// </summary>
        // ------------------------------------------------------------
        private void BindTextCell(VisualElement element, int index, PickerColumnSpec<TEntry> column)
        {
            var label = element as Label;
            if (label == null) return;

            ApplyRowTone(label, index);
            ApplyColumnOptions(label, column.Options);

            if (index < 0 || index >= pageItems.Count)
            {
                label.text = string.Empty;
                return;
            }

            label.text = pageItems[index].GetColumn(column.ID).DisplayText;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// column 옵션을 재사용 cell label에 반영한다.
        /// </summary>
        // ------------------------------------------------------------
        private static void ApplyColumnOptions(Label label, PickerColumnOptions options)
        {
            label.EnableInClassList("xeri-picker__table-cell-label--align-left", options.Alignment == PickerColumnAlignment.Left);
            label.EnableInClassList("xeri-picker__table-cell-label--align-center", options.Alignment == PickerColumnAlignment.Center);
            label.EnableInClassList("xeri-picker__table-cell-label--align-right", options.Alignment == PickerColumnAlignment.Right);
            label.EnableInClassList("xeri-picker__table-cell-label--overflow-ellipsis", options.Overflow == PickerColumnOverflow.Ellipsis);
            label.EnableInClassList("xeri-picker__table-cell-label--overflow-clip", options.Overflow == PickerColumnOverflow.Clip);
            label.EnableInClassList("xeri-picker__table-cell-label--overflow-wrap", options.Overflow == PickerColumnOverflow.Wrap);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// table row 홀짝 배경 tone을 cell에 적용한다.
        /// </summary>
        // ------------------------------------------------------------
        private static void ApplyRowTone(VisualElement element, int index)
        {
            element.EnableInClassList("xeri-picker__table-cell--even", index % 2 == 0);
            element.EnableInClassList("xeri-picker__table-cell--odd", index % 2 != 0);
        }

    #endregion

    #region 사용자 입력과 선택

        // ----------------------------------------------------------------------
        /// <summary>
        /// table 클릭 직후 keyboard shortcut이 이어서 동작하도록 focus를 확보한다.
        /// </summary>
        // ----------------------------------------------------------------------
        private void HandlePointerDown(PointerDownEvent evt)
        {
            root.Focus();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 선택 변경을 session 현재 entry로 반영한다.
        /// </summary>
        // ------------------------------------------------------------
        private void HandleSelectionChanged(IEnumerable<object> selectedItems)
        {
            var entry = GetFirstEntry(selectedItems);

            if (entry != null)
            {
                root.Focus();
            }

            session.SetCurrentEntry(entry);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 더블 클릭 항목을 확정 선택한다.
        /// </summary>
        // ------------------------------------------------------------
        private void HandleItemsChosen(IEnumerable<object> chosenItems)
        {
            session.Confirm(GetFirstEntry(chosenItems));
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// UI Toolkit column 정렬 입력을 session 전체 결과 정렬로 위임한다.
        /// </summary>
        // ----------------------------------------------------------------------
        private void HandleColumnSortingChanged()
        {
            foreach (var sortedColumn in listView.sortedColumns)
            {
                var columnID = sortedColumn.columnName;

                if (string.IsNullOrEmpty(columnID))
                {
                    columnID = sortedColumn.column?.name;
                }

                if (string.IsNullOrEmpty(columnID)) return;

                session.SetSort(columnID, sortedColumn.direction == SortDirection.Ascending);
                return;
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// UI Toolkit 선택 목록에서 첫 번째 Picker 항목을 찾는다.
        /// </summary>
        // ------------------------------------------------------------
        private static PickerEntry<TEntry, TValue> GetFirstEntry(IEnumerable<object> items)
        {
            foreach (var item in items)
            {
                if (item is PickerEntry<TEntry, TValue> entry)
                {
                    return entry;
                }
            }

            return null;
        }

    #endregion

    }
}
