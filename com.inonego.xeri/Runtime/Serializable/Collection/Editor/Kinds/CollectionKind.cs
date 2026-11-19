/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : CollectionKind.cs
수정일 : 2026-11-19

# 설명
컬렉션 종류 하나의 편집 규칙을 데이터로 정의한다.
열, 행 배열 경로, 지원 편집, 추가 위치, 키 규칙, 표시 순서, Runtime 동기화 방식과 선택적 hook을 가진다.

# 특이사항, 제약사항
종류 정의는 UI와 SerializedObject를 모른다. 종류별 차이는 상속이 아니라 이 값과 hook으로만 표현한다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEditor;

using inonego;
using inonego.Xeri;

namespace inonego.Xeri.Serializable.Editor
{
    // ============================================================
    /// <summary>
    /// 새 행을 넣는 위치.
    /// </summary>
    // ============================================================
    internal enum CollectionInsert { End, Start }

    // ============================================================
    /// <summary>
    /// 중복 키 가운데 Runtime이 사용하는 행.
    /// </summary>
    // ============================================================
    internal enum CollectionDuplicateWinner { First, Last }

    // ============================================================
    /// <summary>
    /// 변경 확정 뒤 Runtime 컬렉션을 맞추는 방식.
    /// </summary>
    // ============================================================
    internal enum CollectionSync { None, Runtime, Normalize }

    // ============================================================
    /// <summary>
    /// 컬렉션 종류가 허용하는 편집.
    /// </summary>
    // ============================================================
    [Flags]
    internal enum CollectionEdits
    {
        None = 0,
        Add = 1,
        Remove = 2,
        Duplicate = 4,
        Move = 8,
        Paste = 16,
        KeySet = 32,
    }

    // ============================================================
    /// <summary>
    /// 키 열의 중복·누락 규칙.
    /// </summary>
    // ============================================================
    internal sealed class CollectionKeyRule
    {

    #region 필드

        // ------------------------------------------------------------
        /// <summary>
        /// 키 열 식별자를 반환하며 null이면 행 값 자체가 키다.
        /// </summary>
        // ------------------------------------------------------------
        public string ColumnID { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// Runtime과 같은 키 비교자를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public IEqualityComparer<object> Comparer { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 중복 키 가운데 사용하는 행을 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public CollectionDuplicateWinner Winner { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// null 키를 무효로 보는지 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public bool RequiresKey { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 진단 문구에 쓸 키의 이름("키", "값")을 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public string Noun { get; }

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// 키 규칙을 만든다.
        /// </summary>
        // ------------------------------------------------------------
        public CollectionKeyRule
        (
            string columnID,
            IEqualityComparer<object> comparer,
            CollectionDuplicateWinner winner,
            bool requiresKey,
            string noun
        )
        {
            ColumnID = columnID;
            Comparer = comparer ?? throw new ArgumentNullException(nameof(comparer));
            Winner = winner;
            RequiresKey = requiresKey;
            Noun = noun ?? "키";
        }

    #endregion

    }

    // ============================================================
    /// <summary>
    /// 종류별 추가 처리를 붙이는 선택적 hook.
    /// </summary>
    // ============================================================
    internal interface ICollectionKindHooks
    {
        // ------------------------------------------------------------
        /// <summary>
        /// 편집 세션이 만들어질 때 소유자 상태를 관찰한다.
        /// </summary>
        // ------------------------------------------------------------
        void Attach(CollectionSource source);

        // ------------------------------------------------------------
        /// <summary>
        /// 새 행의 값을 넣은 직후 추가 초기화를 한다.
        /// </summary>
        // ------------------------------------------------------------
        void InitializeRow(CollectionSource source, int sourceIndex);

        // ------------------------------------------------------------
        /// <summary>
        /// 행 무효 여부와 무관한 추가 진단을 더한다.
        /// </summary>
        // ------------------------------------------------------------
        void AppendDiagnostics
        (
            CollectionSource source, int sourceIndex, List<CollectionCellDiagnostic> output
        );

        // ------------------------------------------------------------
        /// <summary>
        /// 변경 확정과 Undo 복원 뒤 소유자 상태를 맞춘다.
        /// </summary>
        // ------------------------------------------------------------
        void SyncOwner(CollectionSource source);
    }

    // ============================================================
    /// <summary>
    /// 컬렉션 종류 하나의 편집 규칙.
    /// </summary>
    // ============================================================
    internal sealed class CollectionKind
    {

    #region 필드

        // ------------------------------------------------------------
        /// <summary>
        /// Undo 이름에 쓰는 종류 이름을 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public string Label { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 편집 규칙을 설명하는 문구를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public string Description { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 표의 열 목록을 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public IReadOnlyList<CollectionColumn> Columns { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> 컬렉션 기준 행 배열 경로를 반환한다.
        /// <br/> null이면 컬렉션 자체가 행 배열이다.
        /// </summary>
        // ------------------------------------------------------------
        public string RowsPath { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 허용하는 편집을 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public CollectionEdits Edits { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 새 행을 넣는 위치를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public CollectionInsert Insert { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 키 규칙을 반환하며 키가 없으면 null이다.
        /// </summary>
        // ------------------------------------------------------------
        public CollectionKeyRule KeyRule { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 표시 순서 기준 열을 반환하며 저장 순서면 null이다.
        /// </summary>
        // ------------------------------------------------------------
        public CollectionColumn DisplayOrderColumn { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 표시 순서 기준 열의 값 비교를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public Comparison<object> DisplayOrderComparison { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 변경 확정 뒤 Runtime을 맞추는 방식을 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public CollectionSync Sync { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 종류별 추가 처리를 반환하며 없으면 null이다.
        /// </summary>
        // ------------------------------------------------------------
        public ICollectionKindHooks Hooks { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 행 순서를 사용자가 바꿀 수 있는지 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public bool CanReorder => Supports(CollectionEdits.Move);

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// 컬렉션 종류 정의를 만든다.
        /// </summary>
        // ------------------------------------------------------------
        public CollectionKind
        (
            string label,
            string description,
            IReadOnlyList<CollectionColumn> columns,
            string rowsPath,
            CollectionEdits edits,
            CollectionSync sync,
            CollectionInsert insert = CollectionInsert.End,
            CollectionKeyRule keyRule = null,
            CollectionColumn displayOrderColumn = null,
            Comparison<object> displayOrderComparison = null,
            ICollectionKindHooks hooks = null
        )
        {
            if (columns == null || columns.Count == 0)
            {
                throw new ArgumentException("열이 하나 이상 필요합니다.", nameof(columns));
            }

            if (displayOrderColumn != null && displayOrderComparison == null)
            {
                throw new ArgumentNullException(nameof(displayOrderComparison));
            }

            Label = label ?? string.Empty;
            Description = description ?? string.Empty;
            Columns = columns;
            RowsPath = string.IsNullOrEmpty(rowsPath) ? null : rowsPath;
            Edits = edits;
            Sync = sync;
            Insert = insert;
            KeyRule = keyRule;
            DisplayOrderColumn = displayOrderColumn;
            DisplayOrderComparison = displayOrderComparison;
            Hooks = hooks;
            if (keyRule?.ColumnID != null && FindColumn(keyRule.ColumnID) == null)
            {
                throw new ArgumentException("키 규칙의 열이 열 목록에 없습니다.", nameof(keyRule));
            }
        }

    #endregion

    #region 조회

        // ------------------------------------------------------------
        /// <summary>
        /// 지정한 편집을 허용하는지 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public bool Supports(CollectionEdits edit)
        {
            return (Edits & edit) == edit;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 식별자에 맞는 열을 반환하며 없으면 null이다.
        /// </summary>
        // ------------------------------------------------------------
        public CollectionColumn FindColumn(string columnID)
        {
            foreach (var column in Columns)
            {
                if (string.Equals(column.ID, columnID, StringComparison.Ordinal))
                {
                    return column;
                }
            }

            return null;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 키 열을 반환하며 키가 없거나 행 값 자체가 키면 null이다.
        /// </summary>
        // ------------------------------------------------------------
        public CollectionColumn KeyColumn => KeyRule?.ColumnID == null ? null : FindColumn(KeyRule.ColumnID);

    #endregion

    }
}
