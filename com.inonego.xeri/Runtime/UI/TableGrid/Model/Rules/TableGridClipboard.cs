/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TableGridClipboard.cs
수정일 : 2026-11-23

# 설명
선택과 클립보드 사이의 변환 규칙. 선택을 표시 글자 TSV와 값 묶음으로 만들고(복사),
값 묶음을 셀 변경 목록으로 바꾸며(붙여넣기), 선택 범위를 비우는 변경 목록을 만든다(지우기).
잘라낸 셀·행을 같은 표에 붙여넣으면 붙여넣기와 원본 비우기를 한 변경 목록으로 만든다(옮기기).
클립보드 행을 새 행 값으로 바꾼다(복사한 행 삽입).
값 해석·검증은 열의 값 규칙(ToText·TryParse·Validate·CopyValue·ClearValue)이 맡는다.

# 특이사항, 제약사항
- TSV 읽기·쓰기(큰따옴표 규칙)는 TableGridTsv가 맡고, 계획 형식은 TableGridClipPlans, 값 묶음은 TableGridClipboardBundle이다.
- 복사는 시스템 클립보드에 TSV(값 규칙 ToText)를 넣고, 표는 셀마다 값 규칙 CopyValue로 복사한 값 묶음과 그 TSV,
  복사한 자리(행·열 ID)를 보관한다. 열 이름 줄은 TSV에만 들고 값 묶음에는 들지 않으므로 되붙이면 그 줄은 빠진다.
- 붙여넣기 입력은 값 묶음 하나다. 클립보드 글자가 마지막 복사의 TSV와 같으면 그 값 묶음(형식이 맞지 않으면 거부 셀),
  다르면 글자에서 읽은 묶음(FromText)이라 열마다 TryParse로 읽는다(글자를 읽을 수 없는 열은 건너뛴다).
  값 묶음 값은 셀마다 다시 복사해 요청한다.
- None 열과 읽기 전용 열은 복사에는 들어가고 비우기·덮어쓰기는 건너뛴다(안내 문구 없음).
- Detail 열은 값 묶음만 받고(값 통째 교체) 비우지 않는다. 지우기는 값 규칙이 CanClear일 때만 ClearValue를 요청한다.
- 붙여넣기는 셀 선택이면 범위의 좌상단부터, 행 선택이면 고른 행의 첫 열부터 채운다. 값이 하나뿐이면 선택 전체를 그 값으로 채운다.
- 여러 행을 고른 행 선택은 고른 행에 차례로 채운다(사이에 고르지 않은 행이 있어도 건너뛴다).
- 열 선택은 모든 데이터 행 × 고른 열(보기 순서, 떨어진 열도 이어 붙임)이 대상이다. 붙여넣기는 첫 데이터 행부터 고른 열에 차례로
  채우고(원본이 더 넓으면 마지막 고른 열 뒤로 잇는다), 값이 하나면 고른 열 전체를 채운다. 잘라내기는 이어진 열일 때만 된다.
- 옮기기는 값이 실제로 들어간 원본 셀만 비운다. 새 값으로 덮인 칸·거부된 칸, 결과에서 빠진 행, 비울 수 없는 열은 그대로 둔다.
- 행 삽입은 클립보드 행마다 첫 표시 열부터 값을 읽는다(행 선택 붙여넣기와 같은 자리 규칙). 받지 않는 열·맞지 않는 값은 빼고 센다.
- 잘못된 값만 거부 셀로 강조한다. 그룹 머리 행은 원본 행을 소비하지 않고 건너뛴다.
- 대상 셀 수가 상한을 넘으면 일부만 바꾸지 않도록 전체를 거부한다. 열 선택은 복사도 같은 상한으로 거부한다.
- 소스가 결과 안의 모든 행을 바로 주므로 범위 전체를 처리한다.
데이터는 바꾸지 않고 변경 목록만 만든다. UI Toolkit에 의존하지 않는다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace inonego.Xeri.UI.TableGrid
{
    // ============================================================
    /// <summary>
    /// 선택과 클립보드 사이의 변환 규칙.
    /// </summary>
    // ============================================================
    internal static class TableGridClipboard
    {

    #region 필드

        private const int MAX_CELLS = 100000;

        // 클립보드 셀을 열 값으로 읽은 결과.
        private enum ReadResult
        {
            Value,
            Skipped,
            Invalid,
        }

    #endregion

    #region 복사

        // --------------------------------------------------------------------------------
        /// <summary>
        /// <br/> 선택을 TSV와 값 묶음으로 만든다. 복사할 값이 없으면 null이다.
        /// <br/> includeHeaders이면 TSV 첫 줄에 열 이름을 넣는다(값 묶음 행에는 들지 않는다).
        /// </summary>
        // --------------------------------------------------------------------------------
        public static TableGridClipboardBundle BuildSelection(TableGridModel model, bool includeHeaders)
        {
            var columns = model.Columns.VisibleColumns;
            var rows = new List<int>();
            var targetColumns = new List<int>();
            if (!CollectArea(model, rows, targetColumns) || rows.Count == 0 || targetColumns.Count == 0)
            {
                return null;
            }

            var builder = new StringBuilder();
            var columnIDs = new List<string>(targetColumns.Count);
            for (var index = 0; index < targetColumns.Count; index++)
            {
                var column = columns[targetColumns[index]];
                columnIDs.Add(column.ID);
                if (includeHeaders)
                {
                    TableGridTsv.Append(builder, column.Title, index == 0);
                }
            }

            if (includeHeaders)
            {
                builder.Append('\n');
            }

            // 셀마다 표시 글자는 TSV로, 값은 값 규칙으로 복사해 값 묶음에 담는다.
            var cells = new List<IReadOnlyList<TableGridClipCell>>(rows.Count);
            var rowIDs = new List<string>(rows.Count);
            foreach (var row in rows)
            {
                model.TryGetDataRowID(row, out var rowID);
                rowIDs.Add(rowID);
                var line = new List<TableGridClipCell>(targetColumns.Count);
                for (var index = 0; index < targetColumns.Count; index++)
                {
                    var cell = ReadCell(model, row, targetColumns[index]);
                    line.Add(cell);
                    TableGridTsv.Append(builder, cell.Text, index == 0);
                }

                cells.Add(line);
                builder.Append('\n');
            }

            // 마지막 줄바꿈은 붙여넣기 대상에서 빈 행이 생기지 않게 뺀다.
            builder.Length--;
            return new TableGridClipboardBundle(builder.ToString(), cells, model.Selection.Current.Kind, rowIDs, columnIDs);
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> 복사할 수 있는지 확인한다. 못 하면 이유를 돌려준다.
        /// <br/> 열 선택은 열 전체라 결과 행 수만큼 커지므로 셀 수 상한을 넘으면 거부한다.
        /// </summary>
        // ----------------------------------------------------------------------
        public static string CheckCopy(TableGridModel model)
        {
            var selection = model.Selection;
            if (selection.Current.Kind != TableGridSelectionKind.Column)
            {
                return null;
            }

            return IsWithinLimit(model.Rows.RowCount, selection.ColumnPositions.Count) ? null : "복사 범위가 최대 셀 수를 넘었습니다";
        }

        private static TableGridClipCell ReadCell(TableGridModel model, int rowIndex, int columnIndex)
        {
            if (!model.TryGetCell(rowIndex, columnIndex, out var column, out var row))
            {
                return new TableGridClipCell(string.Empty);
            }

            var text = column.GetText(row.Item);
            return column.TryGetValue(row.Item, out var value)
                ? new TableGridClipCell(text, column.CopyValue(value))
                : new TableGridClipCell(text);
        }

    #endregion

    #region 붙여넣기

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> 값 묶음을 선택 위치의 셀 변경 계획으로 바꾼다.
        /// <br/> 값이 있는 셀은 형식이 맞지 않으면 거부 셀이 되고, 글자만 있는 셀은 열마다 값 규칙 TryParse로 읽는다.
        /// </summary>
        // ----------------------------------------------------------------------
        public static TableGridEditPlan BuildPaste(TableGridModel model, TableGridClipboardBundle bundle)
        {
            return PlanPaste(model, bundle.Rows, null);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> 셀 행을 선택 위치의 셀 변경 계획으로 바꾼다.
        /// <br/> 셀 선택이 없거나 범위가 상한을 넘으면 Error가 채워진다.
        /// <br/> placed가 있으면 값이 들어간 원본 셀 위치(행, 열)를 담는다.
        /// </summary>
        // ------------------------------------------------------------
        private static TableGridEditPlan PlanPaste
        (
            TableGridModel model, IReadOnlyList<IReadOnlyList<TableGridClipCell>> source,
            ISet<(int Row, int Column)> placed
        )
        {
            var plan = new TableGridEditPlan();
            var selection = model.Selection;
            var columns = model.Columns.VisibleColumns;
            var kind = selection.Current.Kind;
            var selectedRows = kind == TableGridSelectionKind.Row ? model.CollectSelectedRowPositions() : null;
            var hasTarget = kind switch
            {
                TableGridSelectionKind.Row => selectedRows.Count > 0,
                TableGridSelectionKind.Column => selection.ColumnPositions.Count > 0,
                _ => selection.HasCellRange,
            };
            if (!hasTarget || source.Count == 0 || columns.Count == 0)
            {
                plan.Error = "붙여넣을 셀이나 행을 먼저 고르세요";
                return plan;
            }

            // 행 선택이면 고른 행의 첫 열부터, 셀 선택이면 범위의 좌상단부터, 열 선택이면 고른 열의 첫 데이터 행부터 채운다.
            var fill = source.Count == 1 && source[0].Count == 1;
            var sourceWidth = 1;
            foreach (var row in source)
            {
                sourceWidth = Math.Max(sourceWidth, row.Count);
            }

            var startRow = kind switch
            {
                TableGridSelectionKind.Row => selectedRows[0],
                TableGridSelectionKind.Column => 0,
                _ => selection.RowMin,
            };
            var fillEndRow = kind switch
            {
                TableGridSelectionKind.Row => startRow,
                TableGridSelectionKind.Column => int.MaxValue,
                _ => selection.RowMax,
            };
            var targetColumns = CollectPasteColumns(model, kind, sourceWidth, fill);
            var targetRows = CollectPasteRows(model, selectedRows, startRow, fillEndRow, source.Count, fill);
            if (!IsWithinLimit(Math.Max(1, targetRows.Count), targetColumns.Count))
            {
                plan.Error = "붙여넣기 범위가 최대 셀 수를 넘었습니다";
                return plan;
            }

            if (!fill)
            {
                for (var index = targetRows.Count; index < source.Count; index++)
                {
                    plan.Outside += source[index].Count;
                }
            }

            for (var target = 0; target < targetRows.Count; target++)
            {
                model.TryGetDataRowID(targetRows[target], out var rowID);
                var sourceRow = fill ? 0 : target;
                var values = source[sourceRow];
                for (var index = 0; index < targetColumns.Count; index++)
                {
                    var valueColumn = fill ? 0 : index;
                    if (valueColumn >= values.Count)
                    {
                        continue;
                    }

                    var column = targetColumns[index];
                    if (column >= columns.Count)
                    {
                        plan.Outside++;
                        continue;
                    }

                    if (AddEdit(plan, rowID, columns[column], values[valueColumn]))
                    {
                        placed?.Add((sourceRow, valueColumn));
                    }
                }
            }

            return plan;
        }

        // --------------------------------------------------------------------------------
        /// <summary>
        /// <br/> 붙여넣을 표시 열 위치를 원본 열 순서대로 모은다.
        /// <br/> 표 끝을 넘는 위치도 담아 범위 밖으로 센다.
        /// <br/> 채우기면 선택의 열 전체(행 선택은 모든 열), 아니면 시작 열부터 원본 폭만큼이다.
        /// <br/> 열 선택은 고른 열을 보기 순서로 쓰고, 원본이 더 넓으면 마지막 고른 열 뒤로 잇는다.
        /// </summary>
        // --------------------------------------------------------------------------------
        private static List<int> CollectPasteColumns(TableGridModel model, TableGridSelectionKind kind, int sourceWidth, bool fill)
        {
            var selection = model.Selection;
            var targetColumns = new List<int>();
            if (kind == TableGridSelectionKind.Column)
            {
                var picked = selection.ColumnPositions;
                var count = fill ? picked.Count : sourceWidth;
                var last = picked[picked.Count - 1];
                for (var index = 0; index < count; index++)
                {
                    targetColumns.Add(index < picked.Count ? picked[index] : last + index - picked.Count + 1);
                }

                return targetColumns;
            }

            var start = kind == TableGridSelectionKind.Row ? 0 : selection.ColumnMin;
            var end = start + sourceWidth - 1;
            if (fill)
            {
                end = kind == TableGridSelectionKind.Row ? model.Columns.VisibleColumns.Count - 1 : selection.ColumnMax;
            }

            AddColumnRange(targetColumns, start, end);
            return targetColumns;
        }

        // --------------------------------------------------------------------------------
        /// <summary>
        /// <br/> 붙여넣을 행 위치를 고른다.
        /// <br/> 여러 행을 고른 행 선택이면 고른 행에 차례로 채우고, 남는 원본 행은 버린다.
        /// <br/> 그 밖에는 시작 행부터 원본 행 수만큼(채우기면 fillEndRow까지) 아래로 채운다.
        /// </summary>
        // --------------------------------------------------------------------------------
        private static List<int> CollectPasteRows
        (
            TableGridModel model, List<int> selectedRows, int startRow, int fillEndRow, int sourceRows, bool fill
        )
        {
            if (selectedRows != null && selectedRows.Count > 1)
            {
                return fill ? selectedRows : selectedRows.GetRange(0, Math.Min(sourceRows, selectedRows.Count));
            }

            if (!fill)
            {
                return model.CollectDataRowPositions(startRow, int.MaxValue, sourceRows);
            }

            return model.CollectDataRowPositions(startRow, fillEndRow);
        }

        private static bool AddEdit(TableGridEditPlan plan, string rowID, TableGridColumn column, TableGridClipCell cell)
        {
            switch (ReadValue(column, cell, out var value))
            {
                case ReadResult.Skipped:
                    // 읽기 전용 셀은 원래 값을 그대로 두는 것이 의도이므로 강조하지 않고 개수만 센다(행 전체 붙여넣기).
                    plan.ReadOnly++;
                    return false;
                case ReadResult.Invalid:
                    Reject(plan, rowID, column);
                    return false;
            }

            plan.Edits.Add(new TableGridEditIntent(rowID, column.ID, value));
            return true;
        }

    #endregion

    #region 잘라내기와 옮기기

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> 잘라낼 수 있는지 확인한다. 못 하면 이유를 돌려준다.
        /// <br/> 열 선택은 이어진 열일 때만 잘라낼 수 있다.
        /// </summary>
        // ----------------------------------------------------------------------
        public static string CheckCut(TableGridModel model)
        {
            var selection = model.Selection;
            switch (selection.Current.Kind)
            {
                case TableGridSelectionKind.Row:
                    return selection.Current.RowIDs.Count > 0 ? null : "잘라낼 행을 먼저 고르세요";
                case TableGridSelectionKind.Column:
                    return selection.IsColumnRun ? null : "떨어진 열은 잘라낼 수 없습니다. 이어진 열을 고르세요";
                default:
                    return selection.HasCellRange ? null : "잘라낼 셀을 먼저 고르세요";
            }
        }

        // --------------------------------------------------------------------------------
        /// <summary>
        /// <br/> 같은 표에서 잘라낸 값 묶음을 선택 위치로 옮기는 계획을 만든다.
        /// <br/> 붙여넣기 변경 뒤에, 값이 실제로 들어간 원본 셀을 비우는 변경을 잇는다.
        /// <br/> 새 값으로 덮이거나 거부된 칸, 결과에서 빠진 행, 비울 수 없는 열의 원본은 그대로 둔다.
        /// </summary>
        // --------------------------------------------------------------------------------
        public static TableGridEditPlan BuildMove(TableGridModel model, TableGridClipboardBundle bundle)
        {
            var placed = new HashSet<(int Row, int Column)>();
            var plan = PlanPaste(model, bundle.Rows, placed);
            if (plan.Error != null)
            {
                return plan;
            }

            var targets = new HashSet<TableGridCellAddress>(plan.Rejected);
            foreach (var edit in plan.Edits)
            {
                targets.Add(new TableGridCellAddress(edit.RowID, edit.ColumnID));
            }

            foreach (var (row, column) in placed)
            {
                var rowID = bundle.SourceRowIDs[row];
                var columnID = bundle.SourceColumnIDs[column];
                var target = model.Columns.Find(columnID);
                var overwritten = targets.Contains(new TableGridCellAddress(rowID, columnID));
                if (overwritten || model.Rows.IndexOf(rowID) < 0 || target == null || !CanClear(target))
                {
                    continue;
                }

                plan.Edits.Add(new TableGridEditIntent(rowID, columnID, target.ClearValue));
            }

            return plan;
        }

    #endregion

    #region 행 삽입

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> 값 묶음을 새 행 값으로 바꾼다(복사한 행 삽입).
        /// <br/> 값이 있는 셀은 형식이 맞아야 받고, 글자만 있는 셀은 열마다 값 규칙 TryParse로 읽는다.
        /// </summary>
        // ------------------------------------------------------------
        public static TableGridInsertPlan BuildInsert(TableGridModel model, TableGridClipboardBundle bundle)
        {
            var source = bundle.Rows;
            var plan = new TableGridInsertPlan();
            var columns = model.Columns.VisibleColumns;
            var sourceWidth = 0;
            foreach (var row in source)
            {
                sourceWidth = Math.Max(sourceWidth, row.Count);
            }

            if (source.Count == 0 || columns.Count == 0)
            {
                plan.Error = "넣을 행이 없습니다";
                return plan;
            }

            if (!IsWithinLimit(source.Count, sourceWidth))
            {
                plan.Error = "삽입 범위가 최대 셀 수를 넘었습니다";
                return plan;
            }

            // 행 선택 붙여넣기처럼 첫 표시 열부터 차례로 채운다.
            foreach (var row in source)
            {
                var values = new Dictionary<string, object>(StringComparer.Ordinal);
                for (var index = 0; index < row.Count; index++)
                {
                    if (index >= columns.Count)
                    {
                        plan.Outside++;
                        continue;
                    }

                    var column = columns[index];
                    switch (ReadValue(column, row[index], out var value))
                    {
                        case ReadResult.Value:
                            values[column.ID] = value;
                            break;
                        case ReadResult.Skipped:
                            plan.ReadOnly++;
                            break;
                        default:
                            plan.Invalid++;
                            break;
                    }
                }

                plan.Rows.Add(values);
            }

            return plan;
        }

    #endregion

    #region 지우기

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> 선택 범위(열 선택이면 그 열 전체)의 셀을 비우는 계획을 만든다.
        /// <br/> 값은 값 규칙의 ClearValue다.
        /// <br/> 읽기 전용·Detail 열과 지우기를 받지 않는 값 규칙의 열은 건너뛴다.
        /// </summary>
        // ----------------------------------------------------------------------
        public static TableGridEditPlan BuildClear(TableGridModel model)
        {
            var plan = new TableGridEditPlan();
            var rows = new List<int>();
            var targetColumns = new List<int>();

            // 행 선택의 Delete는 행 삭제라 셀을 비우지 않는다.
            if (model.Selection.Current.Kind == TableGridSelectionKind.Row || !CollectArea(model, rows, targetColumns))
            {
                plan.Error = "지울 셀을 먼저 고르세요";
                return plan;
            }

            if (!IsWithinLimit(rows.Count, targetColumns.Count))
            {
                plan.Error = "지우기 범위가 최대 셀 수를 넘었습니다";
                return plan;
            }

            var columns = model.Columns.VisibleColumns;
            foreach (var row in rows)
            {
                model.TryGetDataRowID(row, out var rowID);
                foreach (var column in targetColumns)
                {
                    var target = columns[column];
                    if (CanClear(target))
                    {
                        plan.Edits.Add(new TableGridEditIntent(rowID, target.ID, target.ClearValue));
                    }
                    else
                    {
                        plan.ReadOnly++;
                    }
                }
            }

            return plan;
        }

        private static bool CanClear(TableGridColumn column)
        {
            return column.Editable && column.Edit.Kind != TableGridCellEditKind.Detail && column.Format.CanClear;
        }

    #endregion

    #region 셀 값 읽기

        // --------------------------------------------------------------------------------
        /// <summary>
        /// <br/> 클립보드 셀 하나를 열 값으로 읽는다(붙여넣기·행 삽입 공용).
        /// <br/> 읽기 전용 열과 그 값을 받지 않는 열은 Skipped, 맞지 않는 값은 Invalid다.
        /// </summary>
        // --------------------------------------------------------------------------------
        private static ReadResult ReadValue(TableGridColumn column, TableGridClipCell cell, out object value)
        {
            value = null;
            if (!column.Editable)
            {
                return ReadResult.Skipped;
            }

            if (cell.HasValue)
            {
                // 값이 있는 셀은 형식·검증이 맞을 때만 받고, 같은 값을 여러 셀에 넣어도 공유하지 않게 다시 복사한다.
                if (!column.TryValidateValue(cell.Value, out _))
                {
                    return ReadResult.Invalid;
                }

                value = column.CopyValue(cell.Value);
                return ReadResult.Value;
            }

            // 글자만 있는 셀은 글자를 읽는 열만 받는다. Detail 열은 값이 있는 셀만 받는다.
            if (column.Edit.Kind == TableGridCellEditKind.Detail || !column.Format.CanParse)
            {
                return ReadResult.Skipped;
            }

            return column.TryParseText(cell.Text, out value, out _) ? ReadResult.Value : ReadResult.Invalid;
        }

        private static void Reject(TableGridEditPlan plan, string rowID, TableGridColumn column)
        {
            plan.Invalid++;
            plan.Rejected.Add(new TableGridCellAddress(rowID, column.ID));
        }

    #endregion

    #region 범위

        // --------------------------------------------------------------------------------
        /// <summary>
        /// <br/> 선택이 덮는 데이터 행 위치와 표시 열 위치(화면 순서)를 모은다(복사·지우기).
        /// <br/> 행 선택은 고른 행의 모든 열, 셀 선택은 범위, 열 선택은 모든 데이터 행의 고른 열이다.
        /// <br/> 덮을 선택이 없으면(셀 범위가 보이지 않으면) false다.
        /// </summary>
        // --------------------------------------------------------------------------------
        private static bool CollectArea(TableGridModel model, List<int> rows, List<int> columns)
        {
            var selection = model.Selection;
            switch (selection.Current.Kind)
            {
                case TableGridSelectionKind.Row:
                    rows.AddRange(model.CollectSelectedRowPositions());
                    AddColumnRange(columns, 0, model.Columns.VisibleColumns.Count - 1);
                    return true;
                case TableGridSelectionKind.Column:
                    rows.AddRange(model.CollectDataRowPositions(0, int.MaxValue));
                    columns.AddRange(selection.ColumnPositions);
                    return true;
                case TableGridSelectionKind.Cell when selection.HasCellRange:
                    rows.AddRange(model.CollectDataRowPositions(selection.RowMin, selection.RowMax));
                    AddColumnRange(columns, selection.ColumnMin, selection.ColumnMax);
                    return true;
                default:
                    return false;
            }
        }

        private static void AddColumnRange(List<int> columns, int first, int last)
        {
            for (var column = first; column <= last; column++)
            {
                columns.Add(column);
            }
        }

        private static bool IsWithinLimit(int rowCount, int columnCount)
        {
            return (long)rowCount * columnCount <= MAX_CELLS;
        }

    #endregion

    }
}
