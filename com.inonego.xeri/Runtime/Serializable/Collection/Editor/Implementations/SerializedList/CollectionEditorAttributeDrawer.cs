/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : CollectionEditorAttributeDrawer.cs
수정일 : 2026-10-31

# 설명
CollectionEditorAttribute가 붙은 List<T>와 1차원 배열을 전용 View로 표시한다.
요소 타입, SerializeReference 및 ReadOnly 여부를 함께 전달한다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace inonego.Xeri.Serializable.Editor
{
    // ============================================================
    /// <summary>
    /// CollectionEditorAttribute UI Toolkit PropertyDrawer.
    /// </summary>
    // ============================================================
    [CustomPropertyDrawer(typeof(CollectionEditorAttribute))]
    public sealed class CollectionEditorAttributeDrawer : PropertyDrawer
    {

    #region 메서드

        /// <inheritdoc />
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            return CreateGUI(property, fieldInfo);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 선택된 Attribute Drawer에 관계없이 같은 컬렉션 View를 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        internal static VisualElement CreateGUI(SerializedProperty property, FieldInfo field)
        {
            if (!property.isArray
                || property.propertyType == SerializedPropertyType.String)
            {
                return new HelpBox
                (
                    "List<T> 또는 1차원 배열에만 사용할 수 있습니다.",
                    HelpBoxMessageType.Warning
                );
            }

            var options = field?.GetCustomAttribute<CollectionEditorAttribute>(inherit: true);
            var fieldType = field?.FieldType;
            var elementType = GetElementType(fieldType);
            var isManagedReference = field?.IsDefined
            (
                typeof(SerializeReference),
                inherit: true
            ) == true;

            return CollectionEditorDrawerGUI.Create
            (
                property,
                "CollectionEditor",
                () => new SerializedListEditorModel
                (
                    property,
                    options?.Unique ?? false,
                    elementType,
                    isManagedReference
                ),
                field?.IsDefined(typeof(ReadOnlyAttribute), inherit: true) == true
            );
        }

        private static Type GetElementType(Type collectionType)
        {
            if (collectionType == null)
            {
                return null;
            }

            if (collectionType.IsArray)
            {
                return collectionType.GetElementType();
            }

            if (collectionType.IsGenericType
                && collectionType.GetGenericTypeDefinition() == typeof(List<>))
            {
                return collectionType.GetGenericArguments()[0];
            }

            return null;
        }

    #endregion

    }
}
