/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : SerializedListKindProvider.cs
수정일 : 2026-11-19

# 설명
[CollectionEditor]를 붙인 List<T>·1차원 배열의 종류 정의를 만든다.
Columns를 지정하면 요소의 직렬화 필드를 열로 나누고, Unique면 앞쪽 값을 유지하는 중복 규칙을 둔다.

# 특이사항, 제약사항
SerializeReference 요소는 요소마다 실제 형식이 달라 필드 열을 만들 수 없다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

using UnityEditor;
using UnityEngine;

using inonego;
using inonego.Xeri;
using inonego.Xeri.UI;

namespace inonego.Xeri.Serializable.Editor
{
    // ============================================================
    /// <summary>
    /// [CollectionEditor] 목록 종류 Provider.
    /// </summary>
    // ============================================================
    internal sealed class SerializedListKindProvider : ICollectionKindProvider
    {

    #region 필드

        private const string valueColumnID = "Value";

    #endregion

    #region 종류 생성

        // ------------------------------------------------------------
        /// <summary>
        /// [CollectionEditor] 목록 필드면 종류 정의를 만든다.
        /// </summary>
        // ------------------------------------------------------------
        public bool TryCreate(SerializedProperty property, FieldInfo field, out CollectionKind kind)
        {
            kind = null;
            var attribute = field?.GetCustomAttribute<CollectionEditorAttribute>(inherit: true);
            if (attribute == null || !property.isArray || property.propertyType == SerializedPropertyType.String)
            {
                return false;
            }

            var isReference = field.IsDefined(typeof(SerializeReference), inherit: true);
            kind = Create(property, attribute.Unique, GetElementType(field.FieldType), isReference, attribute.Columns);
            return true;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 요소 형식과 열 설정으로 목록 종류 정의를 만든다.
        /// </summary>
        // ------------------------------------------------------------
        public static CollectionKind Create
        (
            SerializedProperty property,
            bool unique,
            Type elementType,
            bool isReference,
            IReadOnlyList<string> fieldColumns = null
        )
        {
            if (!property.isArray || property.propertyType == SerializedPropertyType.String)
            {
                throw new ArgumentException("CollectionEditor에는 List<T> 또는 1차원 배열이 필요합니다.", nameof(property));
            }

            var columns = fieldColumns != null && fieldColumns.Count > 0
                ? CreateFieldColumns(elementType, isReference, fieldColumns)
                : new[]
            {
                new CollectionColumn
                (
                    valueColumnID, "값", TableColumnLayout.Flexible(240f, 100f, 0f, 1f), null,
                    elementType, isReference, createsValueOnAdd: isReference
                ),
            };

            // Unique는 행 값 자체를 키로 보고 앞쪽 값을 유지한다.
            var keyRule = unique
                ? new CollectionKeyRule(null, CollectionKinds.CreateComparer(elementType), CollectionDuplicateWinner.First, false, "값")
                : null;
            const CollectionEdits edits = CollectionEdits.Add | CollectionEdits.Remove | CollectionEdits.Duplicate
                | CollectionEdits.Move | CollectionEdits.Paste;
            return new CollectionKind("컬렉션", string.Empty, columns, null, edits, CollectionSync.None, keyRule: keyRule);
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

            return collectionType.IsGenericType && collectionType.GetGenericTypeDefinition() == typeof(List<>)
                ? collectionType.GetGenericArguments()[0]
                : null;
        }

    #endregion

    #region 필드 열

        // ------------------------------------------------------------
        /// <summary>
        /// 지정한 직렬화 필드마다 표시 이름을 가진 열을 만든다.
        /// </summary>
        // ------------------------------------------------------------
        private static CollectionColumn[] CreateFieldColumns
        (
            Type elementType, bool isReference, IReadOnlyList<string> fieldNames
        )
        {
            if (elementType == null || isReference)
            {
                throw new InvalidOperationException("Columns는 SerializeReference가 아닌 class/struct 요소에만 사용할 수 있습니다.");
            }

            var result = new CollectionColumn[fieldNames.Count];
            for (var index = 0; index < fieldNames.Count; index++)
            {
                var name = fieldNames[index];
                var field = SerializedPropertyHelper.FindField(elementType, name);
                if (field == null || !SerializedPropertyHelper.IsSerializedField(field))
                {
                    throw new InvalidOperationException($"{elementType.Name}에 직렬화 필드 '{name}'이(가) 없습니다.");
                }

                var isFieldReference = field.IsDefined(typeof(SerializeReference), inherit: false);
                result[index] = new CollectionColumn
                (
                    name, GetFieldHeader(field), GetFieldLayout(field.FieldType, index == 0),
                    name, field.FieldType, isFieldReference, createsValueOnAdd: isFieldReference
                );
            }

            return result;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 필드 형식에 맞는 열 폭과 남는 폭 비율을 정한다.
        /// </summary>
        // ------------------------------------------------------------
        private static TableColumnLayout GetFieldLayout(Type fieldType, bool isFirst)
        {
            // 첫 열은 행의 식별 이름이므로 남는 폭을 가장 많이 차지한다.
            if (isFirst)
            {
                return TableColumnLayout.Flexible(160f, 100f, 0f, 1.2f);
            }

            // 드롭다운은 선택지 이름이 잘리지 않도록 폭을 고정한다.
            if (fieldType.IsEnum)
            {
                return TableColumnLayout.Fixed(110f);
            }

            // 체크박스는 좁게, 문자열은 넓게, 숫자는 그 사이로 둔다.
            if (fieldType == typeof(bool))
            {
                return TableColumnLayout.Flexible(72f, 60f, 0f, 0.8f);
            }

            var isNumber = fieldType.IsPrimitive || fieldType == typeof(decimal);
            return isNumber
                ? TableColumnLayout.Flexible(90f, 64f, 0f, 1f)
                : TableColumnLayout.Flexible(140f, 80f, 0f, 1.5f);
        }

        private static string GetFieldHeader(FieldInfo field)
        {
            var inspectorName = field.GetCustomAttribute<InspectorNameAttribute>();
            return inspectorName != null && !string.IsNullOrWhiteSpace(inspectorName.displayName)
                ? inspectorName.displayName
                : ObjectNames.NicifyVariableName(field.Name);
        }

    #endregion

    }
}
