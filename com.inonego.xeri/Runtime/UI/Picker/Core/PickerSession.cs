/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : PickerSession.cs
수정일 : 2026-10-28

# 설명
Picker 실행 단위의 filter, sort, selection 의미를 관리한다.
검색과 page slice 상태는 공통 SearchPagedList<T>에 위임한다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

using inonego;
using inonego.Xeri;
using inonego.Xeri.UI;
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
        private readonly IReadOnlyList<PickerEntry<TEntry, TValue>> allEntries;
        private readonly SearchPagedList<PickerEntry<TEntry, TValue>> searchPagedEntries;
        private readonly PageNavigationController<PickerEntry<TEntry, TValue>> navigation;
        private readonly Dictionary<string, bool> filterStates = new();
        private string sortColumnID = string.Empty;
        private bool sortAscending = true;

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 페이지에 표시할 entry 목록.
        /// </summary>
        // ------------------------------------------------------------
        public IReadOnlyList<PickerEntry<TEntry, TValue>> PageEntries
            => searchPagedEntries.PageItems;

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
        public int FilteredCount => searchPagedEntries.FilteredCount;

        // ------------------------------------------------------------
        /// <summary>
        /// 읽기 전용 paginator 상태.
        /// </summary>
        // ------------------------------------------------------------
        public IReadOnlyPaginator Paginator => searchPagedEntries.Paginator;

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 페이지 index.
        /// </summary>
        // ------------------------------------------------------------
        public int PageIndex => Paginator.PageIndex;

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 페이지 표시 번호.
        /// </summary>
        // ------------------------------------------------------------
        public int PageNumber => Paginator.PageNumber;

        // ------------------------------------------------------------
        /// <summary>
        /// 전체 페이지 개수.
        /// </summary>
        // ------------------------------------------------------------
        public int PageCount => Paginator.PageCount;

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
        /// 명시적인 페이지 이동이 목록 상태에 반영된 후 발생한다.
        /// </summary>
        // ------------------------------------------------------------
        internal event Action<PageNavigationResult> PageNavigated = null;

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
            this.spec = spec ?? throw new ArgumentNullException(nameof(spec));
            this.onSelected = onSelected ?? (_ => { });
            allEntries = spec.CreateEntries(entries);
            searchPagedEntries = new SearchPagedList<PickerEntry<TEntry, TValue>>
            (
                allEntries,
                entry => entry.SearchText,
                pageSize,
                MatchesFilters,
                GetComparison
            );
            navigation = new PageNavigationController<PickerEntry<TEntry, TValue>>(searchPagedEntries);

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
            searchPagedEntries.SetSearchText(value);
            Changed?.Invoke(this, EventArgs.Empty);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// filter 활성 상태를 설정한다.
        /// </summary>
        // ------------------------------------------------------------
        public void SetFilterEnabled(string filterID, bool enabled)
        {
            filterStates[filterID ?? string.Empty] = enabled;
            searchPagedEntries.Refresh(resetPage: true);
            Changed?.Invoke(this, EventArgs.Empty);
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
            searchPagedEntries.Refresh(resetPage: true);
            Changed?.Invoke(this, EventArgs.Empty);
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
            // ListView가 현재 선택을 다시 반영해도 상태가 같으면 View 갱신을 재진입시키지 않는다.
            if (ReferenceEquals(CurrentEntry, entry)) return;

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
            SetCurrentEntry(null);
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
            if (PageEntries.Count == 0)
            {
                SetCurrentEntry(null);
                return;
            }

            var index = searchPagedEntries.IndexOfPage(CurrentEntry);
            if (index > 0)
            {
                SetCurrentEntry(PageEntries[index - 1]);
                return;
            }

            if (index == 0 && Paginator.CanMovePrev)
            {
                NavigatePage(new PageNavigationRequest(PageNavigationKind.Previous));
                return;
            }

            if (index < 0)
            {
                SetCurrentEntry(PageEntries[PageEntries.Count - 1]);
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 표시 목록 기준으로 다음 entry를 선택한다.
        /// </summary>
        // ------------------------------------------------------------
        public void MoveSelectionNext()
        {
            if (PageEntries.Count == 0)
            {
                SetCurrentEntry(null);
                return;
            }

            var index = searchPagedEntries.IndexOfPage(CurrentEntry);
            if (0 <= index && index < PageEntries.Count - 1)
            {
                SetCurrentEntry(PageEntries[index + 1]);
                return;
            }

            if (index == PageEntries.Count - 1 && Paginator.CanMoveNext)
            {
                NavigatePage(new PageNavigationRequest(PageNavigationKind.Next));
                return;
            }

            if (index < 0)
            {
                SetCurrentEntry(PageEntries[0]);
            }
        }

    #endregion

    #region 페이지

        // ------------------------------------------------------------
        /// <summary>
        /// 페이지 이동을 실행하고 선택 항목 및 표시 변경을 통지한다.
        /// </summary>
        // ------------------------------------------------------------
        public PageNavigationResult NavigatePage(PageNavigationRequest request)
        {
            var result = navigation.Navigate(request);
            if (!result.ShouldApply) return result;

            CurrentEntry = result.SelectedIndex >= 0
                ? PageEntries[result.SelectedIndex]
                : null;

            Changed?.Invoke(this, EventArgs.Empty);
            PageNavigated?.Invoke(result);

            return result;
        }

    #endregion

    #region 내부 처리

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 검색/필터/정렬 규칙으로 공통 search/page state를 갱신한다.
        /// </summary>
        // ------------------------------------------------------------
        private void Refresh()
        {
            searchPagedEntries.Refresh();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 선택값이 있는 페이지로 이동한다.
        /// </summary>
        // ------------------------------------------------------------
        private void MoveToCurrentEntryPage()
        {
            if (CurrentEntry == null) return;

            searchPagedEntries.MoveToItem(CurrentEntry);
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
        /// 현재 sort 상태에 사용할 comparer를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        private Comparison<PickerEntry<TEntry, TValue>> GetComparison()
        {
            return IsSortableColumn(sortColumnID)
                ? CompareByColumn
                : null;
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
                if (column.ID == columnID && column.Options.Sortable)
                {
                    return true;
                }
            }

            return false;
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

        // ----------------------------------------------------------------------
        /// <summary>
        /// 같은 타입의 comparable 값은 raw value로, 그 외에는 표시 문자열로 비교한다.
        /// </summary>
        // ----------------------------------------------------------------------
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
