/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TableGridSource.cs
수정일 : 2026-11-22

# 설명
표가 행을 묻는 동기 소스 계약과 로컬 목록 파이프라인(검색 → 필터 → 정렬) source를 정의한다.
표는 행 수, 위치의 행, ID의 위치를 언제든 바로 묻고 소스는 바로 답한다.

# 특이사항, 제약사항
조회는 입력 데이터를 변경하지 않는다.
외부 데이터(SQL·DB·파일 등)는 호스트가 가져와 목록 소스로 넘긴다.
검색 일치는 SearchTextUtility 규칙(공백뿐이면 검색 안 함, 대소문자 무시 포함)을 따르고,
검색 글자는 고른 열의 GetText를 공백으로 이어 만든다(Picker·Collection 검색 글자와 같은 방식).
TableGridListSource는 항목별 행·검색 글자·ID 색인을 캐시하며, 항목이나 항목 수가 바뀌면 Invalidate로 버린다(캐시 크기는 그 뒤 처음 읽을 때 정한다).
페이지는 source가 아니라 표(Paged 모드)가 나눈다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

using inonego;
using inonego.Xeri;
using inonego.Xeri.UI;

namespace inonego.Xeri.UI.TableGrid
{

    #region 공급자 기능

    // ============================================================
    /// <summary>
    /// 데이터 source가 지원하는 기능을 설명한다.
    /// </summary>
    // ============================================================
    public sealed class TableGridSourceCapabilities
    {

    #region 능력 값

        // ------------------------------------------------------------
        /// <summary>
        /// 행 순서를 바꿀 수 있는지로 source 능력 정보를 만든다.
        /// </summary>
        // ------------------------------------------------------------
        public TableGridSourceCapabilities(bool canReorder = false)
        {
            CanReorder = canReorder;
        }

        // --------------------------------------------------------------------------------
        /// <summary>
        /// 행 순서를 바꿀 수 있는 데이터인지 반환한다. 표의 행 이동 요청은 이 값이 true일 때만 연다.
        /// </summary>
        // --------------------------------------------------------------------------------
        public bool CanReorder { get; }

    #endregion

    }

    #endregion

    #region 조회 계약

    // ============================================================
    /// <summary>
    /// <br/> TableGrid에 행을 동기로 제공하는 source 계약이다.
    /// <br/> 위치는 검색·필터·정렬을 거친 결과 순서다.
    /// </summary>
    // ============================================================
    public interface ITableGridSource
    {
        // ------------------------------------------------------------
        /// <summary>
        /// source가 지원하는 기능을 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        TableGridSourceCapabilities Capabilities { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 결과 행 수.
        /// </summary>
        // ------------------------------------------------------------
        int Count { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 결과 순서 index번째 행을 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        TableGridRow GetRow(int index);

        // ------------------------------------------------------------
        /// <summary>
        /// 행 ID의 결과 안 위치를 반환한다. 없으면 -1이다.
        /// </summary>
        // ------------------------------------------------------------
        int IndexOf(string rowID);
    }

    #endregion

    #region 검색

    // ======================================================================
    /// <summary>
    /// 검색 글자로 행을 거를 수 있는 source. TableGrid.SearchText가 부른다.
    /// </summary>
    // ======================================================================
    public interface ITableGridSearchableSource
    {
        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> 검색 글자와 검색할 열을 지정한다. 공백뿐이면 거르지 않는다.
        /// <br/> 열의 GetText(값 규칙 글자)를 이은 글자에 검색 글자가 든 행만 남긴다.
        /// </summary>
        // ----------------------------------------------------------------------
        void SetSearch(string text, IReadOnlyList<TableGridColumn> columns);
    }

    // ======================================================================
    /// <summary>
    /// <br/> 행 항목의 검색 글자를 만들고 검색 글자와 맞는지 판정한다.
    /// <br/> 일치 규칙은 SearchTextUtility(대소문자 무시 포함)를 따른다.
    /// </summary>
    // ======================================================================
    internal static class TableGridSearch
    {
        // ----------------------------------------------------------------------
        /// <summary>
        /// 고른 열의 글자를 공백으로 이은 검색 글자가 검색 글자를 포함하면 true다.
        /// </summary>
        // ----------------------------------------------------------------------
        public static bool Matches(object item, string text, IReadOnlyList<TableGridColumn> columns)
        {
            return SearchTextUtility.Matches(BuildText(item, columns, new StringBuilder()), text);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 고른 열의 값 규칙 글자를 공백으로 이어 항목의 검색 글자를 만든다.
        /// </summary>
        // ------------------------------------------------------------
        public static string BuildText(object item, IReadOnlyList<TableGridColumn> columns, StringBuilder builder)
        {
            builder.Clear();
            foreach (var column in columns)
            {
                if (column != null)
                {
                    SearchTextUtility.Append(builder, column.GetText(item));
                }
            }

            return builder.ToString();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 검색 글자와 열이 있어 거를 수 있는지 확인한다.
        /// </summary>
        // ------------------------------------------------------------
        public static bool IsActive(string text, IReadOnlyList<TableGridColumn> columns)
        {
            return !string.IsNullOrWhiteSpace(text) && columns != null && columns.Count > 0;
        }
    }

    #endregion

    #region 목록 공급자

    // ======================================================================
    /// <summary>
    /// <br/> 로컬 목록을 검색 → 필터 → 정렬한 결과를 동기 행 source로 노출한다.
    /// <br/> 항목을 행 항목(TableGridRow.Item)에 그대로 담고, 셀 값은 열이 꺼낸다.
    /// <br/> 결과는 읽을 때 한 번 다시 계산하고, 페이지는 표(Paged 모드)가 나눈다.
    /// </summary>
    // ======================================================================
    public class TableGridListSource<T> : ITableGridSource, ITableGridSearchableSource
    {

    #region 내부 타입

        // ============================================================
        /// <summary>
        /// 같은 순위의 항목은 원본 위치 순서로 두는 안정 비교기.
        /// </summary>
        // ============================================================
        private sealed class StableComparer : IComparer<int>
        {
            private readonly TableGridListSource<T> owner;

            // ------------------------------------------------------------
            /// <summary>
            /// 비교할 항목과 정렬 규칙을 가진 source를 연결한다.
            /// </summary>
            // ------------------------------------------------------------
            public StableComparer(TableGridListSource<T> owner)
            {
                this.owner = owner;
            }

            // ------------------------------------------------------------
            /// <summary>
            /// 정렬 규칙으로 비교하고 같으면 원본 위치로 비교한다.
            /// </summary>
            // ------------------------------------------------------------
            public int Compare(int left, int right)
            {
                var result = owner.comparison(owner.items[left], owner.items[right]);

                return result != 0 ? result : left.CompareTo(right);
            }
        }

    #endregion

    #region 목록 및 접근자

        private readonly IReadOnlyList<T> items;
        private readonly Func<T, string> rowID;
        private readonly Func<T, string> searchTextSelector;
        private readonly List<int> order = new();
        private readonly List<T> results = new();
        private readonly ReadOnlyCollection<T> resultView;
        private readonly StringBuilder searchBuilder = new();
        private readonly StableComparer stableComparer;
        private readonly Dictionary<string, int> sourceIndexByRowID = new(StringComparer.Ordinal);

        private IReadOnlyList<TableGridColumn> searchColumns = Array.Empty<TableGridColumn>();
        private string[] searchTexts;
        private TableGridRow[] rowsBySource;
        private string[] rowIDsBySource;
        private int[] positionBySource;
        private bool rowIndexBuilt;
        private Func<T, bool> filter;
        private Comparison<T> comparison;
        private bool dirty = true;

    #endregion

    #region 생성

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> 목록과 행 ID 함수를 받아 쓰기 없는 source를 구성한다.
        /// <br/> searchText를 주면 검색 열 대신 이 함수의 글자로 검색한다.
        /// <br/> 열에 없는 값까지 검색할 때 쓴다.
        /// </summary>
        // ------------------------------------------------------------
        public TableGridListSource
        (
            IReadOnlyList<T> items,
            Func<T, string> rowID,
            bool canReorder = false,
            Func<T, string> searchText = null
        )
        {
            this.items = items ?? throw new ArgumentNullException(nameof(items));
            this.rowID = rowID ?? throw new ArgumentNullException(nameof(rowID));
            searchTextSelector = searchText;
            resultView = results.AsReadOnly();
            stableComparer = new StableComparer(this);
            Capabilities = new TableGridSourceCapabilities(canReorder);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// source가 지원하는 기능을 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public TableGridSourceCapabilities Capabilities { get; }

    #endregion

    #region 결과 읽기

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> 지금 적용된 검색 글자. 없으면 빈 글자다.
        /// <br/> 지정하면 검색 열은 그대로 두고 결과를 다시 만든다. null은 빈 글자다.
        /// </summary>
        // ----------------------------------------------------------------------
        public string SearchText
        {
            get => searchText;
            set
            {
                searchText = value ?? string.Empty;
                dirty = true;
            }
        }
        private string searchText = string.Empty;

        // ------------------------------------------------------------
        /// <summary>
        /// 검색·필터를 통과한 항목 수.
        /// </summary>
        // ------------------------------------------------------------
        public int Count => EnsureResults().Count;

        // ------------------------------------------------------------
        /// <summary>
        /// 결과 순서의 index번째 항목.
        /// </summary>
        // ------------------------------------------------------------
        public T this[int index] => EnsureResults()[index];

        // ------------------------------------------------------------
        /// <summary>
        /// 검색 → 필터 → 정렬 결과 항목의 읽기 전용 목록.
        /// </summary>
        // ------------------------------------------------------------
        public IReadOnlyList<T> Items
        {
            get
            {
                EnsureResults();
                return resultView;
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 결과 순서에서 항목의 위치를 반환한다. 없으면 -1이다.
        /// </summary>
        // ------------------------------------------------------------
        public int IndexOf(T item)
        {
            return EnsureResults().IndexOf(item);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> 결과 순서 index번째 행을 반환한다.
        /// <br/> 행은 항목마다 처음 물을 때 한 번 만들어 둔다.
        /// </summary>
        // ------------------------------------------------------------
        public TableGridRow GetRow(int index)
        {
            EnsureResults();

            return GetSourceRow(order[index]);
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> 행 ID의 결과 안 위치를 반환한다. 없거나 검색·필터로 빠졌으면 -1이다.
        /// <br/> ID 색인은 항목이 바뀔 때만 다시 만든다.
        /// </summary>
        // ----------------------------------------------------------------------
        int ITableGridSource.IndexOf(string id)
        {
            EnsureResults();
            if (id == null || !EnsureRowIndex().TryGetValue(id, out var sourceIndex))
            {
                return -1;
            }

            return positionBySource[sourceIndex];
        }

    #endregion

    #region 검색·필터·정렬 지정

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> 검색 글자와 검색할 열을 지정한다. 공백뿐이면 모두 보인다.
        /// <br/> 열 구성이 바뀌면 항목별 검색 글자 캐시를 버린다.
        /// <br/> 생성할 때 검색 글자 함수를 줬으면 열 대신 그 함수로 검색한다.
        /// </summary>
        // ------------------------------------------------------------
        public void SetSearch(string text, IReadOnlyList<TableGridColumn> columns)
        {
            var nextColumns = columns ?? Array.Empty<TableGridColumn>();
            if (!HasSameColumns(nextColumns))
            {
                searchColumns = CopyColumns(nextColumns);
                if (searchTextSelector == null)
                {
                    searchTexts = null;
                }
            }

            SearchText = text;
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// 검색을 통과한 항목에 더 적용할 조건을 지정한다. null이면 거르지 않는다.
        /// </summary>
        // ----------------------------------------------------------------------
        public void SetFilter(Func<T, bool> predicate)
        {
            filter = predicate;
            dirty = true;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> 결과 정렬 규칙을 지정한다. null이면 원본 순서다.
        /// <br/> 같은 순위의 항목은 원본 순서를 지킨다(안정 정렬).
        /// </summary>
        // ------------------------------------------------------------
        public void SetSort(Comparison<T> sortComparison)
        {
            comparison = sortComparison;
            dirty = true;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> 열의 형식 있는 값 비교(값 규칙 Compare)와 방향으로 정렬한다.
        /// <br/> 같은 값의 항목은 원본 순서를 지킨다(안정 정렬).
        /// </summary>
        // ------------------------------------------------------------
        public void SetSort(TableGridColumn column, TableGridSortDirection direction)
        {
            if (column == null)
            {
                throw new ArgumentNullException(nameof(column));
            }

            // 내림차순은 비교 순서를 뒤집어 값이 없는 항목의 위치도 함께 뒤집는다.
            if (direction == TableGridSortDirection.Descending)
            {
                SetSort((left, right) => column.CompareItems(right, left));
                return;
            }

            SetSort((left, right) => column.CompareItems(left, right));
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> 검색 글자·행·ID 캐시는 두고 검색 → 필터 → 정렬을 다시 적용한다.
        /// <br/> 필터·정렬 함수가 읽는 상태가 바뀌었을 때 부른다.
        /// </summary>
        // ----------------------------------------------------------------------
        public void Refresh()
        {
            dirty = true;
        }

        // --------------------------------------------------------------------------------
        /// <summary>
        /// <br/> 항목이나 그 글자·ID가 바뀌었을 때 항목별 캐시(검색 글자, 행, ID 색인)를 버리고
        /// <br/> 다시 적용한다. 항목 수가 바뀌었을 때도 반드시 부른다(캐시는 이때만 새 크기로 만든다).
        /// </summary>
        // --------------------------------------------------------------------------------
        public void Invalidate()
        {
            searchTexts = null;
            rowsBySource = null;
            rowIDsBySource = null;
            rowIndexBuilt = false;
            dirty = true;
        }

    #endregion

    #region 파이프라인

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> 바뀐 조건이 있으면 검색 → 필터 → 정렬 결과를 다시 만든다.
        /// <br/> 원본 위치 → 결과 위치 배열은 재사용해 다시 채운다.
        /// </summary>
        // ------------------------------------------------------------
        private List<T> EnsureResults()
        {
            if (!dirty)
            {
                return results;
            }

            // 계산 중 다시 읽혀도 되풀이하지 않도록 먼저 내리되, 호스트 함수가 예외를 던지면 이전 결과를 새 결과로 믿지 않게 되돌린다.
            dirty = false;
            try
            {
                order.Clear();

                // 검색과 필터를 통과한 원본 위치만 모은다. 검색 글자는 검색 중일 때만 만든다.
                var searching = IsSearching();
                for (var index = 0; index < items.Count; index++)
                {
                    if (searching && !SearchTextUtility.Matches(GetSearchText(index), SearchText))
                    {
                        continue;
                    }

                    if (filter != null && !filter(items[index]))
                    {
                        continue;
                    }

                    order.Add(index);
                }

                if (comparison != null)
                {
                    order.Sort(stableComparer);
                }
            }
            catch
            {
                dirty = true;
                throw;
            }

            results.Clear();
            if (positionBySource == null || positionBySource.Length != items.Count)
            {
                positionBySource = new int[items.Count];
            }

            Array.Fill(positionBySource, -1);
            for (var position = 0; position < order.Count; position++)
            {
                var index = order[position];
                results.Add(items[index]);
                positionBySource[index] = position;
            }

            return results;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 원본 위치의 행을 캐시에서 읽고, 없으면 만들어 둔다.
        /// </summary>
        // ------------------------------------------------------------
        private TableGridRow GetSourceRow(int sourceIndex)
        {
            rowsBySource ??= new TableGridRow[items.Count];
            return rowsBySource[sourceIndex] ??= new TableGridRow(GetSourceID(sourceIndex), items[sourceIndex]);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 원본 위치의 행 ID를 캐시에서 읽고, 없으면 ID 함수로 한 번 만든다.
        /// </summary>
        // ------------------------------------------------------------
        private string GetSourceID(int sourceIndex)
        {
            rowIDsBySource ??= new string[items.Count];
            return rowIDsBySource[sourceIndex] ??= rowID(items[sourceIndex]);
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> 행 ID → 원본 위치 색인을 만든다. 항목이 바뀌지 않았으면 그대로 쓴다.
        /// <br/> ID만 모으므로 행 객체는 만들지 않는다.
        /// </summary>
        // ----------------------------------------------------------------------
        private Dictionary<string, int> EnsureRowIndex()
        {
            if (rowIndexBuilt)
            {
                return sourceIndexByRowID;
            }

            sourceIndexByRowID.Clear();
            for (var index = 0; index < items.Count; index++)
            {
                // 같은 ID가 있으면 앞의 항목을 쓴다.
                sourceIndexByRowID.TryAdd(GetSourceID(index), index);
            }

            rowIndexBuilt = true;

            return sourceIndexByRowID;
        }

        private bool IsSearching()
        {
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                return false;
            }

            return searchTextSelector != null || TableGridSearch.IsActive(SearchText, searchColumns);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 원본 위치의 검색 글자를 캐시에서 읽고, 없으면 만들어 둔다.
        /// </summary>
        // ------------------------------------------------------------
        private string GetSearchText(int index)
        {
            searchTexts ??= new string[items.Count];
            var text = searchTexts[index];
            if (text != null)
            {
                return text;
            }

            var item = items[index];
            text = searchTextSelector != null
                ? searchTextSelector(item) ?? string.Empty
                : TableGridSearch.BuildText(item, searchColumns, searchBuilder);
            searchTexts[index] = text;

            return text;
        }

        private bool HasSameColumns(IReadOnlyList<TableGridColumn> columns)
        {
            if (columns.Count != searchColumns.Count)
            {
                return false;
            }

            for (var index = 0; index < columns.Count; index++)
            {
                if (!ReferenceEquals(columns[index], searchColumns[index]))
                {
                    return false;
                }
            }

            return true;
        }

        private static TableGridColumn[] CopyColumns(IReadOnlyList<TableGridColumn> columns)
        {
            var copy = new TableGridColumn[columns.Count];
            for (var index = 0; index < columns.Count; index++)
            {
                copy[index] = columns[index];
            }

            return copy;
        }

    #endregion

    }

    #endregion

}
