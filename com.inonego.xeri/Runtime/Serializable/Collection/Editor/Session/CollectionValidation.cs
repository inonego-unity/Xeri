/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : CollectionValidation.cs
수정일 : 2026-11-19

# 설명
컬렉션 전체 또는 초안의 검증 결과를 보관한다.
행별 진단, 무효 행 목록과 초안 적용 가능 여부를 함께 제공한다.
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
    /// 컬렉션 검증 결과.
    /// </summary>
    // ============================================================
    internal sealed class CollectionValidation
    {

    #region 필드

        private static readonly IReadOnlyList<CollectionCellDiagnostic> none = Array.Empty<CollectionCellDiagnostic>();

        private readonly Dictionary<int, List<CollectionCellDiagnostic>> rowDiagnostics = new();

        // ------------------------------------------------------------
        /// <summary>
        /// 모든 진단을 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public IReadOnlyList<CollectionCellDiagnostic> Diagnostics => diagnostics;
        private readonly List<CollectionCellDiagnostic> diagnostics = new();

        // ------------------------------------------------------------
        /// <summary>
        /// Runtime에서 제외되거나 무시되는 행 목록을 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public IReadOnlyList<int> InvalidRows => new List<int>(invalidRows);
        private readonly SortedSet<int> invalidRows = new();

        // ------------------------------------------------------------
        /// <summary>
        /// 초안을 적용할 수 있는지 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public bool CanApply { get; private set; } = true;

    #endregion

    #region 조회

        // ------------------------------------------------------------
        /// <summary>
        /// 행이 무효한지 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public bool IsRowInvalid(int sourceIndex)
        {
            return invalidRows.Contains(sourceIndex);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 행의 진단 목록을 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public IReadOnlyList<CollectionCellDiagnostic> GetDiagnostics(int sourceIndex)
        {
            return rowDiagnostics.TryGetValue(sourceIndex, out var list) ? list : none;
        }

    #endregion

    #region 기록

        // ------------------------------------------------------------
        /// <summary>
        /// 행의 진단을 기록한다.
        /// </summary>
        // ------------------------------------------------------------
        public void Add(int sourceIndex, CollectionCellDiagnostic diagnostic)
        {
            if (!rowDiagnostics.TryGetValue(sourceIndex, out var list))
            {
                list = new List<CollectionCellDiagnostic>();
                rowDiagnostics.Add(sourceIndex, list);
            }

            list.Add(diagnostic);
            diagnostics.Add(diagnostic);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 행 진단을 기록하고 행을 무효로 표시한다.
        /// </summary>
        // ------------------------------------------------------------
        public void AddInvalid(int sourceIndex, CollectionCellDiagnostic diagnostic)
        {
            Add(sourceIndex, diagnostic);
            invalidRows.Add(sourceIndex);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 초안 오류를 기록하고 적용을 막는다.
        /// </summary>
        // ------------------------------------------------------------
        public void Block(CollectionCellDiagnostic diagnostic)
        {
            diagnostics.Add(diagnostic);
            CanApply = false;
        }

    #endregion

    }
}
