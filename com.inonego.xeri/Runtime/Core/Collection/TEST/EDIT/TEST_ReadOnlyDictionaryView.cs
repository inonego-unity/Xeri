/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TEST_ReadOnlyDictionaryView.cs
수정일 : 2026-11-25

# 설명
ReadOnlyDictionaryView의 원본 딕셔너리 변경 반영, 외부 수정 차단, 구체 열거자와 할당 계약을 검증한다.

# 테스트 구성
 V: 읽기 전용 Live View
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

using NUnit;
using NUnit.Framework;

namespace inonego.Xeri.TEST.Core._Collection
{
    // ============================================================
    /// <summary>
    /// 읽기 전용 Dictionary View의 런타임 조회 계약을 확인한다.
    /// </summary>
    // ============================================================
    public class TEST_ReadOnlyDictionaryView
    {

    #region V: 읽기 전용 Live View

        // ------------------------------------------------------------
        /// <summary>
        /// 원본 변경을 복사 없이 조회하고 수정 인터페이스를 숨긴다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_ReadOnlyDictionaryView_원본변경을반영하고_수정권한을노출하지않음()
        {
            var source = new Dictionary<string, int>
            {
                { "a", 1 }
            };
            var view = new ReadOnlyDictionaryView<string, int>(source);

            Assert.AreEqual(1, view.Count);
            Assert.AreEqual(1, view["a"]);
            Assert.IsTrue(view.ContainsKey("a"));
            Assert.IsFalse(((object)view) is IDictionary<string, int>);
            Assert.IsFalse(((object)view) is Dictionary<string, int>);

            source["b"] = 2;
            Assert.AreEqual(2, view.Count);
            Assert.IsTrue(view.TryGetValue("b", out var value));
            Assert.AreEqual(2, value);

            source.Remove("a");
            Assert.IsFalse(view.ContainsKey("a"));
            Assert.Throws<KeyNotFoundException>(() => { var _ = view["a"]; });

            source.Clear();
            Assert.AreEqual(0, view.Count);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Keys와 Values는 원본을 그대로 보여 주며 쓰기 호출은 거절한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_ReadOnlyDictionaryView_Keys와Values는읽기전용()
        {
            var source = new Dictionary<string, int>
            {
                { "a", 1 },
                { "b", 2 }
            };
            var view = new ReadOnlyDictionaryView<string, int>(source);

            var expectedKeys = new List<string>
            {
                "a",
                "b"
            };
            var expectedValues = new List<int>
            {
                1,
                2
            };

            CollectionAssert.AreEquivalent(expectedKeys, view.Keys);
            CollectionAssert.AreEquivalent(expectedValues, view.Values);

            source["c"] = 3;
            Assert.AreEqual(3, view.Keys.Count);

            Assert.Throws<NotSupportedException>(() => ((ICollection<string>)view.Keys).Add("x"));
            Assert.Throws<NotSupportedException>(() => ((ICollection<int>)view.Values).Clear());
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 구체 열거는 struct Enumerator로 모든 항목을 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_ReadOnlyDictionaryView_구체열거는_struct열거자로모든항목반환()
        {
            var source = new Dictionary<string, int>
            {
                { "a", 1 },
                { "b", 2 },
                { "c", 3 }
            };
            var view = new ReadOnlyDictionaryView<string, int>(source);
            Dictionary<string, int>.Enumerator enumerator = view.GetEnumerator();

            var values = new Dictionary<string, int>();

            while (enumerator.MoveNext())
            {
                values.Add(enumerator.Current.Key, enumerator.Current.Value);
            }

            CollectionAssert.AreEquivalent(source, values);
            CollectionAssert.AreEquivalent(source, (IEnumerable<KeyValuePair<string, int>>)view);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> default View는 빈 딕셔너리이고 null 원본은 허용하지 않으며
        /// <br/> var로 받은 foreach는 할당하지 않는다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_ReadOnlyDictionaryView_default는빈딕셔너리이고_Null거절_foreach는할당하지않음()
        {
            var empty = default(ReadOnlyDictionaryView<int, int>);
            var source = new Dictionary<int, int>
            {
                { 1, 10 },
                { 2, 20 }
            };
            var view = new ReadOnlyDictionaryView<int, int>(source);
            var sum = 0;

            Assert.AreEqual(0, empty.Count);
            Assert.IsFalse(empty.ContainsKey(1));
            Assert.IsFalse(empty.TryGetValue(1, out _));
            Assert.Throws<ArgumentNullException>
            (
                () => new ReadOnlyDictionaryView<int, int>(null)
            );

            // 계측이 실제로 할당을 잡는지 먼저 확인한다.
            var leaked = new List<object>();
            Assert.That(() => leaked.Add(new object()), new UnityEngine.TestTools.Constraints.AllocatingGCMemoryConstraint());

            foreach (var pair in view)
            {
                sum += pair.Value;
            }

            Assert.AreEqual(30, sum);

            Assert.That
            (
                () =>
                {
                    foreach (var pair in view)
                    {
                        sum += pair.Value;
                    }
                },
                UnityEngine.TestTools.Constraints.ConstraintExtensions.AllocatingGCMemory(Is.Not)
            );
        }

    #endregion

    }
}
