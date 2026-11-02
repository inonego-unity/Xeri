/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : MValueDrawer.cs
수정일 : 2026-10-30

# 설명
MValue<T> 전용 UI Toolkit PropertyDrawer.
Base → Modified 1행 편집과 modifiers Collection Editor section을 함께 제공한다.
체크박스 체크 또는 필드 클릭/드래그 → Base 편집 모드(초록 테두리).
체크박스 해제 → Base 적용 후 표시 모드 복귀.
int·float 타입만 지원하며 그 외는 기본 PropertyField로 폴백한다.

# 특이사항
직렬화된 Modifier 변경 확정은 Collection Editor Model, Base 복원은 MValue의 동기화 계약이 담당한다.
Drawer는 현재 owner의 Bind/Unbind와 표시 동기화만 관리한다.
modifiedField는 항상 disabled 상태이며 xeri-set-field 클래스로 회색 처리를 방지한다.
편집 모드(FieldEditor.IsEditing) 중에는 setField 외부 갱신을 억제하고 modifiedField는 즉시 갱신한다.
========================================================================= BLOCK_HEADER_END */

using System;

using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace inonego.Xeri
{
    using Serializable;
    using Serializable.Editor;

    // ============================================================
    /// <summary>
    /// MValue&lt;T&gt; 전용 PropertyDrawer.
    /// </summary>
    // ============================================================
    [CustomPropertyDrawer(typeof(MValue<>))]
    public class MValueDrawer : PropertyDrawer
    {

    #region 상수

        private const string modifiersFieldName = "modifiers";

    #endregion

    #region 메서드

        // ------------------------------------------------------------
        /// <summary>
        /// 타입별 분기 후 전용 GUI를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            if (property.serializedObject.isEditingMultipleObjects)
            {
                return new HelpBox("MValue 여러 오브젝트 동시 편집은 지원하지 않습니다.", HelpBoxMessageType.Info);
            }

            var so       = property.serializedObject;
            var target   = so.targetObject;
            var instance = SerializedPropertyHelper.GetTargetObject(property);

            if (instance is MValue<int> intValue)
            {
                return CreateGUI(property, so, target, intValue, new IntegerField(), new IntegerField());
            }

            if (instance is MValue<float> floatValue)
            {
                return CreateGUI(property, so, target, floatValue, new FloatField(), new FloatField());
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
            SerializedProperty property, SerializedObject so, UnityEngine.Object target,
            MValue<T> instance, BaseField<T> setField, BaseField<T> modifiedField
        )
        where T : struct
        {
            var root = ValueDrawerHelper.CreateRoot();
            var ownerPropertyPath = property.propertyPath;
            MValue<T> boundInstance = null;

            ValueSerializationTracker.Observe(target, ownerPropertyPath, instance);

            // UXML 기본 행에 MValue의 [Base] [→] [Modified] 필드를 연결한다.
            var row        = root.Q<VisualElement>("value-base-row");
            var fieldLabel = root.Q<Label>("value-field-label");
            var editToggle = root.Q<Toggle>("value-base-toggle");

            var arrowLabel = new Label("→");
            arrowLabel.AddToClassList("xeri-arrow");

            fieldLabel.text = property.displayName;
            setField.label = "Base";
            setField.AddToClassList("xeri-set-field");
            setField.AddToClassList("xeri-mvalue-base-field");
            setField.SetValueWithoutNotify(instance.Base);

            // modifiedField는 항상 읽기 전용; xeri-set-field로 disabled 시 회색 처리 방지
            modifiedField.AddToClassList("xeri-set-field");
            modifiedField.AddToClassList("xeri-modified-field");
            modifiedField.SetValueWithoutNotify(instance.Modified);
            modifiedField.SetEnabled(false);

            row.Insert(1, setField);
            row.Insert(2, arrowLabel);
            row.Insert(3, modifiedField);

            var modifiersProperty = property.FindPropertyRelative(modifiersFieldName);
            if (modifiersProperty != null)
            {
                root.Add(new CollectionEditorView(new MValueModifierEditorModel<T>(modifiersProperty)));
            }

            // 현재 경로로 owner를 찾는다. Unity가 중첩 인스턴스를 교체할 수 있다.
            MValue<T> ResolveInstance() => ValueDrawerHelper.ResolveInstance<MValue<T>>(so, ownerPropertyPath);

            T GetBase() => ResolveInstance()?.Base ?? default;

            // FieldEditor 생성자에 넘기는 콜백도 이 변수를 참조하므로 먼저 초기화한다.
            ValueDrawerHelper.FieldEditor<T> editor = null;

            void OnApply()
            {
                BindCurrentOwner();
                var current = ResolveInstance();
                if (current == null)
                {
                    return;
                }

                ValueDrawerHelper.ApplyBaseValue(so, target, current, setField.value, "Set MValue");
                RefreshFields();
            }

            editor = new ValueDrawerHelper.FieldEditor<T>(editToggle, setField, GetBase, OnApply);

            void HandleBaseChange(object _, ValueChangeEventArgs<T> e)
            {
                if (editor == null || !editor.IsEditing)
                {
                    setField.SetValueWithoutNotify(e.Current);
                }
            }

            void HandleModifiedChange(object _, ValueChangeEventArgs<T> e)
            {
                modifiedField.SetValueWithoutNotify(e.Current);
            }

            // 이벤트 구독 수명은 panel과 현재 owner의 수명 양쪽을 따른다.
            void UnbindOwner()
            {
                if (boundInstance == null)
                {
                    return;
                }

                boundInstance.OnBaseChange -= HandleBaseChange;
                boundInstance.OnModifiedChange -= HandleModifiedChange;
                boundInstance = null;
            }

            void BindCurrentOwner()
            {
                var current = ResolveInstance();
                if (ReferenceEquals(boundInstance, current))
                {
                    return;
                }

                UnbindOwner();
                if (root.panel == null || current == null)
                {
                    return;
                }

                boundInstance = current;
                boundInstance.OnBaseChange += HandleBaseChange;
                boundInstance.OnModifiedChange += HandleModifiedChange;
            }

            void RefreshFields()
            {
                var current = ResolveInstance();
                if (current == null)
                {
                    return;
                }

                if (editor == null || !editor.IsEditing)
                {
                    setField.SetValueWithoutNotify(current.Base);
                }

                modifiedField.SetValueWithoutNotify(current.Modified);
            }

            void HandleAttach(AttachToPanelEvent _)
            {
                BindCurrentOwner();
                RefreshFields();
            }

            void HandleDetach(DetachFromPanelEvent _)
            {
                UnbindOwner();
            }

            root.RegisterCallback<AttachToPanelEvent>(HandleAttach);
            root.RegisterCallback<DetachFromPanelEvent>(HandleDetach);

            // Collection의 편집 확정은 Model이 담당한다. 이 콜백은 owner만 다시 바인딩한다.
            root.TrackPropertyValue
            (
                property,
                _ =>
                {
                    BindCurrentOwner();
                    RefreshFields();
                }
            );

            // Base의 직접 직렬화 복원(Undo 포함)만 MValue의 확정 경계에 전달한다.
            var baseProperty = property.FindPropertyRelative("base");
            if (baseProperty != null)
            {
                root.TrackPropertyValue
                (
                    baseProperty,
                    _ =>
                    {
                        BindCurrentOwner();
                        var current = ResolveInstance();
                        if (current != null)
                        {
                            ValueSerializationTracker.FinalizeSerializedChange(target, ownerPropertyPath, current);
                        }

                        RefreshFields();
                    }
                );
            }

            // 하위 Collection cell은 자체 binding을 소유하므로 root.Bind는 생략한다.
            return root;
        }

    #endregion

    }
}
