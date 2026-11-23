/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TableGridIntents.cs
수정일 : 2026-11-22

# 설명
TableGrid의 편집·상세 편집·정렬·필터·그룹 요청과 붙여넣기 진단 정보를 정의한다.
편집 요청 값(Value·PreviousValue)과 행 추가 요청의 행 값(RowValues)은 열 값 형식(TableGridColumn.ValueType)의 값이다.

# 특이사항, 제약사항
각 intent는 UI 데이터만 전달하며 데이터 변경이나 쿼리 실행을 수행하지 않는다.
실행 취소는 데이터를 가진 호스트가 맡고 표는 이력을 두지 않는다(에디터는 Unity Undo, 런타임은 Xeri Commanding의
DoSession 같은 호스트 쪽 이력. 표 쪽 연결 예제는 에디터 예제 창의 Unity Undo뿐이다).
표는 한 조작을 한 요청으로 보내고, 요청에 변경 전 값(PreviousValue)과 요청 시점의 선택(Selection)을 담는다.
호스트는 요청 하나를 한 단계로 기록하고, 반영한 뒤(또는 되돌린 뒤) ApplyEditResults(행 구성이 바뀌면 SetSource)로
보이고 기록한 Selection을 다시 지정한다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

namespace inonego.Xeri.UI.TableGrid
{

    #region 호스트 요청

    // ============================================================
    /// <summary>
    /// 호스트의 외부 처리에 전달할 셀 편집 요청 데이터다.
    /// </summary>
    // ============================================================
    public sealed class TableGridEditIntent
    {
        // ------------------------------------------------------------
        /// <summary>
        /// <br/> 셀 편집 요청을 만든다. value는 열 값 형식의 새 값이다.
        /// <br/> previousValue는 요청 시점의 셀 값이다. 모르면 null이다.
        /// </summary>
        // ------------------------------------------------------------
        public TableGridEditIntent(string rowID, string columnID, object value, object previousValue = null)
        {
            RowID = TableGridID.Require(rowID, nameof(rowID), "행");
            ColumnID = TableGridID.Require(columnID, nameof(columnID), "열");
            Value = value;
            PreviousValue = previousValue;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 편집 대상 행 ID.
        /// </summary>
        // ------------------------------------------------------------
        public string RowID { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 편집 대상 열 ID.
        /// </summary>
        // ------------------------------------------------------------
        public string ColumnID { get; }

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> 새 셀 값. 열 값 형식(TableGridColumn.ValueType)의 값이다.
        /// <br/> 지우기면 값 규칙의 ClearValue, 붙여넣기면 값 묶음의 복사본이다.
        /// </summary>
        // ----------------------------------------------------------------------
        public object Value { get; }

        // --------------------------------------------------------------------------------
        /// <summary>
        /// <br/> 요청 시점에 열이 행 항목에서 꺼낸 값. 호스트가 실행 취소 명령을 만들 때 쓴다.
        /// <br/> 표가 셀 값을 읽지 못했거나(결과에 없는 행 등) 호스트가 직접 만든 요청이면 null이라,
        /// <br/> 셀의 실제 null 값과 구분되지 않는다. 구분이 필요하면 호스트가 자기 데이터에서 읽는다.
        /// </summary>
        // --------------------------------------------------------------------------------
        public object PreviousValue { get; }

    }

    // ======================================================================
    /// <summary>
    /// <br/> 붙여넣기(잘라낸 셀 옮기기 포함)에서 건너뛴 셀 수를 사유별로 기록한다.
    /// <br/> 요청한 셀은 편집 요청의 Edits가 담는다.
    /// </summary>
    // ======================================================================
    public readonly struct TableGridPasteFeedback
    {
        // ------------------------------------------------------------
        /// <summary>
        /// 건너뛴 셀 수를 사유별로 기록한다.
        /// </summary>
        // ------------------------------------------------------------
        public TableGridPasteFeedback(int readOnly, int invalid, int outside)
        {
            ReadOnly = readOnly;
            Invalid = invalid;
            Outside = outside;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 읽기 전용이거나 붙여넣을 수 없는 열이라 건너뛴 셀 수.
        /// </summary>
        // ------------------------------------------------------------
        public int ReadOnly { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 값이 유효하지 않아 건너뛴 셀 수.
        /// </summary>
        // ------------------------------------------------------------
        public int Invalid { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 표 범위 밖이라 건너뛴 셀 수.
        /// </summary>
        // ------------------------------------------------------------
        public int Outside { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 건너뛴 전체 셀 수.
        /// </summary>
        // ------------------------------------------------------------
        public int Skipped => ReadOnly + Invalid + Outside;
    }

    // ======================================================================
    /// <summary>
    /// <br/> 열 메뉴·쿼리 칩에서 정렬·필터·그룹을 바꿔 달라는 요청.
    /// <br/> 호스트가 Requested로 조회한 뒤 TableGrid.QueryState로 반영한다.
    /// </summary>
    // ======================================================================
    public sealed class TableGridQueryRequest : EventArgs
    {
        // ------------------------------------------------------------
        /// <summary>
        /// 지금 표에 보이는 쿼리 상태와 요청한 쿼리 상태를 담는다.
        /// </summary>
        // ------------------------------------------------------------
        public TableGridQueryRequest(TableGridQueryState current, TableGridQueryState requested)
        {
            Current = current ?? throw new ArgumentNullException(nameof(current));
            Requested = requested ?? throw new ArgumentNullException(nameof(requested));
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 요청 시점에 표에 보이던 쿼리 상태.
        /// </summary>
        // ------------------------------------------------------------
        public TableGridQueryState Current { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 사용자가 고른 결과 쿼리 상태.
        /// </summary>
        // ------------------------------------------------------------
        public TableGridQueryState Requested { get; }
    }

    // ============================================================
    /// <summary>
    /// 셀 변경 요청이 어디서 왔는지.
    /// </summary>
    // ============================================================
    public enum TableGridEditSource
    {
        // ------------------------------------------------------------
        /// <summary>
        /// 셀 편집기(입력칸·선택형 값 메뉴)에서 확정했다.
        /// </summary>
        // ------------------------------------------------------------
        Typing,

        // ------------------------------------------------------------
        /// <summary>
        /// TSV 붙여넣기다.
        /// </summary>
        // ------------------------------------------------------------
        Paste,

        // ------------------------------------------------------------
        /// <summary>
        /// 선택한 셀 지우기(Delete)다.
        /// </summary>
        // ------------------------------------------------------------
        Clear,

        // --------------------------------------------------------------------------------
        /// <summary>
        /// 잘라낸 셀·행을 같은 표에 붙여넣어 값을 옮겼다. 덮어쓴 값과 원본 비우기가 한 요청에 담긴다.
        /// </summary>
        // --------------------------------------------------------------------------------
        Cut,

        // ------------------------------------------------------------
        /// <summary>
        /// Inline 셀 안 컨트롤에서 바꿨다.
        /// </summary>
        // ------------------------------------------------------------
        Inline,
    }

    // ================================================================================
    /// <summary>
    /// <br/> 셀 값을 바꿔 달라는 요청. 표는 데이터를 바꾸지 않는다.
    /// <br/> 호스트가 반영한 변경은 TableGrid.ApplyEditResults나 Refresh로 표에 보인다.
    /// </summary>
    // ================================================================================
    public sealed class TableGridEditRequest : EventArgs
    {
        // --------------------------------------------------------------------------------
        /// <summary>
        /// 변경 목록과 출처, 붙여넣기 결과(붙여넣기·잘라낸 셀 옮기기일 때만), 요청 시점의 선택을 담는다.
        /// </summary>
        // --------------------------------------------------------------------------------
        public TableGridEditRequest
        (
            IReadOnlyList<TableGridEditIntent> edits, TableGridEditSource source,
            TableGridPasteFeedback? feedback = null, TableGridSelection selection = null
        )
        {
            Edits = edits ?? throw new ArgumentNullException(nameof(edits));
            Source = source;
            Feedback = feedback;
            Selection = selection ?? TableGridSelection.None;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 바꿀 셀과 값. 한 번의 조작에서 나온 변경은 한 요청에 모두 담긴다.
        /// </summary>
        // ------------------------------------------------------------
        public IReadOnlyList<TableGridEditIntent> Edits { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 요청을 만든 조작.
        /// </summary>
        // ------------------------------------------------------------
        public TableGridEditSource Source { get; }

        // ----------------------------------------------------------------------
        /// <summary>
        /// 붙여넣기·잘라낸 셀 옮기기에서 건너뛴 셀의 사유별 개수. 그 밖에는 null이다.
        /// </summary>
        // ----------------------------------------------------------------------
        public TableGridPasteFeedback? Feedback { get; }

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> 요청 시점의 선택. 호스트가 실행 취소·다시 실행 단계에 함께 기록해 두었다가
        /// <br/> 그 단계를 되돌릴 때 TableGrid.Selection으로 다시 보인다.
        /// </summary>
        // ----------------------------------------------------------------------
        public TableGridSelection Selection { get; }
    }

    // ============================================================
    /// <summary>
    /// 선택한 데이터 행을 지워 달라는 요청. 표는 데이터를 바꾸지 않는다.
    /// </summary>
    // ============================================================
    public sealed class TableGridDeleteRowsRequest : EventArgs
    {
        // ------------------------------------------------------------
        /// <summary>
        /// 지울 행 ID 목록과 요청 시점의 선택을 담는다.
        /// </summary>
        // ------------------------------------------------------------
        public TableGridDeleteRowsRequest(IReadOnlyList<string> rowIDs, TableGridSelection selection = null)
        {
            RowIDs = rowIDs ?? throw new ArgumentNullException(nameof(rowIDs));
            Selection = selection ?? TableGridSelection.None;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 지울 행 ID 목록.
        /// </summary>
        // ------------------------------------------------------------
        public IReadOnlyList<string> RowIDs { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 요청 시점의 선택. 실행 취소로 행을 되살릴 때 다시 보인다.
        /// </summary>
        // ------------------------------------------------------------
        public TableGridSelection Selection { get; }
    }

    // ================================================================================
    /// <summary>
    /// <br/> 행을 다른 행 앞·뒤로 옮겨 달라는 요청(끌기 손잡이·Alt+방향키·메뉴·잘라낸 행 삽입).
    /// <br/> 읽기 전용 값까지 행째 옮긴다. 표는 데이터를 바꾸지 않는다.
    /// </summary>
    // ================================================================================
    public sealed class TableGridMoveRowsRequest : EventArgs
    {
        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> 옮길 행, 넣을 자리의 행과 앞·뒤, 요청한 입력, 요청 시점의 선택을 담는다.
        /// <br/> source가 ApplySort이면 행과 자리 없이
        /// <br/> 지금 보기 정렬 순서를 데이터에 적용해 달라는 요청이다.
        /// </summary>
        // ----------------------------------------------------------------------
        public TableGridMoveRowsRequest
        (
            IReadOnlyList<string> rowIDs, string targetRowID, TableGridMovePlacement placement,
            TableGridMoveSource source, TableGridSelection selection = null
        )
        {
            RowIDs = rowIDs ?? throw new ArgumentNullException(nameof(rowIDs));
            if (source != TableGridMoveSource.ApplySort)
            {
                TableGridID.Require(targetRowID, nameof(targetRowID), "대상 행");
            }

            TargetRowID = targetRowID;
            Placement = placement;
            Source = source;
            Selection = selection ?? TableGridSelection.None;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 옮길 행 ID(화면 순서).
        /// </summary>
        // ------------------------------------------------------------
        public IReadOnlyList<string> RowIDs { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 옮긴 행들이 들어갈 자리의 행. ApplySort이면 null이다.
        /// </summary>
        // ------------------------------------------------------------
        public string TargetRowID { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 자리 행의 앞(Before)에 넣을지 뒤(After)에 넣을지.
        /// </summary>
        // ------------------------------------------------------------
        public TableGridMovePlacement Placement { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 요청한 입력(끌기·키보드·메뉴·잘라내기·정렬 적용).
        /// </summary>
        // ------------------------------------------------------------
        public TableGridMoveSource Source { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 요청 시점의 선택. 실행 취소로 되돌릴 때 다시 보인다.
        /// </summary>
        // ------------------------------------------------------------
        public TableGridSelection Selection { get; }
    }

    // ============================================================
    /// <summary>
    /// <br/> Detail 열 셀의 상세 편집을 열어 달라는 요청.
    /// <br/> 편집 화면과 반영은 호스트가 맡는다(셀 밖 편집).
    /// </summary>
    // ============================================================
    public sealed class TableGridDetailRequest : EventArgs
    {
        // ------------------------------------------------------------
        /// <summary>
        /// 상세 편집할 셀의 행·열 ID를 담는다.
        /// </summary>
        // ------------------------------------------------------------
        public TableGridDetailRequest(string rowID, string columnID)
        {
            RowID = TableGridID.Require(rowID, nameof(rowID), "행");
            ColumnID = TableGridID.Require(columnID, nameof(columnID), "열");
            ColumnID = columnID;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 상세 편집할 행 ID.
        /// </summary>
        // ------------------------------------------------------------
        public string RowID { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 상세 편집할 열 ID.
        /// </summary>
        // ------------------------------------------------------------
        public string ColumnID { get; }
    }

    #endregion

    // ================================================================================
    /// <summary>
    /// <br/> 새 행을 넣어 달라는 요청. 추가 흐름(형식 선택·초안 등)과 반영은 호스트가 한다.
    /// <br/> 행 값이 없으면 빈 행 하나, 있으면 그 수만큼 행을 넣고 값을 채운다(복사한 행 삽입).
    /// </summary>
    // ================================================================================
    public sealed class TableGridAddRowRequest : EventArgs
    {
        // --------------------------------------------------------------------------------
        /// <summary>
        /// <br/> 넣을 자리와 기준 행, 요청 시점의 선택, 넣을 행 값을 담는다.
        /// <br/> End이면 기준 행이 없다. rowValues가 null이거나 비어 있으면 빈 행 하나다.
        /// </summary>
        // --------------------------------------------------------------------------------
        public TableGridAddRowRequest
        (
            TableGridAddPosition position, string rowID, TableGridSelection selection = null,
            IReadOnlyList<IReadOnlyDictionary<string, object>> rowValues = null
        )
        {
            if (position != TableGridAddPosition.End)
            {
                TableGridID.Require(rowID, nameof(rowID), "기준 행");
            }

            Position = position;
            RowID = position == TableGridAddPosition.End ? null : rowID;
            Selection = selection ?? TableGridSelection.None;
            RowValues = CopyRowValues(rowValues);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 넣을 자리(끝, 기준 행의 앞·뒤).
        /// </summary>
        // ------------------------------------------------------------
        public TableGridAddPosition Position { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 기준 행 ID. End이면 null이다.
        /// </summary>
        // ------------------------------------------------------------
        public string RowID { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 요청 시점의 선택.
        /// </summary>
        // ------------------------------------------------------------
        public TableGridSelection Selection { get; }

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> 넣을 행마다 열 ID → 값. 비어 있으면 빈 행 하나를 넣는다.
        /// <br/> 값은 열 값 형식이고, 받지 않는 열(읽기 전용 등)은 들지 않는다.
        /// </summary>
        // ----------------------------------------------------------------------
        public IReadOnlyList<IReadOnlyDictionary<string, object>> RowValues { get; }

        private static IReadOnlyList<IReadOnlyDictionary<string, object>> CopyRowValues
        (
            IReadOnlyList<IReadOnlyDictionary<string, object>> rowValues
        )
        {
            if (rowValues == null || rowValues.Count == 0)
            {
                return Array.Empty<IReadOnlyDictionary<string, object>>();
            }

            var copy = new IReadOnlyDictionary<string, object>[rowValues.Count];
            for (var index = 0; index < rowValues.Count; index++)
            {
                var values = rowValues[index] ?? throw new ArgumentException("행 값 목록에 null이 들 수 없습니다.", nameof(rowValues));
                var row = new Dictionary<string, object>(values.Count, StringComparer.Ordinal);
                foreach (var pair in values)
                {
                    row[pair.Key] = pair.Value;
                }

                copy[index] = row;
            }

            return copy;
        }
    }
}
