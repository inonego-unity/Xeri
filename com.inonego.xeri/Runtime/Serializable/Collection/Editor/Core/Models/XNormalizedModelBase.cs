/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : XNormalizedModelBase.cs
수정일 : 2026-10-31

# 설명
Runtime 정규화로 직렬화 행 구조를 복원하는 Collection Editor Model의 공통 변경 처리를 제공한다.
항목 변경 후 Runtime 정규화를 실행하며, 수동 행 이동은 컬렉션별 정책에 따라 허용한다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEditor;
using UnityEngine;

namespace inonego.Xeri.Serializable.Editor
{
    // ============================================================
    /// <summary>
    /// Runtime 정규화 기반 Xeri Collection Editor Model 공통 구현.
    /// </summary>
    // ============================================================
    internal abstract class XNormalizedModelBase
        : CollectionEditorModelBase
    {

    #region 상태

        /// <inheritdoc />
        public override int Count => GetBackingList().arraySize;

        /// <inheritdoc />
        public override bool CanReorder => false;

        private readonly string backingFieldName;
        private readonly string entryUndoName;

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// Runtime 정규화를 사용하는 컬렉션의 공통 Editor Model을 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        protected XNormalizedModelBase
        (
            SerializedProperty property,
            string backingFieldName,
            string entryUndoName
        )
            : base(property)
        {
            this.backingFieldName = backingFieldName
                ?? throw new ArgumentNullException(nameof(backingFieldName));
            this.entryUndoName = entryUndoName
                ?? throw new ArgumentNullException(nameof(entryUndoName));

            _ = GetBackingList();
        }

    #endregion

    #region 조회

        /// <inheritdoc />
        public override SerializedProperty GetRowProperty(int sourceIndex)
        {
            return GetBackingList().GetArrayElementAtIndex(sourceIndex);
        }

        /// <inheritdoc />
        public override SerializedProperty GetCellProperty
        (
            int sourceIndex,
            string columnID
        )
        {
            return GetRowProperty(sourceIndex).FindPropertyRelative(columnID);
        }

        private SerializedProperty GetBackingList()
        {
            var root = GetCollectionProperty();
            var list = root?.FindPropertyRelative(backingFieldName);

            if (list == null || !list.isArray)
            {
                throw new InvalidOperationException
                (
                    $"{entryUndoName}의 직렬화 목록을 찾을 수 없습니다."
                );
            }

            return list;
        }

    #endregion

    #region 행 변경

        /// <inheritdoc />
        public override CollectionRowCommandResult Add(Action<int> initialize = null)
        {
            return AddDefaultRow
            (
                GetBackingList,
                insertIndex: null,
                $"{entryUndoName} 항목 추가",
                initialize
            );
        }

        /// <inheritdoc />
        public override CollectionRowCommandResult DuplicateAt(int sourceIndex)
        {
            return DuplicateRow
            (
                GetBackingList,
                sourceIndex,
                insertIndex: null,
                $"{entryUndoName} 항목 복제",
                $"{entryUndoName} Entry"
            );
        }

        /// <inheritdoc />
        public override void RemoveAt(int sourceIndex)
        {
            RemoveRow
            (
                GetBackingList,
                sourceIndex,
                $"{entryUndoName} 항목 삭제"
            );
        }

        /// <inheritdoc />
        public override void RemoveMany
        (
            IReadOnlyList<int> sourceIndices,
            string undoName
        )
        {
            RemoveRows(GetBackingList, sourceIndices, undoName);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 행 이동 후 Runtime 정규화를 실행하고 최종 위치를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public override int Move(int sourceIndex, int destinationIndex)
        {
            if (!CanReorder)
            {
                return -1;
            }

            return MoveRow
            (
                GetBackingList,
                sourceIndex,
                destinationIndex,
                $"{entryUndoName} 항목 이동"
            );
        }

    #endregion

    #region 변경 확정

        // ----------------------------------------------------------------------
        /// <summary>
        /// 셀 편집은 Unity Undo를 보완하고, 행 명령은 이미 기록된 Undo를 사용한다.
        /// </summary>
        // ----------------------------------------------------------------------
        protected override void CommitSerializedChanges(bool recordUndo)
        {
            if (recordUndo)
            {
                Undo.RecordObjects
                (
                    SerializedObject.targetObjects,
                    $"{entryUndoName} 정리"
                );
            }

            NormalizeSerializedState();
        }

    #endregion

    }
}
