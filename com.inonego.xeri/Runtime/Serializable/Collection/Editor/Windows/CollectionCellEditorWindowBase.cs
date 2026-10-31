/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : CollectionCellEditorWindowBase.cs
수정일 : 2026-10-31

# 설명
Key/Order 임시 편집창과 Value 실시간 편집창의 Resources 레이아웃과 진단 표시를 공유한다.
편집 버퍼, Commit과 Undo 소유권은 각각의 하위 창에 유지한다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace inonego.Xeri.Serializable.Editor
{
    // ============================================================
    /// <summary>
    /// Collection Cell Editor 창의 공통 표시 기능.
    /// </summary>
    // ============================================================
    internal abstract class CollectionCellEditorWindowBase : EditorWindow
    {

    #region 필드

        private DiagnosticListView diagnosticList;

    #endregion

    #region 레이아웃 생성

        // ------------------------------------------------------------
        /// <summary>
        /// 공통 창 레이아웃과 진단 영역을 구성한다.
        /// </summary>
        // ------------------------------------------------------------
        protected ScrollView BuildEditorLayout(string title, string caption)
        {
            var root = rootVisualElement;
            var layout = CollectionEditorResources.LoadEditWindowLayout();
            var styleSheet = CollectionEditorResources.LoadEditWindowStyle();

            root.Clear();
            root.AddToClassList("xeri-collection-editor-host");
            layout.CloneTree(root);
            var template = root.Q<TemplateContainer>();
            if (template != null)
            {
                template.AddToClassList("xeri-collection-editor-template");
            }

            if (!root.styleSheets.Contains(styleSheet))
            {
                root.styleSheets.Add(styleSheet);
            }

            root.Q<Label>("editor-title").text = title;
            rootVisualElement.Q<Label>("editor-caption").text = caption;
            diagnosticList = new DiagnosticListView();
            rootVisualElement.Q<VisualElement>("editor-status").Add(diagnosticList);
            return rootVisualElement.Q<ScrollView>("editor-scroll");
        }

    #endregion

    #region 진단과 해제

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 검증 결과를 공통 진단 영역에 표시한다.
        /// </summary>
        // ------------------------------------------------------------
        protected void ShowDiagnostics(IReadOnlyList<CollectionCellDiagnostic> diagnostics)
        {
            if (diagnosticList == null)
            {
                return;
            }

            if (diagnostics == null || diagnostics.Count == 0)
            {
                diagnosticList.SetDiagnostics(Array.Empty<Diagnostic>());
                return;
            }

            var items = new List<Diagnostic>(diagnostics.Count);
            var details = new List<string>(diagnostics.Count);
            foreach (var diagnostic in diagnostics)
            {
                items.Add(diagnostic.Diagnostic);
                details.Add(diagnostic.PropertyPath);
            }

            diagnosticList.SetDiagnostics(items, details);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 창 전환·종료 시 진단과 직렬화 UI 바인딩을 함께 해제한다.
        /// </summary>
        // ------------------------------------------------------------
        protected void ReleaseEditorLayout()
        {
            rootVisualElement.Unbind();
            rootVisualElement.Clear();
            diagnosticList = null;
        }

    #endregion

    }
}
