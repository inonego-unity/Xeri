/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : ValueDrawer.cs
수정일 : 2026-10-30

# 설명
Value<T> 전용 UI Toolkit PropertyDrawer.
Inspector에서 [Field Name | Base | 현재값 필드 | ☐] 1행 레이아웃을 렌더링한다.
체크박스 체크 또는 필드 클릭/드래그 → 편집 모드(초록 테두리).
체크박스 해제 → 값 적용 후 표시 모드 복귀.
int·float 타입만 지원하며 그 외는 기본 PropertyField로 폴백한다.

# 특이사항
Undo 복원 시 공통 ValueSerializationTracker가 이전 상태를 비교하고 통지한다.
편집 모드(FieldEditor.IsEditing) 중에는 필드 갱신을 억제해 입력 중인 값을 보호한다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace inonego.Xeri
{
    using Serializable;

    // ============================================================
    /// <summary>
    /// Value&lt;T&gt; 전용 PropertyDrawer.
    /// </summary>
    // ============================================================
    [CustomPropertyDrawer(typeof(Value<>))]
    public class ValueDrawer : PropertyDrawer
    {

    #region GUI 생성 및 동기화

        // ------------------------------------------------------------
        /// <summary>
        /// 타입별 분기 후 전용 GUI를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var so       = property.serializedObject;
            var target   = so.targetObject;
            var baseProp = property.FindPropertyRelative("base");
            var instance = SerializedPropertyHelper.GetTargetObject(property);

            if (instance is Value<int> intValue)
            {
                return CreateGUI
                (
                    property, so, target, baseProp, intValue, new IntegerField()
                );
            }

            if (instance is Value<float> floatValue)
            {
                return CreateGUI
                (
                    property, so, target, baseProp, floatValue, new FloatField()
                );
            }

            return new PropertyField(property);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// int·float 공통 1행 레이아웃을 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        private static VisualElement CreateGUI<T>
        (
            SerializedProperty property, SerializedObject so,
            UnityEngine.Object target, SerializedProperty baseProp,
            Value<T> instance, BaseField<T> setField
        )
        where T : struct
        {
            var root = ValueDrawerHelper.CreateRoot();
            var ownerPath = property.propertyPath;

            // 동일한 값은 여러 Inspector에서도 하나의 직렬화 스냅샷을 공유한다.
            ValueSerializationTracker.Observe(target, ownerPath, instance);

            Value<T> ResolveInstance() => ValueDrawerHelper.ResolveInstance<Value<T>>(so, ownerPath);

            // UXML의 [fieldLabel] [editToggle] 사이에 타입별 Base 입력 필드를 연결한다.
            var row        = root.Q<VisualElement>("value-base-row");
            var fieldLabel = root.Q<Label>("value-field-label");
            var editToggle = root.Q<Toggle>("value-base-toggle");

            fieldLabel.text = property.displayName;
            setField.label = "Base";
            setField.AddToClassList("xeri-set-field");
            setField.AddToClassList("xeri-value-base-field");
            setField.SetValueWithoutNotify(instance.Base);

            row.Insert(1, setField);

            // 토글·PointerDown·FocusIn 이벤트 등록 및 편집 모드 상태 관리
            T GetBase() => ResolveInstance()?.Base ?? default;

            void OnApply()
            {
                ValueDrawerHelper.ApplyBaseValue(so, target, ResolveInstance(), setField.value, "Set Value");
            }

            var editor = new ValueDrawerHelper.FieldEditor<T>(editToggle, setField, GetBase, OnApply);

            // 직렬화 복원은 공통 추적기에서 비교하고 일반 이벤트는 기준 상태만 갱신한다.
            void OnBasePropChanged(SerializedProperty _)
            {
                var current = ResolveInstance();
                if (current == null) return;

                ValueSerializationTracker.FinalizeSerializedChange(target, ownerPath, current);
                if (!editor.IsEditing)
                {
                    setField.SetValueWithoutNotify(current.Base);
                }
            }

            root.TrackPropertyValue(baseProp, OnBasePropChanged);

            root.Bind(so);

            // 50ms 폴링 — Play Mode에서 외부 수정을 실시간 반영
            void Tick()
            {
                var current = ResolveInstance();
                if (!editor.IsEditing && current != null)
                {
                    setField.SetValueWithoutNotify(current.Base);
                }
            }

            ValueDrawerHelper.StartPolling(root, Tick);

            return root;
        }

    #endregion

    }
}
