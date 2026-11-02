/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : SerializeReferencePickerDrawer.cs
수정일 : 2026-09-25

# 설명
SerializeReferencePickerAttribute 필드를 UI Toolkit으로 표시한다.
타입 선택과 clipboard 동작은 ManagedReferenceEditorAction에 위임하고 이 Drawer는 full field layout만 소유한다.

# 특이사항
PropertyDrawer는 Unity가 공유하므로 대상 property와 실제 instance를 인스턴스 필드에 보관하지 않는다.
========================================================================= BLOCK_HEADER_END */

using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

using inonego;
using inonego.Xeri;

namespace inonego.Xeri.Serializable.Editor
{
    // ======================================================================
    /// <summary>
    /// opt-in SerializeReference picker UI Toolkit PropertyDrawer.
    /// </summary>
    // ======================================================================
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

        // --------------------------------------------------------------------------------
        /// <summary>
        /// managed-reference type header, action menu와 child property를 만든다.
        /// </summary>
        // --------------------------------------------------------------------------------
        private static VisualElement CreateReferenceGUI
        (
            SerializedProperty property,
            System.Type declaredType,
            string label
        )
        {
            var root = LoadRoot();
            if (root == null)
            {
                return new PropertyField(property);
            }

            var header = root.Q<ManagedReferenceField>("reference-header");
            var typeButton = root.Q<Button>("type-button");
            var actionButton = root.Q<Button>("action-button");
            var content = root.Q<VisualElement>("content");

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
                var displayName = ManagedReferenceEditorAction.GetDisplayName(property);
                typeButton.text = displayName;
                typeButton.tooltip = displayName;

                content.Clear();
                content.style.display = DisplayStyle.Flex;

                if (property.managedReferenceValue == null)
                {
                    content.style.display = DisplayStyle.None;
                    return;
                }

                SerializedPropertyHelper.AppendVisibleChildren(property, content);
                content.Bind(property.serializedObject);
            }

            typeButton.clicked += () => ManagedReferenceEditorAction.ShowTypePicker
            (
                property,
                declaredType,
                typeButton,
                Refresh
            );
            actionButton.clicked += () => ManagedReferenceEditorAction.ShowActions
            (
                property,
                declaredType,
                Refresh,
                menuOwner: actionButton
            );

            root.TrackPropertyValue(property, _ => Refresh());
            Refresh();

            return root;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// drawer template에 공통 USS를 적용한 root를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        private static VisualElement LoadRoot()
        {
            var dir = EditorAssetHelper.GetScriptDirectory
            (
                typeof(SerializeReferencePickerDrawer)
            );
            var uiDirectory = $"{dir}/{UI_DIRECTORY_NAME}";
            var template = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>
            (
                $"{uiDirectory}/{UXML_FILE_NAME}"
            );
            var style = AssetDatabase.LoadAssetAtPath<StyleSheet>
            (
                $"{uiDirectory}/{USS_FILE_NAME}"
            );

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
