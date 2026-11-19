/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : XQueueDrawer.cs
수정일 : 2026-11-19

# 설명
XQueue 계열을 공통 컬렉션 표(CollectionTable)로 표시하는 UI Toolkit PropertyDrawer.
========================================================================= BLOCK_HEADER_END */

using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace inonego.Xeri.Serializable.Editor
{
    // ============================================================
    /// <summary>
    /// XQueue 계열 PropertyDrawer.
    /// </summary>
    // ============================================================
    [CustomPropertyDrawer(typeof(XQueue_R<>))]
    [CustomPropertyDrawer(typeof(XQueue_V<>))]
    public sealed class XQueueDrawer : PropertyDrawer
    {
        // ------------------------------------------------------------
        /// <summary>
        /// XQueue 속성을 공통 컬렉션 표 UI로 만든다.
        /// </summary>
        // ------------------------------------------------------------
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            return CollectionDrawerGUI.Create(property, fieldInfo, preferredLabel);
        }
    }
}
