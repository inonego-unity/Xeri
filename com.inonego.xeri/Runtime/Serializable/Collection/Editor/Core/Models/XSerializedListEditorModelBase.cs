/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : XSerializedListEditorModelBase.cs
수정일 : 2026-11-01

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
        /// 새 항목을 삽입할 기본 위치를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        protected virtual int? InsertIndex => null;

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
        protected readonly IEqualityComparer<object> elementComparer;

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

            elementComparer = CreateBoxedComparer
            (
                elementType ?? typeof(object),
                instance?.GetType().GetProperty("Comparer")?.GetValue(instance)
            );
            elementEquals = elementComparer.Equals;
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
            return SerializedPropertyHelper.GetValue(GetCellProperty(sourceIndex, valueColumnID));
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

    #region 타입 해석

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
            var type = FindCollectionType
            (
                collectionType,
                typeof(XHashSet_R<>), typeof(XHashSet_V<>),
                typeof(XQueue_R<>), typeof(XQueue_V<>),
                typeof(XStack_R<>), typeof(XStack_V<>)
            );
            resolvedElementType = type?.GetGenericArguments()[0];
            var definition = type?.GetGenericTypeDefinition();
            resolvedManagedReference = definition == typeof(XHashSet_R<>)
                || definition == typeof(XQueue_R<>)
                || definition == typeof(XStack_R<>);
        }

    #endregion

    #region 행 변경

        /// <inheritdoc />
        public override CollectionRowCommandResult Add(Action<int> initialize = null)
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
        public override CollectionRowCommandResult DuplicateAt(int sourceIndex)
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
        public override int Move(int sourceIndex, int destinationIndex)
        {
            if (!CanReorder)
            {
                return -1;
            }

            return MoveRow
            (
                GetSerializedList,
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
            UpdateRuntimeCollection();
        }

    #endregion

    }
}
