/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : HelpBoxAttributeDrawer.cs
수정일 : 2026-05-02

# 설명
HelpBoxAttribute 전용 PropertyDrawer.
어트리뷰트가 붙은 필드 위에 HelpBoxElement를 표시하고, 그 아래에 원본 PropertyField를 배치한다.
========================================================================= BLOCK_HEADER_END */

using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace inonego.Xeri
{
    // ============================================================
    /// <summary>
    /// HelpBoxAttribute 전용 PropertyDrawer.
    /// </summary>
    // ============================================================
    [CustomPropertyDrawer(typeof(HelpBoxAttribute))]
    public class HelpBoxAttributeDrawer : PropertyDrawer
    {
        // ------------------------------------------------------------
        /// <summary>
        /// HelpBoxElement + PropertyField 조합을 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var attr = (HelpBoxAttribute)attribute;

            var container = new VisualElement();

            // 커스텀 아이콘 이름이 있으면 그것 우선, 없으면 메시지 타입 기반
            var helpBox = string.IsNullOrEmpty(attr.CustomIconName)
                ? new HelpBoxElement(attr.Message, attr.MessageType)
                : new HelpBoxElement(attr.Message, attr.CustomIconName);

            container.Add(helpBox);
            container.Add(new PropertyField(property));

            return container;
        }
    }
}
