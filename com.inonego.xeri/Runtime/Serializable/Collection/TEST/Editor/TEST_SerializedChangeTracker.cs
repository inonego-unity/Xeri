/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TEST_SerializedChangeTracker.cs
수정일 : 2026-11-25

# 설명
직렬화 변경 관찰기(SerializedChangeTracker)의 관찰·보고·Undo 무효화 규칙과
변경 비교 기준(SerializedPropertyBaseline)의 해시·스냅샷·없음 상태 비교를 검증한다.
Unity Test Runner (Edit Mode) 에서 실행한다.

# 테스트 구성
 B: 비교 기준 (해시, 관리 참조 스냅샷, 없음)
 T: 관찰과 변경 세대 (기준 잡기, 발견, 보고, 공유, 전파)
 U: Undo 무효화와 없는 대상·경로

# 특이사항, 제약사항
변경 세대는 정적 전역 수열이라 절대값이 아니라 이전 값과의 증감만 비교한다.
다른 Inspector의 변경은 SerializedObject.Update 뒤에 관찰해야 하므로 두 번째 SerializedObject는 쓰기 전에 만든다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEditor;
using UnityEngine;

using NUnit;
using NUnit.Framework;

using inonego;
using inonego.Xeri;
using inonego.Xeri.Serializable;
using inonego.Xeri.Serializable.Editor;

namespace inonego.Xeri.TEST.Serializable._SerializedChangeTracker
{
    // ============================================================
    /// <summary>
    /// 직렬화 변경 관찰기와 비교 기준 테스트.
    /// </summary>
    // ============================================================
    public class TEST_SerializedChangeTracker
    {

    #region 타입과 상태

        [Serializable]
        private sealed class Row
        {
            [SerializeField] internal string Label;
            [SerializeField] internal int Count;
        }

        private sealed class Target : ScriptableObject
        {
            [SerializeField] internal int Number;
            [SerializeField] internal int Other;
            [SerializeField] internal List<Row> Rows = new();
            [SerializeReference] internal Row Reference = new();
        }

        private Target target;
        private SerializedObject serialized;

    #endregion

    #region 픽스처

        // ------------------------------------------------------------
        /// <summary>
        /// 행 하나를 가진 대상과 SerializedObject를 만든다.
        /// </summary>
        // ------------------------------------------------------------
        [SetUp]
        public void SetUp()
        {
            target = ScriptableObject.CreateInstance<Target>();
            target.Rows.Add(NewRow("a", 1));
            serialized = new SerializedObject(target);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// SerializedObject와 대상을 정리한다.
        /// </summary>
        // ------------------------------------------------------------
        [TearDown]
        public void TearDown()
        {
            serialized.Dispose();
            UnityEngine.Object.DestroyImmediate(target);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 라벨과 개수를 가진 행을 만든다.
        /// </summary>
        // ------------------------------------------------------------
        private static Row NewRow(string label, int count)
        {
            var row = new Row();
            row.Label = label;
            row.Count = count;

            return row;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 정수 필드를 쓰고 적용한다.
        /// </summary>
        // ------------------------------------------------------------
        private static void SetInt(SerializedObject serializedObject, string path, int value)
        {
            serializedObject.Update();
            serializedObject.FindProperty(path).intValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

    #endregion

    #region B-1: 해시 비교

        // ------------------------------------------------------------
        /// <summary>
        /// 캡처 직후는 같고 값이 바뀌면 다르다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_SerializedPropertyBaseline_캡처직후는_같고_값이_바뀌면_다름()
        {
            var baseline = new SerializedPropertyBaseline();

            Assert.IsFalse(baseline.IsCaptured);

            baseline.Capture(serialized.FindProperty("Number"));

            Assert.IsTrue(baseline.IsCaptured);
            Assert.IsTrue(baseline.Matches(serialized.FindProperty("Number")));

            SetInt(serialized, "Number", 5);

            Assert.IsFalse(baseline.Matches(serialized.FindProperty("Number")));
        }

    #endregion

    #region B-2: 관리 참조 스냅샷 비교

        // ------------------------------------------------------------
        /// <summary>
        /// SerializeReference 내용이 바뀌면 스냅샷 비교로 다르다고 본다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_SerializedPropertyBaseline_관리참조_내용이_바뀌면_다름()
        {
            var baseline = new SerializedPropertyBaseline();

            baseline.Capture(serialized.FindProperty("Reference"));

            Assert.IsTrue(baseline.Matches(serialized.FindProperty("Reference")));

            SetInt(serialized, "Reference.Count", 9);

            Assert.IsFalse(baseline.Matches(serialized.FindProperty("Reference")));
        }

    #endregion

    #region B-3: 없음 상태

        // ------------------------------------------------------------
        /// <summary>
        /// 계속 없으면 같고, 있다가 없어지거나 없다가 생기면 다르다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_SerializedPropertyBaseline_없음상태_전이만_바뀐것으로_봄()
        {
            var missing = new SerializedPropertyBaseline();
            var present = new SerializedPropertyBaseline();

            missing.Capture(null);
            present.Capture(serialized.FindProperty("Number"));

            Assert.IsTrue(missing.Matches(null));
            Assert.IsFalse(missing.Matches(serialized.FindProperty("Number")));
            Assert.IsFalse(present.Matches(null));
        }

    #endregion

    #region T-1: 관찰

        // ------------------------------------------------------------
        /// <summary>
        /// 첫 관찰은 기준만 잡고 번호를 올리지 않는다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_SerializedChangeTracker_첫관찰은_기준만_잡고_번호를_올리지_않음()
        {
            var before = SerializedChangeTracker.Get(target, "Number");

            Assert.IsFalse(SerializedChangeTracker.Observe(serialized, "Number"));
            Assert.AreEqual(before, SerializedChangeTracker.Get(target, "Number"));
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// 변경이 없으면 반복 관찰해도 번호가 오르지 않고 변경이 있으면 한 번 오른다.
        /// </summary>
        // ----------------------------------------------------------------------
        [Test]
        public void TEST_SerializedChangeTracker_변경이_있을때만_한번_번호가_오름()
        {
            SerializedChangeTracker.Observe(serialized, "Number");
            var before = SerializedChangeTracker.Get(target, "Number");

            Assert.IsFalse(SerializedChangeTracker.Observe(serialized, "Number"));
            Assert.AreEqual(before, SerializedChangeTracker.Get(target, "Number"));

            SetInt(serialized, "Number", 7);

            Assert.IsTrue(SerializedChangeTracker.Observe(serialized, "Number"));

            var after = SerializedChangeTracker.Get(target, "Number");

            Assert.IsTrue(after > before);
            Assert.IsFalse(SerializedChangeTracker.Observe(serialized, "Number"));
            Assert.AreEqual(after, SerializedChangeTracker.Get(target, "Number"));
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 쓴 쪽이 보고하면 같은 변경이 다음 관찰에서 다시 발견되지 않는다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_SerializedChangeTracker_보고한_변경은_다시_발견되지_않음()
        {
            SerializedChangeTracker.Observe(serialized, "Number");
            var before = SerializedChangeTracker.Get(target, "Number");

            SetInt(serialized, "Number", 3);
            SerializedChangeTracker.ReportWrite(serialized, "Number");

            var reported = SerializedChangeTracker.Get(target, "Number");

            Assert.IsTrue(reported > before);
            Assert.IsFalse(SerializedChangeTracker.Observe(serialized, "Number"));
            Assert.AreEqual(reported, SerializedChangeTracker.Get(target, "Number"));
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// 같은 대상을 보는 두 SerializedObject는 한 변경을 한 번만 기록한다.
        /// </summary>
        // ----------------------------------------------------------------------
        [Test]
        public void TEST_SerializedChangeTracker_두_SerializedObject는_한_변경을_한번만_기록()
        {
            using var other = new SerializedObject(target);

            SerializedChangeTracker.Observe(serialized, "Number");
            SerializedChangeTracker.Observe(other, "Number");

            SetInt(serialized, "Number", 11);
            other.Update();

            Assert.IsTrue(SerializedChangeTracker.Observe(other, "Number"));

            var recorded = SerializedChangeTracker.Get(target, "Number");

            Assert.IsFalse(SerializedChangeTracker.Observe(serialized, "Number"));
            Assert.AreEqual(recorded, SerializedChangeTracker.Get(target, "Number"));
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// 변경은 같은 경로의 상·하위 경로 번호를 함께 올리고 관련 없는 경로는 올리지 않는다.
        /// </summary>
        // ----------------------------------------------------------------------
        [Test]
        public void TEST_SerializedChangeTracker_상하위_경로는_함께_오르고_무관한_경로는_그대로()
        {
            const string rows = "Rows";
            const string count = "Rows.Array.data[0].Count";

            SerializedChangeTracker.Observe(serialized, rows);
            SerializedChangeTracker.Observe(serialized, count);
            SerializedChangeTracker.Observe(serialized, "Other");

            var rowsBefore = SerializedChangeTracker.Get(target, rows);
            var otherBefore = SerializedChangeTracker.Get(target, "Other");

            SetInt(serialized, count, 4);

            Assert.IsTrue(SerializedChangeTracker.Observe(serialized, count));

            var rowsAfter = SerializedChangeTracker.Get(target, rows);

            Assert.IsTrue(rowsAfter > rowsBefore);
            Assert.AreEqual(rowsAfter, SerializedChangeTracker.Get(target, count));
            Assert.AreEqual(otherBefore, SerializedChangeTracker.Get(target, "Other"));
        }

    #endregion

    #region U-1: Undo 무효화

        // ------------------------------------------------------------
        /// <summary>
        /// Undo 무효화 뒤에는 이미 관찰한 경로도 새 경로도 이전 번호보다 크다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_SerializedChangeTracker_Undo무효화는_모든_경로를_이전보다_크게_만듦()
        {
            SerializedChangeTracker.Observe(serialized, "Number");
            var before = SerializedChangeTracker.Get(target, "Number");
            var otherBefore = SerializedChangeTracker.Get(target, "Other");

            SerializedChangeTracker.InvalidateAll();

            Assert.IsTrue(SerializedChangeTracker.Get(target, "Number") > before);
            Assert.IsTrue(SerializedChangeTracker.Get(target, "Other") > otherBefore);
            Assert.IsTrue(SerializedChangeTracker.Get(target, "NewPath") > before);
        }

    #endregion

    #region U-2: 없는 대상과 경로

        // ------------------------------------------------------------
        /// <summary>
        /// 없는 경로는 예외 없이 처리하고 계속 없으면 번호를 올리지 않는다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_SerializedChangeTracker_없는경로는_예외없이_번호를_올리지_않음()
        {
            Assert.DoesNotThrow(() => SerializedChangeTracker.Observe(serialized, "Missing"));

            var before = SerializedChangeTracker.Get(target, "Missing");

            Assert.IsFalse(SerializedChangeTracker.Observe(serialized, "Missing"));
            Assert.IsFalse(SerializedChangeTracker.Observe(serialized, "Missing"));
            Assert.AreEqual(before, SerializedChangeTracker.Get(target, "Missing"));
        }

        // ------------------------------------------------------------
        /// <summary>
        /// SerializedObject나 경로가 없으면 관찰하지 않는다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_SerializedChangeTracker_대상이나_경로가_없으면_관찰하지_않음()
        {
            Assert.IsFalse(SerializedChangeTracker.Observe(null, "Number"));
            Assert.IsFalse(SerializedChangeTracker.Observe(serialized, null));
        }

    #endregion

    }
}
