/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TableGridQuery.cs
수정일 : 2026-11-23

# 설명
도메인 독립적인 TableGrid 필터 규칙과 정렬·그룹 쿼리 스냅샷을 정의한다.

# 특이사항, 제약사항
UI 상태만 표현하며 실제 쿼리 평가와 데이터 조회는 호스트가 담당한다.
쿼리 상태는 불변이므로 열 하나의 정렬·필터·그룹만 바꾸는 With* 메서드는 항상 새 상태를 반환한다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace inonego.Xeri.UI.TableGrid
{

    #region 필터 연산자

    // ============================================================
    /// <summary>
    /// 데이터 소스가 해석할 수 있는 범용 비교 연산자다.
    /// </summary>
    // ============================================================
    public enum TableGridFilterOperator
    {
        Contains,
        Equals,
        NotEquals,
        StartsWith,
        GreaterThan,
        GreaterThanOrEqual,
        LessThan,
        LessThanOrEqual
    }

    // ============================================================
    /// <summary>
    /// 필터 편집 UI의 기본 입력 종류를 지정한다.
    /// </summary>
    // ============================================================
    public enum TableGridColumnValueKind
    {
        Text,
        Number,
        Choice
    }

    #endregion

    #region 필터 규칙

    // ============================================================
    /// <summary>
    /// 안정적 ID를 가진 단일 필터 조건의 불변 스냅샷이다.
    /// </summary>
    // ============================================================
    public sealed class TableGridFilterRule
    {

        // ------------------------------------------------------------
        /// <summary>
        /// 필터 규칙의 식별자와 열·연산자·비교 값을 설정한다.
        /// </summary>
        // ------------------------------------------------------------
        public TableGridFilterRule(string id, string columnID, TableGridFilterOperator filterOperator, string value)
            : this(id, columnID, filterOperator, value, TableGridColumnValueKind.Text, null, null, null)
        {
            // NONE
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 숫자 범위를 값 두 개로 표현하되 비교 자체는 소스에 위임한다.
        /// </summary>
        // ------------------------------------------------------------
        public static TableGridFilterRule NumberRange(string id, string columnID, string minimum, string maximum)
        {
            return new TableGridFilterRule
            (
                id, columnID, TableGridFilterOperator.Equals, string.Empty,
                TableGridColumnValueKind.Number, minimum, maximum, null
            );
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 선택형 열의 허용 값을 중복 제거한 불변 목록으로 표현한다.
        /// </summary>
        // ------------------------------------------------------------
        public static TableGridFilterRule ChoiceSet(string id, string columnID, IReadOnlyList<string> choices)
        {
            if (choices == null)
            {
                throw new ArgumentNullException(nameof(choices));
            }

            return new TableGridFilterRule
            (
                id, columnID, TableGridFilterOperator.Equals, string.Empty,
                TableGridColumnValueKind.Choice, null, null, choices
            );
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 텍스트, 범위, 선택 집합을 같은 불변 쿼리 계약으로 구성한다.
        /// </summary>
        // ------------------------------------------------------------
        private TableGridFilterRule
        (
            string id,
            string columnID,
            TableGridFilterOperator filterOperator,
            string value,
            TableGridColumnValueKind valueKind,
            string minimum,
            string maximum,
            IReadOnlyList<string> choices
        )
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException("필터 규칙 ID가 필요합니다.", nameof(id));
            }

            if (string.IsNullOrWhiteSpace(columnID))
            {
                throw new ArgumentException("열 ID가 필요합니다.", nameof(columnID));
            }

            if (!Enum.IsDefined(typeof(TableGridFilterOperator), filterOperator))
            {
                throw new ArgumentOutOfRangeException(nameof(filterOperator));
            }

            ID = id;
            ColumnID = columnID;
            Operator = filterOperator;
            Value = value ?? string.Empty;
            ValueKind = valueKind;
            Minimum = minimum ?? string.Empty;
            Maximum = maximum ?? string.Empty;

            HashSet<string> distinct = new HashSet<string>(StringComparer.Ordinal);
            List<string> selected = new List<string>();
            if (choices != null)
            {
                foreach (string choice in choices)
                {
                    if (choice != null && distinct.Add(choice))
                    {
                        selected.Add(choice);
                    }
                }
            }

            Choices = new ReadOnlyCollection<string>(selected);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 필터 규칙의 식별자.
        /// </summary>
        // ------------------------------------------------------------
        public string ID { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 필터를 건 열의 ID.
        /// </summary>
        // ------------------------------------------------------------
        public string ColumnID { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 텍스트 규칙의 비교 연산자. 범위·선택 규칙은 쓰지 않는다.
        /// </summary>
        // ------------------------------------------------------------
        public TableGridFilterOperator Operator { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 텍스트 규칙의 비교 값.
        /// </summary>
        // ------------------------------------------------------------
        public string Value { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 규칙 종류. 텍스트, 숫자 범위, 선택 집합 중 하나다.
        /// </summary>
        // ------------------------------------------------------------
        public TableGridColumnValueKind ValueKind { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 숫자 범위 규칙의 최솟값 글자. 비교는 호스트가 한다.
        /// </summary>
        // ------------------------------------------------------------
        public string Minimum { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 숫자 범위 규칙의 최댓값 글자. 비교는 호스트가 한다.
        /// </summary>
        // ------------------------------------------------------------
        public string Maximum { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 선택 집합 규칙의 허용 값. 중복을 뺀 읽기 전용 목록이다.
        /// </summary>
        // ------------------------------------------------------------
        public IReadOnlyList<string> Choices { get; }

    }

    #endregion

    #region 쿼리 스냅샷

    // ============================================================
    /// <summary>
    /// 여러 필터와 단일 정렬·그룹 열을 가지는 불변 UI 쿼리다.
    /// </summary>
    // ============================================================
    public sealed class TableGridQueryState
    {

    #region 상태 값

        // ------------------------------------------------------------
        /// <summary>
        /// <br/>필터·정렬·그룹 구성의 방어적 복사본을 생성한다.
        /// <br/>조회 결과 변경 여부는 UI가 아닌 외부 호스트에서 결정한다.
        /// </summary>
        // ------------------------------------------------------------
        public TableGridQueryState
        (
            IReadOnlyList<TableGridFilterRule> filters,
            string sortColumnID = null,
            TableGridSortDirection? sortDirection = null,
            string groupColumnID = null
        )
        {
            if ((string.IsNullOrEmpty(sortColumnID) && sortDirection.HasValue)
                || (!string.IsNullOrEmpty(sortColumnID) && !sortDirection.HasValue))
            {
                throw new ArgumentException("정렬 열과 방향은 함께 지정해야 합니다.");
            }

            if (sortDirection.HasValue && !Enum.IsDefined(typeof(TableGridSortDirection), sortDirection.Value))
            {
                throw new ArgumentOutOfRangeException(nameof(sortDirection));
            }

            List<TableGridFilterRule> copy = new List<TableGridFilterRule>();
            HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
            if (filters != null)
            {
                foreach (TableGridFilterRule rule in filters)
                {
                    if (rule == null || !ids.Add(rule.ID))
                    {
                        throw new ArgumentException("필터 규칙은 null일 수 없고 ID가 서로 달라야 합니다.", nameof(filters));
                    }

                    copy.Add(rule);
                }
            }

            Filters = new ReadOnlyCollection<TableGridFilterRule>(copy);
            SortColumnID = sortColumnID;
            SortDirection = sortDirection;
            GroupColumnID = groupColumnID;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 필터·정렬·그룹이 없는 쿼리 상태를 만든다.
        /// </summary>
        // ------------------------------------------------------------
        public static TableGridQueryState Empty => new TableGridQueryState(null);

        // ------------------------------------------------------------
        /// <summary>
        /// 걸려 있는 필터 규칙의 읽기 전용 목록.
        /// </summary>
        // ------------------------------------------------------------
        public IReadOnlyList<TableGridFilterRule> Filters { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 정렬 열의 ID. 정렬이 없으면 null이다.
        /// </summary>
        // ------------------------------------------------------------
        public string SortColumnID { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 정렬 방향. 정렬이 없으면 null이다.
        /// </summary>
        // ------------------------------------------------------------
        public TableGridSortDirection? SortDirection { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 그룹 열의 ID. 그룹이 없으면 null이다.
        /// </summary>
        // ------------------------------------------------------------
        public string GroupColumnID { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 필터·정렬·그룹 중 하나라도 걸려 있는지 여부.
        /// </summary>
        // ------------------------------------------------------------
        public bool HasRules => Filters.Count > 0 || SortDirection.HasValue || !string.IsNullOrEmpty(GroupColumnID);

    #endregion

    #region 열 단위 변경

        // --------------------------------------------------------------------------------
        /// <summary>
        /// <br/> 정렬 열·방향만 바꾼 새 쿼리 상태를 만든다. direction이 null이면 정렬을 푼다.
        /// <br/> 열 메뉴와 쿼리 칩이 호스트에 보낼 요청 상태를 만들 때 쓴다.
        /// </summary>
        // --------------------------------------------------------------------------------
        internal TableGridQueryState WithSort(string columnID, TableGridSortDirection? direction)
        {
            return new TableGridQueryState
            (
                Filters,
                direction.HasValue ? columnID : null,
                direction,
                GroupColumnID
            );
        }

        // --------------------------------------------------------------------------------
        /// <summary>
        /// <br/> 열의 필터 규칙만 바꾼 새 쿼리 상태를 만든다. rule이 null이면 그 열의 필터를 푼다.
        /// <br/> 열마다 필터 규칙은 하나만 둔다.
        /// </summary>
        // --------------------------------------------------------------------------------
        internal TableGridQueryState WithFilter(string columnID, TableGridFilterRule rule)
        {
            var filters = new List<TableGridFilterRule>();
            foreach (var filter in Filters)
            {
                if (!string.Equals(filter.ColumnID, columnID, StringComparison.Ordinal))
                {
                    filters.Add(filter);
                }
            }

            if (rule != null)
            {
                filters.Add(rule);
            }

            return new TableGridQueryState(filters, SortColumnID, SortDirection, GroupColumnID);
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// 그룹 열만 바꾼 새 쿼리 상태를 만든다. columnID가 null이면 그룹을 푼다.
        /// </summary>
        // ----------------------------------------------------------------------
        internal TableGridQueryState WithGroup(string columnID)
        {
            return new TableGridQueryState(Filters, SortColumnID, SortDirection, columnID);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 열에 걸린 필터 규칙을 찾는다. 없으면 null이다.
        /// </summary>
        // ------------------------------------------------------------
        internal TableGridFilterRule FindFilter(string columnID)
        {
            foreach (var filter in Filters)
            {
                if (string.Equals(filter.ColumnID, columnID, StringComparison.Ordinal))
                {
                    return filter;
                }
            }

            return null;
        }

    #endregion

    }

    #endregion

}
