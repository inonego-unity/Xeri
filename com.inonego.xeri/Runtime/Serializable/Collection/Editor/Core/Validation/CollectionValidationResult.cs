/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : CollectionValidationResult.cs
수정일 : 2026-10-29

# 설명
여러 진단을 보관하며 표시 심각도와 별도로 적용 가능 여부 및 행 무효 여부를 제공한다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

namespace inonego.Xeri.Serializable.Editor
{
    // ============================================================
    /// <summary>
    /// 진단 메시지와 데이터 처리 정책을 분리한 검증 결과.
    /// </summary>
    // ============================================================
    internal sealed class CollectionValidationResult
    {

    #region 필드

        public IReadOnlyList<CollectionCellDiagnostic> Diagnostics => diagnostics;
        private readonly List<CollectionCellDiagnostic> diagnostics = new();

        public bool CanApply { get; private set; } = true;
        public bool IsRowInvalid { get; private set; }

    #endregion

    #region 진단과 정책

        // ------------------------------------------------------------
        /// <summary>
        /// 기존 진단 목록에 메시지를 추가한다.
        /// </summary>
        // ------------------------------------------------------------
        public void Add(CollectionCellDiagnostic diagnostic)
        {
            diagnostics.Add(diagnostic);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 후보 값을 원본에 적용하지 못하도록 표시한다.
        /// </summary>
        // ------------------------------------------------------------
        public void BlockApply()
        {
            CanApply = false;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 행 구조가 유효하지 않아 정리 대상임을 표시한다.
        /// </summary>
        // ------------------------------------------------------------
        public void MarkRowInvalid()
        {
            IsRowInvalid = true;
        }

    #endregion

    }
}
