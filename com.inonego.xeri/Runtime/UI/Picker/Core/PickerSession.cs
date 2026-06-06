/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : PickerSession.cs
수정일 : 2026-06-07

# 설명
Picker 실행 단위의 검색, 필터, 정렬, 현재 선택, 페이징 상태를 관리한다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

using inonego.Xeri;
using inonego.Xeri.Utility;

namespace inonego.Xeri.UI.Picker
{
   // ============================================================
   /// <summary>
   /// Picker 실행 단위의 상태를 관리한다.
   /// </summary>
   // ============================================================
   public sealed class PickerSession<TEntry, TValue>
   {

   #region 필드

      private readonly PickerSpec<TEntry, TValue> spec;
      private readonly Action<TValue> onSelected;
      private readonly List<PickerEntry<TEntry, TValue>> allEntries;
      private readonly List<PickerEntry<TEntry, TValue>> filteredEntries = new();
      private readonly List<PickerEntry<TEntry, TValue>> pageEntries = new();
      private readonly Dictionary<string, bool> filterStates = new();
      private readonly Paginator paginator;
      private string searchText = string.Empty;
      private string sortColumnID = string.Empty;
      private bool sortAscending = true;

      // ------------------------------------------------------------
      /// <summary>
      /// 현재 페이지에 표시할 entry 목록.
      /// </summary>
      // ------------------------------------------------------------
      public IReadOnlyList<PickerEntry<TEntry, TValue>> PageEntries => pageEntries;

      // ------------------------------------------------------------
      /// <summary>
      /// 현재 강조된 entry.
      /// </summary>
      // ------------------------------------------------------------
      public PickerEntry<TEntry, TValue> CurrentEntry { get; private set; } = null;

      // ------------------------------------------------------------
      /// <summary>
      /// filter/search 적용 후 전체 entry 개수.
      /// </summary>
      // ------------------------------------------------------------
      public int FilteredCount => filteredEntries.Count;

      // ------------------------------------------------------------
      /// <summary>
      /// 읽기 전용 paginator 상태.
      /// </summary>
      // ------------------------------------------------------------
      public IReadOnlyPaginator Paginator => paginator;

      // ------------------------------------------------------------
      /// <summary>
      /// 현재 페이지 index.
      /// </summary>
      // ------------------------------------------------------------
      public int PageIndex => paginator.PageIndex;

      // ------------------------------------------------------------
      /// <summary>
      /// 현재 페이지 표시 번호.
      /// </summary>
      // ------------------------------------------------------------
      public int PageNumber => paginator.PageNumber;

      // ------------------------------------------------------------
      /// <summary>
      /// 전체 페이지 개수.
      /// </summary>
      // ------------------------------------------------------------
      public int PageCount => paginator.PageCount;

      // ------------------------------------------------------------
      /// <summary>
      /// table column 정의 목록.
      /// </summary>
      // ------------------------------------------------------------
      public IReadOnlyList<PickerColumnSpec<TEntry>> Columns => spec.Columns;

      // ------------------------------------------------------------
      /// <summary>
      /// filter 정의 목록.
      /// </summary>
      // ------------------------------------------------------------
      public IReadOnlyList<PickerFilterSpec<TEntry, TValue>> Filters => spec.Filters;

      // ------------------------------------------------------------
      /// <summary>
      /// view 전용 표시 옵션.
      /// </summary>
      // ------------------------------------------------------------
      public PickerViewOptions ViewOptions => spec.ViewOptions;

      // ------------------------------------------------------------
      /// <summary>
      /// preview 영역 표시 여부.
      /// </summary>
      // ------------------------------------------------------------
      public bool ShowPreview => ViewOptions.ShowPreview;

      // ------------------------------------------------------------
      /// <summary>
      /// 선택 없음 preview에 표시할 기본 tag label 목록.
      /// </summary>
      // ------------------------------------------------------------
      public IReadOnlyList<string> DefaultPreviewTags => spec.DefaultPreviewTags;

   #endregion

   #region 이벤트

      // ------------------------------------------------------------
      /// <summary>
      /// session 표시 상태가 변경될 때 발생한다.
      /// </summary>
      // ------------------------------------------------------------
      public event EventHandler Changed = null;

      // ------------------------------------------------------------
      /// <summary>
      /// 선택이 확정될 때 발생한다.
      /// </summary>
      // ------------------------------------------------------------
      public event EventHandler<PickerSelectionEventArgs<TEntry, TValue>> Confirmed = null;

   #endregion

   #region 생성자

      // ------------------------------------------------------------
      /// <summary>
      /// Picker session을 생성한다.
      /// </summary>
      // ------------------------------------------------------------
      public PickerSession
      (
         PickerSpec<TEntry, TValue> spec,
         IReadOnlyList<TEntry> entries,
         TValue currentValue,
         Action<TValue> onSelected,
         int pageSize
      ) : base()
      {
         this.spec       = spec ?? throw new ArgumentNullException(nameof(spec));
         this.onSelected = onSelected ?? (_ => { });
         paginator       = new Paginator(pageSize);
         allEntries      = CreateEntries(entries);

         foreach (var filter in spec.Filters)
         {
            filterStates[filter.ID] = filter.DefaultEnabled;
         }

         CurrentEntry = FindEntryByValue(currentValue);
         Refresh();
         MoveToCurrentEntryPage();
      }

   #endregion

   #region 검색/필터/정렬

      // ------------------------------------------------------------
      /// <summary>
      /// 검색어를 설정한다.
      /// </summary>
      // ------------------------------------------------------------
      public void SetSearchText(string value)
      {
         searchText = value ?? string.Empty;
         paginator.MoveFirst();
         RefreshAndNotify();
      }

      // ------------------------------------------------------------
      /// <summary>
      /// filter 활성 상태를 설정한다.
      /// </summary>
      // ------------------------------------------------------------
      public void SetFilterEnabled(string filterID, bool enabled)
      {
         filterStates[filterID ?? string.Empty] = enabled;
         paginator.MoveFirst();
         RefreshAndNotify();
      }

      // ------------------------------------------------------------
      /// <summary>
      /// filter 활성 상태를 반환한다.
      /// </summary>
      // ------------------------------------------------------------
      public bool GetFilterEnabled(string filterID)
      {
         return filterStates.TryGetValue(filterID ?? string.Empty, out var enabled) && enabled;
      }

      // ------------------------------------------------------------
      /// <summary>
      /// column 정렬을 설정한다.
      /// </summary>
      // ------------------------------------------------------------
      public void SetSort(string columnID, bool ascending)
      {
         if (!IsSortableColumn(columnID)) return;

         sortColumnID = columnID ?? string.Empty;
         sortAscending = ascending;
         paginator.MoveFirst();
         RefreshAndNotify();
      }

   #endregion

   #region 선택

      // ------------------------------------------------------------
      /// <summary>
      /// 현재 entry를 설정한다.
      /// </summary>
      // ------------------------------------------------------------
      public void SetCurrentEntry(PickerEntry<TEntry, TValue> entry)
      {
         CurrentEntry = entry;
         Changed?.Invoke(this, EventArgs.Empty);
      }

      // ------------------------------------------------------------
      /// <summary>
      /// 현재 선택을 비운다.
      /// </summary>
      // ------------------------------------------------------------
      public void ClearCurrentEntry()
      {
         CurrentEntry = null;
         Changed?.Invoke(this, EventArgs.Empty);
      }

      // ------------------------------------------------------------
      /// <summary>
      /// 지정한 entry를 확정 선택한다.
      /// </summary>
      // ------------------------------------------------------------
      public void Confirm(PickerEntry<TEntry, TValue> entry)
      {
         if (entry == null || !entry.IsEnabled) return;

         CurrentEntry = entry;
         onSelected(entry.Value);
         Confirmed?.Invoke(this, new PickerSelectionEventArgs<TEntry, TValue>(entry));
         Changed?.Invoke(this, EventArgs.Empty);
      }

      // ------------------------------------------------------------
      /// <summary>
      /// 현재 entry를 확정 선택한다.
      /// </summary>
      // ------------------------------------------------------------
      public void ConfirmCurrent()
      {
         Confirm(CurrentEntry);
      }

      // ------------------------------------------------------------
      /// <summary>
      /// 현재 표시 목록 기준으로 이전 entry를 선택한다.
      /// </summary>
      // ------------------------------------------------------------
      public void MoveSelectionPrev()
      {
         if (pageEntries.Count == 0)
         {
            SetCurrentEntry(null);
            return;
         }

         var index = pageEntries.IndexOf(CurrentEntry);
         if (index > 0)
         {
            SetCurrentEntry(pageEntries[index - 1]);
            return;
         }

         if (index == 0 && paginator.CanMovePrev)
         {
            MovePrev();
            return;
         }

         if (index < 0)
         {
            SetCurrentEntry(pageEntries[pageEntries.Count - 1]);
         }
      }

      // ------------------------------------------------------------
      /// <summary>
      /// 현재 표시 목록 기준으로 다음 entry를 선택한다.
      /// </summary>
      // ------------------------------------------------------------
      public void MoveSelectionNext()
      {
         if (pageEntries.Count == 0)
         {
            SetCurrentEntry(null);
            return;
         }

         var index = pageEntries.IndexOf(CurrentEntry);
         if (0 <= index && index < pageEntries.Count - 1)
         {
            SetCurrentEntry(pageEntries[index + 1]);
            return;
         }

         if (index == pageEntries.Count - 1 && paginator.CanMoveNext)
         {
            MoveNext();
            return;
         }

         if (index < 0)
         {
            SetCurrentEntry(pageEntries[0]);
         }
      }

   #endregion

   #region 페이지

      // ------------------------------------------------------------
      /// <summary>
      /// 첫 페이지로 이동한다.
      /// </summary>
      // ------------------------------------------------------------
      public void MoveFirst()
      {
         var previousPageIndex = paginator.PageIndex;
         paginator.MoveFirst();
         Refresh();
         if (paginator.PageIndex != previousPageIndex)
         {
            SelectFirstPageEntry();
         }

         Changed?.Invoke(this, EventArgs.Empty);
      }

      // ------------------------------------------------------------
      /// <summary>
      /// 이전 페이지로 이동한다.
      /// </summary>
      // ------------------------------------------------------------
      public void MovePrev()
      {
         var previousPageIndex = paginator.PageIndex;
         paginator.MovePrev();
         Refresh();
         if (paginator.PageIndex != previousPageIndex)
         {
            SelectLastPageEntry();
         }

         Changed?.Invoke(this, EventArgs.Empty);
      }

      // ------------------------------------------------------------
      /// <summary>
      /// 다음 페이지로 이동한다.
      /// </summary>
      // ------------------------------------------------------------
      public void MoveNext()
      {
         var previousPageIndex = paginator.PageIndex;
         paginator.MoveNext();
         Refresh();
         if (paginator.PageIndex != previousPageIndex)
         {
            SelectFirstPageEntry();
         }

         Changed?.Invoke(this, EventArgs.Empty);
      }

      // ------------------------------------------------------------
      /// <summary>
      /// 마지막 페이지로 이동한다.
      /// </summary>
      // ------------------------------------------------------------
      public void MoveLast()
      {
         var previousPageIndex = paginator.PageIndex;
         paginator.MoveLast();
         Refresh();
         if (paginator.PageIndex != previousPageIndex)
         {
            SelectLastPageEntry();
         }

         Changed?.Invoke(this, EventArgs.Empty);
      }

      // ------------------------------------------------------------
      /// <summary>
      /// 지정한 페이지로 이동한다.
      /// </summary>
      // ------------------------------------------------------------
      public void SetPage(int pageIndex)
      {
         paginator.MoveTo(pageIndex);
         RefreshAndNotify();
      }

   #endregion

   #region 내부 처리

      // ------------------------------------------------------------
      /// <summary>
      /// 표시 목록을 다시 계산하고 변경 이벤트를 알린다.
      /// </summary>
      // ------------------------------------------------------------
      private void RefreshAndNotify()
      {
         Refresh();
         Changed?.Invoke(this, EventArgs.Empty);
      }

      // ------------------------------------------------------------
      /// <summary>
      /// 검색, 필터, 정렬, 페이지 범위를 적용해 표시 entry를 갱신한다.
      /// </summary>
      // ------------------------------------------------------------
      private void Refresh()
      {
         filteredEntries.Clear();
         foreach (var entry in allEntries)
         {
            if (!MatchesSearch(entry)) continue;
            if (!MatchesFilters(entry)) continue;

            filteredEntries.Add(entry);
         }

         if (IsSortableColumn(sortColumnID))
         {
            filteredEntries.Sort(CompareByColumn);
         }

         paginator.TotalCount = filteredEntries.Count;
         RefreshPageEntries();
      }

      // ------------------------------------------------------------
      /// <summary>
      /// 현재 page range에 해당하는 entry를 재사용 buffer에 채운다.
      /// </summary>
      // ------------------------------------------------------------
      private void RefreshPageEntries()
      {
         pageEntries.Clear();

         var range = paginator.Range;
         if (range.IsEmpty) return;

         var endIndex = Math.Min(range.BeginIndex + range.Count, filteredEntries.Count);
         for (var i = range.BeginIndex; i < endIndex; i++)
         {
            pageEntries.Add(filteredEntries[i]);
         }
      }

      // ------------------------------------------------------------
      /// <summary>
      /// 현재 페이지 첫 entry를 현재 선택으로 설정한다.
      /// </summary>
      // ------------------------------------------------------------
      private void SelectFirstPageEntry()
      {
         CurrentEntry = pageEntries.Count > 0 ? pageEntries[0] : null;
      }

      // ------------------------------------------------------------
      /// <summary>
      /// 현재 페이지 마지막 entry를 현재 선택으로 설정한다.
      /// </summary>
      // ------------------------------------------------------------
      private void SelectLastPageEntry()
      {
         CurrentEntry = pageEntries.Count > 0 ? pageEntries[pageEntries.Count - 1] : null;
      }

      // ------------------------------------------------------------
      /// <summary>
      /// 현재 선택값이 있는 페이지로 이동한다.
      /// </summary>
      // ------------------------------------------------------------
      private void MoveToCurrentEntryPage()
      {
         if (CurrentEntry == null) return;

         var index = filteredEntries.IndexOf(CurrentEntry);
         if (index < 0) return;

         paginator.MoveTo(index / paginator.PerPage);
         Refresh();
      }

      // ------------------------------------------------------------
      /// <summary>
      /// 검색어와 entry 검색 문자열을 비교한다.
      /// </summary>
      // ------------------------------------------------------------
      private bool MatchesSearch(PickerEntry<TEntry, TValue> entry)
      {
         if (string.IsNullOrWhiteSpace(searchText)) return true;

         return entry.SearchText.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0;
      }

      // ------------------------------------------------------------
      /// <summary>
      /// 활성화된 모든 filter를 통과하는지 확인한다.
      /// </summary>
      // ------------------------------------------------------------
      private bool MatchesFilters(PickerEntry<TEntry, TValue> entry)
      {
         foreach (var filter in spec.Filters)
         {
            if (!filterStates.TryGetValue(filter.ID, out var enabled) || !enabled) continue;

            if (!filter.IsMatch(entry))
            {
               return false;
            }
         }

         return true;
      }

      // ------------------------------------------------------------
      /// <summary>
      /// column raw value와 표시 문자열로 기본 정렬을 수행한다.
      /// </summary>
      // ------------------------------------------------------------
      private int CompareByColumn(PickerEntry<TEntry, TValue> left, PickerEntry<TEntry, TValue> right)
      {
         var leftColumn = left.GetColumn(sortColumnID);
         var rightColumn = right.GetColumn(sortColumnID);
         var result = CompareDefault
         (
            leftColumn.Value,
            rightColumn.Value,
            leftColumn.DisplayText,
            rightColumn.DisplayText
         );

         return sortAscending ? result : -result;
      }

      // ------------------------------------------------------------
      /// <summary>
      /// column이 정렬 가능한지 확인한다.
      /// </summary>
      // ------------------------------------------------------------
      private bool IsSortableColumn(string columnID)
      {
         foreach (var column in spec.Columns)
         {
            if (column.ID == columnID && column.Sortable)
            {
               return true;
            }
         }

         return false;
      }

      // ------------------------------------------------------------
      /// <summary>
      /// 원본 entry 목록을 session 내부 표시 entry로 변환한다.
      /// </summary>
      // ------------------------------------------------------------
      private List<PickerEntry<TEntry, TValue>> CreateEntries(IReadOnlyList<TEntry> entries)
      {
         if (entries == null) return new List<PickerEntry<TEntry, TValue>>();

         var result = new List<PickerEntry<TEntry, TValue>>(entries.Count);
         for (var i = 0; i < entries.Count; i++)
         {
            result.Add(spec.CreateEntry(entries[i]));
         }

         return result;
      }

      // ------------------------------------------------------------
      /// <summary>
      /// 현재 선택값과 같은 값을 가진 entry를 찾는다.
      /// </summary>
      // ------------------------------------------------------------
      private PickerEntry<TEntry, TValue> FindEntryByValue(TValue value)
      {
         foreach (var entry in allEntries)
         {
            if (EqualityComparer<TValue>.Default.Equals(entry.Value, value))
            {
               return entry;
            }
         }

         return null;
      }

      // ------------------------------------------------------------
      /// <summary>
      /// 같은 타입의 comparable 값은 raw value로, 그 외에는 표시 문자열로 비교한다.
      /// </summary>
      // ------------------------------------------------------------
      private static int CompareDefault
      (
         object leftValue,
         object rightValue,
         string leftDisplayText,
         string rightDisplayText
      )
      {
         if (ReferenceEquals(leftValue, rightValue)) return 0;
         if (leftValue == null) return -1;
         if (rightValue == null) return 1;

         if (leftValue.GetType() == rightValue.GetType() && leftValue is IComparable comparable)
         {
            return comparable.CompareTo(rightValue);
         }

         return StringComparer.Ordinal.Compare(leftDisplayText ?? string.Empty, rightDisplayText ?? string.Empty);
      }

   #endregion

   }
}
