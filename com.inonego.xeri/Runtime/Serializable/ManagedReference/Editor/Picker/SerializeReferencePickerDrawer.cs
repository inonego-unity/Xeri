/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : SerializeReferencePickerDrawer.cs
수정일 : 2026-08-05

# 설명
SerializeReferencePickerAttribute 필드를 UI Toolkit으로 표시한다.
concrete/generic type 생성과 Value/Link clipboard 명령을 제공한다.

# 특이사항
PropertyDrawer는 Unity가 공유하므로 대상 property와 실제 instance를 인스턴스 필드에 보관하지 않는다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Linq;

using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

using inonego.Xeri;
using inonego.Xeri.Editor.Picker;
using inonego.Xeri.UI;
using inonego.Xeri.UI.Picker;

namespace inonego.Xeri.Serializable.Editor
{
    // ============================================================
    /// <summary>
    /// opt-in SerializeReference picker UI Toolkit PropertyDrawer.
    /// </summary>
    // ============================================================
    [CustomPropertyDrawer(typeof(SerializeReferencePickerAttribute))]
    public sealed class SerializeReferencePickerDrawer : PropertyDrawer
    {
    #region 상수

        private const string UXML_FILE_NAME = "SerializeReferencePickerDrawer.uxml";
        private const string USS_FILE_NAME = "SerializeReferencePickerDrawer.uss";
        private const string UI_DIRECTORY_NAME = "UI";

    #endregion

    #region 메서드

        // ------------------------------------------------------------
        /// <summary>
        /// Unity가 전달한 managed-reference 요소의 picker UI를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            if (property.propertyType != SerializedPropertyType.ManagedReference)
            {
                return new PropertyField(property);
            }

            var declaredType = ManagedReferenceTypeCatalog.GetDeclaredReferenceType(property);
            if (declaredType == null)
            {
                return new PropertyField(property);
            }

            return CreateReferenceGUI(property, declaredType, property.displayName);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// managed-reference 하나의 type header, action menu, child property를 만든다.
        /// </summary>
        // ------------------------------------------------------------
        private static VisualElement CreateReferenceGUI
        (
            SerializedProperty property,
            Type declaredType,
            string label
        )
        {
            var root = LoadRoot();
            if (root == null)
            {
                return new PropertyField(property);
            }

            var header       = root.Q<ManagedReferenceField>("reference-header");
            var typeButton   = root.Q<Button>("type-button");
            var actionButton = root.Q<Button>("action-button");
            var content      = root.Q<VisualElement>("content");

            if (header == null
                || typeButton == null
                || actionButton == null
                || content == null)
            {
                return new PropertyField(property);
            }

            header.label = label;

            void Refresh()
            {
                var currentValue = property.managedReferenceValue;
                var displayName = property.hasMultipleDifferentValues
                    ? "여러 값"
                    : ManagedReferenceTypeCatalog.GetDisplayName(currentValue?.GetType());
                typeButton.text    = displayName;
                typeButton.tooltip = displayName;

                content.Clear();
                content.style.display = DisplayStyle.Flex;
                if (currentValue == null)
                {
                    content.style.display = DisplayStyle.None;
                    return;
                }

                SerializedPropertyHelper.AppendVisibleChildren(property, content);
            }

            void ApplyNewReference(Type type)
            {
                if (!ManagedReferenceTypeCatalog.CanCreate(type))
                {
                    EditorUtility.DisplayDialog("SerializeReference 선택", "선택한 타입은 기본 생성자로 만들 수 없습니다.", "확인");
                    return;
                }

                foreach (var target in property.serializedObject.targetObjects)
                {
                    using var targetObject = new SerializedObject(target);
                    var targetProperty = targetObject.FindProperty(property.propertyPath);
                    if (targetProperty == null)
                    {
                        continue;
                    }

                    // type 변경은 기존 데이터를 이식하지 않고 새 기본 instance를 대입하는 명시적 생성 동작이다.
                    targetProperty.managedReferenceValue = Activator.CreateInstance(type);
                    targetObject.ApplyModifiedProperties();
                }

                property.serializedObject.Update();
                Refresh();
            }

            void ShowTypePicker()
            {
                const int dropdownPageSize = 5;

                var candidates = ManagedReferenceTypeCatalog.GetCreationCandidates(declaredType);
                if (candidates.Count == 0)
                {
                    EditorUtility.DisplayDialog("SerializeReference 선택", "생성할 수 있는 managed reference 타입 후보가 없습니다.", "확인");
                    return;
                }

                var currentType = property.managedReferenceValue?.GetType();
                var currentValue = candidates.FirstOrDefault(candidate => candidate.Type == currentType);
                var rect = GetScreenRect(typeButton);
                var spec = PickerSpec<ManagedReferenceCreationCandidate, ManagedReferenceCreationCandidate>.Create($"{declaredType.Name} 선택")
                    .Value(candidate => candidate)
                    .Preview(false)
                    .Label(candidate => candidate.DisplayName)
                    .Column
                    (
                        "타입",
                        candidate => candidate.DisplayName,
                        PickerColumnOptions.Flexible(width: 260f, minWidth: 160f, stretchWeight: 1f)
                    )
                    .Build();

                Picker.Show
                (
                    spec,
                    candidates,
                    currentValue,
                    selectedCandidate =>
                    {
                        if (selectedCandidate.RequiresGenericTypeCreation)
                        {
                            GenericTypePicker.Show
                            (
                                selectedCandidate.Type,
                                ManagedReferenceTypeCatalog.GetGenericArgumentCandidates,
                                ManagedReferenceTypeCatalog.GetDisplayName,
                                ApplyNewReference,
                                selectedCandidate.FixedArguments,
                                type => GetGenericTypeValidationMessage(type, declaredType)
                            );
                            return;
                        }

                        ApplyNewReference(selectedCandidate.Type);
                    },
                    rect,
                    pageSize: dropdownPageSize
                );
            }

            string GetGenericTypeValidationMessage(Type type, Type targetType)
            {
                if (!targetType.IsAssignableFrom(type))
                {
                    return "구성한 타입을 이 필드에 할당할 수 없습니다.";
                }

                return ManagedReferenceTypeCatalog.CanCreate(type)
                    ? string.Empty
                    : "구성한 타입은 public 기본 생성자로 만들 수 없습니다.";
            }

            void ShowActions()
            {
                var menu = new GenericMenu();

                if (!property.hasMultipleDifferentValues && property.managedReferenceValue != null)
                {
                    menu.AddDisabledItem(new GUIContent($"#{property.managedReferenceId}"));
                    menu.AddSeparator(string.Empty);
                }

                if (property.hasMultipleDifferentValues || property.managedReferenceValue != null)
                {
                    menu.AddItem(new GUIContent("Make Null"), false, () =>
                    {
                        foreach (var target in property.serializedObject.targetObjects)
                        {
                            var targetObject   = new SerializedObject(target);
                            var targetProperty = targetObject.FindProperty(property.propertyPath);
                            if (targetProperty == null)
                            {
                                continue;
                            }

                            // multi-object 편집에서는 각 root의 현재 reference를 명시적으로 null로 전환한다.
                            targetProperty.managedReferenceValue = null;
                            targetObject.ApplyModifiedProperties();
                        }

                        property.serializedObject.Update();
                        Refresh();
                    });
                }
                else
                {
                    menu.AddDisabledItem(new GUIContent("Make Null"));
                }

                menu.AddSeparator(string.Empty);

                // Value 복사는 null도 유효한 독립 값이므로 명령을 항상 활성화한다.
                menu.AddItem(new GUIContent("Copy as Value"), false, () =>
                {
                    ManagedReferenceClipboard.TryCopyAsValue(property, out _);
                });

                if (ManagedReferenceClipboard.CanPasteAsValue(property, declaredType, out _))
                {
                    menu.AddItem(new GUIContent("Paste as Value"), false, () =>
                    {
                        ManagedReferenceClipboard.TryPasteAsValue(property, declaredType, out _);
                        Refresh();
                    });
                }
                else
                {
                    menu.AddDisabledItem(new GUIContent("Paste as Value"));
                }

                if (ManagedReferenceClipboard.CanCopyAsLink(property, out _))
                {
                    menu.AddItem(new GUIContent("Copy as Link"), false, () =>
                    {
                        ManagedReferenceClipboard.TryCopyAsLink(property, out _);
                    });
                }
                else
                {
                    menu.AddDisabledItem(new GUIContent("Copy as Link"));
                }

                if (ManagedReferenceClipboard.CanPasteAsLink(property, declaredType, out _))
                {
                    menu.AddItem(new GUIContent("Paste as Link"), false, () =>
                    {
                        ManagedReferenceClipboard.TryPasteAsLink(property, declaredType, out _);
                        Refresh();
                    });
                }
                else
                {
                    menu.AddDisabledItem(new GUIContent("Paste as Link"));
                }

                menu.ShowAsContext();
            }

            typeButton.clicked   += ShowTypePicker;
            actionButton.clicked += ShowActions;
            root.TrackPropertyValue(property, _ => Refresh());
            Refresh();

            return root;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// UI Toolkit element의 world bound를 EditorWindow dropdown용 screen rect로 변환한다.
        /// </summary>
        // ------------------------------------------------------------
        private static Rect GetScreenRect(VisualElement element)
        {
            var worldBound = element.worldBound;

            // ShowAsDropDown은 screen 좌표를 요구하므로 현재 GUI view 기준 좌표를 변환한다.
            var screenPosition = GUIUtility.GUIToScreenPoint(worldBound.position);
            return new Rect(screenPosition, worldBound.size);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// drawer template에 공통 USS를 적용한 root를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        private static VisualElement LoadRoot()
        {
            var dir         = EditorAssetHelper.GetScriptDirectory(typeof(SerializeReferencePickerDrawer));
            var uiDirectory = $"{dir}/{UI_DIRECTORY_NAME}";
            var template    = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>($"{uiDirectory}/{UXML_FILE_NAME}");
            var style       = AssetDatabase.LoadAssetAtPath<StyleSheet>($"{uiDirectory}/{USS_FILE_NAME}");

            if (template == null)
            {
                return null;
            }

            var root = template.CloneTree();
            if (style != null)
            {
                root.styleSheets.Add(style);
            }

            return root;
        }

    #endregion

    }
}
