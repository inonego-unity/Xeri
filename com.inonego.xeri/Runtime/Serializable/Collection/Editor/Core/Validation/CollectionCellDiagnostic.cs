/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : CollectionCellDiagnostic.cs
수정일 : 2026-10-29

# 설명
공통 Diagnostic을 Collection의 열과 중첩 PropertyPath에 연결한다.
========================================================================= BLOCK_HEADER_END */

using inonego;
using inonego.Xeri;

namespace inonego.Xeri.Serializable.Editor
{
    // ============================================================
    /// <summary>
    /// Collection 셀에 연결된 진단과 대상 위치를 보관한다.
    /// </summary>
    // ============================================================
    internal readonly struct CollectionCellDiagnostic
    {

    #region 프로퍼티

        public Diagnostic Diagnostic { get; }
        public string Code => Diagnostic.Code;
        public DiagnosticSeverity Severity => Diagnostic.Severity;
        public string Message => Diagnostic.Message;
        public string ColumnID { get; }
        public string PropertyPath { get; }

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// 일반 진단과 Collection 전용 표시 대상을 결합한다.
        /// </summary>
        // ------------------------------------------------------------
        public CollectionCellDiagnostic
        (
            string code, DiagnosticSeverity severity, string message,
            string columnID = null, string propertyPath = null
        )
        {
            Diagnostic = new Diagnostic(code, severity, message);
            ColumnID = columnID;
            PropertyPath = propertyPath;
        }

    #endregion

    }
}
