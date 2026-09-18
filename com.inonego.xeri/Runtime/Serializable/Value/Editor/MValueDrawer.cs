/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : MValueDrawer.cs
수정일 : 2026-09-18

# 설명
MValue<T> 전용 UI Toolkit PropertyDrawer.
Inspector에서 [Field Name | Base | setField | → | modifiedField(읽기전용) | ☐] 1행 레이아웃을 렌더링한다.
체크박스 체크 또는 필드 클릭/드래그 → 편집 모드(초록 테두리).
체크박스 해제 → 값 적용 후 표시 모드 복귀.
int·float 타입만 지원하며 그 외는 기본 PropertyField로 폴백한다.

# 특이사항
Undo 복원 시 MValue의 serialization callback이 runtime state를 재구성한다.
Drawer는 OnBaseChange / OnModifiedChange와 복원 전후 값 비교로 UI와 누락 이벤트만 동기화한다.
modifiedField는 항상 disabled 상태이며 xeri-set-field 클래스로 회색 처리를 방지한다.
편집 모드(FieldEditor.IsEditing) 중에는 setField 외부 갱신을 억제하고 modifiedField는 즉시 갱신한다.
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
            var instance = SerializedPropertyHelper.GetTargetObject(property);

            if (instance is MValue<int> intValue)
            {
                return CreateGUI
                (
                    property, so, target, intValue,
                    new IntegerField(), new IntegerField()
                );
            }

            if (instance is MValue<float> floatValue)
            {
                return CreateGUI
                (
                    property, so, target, floatValue,
                    new FloatField(), new FloatField()
                );
            }

            return new PropertyField(property);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> int·float 공통 1행 레이아웃을 반환한다.
        /// <br/> setField는 Base값 표시와 편집을 담당한다.
        /// <br/> modifiedField는 읽기 전용으로 Modified값을 표시한다.
        /// </summary>
        // ------------------------------------------------------------
        private static VisualElement CreateGUI<T>
        (
            SerializedProperty property,
            SerializedObject so,
            UnityEngine.Object target,
            MValue<T> instance,
            BaseField<T> setField,
            BaseField<T> modifiedField
        )
        where T : struct
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

            // 일반 API 변경은 Value 이벤트를 통해 즉시 UI와 lastKnown 상태에 반영한다.
            void HandleBaseChange(object _, ValueChangeEventArgs<T> e)
            {
                lastKnownBase = e.Current;

                if (!editor.IsEditing)
                {
                    setField.SetValueWithoutNotify(e.Current);
                }
            }

            void HandleModifiedChange(object _, ValueChangeEventArgs<T> e)
            {
                lastKnownModified = e.Current;
                modifiedField.SetValueWithoutNotify(e.Current);
            }

            void HandleAttach(AttachToPanelEvent _)
            {
                instance.OnBaseChange -= HandleBaseChange;
                instance.OnBaseChange += HandleBaseChange;
                instance.OnModifiedChange -= HandleModifiedChange;
                instance.OnModifiedChange += HandleModifiedChange;

                lastKnownBase = instance.Base;
                lastKnownModified = instance.Modified;

                if (!editor.IsEditing)
                {
                    setField.SetValueWithoutNotify(lastKnownBase);
                }

                modifiedField.SetValueWithoutNotify(lastKnownModified);
            }

            void HandleDetach(DetachFromPanelEvent _)
            {
                instance.OnBaseChange -= HandleBaseChange;
                instance.OnModifiedChange -= HandleModifiedChange;
            }

            root.RegisterCallback<AttachToPanelEvent>(HandleAttach);
            root.RegisterCallback<DetachFromPanelEvent>(HandleDetach);

            // Undo 등 serialized state 복원 후 누락된 Value 이벤트와 UI를 동기화한다.
            void OnSerializedObjectChanged(SerializedObject _)
            {
                SynchronizeSerializedState
                (
                    instance,
                    lastKnownBase,
                    lastKnownModified
                );

                lastKnownBase = instance.Base;
                lastKnownModified = instance.Modified;

                if (!editor.IsEditing)
                {
                    setField.SetValueWithoutNotify(lastKnownBase);
                }

                modifiedField.SetValueWithoutNotify(lastKnownModified);
            }

            root.TrackSerializedObjectValue(so, OnSerializedObjectChanged);
            root.Bind(so);

            return root;
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// 역직렬화 후 현재 MValue state를 기준으로 누락된 변경 이벤트를 전달한다.
        /// </summary>
        // ----------------------------------------------------------------------
        internal static void SynchronizeSerializedState<T>
        (
            MValue<T> instance,
            T previousBase,
            T previousModified
        )
        {
            if (instance == null)
            {
                throw new ArgumentNullException(nameof(instance));
            }

            if (!EqualityComparer<T>.Default.Equals(previousBase, instance.Base))
            {
                instance.InvokeOnBaseChange(previousBase);
            }

            if (!EqualityComparer<T>.Default.Equals(previousModified, instance.Modified))
            {
                instance.InvokeOnModifiedChange(previousModified);
            }
        }

    #endregion

    }
}
