/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : XPriorityQueueDrawer.cs
수정일 : 2026-11-19

# 설명
XPriorityQueue를 공통 컬렉션 표(CollectionTable)로 표시한다. 힙 정규화는 편집 세션이 맡는다.
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
        // ------------------------------------------------------------
        /// <summary>
        /// XPriorityQueue 속성을 공통 컬렉션 표 UI로 만든다.
        /// </summary>
        // ------------------------------------------------------------
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            return CollectionDrawerGUI.Create(property, fieldInfo, preferredLabel);
        }
    }
}
