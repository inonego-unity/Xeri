/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : XOrderedEditorModel.cs
수정일 : 2026-11-01

# 설명
XOrdered의 Entry를 Order/Key/Value table로 편집하고 행 이동을 지원한다.
항목 이동·변경 이후 Order 안정 정렬을 적용하여 동일 Order 내의 상대 순서를 유지한다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

using UnityEditor;

using inonego;
using inonego.Xeri;

namespace inonego.Xeri.Serializable.Editor
{
    // ============================================================
    /// <summary>
    /// XOrdered Collection Editor Model.
    /// </summary>
    // ============================================================
    internal class XOrderedEditorModel
        : XNormalizedModelBase
    {

    #region 상수

        private const string entriesFieldName = "entries";
        private const string orderFieldName = "order";
        private const string keyFieldName = "key";
        private const string valueFieldName = "value";

    #endregion

    #region 상태

        /// <inheritdoc />
        public override IReadOnlyList<CollectionColumnSpec> Columns => columns;
        private readonly IReadOnlyList<CollectionColumnSpec> columns;

        private readonly bool isKeyed;
        private readonly Type valueType;
        private readonly Func<object, object, bool> keyEquals;
        private readonly IEqualityComparer<object> keyComparer;

        /// <inheritdoc />
        public override string ValidationColumnID => isKeyed ? keyFieldName : orderFieldName;

        /// <inheritdoc />
        public override bool CanReorder => true;
        /// <inheritdoc />
        public override string Description => "순서 값에 따라 정렬하며, 같은 순서의 항목은 드래그한 순서를 유지합니다.";

        /// <inheritdoc />
        protected override bool ReordersRowsOnCommit(string propertyPath)
        {
            return propertyPath != null
                && (propertyPath.EndsWith("." + orderFieldName, StringComparison.Ordinal)
                    || propertyPath.EndsWith("." + keyFieldName, StringComparison.Ordinal));
        }

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// XOrdered property의 Editor Model을 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        public XOrderedEditorModel(SerializedProperty property)
            : base(property, entriesFieldName, "XOrdered")
        {
            var instance = SerializedPropertyHelper.GetTargetObject(property);
            var genericArguments = FindCollectionType(instance?.GetType(), typeof(XOrdered<,>), typeof(XOrdered<,,>))?.GetGenericArguments();
            isKeyed = genericArguments?.Length == 3;
            valueType = genericArguments != null && genericArguments.Length >= 2
                ? genericArguments[^1]
                : null;

            keyComparer = CreateBoxedComparer(isKeyed ? genericArguments[1] : typeof(object));
            keyEquals = keyComparer.Equals;

            if (isKeyed)
            {
                columns = new CollectionColumnSpec[]
                {
                    // Order는 고정하고 동일한 기준 폭의 Key:Value를 4:6으로 배분한다.
                    new(orderFieldName, "순서", 70f, 70f, stretchable: false),
                    new(keyFieldName, "키", 150f, 70f, stretchWeight: 4f),
                    new(valueFieldName, "값", 150f, 100f, stretchWeight: 6f),
                };
            }
            else
            {
                columns = new CollectionColumnSpec[]
                {
                    // Key 없는 XOrdered도 Order만 70px로 고정한다.
                    new(orderFieldName, "순서", 70f, 70f, stretchable: false),
                    new(valueFieldName, "값", 240f, 100f),
                };
            }
        }

    #endregion

    #region 조회

        // ------------------------------------------------------------
        /// <summary>
        /// Key와 Order는 즉시 편집하지 않고 편집 창에서 확정한다.
        /// </summary>
        // ------------------------------------------------------------
        public override CollectionCellEditMode GetCellEditMode
        (
            int sourceIndex,
            string columnID
        )
        {
            return columnID == keyFieldName || columnID == orderFieldName
                ? CollectionCellEditMode.ReplaceOnly
                : CollectionCellEditMode.Default;
        }

        /// <inheritdoc />
        public override bool TryGetManagedReferenceAddTarget
        (
            out string columnID,
            out Type declaredType
        )
        {
            columnID = valueFieldName;
            declaredType = valueType;
            return declaredType != null;
        }

        private object GetKeyObject(int sourceIndex)
        {
            return SerializedPropertyHelper.GetValue(GetCellProperty(sourceIndex, keyFieldName));
        }

    #endregion

    #region 검증

        /// <inheritdoc />
        public override string GetRowValidationMessage(int sourceIndex)
        {
            if (!isKeyed)
            {
                return string.Empty;
            }

            var key = GetKeyObject(sourceIndex);
            if (key == null)
            {
                return "키가 지정되지 않았습니다.";
            }

            // Key lookup은 앞에서부터 첫 일치를 반환하므로 뒤쪽 duplicate만 invalid다.
            for (var index = 0; index < sourceIndex; index++)
            {
                var other = GetKeyObject(index);
                if (other != null && keyEquals(key, other))
                {
                    return $"{index + 1}번 항목과 키가 중복됩니다. 앞쪽 값이 사용됩니다.";
                }
            }

            return string.Empty;
        }

        /// <inheritdoc />
        public override IReadOnlyList<int> GetInvalidRowIndices()
        {
            var indices = new List<int>();
            if (!isKeyed)
            {
                return indices;
            }

            var seen = new HashSet<object>(keyComparer);
            for (var index = 0; index < Count; index++)
            {
                var key = GetKeyObject(index);
                if (key == null || !seen.Add(key))
                {
                    indices.Add(index);
                }
            }

            return indices;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Key/Order 후보의 적용 가능성을 공통 진단 결과로 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public override CollectionValidationResult Validate(CollectionValidationContext context)
        {
            if (!context.IsCandidate)
            {
                return base.Validate(context);
            }

            var result = new CollectionValidationResult();
            var reason = ValidateStructuralReplacement
            (
                context.SourceIndex ?? -1, context.ColumnID, context.CandidateValue
            );
            if (!string.IsNullOrEmpty(reason))
            {
                result.Add
                (
                    new CollectionCellDiagnostic
                    (
                        "collection.structure.invalid-candidate", DiagnosticSeverity.Error,
                        reason, context.ColumnID
                    )
                );
                result.BlockApply();
            }

            return result;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Key 중복 및 필드 타입을 확정 전에 검증한다.
        /// </summary>
        // ------------------------------------------------------------
        public string ValidateStructuralReplacement
        (
            int sourceIndex,
            string columnID,
            object candidate
        )
        {
            if (sourceIndex < 0 || sourceIndex >= Count)
            {
                return "편집할 행이 존재하지 않습니다.";
            }

            if (columnID != orderFieldName && (!isKeyed || columnID != keyFieldName))
            {
                return "키 또는 순서 필드가 아닙니다.";
            }

            var property = GetCellProperty(sourceIndex, columnID);
            if (property == null)
            {
                return "편집할 필드를 찾을 수 없습니다.";
            }

            var row = SerializedPropertyHelper.GetTargetObject(GetRowProperty(sourceIndex));
            var declaredType = row?.GetType().GetField
            (
                columnID,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
            )?.FieldType;
            if (candidate == null || declaredType == null
                || !declaredType.IsInstanceOfType(candidate))
            {
                return "편집할 필드의 타입이 올바르지 않습니다.";
            }

            if (columnID != keyFieldName)
            {
                return string.Empty;
            }

            for (var index = 0; index < Count; index++)
            {
                if (index != sourceIndex && keyEquals(candidate, GetKeyObject(index)))
                {
                    return $"{index + 1}번 항목과 키가 중복됩니다.";
                }
            }

            return string.Empty;
        }

    #endregion

    #region Key·Order 구조 편집

        // ------------------------------------------------------------
        /// <summary>
        /// 검증된 Key/Order 값을 확정하고 정렬 및 owner 상태를 갱신한다.
        /// </summary>
        // ------------------------------------------------------------
        public int ReplaceStructuralValue
        (
            int sourceIndex,
            string columnID,
            object candidate
        )
        {
            var reason = ValidateStructuralReplacement(sourceIndex, columnID, candidate);
            if (!string.IsNullOrEmpty(reason))
            {
                throw new InvalidOperationException(reason);
            }

            // Unity가 Entry 객체를 교체하더라도 Keyed 행은 고유 Key로 추적한다.
            var key = isKeyed && columnID != keyFieldName
                ? GetKeyObject(sourceIndex)
                : candidate;
            var undoName = columnID == keyFieldName ? "XOrdered 키 변경" : "XOrdered 순서 변경";
            Undo.RecordObjects(SerializedObject.targetObjects, undoName);
            SerializedObject.Update();
            var property = GetCellProperty(sourceIndex, columnID);
            var path = property.propertyPath;
            property.boxedValue = candidate;
            var resolved = CommitRowChanges(sourceIndex, path);
            if (resolved >= 0 || !isKeyed)
            {
                return resolved;
            }

            // 기존 중복 Key가 있으면 어느 행인지 단정하지 않는다.
            var match = -1;
            for (var index = 0; index < Count; index++)
            {
                if (!keyEquals(key, GetKeyObject(index)))
                {
                    continue;
                }

                if (match >= 0)
                {
                    return -1;
                }

                match = index;
            }

            return match;
        }

    #endregion

    }
}
