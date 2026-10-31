/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : XOrderedEditorModel.cs
수정일 : 2026-10-31

# 설명
XOrdered의 serialized Entry 목록을 Order/Key/Value table로 노출한다.
Value 추가 및 Order·Key 변경 이후 Runtime 정규화로 정렬 상태를 복원한다.
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

        /// <inheritdoc />
        public override string ValidationColumnID => isKeyed ? keyFieldName : orderFieldName;
        /// <inheritdoc />
        public override string Description => "순서 값을 적용하면 오름차순으로 정렬됩니다.";

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

            keyEquals = CreateEquality(isKeyed ? genericArguments[1] : typeof(object));

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

        private object GetKeyObject(int sourceIndex)
        {
            return GetPropertyValue
            (
                GetCellProperty(sourceIndex, keyFieldName)
            );
        }

    #endregion

    }
}
