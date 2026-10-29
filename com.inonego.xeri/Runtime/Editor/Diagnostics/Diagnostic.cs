/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : Diagnostic.cs
수정일 : 2026-10-29

# 설명
진단의 식별자, 심각도와 메시지를 UI 및 대상 위치와 독립적으로 보관한다.
========================================================================= BLOCK_HEADER_END */

namespace inonego.Xeri
{
    // ============================================================
    /// <summary>
    /// 특정 검증 규칙에서 발생한 단일 진단 메시지.
    /// </summary>
    // ============================================================
    public readonly struct Diagnostic
    {

    #region 프로퍼티

        public string Code { get; }
        public DiagnosticSeverity Severity { get; }
        public string Message { get; }

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// 진단의 식별자, 심각도와 설명을 지정한다.
        /// </summary>
        // ------------------------------------------------------------
        public Diagnostic(string code, DiagnosticSeverity severity, string message)
        {
            Code = code;
            Severity = severity;
            Message = message;
        }

    #endregion

    }
}
