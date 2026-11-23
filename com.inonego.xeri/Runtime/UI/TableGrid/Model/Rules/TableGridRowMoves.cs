/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TableGridRowMoves.cs
수정일 : 2026-11-23

# 설명
행 이동 요청에서 "무엇을 어디로 옮길지"를 정하는 규칙. 옮길 행을 모으고(CollectMoving),
한 칸 위·아래·맨 위·맨 아래(TryResolve)나 입력한 데이터 행 번호(TryResolveNumber)로 목표 행과 놓을 자리를 정한다.

# 특이사항, 제약사항
- 옮길 행은 행 선택이면 고른 행, 셀 선택이면 범위의 행(범위가 없으면 활성 행)이며 화면 순서다.
- 목표 행은 옮길 행이 아닌 데이터 행만 쓴다(그룹 머리 행은 건너뛴다). 맨 위·맨 아래는 이미 그 자리면 옮기지 않는다.
- 데이터는 바꾸지 않고 목표만 정한다. 요청 전송·상태 줄 안내는 TableGrid가 맡는다. UI Toolkit에 의존하지 않는다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

namespace inonego.Xeri.UI.TableGrid
{
    // ============================================================
    /// <summary>
    /// 행 이동의 대상 행과 놓을 자리를 정하는 규칙.
    /// </summary>
    // ============================================================
    internal static class TableGridRowMoves
    {

    #region 옮길 행

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> 옮길 행을 화면 순서로 모은다.
        /// <br/> 행 선택이면 고른 행, 셀 선택이면 범위의 행(없으면 활성 행)이다.
        /// </summary>
        // ----------------------------------------------------------------------
        public static List<string> CollectMoving(TableGridModel model)
        {
            if (model.Selection.Current.Kind == TableGridSelectionKind.Row)
            {
                return model.ToRowIDs(model.CollectSelectedRowPositions());
            }

            var (first, last) = GetCellSelectionRows(model);
            return model.ToRowIDs(model.CollectDataRowPositions(first, last));
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 셀 선택이 덮는 행 위치. 범위가 없으면 활성 행 하나다(활성 행이 없으면 -1).
        /// </summary>
        // ------------------------------------------------------------
        public static (int First, int Last) GetCellSelectionRows(TableGridModel model)
        {
            var selection = model.Selection;
            return selection.HasCellRange ? (selection.RowMin, selection.RowMax) : (selection.ActiveRow, selection.ActiveRow);
        }

    #endregion

    #region 목표 정하기

        // --------------------------------------------------------------------------------
        /// <summary>
        /// <br/> 옮길 행(rowIDs)을 move만큼 옮길 때의 목표 행과 놓을 자리를 정한다.
        /// <br/> 위·아래는 옮길 행 바로 밖의 데이터 행, 맨 위·맨 아래는 처음·끝의 데이터 행이다.
        /// <br/> 옮길 곳이 없거나 이미 그 자리면 false다.
        /// </summary>
        // --------------------------------------------------------------------------------
        public static bool TryResolve
        (
            TableGridModel model, IReadOnlyList<string> rowIDs, TableGridRowMove move,
            out string targetRowID, out TableGridMovePlacement placement
        )
        {
            var moving = new HashSet<string>(rowIDs, StringComparer.Ordinal);
            var first = model.Rows.IndexOf(rowIDs[0]);
            var last = model.Rows.IndexOf(rowIDs[rowIDs.Count - 1]);
            int targetIndex;
            switch (move)
            {
                case TableGridRowMove.Up:
                    targetIndex = FindDataRowExcept(model, first - 1, -1, moving, out targetRowID);
                    placement = TableGridMovePlacement.Before;
                    break;
                case TableGridRowMove.Down:
                    targetIndex = FindDataRowExcept(model, last + 1, 1, moving, out targetRowID);
                    placement = TableGridMovePlacement.After;
                    break;
                case TableGridRowMove.Top:
                    targetIndex = FindDataRowExcept(model, 0, 1, moving, out targetRowID);
                    placement = TableGridMovePlacement.Before;
                    targetIndex = targetIndex < first ? targetIndex : -1;
                    break;
                default:
                    targetIndex = FindDataRowExcept(model, model.Rows.RowCount - 1, -1, moving, out targetRowID);
                    placement = TableGridMovePlacement.After;
                    targetIndex = targetIndex > last ? targetIndex : -1;
                    break;
            }

            return targetIndex >= 0;
        }

        // --------------------------------------------------------------------------------
        /// <summary>
        /// <br/> 입력한 데이터 행 번호(행 번호 칸과 같은 번호, 1부터)의 행을 목표로 정한다.
        /// <br/> 옮길 행보다 앞 번호면 그 행 앞에, 아니면 뒤에 놓는다.
        /// <br/> 정할 수 없으면 입력에 보일 안내 문구를, 정했으면 null을 돌려준다.
        /// </summary>
        // --------------------------------------------------------------------------------
        public static string TryResolveNumber
        (
            TableGridModel model, IReadOnlyList<string> rowIDs, string text,
            out string targetRowID, out TableGridMovePlacement placement
        )
        {
            targetRowID = null;
            placement = TableGridMovePlacement.After;

            // 번호는 데이터 행 번호라 그룹 머리 행을 건너뛴 위치를 찾는다.
            var index = int.TryParse(text, out var number) ? model.Rows.FindDataRowByNumber(number) : -1;
            if (index < 0 || !model.TryGetDataRowID(index, out targetRowID))
            {
                return $"1–{model.Rows.DataRowCount:N0} 사이 번호를 입력하세요";
            }

            if (new HashSet<string>(rowIDs, StringComparer.Ordinal).Contains(targetRowID))
            {
                return "옮길 행의 자리로는 옮길 수 없습니다";
            }

            var first = model.Rows.IndexOf(rowIDs[0]);
            placement = index < first ? TableGridMovePlacement.Before : TableGridMovePlacement.After;
            return null;
        }

        private static int FindDataRowExcept
        (
            TableGridModel model, int start, int step, HashSet<string> exclude, out string rowID
        )
        {
            for (var index = start; index >= 0 && index < model.Rows.RowCount; index += step)
            {
                if (model.TryGetDataRowID(index, out rowID) && !exclude.Contains(rowID))
                {
                    return index;
                }
            }

            rowID = null;
            return -1;
        }

    #endregion

    }
}
