/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TableGridSelectionState.cs
수정일 : 2026-11-22

# 설명
현재 선택과, 그 선택을 표시 위치(행·열 인덱스)로 옮긴 결과를 보관한다.
셀마다 어떤 선택 바탕(범위 안·활성)을 받는지도 여기서 계산한다. 범위 외곽선은 TableGridViewport의 선택 층이 그린다.
열 선택은 고른 열의 표시 위치를 모아 두고, 그 열의 모든 데이터 행 셀에 범위 바탕을 준다.

# 특이사항, 제약사항
선택 값은 행·열 ID로 담고, 표시 위치는 Project로 다시 계산한다.
결과·순서가 바뀔 때 선택을 어떻게 남길지는 TableGridModel이 정한다(행·열은 ID, 셀은 같은 자리).
결과에 없는 행이나 숨긴 열을 가리키는 셀 선택은 위치가 없으므로 표시하지 않는다.
열 선택은 셀 범위가 아니므로 HasCellRange가 거짓이다. 활성 셀은 기준 열과 열 선택의 활성 행으로 정한다.
UI Toolkit에 의존하지 않는다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

namespace inonego.Xeri.UI.TableGrid
{
    // ============================================================
    /// <summary>
    /// 셀 하나의 선택 표시 상태.
    /// </summary>
    // ============================================================
    [Flags]
    internal enum TableGridCellMarks
    {
        None     = 0,
        InRange  = 1 << 0,
        Active   = 1 << 1,
        Rejected = 1 << 2,
        Cut      = 1 << 3,
    }

    // ============================================================
    /// <summary>
    /// 선택과 그 표시 위치.
    /// </summary>
    // ============================================================
    internal sealed class TableGridSelectionState
    {

    #region 필드

        private readonly HashSet<string> selectedRowIDs = new(StringComparer.Ordinal);
        private readonly HashSet<string> selectedColumnIDs = new(StringComparer.Ordinal);

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 선택. 선택이 없으면 TableGridSelection.None이다.
        /// </summary>
        // ------------------------------------------------------------
        public TableGridSelection Current { get; private set; } = TableGridSelection.None;

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> 행 선택의 기준 행 ID. Shift 범위와 키보드 이동의 출발점이다.
        /// <br/> 행을 누르거나 Ctrl로 토글할 때 입력이 정한다.
        /// </summary>
        // ------------------------------------------------------------
        public string RowAnchorID { get; set; }

        // --------------------------------------------------------------------------------
        /// <summary>
        /// <br/> 행 선택의 활성 열 ID. Selection이 Row·MultiRow인 표에서 행을 고른 입력이 정하며,
        /// <br/> 있으면 기준 행의 그 열이 활성 셀이 된다(편집·키보드 이동의 출발점).
        /// <br/> 행 선택 칸으로 고르면 없다.
        /// </summary>
        // --------------------------------------------------------------------------------
        public string RowActiveColumnID { get; set; }

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> 열 선택의 기준 열 ID. Shift 범위의 출발점이자 활성 셀의 열이다.
        /// <br/> 헤더를 누르거나 Ctrl로 토글할 때 입력이 정한다.
        /// </summary>
        // ----------------------------------------------------------------------
        public string ColumnAnchorID { get; set; }

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> 열 선택의 활성 행 ID. 열을 고르기 전의 활성 행을 잇고,
        /// <br/> 없으면 첫 데이터 행이다(TableGridModel이 맞춘다).
        /// </summary>
        // ------------------------------------------------------------
        public string ColumnActiveRowID { get; set; }

        // ------------------------------------------------------------
        /// <summary>
        /// 열 선택의 표시 열 위치(오름차순). 열 선택이 아니면 비어 있다.
        /// </summary>
        // ------------------------------------------------------------
        public IReadOnlyList<int> ColumnPositions => columnPositions;
        private readonly List<int> columnPositions = new();

        // ------------------------------------------------------------
        /// <summary>
        /// 열 선택의 표시 열이 빈틈없이 이어져 있는지 여부.
        /// </summary>
        // ------------------------------------------------------------
        public bool IsColumnRun => columnPositions.Count > 0
            && columnPositions[columnPositions.Count - 1] - columnPositions[0] + 1 == columnPositions.Count;

        // ------------------------------------------------------------
        /// <summary>
        /// 셀 범위의 첫 행 위치. 표시할 범위가 없으면 -1이다.
        /// </summary>
        // ------------------------------------------------------------
        public int RowMin { get; private set; } = -1;

        // ------------------------------------------------------------
        /// <summary>
        /// 셀 범위의 마지막 행 위치. 표시할 범위가 없으면 -1이다.
        /// </summary>
        // ------------------------------------------------------------
        public int RowMax { get; private set; } = -1;

        // ------------------------------------------------------------
        /// <summary>
        /// 셀 범위의 첫 표시 열 위치. 표시할 범위가 없으면 -1이다.
        /// </summary>
        // ------------------------------------------------------------
        public int ColumnMin { get; private set; } = -1;

        // ------------------------------------------------------------
        /// <summary>
        /// 셀 범위의 마지막 표시 열 위치. 표시할 범위가 없으면 -1이다.
        /// </summary>
        // ------------------------------------------------------------
        public int ColumnMax { get; private set; } = -1;

        // ------------------------------------------------------------
        /// <summary>
        /// 셀 범위 시작 셀의 행 위치. 표시할 범위가 없으면 -1이다.
        /// </summary>
        // ------------------------------------------------------------
        public int AnchorRow { get; private set; } = -1;

        // ------------------------------------------------------------
        /// <summary>
        /// 셀 범위 시작 셀의 표시 열 위치. 표시할 범위가 없으면 -1이다.
        /// </summary>
        // ------------------------------------------------------------
        public int AnchorColumn { get; private set; } = -1;

        // ------------------------------------------------------------
        /// <summary>
        /// 활성 셀의 행 위치. 없으면 -1이다.
        /// </summary>
        // ------------------------------------------------------------
        public int ActiveRow { get; private set; } = -1;

        // ------------------------------------------------------------
        /// <summary>
        /// 활성 셀의 표시 열 위치. 없으면 -1이다.
        /// </summary>
        // ------------------------------------------------------------
        public int ActiveColumn { get; private set; } = -1;

        // ------------------------------------------------------------
        /// <summary>
        /// 표시할 셀 범위가 있는지 여부.
        /// </summary>
        // ------------------------------------------------------------
        public bool HasCellRange => RowMin >= 0;

        // ------------------------------------------------------------
        /// <summary>
        /// 셀 범위의 행 수. 범위가 없으면 0이다.
        /// </summary>
        // ------------------------------------------------------------
        public int RangeRowCount => HasCellRange ? RowMax - RowMin + 1 : 0;

        // ------------------------------------------------------------
        /// <summary>
        /// 셀 범위의 열 수. 범위가 없으면 0이다.
        /// </summary>
        // ------------------------------------------------------------
        public int RangeColumnCount => HasCellRange ? ColumnMax - ColumnMin + 1 : 0;

    #endregion

    #region 선택 변경

        // ------------------------------------------------------------
        /// <summary>
        /// 선택을 바꾼다. 같은 선택이면 바꾸지 않고 false를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public bool Set(TableGridSelection selection, out TableGridSelection previous)
        {
            previous = Current;
            selection ??= TableGridSelection.None;
            if (ReferenceEquals(selection, Current))
            {
                return false;
            }

            Current = selection;
            selectedRowIDs.Clear();
            foreach (var rowID in selection.RowIDs)
            {
                selectedRowIDs.Add(rowID);
            }

            selectedColumnIDs.Clear();
            foreach (var columnID in selection.ColumnIDs)
            {
                selectedColumnIDs.Add(columnID);
            }

            return true;
        }

        // --------------------------------------------------------------------------------
        /// <summary>
        /// <br/> 호스트가 건 행·열 선택에서 키보드 이동의 기준(행 선택은 기준 행, 열 선택은 기준 열)이
        /// <br/> 선택 밖이거나 없으면 선택의 첫 항목으로 맞춘다.
        /// <br/> 기준은 입력이 정하므로 호스트의 선택에는 기준이 없어,
        /// <br/> 맞추지 않으면 키보드 이동이 출발점을 잃는다.
        /// <br/> 선택 안에 있는 기준과 활성 열은 그대로 두며, 셀 선택이나 빈 선택은 바꾸지 않는다.
        /// </summary>
        // --------------------------------------------------------------------------------
        public void AlignAnchor(TableGridSelection selection)
        {
            switch (selection?.Kind)
            {
                case TableGridSelectionKind.Row:
                    RowAnchorID = AlignAnchorID(RowAnchorID, selection.RowIDs);
                    break;
                case TableGridSelectionKind.Column:
                    ColumnAnchorID = AlignAnchorID(ColumnAnchorID, selection.ColumnIDs);
                    break;
            }
        }

        private static string AlignAnchorID(string anchorID, IReadOnlyList<string> ids)
        {
            if (ids.Count == 0)
            {
                return anchorID;
            }

            foreach (var id in ids)
            {
                if (string.Equals(id, anchorID, StringComparison.Ordinal))
                {
                    return anchorID;
                }
            }

            return ids[0];
        }

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> 셀·열 선택을 현재 표시 위치로 옮겨 계산한다.
        /// <br/> 셀 선택은 행 또는 열 위치를 못 찾으면 범위를 표시하지 않고,
        /// <br/> 열 선택은 숨긴 열을 빼고 보이는 열만 표시한다.
        /// </summary>
        // ------------------------------------------------------------
        public void Project(Func<string, int> rowIndexOf, Func<string, int> columnIndexOf)
        {
            RowMin = RowMax = ColumnMin = ColumnMax = -1;
            AnchorRow = AnchorColumn = -1;
            ActiveRow = ActiveColumn = -1;
            columnPositions.Clear();

            if (Current.Kind == TableGridSelectionKind.Column)
            {
                ProjectColumns(rowIndexOf, columnIndexOf);
                return;
            }

            if (Current.Kind == TableGridSelectionKind.Row)
            {
                // 행 선택은 범위를 그리지 않는다. 활성 열이 있으면 기준 행의 그 셀을 활성 셀로 둔다.
                var row = IsRowSelected(RowAnchorID) ? rowIndexOf(RowAnchorID) : -1;
                var column = string.IsNullOrEmpty(RowActiveColumnID) ? -1 : columnIndexOf(RowActiveColumnID);
                if (row >= 0 && column >= 0)
                {
                    ActiveRow = row;
                    ActiveColumn = column;
                }

                return;
            }

            if (Current.Kind != TableGridSelectionKind.Cell)
            {
                return;
            }

            var anchorRow = rowIndexOf(Current.Anchor.RowID);
            var focusRow = rowIndexOf(Current.Focus.RowID);
            var anchorColumn = columnIndexOf(Current.Anchor.ColumnID);
            var focusColumn = columnIndexOf(Current.Focus.ColumnID);
            if (anchorRow < 0 || focusRow < 0 || anchorColumn < 0 || focusColumn < 0)
            {
                return;
            }

            RowMin = Math.Min(anchorRow, focusRow);
            RowMax = Math.Max(anchorRow, focusRow);
            ColumnMin = Math.Min(anchorColumn, focusColumn);
            ColumnMax = Math.Max(anchorColumn, focusColumn);
            AnchorRow = anchorRow;
            AnchorColumn = anchorColumn;
            ActiveRow = focusRow;
            ActiveColumn = focusColumn;
        }

        private void ProjectColumns(Func<string, int> rowIndexOf, Func<string, int> columnIndexOf)
        {
            foreach (var columnID in Current.ColumnIDs)
            {
                var position = columnIndexOf(columnID);
                if (position >= 0)
                {
                    columnPositions.Add(position);
                }
            }

            columnPositions.Sort();

            // 기준 열이 선택 안에 있고 활성 행이 결과에 있으면 그 셀이 활성 셀이다.
            var column = IsColumnSelected(ColumnAnchorID) ? columnIndexOf(ColumnAnchorID) : -1;
            var row = string.IsNullOrEmpty(ColumnActiveRowID) ? -1 : rowIndexOf(ColumnActiveRowID);
            if (row >= 0 && column >= 0)
            {
                ActiveRow = row;
                ActiveColumn = column;
            }
        }

    #endregion

    #region 표시 조회

        // ------------------------------------------------------------
        /// <summary>
        /// 행 ID가 행 선택에 들어 있는지 여부.
        /// </summary>
        // ------------------------------------------------------------
        public bool IsRowSelected(string rowID)
        {
            return rowID != null && selectedRowIDs.Contains(rowID);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 열 ID가 열 선택에 들어 있는지 여부.
        /// </summary>
        // ------------------------------------------------------------
        public bool IsColumnSelected(string columnID)
        {
            return columnID != null && selectedColumnIDs.Contains(columnID);
        }

        // --------------------------------------------------------------------------------
        /// <summary>
        /// <br/> 셀 위치가 받을 선택 표시(바탕)를 계산한다. 범위 외곽선은 본문 위 선택 층이 따로 그린다.
        /// <br/> 셀 하나만 고른 경우는 범위 바탕 없이 활성 표시만 준다.
        /// <br/> 열 선택은 고른 열의 모든 셀이 범위 안이다(데이터 행만 묻는다).
        /// </summary>
        // --------------------------------------------------------------------------------
        public TableGridCellMarks GetCellMarks(int row, int column)
        {
            var marks = TableGridCellMarks.None;
            var inRange = HasCellRange && row >= RowMin && row <= RowMax && column >= ColumnMin && column <= ColumnMax;
            if (inRange && (RowMin != RowMax || ColumnMin != ColumnMax))
            {
                marks |= TableGridCellMarks.InRange;
            }

            if (columnPositions.BinarySearch(column) >= 0)
            {
                marks |= TableGridCellMarks.InRange;
            }

            if (row == ActiveRow && column == ActiveColumn)
            {
                marks |= TableGridCellMarks.Active;
            }

            return marks;
        }

    #endregion

    }
}
