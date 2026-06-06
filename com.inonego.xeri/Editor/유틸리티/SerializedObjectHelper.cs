/* BLOCK_HEADER_BEGIN =======================================================================
파일명: SerializedObjectHelper.cs
수정일: 2026-05-20

# 설명
SerializedObject의 프로퍼티를 IMGUI 또는 UI Toolkit으로 그리기 위한 헬퍼.
m_Script 필드는 제외하고 표시 가능한 직계 프로퍼티를 순회한다.
========================================================================= BLOCK_HEADER_END */

using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace inonego.Xeri
{
    // ============================================================
    /// <summary>
    /// SerializedObject 렌더링 헬퍼.
    /// </summary>
    // ============================================================
    public static class SerializedObjectHelper
    {
        // ------------------------------------------------------------
        /// <summary>
        /// SerializedObject의 모든 표시 가능한 프로퍼티를 IMGUI로 그린다.
        /// </summary>
        // ------------------------------------------------------------
        public static void DrawAll(SerializedObject serializedObject)
        {
            SerializedProperty iterator = serializedObject.GetIterator();
            bool enterChildren = true;

            while (iterator.NextVisible(enterChildren))
            {
                enterChildren = false;

                if (iterator.name == "m_Script")
                {
                    continue;
                }

                EditorGUILayout.PropertyField(iterator, true);
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// SerializedObject의 모든 표시 가능한 프로퍼티를 포함하는 UI Toolkit 요소를 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        public static VisualElement CreateAll(SerializedObject serializedObject)
        {
            var root = new VisualElement();

            SerializedProperty iterator = serializedObject.GetIterator();
            bool enterChildren = true;

            while (iterator.NextVisible(enterChildren))
            {
                enterChildren = false;

                if (iterator.name == "m_Script")
                {
                    continue;
                }

                root.Add(new PropertyField(iterator.Copy()));
            }

            root.Bind(serializedObject);
            return root;
        }
    }
}
