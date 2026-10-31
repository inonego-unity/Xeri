/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : SerializedListEditorModel.cs
수정일 : 2026-10-21

# 설명
CollectionEditorAttribute가 붙은 List<T>와 1차원 배열을 1열 Collection Editor로 노출한다.
선택적 Unique validation, managed-reference Add metadata와 serialized 순서 기반 mutation을 담당한다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEditor;
using UnityEngine;

using inonego;
using inonego.Xeri;

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

        /// <inheritdoc />
        public override IReadOnlyList<CollectionColumnSpec> Columns => columns;
        private readonly IReadOnlyList<CollectionColumnSpec> columns;

        /// <inheritdoc />
        public override int Count => GetListProperty().arraySize;

        /// <inheritdoc />
        public override bool CanReorder => true;

        // ------------------------------------------------------------
        /// <summary>
        /// 중복 항목을 허용하지 않는 목록인지 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        internal bool IsUnique => unique;
        private readonly bool unique;
        private readonly Type elementType;
        private readonly bool isManagedReference;
        private readonly Func<object, object, bool> equals;

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
            bool isManagedReference
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
            equals = CreateEquality(elementType ?? typeof(object));
            this.isManagedReference = isManagedReference;

            columns = new[]
            {
                new CollectionColumnSpec
                (
                    valueColumnID,
                    "값",
                    width: 240f,
                    minWidth: 100f
                ),
            };
        }

    #endregion

    #region 조회

        /// <inheritdoc />
        public override ICollectionEditorModel CreateFor(SerializedProperty property)
        {
            return new SerializedListEditorModel(property, unique, elementType, isManagedReference);
        }

        /// <inheritdoc />
        public override SerializedProperty GetRowProperty(int sourceIndex)
        {
            return GetListProperty().GetArrayElementAtIndex(sourceIndex);
        }

        /// <inheritdoc />
        public override SerializedProperty GetCellProperty
        (
            int sourceIndex,
            string columnID
        )
        {
            return GetRowProperty(sourceIndex);
        }

        /// <inheritdoc />
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

        /// <inheritdoc />
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
            return GetPropertyValue
            (
                GetCellProperty(sourceIndex, valueColumnID)
            );
        }

    #endregion

    #region 행 변경

        /// <inheritdoc />
        public override int Add(Action<int> initialize = null)
        {
            return AddDefaultRow
            (
                GetListProperty,
                insertIndex: null,
                "컬렉션 항목 추가",
                initialize
            );
        }

        /// <inheritdoc />
        public override int DuplicateAt(int sourceIndex)
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

        /// <inheritdoc />
        public override void RemoveAt(int sourceIndex)
        {
            RemoveRow
            (
                GetListProperty,
                sourceIndex,
                "컬렉션 항목 삭제"
            );
        }

        /// <inheritdoc />
        public override void RemoveMany
        (
            IReadOnlyList<int> sourceIndices,
            string undoName
        )
        {
            RemoveRows(GetListProperty, sourceIndices, undoName);
        }

        /// <inheritdoc />
        public override void Move(int sourceIndex, int destinationIndex)
        {
            MoveRow
            (
                GetListProperty,
                sourceIndex,
                destinationIndex,
                "컬렉션 항목 이동"
            );
        }

        /// <inheritdoc />
        protected override void CommitSerializedChanges(bool recordUndo)
        {
            // NONE
        }

    #endregion

    }
}
