/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : MValueModifierEditorModel.cs
수정일 : 2026-10-30

# 설명
MValue<T>의 modifiers XOrdered에 owner lifecycle commit을 추가하는 Collection Editor Model.
XOrdered invariant 복구 뒤 MValue의 modifier 구독과 Modified cache를 재구성한다.
========================================================================= BLOCK_HEADER_END */

using System;

using UnityEditor;

namespace inonego.Xeri.Serializable.Editor
{
    // ============================================================
    /// <summary>
    /// MValue modifiers용 owner-aware XOrdered Editor Model.
    /// </summary>
    // ============================================================
    internal sealed class MValueModifierEditorModel<T> : XOrderedEditorModel
    where T : struct
    {

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// modifiers property에서 owner를 경로 기반으로 연결한다.
        /// </summary>
        // ------------------------------------------------------------
        public MValueModifierEditorModel(SerializedProperty modifiersProperty)
            : base(modifiersProperty ?? throw new ArgumentNullException(nameof(modifiersProperty)))
        {
            // NONE
        }

    #endregion

    #region 진단

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> 숫자 Modifier의 DIV 0 상태를 Value 셀 경고로 제공한다.
        /// <br/> 행의 구조적 무효 여부와 런타임 계산에는 영향을 주지 않는다.
        /// </summary>
        // ------------------------------------------------------------
        public override CollectionValidationResult Validate(CollectionValidationContext context)
        {
            var result = base.Validate(context);
            if (context.IsCandidate || !context.SourceIndex.HasValue
                || context.SourceIndex.Value < 0 || context.SourceIndex.Value >= Count)
            {
                return result;
            }

            var property = GetCellProperty(context.SourceIndex.Value, "value");
            if (property?.propertyType != SerializedPropertyType.ManagedReference)
            {
                return result;
            }

            var operation = property.FindPropertyRelative("operation");
            var divisor = property.FindPropertyRelative("value");
            if (operation == null || divisor == null)
            {
                return result;
            }

            var invalid = property.managedReferenceValue switch
            {
                NumericIModifier when typeof(T) == typeof(int)
                    => operation.enumValueIndex == (int)NumericIOperation.DIV
                        && divisor.intValue == 0,
                NumericFModifier when typeof(T) == typeof(float)
                    => operation.enumValueIndex == (int)NumericFOperation.DIV
                        && divisor.floatValue == 0f,
                _ => false
            };
            if (invalid)
            {
                result.Add
                (
                    new CollectionCellDiagnostic
                    (
                        "modifier.numeric.divide-by-zero", DiagnosticSeverity.Warning,
                        "0으로 나누는 연산은 적용되지 않습니다.", "value", divisor.propertyPath
                    )
                );
            }

            return result;
        }

    #endregion

    #region 변경 처리

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> 새 Numeric Modifier가 SET 0으로 MValue를 덮어쓰지 않도록 한다.
        /// <br/> 새로 생성한 기본 타입에만 ADD 0을 적용한다.
        /// <br/> 기존 항목·파생 타입·일반 Collection Editor 생성 정책은 유지한다.
        /// </summary>
        // ----------------------------------------------------------------------
        public override int Add(Action<int> initialize = null)
        {
            if (initialize == null)
            {
                return base.Add();
            }

            return base.Add
            (
                index =>
                {
                    initialize(index);

                    var valueProperty = GetCellProperty(index, "value");
                    if (valueProperty?.propertyType != SerializedPropertyType.ManagedReference)
                    {
                        return;
                    }

                    var type = valueProperty.managedReferenceValue?.GetType();
                    var operation = valueProperty.FindPropertyRelative("operation");
                    if (operation == null || operation.propertyType != SerializedPropertyType.Enum)
                    {
                        return;
                    }

                    if (typeof(T) == typeof(int) && type == typeof(NumericIModifier))
                    {
                        operation.enumValueIndex = (int)NumericIOperation.ADD;
                    }
                    else if (typeof(T) == typeof(float) && type == typeof(NumericFModifier))
                    {
                        operation.enumValueIndex = (int)NumericFOperation.ADD;
                    }
                }
            );
        }

        /// <inheritdoc />
        protected override void RefreshOwner()
        {
            FinalizeOwnerChange();
        }

        /// <inheritdoc />
        protected override void RestoreOwner()
        {
            FinalizeOwnerChange();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 편집 확정과 Undo 복원 모두 현재 소유자의 직렬화 상태를 확정한다.
        /// </summary>
        // ------------------------------------------------------------
        private void FinalizeOwnerChange()
        {
            // Apply가 중첩 owner를 교체할 수 있으므로 항상 property path로 다시 찾는다.
            var lastDot = CollectionPropertyPath.LastIndexOf('.');
            if (lastDot < 0)
            {
                return;
            }

            var parent = SerializedObject.FindProperty(CollectionPropertyPath[..lastDot]);
            if (parent != null
                && SerializedPropertyHelper.GetTargetObject(parent) is MValue<T> currentOwner)
            {
                ValueSerializationTracker.FinalizeSerializedChange
                (
                    SerializedObject.targetObject, parent.propertyPath, currentOwner
                );
            }
        }

    #endregion

    }
}
