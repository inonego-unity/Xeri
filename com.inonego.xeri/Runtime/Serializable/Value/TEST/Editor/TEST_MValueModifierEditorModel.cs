/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TEST_MValueModifierEditorModel.cs
수정일 : 2026-10-30

# 설명
MValue 전용 Collection Editor Model의 Modifier 진단 계약을 검증한다.
숫자 DIV 0은 경고만 제공하며 행 삭제나 적용 차단을 요구하지 않는다.

# 테스트 구성
 V: Modifier 행 진단
========================================================================= BLOCK_HEADER_END */

using System;

using UnityEngine;
using UnityEditor;

using NUnit;
using NUnit.Framework;

using inonego;
using inonego.Xeri;
using inonego.Xeri.Serializable;
using inonego.Xeri.Serializable.Editor;

namespace inonego.Xeri.TEST.Serializable._Value
{
    // ============================================================
    /// <summary>
    /// MValue Modifier Editor Model의 행 진단 테스트.
    /// </summary>
    // ============================================================
    public class TEST_MValueModifierEditorModel
    {

    #region 헬퍼

        // ============================================================
        /// <summary>
        /// 직렬화된 Modifier를 제공하는 테스트용 소유자.
        /// </summary>
        // ============================================================
        private sealed class MValueOwner : ScriptableObject
        {
            public MValue<int> Value = new(10);
        }

    #endregion

    #region V-1: 숫자 Modifier 경고

        // ------------------------------------------------------------
        /// <summary>
        /// 0으로 나누는 Modifier는 경고만 제공하고 행을 무효화하지 않는다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_MValueModifierEditorModel_DIV0_경고_적용허용()
        {
            var owner = ScriptableObject.CreateInstance<MValueOwner>();
            try
            {
                owner.Value.AddModifier
                (
                    "divide", new NumericIModifier(NumericIOperation.DIV, 0),
                    invokeEvent: false
                );

                var serialized = new SerializedObject(owner);
                var property = serialized.FindProperty("Value.modifiers");
                Assert.IsNotNull(property);

                var model = new MValueModifierEditorModel<int>(property);
                var validation = model.Validate(CollectionValidationContext.ForRow(0));

                Assert.IsTrue(validation.CanApply);
                Assert.IsFalse(validation.IsRowInvalid);
                Assert.AreEqual(1, validation.Diagnostics.Count);
                Assert.AreEqual("modifier.numeric.divide-by-zero", validation.Diagnostics[0].Code);
                Assert.AreEqual(DiagnosticSeverity.Warning, validation.Diagnostics[0].Severity);
                Assert.AreEqual("value", validation.Diagnostics[0].ColumnID);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(owner);
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 0이 아닌 제수는 DIV0 진단을 만들지 않는다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_MValueModifierEditorModel_정상제수_경고없음()
        {
            var owner = ScriptableObject.CreateInstance<MValueOwner>();
            try
            {
                owner.Value.AddModifier
                (
                    "divide", new NumericIModifier(NumericIOperation.DIV, 2),
                    invokeEvent: false
                );

                var serialized = new SerializedObject(owner);
                var property = serialized.FindProperty("Value.modifiers");
                Assert.IsNotNull(property);

                var model = new MValueModifierEditorModel<int>(property);
                var validation = model.Validate(CollectionValidationContext.ForRow(0));

                Assert.IsTrue(validation.CanApply);
                Assert.IsFalse(validation.IsRowInvalid);
                Assert.AreEqual(0, validation.Diagnostics.Count);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(owner);
            }
        }

    #endregion

    }
}
