/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : SeverityIndicator.cs
수정일 : 2026-10-29

# 설명
진단 심각도를 공통 Lucide 아이콘과 USS 색상으로 표현한다.
========================================================================= BLOCK_HEADER_END */

using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace inonego.Xeri
{
    // ============================================================
    /// <summary>
    /// 진단 심각도 하나에 해당하는 Xeri 공통 아이콘 표시.
    /// </summary>
    // ============================================================
    public sealed class SeverityIndicator : Image
    {

    #region 필드

        private static readonly StyleSheet severityStyle = AssetDatabase.LoadAssetAtPath<StyleSheet>
        (
            $"{EditorAssetHelper.GetScriptDirectory(typeof(SeverityIndicator))}/Severity.uss"
        );

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// 아이콘 표시 요소를 초기화하고 심각도 미지정 시 숨긴다.
        /// </summary>
        // ------------------------------------------------------------
        public SeverityIndicator()
        {
            AddToClassList("xeri-severity-indicator");
            if (severityStyle != null)
            {
                styleSheets.Add(severityStyle);
            }

            scaleMode = ScaleMode.ScaleToFit;
            SetSeverity(null);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 지정한 심각도의 공통 Lucide 아이콘을 표시한다.
        /// </summary>
        // ------------------------------------------------------------
        public SeverityIndicator(DiagnosticSeverity severity) : this()
        {
            SetSeverity(severity);
        }

    #endregion

    #region 표시

        // ------------------------------------------------------------
        /// <summary>
        /// 심각도에 맞는 Lucide 아이콘을 지정하고 null이면 숨긴다.
        /// </summary>
        // ------------------------------------------------------------
        public void SetSeverity(DiagnosticSeverity? severity)
        {
            EnableInClassList("xeri-severity-indicator--error", severity == DiagnosticSeverity.Error);
            EnableInClassList("xeri-severity-indicator--warning", severity == DiagnosticSeverity.Warning);
            EnableInClassList("xeri-severity-indicator--info", severity == DiagnosticSeverity.Info);

            if (!severity.HasValue)
            {
                vectorImage = null;
                style.display = DisplayStyle.None;
                return;
            }

            var iconName = severity.Value switch
            {
                DiagnosticSeverity.Error => "circle-x",
                DiagnosticSeverity.Warning => "triangle-alert",
                _ => "circle-dot"
            };

            vectorImage = XeriIcon.Load(iconName);
            style.display = DisplayStyle.Flex;
        }

    #endregion

    }
}
