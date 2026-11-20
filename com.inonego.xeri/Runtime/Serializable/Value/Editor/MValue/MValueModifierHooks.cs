/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : MValueModifierHooks.cs
수정일 : 2026-11-19

# 설명
MValue<T>의 modifiers(XOrdered)를 컬렉션 표로 편집할 때 필요한 추가 처리를 hook으로 제공한다.
새 숫자 Modifier의 기본 연산, DIV 0 경고, 변경 확정·Undo 뒤 소유자(MValue) 상태 확정을 맡는다.

# 특이사항, 제약사항
소유자는 Apply 뒤 교체될 수 있으므로 매번 현재 경로에서 다시 찾는다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEditor;

using inonego;
using inonego.Xeri;
using inonego.Xeri.Editor;

namespace inonego.Xeri.Serializable.Editor
{
    // ============================================================
    /// <summary>
    /// MValue modifiers 편집 hook.
    /// </summary>
    // ============================================================
    internal sealed class MValueModifierHooks<T> : ICollectionKindHooks
    where T : struct
    {

    #region 필드

        private const string valueColumnID = "value";

    #endregion

    #region 종류 생성

        // ------------------------------------------------------------
        /// <summary>
        /// modifiers 속성의 XOrdered 종류 정의에 이 hook을 붙여 만든다.
        /// </summary>
        // ------------------------------------------------------------
        public static CollectionKind CreateKind(SerializedProperty modifiersProperty)
        {
            if (modifiersProperty == null)
            {
                throw new ArgumentNullException(nameof(modifiersProperty));
            }

            return XOrderedKindProvider.Create(modifiersProperty, new MValueModifierHooks<T>());
        }

    #endregion

    #region hook

        // ------------------------------------------------------------
        /// <summary>
        /// 편집 확정이 세션에서 일어나므로 소유자의 이전 상태를 먼저 관찰한다.
        /// </summary>
        // ------------------------------------------------------------
        public void Attach(CollectionSource source)
        {
            if (TryGetOwner(source, out var ownerProperty, out var owner))
            {
                ValueSerializationTracker.Observe(source.SerializedObject.targetObject, ownerProperty.propertyPath, owner);
            }
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> 새 숫자 Modifier가 SET 0으로 MValue를 덮어쓰지 않도록 ADD로 둔다.
        /// <br/> 파생 타입과 다른 Modifier는 그대로 둔다.
        /// </summary>
        // ----------------------------------------------------------------------
        public void InitializeRow(CollectionSource source, int sourceIndex)
        {
            var property = GetValueProperty(source, sourceIndex);
            var operation = property?.FindPropertyRelative("operation");
            if (operation == null || operation.propertyType != SerializedPropertyType.Enum)
            {
                return;
            }

            var type = property.managedReferenceValue?.GetType();
            if (typeof(T) == typeof(int) && type == typeof(NumericIModifier))
            {
                operation.enumValueIndex = (int)NumericIOperation.ADD;
            }
            else if (typeof(T) == typeof(float) && type == typeof(NumericFModifier))
            {
                operation.enumValueIndex = (int)NumericFOperation.ADD;
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> 숫자 Modifier의 DIV 0 상태를 값 셀 경고로 더한다.
        /// <br/> 행 무효 여부와 Runtime 계산에는 영향을 주지 않는다.
        /// </summary>
        // ------------------------------------------------------------
        public void AppendDiagnostics
        (
            CollectionSource source, int sourceIndex, List<CollectionCellDiagnostic> output
        )
        {
            var property = GetValueProperty(source, sourceIndex);
            var operation = property?.FindPropertyRelative("operation");
            var divisor = property?.FindPropertyRelative("value");
            if (operation == null || divisor == null)
            {
                return;
            }

            var invalid = property.managedReferenceValue switch
            {
                NumericIModifier when typeof(T) == typeof(int)
                    => operation.enumValueIndex == (int)NumericIOperation.DIV && divisor.intValue == 0,
                NumericFModifier when typeof(T) == typeof(float)
                    => operation.enumValueIndex == (int)NumericFOperation.DIV && divisor.floatValue == 0f,
                _ => false,
            };
            if (invalid)
            {
                output.Add
                (
                    new CollectionCellDiagnostic
                    (
                        "modifier.numeric.divide-by-zero", DiagnosticSeverity.Warning,
                        "0으로 나누는 연산은 적용되지 않습니다.", valueColumnID, divisor.propertyPath
                    )
                );
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 편집 확정과 Undo 복원 뒤 현재 소유자의 직렬화 상태를 확정한다.
        /// </summary>
        // ------------------------------------------------------------
        public void SyncOwner(CollectionSource source)
        {
            if (TryGetOwner(source, out var ownerProperty, out var owner))
            {
                ValueSerializationTracker.FinalizeSerializedChange(source.SerializedObject.targetObject, ownerProperty.propertyPath, owner);
            }
        }

    #endregion

    #region 조회

        private static SerializedProperty GetValueProperty(CollectionSource source, int sourceIndex)
        {
            var column = source.Kind.FindColumn(valueColumnID);
            var property = column == null ? null : source.GetCell(sourceIndex, column);
            return property?.propertyType == SerializedPropertyType.ManagedReference ? property : null;
        }

        private static bool TryGetOwner(CollectionSource source, out SerializedProperty ownerProperty, out MValue<T> owner)
        {
            ownerProperty = null;
            owner = null;
            var lastDot = source.Path.LastIndexOf('.');
            if (lastDot < 0)
            {
                return false;
            }

            ownerProperty = source.SerializedObject.FindProperty(source.Path[..lastDot]);
            if (ownerProperty == null || SerializedPropertyHelper.GetTargetObject(ownerProperty) is not MValue<T> resolved)
            {
                return false;
            }

            owner = resolved;
            return true;
        }

    #endregion

    }
}
