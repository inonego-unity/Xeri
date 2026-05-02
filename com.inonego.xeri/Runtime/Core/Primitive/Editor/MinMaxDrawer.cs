/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : MinMaxDrawer.cs
수정일 : 2026-05-02

# 설명
MinMax<T> 전용 PropertyDrawer.
Inspector에서 [Label | Min field | Max field] 단일 행 레이아웃을 렌더링한다.
int·float 타입에 한해 Min > Max가 되면 자동으로 클램프해 불변 조건을 유지한다.
========================================================================= BLOCK_HEADER_END */

using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace inonego.Xeri
{
    using Primitive;

    // ============================================================
    /// <summary>
    /// MinMax&lt;T&gt; 전용 PropertyDrawer.
    /// </summary>
    // ============================================================
    [CustomPropertyDrawer(typeof(MinMax<>))]
    public class MinMaxDrawer : PropertyDrawer
    {
        // ------------------------------------------------------------
        /// <summary>
        /// [Label | Min field | Max field] 단일 행 레이아웃을 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var minProp = property.FindPropertyRelative("min");
            var maxProp = property.FindPropertyRelative("max");

            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems    = Align.Center;

            var label = new Label(property.displayName);
            label.AddToClassList("unity-base-field__label");

            var minField = CreateSubField("Min", minProp);
            var maxField = CreateSubField("Max", maxProp);
            maxField.style.marginLeft = 4;

            // Min > Max 또는 Max < Min이 되면 경계값으로 클램프한다 (int·float 지원).
            row.TrackPropertyValue(minProp, p => ClampMin(p, maxProp));
            row.TrackPropertyValue(maxProp, p => ClampMax(minProp, p));

            row.Add(label);
            row.Add(minField);
            row.Add(maxField);

            row.Bind(property.serializedObject);

            return row;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 서브 레이블 + PropertyField 조합 그룹을 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        private static VisualElement CreateSubField(string subLabel, SerializedProperty prop)
        {
            var group = new VisualElement();
            group.style.flexDirection = FlexDirection.Row;
            group.style.flexGrow      = 1;
            group.style.alignItems    = Align.Center;

            var label = new Label(subLabel);
            label.style.minWidth        = 26;
            label.style.flexShrink      = 0;
            label.style.marginRight     = 1;
            label.style.unityTextAlign  = TextAnchor.MiddleRight;

            var field = new PropertyField(prop, string.Empty);
            field.style.flexGrow  = 1;
            field.style.flexBasis = 0;
            field.style.minWidth  = 0;
            field.BindProperty(prop);

            group.Add(label);
            group.Add(field);

            return group;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// min이 max를 초과하면 max 값으로 클램프한다.
        /// </summary>
        // ------------------------------------------------------------
        private static void ClampMin(SerializedProperty minProp, SerializedProperty maxProp)
        {
            switch (minProp.propertyType)
            {
                case SerializedPropertyType.Integer:
                    if (minProp.intValue > maxProp.intValue)
                    {
                        minProp.intValue = maxProp.intValue;
                        minProp.serializedObject.ApplyModifiedPropertiesWithoutUndo();
                    }
                    break;

                case SerializedPropertyType.Float:
                    if (minProp.floatValue > maxProp.floatValue)
                    {
                        minProp.floatValue = maxProp.floatValue;
                        minProp.serializedObject.ApplyModifiedPropertiesWithoutUndo();
                    }
                    break;
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// max가 min 미만이면 min 값으로 클램프한다.
        /// </summary>
        // ------------------------------------------------------------
        private static void ClampMax(SerializedProperty minProp, SerializedProperty maxProp)
        {
            switch (maxProp.propertyType)
            {
                case SerializedPropertyType.Integer:
                    if (maxProp.intValue < minProp.intValue)
                    {
                        maxProp.intValue = minProp.intValue;
                        maxProp.serializedObject.ApplyModifiedPropertiesWithoutUndo();
                    }
                    break;

                case SerializedPropertyType.Float:
                    if (maxProp.floatValue < minProp.floatValue)
                    {
                        maxProp.floatValue = minProp.floatValue;
                        maxProp.serializedObject.ApplyModifiedPropertiesWithoutUndo();
                    }
                    break;
            }
        }
    }
}
