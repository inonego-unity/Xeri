/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : XPriorityQueueEditorModel.cs
수정일 : 2026-10-31

# 설명
XPriorityQueue의 serialized heap을 Element/Priority table로 노출한다.
Element 추가 및 Priority 변경 이후 Runtime 정규화로 Heap 상태를 복원한다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

using UnityEditor;

namespace inonego.Xeri.Serializable.Editor
{
    // ============================================================
    /// <summary>
    /// XPriorityQueue Collection Editor Model.
    /// </summary>
    // ============================================================
    internal sealed class XPriorityQueueEditorModel
        : XNormalizedModelBase
    {

    #region 상수

        private const string heapFieldName = "heap";
        private const string elementFieldName = "Element";
        private const string priorityFieldName = "Priority";

    #endregion

    #region 상태

        /// <inheritdoc />
        public override IReadOnlyList<CollectionColumnSpec> Columns => columns;
        private readonly IReadOnlyList<CollectionColumnSpec> columns;
        private readonly Type elementType;
        private readonly Comparison<object> comparePriority;

        /// <inheritdoc />
        public override string Description => "우선순위가 높은 항목부터 표시하며, 내부 저장 순서와 다릅니다.";
        /// <inheritdoc />
        protected override bool ReordersRowsOnCommit(string propertyPath)
        {
            return propertyPath != null
                && propertyPath.EndsWith("." + priorityFieldName, StringComparison.Ordinal);
        }

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// XPriorityQueue property의 Editor Model을 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        public XPriorityQueueEditorModel(SerializedProperty property)
            : base(property, heapFieldName, "XPriorityQueue")
        {
            var instance = SerializedPropertyHelper.GetTargetObject(property);
            var collectionType = FindCollectionType(instance?.GetType(), typeof(XPriorityQueue<,>));
            if (collectionType == null)
            {
                throw new ArgumentException
                (
                    "XPriorityQueue의 직렬화 인스턴스를 확인할 수 없습니다.",
                    nameof(property)
                );
            }

            var genericArguments = collectionType.GetGenericArguments();
            elementType = genericArguments[0];

            var compareMethod = typeof(XPriorityQueueEditorModel).GetMethod
            (
                nameof(CreatePriorityComparison),
                BindingFlags.NonPublic | BindingFlags.Static
            );
            comparePriority = (Comparison<object>)compareMethod
                .MakeGenericMethod(genericArguments[1]).Invoke(null, null);

            columns = new[]
            {
                new CollectionColumnSpec
                (
                    elementFieldName,
                    "항목",
                    220f,
                    100f
                ),
                new CollectionColumnSpec
                (
                    priorityFieldName,
                    "우선순위",
                    100f,
                    70f
                ),
            };
        }

    #endregion

    #region 조회

        /// <inheritdoc />
        public override IEnumerable<int> GetSourceRows()
        {
            var rows = new List<int>();
            for (var index = 0; index < Count; index++)
            {
                rows.Add(index);
            }

            // 표시 순서만 정렬하고 serialized heap의 index는 변경하지 않는다.
            rows.Sort(CompareRows);
            return rows;
        }

        private int CompareRows(int left, int right)
        {
            var result = comparePriority
            (
                GetPropertyValue(GetCellProperty(right, priorityFieldName)),
                GetPropertyValue(GetCellProperty(left, priorityFieldName))
            );
            return result != 0 ? result : left.CompareTo(right);
        }

        private static Comparison<object> CreatePriorityComparison<T>()
        {
            return (left, right) => Comparer<T>.Default.Compare((T)left, (T)right);
        }

        /// <inheritdoc />
        public override bool TryGetManagedReferenceAddTarget
        (
            out string columnID,
            out Type declaredType
        )
        {
            columnID = elementFieldName;
            declaredType = elementType;
            return declaredType != null;
        }

    #endregion

    }
}
