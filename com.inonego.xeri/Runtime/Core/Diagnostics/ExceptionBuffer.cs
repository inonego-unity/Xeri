/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : ExceptionBuffer.cs
수정일 : 2026-11-25

# 설명
오류가 발생할 때만 Exception 보관 목록을 생성하고, 외부에는 읽기 전용 View(Exceptions)로 제공한다.
수집한 실패는 ThrowWhenAny 로 던진다. 하나뿐이면 원래 예외를 스택 그대로, 둘 이상이면 AggregateException 으로 던진다.
던지지 않고 값으로 받으려면 ToException, 흐름을 끊지 않고 남기려면 LogAll 을 쓴다.

# 특이사항, 제약사항
구조체라 값으로 복사해 넘기면 첫 Add가 복사본에서만 일어나 실패를 잃을 수 있다. 다른 메서드에 넘길 때는 ref로 넘긴다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime;
using System.Runtime.ExceptionServices;

namespace inonego.Xeri
{
    // ============================================================
    /// <summary>
    /// 정상 경로에 할당을 만들지 않는 일회성 Exception 수집 버퍼.
    /// </summary>
    // ============================================================
    public struct ExceptionBuffer
    {

    #region 상태

        private List<Exception> exceptions;

        // ------------------------------------------------------------
        /// <summary>
        /// 누적된 예외의 개수를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public int Count => exceptions?.Count ?? 0;

        // --------------------------------------------------------------------------------
        /// <summary>
        /// <br/> 누적된 예외를 등록 순서대로 읽기 전용 View로 반환한다. 오류가 없으면 빈 View이다.
        /// <br/> 복사 없이 내부 목록을 보여 주므로 이후에 쌓인 예외도 반영한다.
        /// </summary>
        // --------------------------------------------------------------------------------
        public ReadOnlyListView<Exception> Exceptions => exceptions == null ? default : new ReadOnlyListView<Exception>(exceptions);

    #endregion

    #region 예외 수집

        // ------------------------------------------------------------
        /// <summary>
        /// 첫 오류가 들어올 때만 저장소를 만든다.
        /// </summary>
        // ------------------------------------------------------------
        public void Add(Exception exception)
        {
            (exceptions ??= new List<Exception>()).Add(exception);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 최초 실패를 복구 실패보다 앞에 배치한다.
        /// </summary>
        // ------------------------------------------------------------
        public void Prepend(Exception exception)
        {
            (exceptions ??= new List<Exception>()).Insert(0, exception);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 빈 결과를 받으면 저장소를 만들지 않고 오류만 합친다.
        /// </summary>
        // ------------------------------------------------------------
        public void AddRange(IReadOnlyList<Exception> source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            var count = source.Count;

            for (var index = 0; index < count; index++)
            {
                Add(source[index]);
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 다른 버퍼를 합칠 때 값 형식의 인터페이스 박싱을 피한다.
        /// </summary>
        // ------------------------------------------------------------
        public void AddRange(ExceptionBuffer other)
        {
            // 버퍼 복사본이 동일한 저장소를 공유해도 병합을 시작할 때의 개수까지만 합친다.
            var count = other.Count;

            for (var index = 0; index < count; index++)
            {
                Add(other.exceptions[index]);
            }
        }

    #endregion

    #region 결과 반환

        // ------------------------------------------------------------
        /// <summary>
        /// 내부 오류 목록에서 값 형식 박싱 없이 집계 예외를 구성한다.
        /// </summary>
        // ------------------------------------------------------------
        public AggregateException CreateAggregateException(string message)
        {
            IEnumerable<Exception> list = exceptions;
            return new AggregateException(message, list ?? Array.Empty<Exception>());
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> 수집한 오류가 있으면 던지고, 없으면 아무것도 하지 않는다.
        /// <br/> 하나뿐이면 원래 예외를 스택 추적 그대로 던진다.
        /// <br/> 둘 이상이면 message를 가진 AggregateException으로 던진다.
        /// </summary>
        // ----------------------------------------------------------------------
        public void ThrowWhenAny(string message)
        {
            var count = Count;

            if (count == 0)
            {
                return;
            }

            if (count == 1)
            {
                ExceptionDispatchInfo.Capture(exceptions[0]).Throw();
            }

            throw CreateAggregateException(message);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> 수집한 오류를 던지지 않고 하나의 예외 값으로 반환한다.
        /// <br/> 없으면 null, 하나뿐이면 그 예외이다.
        /// <br/> 둘 이상이면 message를 가진 AggregateException이다.
        /// </summary>
        // ------------------------------------------------------------
        public Exception ToException(string message)
        {
            var count = Count;

            if (count == 0)
            {
                return null;
            }

            if (count == 1)
            {
                return exceptions[0];
            }

            return CreateAggregateException(message);
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> 수집한 오류를 등록 순서대로 하나씩 Unity 콘솔에 남기고 던지지 않는다.
        /// <br/> 표현 계층처럼 실패가 흐름을 끊으면 안 되는 곳에서 쓴다.
        /// </summary>
        // ----------------------------------------------------------------------
        public void LogAll(UnityEngine.Object context = null)
        {
            var count = Count;

            for (var index = 0; index < count; index++)
            {
                UnityEngine.Debug.LogException(exceptions[index], context);
            }
        }

    #endregion

    }
}
