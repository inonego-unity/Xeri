/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TableGridViewState.cs
수정일 : 2026-11-21

# 설명
TableGrid 보기 상태(열 표시·순서·사용자 폭·고정 열 수, 선택적으로 쿼리와 세션)를 담는 순수 직렬화 데이터와
그 공통 직렬화 경로(JSON)를 정의한다. 런타임 소비처와 Editor 저장 도우미가 같은 경로를 쓴다.

# 특이사항, 제약사항
JSON은 Xeri Core UnityJsonSerializer(JsonUtility)로 만든다. JsonUtility는 null 하위 객체를 기본값으로 채우므로
선택 항목은 Has* 값으로 있는지 구분한다.
쿼리는 표시 글자가 아니라 원래 값을 담는다. 저장 위치는 정하지 않는다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

using inonego;
using inonego.Xeri;
using inonego.Xeri.Serializable;

namespace inonego.Xeri.UI.TableGrid
{
    // ============================================================
    /// <summary>
    /// TableGrid 보기 상태의 직렬화 데이터.
    /// </summary>
    // ============================================================
    [Serializable]
    public sealed class TableGridViewState
    {

    #region 상수

        // ------------------------------------------------------------
        /// <summary>
        /// 지금 형식 버전. 다르면 적용할 때 열 부분만 쓴다.
        /// </summary>
        // ------------------------------------------------------------
        public const int CURRENT_VERSION = 1;

    #endregion

    #region 필드

        // ------------------------------------------------------------
        /// <summary>
        /// 형식 버전.
        /// </summary>
        // ------------------------------------------------------------
        public int Version = CURRENT_VERSION;

        // ------------------------------------------------------------
        /// <summary>
        /// 열마다의 표시·순서·사용자 폭.
        /// </summary>
        // ------------------------------------------------------------
        public List<TableGridColumnViewState> Columns = new();

        // ------------------------------------------------------------
        /// <summary>
        /// 왼쪽에 고정한 표시 열 수.
        /// </summary>
        // ------------------------------------------------------------
        public int FrozenColumnCount;

        // ------------------------------------------------------------
        /// <summary>
        /// Query를 담았는지 여부.
        /// </summary>
        // ------------------------------------------------------------
        public bool HasQuery;

        // ------------------------------------------------------------
        /// <summary>
        /// 정렬·필터·그룹(원래 값).
        /// </summary>
        // ------------------------------------------------------------
        public TableGridQueryViewState Query = new();

        // ------------------------------------------------------------
        /// <summary>
        /// Session을 담았는지 여부.
        /// </summary>
        // ------------------------------------------------------------
        public bool HasSession;

        // ------------------------------------------------------------
        /// <summary>
        /// 스크롤 위치와 선택 행.
        /// </summary>
        // ------------------------------------------------------------
        public TableGridSessionViewState Session = new();

    #endregion

    #region 직렬화

        // ------------------------------------------------------------
        /// <summary>
        /// 보기 상태를 JSON으로 만든다.
        /// </summary>
        // ------------------------------------------------------------
        public string ToJson()
        {
            return UnityJsonSerializer.Default.Serialize(this);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// JSON에서 보기 상태를 읽는다. 비었거나 읽지 못하면 false다.
        /// </summary>
        // ------------------------------------------------------------
        public static bool TryFromJson(string json, out TableGridViewState state)
        {
            state = null;
            if (string.IsNullOrWhiteSpace(json))
            {
                return false;
            }

            try
            {
                state = UnityJsonSerializer.Default.Deserialize<TableGridViewState>(json);
            }
            catch (Exception)
            {
                state = null;
            }

            if (state == null)
            {
                return false;
            }

            state.Columns ??= new List<TableGridColumnViewState>();
            state.Query ??= new TableGridQueryViewState();
            state.Session ??= new TableGridSessionViewState();
            return true;
        }

    #endregion

    }

    // ============================================================
    /// <summary>
    /// 열 하나의 보기 상태.
    /// </summary>
    // ============================================================
    [Serializable]
    public sealed class TableGridColumnViewState
    {
        // ------------------------------------------------------------
        /// <summary>
        /// 열 ID.
        /// </summary>
        // ------------------------------------------------------------
        public string ID;

        // ------------------------------------------------------------
        /// <summary>
        /// 보이는지 여부.
        /// </summary>
        // ------------------------------------------------------------
        public bool Visible;

        // ------------------------------------------------------------
        /// <summary>
        /// 표시 순서. 숨긴 열은 -1이다.
        /// </summary>
        // ------------------------------------------------------------
        public int Order = -1;

        // ------------------------------------------------------------
        /// <summary>
        /// 고정 열의 사용자 폭(px)이 있는지 여부.
        /// </summary>
        // ------------------------------------------------------------
        public bool HasWidth;

        // ------------------------------------------------------------
        /// <summary>
        /// 고정 열의 사용자 폭(px).
        /// </summary>
        // ------------------------------------------------------------
        public float Width;

        // ------------------------------------------------------------
        /// <summary>
        /// 가변 열의 사용자 비율이 있는지 여부.
        /// </summary>
        // ------------------------------------------------------------
        public bool HasWeight;

        // ------------------------------------------------------------
        /// <summary>
        /// 가변 열의 사용자 비율(가중치).
        /// </summary>
        // ------------------------------------------------------------
        public float Weight;
    }

    // ============================================================
    /// <summary>
    /// 쿼리(정렬·필터·그룹)의 보기 상태. 원래 값을 담는다.
    /// </summary>
    // ============================================================
    [Serializable]
    public sealed class TableGridQueryViewState
    {
        // ------------------------------------------------------------
        /// <summary>
        /// 정렬 열 ID. 정렬이 없으면 비어 있다.
        /// </summary>
        // ------------------------------------------------------------
        public string SortColumnID;

        // ------------------------------------------------------------
        /// <summary>
        /// 정렬 방향. SortColumnID가 있을 때만 쓴다.
        /// </summary>
        // ------------------------------------------------------------
        public TableGridSortDirection SortDirection;

        // ------------------------------------------------------------
        /// <summary>
        /// 그룹 열 ID. 그룹이 없으면 비어 있다.
        /// </summary>
        // ------------------------------------------------------------
        public string GroupColumnID;

        // ------------------------------------------------------------
        /// <summary>
        /// 필터 규칙.
        /// </summary>
        // ------------------------------------------------------------
        public List<TableGridFilterViewState> Filters = new();

        // ------------------------------------------------------------
        /// <summary>
        /// 쿼리 상태에서 보기 상태를 만든다.
        /// </summary>
        // ------------------------------------------------------------
        public static TableGridQueryViewState From(TableGridQueryState query)
        {
            var state = new TableGridQueryViewState();
            if (query == null)
            {
                return state;
            }

            if (query.SortDirection.HasValue)
            {
                state.SortColumnID = query.SortColumnID;
                state.SortDirection = query.SortDirection.Value;
            }

            state.GroupColumnID = query.GroupColumnID;
            foreach (var rule in query.Filters)
            {
                state.Filters.Add(TableGridFilterViewState.From(rule));
            }

            return state;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 쿼리 상태로 되돌린다. 읽을 수 없는 필터 규칙은 건너뛴다.
        /// </summary>
        // ------------------------------------------------------------
        public TableGridQueryState ToQuery()
        {
            var filters = new List<TableGridFilterRule>();
            foreach (var filter in Filters ?? new List<TableGridFilterViewState>())
            {
                if (filter != null && filter.TryToRule(out var rule))
                {
                    filters.Add(rule);
                }
            }

            var hasSort = !string.IsNullOrEmpty(SortColumnID) && Enum.IsDefined(typeof(TableGridSortDirection), SortDirection);
            return new TableGridQueryState
            (
                filters,
                hasSort ? SortColumnID : null,
                hasSort ? SortDirection : (TableGridSortDirection?)null,
                string.IsNullOrEmpty(GroupColumnID) ? null : GroupColumnID
            );
        }
    }

    // ============================================================
    /// <summary>
    /// 필터 규칙 하나의 보기 상태.
    /// </summary>
    // ============================================================
    [Serializable]
    public sealed class TableGridFilterViewState
    {
        // ------------------------------------------------------------
        /// <summary>
        /// 규칙 ID.
        /// </summary>
        // ------------------------------------------------------------
        public string ID;

        // ------------------------------------------------------------
        /// <summary>
        /// 열 ID.
        /// </summary>
        // ------------------------------------------------------------
        public string ColumnID;

        // ------------------------------------------------------------
        /// <summary>
        /// 규칙 종류(글자·숫자 범위·선택형).
        /// </summary>
        // ------------------------------------------------------------
        public TableGridColumnValueKind ValueKind;

        // ------------------------------------------------------------
        /// <summary>
        /// 글자 규칙의 연산자.
        /// </summary>
        // ------------------------------------------------------------
        public TableGridFilterOperator Operator;

        // ------------------------------------------------------------
        /// <summary>
        /// 글자 규칙의 비교 값.
        /// </summary>
        // ------------------------------------------------------------
        public string Value;

        // ------------------------------------------------------------
        /// <summary>
        /// 숫자 범위의 최솟값.
        /// </summary>
        // ------------------------------------------------------------
        public string Minimum;

        // ------------------------------------------------------------
        /// <summary>
        /// 숫자 범위의 최댓값.
        /// </summary>
        // ------------------------------------------------------------
        public string Maximum;

        // ------------------------------------------------------------
        /// <summary>
        /// 선택형 규칙의 허용 값(원래 값).
        /// </summary>
        // ------------------------------------------------------------
        public List<string> Choices = new();

        // ------------------------------------------------------------
        /// <summary>
        /// 필터 규칙에서 보기 상태를 만든다.
        /// </summary>
        // ------------------------------------------------------------
        public static TableGridFilterViewState From(TableGridFilterRule rule)
        {
            return new TableGridFilterViewState
            {
                ID = rule.ID,
                ColumnID = rule.ColumnID,
                ValueKind = rule.ValueKind,
                Operator = rule.Operator,
                Value = rule.Value,
                Minimum = rule.Minimum,
                Maximum = rule.Maximum,
                Choices = new List<string>(rule.Choices),
            };
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 필터 규칙으로 되돌린다. ID·열 ID가 없거나 값이 맞지 않으면 false다.
        /// </summary>
        // ------------------------------------------------------------
        public bool TryToRule(out TableGridFilterRule rule)
        {
            rule = null;
            if (string.IsNullOrWhiteSpace(ID) || string.IsNullOrWhiteSpace(ColumnID))
            {
                return false;
            }

            switch (ValueKind)
            {
                case TableGridColumnValueKind.Number:
                    rule = TableGridFilterRule.NumberRange(ID, ColumnID, Minimum, Maximum);
                    return true;

                case TableGridColumnValueKind.Choice:
                    rule = TableGridFilterRule.ChoiceSet(ID, ColumnID, Choices ?? new List<string>());
                    return true;

                case TableGridColumnValueKind.Text:
                    if (!Enum.IsDefined(typeof(TableGridFilterOperator), Operator))
                    {
                        return false;
                    }

                    rule = new TableGridFilterRule(ID, ColumnID, Operator, Value);
                    return true;

                default:
                    return false;
            }
        }
    }

    // ============================================================
    /// <summary>
    /// 세션 보기 상태(스크롤 위치, 선택한 행 ID).
    /// </summary>
    // ============================================================
    [Serializable]
    public sealed class TableGridSessionViewState
    {
        // ------------------------------------------------------------
        /// <summary>
        /// 가로 스크롤 위치.
        /// </summary>
        // ------------------------------------------------------------
        public float ScrollX;

        // ------------------------------------------------------------
        /// <summary>
        /// 세로 스크롤 위치.
        /// </summary>
        // ------------------------------------------------------------
        public float ScrollY;

        // ------------------------------------------------------------
        /// <summary>
        /// 행 선택이면 선택한 행 ID.
        /// </summary>
        // ------------------------------------------------------------
        public List<string> SelectedRowIDs = new();
    }
}
