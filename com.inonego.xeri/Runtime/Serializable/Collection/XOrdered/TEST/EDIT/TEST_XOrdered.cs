/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TEST_XOrdered.cs
수정일 : 2026-09-18

# 설명
XOrdered<TOrder, TValue>와 XOrdered<TOrder, TKey, TValue>의 현재 공개 계약을 검증한다.
정렬, 동일 Order 안정성, Entry 노출, Key lookup, 다중 등록과 직렬화를 다룬다.

# 테스트 구성
 U: Unkeyed XOrdered 기본 계약
 K: Keyed XOrdered 기본 계약
 S: Unity JSON 직렬화 계약
 P: 대량 데이터 정렬 및 lookup 일관성
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
    public class TEST_XOrdered
    {

    #region 테스트 타입

        // ======================================================================
        /// <summary>
        /// Comparer&lt;TOrder&gt;.Default의 enum 지원을 검증하는 Order.
        /// </summary>
        // ======================================================================
        private enum TestOrder
        {
            Early = -10,
            Middle = 0,
            Late = 10,
        }

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

    #region U-1: Add 정렬과 동일 Order 안정 삽입

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

    #region U-2: Remove / Contains / IndexOf / Clear

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

    #region U-3: Entry / IReadOnlyXOrdered / Enumerator

        [Test]
        public void TEST_XOrdered_Entry_Deconstruct_IReadOnlyXOrdered_Enumerator()
        {
            // Arrange
            var ordered = new XOrdered<int, TestElement>();

            ordered.Add(20, new TestElement("B"));
            ordered.Add(10, new TestElement("A"));

            // Act
            IReadOnlyXOrdered<int, TestElement> readOnly = ordered;
            var (order, value) = readOnly[0];
            var enumerator = ordered.GetEnumerator();

            // Assert
            Assert.AreEqual(2, readOnly.Count);
            Assert.IsTrue(readOnly.Contains(new TestElement("A")));
            Assert.AreEqual(0, readOnly.IndexOf(new TestElement("A")));
            Assert.AreEqual(10, order);
            Assert.AreEqual("A", value.Name);
            Assert.AreEqual
            (
                typeof(List<XOrdered<int, TestElement>.Entry>.Enumerator),
                enumerator.GetType()
            );
        }

    #endregion

    #region U-4: Enum Order

        [Test]
        public void TEST_XOrdered_EnumOrder_ComparerDefault_지원()
        {
            // Arrange
            var ordered = new XOrdered<TestOrder, TestElement>();

            // Act
            ordered.Add(TestOrder.Late, new TestElement("Late"));
            ordered.Add(TestOrder.Early, new TestElement("Early"));
            ordered.Add(TestOrder.Middle, new TestElement("Middle"));

            // Assert
            Assert.AreEqual(TestOrder.Early, ordered[0].Order);
            Assert.AreEqual(TestOrder.Middle, ordered[1].Order);
            Assert.AreEqual(TestOrder.Late, ordered[2].Order);
        }

    #endregion

    #region U-5: 대량 데이터 정렬

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

    #region K-1: Add / Key 조회 / Entry identity

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

    #region K-2: 동일 Order 안정 순서와 Entry Deconstruct

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

    #region K-3: 중복 Key 예외

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

    #region K-4: 동일 Value 다중 등록과 정확한 Remove

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

    #region K-5: Clear와 재사용

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

    #region K-6: IReadOnlyXOrdered와 구체 Enumerator

        [Test]
        public void TEST_XOrdered_Keyed_IReadOnlyXOrdered_전체Entry_Enumerator()
        {
            // Arrange
            var ordered = new XOrdered<int, string, TestElement>();

            ordered.Add(20, "b", new TestElement("B"));
            ordered.Add(10, "a", new TestElement("A"));

            // Act
            IReadOnlyXOrdered<int, string, TestElement> readOnly = ordered;
            var enumerator = ordered.GetEnumerator();
            var (order, key, value) = readOnly[0];

            // Assert
            Assert.AreEqual(2, readOnly.Count);
            Assert.IsTrue(readOnly.ContainsKey("a"));
            Assert.IsTrue(readOnly.TryGetValue("a", out var foundValue));
            Assert.AreEqual("A", foundValue.Name);
            Assert.IsTrue(readOnly.TryGetEntry("a", out var foundEntry));
            Assert.AreSame(readOnly[0], foundEntry);
            Assert.AreEqual(10, order);
            Assert.AreEqual("a", key);
            Assert.AreEqual("A", value.Name);
            Assert.AreEqual
            (
                typeof(List<XOrdered<int, string, TestElement>.Entry>.Enumerator),
                enumerator.GetType()
            );
        }

    #endregion

    #region S-1: Unkeyed JSON 직렬화

        [Test]
        public void TEST_XOrdered_JSON_직렬화_순서보존_재사용()
        {
            // Arrange
            var original = new XOrdered<int, TestElement>();

            original.Add(30, new TestElement("C"));
            original.Add(10, new TestElement("A"));
            original.Add(20, new TestElement("B"));

            // Act
            var json = JsonUtility.ToJson(original);
            var restored = JsonUtility.FromJson<XOrdered<int, TestElement>>(json);

            // Assert
            Assert.AreEqual(3, restored.Count);
            Assert.AreEqual(10, restored[0].Order);
            Assert.AreEqual("A", restored[0].Value.Name);
            Assert.AreEqual(20, restored[1].Order);
            Assert.AreEqual("B", restored[1].Value.Name);
            Assert.AreEqual(30, restored[2].Order);
            Assert.AreEqual("C", restored[2].Value.Name);

            restored.Add(15, new TestElement("Inserted"));

            Assert.AreEqual(4, restored.Count);
            Assert.AreEqual(15, restored[1].Order);
            Assert.AreEqual("Inserted", restored[1].Value.Name);
        }

    #endregion

    #region S-2: Keyed JSON 직렬화와 Lookup 재구성

        [Test]
        public void TEST_XOrdered_Keyed_JSON_직렬화_Lookup_Lazy재구성()
        {
            // Arrange
            var original = new XOrdered<int, string, TestElement>();

            original.Add(30, "c", new TestElement("C"));
            original.Add(10, "a", new TestElement("A"));
            original.Add(20, "b", new TestElement("B"));

            // Act
            var json = JsonUtility.ToJson(original);
            var restored = JsonUtility.FromJson<XOrdered<int, string, TestElement>>(json);

            var foundValue = restored.TryGetValue("b", out var value);
            var foundEntry = restored.TryGetEntry("b", out var entry);

            // Assert
            Assert.AreEqual(3, restored.Count);
            Assert.IsTrue(restored.ContainsKey("a"));
            Assert.IsTrue(restored.ContainsKey("b"));
            Assert.IsTrue(restored.ContainsKey("c"));
            Assert.IsTrue(foundValue);
            Assert.AreEqual("B", value.Name);
            Assert.IsTrue(foundEntry);
            Assert.AreSame(restored[1], entry);

            restored.Add(15, "inserted", new TestElement("Inserted"));

            Assert.AreEqual("inserted", restored[1].Key);
            Assert.IsTrue(restored.ContainsKey("inserted"));
        }

    #endregion

    #region S-3: SerializeReference 공유 참조

        [Test]
        public void TEST_XOrdered_Keyed_JSON_SerializeReference_공유참조_보존()
        {
            // Arrange
            var shared = new TestElement("Shared");
            var original = new XOrdered<int, string, TestElement>();

            original.Add(10, "a", shared);
            original.Add(20, "b", shared);

            // Act
            var json = JsonUtility.ToJson(original);
            var restored = JsonUtility.FromJson<XOrdered<int, string, TestElement>>(json);

            // Assert
            Assert.AreEqual(2, restored.Count);
            Assert.AreSame(restored[0].Value, restored[1].Value);
            Assert.AreEqual("Shared", restored[0].Value.Name);
            Assert.IsTrue(restored.TryGetValue("a", out var first));
            Assert.IsTrue(restored.TryGetValue("b", out var second));
            Assert.AreSame(first, second);
        }

    #endregion

    #region P-1: 대량 데이터 정렬과 Lookup 일관성

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
