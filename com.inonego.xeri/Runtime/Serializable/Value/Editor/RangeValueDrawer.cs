/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : RangeValueDrawer.cs
수정일 : 2026-05-03

# 설명
RangeValue<T> 전용 UI Toolkit PropertyDrawer.
Inspector에서 2행 레이아웃을 렌더링한다.
  Row 1: [Field Name | Base | 현재값 필드 | ☐]
  Row 2: [spacer    | Range | Min 필드 | Max 필드 | ☐]
각 행의 체크박스를 독립적으로 체크/해제하여 편집·적용을 수행한다.
int·float 타입만 지원하며 그 외는 기본 PropertyField로 폴백한다.

# 특이사항
Undo 복원 시 backing field는 C++ 직렬화로 이미 복원된다.
lastKnownBase / lastKnownRange 캐시로 복원 여부를 감지해 InvokeOn* 메서드로 이벤트를 강제 발화한다.
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

            var rangeBaseProp = property.FindPropertyRelative("range").FindPropertyRelative("base");
            var minProp       = rangeBaseProp.FindPropertyRelative("min");
            var maxProp       = rangeBaseProp.FindPropertyRelative("max");

            var instance = SerializedPropertyHelper.GetTargetObject(property);

            if (instance is RangeValue<int> intValue)
                return CreateGUI(property, so, target, baseProp, minProp, maxProp, intValue,
                                 new IntegerField(),
                                 CreateRangeSubField<int, IntegerField>("Min", intValue.Min),
                                 CreateRangeSubField<int, IntegerField>("Max", intValue.Max),
                                 p => p.intValue);
            if (instance is RangeValue<float> floatValue)
                return CreateGUI(property, so, target, baseProp, minProp, maxProp, floatValue,
                                 new FloatField(),
                                 CreateRangeSubField<float, FloatField>("Min", floatValue.Min),
                                 CreateRangeSubField<float, FloatField>("Max", floatValue.Max),
                                 p => p.floatValue);

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

        // -----------------------------------------------------------------------
        /// <summary>
        /// <br/> int·float 공통 2행 레이아웃을 반환한다.
        /// <br/> 타입별 차이(필드 종류, readProp)만 인자로 주입받는다.
        /// </summary>
        // -----------------------------------------------------------------------
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
            BaseField<T> setMaxField,
            Func<SerializedProperty, T> readProp
        ) where T : struct, IComparable<T>
        {
            var root           = new VisualElement();
            var lastKnownBase  = instance.Base;
            var lastKnownRange = new Range<T>(instance.Min, instance.Max);
            var isEditingRange = false;

            ValueDrawerHelper.ApplyStylesheet(root);

            // Row 1: [fieldLabel] [Base] [setBaseField] [editToggleBase]
            var row1           = ValueDrawerHelper.CreateRow();
            var fieldLabel     = ValueDrawerHelper.CreateFieldLabel(property.displayName);
            var editToggleBase = ValueDrawerHelper.CreateEditToggle();

            setBaseField.label = "Base";
            setBaseField.style.flexGrow = 1;
            setBaseField.AddToClassList("xeri-set-field");
            setBaseField.SetValueWithoutNotify(instance.Base);

            row1.Add(fieldLabel);
            row1.Add(setBaseField);
            row1.Add(editToggleBase);
            root.Add(row1);

            // Row 2: [spacer] [Range] [setMinField] [setMaxField] [editToggleRange]
            var row2            = ValueDrawerHelper.CreateRow();
            var editToggleRange = ValueDrawerHelper.CreateEditToggle();

            setMinField.style.marginRight = 4;

            row2.Add(ValueDrawerHelper.CreateSpacer(fieldLabel));
            row2.Add(ValueDrawerHelper.CreateRowLabel("Range"));
            row2.Add(setMinField);
            row2.Add(setMaxField);
            row2.Add(editToggleRange);
            root.Add(row2);

            // Base 편집 모드 — FieldEditor<T>로 이벤트 등록 일괄 처리
            T GetBase() => instance.Base;

            void OnBaseApply()
            {
                Undo.RecordObject(target, "Set Base");
                instance.Set(setBaseField.value, invokeEvent: true);
                lastKnownBase = instance.Base;
                EditorUtility.SetDirty(target);
                so.Update();
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
                setMinField.SetValueWithoutNotify(instance.Min);
                setMaxField.SetValueWithoutNotify(instance.Max);
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
                instance.Range.Set(newRange, invokeEvent: true);
                lastKnownRange = newRange;
                EditorUtility.SetDirty(target);
                so.Update();

                isEditingRange = false;
                editToggleRange.SetValueWithoutNotify(false);
                ValueDrawerHelper.SetFieldBorder(setMinField, false);
                ValueDrawerHelper.SetFieldBorder(setMaxField, false);
            }

            void OnRangeToggleChanged(ChangeEvent<bool> evt)
            {
                if (evt.newValue) EnterRangeEditMode();
                else              ExitRangeEditMode();
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

            // Undo 감지 - base
            void OnBasePropChanged(SerializedProperty _)
            {
                var restored = readProp(baseProp);

                if (!EqualityComparer<T>.Default.Equals(lastKnownBase, restored))
                {
                    instance.InvokeOnBaseChange(lastKnownBase);
                    lastKnownBase = restored;
                }

                if (!baseEditor.IsEditing)
                    setBaseField.SetValueWithoutNotify(restored);
            }

            root.TrackPropertyValue(baseProp, OnBasePropChanged);

            // Undo 감지 - range
            // min·max가 별개 prop이므로 두 콜백이 연달아 발화될 수 있음
            // Equals 체크로 중복 발화를 방지하고 min > max 복원은 무시
            void OnRangePropChanged(SerializedProperty _)
            {
                var restoredMin = readProp(minProp);
                var restoredMax = readProp(maxProp);
                if (Comparer<T>.Default.Compare(restoredMin, restoredMax) > 0) return;

                var restoredRange = new Range<T>(restoredMin, restoredMax);

                if (!lastKnownRange.Equals(restoredRange))
                {
                    instance.InvokeOnRangeChange(lastKnownRange);
                    lastKnownRange = restoredRange;
                }

                if (!isEditingRange)
                {
                    setMinField.SetValueWithoutNotify(restoredMin);
                    setMaxField.SetValueWithoutNotify(restoredMax);
                }
            }

            root.TrackPropertyValue(minProp, OnRangePropChanged);
            root.TrackPropertyValue(maxProp, OnRangePropChanged);

            root.Bind(so);

            // 50ms 폴링 — Play Mode에서 외부 수정을 실시간 반영
            void Tick()
            {
                if (!baseEditor.IsEditing)
                    setBaseField.SetValueWithoutNotify(instance.Base);

                if (!isEditingRange)
                {
                    setMinField.SetValueWithoutNotify(instance.Min);
                    setMaxField.SetValueWithoutNotify(instance.Max);
                }
            }

            ValueDrawerHelper.StartPolling(root, Tick);

            return root;
        }

    #endregion

    }
}
