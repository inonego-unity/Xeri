/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : SerializedListEditorModel.cs
수정일 : 2026-11-17

# 설명
CollectionEditorAttribute가 붙은 List<T>와 1차원 배열을 Collection Editor로 노출한다.
기본은 요소 1열이며 Columns를 지정하면 요소의 직렬화 필드를 열마다 직접 편집한다.
선택적 Unique validation, managed-reference Add metadata와 serialized 순서 기반 mutation을 담당한다.

# 특이사항, 제약사항
열 헤더는 필드의 InspectorName, 없으면 Unity 표시 이름 규칙을 사용한다.
열은 Unity가 직렬화하는 필드(public 또는 SerializeField/SerializeReference, 비static·비readonly·비NonSerialized)만 허용한다.
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
    /// 일반 List/Array Collection Editor Model.
    /// </summary>
    // ============================================================
    internal sealed class SerializedListEditorModel : CollectionEditorModelBase
    {

    #region 상수

        private const string valueColumnID = "Value";

    #endregion

    #region 상태

        // ------------------------------------------------------------
        /// <summary>
        /// 표시할 열 목록을 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public override IReadOnlyList<CollectionColumnSpec> Columns => columns;
        private readonly IReadOnlyList<CollectionColumnSpec> columns;

        // ------------------------------------------------------------
        /// <summary>
        /// 목록의 항목 수를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public override int Count => GetListProperty().arraySize;

        // ------------------------------------------------------------
        /// <summary>
        /// 항목 순서 변경을 지원하는지 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public override bool CanReorder => true;

        /// <inheritdoc />
        protected override bool PreservesRowIndicesOnCommit => true;

        // ------------------------------------------------------------
        /// <summary>
        /// 중복 항목을 허용하지 않는 목록인지 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        internal bool IsUnique => unique;
        private readonly bool unique;

        // ------------------------------------------------------------
        /// <summary>
        /// 요소의 직렬화 필드를 열로 나눠 표시하는지 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        internal bool UsesFieldColumns => fieldColumns.Count > 0;
        private readonly IReadOnlyList<string> fieldColumns;

        private readonly Type elementType;
        private readonly bool isManagedReference;
        private readonly Func<object, object, bool> equals;
        private readonly IEqualityComparer<object> itemComparer;
        private bool? hasUnlistedFields;

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// 일반 serialized List/Array property의 Model을 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        public SerializedListEditorModel
        (
            SerializedProperty property,
            bool unique,
            Type elementType,
            bool isManagedReference,
            IReadOnlyList<string> fieldColumns = null
        )
            : base(property)
        {
            if (!property.isArray
                || property.propertyType == SerializedPropertyType.String)
            {
                throw new ArgumentException
                (
                    "CollectionEditor에는 List<T> 또는 1차원 배열이 필요합니다.",
                    nameof(property)
                );
            }

            this.unique = unique;
            this.elementType = elementType;
            itemComparer = CreateBoxedComparer(elementType ?? typeof(object));
            equals = itemComparer.Equals;
            this.isManagedReference = isManagedReference;
            this.fieldColumns = fieldColumns ?? Array.Empty<string>();
            columns = this.fieldColumns.Count > 0
                ? CreateFieldColumns(elementType, isManagedReference, this.fieldColumns)
                : new[]
            {
                new CollectionColumnSpec
                (
                    valueColumnID,
                    "값",
                    TableColumnLayout.Flexible(240f, 100f, 0f, 1f)
                ),
            };
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 지정한 직렬화 필드마다 표시 이름을 가진 열을 만든다.
        /// </summary>
        // ------------------------------------------------------------
        private static IReadOnlyList<CollectionColumnSpec> CreateFieldColumns
        (
            Type elementType,
            bool isManagedReference,
            IReadOnlyList<string> fieldNames
        )
        {
            // 관리 참조는 요소마다 실제 형식이 달라 고정된 하위 필드 열을 만들 수 없다.
            if (elementType == null || isManagedReference)
            {
                throw new InvalidOperationException
                (
                    "Columns는 SerializeReference가 아닌 class/struct 요소에만 사용할 수 있습니다."
                );
            }

            var result = new List<CollectionColumnSpec>(fieldNames.Count);
            for (var index = 0; index < fieldNames.Count; index++)
            {
                var name = fieldNames[index];
                var field = SerializedPropertyHelper.FindField(elementType, name);
                if (field == null || !IsSerializedField(field))
                {
                    throw new InvalidOperationException
                    (
                        $"{elementType.Name}에 직렬화 필드 '{name}'이(가) 없습니다."
                    );
                }

                // 첫 열은 행의 식별 이름이므로 남는 폭을 더 많이 차지한다.
                result.Add
                (
                    new CollectionColumnSpec
                    (
                        name,
                        GetFieldHeader(field),
                        TableColumnLayout.Flexible(index == 0 ? 180f : 120f, 64f, 0f, index == 0 ? 2f : 1f)
                    )
                );
            }

            return result;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Unity가 직렬화하는 인스턴스 필드인지 판정한다.
        /// </summary>
        // ------------------------------------------------------------
        private static bool IsSerializedField(FieldInfo field)
        {
            if (field.IsStatic || field.IsInitOnly || field.IsLiteral || field.IsNotSerialized)
            {
                return false;
            }

            return field.IsPublic
                || field.IsDefined(typeof(SerializeField), inherit: false)
                || field.IsDefined(typeof(SerializeReference), inherit: false);
        }

        private static string GetFieldHeader(FieldInfo field)
        {
            var inspectorName = field.GetCustomAttribute<InspectorNameAttribute>();
            return inspectorName != null && !string.IsNullOrWhiteSpace(inspectorName.displayName)
                ? inspectorName.displayName
                : ObjectNames.NicifyVariableName(field.Name);
        }

    #endregion

    #region 조회

        // ------------------------------------------------------------
        /// <summary>
        /// 같은 설정으로 다른 직렬화 property용 Model을 만든다.
        /// </summary>
        // ------------------------------------------------------------
        public override ICollectionEditorModel CreateFor(SerializedProperty property)
        {
            return new SerializedListEditorModel
            (
                property, unique, elementType, isManagedReference, fieldColumns
            );
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 원본 인덱스의 행 property를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public override SerializedProperty GetRowProperty(int sourceIndex)
        {
            return GetListProperty().GetArrayElementAtIndex(sourceIndex);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 행과 열에 해당하는 셀 property를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public override SerializedProperty GetCellProperty
        (
            int sourceIndex,
            string columnID
        )
        {
            var row = GetRowProperty(sourceIndex);
            return UsesFieldColumns && columnID != valueColumnID
                ? row.FindPropertyRelative(columnID)
                : row;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 열로 표시하지 않은 직렬화 필드가 요소에 남아 있는지 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        internal bool HasUnlistedFields(SerializedProperty row)
        {
            if (!UsesFieldColumns || row == null)
            {
                return false;
            }

            // 요소 형식이 고정돼 있으므로 첫 확인 결과를 모든 행에 재사용한다.
            if (!hasUnlistedFields.HasValue)
            {
                var listed = new HashSet<string>(fieldColumns, StringComparer.Ordinal);
                var found = false;
                foreach (var child in SerializedPropertyHelper.EnumerateVisibleChildren(row))
                {
                    if (!listed.Contains(child.name))
                    {
                        found = true;
                        break;
                    }
                }

                hasUnlistedFields = found;
            }

            return hasUnlistedFields.Value;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// managed reference 추가에 사용할 열과 선언 형식을 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public override bool TryGetManagedReferenceAddTarget
        (
            out string columnID,
            out Type declaredType
        )
        {
            columnID = valueColumnID;
            declaredType = elementType;
            return isManagedReference && declaredType != null;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// <br/>행의 검증 메시지를 반환한다.
        /// <br/>문제가 없으면 빈 문자열이다.
        /// </summary>
        // ------------------------------------------------------------
        public override string GetRowValidationMessage(int sourceIndex)
        {
            if (!unique)
            {
                return string.Empty;
            }

            var value = GetElementObject(sourceIndex);

            // Unique 정책은 첫 값을 유지하고 뒤쪽 duplicate만 invalid로 표시한다.
            for (var index = 0; index < sourceIndex; index++)
            {
                if (equals(value, GetElementObject(index)))
                {
                    return "이전 행과 값이 중복됩니다.";
                }
            }

            return string.Empty;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 검증 오류가 있는 행의 원본 인덱스 목록을 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public override IReadOnlyList<int> GetInvalidRowIndices()
        {
            var indices = new List<int>();
            if (!unique)
            {
                return indices;
            }

            var seen = new HashSet<object>(itemComparer);
            for (var index = 0; index < Count; index++)
            {
                if (!seen.Add(GetElementObject(index)))
                {
                    indices.Add(index);
                }
            }

            return indices;
        }

        private SerializedProperty GetListProperty()
        {
            var property = GetCollectionProperty();
            if (property == null || !property.isArray)
            {
                throw new InvalidOperationException
                (
                    "직렬화된 목록이나 배열을 찾을 수 없습니다."
                );
            }

            return property;
        }

        private object GetElementObject(int sourceIndex)
        {
            return SerializedPropertyHelper.GetValue(GetRowProperty(sourceIndex));
        }

    #endregion

    #region 행 변경

        // ------------------------------------------------------------
        /// <summary>
        /// 기본 행을 추가한다.
        /// </summary>
        // ------------------------------------------------------------
        public override CollectionRowCommandResult Add(Action<int> initialize = null)
        {
            return AddDefaultRow
            (
                GetListProperty,
                insertIndex: null,
                "컬렉션 항목 추가",
                initialize
            );
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 지정한 행을 복제한다.
        /// </summary>
        // ------------------------------------------------------------
        public override CollectionRowCommandResult DuplicateAt(int sourceIndex)
        {
            return DuplicateRow
            (
                GetListProperty,
                sourceIndex,
                insertIndex: null,
                "컬렉션 항목 복제",
                "컬렉션 항목"
            );
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 지정한 행을 삭제한다.
        /// </summary>
        // ------------------------------------------------------------
        public override void RemoveAt(int sourceIndex)
        {
            RemoveRow
            (
                GetListProperty,
                sourceIndex,
                "컬렉션 항목 삭제"
            );
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 지정한 여러 행을 하나의 Undo 단위로 삭제한다.
        /// </summary>
        // ------------------------------------------------------------
        public override void RemoveMany
        (
            IReadOnlyList<int> sourceIndices,
            string undoName
        )
        {
            RemoveRows(GetListProperty, sourceIndices, undoName);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 행을 목적 위치로 이동한다.
        /// </summary>
        // ------------------------------------------------------------
        public override int Move(int sourceIndex, int destinationIndex)
        {
            return MoveRow
            (
                GetListProperty,
                sourceIndex,
                destinationIndex,
                "컬렉션 항목 이동"
            );
        }

    #endregion

    #region 변경 확정

        /// <inheritdoc />
        protected override void CommitSerializedChanges(bool recordUndo)
        {
            // NONE
        }

    #endregion

    }
}
