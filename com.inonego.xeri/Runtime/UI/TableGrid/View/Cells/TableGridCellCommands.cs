/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TableGridCellCommands.cs
수정일 : 2026-11-21

# 설명
셀 호스트(셀 안 Inline 컨트롤·셀 뷰)와 편집 처리기가 표에 요청을 보내는 통로.
Inline 값 변경은 검증한 뒤 EditRequested(Inline)로, 상세 편집은 DetailRequested로 나간다.

# 특이사항, 제약사항
요청 가능 여부(표 Access, 연결된 요청 이벤트, 열 편집 방식)는 TableGrid가 정한다.
표는 데이터를 바꾸지 않는다. 검증에 실패한 Inline 값은 요청하지 않고 오류 문구만 보인다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

namespace inonego.Xeri.UI.TableGrid
{
    // ============================================================
    /// <summary>
    /// 셀 호스트·편집 처리기가 표에 요청을 보내는 통로.
    /// </summary>
    // ============================================================
    internal sealed class TableGridCellCommands
    {

    #region 필드

        private readonly Func<TableGridColumn, bool> canEdit;
        private readonly Action<TableGridEditIntent> requestInline;
        private readonly Action<TableGridDetailRequest> requestDetail;
        private readonly Action<string> showError;
        private readonly Action focusTable;

    #endregion

    #region 생성

        // ------------------------------------------------------------
        /// <summary>
        /// 표가 요청 판단과 전달 방법을 넘겨 통로를 만든다.
        /// </summary>
        // ------------------------------------------------------------
        public TableGridCellCommands
        (
            Func<TableGridColumn, bool> canEdit, Action<TableGridEditIntent> requestInline,
            Action<TableGridDetailRequest> requestDetail, Action<string> showError, Action focusTable
        )
        {
            this.canEdit = canEdit ?? throw new ArgumentNullException(nameof(canEdit));
            this.requestInline = requestInline ?? throw new ArgumentNullException(nameof(requestInline));
            this.requestDetail = requestDetail ?? throw new ArgumentNullException(nameof(requestDetail));
            this.showError = showError ?? throw new ArgumentNullException(nameof(showError));
            this.focusTable = focusTable ?? throw new ArgumentNullException(nameof(focusTable));
        }

    #endregion

    #region 요청

        // ------------------------------------------------------------
        /// <summary>
        /// 열의 셀 변경(Detail이면 상세 편집)을 지금 요청할 수 있는지 확인한다.
        /// </summary>
        // ------------------------------------------------------------
        public bool CanEdit(TableGridColumn column)
        {
            return column != null && column.Editable && canEdit(column);
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> Inline 열의 셀 값 변경을 요청한다. 값이 열 형식·검증에 맞지 않으면
        /// <br/> 요청하지 않고 오류를 보인다. 요청했으면 true다.
        /// </summary>
        // ----------------------------------------------------------------------
        public bool RequestInline(string rowID, TableGridColumn column, object value)
        {
            if (string.IsNullOrEmpty(rowID) || column?.Edit.Kind != TableGridCellEditKind.Inline || !CanEdit(column))
            {
                return false;
            }

            if (!column.TryValidateValue(value, out var error))
            {
                ShowError(error);
                return false;
            }

            requestInline(new TableGridEditIntent(rowID, column.ID, value));
            return true;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Detail 열 셀의 상세 편집을 요청한다. 요청했으면 true다.
        /// </summary>
        // ------------------------------------------------------------
        public bool RequestDetail(string rowID, TableGridColumn column)
        {
            if (string.IsNullOrEmpty(rowID) || column?.Edit.Kind != TableGridCellEditKind.Detail || !CanEdit(column))
            {
                return false;
            }

            requestDetail(new TableGridDetailRequest(rowID, column.ID));
            return true;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 편집 오류 문구를 보인다.
        /// </summary>
        // ------------------------------------------------------------
        public void ShowError(string error)
        {
            showError(string.IsNullOrEmpty(error) ? "값을 반영할 수 없습니다" : error);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 포커스를 표로 돌린다(Inline 컨트롤에서 Escape).
        /// </summary>
        // ------------------------------------------------------------
        public void FocusTable()
        {
            focusTable();
        }

    #endregion

    }
}
