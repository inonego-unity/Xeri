/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : XPriorityQueueDrawer.cs
수정일 : 2026-10-31

# 설명
XPriorityQueue를 CollectionEditorView로 표시하고 heap invariant 복구를 Editor Model에 위임한다.
========================================================================= BLOCK_HEADER_END */

using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace inonego.Xeri.Serializable.Editor
{
    // ============================================================
    /// <summary>
    /// XPriorityQueue PropertyDrawer.
    /// </summary>
    // ============================================================
    [CustomPropertyDrawer(typeof(XPriorityQueue<,>))]
    public sealed class XPriorityQueueDrawer : PropertyDrawer
    {
        /// <inheritdoc />
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            return CollectionEditorDrawerGUI.CreateForType(property, fieldInfo);
        }
    }
}
