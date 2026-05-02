/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : XNullableDrawer.cs
수정일 : 2026-05-02

# 설명
XNullable<T> 전용 UI Toolkit PropertyDrawer.
Inspector에서 [Label | Value field | Toggle] 단일 행 레이아웃을 렌더링한다.
Toggle이 체크 해제되면 Value field를 비활성화한다.
========================================================================= BLOCK_HEADER_END */

using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace inonego.Xeri
{
    using Serializable;

    // ============================================================
    /// <summary>
    /// XNullable&lt;T&gt; 전용 PropertyDrawer.
    /// </summary>
    // ============================================================
    [CustomPropertyDrawer(typeof(XNullable<>))]
    public class XNullableDrawer : PropertyDrawer
    {
        // ------------------------------------------------------------
        /// <summary>
        /// [Label | Value | Toggle] 단일 행 레이아웃을 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var hasValueProp = property.FindPropertyRelative("hasValue");
            var valueProp    = property.FindPropertyRelative("value");

            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems    = Align.Center;

            var label = new Label(property.displayName);
            label.AddToClassList("unity-base-field__label");

            var valueField = new PropertyField(valueProp, string.Empty);
            valueField.style.flexGrow   = 1;
            valueField.style.flexShrink = 1;
            valueField.SetEnabled(hasValueProp.boolValue);

            var toggle = new Toggle();
            toggle.style.flexShrink = 0;
            toggle.style.marginLeft = 6;
            toggle.BindProperty(hasValueProp);

            // hasValue 변경 시(Undo 포함) value field 활성화 상태를 동기화한다.
            row.TrackPropertyValue(hasValueProp, p => valueField.SetEnabled(p.boolValue));

            row.Add(label);
            row.Add(valueField);
            row.Add(toggle);

            row.Bind(property.serializedObject);

            return row;
        }
    }
}
