/* BLOCK_HEADER_BEGIN ======================================================================================
파일명 : MValueDrawer.cs
수정일 : 2026-05-03

# 설명
MValue<T> 전용 UI Toolkit PropertyDrawer.
Inspector에서 [Field Name | Base | setField | → | modifiedField(읽기전용) | ☐] 1행 레이아웃을 렌더링한다.
체크박스 체크 또는 필드 클릭/드래그 → 편집 모드(초록 테두리).
체크박스 해제 → 값 적용 후 표시 모드 복귀.
int·float 타입만 지원하며 그 외는 기본 PropertyField로 폴백한다.

# 특이사항
Undo 복원 시 backing field는 C++ 직렬화로 이미 복원된다.
lastKnownBase / lastKnownModified 캐시로 복원 여부를 감지해 InvokeOn* 메서드로 이벤트를 강제 발화한다.
modifiedField는 항상 disabled 상태이며 xeri-set-field 클래스로 회색 처리를 방지한다.
편집 모드(FieldEditor.IsEditing) 중에는 setField 갱신을 억제하고 modifiedField는 항상 갱신한다.
======================================================================================== BLOCK_HEADER_END */

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
    /// MValue&lt;T&gt; 전용 PropertyDrawer.
    /// </summary>
    // ============================================================
    [CustomPropertyDrawer(typeof(MValue<>))]
    public class MValueDrawer : PropertyDrawer
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

            if (instance is MValue<int> intValue)
                return CreateGUI(property, so, target, baseProp, intValue,
                                 new IntegerField(), new IntegerField(), p => p.intValue);
            if (instance is MValue<float> floatValue)
                return CreateGUI(property, so, target, baseProp, floatValue,
                                 new FloatField(), new FloatField(), p => p.floatValue);

            return new PropertyField(property);
        }

        // -----------------------------------------------------------------------
        /// <summary>
        /// <br/> int·float 공통 1행 레이아웃을 반환한다.
        /// <br/> setField는 Base값 표시/편집, modifiedField는 항상 읽기 전용으로 Modified값을 표시한다.
        /// </summary>
        // -----------------------------------------------------------------------
        private static VisualElement CreateGUI<T>(
            SerializedProperty property,
            SerializedObject so,
            UnityEngine.Object target,
            SerializedProperty baseProp,
            MValue<T> instance,
            BaseField<T> setField,
            BaseField<T> modifiedField,
            Func<SerializedProperty, T> readProp
        ) where T : struct
        {
            var root              = new VisualElement();
            var lastKnownBase     = instance.Base;
            var lastKnownModified = instance.Modified;

            ValueDrawerHelper.ApplyStylesheet(root);

            // [fieldLabel] [Base] [setField] [→] [modifiedField] [editToggle]
            var row        = ValueDrawerHelper.CreateRow();
            var fieldLabel = ValueDrawerHelper.CreateFieldLabel(property.displayName);
            var editToggle = ValueDrawerHelper.CreateEditToggle();

            var arrowLabel = new Label("→");
            arrowLabel.AddToClassList("xeri-arrow");

            setField.label = "Base";
            setField.style.flexGrow   = 1;
            setField.style.flexShrink = 1;
            setField.style.flexBasis  = 0;
            setField.AddToClassList("xeri-set-field");
            setField.SetValueWithoutNotify(instance.Base);

            // modifiedField는 항상 읽기 전용; xeri-set-field로 disabled 시 회색 처리 방지
            modifiedField.AddToClassList("xeri-set-field");
            modifiedField.AddToClassList("xeri-modified-field");
            modifiedField.SetValueWithoutNotify(instance.Modified);
            modifiedField.SetEnabled(false);

            row.Add(fieldLabel);
            row.Add(setField);
            row.Add(arrowLabel);
            row.Add(modifiedField);
            row.Add(editToggle);
            root.Add(row);

            // 토글·PointerDown·FocusIn 이벤트 등록 및 편집 모드 상태 관리
            T GetBase() => instance.Base;

            void OnApply()
            {
                Undo.RecordObject(target, "Set MValue");
                instance.Set(setField.value, invokeEvent: true);
                lastKnownBase     = instance.Base;
                lastKnownModified = instance.Modified;
                EditorUtility.SetDirty(target);
                so.Update();
            }

            var editor = new ValueDrawerHelper.FieldEditor<T>(editToggle, setField, GetBase, OnApply);

            // Undo 감지 — base / modified 각각 lastKnown 캐시로 비교해 이벤트 강제 발화
            // Modified는 backing field가 아닌 계산값이므로 baseProp 변경 시 함께 확인
            void OnBasePropChanged(SerializedProperty _)
            {
                var restoredBase     = readProp(baseProp);
                var restoredModified = instance.Modified;

                if (!EqualityComparer<T>.Default.Equals(lastKnownBase, restoredBase))
                {
                    instance.InvokeOnBaseChange(lastKnownBase);
                    lastKnownBase = restoredBase;
                }

                if (!EqualityComparer<T>.Default.Equals(lastKnownModified, restoredModified))
                {
                    instance.InvokeOnModifiedChange(lastKnownModified);
                    lastKnownModified = restoredModified;
                }

                if (!editor.IsEditing)
                    setField.SetValueWithoutNotify(restoredBase);

                // modifiedField는 편집 중에도 항상 최신 Modified값을 표시
                modifiedField.SetValueWithoutNotify(restoredModified);
            }

            root.TrackPropertyValue(baseProp, OnBasePropChanged);
            root.Bind(so);

            // 50ms 폴링 — Play Mode에서 Modifier 변동에 의한 Modified값 변화를 실시간 반영
            void Tick()
            {
                if (!editor.IsEditing)
                    setField.SetValueWithoutNotify(instance.Base);

                modifiedField.SetValueWithoutNotify(instance.Modified);
            }

            ValueDrawerHelper.StartPolling(root, Tick);

            return root;
        }

    #endregion

    }
}
