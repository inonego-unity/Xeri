/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TEST_XOrdered.cs
수정일 : 2026-10-31

# 설명
XOrdered의 기본 정렬·조회와 키 기반 고유성·안정 정렬을 함께 검증한다.

# 테스트 구성
 U: 키 없는 XOrdered 정렬과 조회
 K: 키 있는 XOrdered 고유성과 안정 정렬
 P: 대량 데이터 정렬과 조회 일관성
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using NUnit;
using NUnit.Framework;

using inonego;
using inonego.Xeri;
using inonego.Xeri.Serializable;

namespace inonego.Xeri.TEST.Serializable._XOrdered
{
    // ============================================================
    /// <summary>
    /// XOrdered 컬렉션의 현재 공개 계약을 검증한다.
    /// </summary>
    // ============================================================
    public partial class TEST_XOrdered
    {

    #region 헬퍼

        // ============================================================
        /// <summary>
        /// 값 동등성과 Unity 직렬화를 함께 검증하는 테스트 값.
        /// </summary>
        // ============================================================
        [Serializable]
        private sealed class TestElement : IEquatable<TestElement>
        {
            public string Name => name;

            [SerializeField]
            private string name;

            public TestElement()
            {
                // NONE
            }

            public TestElement(string name)
            {
                this.name = name;
            }

            public bool Equals(TestElement other)
            {
                if (other == null)
                {
                    return false;
                }

                return name == other.name;
            }

            public override bool Equals(object obj)
            {
                return obj is TestElement other && Equals(other);
            }

            public override int GetHashCode()
            {
                return name?.GetHashCode() ?? 0;
            }
        }

    #endregion

    #region U-1: 추가와 안정 정렬

        [Test]
        public void TEST_XOrdered_Add_오름차순_동일Order_안정삽입()
        {
            // Arrange
            var ordered = new XOrdered<int, TestElement>();

            // Act
            ordered.Add(20, new TestElement("B"));
            ordered.Add(10, new TestElement("A"));
            ordered.Add(20, new TestElement("C"));
            ordered.Add(20, new TestElement("D"));
            ordered.Add(30, new TestElement("E"));

            // Assert
            Assert.AreEqual(5, ordered.Count);
            Assert.AreEqual(10, ordered[0].Order);
            Assert.AreEqual("A", ordered[0].Value.Name);
            Assert.AreEqual(20, ordered[1].Order);
            Assert.AreEqual("B", ordered[1].Value.Name);
            Assert.AreEqual("C", ordered[2].Value.Name);
            Assert.AreEqual("D", ordered[3].Value.Name);
            Assert.AreEqual(30, ordered[4].Order);
            Assert.AreEqual("E", ordered[4].Value.Name);
        }

    #endregion

    #region U-2: 삭제와 항목 조회

        [Test]
        public void TEST_XOrdered_RemoveContainsIndexOf_EqualityComparer_계약()
        {
            // Arrange
            var ordered = new XOrdered<int, TestElement>();
            var second = new TestElement("B");

            ordered.Add(10, new TestElement("A"));
            ordered.Add(20, second);

            // Act & Assert
            Assert.AreEqual(0, ordered.IndexOf(new TestElement("A")));
            Assert.IsTrue(ordered.Contains(new TestElement("A")));
            Assert.IsTrue(ordered.Remove(new TestElement("A")));
            Assert.AreEqual(1, ordered.Count);
            Assert.AreSame(second, ordered[0].Value);
            Assert.IsFalse(ordered.Remove(new TestElement("Missing")));

            ordered.Clear();
            Assert.AreEqual(0, ordered.Count);

            ordered.Add(5, new TestElement("Reused"));

            Assert.AreEqual(1, ordered.Count);
            Assert.AreEqual("Reused", ordered[0].Value.Name);
        }

    #endregion

    #region U-3: 대량 데이터 정렬

        [Test]
        public void TEST_XOrdered_대량데이터_정렬_유지()
        {
            // Arrange
            const int testCount = 128;
            var random = new System.Random(42);
            var ordered = new XOrdered<int, TestElement>();

            // Act
            for (var index = 0; index < testCount; index++)
            {
                ordered.Add
                (
                    random.Next(0, 16),
                    new TestElement($"Value-{index}")
                );
            }

            // Assert
            Assert.AreEqual(testCount, ordered.Count);

            for (var index = 1; index < ordered.Count; index++)
            {
                Assert.LessOrEqual(ordered[index - 1].Order, ordered[index].Order);
            }
        }

    #endregion

    #region K-1: 키 추가와 조회

        [Test]
        public void TEST_XOrdered_Keyed_Add_정렬_Key조회_EntryIdentity()
        {
            // Arrange
            var ordered = new XOrdered<int, string, TestElement>();

            // Act
            ordered.Add(30, "c", new TestElement("C"));
            ordered.Add(10, "a", new TestElement("A"));
            ordered.Add(20, "b", new TestElement("B"));

            var foundValue = ordered.TryGetValue("b", out var value);
            var foundEntry = ordered.TryGetEntry("b", out var entry);

            // Assert
            Assert.AreEqual(3, ordered.Count);
            Assert.AreEqual("a", ordered[0].Key);
            Assert.AreEqual("b", ordered[1].Key);
            Assert.AreEqual("c", ordered[2].Key);
            Assert.IsTrue(ordered.ContainsKey("a"));
            Assert.IsFalse(ordered.ContainsKey("missing"));
            Assert.IsTrue(foundValue);
            Assert.AreEqual("B", value.Name);
            Assert.IsTrue(foundEntry);
            Assert.AreSame(ordered[1], entry);
            Assert.AreEqual(20, entry.Order);
            Assert.AreEqual("b", entry.Key);
            Assert.AreSame(value, entry.Value);

            Assert.IsFalse(ordered.TryGetValue("missing", out var missingValue));
            Assert.IsNull(missingValue);
            Assert.IsFalse(ordered.TryGetEntry("missing", out var missingEntry));
            Assert.IsNull(missingEntry);
        }

    #endregion

    #region K-2: 동일 순서와 항목 분해

        [Test]
        public void TEST_XOrdered_Keyed_동일Order_안정순서_EntryDeconstruct()
        {
            // Arrange
            var ordered = new XOrdered<int, string, TestElement>();

            ordered.Add(10, "a", new TestElement("A"));
            ordered.Add(10, "b", new TestElement("B"));
            ordered.Add(10, "c", new TestElement("C"));

            var keys = new List<string>();
            var names = new List<string>();

            // Act
            foreach (var (order, key, value) in ordered)
            {
                Assert.AreEqual(10, order);
                keys.Add(key);
                names.Add(value.Name);
            }

            // Assert
            CollectionAssert.AreEqual
            (
                new[]
                {
                    "a",
                    "b",
                    "c",
                },
                keys
            );
            CollectionAssert.AreEqual
            (
                new[]
                {
                    "A",
                    "B",
                    "C",
                },
                names
            );
        }

    #endregion

    #region K-3: 중복 키 예외와 상태 보존

        [Test]
        public void TEST_XOrdered_Keyed_중복Key_예외_상태보존()
        {
            // Arrange
            var ordered = new XOrdered<int, string, TestElement>();
            var original = new TestElement("Original");

            ordered.Add(10, "key", original);

            // Act & Assert
            Assert.Throws<ArgumentException>
            (
                () => ordered.Add(20, "key", new TestElement("Duplicate"))
            );

            Assert.AreEqual(1, ordered.Count);
            Assert.IsTrue(ordered.TryGetValue("key", out var value));
            Assert.AreSame(original, value);
            Assert.AreSame(original, ordered[0].Value);
        }

    #endregion

    #region K-4: 동일 값 항목의 정확한 삭제

        [Test]
        public void TEST_XOrdered_Keyed_동일Value_다중등록_Remove_정확한Entry()
        {
            // Arrange
            var shared = new TestElement("Shared");
            var ordered = new XOrdered<int, string, TestElement>();

            ordered.Add(10, "a", shared);
            ordered.Add(20, "b", shared);

            // Act
            var removed = ordered.Remove("a");

            // Assert
            Assert.IsTrue(removed);
            Assert.AreEqual(1, ordered.Count);
            Assert.IsFalse(ordered.ContainsKey("a"));
            Assert.IsTrue(ordered.ContainsKey("b"));
            Assert.IsTrue(ordered.TryGetValue("b", out var remaining));
            Assert.AreSame(shared, remaining);
            Assert.AreEqual("b", ordered[0].Key);
            Assert.AreSame(shared, ordered[0].Value);
        }

    #endregion

    #region K-5: 전체 삭제와 재사용

        [Test]
        public void TEST_XOrdered_Keyed_Clear_Lookup정리_재사용()
        {
            // Arrange
            var ordered = new XOrdered<int, string, TestElement>();

            ordered.Add(10, "a", new TestElement("A"));
            ordered.Add(20, "b", new TestElement("B"));
            Assert.IsTrue(ordered.ContainsKey("a"));

            // Act
            ordered.Clear();

            // Assert
            Assert.AreEqual(0, ordered.Count);
            Assert.IsFalse(ordered.ContainsKey("a"));
            Assert.IsFalse(ordered.ContainsKey("b"));

            ordered.Add(5, "c", new TestElement("C"));

            Assert.AreEqual(1, ordered.Count);
            Assert.IsTrue(ordered.ContainsKey("c"));
            Assert.AreEqual("C", ordered[0].Value.Name);
        }

    #endregion

    #region P-1: 대량 데이터 정렬과 조회 일관성

        [Test]
        public void TEST_XOrdered_Keyed_대량데이터_정렬과Lookup_일관성()
        {
            // Arrange
            const int testCount = 128;
            var random = new System.Random(42);
            var ordered = new XOrdered<int, string, TestElement>();
            var expected = new Dictionary<string, TestElement>();

            // Act
            for (var index = 0; index < testCount; index++)
            {
                var key = $"key-{index}";
                var value = new TestElement($"Value-{index}");
                var order = random.Next(0, 16);

                expected.Add(key, value);
                ordered.Add(order, key, value);
            }

            // Assert
            Assert.AreEqual(testCount, ordered.Count);

            for (var index = 1; index < ordered.Count; index++)
            {
                Assert.LessOrEqual(ordered[index - 1].Order, ordered[index].Order);
            }

            foreach (var pair in expected)
            {
                Assert.IsTrue(ordered.TryGetValue(pair.Key, out var value));
                Assert.AreSame(pair.Value, value);
            }
        }

    #endregion

    }
}
