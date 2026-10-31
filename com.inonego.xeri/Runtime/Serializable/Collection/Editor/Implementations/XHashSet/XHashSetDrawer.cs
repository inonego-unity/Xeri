/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : XHashSetDrawer.cs
수정일 : 2026-10-31

# 설명
XHashSet 계열을 CollectionEditorView로 표시하는 UI Toolkit PropertyDrawer.
========================================================================= BLOCK_HEADER_END */

using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace inonego.Xeri.Serializable.Editor
{
    // ============================================================
    /// <summary>
    /// XHashSet 계열 PropertyDrawer.
    /// </summary>
    // ============================================================
    [CustomPropertyDrawer(typeof(XHashSet_R<>))]
    [CustomPropertyDrawer(typeof(XHashSet_V<>))]
    public sealed class XHashSetDrawer : PropertyDrawer
    {
        /// <inheritdoc />
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            return CollectionEditorDrawerGUI.CreateForType(property, fieldInfo);
        }
    }
}
