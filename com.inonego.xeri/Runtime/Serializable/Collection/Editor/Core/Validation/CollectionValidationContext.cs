/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : CollectionValidationContext.cs
수정일 : 2026-10-29

# 설명
원본 행 검증과 Key/Order 임시 편집 값 검증의 대상을 같은 계약으로 전달한다.
후보가 null일 수 있으므로 IsCandidate로 검증 종류를 명시한다.
========================================================================= BLOCK_HEADER_END */

namespace inonego.Xeri.Serializable.Editor
{
    // ============================================================
    /// <summary>
    /// 확정 행 또는 적용 전 후보 값에 대한 검증 요청.
    /// </summary>
    // ============================================================
    internal readonly struct CollectionValidationContext
    {

    #region 프로퍼티

        public int? SourceIndex { get; }
        public string ColumnID { get; }
        public object CandidateValue { get; }
        public bool IsCandidate { get; }

    #endregion

    #region 생성

        private CollectionValidationContext
        (
            int? sourceIndex, string columnID, object candidateValue, bool isCandidate
        )
        {
            SourceIndex = sourceIndex;
            ColumnID = columnID;
            CandidateValue = candidateValue;
            IsCandidate = isCandidate;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 이미 확정된 원본 행을 검사한다.
        /// </summary>
        // ------------------------------------------------------------
        public static CollectionValidationContext ForRow(int sourceIndex)
        {
            return new(sourceIndex, null, null, false);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 적용 전 Key/Order 후보 값을 검사한다.
        /// </summary>
        // ------------------------------------------------------------
        public static CollectionValidationContext ForCandidate
        (
            int? sourceIndex, string columnID, object candidateValue
        )
        {
            return new(sourceIndex, columnID, candidateValue, true);
        }

    #endregion

    }
}
