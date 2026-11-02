/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : XOrderedDrawer.cs
수정일 : 2026-10-31

# 설명
XOrdered 계열을 CollectionEditorView로 표시하고 행 이동과 Order 정규화를 Editor Model에 위임한다.
========================================================================= BLOCK_HEADER_END */

using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace inonego.Xeri.Serializable.Editor
{
    // ============================================================
    /// <summary>
    /// XOrdered 계열 PropertyDrawer.
    /// </summary>
    // ============================================================
    [CustomPropertyDrawer(typeof(XOrdered<,>))]
    [CustomPropertyDrawer(typeof(XOrdered<,,>))]
    public sealed class XOrderedDrawer : PropertyDrawer
    {
        /// <inheritdoc />
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            return CollectionEditorDrawerGUI.CreateForType(property, fieldInfo);
        }
    }
}
