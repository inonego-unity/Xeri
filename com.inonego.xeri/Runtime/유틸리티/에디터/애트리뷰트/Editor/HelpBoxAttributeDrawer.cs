/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : HelpBoxAttributeDrawer.cs
수정일 : 2026-05-02

# 설명
HelpBoxAttribute 전용 DecoratorDrawer.
어트리뷰트가 붙은 필드 위에 HelpBoxElement를 표시한다. 필드 렌더링은 Unity가 처리한다.
========================================================================= BLOCK_HEADER_END */

using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace inonego.Xeri
{
    // ============================================================
    /// <summary>
    /// HelpBoxAttribute 전용 DecoratorDrawer.
    /// </summary>
    // ============================================================
    [CustomPropertyDrawer(typeof(HelpBoxAttribute))]
    public class HelpBoxAttributeDrawer : DecoratorDrawer
    {
        // ------------------------------------------------------------
        /// <summary>
        /// HelpBoxElement를 반환한다. 필드 렌더링은 Unity가 처리한다.
        /// </summary>
        // ------------------------------------------------------------
        public override VisualElement CreatePropertyGUI()
        {
            var attr = (HelpBoxAttribute)attribute;

            // 커스텀 아이콘 이름이 있으면 그것 우선, 없으면 메시지 타입 기반
            return string.IsNullOrEmpty(attr.CustomIconName)
                ? new HelpBoxElement(attr.Message, attr.MessageType)
                : new HelpBoxElement(attr.Message, attr.CustomIconName);
        }
    }
}
