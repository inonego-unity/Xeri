/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TEST_XOrdered.cs
수정일 : 2026-05-08

# 설명
XOrdered<TOrder, TValue> 및 XOrdered<TOrder, TKey, TValue>의 핵심 기능 테스트.
Add / Remove / Contains / Clear / 중복 Order / 인덱서 / Deconstruct / IReadOnlyList / IReadOnlyDictionary / AsKeyed / 직렬화 / 복제.

# 테스트 구성
 E:  XOrdered<TOrder, TValue> 기본 기능 (Add/Remove/Contains/Clear/중복 Order/인덱서/Deconstruct)
 I:  XOrdered<TOrder, TValue> 인터페이스 (IReadOnlyList)
 S:  XOrdered<TOrder, TValue> 직렬화/복제
 P:  XOrdered<TOrder, TValue> 스트레스 (대량 데이터)
 KE: XOrdered<TOrder, TKey, TValue> 기본 기능 (Add/Remove/ContainsKey/TryGetValue/인덱서/AsKeyed/Deconstruct/중복 Key 예외)
 KI: XOrdered<TOrder, TKey, TValue> 인터페이스 (IReadOnlyList / IReadOnlyDictionary)
 KS: XOrdered<TOrder, TKey, TValue> 직렬화/복제 (cross-reference 보존, 중복 Value 단일 복제)
 KP: XOrdered<TOrder, TKey, TValue> 스트레스 (대량 데이터)
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

using NUnit.Framework;

namespace inonego.Xeri.TEST.Serializable._XOrdered
{

    using inonego.Xeri.Serializable;

    // ============================================================
    /// <summary>
    /// XOrdered 컬렉션의 핵심 기능 테스트 클래스.
    /// </summary>
    // ============================================================
    public class TEST_XOrdered
    {

    #region 헬퍼

        // ------------------------------------------------------------
        /// <summary>
        /// 테스트용 Value 클래스.
        /// </summary>
        // ------------------------------------------------------------
        [Serializable]
        private class TestElement : IEquatable<TestElement>
        {
            [SerializeField]
            public string Name;

            public TestElement() {}

            public TestElement(string name)
            {
                Name = name;
            }

            public bool Equals(TestElement other)
            {
                if (other == null) return false;

                return Name == other.Name;
            }

            public override bool Equals(object obj)
            {
                return obj is TestElement other && Equals(other);
            }

            public override int GetHashCode()
            {
                return Name?.GetHashCode() ?? 0;
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> 깊은 복제(IDeepCloneable)를 구현한 테스트용 Value 클래스.
        /// <br/> Clone 시 내용 복사 + 새 인스턴스 생성을 검증하는 데 사용.
        /// </summary>
        // ------------------------------------------------------------
        [Serializable]
        private class TestCloneableElement : IDeepCloneable<TestCloneableElement>
        {
            [SerializeField]
            public string Name;

            public TestCloneableElement() {}

            public TestCloneableElement(string name)
            {
                Name = name;
            }

            public TestCloneableElement @new() => new();

            public void CloneFrom(TestCloneableElement source)
            {
                Name = source.Name;
            }
        }

    #endregion

    #region E-1: 기본 생성

        [Test]
        public void TEST_XOrdered_기본_생성_초기상태()
        {
            var ordered = new XOrdered<int, TestElement>();

            Assert.AreEqual(0, ordered.Count);
        }

    #endregion

    #region E-2: Add / Remove / Contains / Clear / 중복 Order / 인덱서

        [Test]
        public void TEST_XOrdered_AddRemoveContainsClear_통합()
        {
            var ordered = new XOrdered<int, TestElement>();

            // ------------------------------------------------------------
            // 1. Add - 역순 추가 → 오름차순 정렬 확인
            // ------------------------------------------------------------
            ordered.Add(30, new TestElement("Third"));
            ordered.Add(10, new TestElement("First"));
            ordered.Add(20, new TestElement("Second"));

            Assert.AreEqual(3, ordered.Count);
            Assert.AreEqual("First",  ordered[0].Value.Name);
            Assert.AreEqual(10,       ordered[0].Order);
            Assert.AreEqual("Second", ordered[1].Value.Name);
            Assert.AreEqual(20,       ordered[1].Order);
            Assert.AreEqual("Third",  ordered[2].Value.Name);
            Assert.AreEqual(30,       ordered[2].Order);

            // ------------------------------------------------------------
            // 2. Contains
            // ------------------------------------------------------------
            var firstValue = ordered[0].Value;

            Assert.IsTrue(ordered.Contains(firstValue));
            Assert.IsFalse(ordered.Contains(new TestElement("NonExistent")));

            // ------------------------------------------------------------
            // 3. 중복 Order 처리 (Upper Bound)
            // ------------------------------------------------------------
            ordered.Add(10, new TestElement("First-Dup"));

            Assert.AreEqual(4, ordered.Count);
            Assert.AreEqual("First",     ordered[0].Value.Name);
            Assert.AreEqual("First-Dup", ordered[1].Value.Name);
            Assert.AreEqual("Second",    ordered[2].Value.Name);
            Assert.AreEqual("Third",     ordered[3].Value.Name);

            // ------------------------------------------------------------
            // 4. Remove
            // ------------------------------------------------------------
            var secondValue = ordered[2].Value;

            Assert.IsTrue(ordered.Remove(secondValue));
            Assert.AreEqual(3, ordered.Count);
            Assert.AreEqual("First",     ordered[0].Value.Name);
            Assert.AreEqual("First-Dup", ordered[1].Value.Name);
            Assert.AreEqual("Third",     ordered[2].Value.Name);
            Assert.IsFalse(ordered.Remove(secondValue));

            // ------------------------------------------------------------
            // 5. Clear
            // ------------------------------------------------------------
            ordered.Clear();

            Assert.AreEqual(0, ordered.Count);

            // ------------------------------------------------------------
            // 6. Clear 후 재사용
            // ------------------------------------------------------------
            ordered.Add(1, new TestElement("A"));
            ordered.Add(2, new TestElement("B"));

            Assert.AreEqual(2, ordered.Count);
            Assert.AreEqual("A", ordered[0].Value.Name);
            Assert.AreEqual("B", ordered[1].Value.Name);
        }

    #endregion

    #region E-3: foreach + Pair.Deconstruct

        [Test]
        public void TEST_XOrdered_Deconstruct_foreach_및_인덱서()
        {
            var ordered = new XOrdered<int, TestElement>();

            ordered.Add(10, new TestElement("First"));
            ordered.Add(20, new TestElement("Second"));
            ordered.Add(30, new TestElement("Third"));

            // foreach 분해
            var orders = new List<int>();
            var names  = new List<string>();

            foreach (var (order, value) in ordered)
            {
                orders.Add(order);
                names.Add(value.Name);
            }

            Assert.AreEqual(new[] { 10, 20, 30 },                 orders);
            Assert.AreEqual(new[] { "First", "Second", "Third" }, names);

            // 인덱서 결과 분해
            var (firstOrder, firstValue) = ordered[0];

            Assert.AreEqual(10,      firstOrder);
            Assert.AreEqual("First", firstValue.Name);
        }

    #endregion

    #region I-1: IReadOnlyList<Pair>

        [Test]
        public void TEST_XOrdered_IReadOnlyList_구현()
        {
            var ordered = new XOrdered<int, TestElement>();

            ordered.Add(10, new TestElement("First"));
            ordered.Add(20, new TestElement("Second"));
            ordered.Add(30, new TestElement("Third"));

            IReadOnlyList<XOrdered<int, TestElement>.Pair> readOnlyList = ordered;

            Assert.AreEqual(3, readOnlyList.Count);
            Assert.AreEqual("First",  readOnlyList[0].Value.Name);
            Assert.AreEqual(10,       readOnlyList[0].Order);
            Assert.AreEqual("Second", readOnlyList[1].Value.Name);
            Assert.AreEqual(20,       readOnlyList[1].Order);
            Assert.AreEqual("Third",  readOnlyList[2].Value.Name);
            Assert.AreEqual(30,       readOnlyList[2].Order);

            // LINQ
            var firstItem = readOnlyList.First();

            Assert.AreEqual("First", firstItem.Value.Name);
            Assert.AreEqual(10,      firstItem.Order);
        }

    #endregion

    #region S-1: JSON 직렬화

        [Test]
        public void TEST_XOrdered_JSON_직렬화_라운드트립()
        {
            var original = new XOrdered<int, TestElement>();

            original.Add(30, new TestElement("Third"));
            original.Add(10, new TestElement("First"));
            original.Add(20, new TestElement("Second"));

            string json         = JsonUtility.ToJson(original);
            var    deserialized = JsonUtility.FromJson<XOrdered<int, TestElement>>(json);

            Assert.AreEqual(original.Count, deserialized.Count);

            for (int i = 0; i < original.Count; i++)
            {
                Assert.AreEqual(original[i].Value.Name, deserialized[i].Value.Name);
                Assert.AreEqual(original[i].Order,      deserialized[i].Order);
            }

            deserialized.Add(0, new TestElement("Zero"));

            Assert.AreEqual(4,      deserialized.Count);
            Assert.AreEqual("Zero", deserialized[0].Value.Name);
            Assert.AreEqual(0,      deserialized[0].Order);
        }

    #endregion

    #region S-2: Clone

        [Test]
        public void TEST_XOrdered_Clone_독립_인스턴스()
        {
            var original = new XOrdered<int, TestElement>();

            original.Add(10, new TestElement("First"));
            original.Add(20, new TestElement("Second"));
            original.Add(30, new TestElement("Third"));

            var cloned = original.Clone();

            Assert.AreEqual(original.Count, cloned.Count);

            for (int i = 0; i < original.Count; i++)
            {
                Assert.AreEqual(original[i].Value.Name, cloned[i].Value.Name);
                Assert.AreEqual(original[i].Order,      cloned[i].Order);
            }

            cloned.Add(40, new TestElement("Fourth"));

            Assert.AreEqual(4, cloned.Count);
            Assert.AreEqual(3, original.Count);
        }

    #endregion

    #region P-1: 대량 데이터 스트레스

        [Test]
        public void TEST_XOrdered_대량_데이터_스트레스()
        {
            var ordered = new XOrdered<int, TestElement>();
            const int testCount = 100;

            // 1. 역순 Add → 오름차순 정렬
            for (int i = testCount; i > 0; i--)
            {
                ordered.Add(i, new TestElement($"Element-{i}"));
            }

            Assert.AreEqual(testCount, ordered.Count);

            for (int i = 0; i < testCount; i++)
            {
                int expectedOrder = i + 1;

                Assert.AreEqual($"Element-{expectedOrder}", ordered[i].Value.Name);
                Assert.AreEqual(expectedOrder,              ordered[i].Order);
            }

            // 2. 랜덤 Add → 정렬 확인
            ordered.Clear();

            var random      = new System.Random(42);
            var addedOrders = new List<int>();

            for (int i = 0; i < testCount; i++)
            {
                int order = random.Next(1, 1000);
                addedOrders.Add(order);
                ordered.Add(order, new TestElement($"Random-{i}"));
            }

            addedOrders.Sort();

            for (int i = 0; i < testCount; i++)
            {
                Assert.IsNotNull(ordered[i].Value);
                Assert.AreEqual(addedOrders[i], ordered[i].Order);
            }

            // 3. Add/Remove 혼합
            ordered.Clear();

            var values = new List<TestElement>();

            for (int i = 0; i < 50; i++)
            {
                var v = new TestElement($"Mix-{i}");
                values.Add(v);
                ordered.Add(i * 2, v);
            }

            Assert.AreEqual(50, ordered.Count);

            for (int i = 0; i < 25; i++)
            {
                ordered.Remove(values[i * 2]);
            }

            Assert.AreEqual(25, ordered.Count);

            for (int i = 50; i < 100; i++)
            {
                ordered.Add(i * 2, new TestElement($"Mix-{i}"));
            }

            Assert.AreEqual(75, ordered.Count);

            int count     = 0;
            int prevOrder = -1;

            foreach (var (order, value) in ordered)
            {
                Assert.IsNotNull(value);
                Assert.GreaterOrEqual(order, prevOrder);

                prevOrder = order;
                count++;
            }

            Assert.AreEqual(75, count);
        }

    #endregion

    #region KE-1: Keyed 기본 생성

        [Test]
        public void TEST_XOrdered_Keyed_기본_생성_초기상태()
        {
            var ordered = new XOrdered<int, string, TestElement>();

            Assert.AreEqual(0, ordered.Count);
        }

    #endregion

    #region KE-2: Add / Remove / ContainsKey / TryGetValue / 인덱서 / Clear / 중복 Key 예외

        [Test]
        public void TEST_XOrdered_Keyed_AddRemoveContainsClear_통합()
        {
            var ordered = new XOrdered<int, string, TestElement>();

            // 1. Add (Order, Key, Value)
            ordered.Add(30, "key3", new TestElement("Third"));
            ordered.Add(10, "key1", new TestElement("First"));
            ordered.Add(20, "key2", new TestElement("Second"));

            Assert.AreEqual(3, ordered.Count);
            Assert.AreEqual("First",  ordered[0].Value.Name);
            Assert.AreEqual(10,       ordered[0].Order);
            Assert.AreEqual("Second", ordered[1].Value.Name);
            Assert.AreEqual(20,       ordered[1].Order);
            Assert.AreEqual("Third",  ordered[2].Value.Name);
            Assert.AreEqual(30,       ordered[2].Order);

            // 2. Key 인덱서 (직접 API — 없으면 null)
            Assert.AreEqual("First",  ordered["key1"].Name);
            Assert.AreEqual("Second", ordered["key2"].Name);
            Assert.AreEqual("Third",  ordered["key3"].Name);
            Assert.IsNull(ordered["nonexistent"]);

            // 3. TryGetValue
            Assert.IsTrue(ordered.TryGetValue("key1", out var value1));
            Assert.AreEqual("First", value1.Name);

            Assert.IsFalse(ordered.TryGetValue("nonexistent", out var value2));
            Assert.IsNull(value2);

            // 4. ContainsKey
            Assert.IsTrue(ordered.ContainsKey("key1"));
            Assert.IsTrue(ordered.ContainsKey("key2"));
            Assert.IsTrue(ordered.ContainsKey("key3"));
            Assert.IsFalse(ordered.ContainsKey("nonexistent"));

            // 5. 중복 Key 예외
            Assert.Throws<ArgumentException>(() =>
            {
                ordered.Add(50, "key1", new TestElement("Duplicate"));
            });

            Assert.AreEqual(3, ordered.Count);

            // 6. 중복 Order 처리
            ordered.Add(10, "key4", new TestElement("First-Dup"));

            Assert.AreEqual(4, ordered.Count);
            Assert.AreEqual("First",     ordered[0].Value.Name);
            Assert.AreEqual("First-Dup", ordered[1].Value.Name);
            Assert.AreEqual("Second",    ordered[2].Value.Name);
            Assert.AreEqual("Third",     ordered[3].Value.Name);

            // 7. Remove (by Key)
            Assert.IsTrue(ordered.Remove("key2"));
            Assert.AreEqual(3, ordered.Count);
            Assert.IsFalse(ordered.ContainsKey("key2"));
            Assert.IsFalse(ordered.Remove("nonexistent"));

            // 8. Clear
            ordered.Clear();

            Assert.AreEqual(0, ordered.Count);
            Assert.IsFalse(ordered.ContainsKey("key1"));

            // 9. Clear 후 재사용
            ordered.Add(1, "keyA", new TestElement("A"));
            ordered.Add(2, "keyB", new TestElement("B"));

            Assert.AreEqual(2, ordered.Count);
            Assert.AreEqual("A", ordered[0].Value.Name);
            Assert.AreEqual("B", ordered[1].Value.Name);
            Assert.IsTrue(ordered.ContainsKey("keyA"));
            Assert.IsTrue(ordered.ContainsKey("keyB"));
        }

    #endregion

    #region KE-3: AsKeyed 순회

        [Test]
        public void TEST_XOrdered_Keyed_AsKeyed_정렬순서_순회()
        {
            var ordered = new XOrdered<int, string, TestElement>();

            ordered.Add(30, "key3", new TestElement("Third"));
            ordered.Add(10, "key1", new TestElement("First"));
            ordered.Add(20, "key2", new TestElement("Second"));

            // AsKeyed - 정렬 순서로 (Order, Key, Value) Deconstruct
            var keyedOrders = new List<int>();
            var keyedKeys   = new List<string>();
            var keyedNames  = new List<string>();

            foreach (var (order, key, value) in ordered.AsKeyed())
            {
                keyedOrders.Add(order);
                keyedKeys.Add(key);
                keyedNames.Add(value.Name);
            }

            Assert.AreEqual(new[] { 10, 20, 30 },                 keyedOrders);
            Assert.AreEqual(new[] { "key1", "key2", "key3" },     keyedKeys);
            Assert.AreEqual(new[] { "First", "Second", "Third" }, keyedNames);
        }

    #endregion

    #region KE-4: foreach + Pair.Deconstruct

        [Test]
        public void TEST_XOrdered_Keyed_Deconstruct_foreach()
        {
            var ordered = new XOrdered<int, string, TestElement>();

            ordered.Add(10, "key1", new TestElement("First"));
            ordered.Add(20, "key2", new TestElement("Second"));
            ordered.Add(30, "key3", new TestElement("Third"));

            var orders = new List<int>();
            var names  = new List<string>();

            foreach (var (order, value) in ordered)
            {
                orders.Add(order);
                names.Add(value.Name);
            }

            Assert.AreEqual(new[] { 10, 20, 30 },                 orders);
            Assert.AreEqual(new[] { "First", "Second", "Third" }, names);
        }

    #endregion

    #region KI-1: IReadOnlyList<Pair>

        [Test]
        public void TEST_XOrdered_Keyed_IReadOnlyList_구현()
        {
            var ordered = new XOrdered<int, string, TestElement>();

            ordered.Add(10, "key1", new TestElement("First"));
            ordered.Add(20, "key2", new TestElement("Second"));
            ordered.Add(30, "key3", new TestElement("Third"));

            IReadOnlyList<XOrderedBase<int, TestElement>.Pair> readOnlyList = ordered;

            Assert.AreEqual(3, readOnlyList.Count);
            Assert.AreEqual("First",  readOnlyList[0].Value.Name);
            Assert.AreEqual(10,       readOnlyList[0].Order);
            Assert.AreEqual("Second", readOnlyList[1].Value.Name);
            Assert.AreEqual(20,       readOnlyList[1].Order);
            Assert.AreEqual("Third",  readOnlyList[2].Value.Name);
            Assert.AreEqual(30,       readOnlyList[2].Order);
        }

    #endregion

    #region KI-2: IReadOnlyDictionary<TKey, TValue>

        [Test]
        public void TEST_XOrdered_Keyed_IReadOnlyDictionary_구현()
        {
            var ordered = new XOrdered<int, string, TestElement>();

            ordered.Add(10, "key1", new TestElement("First"));
            ordered.Add(20, "key2", new TestElement("Second"));
            ordered.Add(30, "key3", new TestElement("Third"));

            IReadOnlyDictionary<string, TestElement> dict = ordered;

            // Count
            Assert.AreEqual(3, dict.Count);

            // 인덱서 (BCL 계약: 키 없으면 throw)
            Assert.AreEqual("First",  dict["key1"].Name);
            Assert.AreEqual("Second", dict["key2"].Name);
            Assert.AreEqual("Third",  dict["key3"].Name);

            Assert.Throws<KeyNotFoundException>(() =>
            {
                var _ = dict["nonexistent"];
            });

            // 직접 API의 this[key]는 여전히 null 반환 (대조)
            Assert.IsNull(ordered["nonexistent"]);

            // ContainsKey / TryGetValue (인터페이스 경로)
            Assert.IsTrue(dict.ContainsKey("key1"));
            Assert.IsFalse(dict.ContainsKey("nonexistent"));

            Assert.IsTrue(dict.TryGetValue("key2", out var v));
            Assert.AreEqual("Second", v.Name);

            // Keys / Values
            var keys   = new List<string>(dict.Keys);
            var values = new List<TestElement>(dict.Values);

            Assert.AreEqual(3, keys.Count);
            Assert.Contains("key1", keys);
            Assert.Contains("key2", keys);
            Assert.Contains("key3", keys);

            Assert.AreEqual(3, values.Count);

            // GetEnumerator → KeyValuePair (dict 순서, sort 순서 아님)
            var pairs = new List<KeyValuePair<string, TestElement>>();

            foreach (var kvp in dict)
            {
                pairs.Add(kvp);
            }

            Assert.AreEqual(3, pairs.Count);
            var pairKeys = pairs.Select(p => p.Key).ToList();
            Assert.Contains("key1", pairKeys);
            Assert.Contains("key2", pairKeys);
            Assert.Contains("key3", pairKeys);
        }

    #endregion

    #region KS-1: JSON 직렬화

        [Test]
        public void TEST_XOrdered_Keyed_JSON_직렬화_라운드트립()
        {
            var original = new XOrdered<int, string, TestElement>();

            original.Add(30, "key3", new TestElement("Third"));
            original.Add(10, "key1", new TestElement("First"));
            original.Add(20, "key2", new TestElement("Second"));

            string json         = JsonUtility.ToJson(original);
            var    deserialized = JsonUtility.FromJson<XOrdered<int, string, TestElement>>(json);

            Assert.AreEqual(original.Count, deserialized.Count);

            for (int i = 0; i < original.Count; i++)
            {
                Assert.AreEqual(original[i].Value.Name, deserialized[i].Value.Name);
                Assert.AreEqual(original[i].Order,      deserialized[i].Order);
            }

            Assert.IsTrue(deserialized.ContainsKey("key1"));
            Assert.IsTrue(deserialized.ContainsKey("key2"));
            Assert.IsTrue(deserialized.ContainsKey("key3"));
            Assert.AreEqual("First",  deserialized["key1"].Name);
            Assert.AreEqual("Second", deserialized["key2"].Name);
            Assert.AreEqual("Third",  deserialized["key3"].Name);

            deserialized.Add(0, "key0", new TestElement("Zero"));

            Assert.AreEqual(4,      deserialized.Count);
            Assert.AreEqual("Zero", deserialized[0].Value.Name);
            Assert.AreEqual(0,      deserialized[0].Order);
            Assert.IsTrue(deserialized.ContainsKey("key0"));
        }

    #endregion

    #region KS-2: Clone 기본

        [Test]
        public void TEST_XOrdered_Keyed_Clone_독립_인스턴스()
        {
            var original = new XOrdered<int, string, TestElement>();

            original.Add(10, "key1", new TestElement("First"));
            original.Add(20, "key2", new TestElement("Second"));
            original.Add(30, "key3", new TestElement("Third"));

            var cloned = original.Clone();

            Assert.AreEqual(original.Count, cloned.Count);

            for (int i = 0; i < original.Count; i++)
            {
                Assert.AreEqual(original[i].Value.Name, cloned[i].Value.Name);
                Assert.AreEqual(original[i].Order,      cloned[i].Order);
            }

            Assert.IsTrue(cloned.ContainsKey("key1"));
            Assert.IsTrue(cloned.ContainsKey("key2"));
            Assert.IsTrue(cloned.ContainsKey("key3"));
            Assert.AreEqual("First",  cloned["key1"].Name);
            Assert.AreEqual("Second", cloned["key2"].Name);
            Assert.AreEqual("Third",  cloned["key3"].Name);

            cloned.Add(40, "key4", new TestElement("Fourth"));

            Assert.AreEqual(4, cloned.Count);
            Assert.AreEqual(3, original.Count);
            Assert.IsTrue(cloned.ContainsKey("key4"));
            Assert.IsFalse(original.ContainsKey("key4"));
        }

    #endregion

    #region KS-3: Clone CrossReference 보존

        [Test]
        public void TEST_XOrdered_Keyed_Clone_CrossReference_보존()
        {
            var original = new XOrdered<int, string, TestCloneableElement>();

            original.Add(10, "key1", new TestCloneableElement("First"));
            original.Add(20, "key2", new TestCloneableElement("Second"));

            var cloned = original.Clone();

            // 핵심: cloned 안에서 list[i].Value와 dictionary[key]는 동일 인스턴스
            Assert.AreSame(cloned[0].Value, cloned["key1"]);
            Assert.AreSame(cloned[1].Value, cloned["key2"]);

            // 깊은 복제: 원본과 복제본은 다른 인스턴스
            Assert.AreNotSame(original[0].Value, cloned[0].Value);
            Assert.AreNotSame(original[1].Value, cloned[1].Value);

            // 내용은 동일
            Assert.AreEqual("First",  cloned[0].Value.Name);
            Assert.AreEqual("Second", cloned[1].Value.Name);
        }

    #endregion

    #region KS-4: Clone 중복 Value 단일 복제

        [Test]
        public void TEST_XOrdered_Keyed_Clone_중복_Value_단일_복제()
        {
            var shared = new TestCloneableElement("Shared");

            var original = new XOrdered<int, string, TestCloneableElement>();

            original.Add(10, "key1", shared);
            original.Add(20, "key2", shared);

            var cloned = original.Clone();

            // 동일 reference가 두 번 등장해도 cloned에서는 단일 복제 인스턴스로 통일됨
            Assert.AreSame(cloned[0].Value, cloned[1].Value);
            Assert.AreSame(cloned[0].Value, cloned["key1"]);
            Assert.AreSame(cloned[0].Value, cloned["key2"]);

            // 원본과는 다른 인스턴스
            Assert.AreNotSame(shared, cloned[0].Value);

            Assert.AreEqual("Shared", cloned[0].Value.Name);
        }

    #endregion

    #region KP-1: 대량 데이터 스트레스

        [Test]
        public void TEST_XOrdered_Keyed_대량_데이터_스트레스()
        {
            var ordered = new XOrdered<int, string, TestElement>();
            const int testCount = 100;

            // 1. 역순 Add → 오름차순
            for (int i = testCount; i > 0; i--)
            {
                ordered.Add(i, $"key{i}", new TestElement($"Element-{i}"));
            }

            Assert.AreEqual(testCount, ordered.Count);

            for (int i = 0; i < testCount; i++)
            {
                int expectedOrder = i + 1;

                Assert.AreEqual($"Element-{expectedOrder}", ordered[i].Value.Name);
                Assert.AreEqual(expectedOrder,              ordered[i].Order);
            }

            // 2. Key 조회
            for (int i = 1; i <= testCount; i++)
            {
                string key = $"key{i}";

                Assert.IsTrue(ordered.ContainsKey(key));
                Assert.IsTrue(ordered.TryGetValue(key, out var value));
                Assert.AreEqual($"Element-{i}", value.Name);
                Assert.AreEqual($"Element-{i}", ordered[key].Name);
            }

            // 3. 랜덤 Add → 정렬 확인
            ordered.Clear();

            var random      = new System.Random(42);
            var addedOrders = new List<int>();

            for (int i = 0; i < testCount; i++)
            {
                int order = random.Next(1, 1000);
                addedOrders.Add(order);
                ordered.Add(order, $"randomKey{i}", new TestElement($"Random-{i}"));
            }

            addedOrders.Sort();

            for (int i = 0; i < testCount; i++)
            {
                Assert.IsNotNull(ordered[i].Value);
                Assert.AreEqual(addedOrders[i], ordered[i].Order);
            }

            // 4. Add/Remove 혼합
            ordered.Clear();

            var keys = new List<string>();

            for (int i = 0; i < 50; i++)
            {
                string key = $"Mix-{i}";
                keys.Add(key);
                ordered.Add(i * 2, key, new TestElement($"Mix-{i}"));
            }

            Assert.AreEqual(50, ordered.Count);

            for (int i = 0; i < 25; i++)
            {
                ordered.Remove(keys[i * 2]);
            }

            Assert.AreEqual(25, ordered.Count);

            for (int i = 50; i < 100; i++)
            {
                ordered.Add(i * 2, $"Mix-{i}", new TestElement($"Mix-{i}"));
            }

            Assert.AreEqual(75, ordered.Count);

            int count     = 0;
            int prevOrder = -1;

            foreach (var (order, value) in ordered)
            {
                Assert.IsNotNull(value);
                Assert.GreaterOrEqual(order, prevOrder);

                prevOrder = order;
                count++;
            }

            Assert.AreEqual(75, count);
        }

    #endregion

    }

}
