/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TEST_ValueEditorGUI.cs
수정일 : 2026-10-30

# 설명
Value·MValue·RangeValue Drawer의 기존 Inspector 구성 계약을 검증한다.

# 테스트 구성
 G: UI Toolkit Drawer의 레이아웃·편집 진입 요소
========================================================================= BLOCK_HEADER_END */

using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

using NUnit;
using NUnit.Framework;

using inonego;
using inonego.Xeri;
using inonego.Xeri.Primitive;
using inonego.Xeri.Serializable;
using inonego.Xeri.Serializable.Editor;

namespace inonego.Xeri.TEST.Serializable._Value
{
    // ============================================================
    /// <summary>
    /// Value 계열 Drawer의 Inspector 구조 회귀 테스트.
    /// </summary>
    // ============================================================
    public class TEST_ValueEditorGUI
    {

    #region 헬퍼

        private sealed class ValueOwner : ScriptableObject
        {
            public Value<int> Value = new(42);
        }

        private sealed class MValueOwner : ScriptableObject
        {
            public MValue<int> Value = new(42);
        }

        private sealed class RangeValueOwner : ScriptableObject
        {
            public RangeValue<int> Value = new(5, new Range<int>(0, 10));
        }

    #endregion

    #region G-1: Value 기본 편집 구성

        // ------------------------------------------------------------
        /// <summary>
        /// 기본 Value는 한 행과 하나의 편집 토글을 제공한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_ValueEditorGUI_Value_기존편집구성유지()
        {
            var owner = ScriptableObject.CreateInstance<ValueOwner>();
            try
            {
                var property = new SerializedObject(owner).FindProperty("Value");
                var root = new ValueDrawer().CreatePropertyGUI(property);

                Assert.AreEqual(1, root.childCount);
                Assert.AreEqual(1, root.Query<Toggle>().ToList().Count);

                var row = root.Q<VisualElement>("value-base-row");
                Assert.IsNotNull(row);
                Assert.AreEqual(3, row.childCount);
                Assert.AreEqual("Value", root.Q<Label>("value-field-label").text);
                Assert.IsTrue(row.ElementAt(1) is IntegerField);
                Assert.IsTrue(row.ElementAt(2) is Toggle);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(owner);
            }
        }

    #endregion

    #region G-2: MValue Modifier 목록 구성

        // ----------------------------------------------------------------------
        /// <summary>
        /// MValue는 Base/Modified 행과 Modifier Collection View를 함께 제공한다.
        /// </summary>
        // ----------------------------------------------------------------------
        [Test]
        public void TEST_ValueEditorGUI_MValue_기존컬렉션구성유지()
        {
            var owner = ScriptableObject.CreateInstance<MValueOwner>();
            try
            {
                var property = new SerializedObject(owner).FindProperty("Value");
                var root = new MValueDrawer().CreatePropertyGUI(property);

                Assert.AreEqual(2, root.childCount);
                Assert.IsNotNull(root.Q<Toggle>());
                Assert.IsNotNull(root.Q<CollectionEditorView>());

                var row = root.Q<VisualElement>("value-base-row");
                Assert.AreEqual(5, row.childCount);
                Assert.IsTrue(row.ElementAt(1) is IntegerField);
                Assert.AreEqual("→", (row.ElementAt(2) as Label)?.text);
                Assert.IsFalse(row.ElementAt(3).enabledSelf);
                Assert.IsTrue(row.ElementAt(4) is Toggle);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(owner);
            }
        }

    #endregion

    #region G-3: RangeValue 독립 편집 구성

        // ------------------------------------------------------------
        /// <summary>
        /// RangeValue는 Base와 Range 두 행에 각각 편집 토글을 제공한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_ValueEditorGUI_RangeValue_기존독립편집구성유지()
        {
            var owner = ScriptableObject.CreateInstance<RangeValueOwner>();
            try
            {
                var property = new SerializedObject(owner).FindProperty("Value");
                var root = new RangeValueDrawer().CreatePropertyGUI(property);

                Assert.AreEqual(2, root.childCount);
                Assert.AreEqual(2, root.Query<Toggle>().ToList().Count);

                var baseRow = root.Q<VisualElement>("value-base-row");
                var rangeRow = root.Q<VisualElement>("value-range-row");
                Assert.AreEqual(3, baseRow.childCount);
                Assert.AreEqual(5, rangeRow.childCount);
                Assert.AreEqual("Range", (rangeRow.ElementAt(1) as Label)?.text);
                Assert.IsTrue(rangeRow.ElementAt(2) is IntegerField);
                Assert.IsTrue(rangeRow.ElementAt(3) is IntegerField);
                Assert.IsTrue(rangeRow.ElementAt(4) is Toggle);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(owner);
            }
        }

    #endregion

    }
}
