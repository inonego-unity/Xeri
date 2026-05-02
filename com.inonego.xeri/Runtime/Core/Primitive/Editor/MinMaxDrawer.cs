/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : MinMaxDrawer.cs
수정일 : 2026-05-03

# 설명
MinMax<T> 전용 PropertyDrawer.
Inspector에서 [Label | Min field | Max field] 단일 행 레이아웃을 렌더링한다.
int·float 타입에 한해 Min > Max가 되면 자동으로 클램프해 불변 조건을 유지한다.
int·float 타입은 레이블 드래그로 값 조정이 가능하다.

# 특이사항
TrackPropertyValue + ApplyModifiedPropertiesWithoutUndo 조합은 드래그 경계에서
값이 빠르게 왕복하며 UI가 깜빡이는 문제를 유발한다.
클램프를 드래그 종료(PointerCaptureOutEvent)와 포커스 아웃(FocusOutEvent) 시점으로
늦추어 깜빡임을 제거한다. 드래그 도중에는 Min > Max가 잠깐 허용된다.
========================================================================= BLOCK_HEADER_END */

using System;

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

            // 수정된 필드가 경계를 넘으면 자기 자신을 클램프한다.
            void ValidateMin() { minProp.serializedObject.Update(); ClampProp(minProp, maxProp, atMost: true); }
            void ValidateMax() { maxProp.serializedObject.Update(); ClampProp(maxProp, minProp, atMost: false); }

            var minField = CreateSubField("Min", minProp, ValidateMin);
            var maxField = CreateSubField("Max", maxProp, ValidateMax);
            maxField.style.marginLeft = 4;

            row.Add(label);
            row.Add(minField);
            row.Add(maxField);

            row.Bind(property.serializedObject);

            return row;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// int·float는 드래그 지원 전용 필드를, 그 외는 레이블+PropertyField 그룹을 반환한다.
        /// onCommit은 드래그 종료 및 포커스 아웃 시 호출된다.
        /// </summary>
        // ------------------------------------------------------------
        private static VisualElement CreateSubField(string subLabel, SerializedProperty prop, Action onCommit = null)
        {
            switch (prop.propertyType)
            {
                case SerializedPropertyType.Integer:
                {
                    var field = new IntegerField(subLabel);
                    SetupNumericField(field, field.labelElement, prop, onCommit);
                    return field;
                }
                case SerializedPropertyType.Float:
                {
                    var field = new FloatField(subLabel);
                    SetupNumericField(field, field.labelElement, prop, onCommit);
                    return field;
                }
                default:
                {
                    var group = new VisualElement();
                    group.style.flexDirection = FlexDirection.Row;
                    group.style.flexGrow      = 1;
                    group.style.alignItems    = Align.Center;

                    var groupLabel = new Label(subLabel);
                    groupLabel.style.minWidth       = 26;
                    groupLabel.style.flexShrink     = 0;
                    groupLabel.style.marginRight    = 1;
                    groupLabel.style.unityTextAlign = TextAnchor.MiddleRight;

                    var field = new PropertyField(prop, string.Empty);
                    field.style.flexGrow  = 1;
                    field.style.flexBasis = 0;
                    field.style.minWidth  = 0;

                    group.Add(groupLabel);
                    group.Add(field);
                    return group;
                }
            }
        }

        // --------------------------------------------------------------------------------
        /// <summary>
        /// 스타일을 적용하고 prop에 바인딩한 뒤 커밋 콜백을 등록한다.
        /// </summary>
        // --------------------------------------------------------------------------------
        private static void SetupNumericField(BindableElement field, VisualElement labelElement, SerializedProperty prop, Action onCommit)
        {
            ApplyDragLabelStyle(labelElement);
            ApplyFieldStyle(field);
            field.BindProperty(prop);
            RegisterCommitCallbacks(field, labelElement, onCommit);
        }

        // --------------------------------------------------------------------------------
        /// <summary>
        /// 드래그 종료(PointerCaptureOutEvent)와 포커스 아웃(FocusOutEvent) 콜백을 등록한다.
        /// </summary>
        // --------------------------------------------------------------------------------
        private static void RegisterCommitCallbacks(VisualElement field, VisualElement dragHandle, Action onCommit)
        {
            if (onCommit == null) return;

            field.RegisterCallback<FocusOutEvent>(_ => onCommit());
            // 레이블 드래그는 포인터 캡처를 사용하므로, 캡처 해제 시점이 드래그 종료다.
            dragHandle.RegisterCallback<PointerCaptureOutEvent>(_ => onCommit());
        }

        internal static void ApplyDragLabelStyle(VisualElement labelElement)
        {
            labelElement.style.minWidth    = 0;
            labelElement.style.flexShrink  = 0;
            labelElement.style.marginLeft  = 4;
            labelElement.style.marginRight = 4;
        }

        internal static void ApplyFieldStyle(VisualElement field)
        {
            field.style.flexGrow  = 1;
            field.style.flexBasis = 0;
            field.style.minWidth  = 30;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// target을 bound에 대해 클램프한다.
        /// atMost=true: target ≤ bound (min이 max를 초과하면 내린다).
        /// atMost=false: target ≥ bound (max가 min 미만이면 올린다).
        /// </summary>
        // ------------------------------------------------------------
        private static void ClampProp(SerializedProperty target, SerializedProperty bound, bool atMost)
        {
            switch (target.propertyType)
            {
                case SerializedPropertyType.Integer:
                {
                    var tInt = target.intValue;
                    var bInt = bound.intValue;
                    if (atMost ? tInt > bInt : tInt < bInt)
                    {
                        target.intValue = bInt;
                        target.serializedObject.ApplyModifiedPropertiesWithoutUndo();
                    }
                    break;
                }
                case SerializedPropertyType.Float:
                {
                    var tFloat = target.floatValue;
                    var bFloat = bound.floatValue;
                    if (atMost ? tFloat > bFloat : tFloat < bFloat)
                    {
                        target.floatValue = bFloat;
                        target.serializedObject.ApplyModifiedPropertiesWithoutUndo();
                    }
                    break;
                }
            }
        }
    }
}
