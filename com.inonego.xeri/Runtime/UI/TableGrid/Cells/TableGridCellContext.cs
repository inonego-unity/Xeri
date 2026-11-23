/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TableGridCellContext.cs
수정일 : 2026-11-24

# 설명
셀 뷰·편집기가 받는 셀 문맥과, 셀 안에서 값 변경·확정·상세 편집을 요청하는 셀 호스트 계약을 정의한다.

# 특이사항, 제약사항
문맥은 값을 담지 않는다(값은 형식 있는 Bind 인수로 전달한다).
셀 호스트는 표가 셀마다 두며, 요청은 TableGrid의 EditRequested·DetailRequested로 나간다.
표는 데이터를 바꾸지 않는다. 호스트가 반영한 값은 ApplyEditResults·Refresh로 셀에 다시 연결된다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

namespace inonego.Xeri.UI.TableGrid
{
    // ============================================================
    /// <summary>
    /// 셀 안에서 값 변경을 요청하는 셀 호스트.
    /// </summary>
    // ============================================================
    public interface ITableGridCellHost
    {
        // --------------------------------------------------------------------------------
        /// <summary>
        /// <br/> 셀 값을 value로 바꿔 달라고 한다. Inline(Immediate)이면 바로 요청하고,
        /// <br/> Inline(Confirm)이면 Commit·Enter·포커스 이탈 때 요청한다.
        /// </summary>
        // --------------------------------------------------------------------------------
        void Replace(object value);

        // ------------------------------------------------------------
        /// <summary>
        /// Inline(Confirm)에서 보류한 값을 요청한다. 없으면 하지 않는다.
        /// </summary>
        // ------------------------------------------------------------
        void Commit();

        // ------------------------------------------------------------
        /// <summary>
        /// 셀 밖 상세 편집을 요청한다(TableGrid.DetailRequested).
        /// </summary>
        // ------------------------------------------------------------
        void OpenDetail();
    }

    // ============================================================
    /// <summary>
    /// 셀 뷰·편집기가 받는 셀 문맥.
    /// </summary>
    // ============================================================
    public readonly struct TableGridCellContext
    {

    #region 문맥

        private readonly Type valueType;

        // ------------------------------------------------------------
        /// <summary>
        /// 셀 문맥을 만든다.
        /// </summary>
        // ------------------------------------------------------------
        public TableGridCellContext
        (
            string rowID, int rowIndex, object item, TableGridColumn column, bool canEdit, ITableGridCellHost host,
            Type valueType = null
        )
        {
            RowID = rowID;
            RowIndex = rowIndex;
            Item = item;
            Column = column;
            CanEdit = canEdit;
            Host = host;
            this.valueType = valueType;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 셀이 속한 행 ID.
        /// </summary>
        // ------------------------------------------------------------
        public string RowID { get; }

        // --------------------------------------------------------------------------------
        /// <summary>
        /// 셀이 속한 행의 위치(0부터, 그룹 머리 행 포함). 화면의 행 번호(데이터 행만 1부터)와 다르다.
        /// </summary>
        // --------------------------------------------------------------------------------
        public int RowIndex { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 행 항목(TableGridRow.Item).
        /// </summary>
        // ------------------------------------------------------------
        public object Item { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 셀이 속한 열.
        /// </summary>
        // ------------------------------------------------------------
        public TableGridColumn Column { get; }

        // ----------------------------------------------------------------------
        /// <summary>
        /// 값 변경을 요청할 수 있는지 여부(표 Access·연결된 편집 요청·열 편집 방식 반영).
        /// </summary>
        // ----------------------------------------------------------------------
        public bool CanEdit { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 값 변경을 요청하는 셀 호스트. 측정할 때는 null이다.
        /// </summary>
        // ------------------------------------------------------------
        public ITableGridCellHost Host { get; }

        // --------------------------------------------------------------------------------
        /// <summary>
        /// <br/> 셀 뷰가 받는 값의 형식. 열 값 형식을 따르며,
        /// <br/> 열 값 형식을 모르는 열(object 열) 안에 셀 뷰를 얹는 호스트가 WithValueType으로 따로 줄 수 있다.
        /// </summary>
        // --------------------------------------------------------------------------------
        public Type ValueType => valueType ?? Column?.ValueType;

        // ------------------------------------------------------------
        /// <summary>
        /// 값 형식만 valueType으로 바꾼 문맥을 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public TableGridCellContext WithValueType(Type valueType)
        {
            return new TableGridCellContext(RowID, RowIndex, Item, Column, CanEdit, Host, valueType);
        }

    #endregion

    }
}
