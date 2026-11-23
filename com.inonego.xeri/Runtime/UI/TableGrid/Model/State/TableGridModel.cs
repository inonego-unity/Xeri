/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TableGridModel.cs
수정일 : 2026-11-22

# 설명
표 상태의 단일 원천. 열 상태, 소스 행, 선택, 쿼리 표시 상태를 함께 보관한다.
열이나 행이 바뀌면 선택을 규칙대로 남기고 표시 위치를 다시 계산한 뒤 화면에 알린다.

# 특이사항, 제약사항
UI Toolkit에 의존하지 않는다. 화면 갱신은 이벤트를 받은 TableGrid가 맡는다.
쿼리 표시 상태는 보여 주기만 하며 조회는 호스트가 한다.
결과·순서가 바뀔 때 행 선택은 ID로 유지하고 결과에서 빠진 행만 뺀다.
열 선택도 ID로 유지해(열 이동 뒤에도 그 열을 따라감) 숨긴 열만 빼고, 활성 행이 결과에서 빠지면 첫 데이터 행으로 둔다.
셀 선택은 Excel처럼 같은 자리(바뀌기 직전 표시 위치)를 유지하고 줄면 남은 안으로 자른다.
행 이동 요청 뒤 한 번은 셀 선택이 시작·끝 셀의 행을 따라간다(FollowSelectionOnce).
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

namespace inonego.Xeri.UI.TableGrid
{
    // ============================================================
    /// <summary>
    /// 표 상태의 단일 원천.
    /// </summary>
    // ============================================================
    internal sealed class TableGridModel
    {

    #region 상태

        private bool followSelectionOnce;

        // ------------------------------------------------------------
        /// <summary>
        /// 원본 열과 표시 열 상태.
        /// </summary>
        // ------------------------------------------------------------
        public TableGridColumnSet Columns { get; } = new();

        // ------------------------------------------------------------
        /// <summary>
        /// 소스 결과의 행.
        /// </summary>
        // ------------------------------------------------------------
        public TableGridRowSet Rows { get; } = new();

        // ------------------------------------------------------------
        /// <summary>
        /// 선택과 그 표시 위치.
        /// </summary>
        // ------------------------------------------------------------
        public TableGridSelectionState Selection { get; } = new();

        // ------------------------------------------------------------
        /// <summary>
        /// 편집 중인 셀과 거부 표시.
        /// </summary>
        // ------------------------------------------------------------
        public TableGridEditState Edit { get; } = new();

        // ------------------------------------------------------------
        /// <summary>
        /// 헤더·칩에 보이는 쿼리 표시 상태. null로 지정하면 빈 상태다.
        /// </summary>
        // ------------------------------------------------------------
        public TableGridQueryState Query
        {
            get => query;
            set => query = value ?? TableGridQueryState.Empty;
        }

        private TableGridQueryState query = TableGridQueryState.Empty;

    #endregion

    #region 이벤트

        // ------------------------------------------------------------
        /// <summary>
        /// 열 상태가 바뀌었을 때 알린다. 선택 위치는 이미 다시 계산되어 있다.
        /// </summary>
        // ------------------------------------------------------------
        public event Action ColumnsChanged;

        // ------------------------------------------------------------
        /// <summary>
        /// 사용자 폭이나 고정 열 수가 바뀌었을 때 알린다. 선택 위치는 그대로다.
        /// </summary>
        // ------------------------------------------------------------
        public event Action ColumnLayoutChanged;

        // ------------------------------------------------------------
        /// <summary>
        /// 소스 행이 바뀌었을 때 알린다. 선택 위치는 이미 다시 계산되어 있다.
        /// </summary>
        // ------------------------------------------------------------
        public event Action RowsChanged;

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> 열·행이 바뀌어 선택 규칙이 선택을 바꿨을 때 이전 선택과 함께 알린다.
        /// <br/> ColumnsChanged·RowsChanged보다 먼저 온다.
        /// </summary>
        // ----------------------------------------------------------------------
        public event Action<TableGridSelection> SelectionAdjusted;

    #endregion

    #region 생성

        // ------------------------------------------------------------
        /// <summary>
        /// 열·행 변경에 선택 규칙과 위치 재계산을 연결한다.
        /// </summary>
        // ------------------------------------------------------------
        public TableGridModel()
        {
            Columns.Changed += () =>
            {
                RetainSelection(false);
                ColumnsChanged?.Invoke();
            };

            Columns.LayoutChanged += () => ColumnLayoutChanged?.Invoke();

            Rows.Changed += () =>
            {
                var follow = followSelectionOnce;
                followSelectionOnce = false;
                RetainSelection(follow);
                RowsChanged?.Invoke();
            };
        }

    #endregion

    #region 변경

        // ------------------------------------------------------------
        /// <summary>
        /// 선택을 바꾸고 표시 위치를 계산한다. 같은 선택이면 false를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public bool SetSelection(TableGridSelection selection, out TableGridSelection previous)
        {
            followSelectionOnce = false;
            if (!Selection.Set(selection, out previous))
            {
                return false;
            }

            ProjectSelection();
            return true;
        }

        // --------------------------------------------------------------------------------
        /// <summary>
        /// <br/> 다음 행 변경 한 번은 셀 선택이 같은 자리 대신 시작·끝 셀의 행을 따라가게 한다.
        /// <br/> 행 이동을 요청할 때 부른다. 그 사이 선택을 바꾸면 취소된다.
        /// </summary>
        // --------------------------------------------------------------------------------
        public void FollowSelectionOnce()
        {
            followSelectionOnce = true;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> 호스트가 건 선택에 맞춰 키보드 이동의 기준을 맞춘다.
        /// <br/> 행 선택은 기준 행을, 열 선택은 기준 열과 활성 행을 맞춘다.
        /// </summary>
        // ------------------------------------------------------------
        public void AlignAnchors(TableGridSelection selection)
        {
            Selection.AlignAnchor(selection);
            if (selection != null && selection.Kind == TableGridSelectionKind.Column)
            {
                AlignColumnActiveRow();
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> 열 선택을 시작할 기준 열을 정한다. 선택을 바꾸기 전에 부른다.
        /// <br/> 활성 행은 지금 활성 셀의 행을 잇고, 없으면 첫 데이터 행이다.
        /// </summary>
        // ------------------------------------------------------------
        public void SetColumnAnchor(string columnID)
        {
            Selection.ColumnAnchorID = columnID;
            Selection.ColumnActiveRowID = TryGetDataRowID(Selection.ActiveRow, out var rowID) ? rowID : null;
            AlignColumnActiveRow();
        }

        private void AlignColumnActiveRow()
        {
            // 열 선택의 활성 행이 결과에 없으면 첫 데이터 행으로 둔다. 데이터 행이 없으면 비운다.
            if (Rows.TryGetDataRow(Rows.IndexOf(Selection.ColumnActiveRowID), out _))
            {
                return;
            }

            var firstRow = TableGridNavigator.FindNearestDataRow(Rows, 0, 0);
            Selection.ColumnActiveRowID = TryGetDataRowID(firstRow, out var rowID) ? rowID : null;
        }

        private void ProjectSelection()
        {
            Selection.Project(Rows.IndexOf, Columns.GetVisibleIndex);
        }

    #endregion

    #region 선택 규칙

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> 열·행이 바뀐 뒤 선택을 규칙대로 남기고 위치를 다시 계산한다.
        /// <br/> 선택이 바뀌었으면 SelectionAdjusted를 알린다.
        /// </summary>
        // ------------------------------------------------------------
        private void RetainSelection(bool followIDs)
        {
            var current = Selection.Current;
            var next = current.Kind switch
            {
                TableGridSelectionKind.Row => RetainRows(current),
                TableGridSelectionKind.Column => RetainColumns(current),
                TableGridSelectionKind.Cell => followIDs && HasCellEndpoints(current) ? current : RetainCellPlace(current),
                _ => current,
            };

            if (ReferenceEquals(next, current) || !Selection.Set(next, out var previous))
            {
                // 열 선택은 그대로여도 활성 행이 결과에서 빠졌을 수 있다.
                if (current.Kind == TableGridSelectionKind.Column)
                {
                    AlignColumnActiveRow();
                }

                ProjectSelection();
                return;
            }

            AlignAnchors(next);
            ProjectSelection();
            SelectionAdjusted?.Invoke(previous);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 행 선택에서 결과에 없는 행을 뺀다. 모두 남으면 같은 선택이다.
        /// </summary>
        // ------------------------------------------------------------
        private TableGridSelection RetainRows(TableGridSelection current)
        {
            var kept = KeepIDs(current.RowIDs, rowID => Rows.IndexOf(rowID) >= 0);
            if (kept == null)
            {
                return current;
            }

            return kept.Count > 0 ? TableGridSelection.Rows(kept) : TableGridSelection.None;
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> 열 선택에서 숨긴(표시 열에 없는) 열을 뺀다. 모두 남으면 같은 선택이다.
        /// <br/> ID로 남기므로 열 순서가 바뀌어도 고른 열을 따라간다.
        /// </summary>
        // ----------------------------------------------------------------------
        private TableGridSelection RetainColumns(TableGridSelection current)
        {
            var kept = KeepIDs(current.ColumnIDs, columnID => Columns.GetVisibleIndex(columnID) >= 0);
            if (kept == null)
            {
                return current;
            }

            return kept.Count > 0 ? TableGridSelection.Columns(kept) : TableGridSelection.None;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> 조건에 맞는 ID만 순서대로 남긴다.
        /// <br/> 모두 남으면 새 목록을 만들지 않고 null을 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        private static List<string> KeepIDs(IReadOnlyList<string> ids, Predicate<string> keep)
        {
            List<string> kept = null;
            for (var index = 0; index < ids.Count; index++)
            {
                var id = ids[index];
                if (keep(id))
                {
                    kept?.Add(id);
                    continue;
                }

                // 처음 빠진 ID를 만나면 그 앞까지를 담아 둔다.
                if (kept == null)
                {
                    kept = new List<string>(ids.Count);
                    for (var before = 0; before < index; before++)
                    {
                        kept.Add(ids[before]);
                    }
                }
            }

            return kept;
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> 셀 선택을 바뀌기 직전의 같은 자리로 다시 잡는다.
        /// <br/> 행·열 수가 줄면 남은 안으로 자르고,
        /// <br/> 데이터 행이 아니면 반대쪽 끝 방향의 가까운 데이터 행으로 옮긴다.
        /// <br/> 바뀌기 전에 표시 위치가 없었으면(호스트가 건 선택)
        /// <br/> 시작·끝 셀을 찾을 수 있을 때만 남긴다.
        /// </summary>
        // ----------------------------------------------------------------------
        private TableGridSelection RetainCellPlace(TableGridSelection current)
        {
            if (!Selection.HasCellRange)
            {
                return HasCellEndpoints(current) ? current : TableGridSelection.None;
            }

            var rowCount = Rows.RowCount;
            var columnCount = Columns.VisibleColumns.Count;
            if (rowCount == 0 || columnCount == 0)
            {
                return TableGridSelection.None;
            }

            var anchorRow = Math.Min(Selection.AnchorRow, rowCount - 1);
            var focusRow = Math.Min(Selection.ActiveRow, rowCount - 1);
            var anchorDataRow = TableGridNavigator.FindNearestDataRow(Rows, anchorRow, focusRow);
            var focusDataRow = TableGridNavigator.FindNearestDataRow(Rows, focusRow, anchorRow);
            var anchorColumn = Math.Min(Selection.AnchorColumn, columnCount - 1);
            var focusColumn = Math.Min(Selection.ActiveColumn, columnCount - 1);
            if (!TryGetCellAddress(anchorDataRow, anchorColumn, out var anchor)
                || !TryGetCellAddress(focusDataRow, focusColumn, out var focus))
            {
                return TableGridSelection.None;
            }

            if (anchor == current.Anchor && focus == current.Focus)
            {
                return current;
            }

            return TableGridSelection.CellRange(anchor, focus);
        }

        private bool HasCellEndpoints(TableGridSelection selection)
        {
            return Rows.IndexOf(selection.Anchor.RowID) >= 0 && Rows.IndexOf(selection.Focus.RowID) >= 0
                && Columns.GetVisibleIndex(selection.Anchor.ColumnID) >= 0
                && Columns.GetVisibleIndex(selection.Focus.ColumnID) >= 0;
        }

    #endregion

    #region 위치와 주소

        // ------------------------------------------------------------
        /// <summary>
        /// 행·표시 열 위치의 셀 주소를 만든다. 데이터 행이 아니면 false다.
        /// </summary>
        // ------------------------------------------------------------
        public bool TryGetCellAddress(int rowIndex, int columnIndex, out TableGridCellAddress address)
        {
            address = default;
            var columns = Columns.VisibleColumns;
            if (columnIndex < 0 || columnIndex >= columns.Count || !TryGetDataRowID(rowIndex, out var rowID))
            {
                return false;
            }

            address = new TableGridCellAddress(rowID, columns[columnIndex].ID);
            return true;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> 행·표시 열 위치의 셀 열과 행 항목을 찾는다.
        /// <br/> 데이터 행이 아니면 false다.
        /// </summary>
        // ------------------------------------------------------------
        public bool TryGetCell(int rowIndex, int columnIndex, out TableGridColumn column, out TableGridRow row)
        {
            column = null;
            row = null;
            var columns = Columns.VisibleColumns;
            if (columnIndex < 0 || columnIndex >= columns.Count || !Rows.TryGetDataRow(rowIndex, out row))
            {
                return false;
            }

            column = columns[columnIndex];
            return true;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> 행·열 ID의 셀 열과 행 항목을 찾는다.
        /// <br/> 결과에 없거나 데이터 행이 아니거나 모르는 열이면 false다.
        /// </summary>
        // ------------------------------------------------------------
        public bool TryGetCell(string rowID, string columnID, out TableGridColumn column, out TableGridRow row)
        {
            row = null;
            column = Columns.Find(columnID);
            return column != null && Rows.TryGetDataRow(Rows.IndexOf(rowID), out row);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> 행·열 ID의 셀 값을 읽는다(편집·클립보드 경계라 박싱한다).
        /// <br/> 결과에 없거나 데이터 행이 아니거나 모르는 열이면 false다.
        /// </summary>
        // ------------------------------------------------------------
        public bool TryGetCellValue(string rowID, string columnID, out object value)
        {
            value = null;
            return TryGetCell(rowID, columnID, out var column, out var row) && column.TryGetValue(row.Item, out value);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 위치의 데이터 행 ID를 찾는다. 데이터 행이 아니면 false다.
        /// </summary>
        // ------------------------------------------------------------
        public bool TryGetDataRowID(int rowIndex, out string rowID)
        {
            var found = Rows.TryGetDataRow(rowIndex, out var row);
            rowID = row?.ID;
            return found;
        }

    #endregion

    #region 행 모으기

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> 행 선택의 고른 행 위치를 화면 순서로 모은다(고른 행 수만큼만 찾는다).
        /// <br/> 결과에서 빠졌거나 데이터 행이 아닌 행은 뺀다.
        /// </summary>
        // ----------------------------------------------------------------------
        public List<int> CollectSelectedRowPositions()
        {
            var rowIDs = Selection.Current.RowIDs;
            var positions = new List<int>(rowIDs.Count);
            foreach (var rowID in rowIDs)
            {
                var index = Rows.IndexOf(rowID);
                if (Rows.TryGetDataRow(index, out _))
                {
                    positions.Add(index);
                }
            }

            positions.Sort();
            return positions;
        }

        // --------------------------------------------------------------------------------
        /// <summary>
        /// <br/> 두 위치 사이(순서 무관, 양 끝 포함)의 데이터 행 위치를 화면 순서로 모은다.
        /// <br/> 결과 밖은 잘라 내고, maxCount개를 모으면 멈춘다. 그룹 머리 행은 건너뛴다.
        /// </summary>
        // --------------------------------------------------------------------------------
        public List<int> CollectDataRowPositions(int from, int to, int maxCount = int.MaxValue)
        {
            var positions = new List<int>();
            var first = Math.Max(0, Math.Min(from, to));
            var last = Math.Min(Math.Max(from, to), Rows.RowCount - 1);
            for (var index = first; index <= last && positions.Count < maxCount; index++)
            {
                if (Rows.TryGetDataRow(index, out _))
                {
                    positions.Add(index);
                }
            }

            return positions;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 데이터 행 위치 목록을 같은 순서의 행 ID 목록으로 바꾼다.
        /// </summary>
        // ------------------------------------------------------------
        public List<string> ToRowIDs(IReadOnlyList<int> positions)
        {
            var rowIDs = new List<string>(positions.Count);
            foreach (var index in positions)
            {
                if (TryGetDataRowID(index, out var rowID))
                {
                    rowIDs.Add(rowID);
                }
            }

            return rowIDs;
        }

    #endregion

    }
}
