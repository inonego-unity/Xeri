/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TableGridRowListSource.cs
수정일 : 2026-11-22

# 설명
이미 구성한 TableGridRow 스냅샷을 동기 행 source로 제공한다.

# 특이사항, 제약사항
그룹 헤더 등 특수 행을 포함할 수 있으며 원본 배열은 생성 시 방어적으로 복사한다.
같은 ID의 행이 있으면 ID 조회는 앞의 행을 쓴다.
검색은 데이터 행 항목의 열 글자(GetText)를 이은 글자로 SearchTextUtility 규칙에 따라 거르며,
그룹 머리는 아래에 남는 데이터 행이 있을 때만 남긴다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

namespace inonego.Xeri.UI.TableGrid
{
    // ============================================================
    /// <summary>
    /// 미리 계산한 행 스냅샷을 그대로 조회하는 공급자다.
    /// </summary>
    // ============================================================
    public sealed class TableGridRowListSource : ITableGridSource, ITableGridSearchableSource
    {

    #region 저장된 행

        private readonly IReadOnlyList<TableGridRow> allRows;
        private readonly Dictionary<string, int> allIndexByRowID = new(StringComparer.Ordinal);
        private readonly int[] positionByAllIndex;
        private readonly List<TableGridRow> matchedRows = new();
        private IReadOnlyList<TableGridRow> rows;

    #endregion

    #region 구성과 조회

        // ------------------------------------------------------------
        /// <summary>
        /// 입력 목록을 복사하여 변경 불가능한 로컬 공급자를 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        public TableGridRowListSource(IReadOnlyList<TableGridRow> rows, bool canReorder = false)
        {
            if (rows == null)
            {
                throw new ArgumentNullException(nameof(rows));
            }

            TableGridRow[] copy = new TableGridRow[rows.Count];
            positionByAllIndex = new int[rows.Count];
            for (int index = 0; index < rows.Count; index++)
            {
                copy[index] = rows[index] ?? throw new ArgumentException("행 목록에 null이 들 수 없습니다.", nameof(rows));
                allIndexByRowID.TryAdd(copy[index].ID, index);
                positionByAllIndex[index] = index;
            }

            allRows = Array.AsReadOnly(copy);
            this.rows = allRows;
            Capabilities = new TableGridSourceCapabilities(canReorder);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// source가 지원하는 기능을 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public TableGridSourceCapabilities Capabilities { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 검색 결과 행 수.
        /// </summary>
        // ------------------------------------------------------------
        public int Count => rows.Count;

        // ------------------------------------------------------------
        /// <summary>
        /// 검색 결과 순서 index번째 행을 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public TableGridRow GetRow(int index)
        {
            return rows[index];
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 행 ID의 검색 결과 안 위치를 반환한다. 없으면 -1이다.
        /// </summary>
        // ------------------------------------------------------------
        public int IndexOf(string rowID)
        {
            return rowID != null && allIndexByRowID.TryGetValue(rowID, out var index) ? positionByAllIndex[index] : -1;
        }

        // --------------------------------------------------------------------------------
        /// <summary>
        /// <br/> 검색 글자로 데이터 행을 거른다. 그룹 머리는 아래에 남는 데이터 행이 있을 때만 남기고,
        /// <br/> 요약 행은 검색 중에 뺀다. 공백뿐이면 모두 보인다.
        /// </summary>
        // --------------------------------------------------------------------------------
        public void SetSearch(string text, IReadOnlyList<TableGridColumn> columns)
        {
            if (!TableGridSearch.IsActive(text, columns))
            {
                rows = allRows;
                for (var index = 0; index < positionByAllIndex.Length; index++)
                {
                    positionByAllIndex[index] = index;
                }

                return;
            }

            Array.Fill(positionByAllIndex, -1);
            matchedRows.Clear();
            var pendingHeader = -1;
            for (var index = 0; index < allRows.Count; index++)
            {
                var row = allRows[index];
                if (row.Kind == TableGridRowKind.GroupHeader)
                {
                    pendingHeader = index;
                    continue;
                }

                if (!row.IsData || !TableGridSearch.Matches(row.Item, text, columns))
                {
                    continue;
                }

                if (pendingHeader >= 0)
                {
                    positionByAllIndex[pendingHeader] = matchedRows.Count;
                    matchedRows.Add(allRows[pendingHeader]);
                    pendingHeader = -1;
                }

                positionByAllIndex[index] = matchedRows.Count;
                matchedRows.Add(row);
            }

            rows = matchedRows;
        }

    #endregion

    }
}
