/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TEST_BoundedFIFO.cs
수정일 : 2026-11-24

# 설명
BoundedFIFOMap과 BoundedFIFOSet의 용량 검증, 중복 거절, 밀어냄 순서와 알림, 비우기, 할당 없음 계약을 검증한다.
Unity Test Runner (Edit Mode) 에서 실행한다.

# 테스트 구성
 M: BoundedFIFOMap
 S: BoundedFIFOSet
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Constraints;
using Is = UnityEngine.TestTools.Constraints.Is;

using NUnit;
using NUnit.Framework;

namespace inonego.Xeri.TEST.Core._Collection
{
    // ============================================================
    /// <summary>
    /// 최근 N개 창 컬렉션의 런타임 계약을 확인한다.
    /// </summary>
    // ============================================================
    public class TEST_BoundedFIFO
    {

    #region M: BoundedFIFOMap

        // ------------------------------------------------------------
        /// <summary>
        /// 용량은 1 이상이어야 한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_BoundedFIFOMap_용량이1미만이면_예외()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new BoundedFIFOMap<int, int>(0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new BoundedFIFOMap<int, int>(-1));
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 새 키는 추가되고 같은 키는 값과 순서를 바꾸지 않고 거절된다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_BoundedFIFOMap_중복키는_값과순서를유지하며거절()
        {
            var window = new BoundedFIFOMap<int, string>(3);

            Assert.IsTrue(window.TryAdd(1, "a"));
            Assert.IsTrue(window.TryAdd(2, "b"));
            Assert.IsFalse(window.TryAdd(1, "z"));

            Assert.AreEqual(2, window.Count);
            Assert.IsTrue(window.TryGetValue(1, out var value));
            Assert.AreEqual("a", value);
            CollectionAssert.AreEqual(new[] { 1, 2 }, window.Select(pair => pair.Key).ToArray());
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 가득 찬 창은 가장 오래된 항목부터 밀어내고 그 키와 값을 알린다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_BoundedFIFOMap_가득차면_가장오래된항목을밀어내고알림()
        {
            var window = new BoundedFIFOMap<int, string>(2);
            var dropped = new List<KeyValuePair<int, string>>();
            window.OnDropped += (key, value) => dropped.Add(new KeyValuePair<int, string>(key, value));

            window.TryAdd(1, "a");
            window.TryAdd(2, "b");
            Assert.AreEqual(0, dropped.Count);

            window.TryAdd(3, "c");
            window.TryAdd(4, "d");

            Assert.AreEqual(2, window.Count);
            Assert.IsFalse(window.ContainsKey(1));
            Assert.IsFalse(window.ContainsKey(2));
            CollectionAssert.AreEqual(new[] { 3, 4 }, window.Select(pair => pair.Key).ToArray());
            CollectionAssert.AreEqual
            (
                new[] { new KeyValuePair<int, string>(1, "a"), new KeyValuePair<int, string>(2, "b") },
                dropped
            );
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 밀려난 키는 창 밖이므로 다시 추가할 수 있다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_BoundedFIFOMap_밀려난키는_다시추가가능()
        {
            var window = new BoundedFIFOMap<int, int>(1);

            Assert.IsTrue(window.TryAdd(1, 10));
            Assert.IsTrue(window.TryAdd(2, 20));
            Assert.IsTrue(window.TryAdd(1, 11));

            Assert.IsTrue(window.TryGetValue(1, out var value));
            Assert.AreEqual(11, value);
            Assert.AreEqual(1, window.Count);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 비우기는 밀어냄이 아니므로 알리지 않고 용량을 다시 쓸 수 있다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_BoundedFIFOMap_비우기는_알리지않고용량을되돌림()
        {
            var window = new BoundedFIFOMap<int, int>(2);
            var droppedCount = 0;
            window.OnDropped += (key, value) => droppedCount++;

            window.TryAdd(1, 1);
            window.TryAdd(2, 2);
            window.Clear();

            Assert.AreEqual(0, window.Count);
            Assert.IsTrue(window.TryAdd(3, 3));
            Assert.IsTrue(window.TryAdd(4, 4));
            Assert.AreEqual(0, droppedCount);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 구독자가 던져도 새 항목은 이미 담겼고 두 내부 구조는 일관된다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_BoundedFIFOMap_알림예외가전파되어도_창은일관됨()
        {
            var window = new BoundedFIFOMap<int, int>(2);
            window.TryAdd(1, 1);
            window.TryAdd(2, 2);
            window.OnDropped += (key, value) => throw new InvalidOperationException("알림 실패");

            Assert.Throws<InvalidOperationException>(() => window.TryAdd(3, 3));

            Assert.AreEqual(2, window.Count);
            Assert.IsFalse(window.ContainsKey(1));
            CollectionAssert.AreEqual(new[] { 2, 3 }, window.Select(pair => pair.Key).ToArray());
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 지정한 비교자로 키의 같음을 판정한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_BoundedFIFOMap_지정한비교자로키를비교()
        {
            var window = new BoundedFIFOMap<string, int>(2, StringComparer.OrdinalIgnoreCase);

            Assert.IsTrue(window.TryAdd("Key", 1));
            Assert.IsFalse(window.TryAdd("KEY", 2));
            Assert.IsTrue(window.ContainsKey("key"));
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 가득 찬 뒤의 추가·밀어냄·조회 반복은 GC 할당을 만들지 않는다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_BoundedFIFOMap_가득찬뒤_추가밀어냄조회반복은_할당없음()
        {
            var window = new BoundedFIFOMap<int, int>(8);
            var next = 0;

            for (var i = 0; i < 16; i++)
            {
                window.TryAdd(next++, 0);
            }

            void Cycle()
            {
                window.TryAdd(next++, 0);
                window.ContainsKey(next - 1);
                window.TryGetValue(next - 2, out _);
            }

            Assert.That(Cycle, Is.Not.AllocatingGCMemory());
        }

    #endregion

    #region S: BoundedFIFOSet

        // ------------------------------------------------------------
        /// <summary>
        /// 항목 추가·중복 거절·포함 조회가 동작한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_BoundedFIFOSet_추가와중복거절과포함조회()
        {
            var window = new BoundedFIFOSet<int>(3);

            Assert.IsTrue(window.TryAdd(1));
            Assert.IsTrue(window.TryAdd(2));
            Assert.IsFalse(window.TryAdd(1));

            Assert.AreEqual(2, window.Count);
            Assert.AreEqual(3, window.Capacity);
            Assert.IsTrue(window.Contains(1));
            Assert.IsFalse(window.Contains(9));
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 가득 차면 가장 오래된 항목을 밀어내 알리고 열거는 오래된 순서다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_BoundedFIFOSet_가득차면_오래된항목을밀어내고알림()
        {
            var window = new BoundedFIFOSet<int>(2);
            var dropped = new List<int>();
            window.OnDropped += dropped.Add;

            window.TryAdd(1);
            window.TryAdd(2);
            window.TryAdd(3);

            CollectionAssert.AreEqual(new[] { 1 }, dropped);
            CollectionAssert.AreEqual(new[] { 2, 3 }, window.ToArray());
            Assert.IsFalse(window.Contains(1));
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 비우기는 알리지 않고 모든 항목을 제거한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_BoundedFIFOSet_비우기는_알리지않음()
        {
            var window = new BoundedFIFOSet<int>(2);
            var droppedCount = 0;
            window.OnDropped += item => droppedCount++;

            window.TryAdd(1);
            window.TryAdd(2);
            window.Clear();

            Assert.AreEqual(0, window.Count);
            Assert.AreEqual(0, droppedCount);
        }

    #endregion

    }
}
