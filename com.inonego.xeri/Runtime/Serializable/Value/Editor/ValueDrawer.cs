/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : ValueDrawer.cs
수정일 : 2026-05-03

# 설명
Value<T> 전용 UI Toolkit PropertyDrawer.
Inspector에서 [Field Name | Base | 현재값 필드 | ☐] 1행 레이아웃을 렌더링한다.
체크박스 체크 또는 필드 클릭/드래그 → 편집 모드(초록 테두리).
체크박스 해제 → 값 적용 후 표시 모드 복귀.
int·float 타입만 지원하며 그 외는 기본 PropertyField로 폴백한다.

# 특이사항
Undo 복원 시 backing field는 C++ 직렬화로 이미 복원된다.
lastKnownBase 캐시로 복원 여부를 감지해 InvokeOnBaseChange로 이벤트를 강제 발화한다.
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

    #region 메서드

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
                return CreateGUI(property, so, target, baseProp, intValue,
                                 new IntegerField(), p => p.intValue);
            if (instance is Value<float> floatValue)
                return CreateGUI(property, so, target, baseProp, floatValue,
                                 new FloatField(), p => p.floatValue);

            return new PropertyField(property);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// int·float 공통 1행 레이아웃을 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        private static VisualElement CreateGUI<T>(
            SerializedProperty property,
            SerializedObject so,
            UnityEngine.Object target,
            SerializedProperty baseProp,
            Value<T> instance,
            BaseField<T> setField,
            Func<SerializedProperty, T> readProp
        ) where T : struct
        {
            var root          = new VisualElement();
            var lastKnownBase = instance.Base;

            ValueDrawerHelper.ApplyStylesheet(root);

            // [fieldLabel] [Base] [setField] [editToggle]
            var row        = ValueDrawerHelper.CreateRow();
            var fieldLabel = ValueDrawerHelper.CreateFieldLabel(property.displayName);
            var editToggle = ValueDrawerHelper.CreateEditToggle();

            setField.label = "Base";
            setField.style.flexGrow = 1;
            setField.AddToClassList("xeri-set-field");
            setField.SetValueWithoutNotify(instance.Base);

            row.Add(fieldLabel);
            row.Add(setField);
            row.Add(editToggle);
            root.Add(row);

            // 토글·PointerDown·FocusIn 이벤트 등록 및 편집 모드 상태 관리
            T GetBase() => instance.Base;

            void OnApply()
            {
                Undo.RecordObject(target, "Set Value");
                instance.Set(setField.value, invokeEvent: true);
                lastKnownBase = instance.Base;
                EditorUtility.SetDirty(target);
                so.Update();
            }

            var editor = new ValueDrawerHelper.FieldEditor<T>(editToggle, setField, GetBase, OnApply);

            // Undo 감지 — Undo 시 C++ 직렬화로 backing field가 복원된 뒤 콜백이 발화됨
            // instance.Base == baseProp.value (둘 다 복원됨) → Set() 호출 시 prev==next → 이벤트 미발화
            // lastKnownBase 캐시와 비교해 실제 변경 여부를 감지하고 이벤트를 강제 발화
            void OnBasePropChanged(SerializedProperty _)
            {
                var restored = readProp(baseProp);

                if (!EqualityComparer<T>.Default.Equals(lastKnownBase, restored))
                {
                    instance.InvokeOnBaseChange(lastKnownBase);
                    lastKnownBase = restored;
                }

                if (!editor.IsEditing)
                    setField.SetValueWithoutNotify(restored);
            }

            root.TrackPropertyValue(baseProp, OnBasePropChanged);
            root.Bind(so);

            // 50ms 폴링 — Play Mode에서 외부 수정을 실시간 반영
            void Tick()
            {
                if (!editor.IsEditing)
                    setField.SetValueWithoutNotify(instance.Base);
            }

            ValueDrawerHelper.StartPolling(root, Tick);

            return root;
        }

    #endregion

    }
}
