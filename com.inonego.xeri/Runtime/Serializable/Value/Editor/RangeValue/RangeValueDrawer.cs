/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : RangeValueDrawer.cs
수정일 : 2026-10-30

# 설명
RangeValue<T> 전용 UI Toolkit PropertyDrawer.
Inspector에서 2행 레이아웃을 렌더링한다.
  Row 1: [Field Name | Base | 현재값 필드 | ☐]
  Row 2: [spacer    | Range | Min 필드 | Max 필드 | ☐]
각 행의 체크박스를 독립적으로 체크/해제하여 편집·적용을 수행한다.
int·float 타입만 지원하며 그 외는 기본 PropertyField로 폴백한다.

# 특이사항
Undo 복원 시 공통 ValueSerializationTracker에서 Base와 Range를 함께 확정한다.
Base 행은 FieldEditor<T>로 편집 모드를 관리한다.
Range 해제(Apply) 시 min > max면 체크 상태를 유지하고 적용을 중단한다.
min > max 검증이 필요하므로 Range 행은 FieldEditor<T>를 사용하지 않고 로컬 함수로 관리한다.
Min·Max 레이블 드래그 또는 포커스 시 Range 편집 모드로 자동 진입한다.
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
    using Primitive;

    // ============================================================
    /// <summary>
    /// RangeValue&lt;T&gt; 전용 PropertyDrawer.
    /// </summary>
    // ============================================================
    [CustomPropertyDrawer(typeof(RangeValue<>))]
    public class RangeValueDrawer : PropertyDrawer
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

            var rangeBaseProp = property.FindPropertyRelative("range").FindPropertyRelative("base");
            var minProp       = rangeBaseProp.FindPropertyRelative("min");
            var maxProp       = rangeBaseProp.FindPropertyRelative("max");

            var instance = SerializedPropertyHelper.GetTargetObject(property);

            if (instance is RangeValue<int> intValue)
            {
                return CreateGUI
                (
                    property, so, target, baseProp, minProp, maxProp, intValue,
                    new IntegerField(),
                    CreateRangeSubField<int, IntegerField>("Min", intValue.Min),
                    CreateRangeSubField<int, IntegerField>("Max", intValue.Max)
                );
            }

            if (instance is RangeValue<float> floatValue)
            {
                return CreateGUI
                (
                    property, so, target, baseProp, minProp, maxProp, floatValue,
                    new FloatField(),
                    CreateRangeSubField<float, FloatField>("Min", floatValue.Min),
                    CreateRangeSubField<float, FloatField>("Max", floatValue.Max)
                );
            }

            return new PropertyField(property);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Range 행용 드래그 레이블 스타일이 적용된 서브 필드를 생성해 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        private static TField CreateRangeSubField<T, TField>(string fieldLabel, T initialValue)
        where TField : BaseField<T>, new()
        {
            var field = new TField();
            field.label = fieldLabel;
            field.AddToClassList("xeri-range-sub-field");
            field.AddToClassList("xeri-set-field");
            RangeDrawer.ApplyDragLabelStyle(field.labelElement);
            RangeDrawer.ApplyFieldStyle(field);
            field.SetValueWithoutNotify(initialValue);
            return field;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> int·float 공통 2행 레이아웃을 반환한다.
        /// <br/> 타입별 필드 종류만 인자로 전달한다.
        /// </summary>
        // ------------------------------------------------------------
        private static VisualElement CreateGUI<T>
        (
            SerializedProperty property,
            SerializedObject so,
            UnityEngine.Object target,
            SerializedProperty baseProp,
            SerializedProperty minProp,
            SerializedProperty maxProp,
            RangeValue<T> instance,
            BaseField<T> setBaseField,
            BaseField<T> setMinField,
            BaseField<T> setMaxField
        ) where T : struct, IComparable<T>
        {
            var root = ValueDrawerHelper.CreateRoot(includeRange: true);
            var ownerPath = property.propertyPath;
            var isEditingRange = false;

            ValueSerializationTracker.Observe(target, ownerPath, instance);

            RangeValue<T> ResolveInstance() => ValueDrawerHelper.ResolveInstance<RangeValue<T>>(so, ownerPath);

            // UXML 기본 행에 Base 필드를 연결한다.
            var row1           = root.Q<VisualElement>("value-base-row");
            var fieldLabel     = root.Q<Label>("value-field-label");
            var editToggleBase = root.Q<Toggle>("value-base-toggle");

            fieldLabel.text = property.displayName;
            setBaseField.label = "Base";
            setBaseField.AddToClassList("xeri-set-field");
            setBaseField.AddToClassList("xeri-value-base-field");
            setBaseField.SetValueWithoutNotify(instance.Base);

            row1.Insert(1, setBaseField);

            // UXML Range 행의 spacer와 두 값 필드를 연결한다.
            var row2            = root.Q<VisualElement>("value-range-row");
            var editToggleRange = root.Q<Toggle>("value-range-toggle");
            var spacer          = root.Q<VisualElement>("value-range-spacer");

            ValueDrawerHelper.BindSpacer(spacer, fieldLabel);
            setMinField.AddToClassList("xeri-range-min-field");

            row2.Insert(2, setMinField);
            row2.Insert(3, setMaxField);

            // Base 편집 모드 — FieldEditor<T>로 이벤트 등록 일괄 처리
            T GetBase() => ResolveInstance()?.Base ?? default;

            void OnBaseApply()
            {
                ValueDrawerHelper.ApplyBaseValue(so, target, ResolveInstance(), setBaseField.value, "Set Base");
            }

            var baseEditor = new ValueDrawerHelper.FieldEditor<T>
            (
                editToggleBase, setBaseField, GetBase, OnBaseApply
            );

            // Range 편집 모드 — min > max 검증이 있어 FieldEditor<T> 미사용, 로컬 함수로 관리
            void EnterRangeEditMode()
            {
                if (isEditingRange) return;
                isEditingRange = true;
                editToggleRange.SetValueWithoutNotify(true);
                var current = ResolveInstance();
                if (current == null) return;

                setMinField.SetValueWithoutNotify(current.Min);
                setMaxField.SetValueWithoutNotify(current.Max);
                ValueDrawerHelper.SetFieldBorder(setMinField, true);
                ValueDrawerHelper.SetFieldBorder(setMaxField, true);
            }

            void ExitRangeEditMode()
            {
                var minValue = setMinField.value;
                var maxValue = setMaxField.value;

                // min > max면 적용 중단하고 토글을 체크 상태로 유지
                if (Comparer<T>.Default.Compare(minValue, maxValue) > 0)
                {
                    editToggleRange.SetValueWithoutNotify(true);
                    return;
                }

                var newRange = new Range<T>(minValue, maxValue);
                Undo.RecordObject(target, "Set Range");
                var current = ResolveInstance();
                if (current == null) return;

                current.Range.Set(newRange, invokeEvent: true);
                EditorUtility.SetDirty(target);
                so.Update();

                isEditingRange = false;
                editToggleRange.SetValueWithoutNotify(false);
                ValueDrawerHelper.SetFieldBorder(setMinField, false);
                ValueDrawerHelper.SetFieldBorder(setMaxField, false);
            }

            void OnRangeToggleChanged(ChangeEvent<bool> evt)
            {
                if (evt.newValue)
                {
                    EnterRangeEditMode();
                }
                else
                {
                    ExitRangeEditMode();
                }
            }

            // Min·Max 레이블 드래그 또는 포커스 시 Range 편집 모드 자동 진입
            void OnMinLabelPointerDown(PointerDownEvent e) => EnterRangeEditMode();
            void OnMaxLabelPointerDown(PointerDownEvent e) => EnterRangeEditMode();
            void OnMinFocusIn(FocusInEvent e)              => EnterRangeEditMode();
            void OnMaxFocusIn(FocusInEvent e)              => EnterRangeEditMode();

            editToggleRange.RegisterValueChangedCallback(OnRangeToggleChanged);
            setMinField.labelElement.RegisterCallback<PointerDownEvent>(OnMinLabelPointerDown, TrickleDown.TrickleDown);
            setMaxField.labelElement.RegisterCallback<PointerDownEvent>(OnMaxLabelPointerDown, TrickleDown.TrickleDown);
            setMinField.RegisterCallback<FocusInEvent>(OnMinFocusIn);
            setMaxField.RegisterCallback<FocusInEvent>(OnMaxFocusIn);

            // 외부 변경은 하나의 Runtime 스냅샷을 기준으로 복원한다.
            void ReconcileCurrent()
            {
                var current = ResolveInstance();
                if (current == null) return;

                // Unity는 Min·Max를 각각 통지할 수 있으므로 유효한 상태만 확정한다.
                if (current.Min.CompareTo(current.Max) > 0)
                {
                    return;
                }

                ValueSerializationTracker.FinalizeSerializedChange(target, ownerPath, current);
                if (!baseEditor.IsEditing)
                {
                    setBaseField.SetValueWithoutNotify(current.Base);
                }

                if (!isEditingRange)
                {
                    setMinField.SetValueWithoutNotify(current.Min);
                    setMaxField.SetValueWithoutNotify(current.Max);
                }
            }

            // Min/Max에 별도 트래킹 콜백이 오더라도 공통 상태는 한 번만 소비한다.
            root.TrackPropertyValue(baseProp, _ => ReconcileCurrent());
            root.TrackPropertyValue(minProp, _ => ReconcileCurrent());
            root.TrackPropertyValue(maxProp, _ => ReconcileCurrent());

            root.Bind(so);

            // 50ms 폴링 — Play Mode에서 외부 수정을 실시간 반영
            void Tick()
            {
                var current = ResolveInstance();
                if (current == null) return;

                if (!baseEditor.IsEditing)
                {
                    setBaseField.SetValueWithoutNotify(current.Base);
                }

                if (!isEditingRange)
                {
                    setMinField.SetValueWithoutNotify(current.Min);
                    setMaxField.SetValueWithoutNotify(current.Max);
                }
            }

            ValueDrawerHelper.StartPolling(root, Tick);

            return root;
        }

    #endregion

    }
}
