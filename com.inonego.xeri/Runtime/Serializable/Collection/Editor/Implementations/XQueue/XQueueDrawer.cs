/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : XQueueDrawer.cs
수정일 : 2026-10-31

# 설명
XQueue 계열을 CollectionEditorView로 표시하는 UI Toolkit PropertyDrawer.
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
        /// <inheritdoc />
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            return CollectionEditorDrawerGUI.CreateForType(property, fieldInfo);
        }
    }
}
