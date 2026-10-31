/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : XSerializedListEditorModelBase.cs
수정일 : 2026-10-21

# 설명
serialized 필드 하나를 backing list로 사용하는 Xeri collection Editor Model의 공통 조작을 제공한다.
구체 collection의 Add 위치, managed-reference element, reorder, validation과 Undo 의미를 반영한다.
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
    // ======================================================================
    /// <summary>
    /// serialized list backing 기반 Xeri Collection Editor Model 공통 구현.
    /// </summary>
    // ======================================================================
    internal abstract class XSerializedListEditorModelBase : CollectionEditorModelBase
    {

    #region 상수

        private const string serializedFieldName = "serialized";
        private const string valueColumnID = "Value";

    #endregion

    #region 상태

        /// <inheritdoc />
        public override IReadOnlyList<CollectionColumnSpec> Columns => columns;
        private readonly IReadOnlyList<CollectionColumnSpec> columns;

        /// <inheritdoc />
        public override int Count => GetSerializedList().arraySize;

        /// <inheritdoc />
        public abstract override bool CanReorder { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 항목 추가 작업에 사용할 Undo 이름을 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        protected abstract string AddUndoName { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 항목 제거 작업에 사용할 Undo 이름을 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        protected abstract string RemoveUndoName { get; }

        private readonly Type elementType;
        private readonly bool isManagedReference;

        // ------------------------------------------------------------
        /// <summary>
        /// 직렬화된 두 원소의 동등성을 확인하는 비교 함수를 보관한다.
        /// </summary>
        // ------------------------------------------------------------
        protected readonly Func<object, object, bool> elementEquals;

    #endregion

    #region 생성자

        // ----------------------------------------------------------------------
        /// <summary>
        /// serialized backing list를 사용하는 collection Model을 생성한다.
        /// </summary>
        // ----------------------------------------------------------------------
        protected XSerializedListEditorModelBase
        (
            SerializedProperty property,
            string columnHeader
        )
            : base(property)
        {
            columns = new[]
            {
                new CollectionColumnSpec
                (
                    valueColumnID,
                    columnHeader,
                    width: 240f,
                    minWidth: 100f
                ),
            };

            var instance = SerializedPropertyHelper.GetTargetObject(property);
            ResolveElementMetadata
            (
                instance?.GetType(),
                out elementType,
                out isManagedReference
            );

            elementEquals = CreateEquality
            (
                elementType ?? typeof(object),
                instance?.GetType().GetProperty("Comparer")?.GetValue(instance)
            );
            _ = GetSerializedList();
        }

    #endregion

    #region 조회

        /// <inheritdoc />
        public override SerializedProperty GetRowProperty(int sourceIndex)
        {
            return GetSerializedList().GetArrayElementAtIndex(sourceIndex);
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

        // ------------------------------------------------------------
        /// <summary>
        /// 지정한 행의 직렬화된 원소 값을 조회한다.
        /// </summary>
        // ------------------------------------------------------------
        protected object GetElementObject(int sourceIndex)
        {
            return GetPropertyValue
            (
                GetCellProperty(sourceIndex, valueColumnID)
            );
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

        private SerializedProperty GetSerializedList()
        {
            var root = GetCollectionProperty();
            var list = root?.FindPropertyRelative(serializedFieldName);

            if (list == null || !list.isArray)
            {
                throw new InvalidOperationException
                (
                    "Xeri 컬렉션의 직렬화 목록을 찾을 수 없습니다."
                );
            }

            return list;
        }

    #endregion

    #region 행 변경

        /// <inheritdoc />
        public override int Add(Action<int> initialize = null)
        {
            return AddDefaultRow
            (
                GetSerializedList,
                InsertIndex,
                AddUndoName,
                initialize
            );
        }

        /// <inheritdoc />
        public override int DuplicateAt(int sourceIndex)
        {
            return DuplicateRow
            (
                GetSerializedList,
                sourceIndex,
                InsertIndex,
                "컬렉션 항목 복제",
                "컬렉션 항목"
            );
        }

        /// <inheritdoc />
        public override void RemoveAt(int sourceIndex)
        {
            RemoveRow
            (
                GetSerializedList,
                sourceIndex,
                RemoveUndoName
            );
        }

        /// <inheritdoc />
        public override void RemoveMany
        (
            IReadOnlyList<int> sourceIndices,
            string undoName
        )
        {
            RemoveRows(GetSerializedList, sourceIndices, undoName);
        }

        /// <inheritdoc />
        public override void Move(int sourceIndex, int destinationIndex)
        {
            if (!CanReorder)
            {
                return;
            }

            MoveRow
            (
                GetSerializedList,
                sourceIndex,
                destinationIndex,
                "컬렉션 항목 이동"
            );
        }

        /// <inheritdoc />
        protected override void CommitSerializedChanges(bool recordUndo)
        {
            UpdateRuntimeCollection();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 새 항목을 삽입할 기본 위치를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        protected virtual int? InsertIndex => null;

        // --------------------------------------------------------------------------------
        /// <summary>
        /// XHashSet/XQueue/XStack 변형에서 element 타입과 SerializeReference 여부를 해석한다.
        /// </summary>
        // --------------------------------------------------------------------------------
        private static void ResolveElementMetadata
        (
            Type collectionType,
            out Type resolvedElementType,
            out bool resolvedManagedReference
        )
        {
            resolvedElementType = null;
            resolvedManagedReference = false;

            for (var type = collectionType; type != null; type = type.BaseType)
            {
                if (!type.IsGenericType)
                {
                    continue;
                }

                var definition = type.GetGenericTypeDefinition();
                var isReference = definition == typeof(XHashSet_R<>)
                    || definition == typeof(XQueue_R<>)
                    || definition == typeof(XStack_R<>);
                var isValue = definition == typeof(XHashSet_V<>)
                    || definition == typeof(XQueue_V<>)
                    || definition == typeof(XStack_V<>);
                if (!isReference && !isValue)
                {
                    continue;
                }

                resolvedElementType = type.GetGenericArguments()[0];
                resolvedManagedReference = isReference;
                return;
            }
        }

    #endregion

    }
}
