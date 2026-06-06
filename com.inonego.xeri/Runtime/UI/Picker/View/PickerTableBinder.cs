/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : PickerTableBinder.cs
수정일 : 2026-06-07

# 설명
Picker MultiColumnListView의 column, row, selection, double click, sorting 입력을 관리한다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UIElements;

namespace inonego.Xeri.UI.Picker
{
   // ============================================================
   /// <summary>
   /// Picker MultiColumnListView 바인더.
   /// </summary>
   // ============================================================
   internal sealed class PickerTableBinder<TEntry, TValue>
   {

   #region 필드

      private const string ImageColumnID = "__xeri_picker_image";

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
      public PickerTableBinder(PickerSession<TEntry, TValue> session, VisualElement root) : base()
      {
         this.session = session;
         this.root    = root;
         listView     = root.Q<MultiColumnListView>("entry-table");
         emptyLabel   = root.Q<Label>("empty-label");

         ConfigureListView();
      }

   #endregion

   #region 메서드

      // ------------------------------------------------------------
      /// <summary>
      /// table 표시 목록과 선택 상태를 갱신한다.
      /// </summary>
      // ------------------------------------------------------------
      public void Refresh()
      {
         RefreshPageItems();

         if (!ReferenceEquals(listView.itemsSource, pageItems))
         {
            listView.itemsSource = pageItems;
         }

         listView.Rebuild();
         emptyLabel.style.display = pageItems.Count == 0 ? DisplayStyle.Flex : DisplayStyle.None;

         if (session.CurrentEntry == null)
         {
            listView.ClearSelection();
            return;
         }

         var index = pageItems.IndexOf(session.CurrentEntry);
         if (index >= 0)
         {
            listView.SetSelection(index);
            return;
         }

         listView.ClearSelection();
      }

      // ------------------------------------------------------------
      /// <summary>
      /// session page 결과를 table용 buffer에 복사한다.
      /// </summary>
      // ------------------------------------------------------------
      private void RefreshPageItems()
      {
         pageItems.Clear();
         foreach (var entry in session.PageEntries)
         {
            pageItems.Add(entry);
         }
      }

      // ------------------------------------------------------------
      /// <summary>
      /// list view 기본 동작과 column을 구성한다.
      /// </summary>
      // ------------------------------------------------------------
      private void ConfigureListView()
      {
         listView.selectionType = SelectionType.Single;
         listView.focusable = false;
         listView.fixedItemHeight = 30f;
         listView.showAlternatingRowBackgrounds = AlternatingRowBackground.None;
         listView.sortingMode = ColumnSortingMode.Custom;
         listView.RegisterCallback<PointerDownEvent>(HandlePointerDown, TrickleDown.TrickleDown);
         listView.selectionChanged += HandleSelectionChanged;
         listView.itemsChosen += HandleItemsChosen;
         listView.columnSortingChanged += HandleColumnSortingChanged;

         listView.columns.Add
         (
            new Column
            {
               name = ImageColumnID,
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

         foreach (var pickerColumn in session.Columns)
         {
            var column = pickerColumn;
            listView.columns.Add
            (
               new Column
               {
                  name = column.ID,
                  title = column.Header,
                  width = column.Width,
                  minWidth = 40f,
                  stretchable = true,
                  sortable = column.Sortable,
                  makeCell = MakeTextCell,
                  bindCell = (element, index) => BindTextCell(element, index, column),
               }
            );
         }
      }

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
      /// column cell에 현재 page entry 값을 바인딩한다.
      /// </summary>
      // ------------------------------------------------------------
      private void BindTextCell(VisualElement element, int index, PickerColumnSpec<TEntry> column)
      {
         var label = element as Label;
         if (label == null) return;

         ApplyRowTone(label, index);

         if (index < 0 || index >= pageItems.Count)
         {
            label.text = string.Empty;
            return;
         }

         label.text = pageItems[index].GetColumn(column.ID).DisplayText;
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

      // ------------------------------------------------------------
      /// <summary>
      /// table 클릭 직후 keyboard shortcut이 이어서 동작하도록 focus를 확보한다.
      /// </summary>
      // ------------------------------------------------------------
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

      // ------------------------------------------------------------
      /// <summary>
      /// UI Toolkit column 정렬 입력을 session 전체 결과 정렬로 위임한다.
      /// </summary>
      // ------------------------------------------------------------
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
      /// UI Toolkit 선택 collection에서 picker entry만 추출한다.
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
