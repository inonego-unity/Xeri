/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TEST_ExceptionBuffer.cs
수정일 : 2026-11-25

# 설명
ExceptionBuffer 의 수집 순서, Exceptions View, ThrowWhenAny·ToException 의 규칙(없음/하나/여럿)과 LogAll 을 검증한다.

# 테스트 구성
 A: 수집·Exceptions
 T: ThrowWhenAny·ToException
 L: LogAll
========================================================================= BLOCK_HEADER_END */

using System;

using NUnit;
using NUnit.Framework;

namespace inonego.Xeri.TEST.Core._ExceptionBuffer
{
    // ============================================================
    /// <summary>
    /// ExceptionBuffer 수집과 던지기 규칙 테스트.
    /// </summary>
    // ============================================================
    public class TEST_ExceptionBuffer
    {

    #region A-1: 수집

        // ------------------------------------------------------------
        /// <summary>
        /// 비어 있는 버퍼는 개수 0과 빈 목록을 제공한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_ExceptionBuffer_비어있으면_개수0과_빈목록()
        {
            var buffer = new ExceptionBuffer();

            Assert.AreEqual(0, buffer.Count);
            Assert.AreEqual(0, buffer.Exceptions.Count);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Add는 순서대로, Prepend는 맨 앞에 보관한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_ExceptionBuffer_Add는_순서대로_Prepend는_맨앞에_보관()
        {
            var first = new InvalidOperationException("first");
            var second = new InvalidOperationException("second");
            var prepended = new InvalidOperationException("prepended");
            var buffer = new ExceptionBuffer();

            buffer.Add(first);
            buffer.Add(second);
            buffer.Prepend(prepended);

            Assert.AreEqual(3, buffer.Count);
            Assert.AreSame(prepended, buffer.Exceptions[0]);
            Assert.AreSame(first, buffer.Exceptions[1]);
            Assert.AreSame(second, buffer.Exceptions[2]);
        }

    #endregion

    #region T-1: ThrowWhenAny

        // ------------------------------------------------------------
        /// <summary>
        /// 비어 있는 버퍼의 ThrowWhenAny는 던지지 않는다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_ExceptionBuffer_ThrowWhenAny는_비어있으면_던지지_않음()
        {
            var buffer = new ExceptionBuffer();

            Assert.DoesNotThrow(() => buffer.ThrowWhenAny("실패"));
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 하나뿐이면 같은 예외 인스턴스를 던진다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_ExceptionBuffer_ThrowWhenAny는_하나면_원래예외를_던짐()
        {
            var original = new InvalidOperationException("original");
            var buffer = new ExceptionBuffer();
            buffer.Add(original);

            var thrown = Assert.Throws<InvalidOperationException>(() => buffer.ThrowWhenAny("실패"));

            Assert.AreSame(original, thrown);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 하나뿐이면 원래 스택 추적이 유지된다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_ExceptionBuffer_ThrowWhenAny는_하나면_원래스택추적을_유지()
        {
            var buffer = new ExceptionBuffer();
            buffer.Add(CaptureFromSource());

            var thrown = Assert.Throws<InvalidOperationException>(() => buffer.ThrowWhenAny("실패"));

            StringAssert.Contains(nameof(ThrowFromSource), thrown.StackTrace);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 둘 이상이면 순서를 지켜 AggregateException으로 묶는다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_ExceptionBuffer_ThrowWhenAny는_여럿이면_AggregateException으로_묶음()
        {
            var first = new InvalidOperationException("first");
            var second = new ArgumentException("second");
            var buffer = new ExceptionBuffer();
            buffer.Add(first);
            buffer.Add(second);

            var thrown = Assert.Throws<AggregateException>(() => buffer.ThrowWhenAny("실패"));

            StringAssert.StartsWith("실패", thrown.Message);
            Assert.AreEqual(2, thrown.InnerExceptions.Count);
            Assert.AreSame(first, thrown.InnerExceptions[0]);
            Assert.AreSame(second, thrown.InnerExceptions[1]);
        }

    #endregion

    #region A-2: Exceptions

        // ------------------------------------------------------------
        /// <summary>
        /// Exceptions는 등록 순서대로 보여 주고 이후에 쌓인 예외도 반영한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_ExceptionBuffer_Exceptions는_순서대로_보여주고_이후에쌓인예외도반영()
        {
            var first = new InvalidOperationException("first");
            var second = new ArgumentException("second");
            var buffer = new ExceptionBuffer();

            buffer.Add(first);
            var view = buffer.Exceptions;
            buffer.Add(second);

            Assert.AreEqual(2, view.Count);
            Assert.AreSame(first, view[0]);
            Assert.AreSame(second, view[1]);
        }

    #endregion

    #region T-2: ToException

        // ------------------------------------------------------------
        /// <summary>
        /// 비어 있으면 null을 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_ExceptionBuffer_ToException은_비어있으면_null()
        {
            var buffer = new ExceptionBuffer();

            Assert.IsNull(buffer.ToException("실패"));
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 하나뿐이면 같은 예외 인스턴스를 던지지 않고 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_ExceptionBuffer_ToException은_하나면_그예외를_반환()
        {
            var original = new InvalidOperationException("original");
            var buffer = new ExceptionBuffer();
            buffer.Add(original);

            Assert.AreSame(original, buffer.ToException("실패"));
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 둘 이상이면 순서를 지켜 AggregateException으로 묶어 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_ExceptionBuffer_ToException은_여럿이면_AggregateException을_반환()
        {
            var first = new InvalidOperationException("first");
            var second = new ArgumentException("second");
            var buffer = new ExceptionBuffer();
            buffer.Add(first);
            buffer.Add(second);

            var result = buffer.ToException("실패") as AggregateException;

            Assert.IsNotNull(result);
            StringAssert.StartsWith("실패", result.Message);
            Assert.AreEqual(2, result.InnerExceptions.Count);
            Assert.AreSame(first, result.InnerExceptions[0]);
            Assert.AreSame(second, result.InnerExceptions[1]);
        }

    #endregion

    #region L-1: LogAll

        // ------------------------------------------------------------
        /// <summary>
        /// LogAll은 던지지 않고 쌓인 예외를 하나씩 콘솔에 남긴다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_ExceptionBuffer_LogAll은_던지지_않고_예외마다_로그를_남김()
        {
            var buffer = new ExceptionBuffer();
            buffer.Add(new InvalidOperationException("log-first"));
            buffer.Add(new ArgumentException("log-second"));

            UnityEngine.TestTools.LogAssert.Expect(UnityEngine.LogType.Exception, new System.Text.RegularExpressions.Regex("log-first"));
            UnityEngine.TestTools.LogAssert.Expect(UnityEngine.LogType.Exception, new System.Text.RegularExpressions.Regex("log-second"));

            Assert.DoesNotThrow(() => buffer.LogAll());
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 비어 있으면 아무것도 남기지 않는다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_ExceptionBuffer_LogAll은_비어있으면_로그를_남기지_않음()
        {
            var buffer = new ExceptionBuffer();

            Assert.DoesNotThrow(() => buffer.LogAll());
        }

    #endregion

    #region 헬퍼

        // ------------------------------------------------------------
        /// <summary>
        /// 스택 추적에 이 메서드 이름이 남는 예외를 던진다.
        /// </summary>
        // ------------------------------------------------------------
        private static void ThrowFromSource()
        {
            throw new InvalidOperationException("source");
        }

        // ------------------------------------------------------------
        /// <summary>
        /// ThrowFromSource 가 던진 예외를 잡아 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        private static Exception CaptureFromSource()
        {
            try
            {
                ThrowFromSource();
            }
            catch (Exception exception)
            {
                return exception;
            }

            return null;
        }

    #endregion

    }
}
