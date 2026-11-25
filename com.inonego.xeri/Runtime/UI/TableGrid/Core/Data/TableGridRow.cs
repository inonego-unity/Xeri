/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TableGridRow.cs
수정일 : 2026-11-25

# 설명
안정적인 행 식별자, 행 항목, 행 종류를 보유하는 행 스냅샷을 정의한다.
셀 값은 형식 있는 열(TableGridColumn<TItem, TValue>)이 행 항목에서 꺼낸다.

# 특이사항, 제약사항
행 항목은 참조를 그대로 보관한다(구조체 항목은 행을 만들 때 한 번 박싱된다).
그룹 머리·요약 행은 제목 글자를 항목에 담는다(Title).
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

namespace inonego.Xeri.UI.TableGrid
{
    // ============================================================
    /// <summary>
    /// 행 ID, 행 항목과 행 종류를 보유하는 행 스냅샷.
    /// </summary>
    // ============================================================
    public sealed class TableGridRow
    {

    #region 행 스냅샷

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> 행을 만든다. item은 데이터 행이면 열이 값을 꺼낼 항목이고,
        /// <br/> 그룹 머리·요약 행이면 제목 글자다.
        /// </summary>
        // ------------------------------------------------------------
        public TableGridRow(string id, object item, TableGridRowKind kind = TableGridRowKind.Data)
        {
            TableGridID.Require(id, nameof(id), "행");

            if (!kind.IsDefined())
            {
                throw new ArgumentOutOfRangeException(nameof(kind));
            }

            ID = id;
            Item = item;
            Kind = kind;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 선택·이동·편집 요청의 기준이 되는 행 ID.
        /// </summary>
        // ------------------------------------------------------------
        public string ID { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 행 항목. 그룹 머리·요약 행이면 제목 글자다.
        /// </summary>
        // ------------------------------------------------------------
        public object Item { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 행 종류(데이터, 그룹 머리, 요약).
        /// </summary>
        // ------------------------------------------------------------
        public TableGridRowKind Kind { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 데이터 행인지 여부(그룹 머리·요약 행이 아님).
        /// </summary>
        // ------------------------------------------------------------
        public bool IsData => Kind == TableGridRowKind.Data;

        // ------------------------------------------------------------
        /// <summary>
        /// 항목이 글자면 그 글자, 아니면 빈 글자. 그룹 머리 제목에 쓴다.
        /// </summary>
        // ------------------------------------------------------------
        public string Title => Item as string ?? string.Empty;

    #endregion

    }
}
