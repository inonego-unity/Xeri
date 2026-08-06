/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : GenerationValidation.cs
수정일 : 2026-08-04

# 설명
생성 Manifest 검증 결과와 원인별 진단 정보를 순수 데이터로 표현한다.

# 제약사항
검증 규칙과 재시도·대체 정책은 각 도메인 Validator가 정의한다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections.Generic;

namespace inonego.Xeri.Generation
{
    // ============================================================
    /// <summary>
    /// 생성 검증 진단이 생성 자체를 막는지 나타낸다.
    /// </summary>
    // ============================================================
    public enum GenerationIssueSeverity
    {
        Warning = 0,
        Error   = 1,
    }

    // ============================================================
    /// <summary>
    /// 하나의 생성 검증 원인과 설명을 보관한다.
    /// </summary>
    // ============================================================
    [Serializable]
    public readonly struct GenerationValidationIssue
    {
    #region 필드

        // ------------------------------------------------------------
        /// <summary>
        /// 도메인 Validator가 부여한 안정 진단 Code다.
        /// </summary>
        // ------------------------------------------------------------
        public GenerationKey Code => code;

        private readonly GenerationKey code;

        // ------------------------------------------------------------
        /// <summary>
        /// 이 진단이 인스턴스화를 막는지 나타낸다.
        /// </summary>
        // ------------------------------------------------------------
        public GenerationIssueSeverity Severity => severity;

        private readonly GenerationIssueSeverity severity;

        // ------------------------------------------------------------
        /// <summary>
        /// 제작자 또는 로그에 표시할 진단 설명이다.
        /// </summary>
        // ------------------------------------------------------------
        public string Message => message;

        private readonly string message;

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// 안정 Code와 심각도, 설명으로 검증 진단을 만든다.
        /// </summary>
        // ------------------------------------------------------------
        public GenerationValidationIssue
        (
            GenerationKey code,
            GenerationIssueSeverity severity,
            string message
        )
        {
            if (!code.IsDefined)
            {
                throw new ArgumentException("Generation Validation Issue에는 Code가 필요합니다.", nameof(code));
            }

            if (string.IsNullOrWhiteSpace(message))
            {
                throw new ArgumentException("Generation Validation Issue의 Message를 비워 둘 수 없습니다.", nameof(message));
            }

            this.code = code;
            this.severity = severity;
            this.message = message;
        }

    #endregion
    }

    // ============================================================
    /// <summary>
    /// 하나의 Manifest 검증 결과와 모든 진단 목록을 보관한다.
    /// </summary>
    // ============================================================
    public readonly struct GenerationValidationResult
    {
    #region 필드

        // ------------------------------------------------------------
        /// <summary>
        /// Error 진단이 없어 Manifest를 인스턴스화할 수 있는지 나타낸다.
        /// </summary>
        // ------------------------------------------------------------
        public bool IsValid => isValid;

        private readonly bool isValid;

        // ------------------------------------------------------------
        /// <summary>
        /// Validator가 발견한 Warning과 Error 전체 목록이다.
        /// </summary>
        // ------------------------------------------------------------
        public IReadOnlyList<GenerationValidationIssue> Issues => issues ?? Array.Empty<GenerationValidationIssue>();

        private readonly GenerationValidationIssue[] issues;

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// 전달받은 진단 전체를 보존하는 검증 결과를 만든다.
        /// </summary>
        // ------------------------------------------------------------
        public GenerationValidationResult(IReadOnlyList<GenerationValidationIssue> issues)
        {
            if (issues == null)
            {
                throw new ArgumentNullException(nameof(issues));
            }

            this.issues = new GenerationValidationIssue[issues.Count];
            isValid = true;

            for (var index = 0; index < issues.Count; index++)
            {
                var issue = issues[index];
                this.issues[index] = issue;

                if (issue.Severity == GenerationIssueSeverity.Error)
                {
                    isValid = false;
                }
            }
        }

    #endregion
    }
}
