/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TEST_CollectionClone.cs
수정일 : 2026-11-24

# 설명
Serialized Collection의 CloneFrom 계약(요소 복제 규칙, 재사용, 실패 시 대상 불변)을 검증한다.
Unity Test Runner (Edit Mode) 에서 실행한다.

# 테스트 구성
 U-1~3: 키 없는 XOrdered 복제 (제자리 복사와 위치 재사용 / 실패와 거절 / 규모와 타입)
 K-1~3: 키 있는 XOrdered 복제 (제자리 복사와 Key 재사용 / 실패와 거절 / 규모와 타입)
 D: XDictionary 복제
 S: XHashSet, XQueue, XStack, XPriorityQueue 복제
 B: Dictionary, HashSet, Queue, Stack, List 확장 메서드
 A: 같은 구성 복제의 GC 할당

# 특이사항, 제약사항
할당 검사는 Unity Test Framework의 AllocatingGCMemory 제약을 쓰므로 Unity 밖에서는 실행할 수 없다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Constraints;
using Is = UnityEngine.TestTools.Constraints.Is;

using NUnit;
using NUnit.Framework;

using inonego;
using inonego.Xeri;
using inonego.Xeri.Serializable;

namespace inonego.Xeri.TEST.Serializable._CollectionClone
{
    // ============================================================
    /// <summary>
    /// Serialized Collection의 CloneFrom 테스트.
    /// </summary>
    // ============================================================
    public class TEST_CollectionClone
    {

    #region 헬퍼

        // ============================================================
        /// <summary>
        /// 복제를 지원하는 테스트 요소.
        /// </summary>
        // ============================================================
        [Serializable]
        private sealed class CloneItem : IDeepCloneable<CloneItem>
        {
            internal string Name;
            internal int Count;
            internal int CloneFromCalls;

            internal CloneItem()
            {
                // NONE
            }

            internal CloneItem(string name, int count)
            {
                Name = name;
                Count = count;
            }

            CloneItem IDeepCloneable<CloneItem>.@new() => new();

            void IDeepCloneableFrom<CloneItem>.CloneFrom(CloneItem source)
            {
                Name = source.Name;
                Count = source.Count;
                CloneFromCalls++;
            }
        }

        // ============================================================
        /// <summary>
        /// 복제를 지원하지 않는 테스트 요소.
        /// </summary>
        // ============================================================
        [Serializable]
        private sealed class PlainItem
        {
            internal string Name;

            internal PlainItem(string name)
            {
                Name = name;
            }
        }

        // ============================================================
        /// <summary>
        /// CloneFrom을 받는 쪽이 Fail이면 실패하는 테스트 요소.
        /// </summary>
        // ============================================================
        [Serializable]
        private sealed class FailItem : IDeepCloneable<FailItem>
        {
            internal string Name;
            internal bool Fail;

            internal FailItem(string name, bool fail = false)
            {
                Name = name;
                Fail = fail;
            }

            FailItem IDeepCloneable<FailItem>.@new() => new(string.Empty);

            void IDeepCloneableFrom<FailItem>.CloneFrom(FailItem source)
            {
                if (Fail)
                {
                    throw new InvalidOperationException("테스트 복제 실패 " + Name);
                }

                Name = source.Name;
            }
        }

        // ============================================================
        /// <summary>
        /// 훅 호출 횟수를 세는 공유 카운터.
        /// </summary>
        // ============================================================
        private sealed class HookCounter
        {
            internal int Calls;
        }

        // ============================================================
        /// <summary>
        /// 호출 횟수를 세는 복제 훅.
        /// </summary>
        // ============================================================
        private readonly struct CountingHook : ICloneReadyHook
        {
            private readonly HookCounter counter;

            internal CountingHook(HookCounter counter)
            {
                this.counter = counter;
            }

            void ICloneReadyHook.OnCloneReady()
            {
                counter.Calls++;
            }
        }

        // ============================================================
        /// <summary>
        /// 키 있는 파생 컬렉션과의 복제를 확인하기 위한 컬렉션.
        /// </summary>
        // ============================================================
        private sealed class DerivedKeyedOrdered : XOrdered<int, string, CloneItem>
        {
            // NONE
        }

        // ============================================================
        /// <summary>
        /// 키 없는 파생 컬렉션과의 복제를 확인하기 위한 컬렉션.
        /// </summary>
        // ============================================================
        private sealed class DerivedUnkeyedOrdered : XOrdered<int, CloneItem>
        {
            // NONE
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 델리게이트 공유를 확인하기 위한 빈 메서드.
        /// </summary>
        // ------------------------------------------------------------
        private static void Noop()
        {
            // NONE
        }

    #endregion

    #region U-1: 키 없는 XOrdered 제자리 복사와 위치 기준 재사용

        // ------------------------------------------------------------
        /// <summary>
        /// 같은 구성이면 기존 인스턴스에 제자리 복사한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_CollectionClone_XOrdered키없음_같은구성_제자리복사()
        {
            var a = new CloneItem("a", 1);
            var b = new CloneItem("b", 2);
            var target = new XOrdered<int, CloneItem>();

            target.Add(0, a);
            target.Add(1, b);

            var source = new XOrdered<int, CloneItem>();
            var sourceA = new CloneItem("A", 10);
            var sourceB = new CloneItem("B", 20);

            source.Add(0, sourceA);
            source.Add(1, sourceB);

            var counter = new HookCounter();

            target.CloneFrom(source, new CountingHook(counter));

            Assert.AreSame(a, target[0].Value);
            Assert.AreSame(b, target[1].Value);
            Assert.AreEqual("A", a.Name);
            Assert.AreEqual(10, a.Count);
            Assert.AreEqual("B", b.Name);
            Assert.AreEqual(20, b.Count);
            Assert.AreEqual(1, counter.Calls);

            // source는 바뀌지 않고 인스턴스도 공유하지 않는다.
            Assert.AreEqual("A", sourceA.Name);
            Assert.AreEqual(0, sourceA.CloneFromCalls);
            Assert.AreNotSame(sourceA, target[0].Value);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 같은 Order의 순서를 그대로 옮기고 Value는 복제한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_CollectionClone_XOrdered키없음_순서유지_값복제()
        {
            var source = new XOrdered<int, CloneItem>();

            source.Add(0, new CloneItem("first", 1));
            source.Add(0, new CloneItem("second", 2));
            source.Add(1, new CloneItem("third", 3));

            var target = new XOrdered<int, CloneItem>();

            target.Add(9, new CloneItem("old", 0));
            target.CloneFrom(source);

            Assert.AreEqual(3, target.Count);
            Assert.AreEqual("first", target[0].Value.Name);
            Assert.AreEqual("second", target[1].Value.Name);
            Assert.AreEqual("third", target[2].Value.Name);
            Assert.AreEqual(1, target[2].Order);
            Assert.AreNotSame(source[0].Value, target[0].Value);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 구성이 다르면 같은 위치의 인스턴스는 재사용하고 나머지는 복제한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_CollectionClone_XOrdered키없음_구성다르면_위치재사용과_복제()
        {
            var first = new CloneItem("a", 1);
            var second = new CloneItem("x", 9);
            var target = new XOrdered<int, CloneItem>();

            target.Add(5, first);
            target.Add(6, second);

            var source = new XOrdered<int, CloneItem>();
            var sourceC = new CloneItem("C", 30);

            source.Add(0, new CloneItem("A", 10));
            source.Add(1, new CloneItem("B", 20));
            source.Add(2, sourceC);

            var counter = new HookCounter();

            target.CloneFrom(source, new CountingHook(counter));

            Assert.AreEqual(3, target.Count);
            Assert.AreEqual(1, counter.Calls);

            // 같은 위치는 기존 인스턴스를 재사용하고 Order는 source를 따른다.
            Assert.AreSame(first, target[0].Value);
            Assert.AreEqual(0, target[0].Order);
            Assert.AreEqual("A", first.Name);
            Assert.AreSame(second, target[1].Value);
            Assert.AreEqual(1, target[1].Order);
            Assert.AreEqual("B", second.Name);

            // 짝이 없는 위치는 복제하며 source 인스턴스를 공유하지 않는다.
            Assert.AreEqual(2, target[2].Order);
            Assert.AreNotSame(sourceC, target[2].Value);
            Assert.AreEqual("C", target[2].Value.Name);
            Assert.AreEqual(30, target[2].Value.Count);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// source가 가진 인스턴스는 재사용하지 않는다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_CollectionClone_XOrdered키없음_source공유인스턴스_재사용안함()
        {
            var shared = new CloneItem("shared", 1);
            var target = new XOrdered<int, CloneItem>();
            var source = new XOrdered<int, CloneItem>();

            target.Add(0, shared);
            source.Add(0, shared);
            source.Add(1, new CloneItem("other", 2));

            target.CloneFrom(source);

            Assert.AreNotSame(shared, target[0].Value);
            Assert.AreEqual("shared", target[0].Value.Name);
            Assert.AreEqual(0, shared.CloneFromCalls);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 여러 위치가 공유하던 인스턴스는 한 위치에만 재사용한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_CollectionClone_XOrdered키없음_공유인스턴스_한번만재사용()
        {
            var shared = new CloneItem("shared", 0);
            var target = new XOrdered<int, CloneItem>();

            target.Add(0, shared);
            target.Add(1, shared);

            var source = new XOrdered<int, CloneItem>();

            source.Add(0, new CloneItem("A", 1));
            source.Add(1, new CloneItem("B", 2));

            target.CloneFrom(source);

            Assert.AreSame(shared, target[0].Value);
            Assert.AreNotSame(shared, target[1].Value);
            Assert.AreEqual("A", target[0].Value.Name);
            Assert.AreEqual("B", target[1].Value.Name);
        }

    #endregion

    #region U-2: 키 없는 XOrdered 실패와 거절

        // ------------------------------------------------------------
        /// <summary>
        /// 복제를 지원하지 않는 요소는 공유하지 않고 예외를 던진다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_CollectionClone_XOrdered키없음_복제불가요소_예외_대상불변()
        {
            var source = new XOrdered<int, PlainItem>();

            source.Add(0, new PlainItem("x"));

            var keep = new PlainItem("keep");
            var target = new XOrdered<int, PlainItem>();

            target.Add(0, keep);

            Assert.Throws<InvalidOperationException>(() => target.CloneFrom(source));

            Assert.AreEqual(1, target.Count);
            Assert.AreSame(keep, target[0].Value);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 새 요소 복제가 실패하면 훅을 호출하지 않는다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_CollectionClone_XOrdered키없음_준비실패시_훅미호출()
        {
            var target = new XOrdered<int, PlainItem>();
            var source = new XOrdered<int, PlainItem>();

            source.Add(0, new PlainItem("x"));

            var counter = new HookCounter();

            Assert.Throws<InvalidOperationException>(() => target.CloneFrom(source, new CountingHook(counter)));

            Assert.AreEqual(0, counter.Calls);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// null Value는 그대로 보존한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_CollectionClone_XOrdered키없음_null값_보존()
        {
            var source = new XOrdered<int, CloneItem>();

            source.Add(0, null);
            source.Add(1, new CloneItem("item", 1));

            var target = new XOrdered<int, CloneItem>();

            target.CloneFrom(source);

            Assert.AreEqual(2, target.Count);
            Assert.IsNull(target[0].Value);
            Assert.AreEqual("item", target[1].Value.Name);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// null은 거절하고 자기 자신은 아무 일도 하지 않는다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_CollectionClone_XOrdered키없음_거절조건과_자기복제()
        {
            var ordered = new XOrdered<int, CloneItem>();

            ordered.Add(0, new CloneItem("a", 1));

            Assert.Throws<ArgumentNullException>(() => ordered.CloneFrom(null));

            var first = ordered[0].Value;

            ordered.CloneFrom(ordered);

            Assert.AreEqual(1, ordered.Count);
            Assert.AreSame(first, ordered[0].Value);
            Assert.AreEqual(0, first.CloneFromCalls);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> 구성이 달라 재구성할 때 재사용 값의 CloneFrom이 실패해도
        /// <br/> 나머지를 끝까지 복사하고 목록은 source 구성이 된다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_CollectionClone_XOrdered키없음_재구성중복사실패_나머지진행_구성맞춤()
        {
            var failing = new FailItem("old-0", true);
            var healthy = new FailItem("old-1");
            var target = new XOrdered<int, FailItem>();

            target.Add(5, failing);
            target.Add(6, healthy);

            var source = new XOrdered<int, FailItem>();

            source.Add(0, new FailItem("A"));
            source.Add(1, new FailItem("B"));
            source.Add(2, new FailItem("C"));

            var counter = new HookCounter();

            Assert.Throws<InvalidOperationException>(() => target.CloneFrom(source, new CountingHook(counter)));

            Assert.AreEqual(1, counter.Calls);
            Assert.AreEqual(3, target.Count);
            Assert.AreSame(failing, target[0].Value);
            Assert.AreEqual("old-0", failing.Name);
            Assert.AreEqual(0, target[0].Order);
            Assert.AreSame(healthy, target[1].Value);
            Assert.AreEqual("B", healthy.Name);
            Assert.AreEqual(2, target[2].Order);
            Assert.AreEqual("C", target[2].Value.Name);
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> 같은 구성이어서 제자리로 복사할 때 한 값이 실패해도
        /// <br/> 나머지 값은 복사되고 실패가 여럿이면 AggregateException이다.
        /// </summary>
        // ----------------------------------------------------------------------
        [Test]
        public void TEST_CollectionClone_XOrdered키없음_제자리복사실패_나머지진행_여럿이면Aggregate()
        {
            var failing = new FailItem("old-0", true);
            var healthy = new FailItem("old-1");
            var target = new XOrdered<int, FailItem>();

            target.Add(0, failing);
            target.Add(1, healthy);

            var source = new XOrdered<int, FailItem>();

            source.Add(0, new FailItem("A"));
            source.Add(1, new FailItem("B"));

            Assert.Throws<InvalidOperationException>(() => target.CloneFrom(source));

            Assert.AreEqual(2, target.Count);
            Assert.AreEqual("old-0", failing.Name);
            Assert.AreEqual("B", healthy.Name);

            healthy.Fail = true;
            healthy.Name = "old-1";

            var aggregate = Assert.Throws<AggregateException>(() => target.CloneFrom(source));

            Assert.AreEqual(2, aggregate.InnerExceptions.Count);
        }

    #endregion

    #region U-3: 키 없는 XOrdered 규모와 타입

        // ------------------------------------------------------------
        /// <summary>
        /// 항목이 많아도 같은 위치는 재사용하고 늘어난 위치는 복제한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_CollectionClone_XOrdered키없음_많은항목_재구성_재사용()
        {
            var target = new XOrdered<int, CloneItem>();
            var source = new XOrdered<int, CloneItem>();
            var keep = new CloneItem("keep", 0);

            for (var number = 0; number < 12; number++)
            {
                target.Add(number, number == 5 ? keep : new CloneItem($"old-{number}", number));
            }

            for (var number = 0; number < 14; number++)
            {
                source.Add(number, new CloneItem($"new-{number}", number * 10));
            }

            target.CloneFrom(source);

            Assert.AreEqual(14, target.Count);
            Assert.AreSame(keep, target[5].Value);
            Assert.AreEqual("new-5", keep.Name);
            Assert.AreNotSame(source[13].Value, target[13].Value);
            Assert.AreEqual("new-13", target[13].Value.Name);

            for (var position = 0; position < 14; position++)
            {
                Assert.AreNotSame(source[position].Value, target[position].Value);
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 구체 타입이 달라도 복제한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_CollectionClone_XOrdered키없음_파생타입과도_복제()
        {
            var source = new XOrdered<int, CloneItem>();

            source.Add(0, new CloneItem("a", 1));

            var derived = new DerivedUnkeyedOrdered();

            derived.CloneFrom(source);

            Assert.AreEqual(1, derived.Count);
            Assert.AreEqual("a", derived[0].Value.Name);

            var back = new XOrdered<int, CloneItem>();

            back.CloneFrom(derived);

            Assert.AreEqual(1, back.Count);
            Assert.AreNotSame(derived[0].Value, back[0].Value);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Clone은 순서를 지킨 독립 복사본을 만든다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_CollectionClone_XOrdered키없음_Clone_독립복사본()
        {
            var source = new XOrdered<int, CloneItem>();

            source.Add(2, new CloneItem("late", 2));
            source.Add(1, new CloneItem("early", 1));

            var clone = source.Clone();

            Assert.AreEqual(2, clone.Count);
            Assert.AreEqual("early", clone[0].Value.Name);
            Assert.AreEqual("late", clone[1].Value.Name);
            Assert.AreNotSame(source[0].Value, clone[0].Value);

            clone[0].Value.Count = 99;

            Assert.AreEqual(1, source[0].Value.Count);
        }

    #endregion

    #region K-1: 키 있는 XOrdered 제자리 복사와 Key 기준 재사용

        // ------------------------------------------------------------
        /// <summary>
        /// 같은 구성이면 기존 인스턴스에 제자리 복사한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_CollectionClone_XOrdered키있음_같은구성_제자리복사()
        {
            var a = new CloneItem("a", 1);
            var b = new CloneItem("b", 2);
            var target = new XOrdered<int, string, CloneItem>();

            target.Add(0, "a", a);
            target.Add(1, "b", b);

            var source = new XOrdered<int, string, CloneItem>();
            var sourceA = new CloneItem("A", 10);
            var sourceB = new CloneItem("B", 20);

            source.Add(0, "a", sourceA);
            source.Add(1, "b", sourceB);

            var counter = new HookCounter();

            target.CloneFrom(source, new CountingHook(counter));

            Assert.AreSame(a, target[0].Value);
            Assert.AreSame(b, target[1].Value);
            Assert.AreEqual("A", a.Name);
            Assert.AreEqual(10, a.Count);
            Assert.AreEqual("B", b.Name);
            Assert.AreEqual(20, b.Count);
            Assert.AreEqual(1, counter.Calls);

            // source는 바뀌지 않고 인스턴스도 공유하지 않는다.
            Assert.AreEqual("A", sourceA.Name);
            Assert.AreEqual(0, sourceA.CloneFromCalls);
            Assert.AreNotSame(sourceA, target[0].Value);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 구성이 다르면 같은 Key 인스턴스는 재사용하고 나머지는 복제한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_CollectionClone_XOrdered키있음_구성다르면_재사용과_복제()
        {
            var a = new CloneItem("a", 1);
            var removed = new CloneItem("x", 9);
            var target = new XOrdered<int, string, CloneItem>();

            target.Add(5, "a", a);
            target.Add(6, "x", removed);

            var source = new XOrdered<int, string, CloneItem>();
            var sourceA = new CloneItem("A", 10);
            var sourceB = new CloneItem("B", 20);

            source.Add(0, "a", sourceA);
            source.Add(1, "b", sourceB);

            var counter = new HookCounter();

            target.CloneFrom(source, new CountingHook(counter));

            Assert.AreEqual(2, target.Count);
            Assert.AreEqual(1, counter.Calls);

            // 같은 Key는 기존 인스턴스를 재사용하고 Order는 source를 따른다.
            Assert.AreSame(a, target[0].Value);
            Assert.AreEqual("a", target[0].Key);
            Assert.AreEqual(0, target[0].Order);
            Assert.AreEqual("A", a.Name);

            // 새 Key는 복제하며 source 인스턴스를 공유하지 않는다.
            Assert.AreEqual("b", target[1].Key);
            Assert.AreEqual(1, target[1].Order);
            Assert.AreNotSame(sourceB, target[1].Value);
            Assert.AreEqual("B", target[1].Value.Name);
            Assert.AreEqual(20, target[1].Value.Count);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// source가 가진 인스턴스는 재사용하지 않는다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_CollectionClone_XOrdered키있음_source공유인스턴스_재사용안함()
        {
            var shared = new CloneItem("shared", 1);
            var target = new XOrdered<int, string, CloneItem>();
            var source = new XOrdered<int, string, CloneItem>();

            target.Add(0, "k", shared);
            source.Add(0, "k", shared);
            source.Add(1, "other", new CloneItem("other", 2));

            target.CloneFrom(source);

            Assert.AreNotSame(shared, target[0].Value);
            Assert.AreEqual("shared", target[0].Value.Name);
            Assert.AreEqual(0, shared.CloneFromCalls);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 여러 Key가 공유하던 인스턴스는 한 Key에만 재사용한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_CollectionClone_XOrdered키있음_공유인스턴스_한번만재사용()
        {
            var shared = new CloneItem("shared", 0);
            var target = new XOrdered<int, string, CloneItem>();

            target.Add(0, "a", shared);
            target.Add(1, "b", shared);

            var source = new XOrdered<int, string, CloneItem>();

            source.Add(0, "a", new CloneItem("A", 1));
            source.Add(1, "b", new CloneItem("B", 2));

            target.CloneFrom(source);

            Assert.AreSame(shared, target[0].Value);
            Assert.AreNotSame(shared, target[1].Value);
            Assert.AreEqual("A", target[0].Value.Name);
            Assert.AreEqual("B", target[1].Value.Name);
        }

    #endregion

    #region K-2: 키 있는 XOrdered 실패와 거절

        // ------------------------------------------------------------
        /// <summary>
        /// 복제를 지원하지 않는 요소는 공유하지 않고 예외를 던진다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_CollectionClone_XOrdered키있음_복제불가요소_예외_대상불변()
        {
            var keep = new PlainItem("keep");
            var target = new XOrdered<int, string, PlainItem>();

            target.Add(0, "keep", keep);

            var source = new XOrdered<int, string, PlainItem>();

            source.Add(0, "x", new PlainItem("x"));

            Assert.Throws<InvalidOperationException>(() => target.CloneFrom(source));

            Assert.AreEqual(1, target.Count);
            Assert.AreEqual("keep", target[0].Key);
            Assert.AreSame(keep, target[0].Value);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 새 요소 복제가 실패하면 훅을 호출하지 않는다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_CollectionClone_XOrdered키있음_준비실패시_훅미호출()
        {
            var target = new XOrdered<int, string, PlainItem>();
            var source = new XOrdered<int, string, PlainItem>();

            source.Add(0, "x", new PlainItem("x"));

            var counter = new HookCounter();

            Assert.Throws<InvalidOperationException>(() => target.CloneFrom(source, new CountingHook(counter)));

            Assert.AreEqual(0, counter.Calls);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// null Value는 그대로 보존한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_CollectionClone_XOrdered키있음_null값_보존()
        {
            var source = new XOrdered<int, string, CloneItem>();

            source.Add(0, "empty", null);
            source.Add(1, "item", new CloneItem("item", 1));

            var target = new XOrdered<int, string, CloneItem>();

            target.CloneFrom(source);

            Assert.AreEqual(2, target.Count);
            Assert.IsNull(target[0].Value);
            Assert.AreEqual("item", target[1].Value.Name);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// null은 거절하고 자기 자신은 아무 일도 하지 않는다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_CollectionClone_XOrdered키있음_거절조건과_자기복제()
        {
            var ordered = new XOrdered<int, string, CloneItem>();

            ordered.Add(0, "a", new CloneItem("a", 1));

            Assert.Throws<ArgumentNullException>(() => ordered.CloneFrom(null));

            var first = ordered[0].Value;

            ordered.CloneFrom(ordered);

            Assert.AreEqual(1, ordered.Count);
            Assert.AreSame(first, ordered[0].Value);
            Assert.AreEqual(0, first.CloneFromCalls);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> 구성이 달라 재구성할 때 재사용 값의 CloneFrom이 실패해도
        /// <br/> 나머지를 끝까지 복사하고 목록은 source 구성이 된다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_CollectionClone_XOrdered키있음_재구성중복사실패_나머지진행_구성맞춤()
        {
            var failing = new FailItem("old-a", true);
            var healthy = new FailItem("old-b");
            var target = new XOrdered<int, string, FailItem>();

            target.Add(5, "a", failing);
            target.Add(6, "b", healthy);
            target.Add(7, "gone", new FailItem("gone"));

            var source = new XOrdered<int, string, FailItem>();

            source.Add(0, "a", new FailItem("A"));
            source.Add(1, "b", new FailItem("B"));
            source.Add(2, "c", new FailItem("C"));

            var counter = new HookCounter();

            Assert.Throws<InvalidOperationException>(() => target.CloneFrom(source, new CountingHook(counter)));

            Assert.AreEqual(1, counter.Calls);
            Assert.AreEqual(3, target.Count);
            Assert.IsFalse(target.ContainsKey("gone"));
            Assert.AreSame(failing, target[0].Value);
            Assert.AreEqual("old-a", failing.Name);
            Assert.AreEqual(0, target[0].Order);
            Assert.AreSame(healthy, target[1].Value);
            Assert.AreEqual("B", healthy.Name);
            Assert.AreEqual("c", target[2].Key);
            Assert.AreEqual("C", target[2].Value.Name);
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> 같은 구성이어서 제자리로 복사할 때 한 값이 실패해도
        /// <br/> 나머지 값은 복사되고 실패가 여럿이면 AggregateException이다.
        /// </summary>
        // ----------------------------------------------------------------------
        [Test]
        public void TEST_CollectionClone_XOrdered키있음_제자리복사실패_나머지진행_여럿이면Aggregate()
        {
            var failing = new FailItem("old-a", true);
            var healthy = new FailItem("old-b");
            var target = new XOrdered<int, string, FailItem>();

            target.Add(0, "a", failing);
            target.Add(1, "b", healthy);

            var source = new XOrdered<int, string, FailItem>();

            source.Add(0, "a", new FailItem("A"));
            source.Add(1, "b", new FailItem("B"));

            Assert.Throws<InvalidOperationException>(() => target.CloneFrom(source));

            Assert.AreEqual(2, target.Count);
            Assert.AreEqual("old-a", failing.Name);
            Assert.AreEqual("B", healthy.Name);

            healthy.Fail = true;
            healthy.Name = "old-b";

            var aggregate = Assert.Throws<AggregateException>(() => target.CloneFrom(source));

            Assert.AreEqual(2, aggregate.InnerExceptions.Count);
        }

    #endregion

    #region K-3: 키 있는 XOrdered 규모와 타입

        // ------------------------------------------------------------
        /// <summary>
        /// 항목이 많아도 같은 Key는 재사용하고 새 Key는 복제한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_CollectionClone_XOrdered키있음_많은항목_재구성_재사용()
        {
            var target = new XOrdered<int, string, CloneItem>();
            var source = new XOrdered<int, string, CloneItem>();
            var keep = new CloneItem("keep", 0);

            for (var number = 0; number < 12; number++)
            {
                target.Add(number, $"key-{number}", number == 5 ? keep : new CloneItem($"old-{number}", number));
            }

            for (var number = 0; number < 14; number++)
            {
                source.Add(number, $"key-{number}", new CloneItem($"new-{number}", number * 10));
            }

            target.CloneFrom(source);

            Assert.AreEqual(14, target.Count);
            Assert.IsTrue(target.TryGetValue("key-5", out var kept));
            Assert.AreSame(keep, kept);
            Assert.AreEqual("new-5", keep.Name);
            Assert.IsTrue(target.TryGetValue("key-13", out var added));
            Assert.AreNotSame(source[13].Value, added);
            Assert.AreEqual("new-13", added.Name);

            for (var position = 0; position < 14; position++)
            {
                Assert.AreNotSame(source[position].Value, target[position].Value);
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 구체 타입이 달라도 복제한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_CollectionClone_XOrdered키있음_파생타입과도_복제()
        {
            var source = new XOrdered<int, string, CloneItem>();

            source.Add(0, "a", new CloneItem("a", 1));

            var derived = new DerivedKeyedOrdered();

            derived.CloneFrom(source);

            Assert.AreEqual(1, derived.Count);
            Assert.AreEqual("a", derived[0].Key);

            var back = new XOrdered<int, string, CloneItem>();

            back.CloneFrom(derived);

            Assert.AreEqual(1, back.Count);
            Assert.AreNotSame(derived[0].Value, back[0].Value);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Clone은 순서를 지킨 독립 복사본을 만든다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_CollectionClone_XOrdered키있음_Clone_독립복사본()
        {
            var source = new XOrdered<int, string, CloneItem>();

            source.Add(2, "late", new CloneItem("late", 2));
            source.Add(1, "early", new CloneItem("early", 1));

            var clone = source.Clone();

            Assert.AreEqual(2, clone.Count);
            Assert.AreEqual("early", clone[0].Key);
            Assert.AreEqual("late", clone[1].Key);
            Assert.AreNotSame(source[0].Value, clone[0].Value);
            Assert.AreEqual(1, clone[0].Value.Count);

            clone[0].Value.Count = 99;

            Assert.AreEqual(1, source[0].Value.Count);
        }

    #endregion

    #region D: XDictionary

        // ------------------------------------------------------------
        /// <summary>
        /// 값 형식 Value는 그대로 복사하고 기존 내용은 비운다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_CollectionClone_XDictionary_값형식_복사()
        {
            var source = new XDictionary_VV<string, int>();

            source["a"] = 1;
            source["b"] = 2;

            var target = new XDictionary_VV<string, int>();

            target["stale"] = 9;
            target.CloneFrom(source);

            Assert.AreEqual(2, target.Count);
            Assert.AreEqual(1, target["a"]);
            Assert.AreEqual(2, target["b"]);
            Assert.IsFalse(target.ContainsKey("stale"));

            // 복제본은 source와 독립이다.
            target["a"] = 100;

            Assert.AreEqual(1, source["a"]);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 참조 Value는 같은 Key를 재사용하고 새 Key는 복제한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_CollectionClone_XDictionary_참조값_재사용과_복제()
        {
            var keep = new CloneItem("keep", 1);
            var gone = new CloneItem("gone", 2);
            var target = new XDictionary_VR<string, CloneItem>();

            target["a"] = keep;
            target["x"] = gone;

            var source = new XDictionary_VR<string, CloneItem>();
            var sourceA = new CloneItem("A", 10);
            var sourceB = new CloneItem("B", 20);

            source["a"] = sourceA;
            source["b"] = sourceB;

            target.CloneFrom(source);

            Assert.AreEqual(2, target.Count);
            Assert.IsFalse(target.ContainsKey("x"));

            // 같은 Key는 기존 인스턴스에 덮어쓴다.
            Assert.AreSame(keep, target["a"]);
            Assert.AreEqual("A", keep.Name);
            Assert.AreEqual(10, keep.Count);

            // 새 Key는 복제하며 source 인스턴스를 공유하지 않는다.
            Assert.AreNotSame(sourceB, target["b"]);
            Assert.AreEqual("B", target["b"].Name);
            Assert.AreEqual(0, sourceA.CloneFromCalls);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// source가 가진 인스턴스는 재사용하지 않는다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_CollectionClone_XDictionary_source공유인스턴스_재사용안함()
        {
            var shared = new CloneItem("shared", 1);
            var target = new XDictionary_VR<string, CloneItem>();
            var source = new XDictionary_VR<string, CloneItem>();

            target["a"] = shared;
            source["a"] = new CloneItem("A", 10);
            source["z"] = shared;

            target.CloneFrom(source);

            Assert.AreNotSame(shared, target["a"]);
            Assert.AreEqual("A", target["a"].Name);
            Assert.AreEqual("shared", shared.Name);
            Assert.AreEqual(0, shared.CloneFromCalls);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 복제를 지원하지 않는 Value는 예외를 던지고 대상은 그대로다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_CollectionClone_XDictionary_복제불가값_대상불변()
        {
            var keep = new PlainItem("keep");
            var target = new XDictionary_VR<string, PlainItem>();

            target["keep"] = keep;

            var source = new XDictionary_VR<string, PlainItem>();

            source["x"] = new PlainItem("x");

            Assert.Throws<InvalidOperationException>(() => target.CloneFrom(source));

            Assert.AreEqual(1, target.Count);
            Assert.AreSame(keep, target["keep"]);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 같은 인스턴스가 여러 Key에 있어도 각각 따로 복제한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_CollectionClone_XDictionary_공유인스턴스_각각복제()
        {
            var shared = new CloneItem("shared", 1);
            var source = new XDictionary_VR<string, CloneItem>();

            source["a"] = shared;
            source["b"] = shared;

            var target = new XDictionary_VR<string, CloneItem>();

            target.CloneFrom(source);

            Assert.AreNotSame(target["a"], target["b"]);
            Assert.AreNotSame(shared, target["a"]);
            Assert.AreEqual("shared", target["b"].Name);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// null은 거절하고 자기 자신은 아무 일도 하지 않는다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_CollectionClone_XDictionary_거절조건과_자기복제()
        {
            var dictionary = new XDictionary_VV<string, int>();

            dictionary["a"] = 1;

            Assert.Throws<ArgumentNullException>(() => dictionary.CloneFrom(null));

            dictionary.CloneFrom(dictionary);

            Assert.AreEqual(1, dictionary.Count);
            Assert.AreEqual(1, dictionary["a"]);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// XDictionary는 일반 Dictionary와 양방향으로 복제한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_CollectionClone_XDictionary_일반Dictionary와_양방향_복제()
        {
            var plain = new Dictionary<string, int>();

            plain["a"] = 1;

            var x = new XDictionary_VV<string, int>();

            x["stale"] = 9;
            x.CloneFrom(plain);

            Assert.AreEqual(1, x.Count);
            Assert.AreEqual(1, x["a"]);

            x["b"] = 2;

            var back = new Dictionary<string, int>();

            back.CloneFrom(x);

            Assert.AreEqual(2, back.Count);
            Assert.AreEqual(2, back["b"]);
        }

    #endregion

    #region S: 집합·큐·스택·우선순위 큐

        // ------------------------------------------------------------
        /// <summary>
        /// HashSet은 값 형식 요소를 그대로 복사한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_CollectionClone_XHashSet_값형식_복사()
        {
            var source = new XHashSet_V<int>();

            source.Add(1);
            source.Add(2);

            var target = new XHashSet_V<int>();

            target.Add(9);
            target.CloneFrom(source);

            Assert.AreEqual(2, target.Count);
            Assert.IsTrue(target.Contains(1));
            Assert.IsTrue(target.Contains(2));
            Assert.IsFalse(target.Contains(9));
        }

        // ------------------------------------------------------------
        /// <summary>
        /// HashSet은 참조 요소를 복제하고 복제 불가면 대상을 그대로 둔다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_CollectionClone_XHashSet_참조요소_복제와_실패()
        {
            var source = new XHashSet_R<CloneItem>();
            var item = new CloneItem("item", 1);

            source.Add(item);

            var target = new XHashSet_R<CloneItem>();

            target.CloneFrom(source);

            Assert.AreEqual(1, target.Count);
            Assert.IsFalse(target.Contains(item));

            var plain = new XHashSet_R<PlainItem>();
            var keep = new PlainItem("keep");
            var plainTarget = new XHashSet_R<PlainItem>();

            plain.Add(new PlainItem("x"));
            plainTarget.Add(keep);

            Assert.Throws<InvalidOperationException>(() => plainTarget.CloneFrom(plain));

            Assert.AreEqual(1, plainTarget.Count);
            Assert.IsTrue(plainTarget.Contains(keep));
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Queue는 front 순서를 그대로 복사한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_CollectionClone_XQueue_front순서_유지()
        {
            var source = new XQueue_V<int>();

            source.Enqueue(1);
            source.Enqueue(2);
            source.Enqueue(3);

            var target = new XQueue_V<int>();

            target.Enqueue(9);
            target.CloneFrom(source);

            Assert.AreEqual(3, target.Count);
            Assert.AreEqual(1, target.Dequeue());
            Assert.AreEqual(2, target.Dequeue());
            Assert.AreEqual(3, target.Dequeue());
            Assert.AreEqual(3, source.Count);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Queue의 참조 요소는 순서를 지키며 새로 복제한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_CollectionClone_XQueue_참조요소_복제()
        {
            var source = new XQueue_R<CloneItem>();
            var first = new CloneItem("first", 1);

            source.Enqueue(first);
            source.Enqueue(new CloneItem("second", 2));

            var target = new XQueue_R<CloneItem>();

            target.CloneFrom(source);

            var clonedFirst = target.Dequeue();

            Assert.AreNotSame(first, clonedFirst);
            Assert.AreEqual("first", clonedFirst.Name);
            Assert.AreEqual("second", target.Dequeue().Name);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Stack은 top 순서를 그대로 복사한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_CollectionClone_XStack_top순서_유지()
        {
            var source = new XStack_V<int>();

            source.Push(1);
            source.Push(2);
            source.Push(3);

            var target = new XStack_V<int>();

            target.Push(9);
            target.CloneFrom(source);

            Assert.AreEqual(3, target.Count);
            Assert.AreEqual(3, target.Pop());
            Assert.AreEqual(2, target.Pop());
            Assert.AreEqual(1, target.Pop());
            Assert.AreEqual(3, source.Count);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Stack의 참조 요소는 순서를 지키며 새로 복제한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_CollectionClone_XStack_참조요소_복제()
        {
            var source = new XStack_R<CloneItem>();
            var bottom = new CloneItem("bottom", 1);
            var top = new CloneItem("top", 2);

            source.Push(bottom);
            source.Push(top);

            var target = new XStack_R<CloneItem>();

            target.CloneFrom(source);

            var clonedTop = target.Pop();
            var clonedBottom = target.Pop();

            Assert.AreNotSame(top, clonedTop);
            Assert.AreEqual("top", clonedTop.Name);
            Assert.AreNotSame(bottom, clonedBottom);
            Assert.AreEqual("bottom", clonedBottom.Name);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 우선순위 큐는 Dequeue 순서를 지키며 요소를 복제한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_CollectionClone_XPriorityQueue_순서유지_요소복제()
        {
            var source = new XPriorityQueue<CloneItem, int>();
            var high = new CloneItem("high", 3);

            source.Enqueue(new CloneItem("low", 1), 1);
            source.Enqueue(high, 3);
            source.Enqueue(new CloneItem("mid", 2), 2);

            var target = new XPriorityQueue<CloneItem, int>();

            target.Enqueue(new CloneItem("old", 0), 9);
            target.CloneFrom(source);

            Assert.AreEqual(3, target.Count);

            var first = target.Dequeue();

            Assert.AreEqual("high", first.Element.Name);
            Assert.AreEqual(3, first.Priority);
            Assert.AreNotSame(high, first.Element);
            Assert.AreEqual("mid", target.Dequeue().Element.Name);
            Assert.AreEqual("low", target.Dequeue().Element.Name);
            Assert.AreEqual(3, source.Count);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 우선순위 큐는 복제 불가 요소에서 예외를 던지고 대상은 그대로다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_CollectionClone_XPriorityQueue_복제불가요소_대상불변()
        {
            var source = new XPriorityQueue<PlainItem, int>();

            source.Enqueue(new PlainItem("x"), 1);

            var keep = new PlainItem("keep");
            var target = new XPriorityQueue<PlainItem, int>();

            target.Enqueue(keep, 1);

            Assert.Throws<InvalidOperationException>(() => target.CloneFrom(source));

            Assert.AreEqual(1, target.Count);
            Assert.AreSame(keep, target.Peek().Element);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// XHashSet·XQueue·XStack은 일반 컬렉션에서 복제한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_CollectionClone_X컬렉션_일반컬렉션에서_복제()
        {
            var set = new HashSet<int>();

            set.Add(1);

            var xSet = new XHashSet_V<int>();

            xSet.CloneFrom(set);

            Assert.IsTrue(xSet.Contains(1));

            var queue = new Queue<int>();

            queue.Enqueue(1);
            queue.Enqueue(2);

            var xQueue = new XQueue_V<int>();

            xQueue.CloneFrom(queue);

            Assert.AreEqual(1, xQueue.Dequeue());

            var stack = new Stack<int>();

            stack.Push(1);
            stack.Push(2);

            var xStack = new XStack_V<int>();

            xStack.CloneFrom(stack);

            Assert.AreEqual(2, xStack.Pop());
        }

    #endregion

    #region B: BCL 컬렉션 확장 메서드

        // ------------------------------------------------------------
        /// <summary>
        /// 일반 Dictionary도 값 형식 Value를 그대로 복사한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_CollectionClone_Dictionary_값형식_복사()
        {
            var source = new Dictionary<string, int>();

            source["a"] = 1;
            source["b"] = 2;

            var target = new Dictionary<string, int>();

            target["stale"] = 9;
            target.CloneFrom(source);

            Assert.AreEqual(2, target.Count);
            Assert.AreEqual(2, target["b"]);
            Assert.IsFalse(target.ContainsKey("stale"));
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// 일반 Dictionary의 참조 Value는 같은 Key를 재사용하고 새 Key는 복제한다.
        /// </summary>
        // ----------------------------------------------------------------------
        [Test]
        public void TEST_CollectionClone_Dictionary_참조값_재사용과_복제()
        {
            var keep = new CloneItem("keep", 1);
            var target = new Dictionary<string, CloneItem>();

            target["a"] = keep;

            var source = new Dictionary<string, CloneItem>();

            source["a"] = new CloneItem("A", 10);
            source["b"] = new CloneItem("B", 20);

            target.CloneFrom(source);

            Assert.AreSame(keep, target["a"]);
            Assert.AreEqual("A", keep.Name);
            Assert.AreNotSame(source["b"], target["b"]);
            Assert.AreEqual("B", target["b"].Name);
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// 일반 Dictionary는 복제 불가 Value에서 예외를 던지고 대상은 그대로다.
        /// </summary>
        // ----------------------------------------------------------------------
        [Test]
        public void TEST_CollectionClone_Dictionary_복제불가값_대상불변()
        {
            var keep = new PlainItem("keep");
            var target = new Dictionary<string, PlainItem>();

            target["keep"] = keep;

            var source = new Dictionary<string, PlainItem>();

            source["x"] = new PlainItem("x");

            Assert.Throws<InvalidOperationException>(() => target.CloneFrom(source));

            Assert.AreEqual(1, target.Count);
            Assert.AreSame(keep, target["keep"]);
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// 일반 컬렉션은 null source를 거절하고 자기 자신은 아무 일도 하지 않는다.
        /// </summary>
        // ----------------------------------------------------------------------
        [Test]
        public void TEST_CollectionClone_BCL_거절조건과_자기복제()
        {
            var list = new List<int>();

            list.Add(1);
            list.Add(2);

            Assert.Throws<ArgumentNullException>(() => list.CloneFrom(null));
            Assert.Throws<ArgumentNullException>(() => ((List<int>)null).CloneFrom(list));

            list.CloneFrom(list);

            Assert.AreEqual(2, list.Count);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 일반 HashSet과 Queue는 값 형식 요소를 그대로 복사한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_CollectionClone_HashSet과Queue_값형식_복사()
        {
            var set = new HashSet<int>();
            var setTarget = new HashSet<int>();

            set.Add(1);
            set.Add(2);
            setTarget.Add(9);
            setTarget.CloneFrom(set);

            Assert.AreEqual(2, setTarget.Count);
            Assert.IsFalse(setTarget.Contains(9));

            var queue = new Queue<int>();

            queue.Enqueue(1);
            queue.Enqueue(2);

            var queueTarget = new Queue<int>();

            queueTarget.Enqueue(9);
            queueTarget.CloneFrom(queue);

            Assert.AreEqual(1, queueTarget.Dequeue());
            Assert.AreEqual(2, queueTarget.Dequeue());
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 일반 Stack은 top 순서를 유지하고 참조 요소는 복제한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_CollectionClone_Stack_top순서와_복제()
        {
            var source = new Stack<CloneItem>();
            var bottom = new CloneItem("bottom", 1);

            source.Push(bottom);
            source.Push(new CloneItem("top", 2));

            var target = new Stack<CloneItem>();

            target.CloneFrom(source);

            Assert.AreEqual("top", target.Pop().Name);

            var clonedBottom = target.Pop();

            Assert.AreEqual("bottom", clonedBottom.Name);
            Assert.AreNotSame(bottom, clonedBottom);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 일반 List는 순서를 유지하고 참조 요소를 복제한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_CollectionClone_List_순서유지와_복제()
        {
            var source = new List<CloneItem>();
            var target = new List<CloneItem>();

            source.Add(new CloneItem("first", 1));
            source.Add(new CloneItem("second", 2));
            target.Add(new CloneItem("old", 0));
            target.CloneFrom(source);

            Assert.AreEqual(2, target.Count);
            Assert.AreEqual("first", target[0].Name);
            Assert.AreEqual("second", target[1].Name);
            Assert.AreNotSame(source[0], target[0]);

            var values = new List<int>();
            var valuesTarget = new List<int>();

            values.Add(3);
            values.Add(4);
            valuesTarget.Add(9);
            valuesTarget.CloneFrom(values);

            Assert.AreEqual(2, valuesTarget.Count);
            Assert.AreEqual(4, valuesTarget[1]);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 일반 List는 복제 불가 요소에서 예외를 던지고 대상은 그대로다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_CollectionClone_List_복제불가요소_대상불변()
        {
            var keep = new PlainItem("keep");
            var target = new List<PlainItem>();
            var source = new List<PlainItem>();

            target.Add(keep);
            source.Add(new PlainItem("x"));

            Assert.Throws<InvalidOperationException>(() => target.CloneFrom(source));

            Assert.AreEqual(1, target.Count);
            Assert.AreSame(keep, target[0]);
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// UnityEngine.Object와 delegate 요소는 공유하고 예외를 던지지 않는다.
        /// </summary>
        // ----------------------------------------------------------------------
        [Test]
        public void TEST_CollectionClone_Unity참조와_delegate_공유()
        {
            var asset = ScriptableObject.CreateInstance<ScriptableObject>();

            try
            {
                var assets = new List<ScriptableObject>();
                var assetsTarget = new List<ScriptableObject>();

                assets.Add(asset);
                assetsTarget.CloneFrom(assets);

                Assert.AreSame(asset, assetsTarget[0]);

                var map = new Dictionary<string, ScriptableObject>();
                var mapTarget = new Dictionary<string, ScriptableObject>();

                map["a"] = asset;
                mapTarget.CloneFrom(map);

                Assert.AreSame(asset, mapTarget["a"]);

                Action callback = Noop;
                var callbacks = new List<Action>();
                var callbacksTarget = new List<Action>();

                callbacks.Add(callback);
                callbacksTarget.CloneFrom(callbacks);

                Assert.AreSame(callback, callbacksTarget[0]);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(asset);
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// object 요소의 박싱된 값 형식·string·Type·delegate는 공유한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_CollectionClone_object요소_공유값_복제()
        {
            Action callback = Noop;
            var source = new List<object>();

            source.Add(1);
            source.Add("text");
            source.Add(typeof(int));
            source.Add(callback);
            source.Add(null);

            var target = new List<object>();

            target.CloneFrom(source);

            Assert.AreEqual(5, target.Count);
            Assert.AreEqual(1, target[0]);
            Assert.AreSame(source[1], target[1]);
            Assert.AreSame(typeof(int), target[2]);
            Assert.AreSame(callback, target[3]);
            Assert.IsNull(target[4]);
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> 재사용 Value의 CloneFrom이 실패해도 나머지를 끝까지 복사하고
        /// <br/> Key 구성을 source에 맞춘 뒤 예외를 던진다.
        /// </summary>
        // ----------------------------------------------------------------------
        [Test]
        public void TEST_CollectionClone_Dictionary_재사용복사실패_나머지진행_구성맞춤()
        {
            var failing = new FailItem("old-a", true);
            var healthy = new FailItem("old-b");
            var target = new Dictionary<string, FailItem>();

            target["a"] = failing;
            target["b"] = healthy;
            target["gone"] = new FailItem("gone");

            var source = new Dictionary<string, FailItem>();
            var sourceC = new FailItem("C");

            source["a"] = new FailItem("A");
            source["b"] = new FailItem("B");
            source["c"] = sourceC;

            Assert.Throws<InvalidOperationException>(() => target.CloneFrom(source));

            Assert.AreEqual(3, target.Count);
            Assert.IsFalse(target.ContainsKey("gone"));
            Assert.AreSame(failing, target["a"]);
            Assert.AreEqual("old-a", failing.Name);
            Assert.AreSame(healthy, target["b"]);
            Assert.AreEqual("B", healthy.Name);
            Assert.AreNotSame(sourceC, target["c"]);
            Assert.AreEqual("C", target["c"].Name);
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// 재사용 Value의 실패가 여럿이면 AggregateException으로 모아 던진다.
        /// </summary>
        // ----------------------------------------------------------------------
        [Test]
        public void TEST_CollectionClone_Dictionary_재사용복사실패가여럿이면Aggregate()
        {
            var target = new Dictionary<string, FailItem>();

            target["a"] = new FailItem("old-a", true);
            target["b"] = new FailItem("old-b", true);

            var source = new Dictionary<string, FailItem>();

            source["a"] = new FailItem("A");
            source["b"] = new FailItem("B");

            var aggregate = Assert.Throws<AggregateException>(() => target.CloneFrom(source));

            Assert.AreEqual(2, aggregate.InnerExceptions.Count);
            Assert.AreEqual(2, target.Count);
        }

    #endregion

    #region A: 할당

        // ------------------------------------------------------------
        /// <summary>
        /// 같은 구성의 키 없는 XOrdered 복제는 할당하지 않는다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_CollectionClone_XOrdered키없음_같은구성_할당없음()
        {
            var live = new XOrdered<int, CloneItem>();
            var snapshot = new XOrdered<int, CloneItem>();
            var item = new CloneItem("a", 0);

            live.Add(0, item);
            snapshot.Add(0, new CloneItem("a", 0));

            void RoundTrip()
            {
                item.Count++;
                snapshot.CloneFrom(live);
                live.CloneFrom(snapshot);
            }

            RoundTrip();

            Assert.That(RoundTrip, Is.Not.AllocatingGCMemory());
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 같은 구성의 키 있는 XOrdered 복제는 할당하지 않는다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_CollectionClone_XOrdered키있음_같은구성_할당없음()
        {
            var live = new XOrdered<int, string, CloneItem>();
            var snapshot = new XOrdered<int, string, CloneItem>();
            var item = new CloneItem("a", 0);

            live.Add(0, "a", item);
            snapshot.Add(0, "a", new CloneItem("a", 0));

            void RoundTrip()
            {
                item.Count++;
                snapshot.CloneFrom(live);
                live.CloneFrom(snapshot);
            }

            RoundTrip();

            Assert.That(RoundTrip, Is.Not.AllocatingGCMemory());
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// 같은 Key의 참조 Value를 재사용하는 Dictionary 복제는 할당하지 않는다.
        /// </summary>
        // ----------------------------------------------------------------------
        [Test]
        public void TEST_CollectionClone_Dictionary_같은Key재사용_할당없음()
        {
            var live = new Dictionary<string, CloneItem>();
            var snapshot = new Dictionary<string, CloneItem>();
            var item = new CloneItem("a", 0);

            live["a"] = item;
            live["b"] = new CloneItem("b", 0);
            snapshot["a"] = new CloneItem("a", 0);
            snapshot["b"] = new CloneItem("b", 0);

            void RoundTrip()
            {
                item.Count++;
                snapshot.CloneFrom(live);
                live.CloneFrom(snapshot);
            }

            RoundTrip();

            Assert.That(RoundTrip, Is.Not.AllocatingGCMemory());
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 값 형식 요소의 컬렉션 복제는 반복해도 할당하지 않는다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_CollectionClone_값형식_컬렉션_할당없음()
        {
            var dictionary = new Dictionary<string, int>();
            var dictionaryTarget = new Dictionary<string, int>();
            var set = new HashSet<int>();
            var setTarget = new HashSet<int>();
            var queue = new Queue<int>();
            var queueTarget = new Queue<int>();
            var stack = new Stack<int>();
            var stackTarget = new Stack<int>();
            var list = new List<int>();
            var listTarget = new List<int>();

            dictionary["a"] = 1;
            dictionary["b"] = 2;
            set.Add(1);
            set.Add(2);
            queue.Enqueue(1);
            stack.Push(1);
            list.Add(1);
            list.Add(2);

            void RoundTrip()
            {
                dictionaryTarget.CloneFrom(dictionary);
                setTarget.CloneFrom(set);
                queueTarget.CloneFrom(queue);
                stackTarget.CloneFrom(stack);
                listTarget.CloneFrom(list);
            }

            // 첫 호출의 용량 확보와 풀 준비를 측정에서 뺀다.
            RoundTrip();

            Assert.That(RoundTrip, Is.Not.AllocatingGCMemory());
        }

    #endregion

    }
}
