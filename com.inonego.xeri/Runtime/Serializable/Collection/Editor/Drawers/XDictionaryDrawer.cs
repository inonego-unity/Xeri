/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : XDictionaryDrawer.cs
수정일 : 2026-11-19

# 설명
XDictionary_RR/RV/VR/VV를 공통 컬렉션 표(CollectionTable)로 표시하는 UI Toolkit PropertyDrawer.
========================================================================= BLOCK_HEADER_END */

using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace inonego.Xeri.Serializable.Editor
{
    // ============================================================
    /// <summary>
    /// XDictionary 계열 공통 PropertyDrawer.
    /// </summary>
    // ============================================================
    [CustomPropertyDrawer(typeof(XDictionary_RR<,>))]
    [CustomPropertyDrawer(typeof(XDictionary_RV<,>))]
    [CustomPropertyDrawer(typeof(XDictionary_VR<,>))]
    [CustomPropertyDrawer(typeof(XDictionary_VV<,>))]
    public sealed class XDictionaryDrawer : PropertyDrawer
    {
        // ------------------------------------------------------------
        /// <summary>
        /// XDictionary 속성을 공통 컬렉션 표 UI로 만든다.
        /// </summary>
        // ------------------------------------------------------------
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            return CollectionDrawerGUI.Create(property, fieldInfo, preferredLabel);
        }
    }
}
