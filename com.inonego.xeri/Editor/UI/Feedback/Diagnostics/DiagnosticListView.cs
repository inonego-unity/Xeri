/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : DiagnosticListView.cs
수정일 : 2026-10-29

# 설명
현재 진단을 심각도 순으로 표시하며 두 항목 정도의 높이에서 내부 스크롤한다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace inonego.Xeri
{
    // ============================================================
    /// <summary>
    /// 심각도별 아이콘을 공유하는 여러 진단의 간결한 목록.
    /// </summary>
    // ============================================================
    public sealed class DiagnosticListView : VisualElement
    {

    #region 필드

        private static readonly StyleSheet listStyle = AssetDatabase.LoadAssetAtPath<StyleSheet>
        (
            $"{EditorAssetHelper.GetScriptDirectory(typeof(DiagnosticListView))}/DiagnosticListView.uss"
        );
        private readonly ScrollView scroll;

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// 높이 제한이 있는 목록을 초기화한다.
        /// </summary>
        // ------------------------------------------------------------
        public DiagnosticListView()
        {
            AddToClassList("xeri-feedback-diagnostics");
            if (listStyle != null)
            {
                styleSheets.Add(listStyle);
            }

            scroll = new ScrollView(ScrollViewMode.Vertical);
            scroll.AddToClassList("xeri-feedback-diagnostics__scroll");
            Add(scroll);
            style.display = DisplayStyle.None;
        }

    #endregion

    #region 표시

        // ------------------------------------------------------------
        /// <summary>
        /// <br/>진단을 심각도 우선순으로 표시한다.
        /// <br/>details는 각 진단의 위치 설명을 나타낸다.
        /// </summary>
        // ------------------------------------------------------------
        public void SetDiagnostics
        (
            IReadOnlyList<Diagnostic> diagnostics, IReadOnlyList<string> details = null
        )
        {
            scroll.Clear();
            scroll.scrollOffset = Vector2.zero;
            if (diagnostics == null || diagnostics.Count == 0)
            {
                style.display = DisplayStyle.None;
                return;
            }

            // 같은 심각도에서는 전달된 진단 순서를 보존한다.
            foreach (var severity in new[]
            {
                DiagnosticSeverity.Error,
                DiagnosticSeverity.Warning,
                DiagnosticSeverity.Info
            })
            {
                for (var index = 0; index < diagnostics.Count; index++)
                {
                    if (diagnostics[index].Severity != severity)
                    {
                        continue;
                    }

                    var detail = details != null && index < details.Count ? details[index] : null;
                    AddDiagnostic(diagnostics[index], detail);
                }
            }

            style.display = DisplayStyle.Flex;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 진단 메시지와 선택적 위치 설명을 한 행에 표시한다.
        /// </summary>
        // ------------------------------------------------------------
        private void AddDiagnostic(Diagnostic diagnostic, string detail)
        {
            var row = new VisualElement();
            row.AddToClassList("xeri-feedback-diagnostics__row");

            var indicator = new SeverityIndicator(diagnostic.Severity);
            indicator.AddToClassList("xeri-feedback-diagnostics__indicator");
            row.Add(indicator);

            var content = new VisualElement();
            content.AddToClassList("xeri-feedback-diagnostics__content");
            var message = new Label(diagnostic.Message);
            message.AddToClassList("xeri-feedback-diagnostics__message");
            content.Add(message);

            if (!string.IsNullOrEmpty(detail))
            {
                var lastDot = detail.LastIndexOf('.');
                var label = new Label(lastDot >= 0 ? detail[(lastDot + 1)..] : detail);
                label.AddToClassList("xeri-feedback-diagnostics__detail");
                label.tooltip = detail;
                content.Add(label);
            }

            row.Add(content);
            scroll.Add(row);
        }

    #endregion

    }
}
