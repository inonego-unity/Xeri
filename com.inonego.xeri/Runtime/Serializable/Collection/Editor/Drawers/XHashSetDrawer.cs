/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : XHashSetDrawer.cs
수정일 : 2026-11-19

# 설명
XHashSet 계열을 공통 컬렉션 표(CollectionTable)로 표시하는 UI Toolkit PropertyDrawer.
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
        // ------------------------------------------------------------
        /// <summary>
        /// XHashSet 속성을 공통 컬렉션 표 UI로 만든다.
        /// </summary>
        // ------------------------------------------------------------
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            return CollectionDrawerGUI.Create(property, fieldInfo, preferredLabel);
        }
    }
}
