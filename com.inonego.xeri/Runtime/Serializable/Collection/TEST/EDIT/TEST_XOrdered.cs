/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TEST_XOrdered.cs
수정일 : 2026-11-24

# 설명
XOrdered의 기본 정렬·조회와 키 기반 고유성·안정 정렬, Key 인덱스, JSON 직렬화와 관리 참조 공유를 함께 검증한다.

# 테스트 구성
 U: 키 없는 XOrdered 정렬과 조회
 K: 키 있는 XOrdered 고유성과 안정 정렬
 P: 대량 데이터 정렬과 조회 일관성
 I: 키 있는 XOrdered의 Key 인덱스 일관성, 중복·null Key, 직렬화 복원
 S: Unity JSON 직렬화 계약과 관리 참조 공유
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

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
        public void TEST_XOrdered_Keyed_Add_정렬_Key조회_Entry값()
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
            Assert.AreEqual(ordered[1].Order, entry.Order);
            Assert.AreEqual(ordered[1].Key, entry.Key);
            Assert.AreSame(ordered[1].Value, entry.Value);
            Assert.AreEqual(20, entry.Order);
            Assert.AreEqual("b", entry.Key);
            Assert.AreSame(value, entry.Value);

            Assert.IsFalse(ordered.TryGetValue("missing", out var missingValue));
            Assert.IsNull(missingValue);
            Assert.IsFalse(ordered.TryGetEntry("missing", out var missingEntry));
            Assert.IsNull(missingEntry.Value);
            Assert.IsNull(missingEntry.Key);
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

    #region I-1: Key 인덱스 경로의 조회·추가·삭제

        // ------------------------------------------------------------
        /// <summary>
        /// 항목이 많아도 조회·추가·삭제 결과가 일관된다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_XOrdered_Index_많은항목_조회추가삭제_일관성()
        {
            var ordered = new XOrdered<int, string, TestElement>();

            for (var number = 0; number < 20; number++)
            {
                ordered.Add(number, $"key-{number}", new TestElement($"value-{number}"));
            }

            for (var number = 0; number < 20; number++)
            {
                Assert.IsTrue(ordered.TryGetValue($"key-{number}", out var value));
                Assert.AreEqual($"value-{number}", value.Name);
            }

            Assert.IsTrue(ordered.Remove("key-3"));
            Assert.IsTrue(ordered.Remove("key-10"));
            Assert.IsFalse(ordered.Remove("key-3"));
            Assert.IsFalse(ordered.ContainsKey("key-3"));
            Assert.IsFalse(ordered.ContainsKey("key-10"));
            Assert.IsTrue(ordered.ContainsKey("key-4"));
            Assert.AreEqual(18, ordered.Count);

            ordered.Add(10, "key-new", new TestElement("new"));

            Assert.IsTrue(ordered.TryGetValue("key-new", out var added));
            Assert.AreEqual("new", added.Name);
            Assert.Throws<ArgumentException>(() => ordered.Add(0, "key-new", new TestElement("duplicate")));

            for (var position = 1; position < ordered.Count; position++)
            {
                Assert.LessOrEqual(ordered[position - 1].Order, ordered[position].Order);
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 같은 Order의 항목 중 Key가 가리키는 항목만 삭제한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_XOrdered_Index_동일Order_Key로_정확히삭제()
        {
            var ordered = new XOrdered<int, string, TestElement>();

            for (var number = 0; number < 12; number++)
            {
                ordered.Add(7, $"key-{number}", new TestElement($"value-{number}"));
            }

            Assert.IsTrue(ordered.Remove("key-5"));
            Assert.AreEqual(11, ordered.Count);
            Assert.IsFalse(ordered.ContainsKey("key-5"));
            Assert.IsTrue(ordered.ContainsKey("key-4"));
            Assert.IsTrue(ordered.ContainsKey("key-6"));
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 전체 삭제 뒤 같은 Key를 다시 쓸 수 있다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_XOrdered_Index_전체삭제뒤_재사용()
        {
            var ordered = new XOrdered<int, string, TestElement>();

            for (var number = 0; number < 12; number++)
            {
                ordered.Add(number, $"key-{number}", new TestElement($"value-{number}"));
            }

            Assert.IsTrue(ordered.ContainsKey("key-0"));

            ordered.Clear();

            Assert.IsFalse(ordered.ContainsKey("key-0"));

            ordered.Add(0, "key-0", new TestElement("again"));

            Assert.IsTrue(ordered.TryGetValue("key-0", out var value));
            Assert.AreEqual("again", value.Name);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 없는 Key 조회는 기본 항목을 돌려준다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_XOrdered_Index_없는Key_기본항목()
        {
            var ordered = new XOrdered<int, string, TestElement>();

            for (var number = 0; number < 12; number++)
            {
                ordered.Add(number, $"key-{number}", new TestElement($"value-{number}"));
            }

            Assert.IsFalse(ordered.TryGetEntry("missing", out var entry));
            Assert.IsNull(entry.Value);
            Assert.IsNull(entry.Key);
        }

    #endregion

    #region I-2: 중복 Key와 null Key

        // ------------------------------------------------------------
        /// <summary>
        /// 직렬화 데이터의 중복 Key는 앞쪽 항목을 쓰고 삭제하면 뒤쪽이 드러난다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_XOrdered_Index_중복Key_앞쪽사용_삭제후_뒤쪽노출()
        {
            var ordered = new XOrdered<int, string, TestElement>();

            for (var number = 0; number < 10; number++)
            {
                ordered.Add(10 + number, $"key-{number}", new TestElement($"value-{number}"));
            }

            // 직렬화 데이터가 Editor에서 중복 Key를 갖게 된 상황을 만든다.
            var entries = (List<XOrderedEntry<int, string, TestElement>>)typeof(XOrderedBase<int, TestElement, XOrderedEntry<int, string, TestElement>>)
                .GetField("entries", BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(ordered);

            entries.Add(new XOrderedEntry<int, string, TestElement>(100, "key-0", new TestElement("duplicate")));
            ((ISerializedCollectionNormalizer)ordered).NormalizeSerializedState();

            Assert.IsTrue(ordered.TryGetValue("key-0", out var first));
            Assert.AreEqual("value-0", first.Name);

            Assert.IsTrue(ordered.Remove("key-0"));
            Assert.IsTrue(ordered.TryGetValue("key-0", out var second));
            Assert.AreEqual("duplicate", second.Name);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// null Key 항목이 있어도 다른 Key 조회가 정상이다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_XOrdered_Index_null키_포함_조회()
        {
            var ordered = new XOrdered<int, string, TestElement>();

            for (var number = 0; number < 12; number++)
            {
                ordered.Add(number, $"key-{number}", new TestElement($"value-{number}"));
            }

            ordered.Add(50, null, new TestElement("empty"));

            Assert.IsTrue(ordered.ContainsKey(null));
            Assert.IsTrue(ordered.TryGetValue("key-5", out var value));
            Assert.AreEqual("value-5", value.Name);
            Assert.IsTrue(ordered.Remove(null));
            Assert.IsFalse(ordered.ContainsKey(null));
            Assert.AreEqual(12, ordered.Count);
        }

    #endregion

    #region I-3: 직렬화 복원 뒤 인덱스

        // ------------------------------------------------------------
        /// <summary>
        /// 직렬화에서 복원한 뒤에도 많은 항목의 Key 조회가 정상이다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_XOrdered_Index_직렬화복원뒤_조회()
        {
            var original = new XOrdered<int, string, TestElement>();

            for (var number = 0; number < 20; number++)
            {
                original.Add(number, $"key-{number}", new TestElement($"value-{number}"));
            }

            // 복원 전에 인덱스를 만들어 두어 복원이 무효화하는지 본다.
            Assert.IsTrue(original.ContainsKey("key-19"));

            var json = JsonUtility.ToJson(original);
            var restored = JsonUtility.FromJson<XOrdered<int, string, TestElement>>(json);

            Assert.AreEqual(20, restored.Count);
            Assert.IsTrue(restored.TryGetValue("key-19", out var value));
            Assert.AreEqual("value-19", value.Name);
            Assert.IsFalse(restored.ContainsKey("missing"));
        }

    #endregion

    #region S-1: 키 없는 컬렉션 직렬화

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

    #region S-2: 키 있는 컬렉션 직렬화와 조회 복구

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
            Assert.AreEqual(restored[1].Order, entry.Order);
            Assert.AreEqual(restored[1].Key, entry.Key);
            Assert.AreSame(restored[1].Value, entry.Value);

            restored.Add(15, "inserted", new TestElement("Inserted"));

            Assert.AreEqual("inserted", restored[1].Key);
            Assert.IsTrue(restored.ContainsKey("inserted"));
        }

    #endregion

    #region S-3: 관리 참조 공유 상태 보존

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

    }
}
