/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TEST_EventHelper.cs
수정일 : 2026-11-25

# 설명
EventHelper의 구독자 목록(Handlers)과 InvokeEach의 호출 순서, 실패 격리, 구독 변경 시점, null 처리와 할당 계약을 검증한다.
Unity Test Runner (Edit Mode) 에서 실행한다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

using NUnit;
using NUnit.Framework;

namespace inonego.Xeri.TEST.Utility._EventHelper
{
    using inonego;
    using inonego.Xeri;

    // ============================================================
    /// <summary>
    /// EventHelper 구독자 목록·InvokeEach 계약 테스트.
    /// </summary>
    // ============================================================
    public class TEST_EventHelper
    {

    #region V: 호출

        // ------------------------------------------------------------
        /// <summary>
        /// 구독자를 등록 순서대로 호출하고 인자를 그대로 넘긴다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_EventHelper_InvokeEach_등록순서로호출하고인자를전달()
        {
            var log = new List<string>();
            var failures = new ExceptionBuffer();

            Action none = () => log.Add("0:a");
            none += () => log.Add("0:b");

            Action<int> one = value => log.Add("1:a" + value);
            one += value => log.Add("1:b" + value);

            Action<int, string> two = (a, b) => log.Add("2:" + a + b);
            Action<int, string, bool> three = (a, b, c) => log.Add("3:" + a + b + c);

            none.InvokeEach(ref failures);
            one.InvokeEach(7, ref failures);
            two.InvokeEach(1, "x", ref failures);
            three.InvokeEach(2, "y", true, ref failures);

            var expected = new[]
            {
                "0:a",
                "0:b",
                "1:a7",
                "1:b7",
                "2:1x",
                "3:2yTrue"
            };

            CollectionAssert.AreEqual(expected, log);
            Assert.AreEqual(0, failures.Count);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// null 이벤트는 아무것도 하지 않고 실패도 만들지 않는다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_EventHelper_InvokeEach_null이벤트는무시()
        {
            var failures = new ExceptionBuffer();
            Action none = null;
            Action<int> one = null;
            Action<int, int> two = null;
            Action<int, int, int> three = null;

            none.InvokeEach(ref failures);
            one.InvokeEach(1, ref failures);
            two.InvokeEach(1, 2, ref failures);
            three.InvokeEach(1, 2, 3, ref failures);

            Assert.AreEqual(0, failures.Count);
        }

    #endregion

    #region V: 실패 격리

        // ------------------------------------------------------------
        /// <summary>
        /// 구독자가 실패해도 나머지를 모두 호출하고 실패를 순서대로 모은다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_EventHelper_InvokeEach_실패해도나머지를호출하고실패를수집()
        {
            var log = new List<string>();
            var failures = new ExceptionBuffer();
            var first = new InvalidOperationException("first");
            var second = new ArgumentException("second");

            Action<int> handlers = value => log.Add("a");
            handlers += value => throw first;
            handlers += value => log.Add("c");
            handlers += value => throw second;
            handlers += value => log.Add("e");

            handlers.InvokeEach(1, ref failures);

            var expected = new[]
            {
                "a",
                "c",
                "e"
            };

            CollectionAssert.AreEqual(expected, log);
            Assert.AreEqual(2, failures.Count);
            Assert.AreSame(first, failures.Exceptions[0]);
            Assert.AreSame(second, failures.Exceptions[1]);
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// 같은 버퍼를 여러 번 넘기면 실패가 계속 쌓이고 호출자가 한 번에 던질 수 있다.
        /// </summary>
        // ----------------------------------------------------------------------
        [Test]
        public void TEST_EventHelper_InvokeEach_같은버퍼에여러이벤트의실패를누적()
        {
            var failures = new ExceptionBuffer();
            Action a = () => throw new InvalidOperationException("a");
            Action<int> b = value => throw new InvalidOperationException("b");

            a.InvokeEach(ref failures);
            b.InvokeEach(1, ref failures);

            Assert.AreEqual(2, failures.Count);
            Assert.Throws<AggregateException>(() => failures.ThrowWhenAny("복수 실패"));
        }

    #endregion

    #region V: 구독 변경

        // ----------------------------------------------------------------------
        /// <summary>
        /// 호출 중 구독을 풀거나 추가해도 이번 호출은 시작 시점의 목록으로 끝까지 진행한다.
        /// </summary>
        // ----------------------------------------------------------------------
        [Test]
        public void TEST_EventHelper_InvokeEach_호출중구독변경은이번호출에영향없음()
        {
            var log = new List<string>();
            var failures = new ExceptionBuffer();
            Action<int> late = value => log.Add("late");
            Action<int> removed = value => log.Add("removed");
            Action<int> handlers = null;

            handlers = value =>
            {
                log.Add("a");
                handlers -= removed;
                handlers += late;
            };
            handlers += removed;

            handlers.InvokeEach(1, ref failures);

            var first = new[]
            {
                "a",
                "removed"
            };

            CollectionAssert.AreEqual(first, log);

            log.Clear();
            handlers.InvokeEach(1, ref failures);

            var second = new[]
            {
                "a",
                "late"
            };

            CollectionAssert.AreEqual(second, log);
            Assert.AreEqual(0, failures.Count);
        }

        // --------------------------------------------------------------------------------
        /// <summary>
        /// 구독을 바꾸면 이후 호출은 새 구성을 반영하고 이전 구성의 델리게이트는 그대로 동작한다.
        /// </summary>
        // --------------------------------------------------------------------------------
        [Test]
        public void TEST_EventHelper_InvokeEach_구독변경후새구성을반영()
        {
            var log = new List<string>();
            var failures = new ExceptionBuffer();
            Action<int> a = value => log.Add("a");
            Action<int> b = value => log.Add("b");
            Action<int> before = a;
            before += b;

            before.InvokeEach(1, ref failures);

            var after = before;
            after -= a;

            after.InvokeEach(1, ref failures);
            before.InvokeEach(1, ref failures);

            var expected = new[]
            {
                "a",
                "b",
                "b",
                "a",
                "b"
            };

            CollectionAssert.AreEqual(expected, log);
        }

    #endregion

    #region V: 할당

        // ------------------------------------------------------------
        /// <summary>
        /// 구독이 그대로면 두 번째 호출부터는 힙을 할당하지 않는다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_EventHelper_InvokeEach_구독이그대로면할당하지않음()
        {
            var count = 0;
            var failures = new ExceptionBuffer();
            Action<int> handlers = value => count += value;
            handlers += value => count += value;
            handlers += value => count += value;

            // 계측이 실제로 할당을 잡는지 먼저 확인한다.
            var leaked = new List<object>();
            Assert.That(() => leaked.Add(new object()), new UnityEngine.TestTools.Constraints.AllocatingGCMemoryConstraint());

            // 첫 호출은 호출 목록을 만들 수 있으므로 계측 전에 한 번 호출한다.
            handlers.InvokeEach(1, ref failures);

            Assert.That
            (
                () =>
                {
                    var local = new ExceptionBuffer();
                    handlers.InvokeEach(1, ref local);
                },
                UnityEngine.TestTools.Constraints.ConstraintExtensions.AllocatingGCMemory(Is.Not)
            );

            Assert.AreEqual(0, failures.Count);
            Assert.Greater(count, 3);
        }

    #endregion

    #region V: 구독자 목록

        private delegate void CustomHandler(string name, int value);

        // --------------------------------------------------------------------------------
        /// <summary>
        /// 구독자를 등록 순서대로 타입 있는 View로 돌려주고 null 이벤트는 빈 View를 돌려준다.
        /// </summary>
        // --------------------------------------------------------------------------------
        [Test]
        public void TEST_EventHelper_Handlers_등록순서로돌려주고null은빈View()
        {
            var log = new List<string>();
            Action<int> handlers = value => log.Add("a" + value);
            handlers += value => log.Add("b" + value);
            Action<int> none = null;

            var view = handlers.Handlers();

            Assert.AreEqual(2, view.Count);

            foreach (var handler in view)
            {
                handler(1);
            }

            var expected = new[]
            {
                "a1",
                "b1"
            };

            CollectionAssert.AreEqual(expected, log);
            Assert.AreEqual(0, none.Handlers().Count);

            var visited = 0;

            foreach (var handler in none.Handlers())
            {
                visited++;
            }

            Assert.AreEqual(0, visited);
        }

        // --------------------------------------------------------------------------------
        /// <summary>
        /// 받은 View는 이후 구독이 바뀌어도 바뀌지 않고 새로 받은 View만 새 구성을 반영한다.
        /// </summary>
        // --------------------------------------------------------------------------------
        [Test]
        public void TEST_EventHelper_Handlers_받은View는구독변경에영향없음()
        {
            Action<int> a = value => { };
            Action<int> b = value => { };
            Action<int> handlers = a;
            handlers += b;

            var before = handlers.Handlers();
            handlers -= a;
            var after = handlers.Handlers();

            Assert.AreEqual(2, before.Count);
            Assert.AreEqual(1, after.Count);
            Assert.AreEqual(b, after[0]);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 전용 delegate도 같은 방식으로 구독자를 돌려준다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_EventHelper_Handlers_전용delegate도구독자를돌려줌()
        {
            var log = new List<string>();

            CustomHandler custom = (name, value) => log.Add("custom:" + name + value);
            custom += (name, value) => log.Add("custom2:" + name + value);

            foreach (var handler in custom.Handlers())
            {
                handler("x", 1);
            }

            var expected = new[]
            {
                "custom:x1",
                "custom2:x1"
            };

            CollectionAssert.AreEqual(expected, log);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 인자 넷까지 전달하며 호출한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_EventHelper_InvokeEach_인자넷까지전달()
        {
            var log = new List<string>();
            var failures = new ExceptionBuffer();
            Action<int, int, int, int> four = (a, b, c, d) => log.Add("" + a + b + c + d);

            four.InvokeEach(1, 2, 3, 4, ref failures);

            Assert.AreEqual(1, log.Count);
            Assert.AreEqual("1234", log[0]);
            Assert.AreEqual(0, failures.Count);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 구독이 그대로면 Handlers의 foreach는 힙을 할당하지 않는다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_EventHelper_Handlers_구독이그대로면foreach가할당하지않음()
        {
            var count = 0;
            Action<int> handlers = value => count += value;
            handlers += value => count += value;

            // 첫 호출은 목록을 만들 수 있으므로 계측 전에 한 번 받는다.
            foreach (var handler in handlers.Handlers())
            {
                handler(1);
            }

            Assert.That
            (
                () =>
                {
                    foreach (var handler in handlers.Handlers())
                    {
                        handler(1);
                    }
                },
                UnityEngine.TestTools.Constraints.ConstraintExtensions.AllocatingGCMemory(Is.Not)
            );

            Assert.Greater(count, 2);
        }

    #endregion

    #region V: EventHandler 계열

        // ----------------------------------------------------------------------
        /// <summary>
        /// EventHandler와 EventHandler&lt;T&gt;도 sender와 인자를 넘기며 호출한다.
        /// </summary>
        // ----------------------------------------------------------------------
        [Test]
        public void TEST_EventHelper_InvokeEach_EventHandler계열은_sender와인자를전달()
        {
            var log = new List<string>();
            var failures = new ExceptionBuffer();
            var sender = new object();

            EventHandler plain = (s, e) => log.Add(ReferenceEquals(s, sender) ? "plain" : "wrong");
            EventHandler<string> generic = (s, e) => log.Add(ReferenceEquals(s, sender) ? "generic:" + e : "wrong");

            plain.InvokeEach(sender, EventArgs.Empty, ref failures);
            generic.InvokeEach(sender, "x", ref failures);

            var expected = new[]
            {
                "plain",
                "generic:x"
            };

            CollectionAssert.AreEqual(expected, log);
            Assert.AreEqual(0, failures.Count);
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// EventHandler 계열도 구독자가 실패해도 나머지를 호출하고 null은 무시한다.
        /// </summary>
        // ----------------------------------------------------------------------
        [Test]
        public void TEST_EventHelper_InvokeEach_EventHandler계열도_실패를격리하고_null은무시()
        {
            var log = new List<string>();
            var failures = new ExceptionBuffer();
            var error = new InvalidOperationException("fail");

            EventHandler handlers = (s, e) => throw error;
            handlers += (s, e) => log.Add("after");
            EventHandler<int> none = null;

            handlers.InvokeEach(null, EventArgs.Empty, ref failures);
            none.InvokeEach(null, 1, ref failures);

            var expected = new[]
            {
                "after"
            };

            CollectionAssert.AreEqual(expected, log);
            Assert.AreEqual(1, failures.Count);
            Assert.AreSame(error, failures.Exceptions[0]);
        }

    #endregion

    #region V: 어댑터 람다 오버로드

        private delegate void OneHandler(int value);

        // ----------------------------------------------------------------------
        /// <summary>
        /// 사용자 정의 델리게이트도 어댑터 람다로 등록 순서대로 호출하고 인자를 전달한다.
        /// </summary>
        // ----------------------------------------------------------------------
        [Test]
        public void TEST_EventHelper_InvokeEach_어댑터람다는_사용자정의델리게이트를호출()
        {
            var log = new List<string>();
            var failures = new ExceptionBuffer();

            OneHandler one = value => log.Add("one:" + value);
            one += value => log.Add("one2:" + value);

            CustomHandler two = (name, value) => log.Add("two:" + name + value);
            two += (name, value) => log.Add("two2:" + name + value);

            one.InvokeEach(7, static (each, value) => each(value), ref failures);
            two.InvokeEach("x", 1, static (each, name, value) => each(name, value), ref failures);

            var expected = new[]
            {
                "one:7",
                "one2:7",
                "two:x1",
                "two2:x1"
            };

            CollectionAssert.AreEqual(expected, log);
            Assert.AreEqual(0, failures.Count);
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> 어댑터 람다 오버로드도 구독자 실패를 격리하고 null 이벤트를 무시한다.
        /// <br/> 호출 방법이 null이면 거절한다.
        /// </summary>
        // ----------------------------------------------------------------------
        [Test]
        public void TEST_EventHelper_InvokeEach_어댑터람다도_실패를격리하고_null은무시()
        {
            var log = new List<string>();
            var failures = new ExceptionBuffer();
            var error = new InvalidOperationException("fail");

            CustomHandler handlers = (name, value) => throw error;
            handlers += (name, value) => log.Add("after");
            CustomHandler none = null;

            handlers.InvokeEach("x", 1, static (each, name, value) => each(name, value), ref failures);
            none.InvokeEach("x", 1, static (each, name, value) => each(name, value), ref failures);

            var expected = new[]
            {
                "after"
            };

            CollectionAssert.AreEqual(expected, log);
            Assert.AreEqual(1, failures.Count);
            Assert.AreSame(error, failures.Exceptions[0]);

            var local = new ExceptionBuffer();
            Assert.Throws<ArgumentNullException>
            (
                () => handlers.InvokeEach("x", 1, (Action<CustomHandler, string, int>)null, ref local)
            );
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// static 람다를 쓰면 구독이 그대로인 두 번째 호출부터 힙을 할당하지 않는다.
        /// </summary>
        // ----------------------------------------------------------------------
        [Test]
        public void TEST_EventHelper_InvokeEach_어댑터람다는_static이면할당하지않음()
        {
            var count = 0;
            CustomHandler handlers = (name, value) => count += value;
            handlers += (name, value) => count += value;

            // 계측이 실제로 할당을 잡는지 먼저 확인한다.
            var leaked = new List<object>();
            Assert.That(() => leaked.Add(new object()), new UnityEngine.TestTools.Constraints.AllocatingGCMemoryConstraint());

            TestDelegate call = () =>
            {
                var local = new ExceptionBuffer();
                handlers.InvokeEach("x", 1, static (each, name, value) => each(name, value), ref local);
            };

            // 첫 호출은 구독자 목록과 static 람다의 캐시를 만들 수 있으므로 같은 호출을 계측 전에 한 번 실행한다.
            call();

            Assert.That(call, UnityEngine.TestTools.Constraints.ConstraintExtensions.AllocatingGCMemory(Is.Not));

            Assert.Greater(count, 2);
        }


        private delegate void ZeroHandler();

        private delegate void ThreeHandler(int a, int b, int c);

        private delegate void FourHandler(int a, int b, int c, int d);

        // ------------------------------------------------------------
        /// <summary>
        /// 어댑터 람다 오버로드는 인자 0개와 3개와 4개 델리게이트도 호출한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_EventHelper_InvokeEach_어댑터람다는_인자0개와3개와4개도지원()
        {
            var log = new List<string>();
            var failures = new ExceptionBuffer();

            ZeroHandler zero = () => log.Add("zero");
            ThreeHandler three = (a, b, c) => log.Add("three:" + a + b + c);
            FourHandler four = (a, b, c, d) => log.Add("four:" + a + b + c + d);

            zero.InvokeEach(static each => each(), ref failures);
            three.InvokeEach(1, 2, 3, static (each, a, b, c) => each(a, b, c), ref failures);
            four.InvokeEach(1, 2, 3, 4, static (each, a, b, c, d) => each(a, b, c, d), ref failures);

            var expected = new[]
            {
                "zero",
                "three:123",
                "four:1234"
            };

            CollectionAssert.AreEqual(expected, log);
            Assert.AreEqual(0, failures.Count);
        }

    #endregion

    }
}
