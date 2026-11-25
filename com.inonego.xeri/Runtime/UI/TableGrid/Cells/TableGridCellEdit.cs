/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TableGridCellEdit.cs
수정일 : 2026-11-25

# 설명
열의 셀 편집 방식(None·Editor·Inline·Detail)을 정의한다.
- None: 보기만 한다(붙여넣기·지우기도 건너뛴다).
- Editor: 평소엔 값만 보이고 Enter·더블클릭·글자 입력으로 편집기를 연다. 편집기를 지정하지 않으면
  등록소의 값 형식별 편집기, 선택형이면 선택 메뉴, 글자를 읽을 수 있으면 입력칸을 쓴다.
- Inline: 셀 뷰가 셀 안 컨트롤을 두고 셀 호스트로 값 변경을 요청한다(Immediate·Confirm).
- Detail: 셀은 보기만 하고 Enter·더블클릭으로 셀 밖 상세 편집을 요청한다(TableGrid.DetailRequested).

# 특이사항, 제약사항
불변이며 여러 열이 공유할 수 있다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

namespace inonego.Xeri.UI.TableGrid
{
    // ============================================================
    /// <summary>
    /// 열의 셀 편집 방식.
    /// </summary>
    // ============================================================
    public sealed class TableGridCellEdit
    {

    #region 편집 방식

        private TableGridCellEdit(TableGridCellEditKind kind, TableGridCellEditor cellEditor, TableGridInlineCommit inlineCommit)
        {
            Kind = kind;
            CellEditor = cellEditor;
            InlineCommit = inlineCommit;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 고칠 수 없는 열.
        /// </summary>
        // ------------------------------------------------------------
        public static TableGridCellEdit None { get; } = new(TableGridCellEditKind.None, null, TableGridInlineCommit.Immediate);

        // ------------------------------------------------------------
        /// <summary>
        /// 셀은 보기만 하고 셀 밖 상세 편집을 요청하는 열.
        /// </summary>
        // ------------------------------------------------------------
        public static TableGridCellEdit Detail { get; } = new(TableGridCellEditKind.Detail, null, TableGridInlineCommit.Immediate);

        // ----------------------------------------------------------------------
        /// <summary>
        /// 편집기로 고치는 열. editor가 null이면 등록소·값 규칙의 기본 편집기를 쓴다.
        /// </summary>
        // ----------------------------------------------------------------------
        public static TableGridCellEdit Editor(TableGridCellEditor editor = null)
        {
            return new TableGridCellEdit(TableGridCellEditKind.Editor, editor, TableGridInlineCommit.Immediate);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 셀 안 컨트롤로 고치는 열. commit은 반영 시점이다.
        /// </summary>
        // ------------------------------------------------------------
        public static TableGridCellEdit Inline(TableGridInlineCommit commit = TableGridInlineCommit.Immediate)
        {
            if (!commit.IsDefined())
            {
                throw new ArgumentOutOfRangeException(nameof(commit));
            }

            return new TableGridCellEdit(TableGridCellEditKind.Inline, null, commit);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 편집 방식 종류.
        /// </summary>
        // ------------------------------------------------------------
        public TableGridCellEditKind Kind { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// Editor 방식에서 쓸 편집기. null이면 기본 편집기다.
        /// </summary>
        // ------------------------------------------------------------
        public TableGridCellEditor CellEditor { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// Inline 방식의 반영 시점.
        /// </summary>
        // ------------------------------------------------------------
        public TableGridInlineCommit InlineCommit { get; }

    #endregion

    }
}
