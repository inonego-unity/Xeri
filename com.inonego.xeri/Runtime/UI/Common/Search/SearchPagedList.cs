/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : SearchPagedList.cs
수정일 : 2026-09-29

# 설명
검색 문자열, 추가 filter, 정렬, Paginator를 한 흐름으로 묶는 공통 목록 상태 모델.
Picker와 Collection Editor가 동일한 search → filter → sort → page slice 구현을 재사용한다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

using inonego;
using inonego.Xeri;
using inonego.Xeri.Utility;

namespace inonego.Xeri.UI
{
    // ============================================================
    /// <summary>
    /// Search와 pagination을 결합한 공통 목록 상태 모델.
    /// </summary>
    // ============================================================
    internal sealed class SearchPagedList<T>
    {

    #region 상태

        private readonly List<T> filteredItems = new();
        private readonly List<T> pageItems = new();
        private readonly Paginator paginator;
        private string searchText = string.Empty;

        private readonly IReadOnlyList<T> source;
        private readonly Func<T, string> searchTextSelector;
        private readonly Func<T, bool> filter;
        private readonly Func<Comparison<T>> comparisonProvider;

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 페이지에 표시할 항목을 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public IReadOnlyList<T> PageItems => pageItems;

        // ------------------------------------------------------------
        /// <summary>
        /// UI 바인딩에 사용할 현재 페이지 목록을 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public IList PageItemsSource => pageItems;

        // ------------------------------------------------------------
        /// <summary>
        /// 페이지 이동 상태를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public IReadOnlyPaginator Paginator => paginator;

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 적용된 검색어를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public string SearchText => searchText;

        // ------------------------------------------------------------
        /// <summary>
        /// 검색과 필터를 통과한 항목 수를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public int FilteredCount => filteredItems.Count;

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// source와 검색/필터/정렬 규칙을 사용하는 목록 상태를 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        public SearchPagedList
        (
            IReadOnlyList<T> source,
            Func<T, string> searchTextSelector,
            int pageSize = 20,
            Func<T, bool> filter = null,
            Func<Comparison<T>> comparisonProvider = null
        )
        {
            this.source = source
                ?? throw new ArgumentNullException(nameof(source));
            this.searchTextSelector = searchTextSelector
                ?? throw new ArgumentNullException(nameof(searchTextSelector));
            this.filter = filter;
            this.comparisonProvider = comparisonProvider;

            paginator = new Paginator(pageSize);
        }

    #endregion

    #region 검색과 새로고침

        // ------------------------------------------------------------
        /// <summary>
        /// 검색어를 변경하고 첫 페이지부터 목록을 다시 계산한다.
        /// </summary>
        // ------------------------------------------------------------
        public void SetSearchText(string value)
        {
            searchText = value ?? string.Empty;
            Refresh(resetPage: true);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 source에서 검색/필터/정렬과 page slice를 다시 계산한다.
        /// </summary>
        // ------------------------------------------------------------
        public void Refresh(bool resetPage = false)
        {
            if (resetPage)
            {
                paginator.MoveFirst();
            }

            filteredItems.Clear();

            foreach (var item in source)
            {
                var indexedText = searchTextSelector(item);
                var matchesSearch = SearchTextUtility.Matches
                (
                    indexedText,
                    searchText
                );
                if (!matchesSearch)
                {
                    continue;
                }

                if (filter != null && !filter(item))
                {
                    continue;
                }

                filteredItems.Add(item);
            }

            var comparison = comparisonProvider?.Invoke();
            if (comparison != null)
            {
                filteredItems.Sort(comparison);
            }

            paginator.TotalCount = filteredItems.Count;
            RefreshPageItems();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 page에서 item의 index를 반환한다. 없으면 -1.
        /// </summary>
        // ------------------------------------------------------------
        public int IndexOfPage(T item)
        {
            return pageItems.IndexOf(item);
        }

    #endregion

    #region 페이지 이동

        // ------------------------------------------------------------
        /// <summary>
        /// 첫 페이지로 이동하고 page slice를 갱신한다.
        /// </summary>
        // ------------------------------------------------------------
        public bool MoveFirst()
        {
            return MovePage(paginator.MoveFirst);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 이전 페이지로 이동하고 page slice를 갱신한다.
        /// </summary>
        // ------------------------------------------------------------
        public bool MovePrev()
        {
            return MovePage(paginator.MovePrev);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 다음 페이지로 이동하고 page slice를 갱신한다.
        /// </summary>
        // ------------------------------------------------------------
        public bool MoveNext()
        {
            return MovePage(paginator.MoveNext);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 마지막 페이지로 이동하고 page slice를 갱신한다.
        /// </summary>
        // ------------------------------------------------------------
        public bool MoveLast()
        {
            return MovePage(paginator.MoveLast);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 지정 page index로 이동하고 page slice를 갱신한다.
        /// </summary>
        // ------------------------------------------------------------
        public bool MoveTo(int pageIndex)
        {
            var previousPageIndex = paginator.PageIndex;
            paginator.MoveTo(pageIndex);
            RefreshPageItems();

            return paginator.PageIndex != previousPageIndex;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// filtered 목록의 item이 속한 페이지로 이동한다.
        /// </summary>
        // ------------------------------------------------------------
        public bool MoveToItem(T item)
        {
            var index = filteredItems.IndexOf(item);
            if (index < 0)
            {
                return false;
            }

            return MoveTo(index / paginator.PerPage);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// paginator 이동 command를 실행하고 page slice를 갱신한다.
        /// </summary>
        // ------------------------------------------------------------
        private bool MovePage(Action move)
        {
            var previousPageIndex = paginator.PageIndex;
            move();
            RefreshPageItems();

            return paginator.PageIndex != previousPageIndex;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 paginator range에 해당하는 item을 page buffer에 채운다.
        /// </summary>
        // ------------------------------------------------------------
        private void RefreshPageItems()
        {
            pageItems.Clear();

            var range = paginator.Range;
            for (var index = range.BeginIndex; index < range.EndIndex; index++)
            {
                pageItems.Add(filteredItems[index]);
            }
        }

    #endregion

    }
}
