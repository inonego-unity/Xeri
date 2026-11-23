/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TableGridNavigator.cs
수정일 : 2026-11-22

# 설명
키보드 이동 규칙. 현재 활성 셀 위치와 이동 종류로 다음 활성 셀 위치를 계산한다.
그룹 머리·요약 행은 이동 방향으로 건너뛰고, 데이터 행에만 멈춘다.

# 특이사항, 제약사항
소스 결과 안의 모든 행으로 움직인다.
페이지·처음·끝 이동의 목표 쪽에 데이터 행이 없으면(끝의 요약 행 등) 출발 행 쪽으로 가장 가까운 데이터 행에 멈춘다.
UI Toolkit에 의존하지 않는다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

namespace inonego.Xeri.UI.TableGrid
{
    // ============================================================
    /// <summary>
    /// 활성 셀 이동 종류.
    /// </summary>
    // ============================================================
    internal enum TableGridMove
    {
        Up,
        Down,
        Left,
        Right,
        Next,
        Previous,
        RowStart,
        RowEnd,
        First,
        Last,
        PageUp,
        PageDown,
    }

    // ============================================================
    /// <summary>
    /// 활성 셀 이동 규칙.
    /// </summary>
    // ============================================================
    internal static class TableGridNavigator
    {

    #region 이동

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> 이동 결과 위치를 계산한다. 움직일 수 있는 데이터 행이 없으면 false다.
        /// <br/> Next·Previous는 행 끝에서 다음·이전 데이터 행으로 넘어간다.
        /// <br/> First·Last는 첫 열·마지막 열로도 옮긴다.
        /// </summary>
        // ----------------------------------------------------------------------
        public static bool TryMove
        (
            TableGridRowSet rows, int columnCount, int row, int column,
            TableGridMove move, int pageRows,
            out int targetRow, out int targetColumn
        )
        {
            targetRow = row;
            targetColumn = column;
            if (rows == null || columnCount <= 0 || rows.RowCount <= 0)
            {
                return false;
            }

            var lastRow = rows.RowCount - 1;
            var lastColumn = columnCount - 1;
            var direction = 0;
            var exact = true;

            switch (move)
            {
                case TableGridMove.Up:
                    targetRow = row - 1;
                    direction = -1;
                    break;
                case TableGridMove.Down:
                    targetRow = row + 1;
                    direction = 1;
                    break;
                case TableGridMove.Left:
                    targetColumn = Math.Max(0, column - 1);
                    break;
                case TableGridMove.Right:
                    targetColumn = Math.Min(lastColumn, column + 1);
                    break;
                case TableGridMove.Next:
                    targetColumn = column + 1;
                    if (targetColumn > lastColumn)
                    {
                        targetColumn = 0;
                        targetRow = row + 1;
                        direction = 1;
                    }

                    break;
                case TableGridMove.Previous:
                    targetColumn = column - 1;
                    if (targetColumn < 0)
                    {
                        targetColumn = lastColumn;
                        targetRow = row - 1;
                        direction = -1;
                    }

                    break;
                case TableGridMove.RowStart:
                    targetColumn = 0;
                    break;
                case TableGridMove.RowEnd:
                    targetColumn = lastColumn;
                    break;
                case TableGridMove.First:
                    targetRow = 0;
                    targetColumn = 0;
                    direction = 1;
                    exact = false;
                    break;
                case TableGridMove.Last:
                    targetRow = lastRow;
                    targetColumn = lastColumn;
                    direction = -1;
                    exact = false;
                    break;
                case TableGridMove.PageUp:
                    targetRow = Math.Max(0, row - pageRows);
                    direction = -1;
                    exact = false;
                    break;
                case TableGridMove.PageDown:
                    targetRow = Math.Min(lastRow, row + pageRows);
                    direction = 1;
                    exact = false;
                    break;
                default:
                    return false;
            }

            if (direction != 0)
            {
                var found = FindDataRow(rows, targetRow, direction);
                if (found < 0 && !exact)
                {
                    // 목표 쪽에 데이터 행이 없으면 출발 행 쪽으로 돌아오며 데이터 행을 찾는다.
                    found = FindDataRowToward(rows, targetRow, row);
                }

                if (found < 0)
                {
                    targetRow = row;
                    targetColumn = column;
                    return false;
                }

                targetRow = found;
            }

            return targetRow != row || targetColumn != column;
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> 위치에서 toward 쪽으로, 없으면 반대쪽으로 가장 가까운 데이터 행을 찾는다.
        /// <br/> 데이터 행이 없으면 -1이다.
        /// </summary>
        // ----------------------------------------------------------------------
        public static int FindNearestDataRow(TableGridRowSet rows, int index, int toward)
        {
            var step = toward >= index ? 1 : -1;
            var found = FindDataRow(rows, index, step);

            return found >= 0 ? found : FindDataRow(rows, index - step, -step);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 방향으로 첫 데이터 행을 찾는다. 없으면 -1이다.
        /// </summary>
        // ------------------------------------------------------------
        private static int FindDataRow(TableGridRowSet rows, int start, int direction)
        {
            for (var index = start; index >= 0 && index < rows.RowCount; index += direction)
            {
                if (rows.TryGetDataRow(index, out _))
                {
                    return index;
                }
            }

            return -1;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 목표에서 출발 행 쪽으로 오며 처음 만나는 데이터 행을 찾는다.
        /// </summary>
        // ------------------------------------------------------------
        private static int FindDataRowToward(TableGridRowSet rows, int target, int origin)
        {
            var step = origin >= target ? 1 : -1;
            for (var index = target; index != origin; index += step)
            {
                if (rows.TryGetDataRow(index, out _))
                {
                    return index;
                }
            }

            return -1;
        }

    #endregion

    }
}
