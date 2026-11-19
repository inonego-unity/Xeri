/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : CollectionEdit.cs
수정일 : 2026-11-19

# 설명
컬렉션 편집 요청(CollectionEdit)과 그 결과(CollectionEditResult)를 정의한다.
편집 요청은 불변 데이터이며 실행은 CollectionSession.Apply가 한다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

using inonego;
using inonego.Xeri;

namespace inonego.Xeri.Serializable.Editor
{
    // ============================================================
    /// <summary>
    /// 컬렉션 편집 요청의 종류.
    /// </summary>
    // ============================================================
    internal enum CollectionEditType
    {
        AddRow,
        Duplicate,
        Remove,
        Move,
        SetCell,
        CommitDraft,
        CommitBound,
        Paste,
        ApplyKeySet,
    }

    // ============================================================
    /// <summary>
    /// 키 집합 일괄 편집 방식.
    /// </summary>
    // ============================================================
    internal enum CollectionKeySetOperation { Add, Remove, Confine }

    // ============================================================
    /// <summary>
    /// 컬렉션 편집 요청.
    /// </summary>
    // ============================================================
    internal sealed class CollectionEdit
    {

    #region 필드

        // ------------------------------------------------------------
        /// <summary>
        /// 편집 종류를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public CollectionEditType Type { get; private set; }

        // ------------------------------------------------------------
        /// <summary>
        /// 대상 행 인덱스를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public int SourceIndex { get; private set; } = -1;

        // ------------------------------------------------------------
        /// <summary>
        /// 이동할 위치를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public int DestinationIndex { get; private set; } = -1;

        // ------------------------------------------------------------
        /// <summary>
        /// 삭제할 행 인덱스 목록을 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public IReadOnlyList<int> SourceIndices { get; private set; }

        // ------------------------------------------------------------
        /// <summary>
        /// 값을 바꿀 열 식별자를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public string ColumnID { get; private set; }

        // ------------------------------------------------------------
        /// <summary>
        /// 셀에 넣을 값을 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public object Value { get; private set; }

        // ------------------------------------------------------------
        /// <summary>
        /// 확정할 초안을 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public CollectionDraft Draft { get; private set; }

        // ------------------------------------------------------------
        /// <summary>
        /// 바인딩이 이미 쓴 속성 경로를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public string PropertyPath { get; private set; }

        // ------------------------------------------------------------
        /// <summary>
        /// 일괄 편집할 키 목록을 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public IReadOnlyList<object> Keys { get; private set; }

        // ------------------------------------------------------------
        /// <summary>
        /// 키 집합 편집 방식을 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public CollectionKeySetOperation KeySetOperation { get; private set; }

        // ------------------------------------------------------------
        /// <summary>
        /// 기본 이름 대신 쓸 Undo 이름을 반환하며 없으면 null이다.
        /// </summary>
        // ------------------------------------------------------------
        public string UndoName { get; private set; }

    #endregion

    #region 생성

        private CollectionEdit(CollectionEditType type)
        {
            Type = type;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 초안 값으로 새 행을 추가한다.
        /// </summary>
        // ------------------------------------------------------------
        public static CollectionEdit AddRow(CollectionDraft draft)
        {
            return new(CollectionEditType.AddRow)
            {
                Draft = draft ?? CollectionDraft.ForNewRow(),
            };
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 행의 독립 복제본을 추가한다.
        /// </summary>
        // ------------------------------------------------------------
        public static CollectionEdit Duplicate(int sourceIndex)
        {
            return new(CollectionEditType.Duplicate)
            {
                SourceIndex = sourceIndex,
            };
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 여러 행을 한 번의 Undo로 삭제한다.
        /// </summary>
        // ------------------------------------------------------------
        public static CollectionEdit Remove(IReadOnlyList<int> sourceIndices, string undoName = null)
        {
            return new(CollectionEditType.Remove)
            {
                SourceIndices = sourceIndices ?? Array.Empty<int>(),
                UndoName = undoName,
            };
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 행을 지정 위치로 옮긴다.
        /// </summary>
        // ------------------------------------------------------------
        public static CollectionEdit Move(int sourceIndex, int destinationIndex)
        {
            return new(CollectionEditType.Move)
            {
                SourceIndex = sourceIndex,
                DestinationIndex = destinationIndex,
            };
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 즉시 반영 열의 셀 값을 바꾼다.
        /// </summary>
        // ------------------------------------------------------------
        public static CollectionEdit SetCell(int sourceIndex, string columnID, object value)
        {
            return new(CollectionEditType.SetCell)
            {
                SourceIndex = sourceIndex,
                ColumnID = columnID,
                Value = value,
            };
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 기존 행의 초안 값을 확정한다.
        /// </summary>
        // ------------------------------------------------------------
        public static CollectionEdit CommitDraft(CollectionDraft draft)
        {
            return new(CollectionEditType.CommitDraft)
            {
                Draft = draft ?? throw new ArgumentNullException(nameof(draft)),
            };
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 바인딩이나 상세 창이 이미 쓴 값을 확정한다.
        /// </summary>
        // ------------------------------------------------------------
        public static CollectionEdit CommitBound(string propertyPath)
        {
            return new(CollectionEditType.CommitBound)
            {
                PropertyPath = propertyPath,
            };
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 클립보드의 행 값을 붙여넣는다.
        /// </summary>
        // ------------------------------------------------------------
        public static CollectionEdit Paste(int sourceIndex)
        {
            return new(CollectionEditType.Paste)
            {
                SourceIndex = sourceIndex,
            };
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 키 집합을 지정한 방식으로 반영한다.
        /// </summary>
        // ------------------------------------------------------------
        public static CollectionEdit ApplyKeySet
        (
            IReadOnlyList<object> keys, CollectionKeySetOperation operation
        )
        {
            return new(CollectionEditType.ApplyKeySet)
            {
                Keys = keys ?? throw new ArgumentNullException(nameof(keys)),
                KeySetOperation = operation,
            };
        }

    #endregion

    }

    // ============================================================
    /// <summary>
    /// 편집 요청의 결과.
    /// </summary>
    // ============================================================
    internal readonly struct CollectionEditResult
    {

    #region 필드

        // ------------------------------------------------------------
        /// <summary>
        /// 변경을 확정했는지 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public bool Succeeded { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 확정 뒤 대상 행 위치를 반환하며 모르면 -1이다.
        /// </summary>
        // ------------------------------------------------------------
        public int RowIndex { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 실패 사유를 반환하며 변경이 없었으면 빈 문자열이다.
        /// </summary>
        // ------------------------------------------------------------
        public string Reason { get; }

    #endregion

    #region 생성

        private CollectionEditResult(bool succeeded, int rowIndex, string reason)
        {
            Succeeded = succeeded;
            RowIndex = rowIndex;
            Reason = reason ?? string.Empty;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 확정한 결과를 만든다.
        /// </summary>
        // ------------------------------------------------------------
        public static CollectionEditResult Success(int rowIndex)
        {
            return new(true, rowIndex, null);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 실패 결과를 만든다.
        /// </summary>
        // ------------------------------------------------------------
        public static CollectionEditResult Failure(string reason)
        {
            return new(false, -1, reason);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 확정할 변경이 없었던 결과를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public static CollectionEditResult Unchanged => new(false, -1, null);

    #endregion

    }
}
