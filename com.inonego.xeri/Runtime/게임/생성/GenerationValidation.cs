/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : GenerationValidation.cs
수정일 : 2026-08-24

# 설명
생성 결과의 검증 진단과 Validator 최소 계약을 제공한다.

# 제약사항
Manifest, Pipeline, Retry, Backtrack, Runtime 인스턴스화 정책을 강제하지 않는다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections.Generic;

using UnityEngine;

namespace inonego.Xeri.Generation
{
    // ============================================================
    /// <summary>
    /// 생성 검증 진단이 결과 사용을 막는지 나타낸다.
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
    public struct GenerationValidationIssue
    {
    #region 필드

        // ------------------------------------------------------------
        /// <summary>
        /// 도메인 Validator가 부여한 안정 진단 Code.
        /// </summary>
        // ------------------------------------------------------------
        public string Code => code;

        [SerializeField]
        private string code;

        // ------------------------------------------------------------
        /// <summary>
        /// 이 진단이 결과 사용을 막는지 나타낸다.
        /// </summary>
        // ------------------------------------------------------------
        public GenerationIssueSeverity Severity => severity;

        [SerializeField]
        private GenerationIssueSeverity severity;

        // ------------------------------------------------------------
        /// <summary>
        /// 제작자 또는 로그에 표시할 진단 설명.
        /// </summary>
        // ------------------------------------------------------------
        public string Message => message;

        [SerializeField]
        private string message;

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// 안정 Code와 심각도, 설명으로 검증 진단을 만든다.
        /// </summary>
        // ------------------------------------------------------------
        public GenerationValidationIssue
        (
            string code,
            GenerationIssueSeverity severity,
            string message
        )
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                throw new ArgumentException("Generation Validation Issue의 Code를 비워 둘 수 없습니다.", nameof(code));
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
    /// 하나의 생성 결과에 대한 Warning/Error 전체 목록을 보관한다.
    /// </summary>
    // ============================================================
    [Serializable]
    public struct GenerationValidationResult
    {
    #region 필드

        // ------------------------------------------------------------
        /// <summary>
        /// Error 진단이 없어 결과를 사용할 수 있는지 나타낸다.
        /// </summary>
        // ------------------------------------------------------------
        public bool IsValid
        {
            get
            {
                var current = Issues;

                for (var index = 0; index < current.Count; index++)
                {
                    if (current[index].Severity == GenerationIssueSeverity.Error)
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Validator가 발견한 Warning과 Error 전체 목록.
        /// </summary>
        // ------------------------------------------------------------
        public IReadOnlyList<GenerationValidationIssue> Issues => issues ?? Array.Empty<GenerationValidationIssue>();

        [SerializeField]
        private GenerationValidationIssue[] issues;

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// 전달받은 진단 전체를 복사해 검증 결과를 만든다.
        /// </summary>
        // ------------------------------------------------------------
        public GenerationValidationResult(IReadOnlyList<GenerationValidationIssue> issues)
        {
            if (issues == null)
            {
                throw new ArgumentNullException(nameof(issues));
            }

            this.issues = new GenerationValidationIssue[issues.Count];

            for (var index = 0; index < issues.Count; index++)
            {
                this.issues[index] = issues[index];
            }
        }

    #endregion
    }

    // ============================================================
    /// <summary>
    /// 생성 결과가 도메인 제약을 만족하는지 판정하는 최소 계약.
    /// </summary>
    // ============================================================
    public interface IGenerationValidator<in TResult>
    {
        // ------------------------------------------------------------
        /// <summary>
        /// 생성 결과의 유효성과 진단 목록을 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        GenerationValidationResult Validate(TResult result);
    }
}
