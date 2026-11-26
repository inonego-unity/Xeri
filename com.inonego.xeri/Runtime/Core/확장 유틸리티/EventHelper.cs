/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : EventHelper.cs
수정일 : 2026-11-25

# 설명
이벤트(멀티캐스트 델리게이트)의 구독자를 다루는 확장 메서드 모음.
Handlers는 구독자 목록을 읽기 전용 View로 돌려주고, InvokeEach는 Action 계열과 EventHandler 계열의 구독자를 하나씩 호출하고, 그 밖의 델리게이트는 호출 방법을 람다로 받는다.
InvokeEach는 구독자 하나가 실패해도 나머지를 계속 호출하고 실패는 호출자의 ExceptionBuffer에 모은다.

# 특이사항, 제약사항
InvokeEach는 예외를 던지지 않는다. 모은 실패는 호출자가 ThrowWhenAny 등으로 처리한다.
실패를 다루는 방식이 다르면(첫 실패에서 중단, 로그만 남김 등) Handlers로 목록을 받아 직접 순회한다.
구독자 목록은 델리게이트 객체 참조를 기준으로 캐시한다. 델리게이트는 불변이므로 구독이 바뀌면 새 객체가 되어
새 목록이 만들어지고, 이미 받은 목록은 바뀌지 않는다. 구독이 바뀐 뒤 첫 호출에만 목록을 할당한다.
호출 중 구독이 바뀌어도 호출 시작 시점의 목록으로 끝까지 호출한다.
같은 델리게이트 객체를 여러 번 알릴 때만 이득이다. 구독이 자주 바뀌는 이벤트나
객체마다 한두 번만 알리는 이벤트(수명이 짧은 객체의 1회성 알림 등)는 알릴 때마다 목록을 새로 만들어 맞지 않는다.
한 번의 처리에서 여러 번 알릴 때 같은 구독자 목록을 유지하려면 델리게이트를 지역 변수에 한 번 담아 사용한다.
캐시는 스레드에 안전하고 목록은 만든 뒤 바뀌지 않는다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Runtime;
using System.Runtime.CompilerServices;

namespace inonego.Xeri
{
    // ============================================================
    /// <summary>
    /// 이벤트 구독자 확장 메서드 정적 헬퍼.
    /// </summary>
    // ============================================================
    public static class EventHelper
    {

    #region 구독자 목록 캐시

        // ======================================================================
        /// <summary>
        /// <br/> 델리게이트 타입별로 구독자 목록을 델리게이트 객체 기준으로 캐시한다.
        /// <br/> 델리게이트 객체가 수거되면 항목도 함께 사라진다.
        /// </summary>
        // ======================================================================
        private static class HandlerCache<TDelegate>
        where TDelegate : Delegate
        {
            private static readonly ConditionalWeakTable<TDelegate, TDelegate[]> lists = new();

            private static readonly ConditionalWeakTable<TDelegate, TDelegate[]>.CreateValueCallback create =
                static source => Create(source);

            // --------------------------------------------------------------------------------
            /// <summary>
            /// 델리게이트의 구독자 배열을 캐시에서 가져오고 없으면 만든다. 반환한 배열은 수정하지 않는다.
            /// </summary>
            // --------------------------------------------------------------------------------
            internal static TDelegate[] Get(TDelegate source)
            {
                return lists.GetValue(source, create);
            }

            private static TDelegate[] Create(TDelegate source)
            {
                var list = source.GetInvocationList();
                var handlers = new TDelegate[list.Length];

                for (var index = 0; index < list.Length; index++)
                {
                    handlers[index] = (TDelegate)list[index];
                }

                return handlers;
            }
        }

    #endregion

    #region 구독자 목록

        // --------------------------------------------------------------------------------
        /// <summary>
        /// <br/> 이벤트의 구독자를 등록 순서대로 읽기 전용 View로 반환한다.
        /// <br/> null 이벤트는 빈 View를 반환한다. 반환한 View는 이후 구독이 바뀌어도 바뀌지 않는다.
        /// </summary>
        // --------------------------------------------------------------------------------
        public static ReadOnlyListView<TDelegate> Handlers<TDelegate>(this TDelegate source)
        where TDelegate : Delegate
        {
            if (source == null)
            {
                return default;
            }

            return new ReadOnlyListView<TDelegate>(HandlerCache<TDelegate>.Get(source));
        }

    #endregion

    #region Action 계열 호출

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> 구독자를 등록된 순서대로 하나씩 호출한다. null이면 아무것도 하지 않는다.
        /// <br/> 구독자가 던진 예외는 failures에 모으고 나머지 구독자를 계속 호출한다.
        /// </summary>
        // ----------------------------------------------------------------------
        public static void InvokeEach(this Action handlers, ref ExceptionBuffer failures)
        {
            if (handlers == null)
            {
                return;
            }

            foreach (var handler in HandlerCache<Action>.Get(handlers))
            {
                try
                {
                    handler();
                }
                catch (Exception exception)
                {
                    failures.Add(exception);
                }
            }
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// 인자 하나를 넘기며 구독자를 하나씩 호출한다. 동작은 인자 없는 오버로드와 같다.
        /// </summary>
        // ----------------------------------------------------------------------
        public static void InvokeEach<T>(this Action<T> handlers, T arg, ref ExceptionBuffer failures)
        {
            if (handlers == null)
            {
                return;
            }

            foreach (var handler in HandlerCache<Action<T>>.Get(handlers))
            {
                try
                {
                    handler(arg);
                }
                catch (Exception exception)
                {
                    failures.Add(exception);
                }
            }
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// 인자 둘을 넘기며 구독자를 하나씩 호출한다. 동작은 인자 없는 오버로드와 같다.
        /// </summary>
        // ----------------------------------------------------------------------
        public static void InvokeEach<T1, T2>(this Action<T1, T2> handlers, T1 arg1, T2 arg2, ref ExceptionBuffer failures)
        {
            if (handlers == null)
            {
                return;
            }

            foreach (var handler in HandlerCache<Action<T1, T2>>.Get(handlers))
            {
                try
                {
                    handler(arg1, arg2);
                }
                catch (Exception exception)
                {
                    failures.Add(exception);
                }
            }
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// 인자 셋을 넘기며 구독자를 하나씩 호출한다. 동작은 인자 없는 오버로드와 같다.
        /// </summary>
        // ----------------------------------------------------------------------
        public static void InvokeEach<T1, T2, T3>(this Action<T1, T2, T3> handlers, T1 arg1, T2 arg2, T3 arg3, ref ExceptionBuffer failures)
        {
            if (handlers == null)
            {
                return;
            }

            foreach (var handler in HandlerCache<Action<T1, T2, T3>>.Get(handlers))
            {
                try
                {
                    handler(arg1, arg2, arg3);
                }
                catch (Exception exception)
                {
                    failures.Add(exception);
                }
            }
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// 인자 넷을 넘기며 구독자를 하나씩 호출한다. 동작은 인자 없는 오버로드와 같다.
        /// </summary>
        // ----------------------------------------------------------------------
        public static void InvokeEach<T1, T2, T3, T4>(this Action<T1, T2, T3, T4> handlers, T1 arg1, T2 arg2, T3 arg3, T4 arg4, ref ExceptionBuffer failures)
        {
            if (handlers == null)
            {
                return;
            }

            foreach (var handler in HandlerCache<Action<T1, T2, T3, T4>>.Get(handlers))
            {
                try
                {
                    handler(arg1, arg2, arg3, arg4);
                }
                catch (Exception exception)
                {
                    failures.Add(exception);
                }
            }
        }

    #endregion

    #region EventHandler 계열 호출

        // ----------------------------------------------------------------------
        /// <summary>
        /// EventHandler 구독자를 하나씩 호출한다. 동작은 인자 없는 오버로드와 같다.
        /// </summary>
        // ----------------------------------------------------------------------
        public static void InvokeEach(this EventHandler handlers, object sender, EventArgs args, ref ExceptionBuffer failures)
        {
            if (handlers == null)
            {
                return;
            }

            foreach (var handler in HandlerCache<EventHandler>.Get(handlers))
            {
                try
                {
                    handler(sender, args);
                }
                catch (Exception exception)
                {
                    failures.Add(exception);
                }
            }
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> EventHandler&lt;TEventArgs&gt; 구독자를 하나씩 호출한다.
        /// <br/> 동작은 인자 없는 오버로드와 같다.
        /// </summary>
        // ----------------------------------------------------------------------
        public static void InvokeEach<TEventArgs>(this EventHandler<TEventArgs> handlers, object sender, TEventArgs args, ref ExceptionBuffer failures)
        {
            if (handlers == null)
            {
                return;
            }

            foreach (var handler in HandlerCache<EventHandler<TEventArgs>>.Get(handlers))
            {
                try
                {
                    handler(sender, args);
                }
                catch (Exception exception)
                {
                    failures.Add(exception);
                }
            }
        }

    #endregion

    #region 어댑터 람다 호출

        // --------------------------------------------------------------------------------
        /// <summary>
        /// <br/> 표준 형태가 아닌 델리게이트의 구독자를 인자 없이로 하나씩 호출한다.
        /// <br/> 호출 방법은 invoke로 받으며 바깥 값을 캡처하지 않는 static 람다를 권한다.
        /// </summary>
        // --------------------------------------------------------------------------------
        public static void InvokeEach<TDelegate>(this TDelegate handlers, Action<TDelegate> invoke, ref ExceptionBuffer failures)
        where TDelegate : Delegate
        {
            if (invoke == null)
            {
                throw new ArgumentNullException(nameof(invoke));
            }

            if (handlers == null)
            {
                return;
            }

            foreach (var handler in HandlerCache<TDelegate>.Get(handlers))
            {
                try
                {
                    invoke(handler);
                }
                catch (Exception exception)
                {
                    failures.Add(exception);
                }
            }
        }

        // --------------------------------------------------------------------------------
        /// <summary>
        /// <br/> 표준 형태가 아닌 델리게이트의 구독자를 인자 하나로 하나씩 호출한다.
        /// <br/> 호출 방법은 invoke로 받으며 바깥 값을 캡처하지 않는 static 람다를 권한다.
        /// </summary>
        // --------------------------------------------------------------------------------
        public static void InvokeEach<TDelegate, T1>(this TDelegate handlers, T1 arg, Action<TDelegate, T1> invoke, ref ExceptionBuffer failures)
        where TDelegate : Delegate
        {
            if (invoke == null)
            {
                throw new ArgumentNullException(nameof(invoke));
            }

            if (handlers == null)
            {
                return;
            }

            foreach (var handler in HandlerCache<TDelegate>.Get(handlers))
            {
                try
                {
                    invoke(handler, arg);
                }
                catch (Exception exception)
                {
                    failures.Add(exception);
                }
            }
        }

        // --------------------------------------------------------------------------------
        /// <summary>
        /// <br/> 표준 형태가 아닌 델리게이트의 구독자를 인자 둘로 하나씩 호출한다.
        /// <br/> 호출 방법은 invoke로 받으며 바깥 값을 캡처하지 않는 static 람다를 권한다.
        /// </summary>
        // --------------------------------------------------------------------------------
        public static void InvokeEach<TDelegate, T1, T2>(this TDelegate handlers, T1 arg1, T2 arg2, Action<TDelegate, T1, T2> invoke, ref ExceptionBuffer failures)
        where TDelegate : Delegate
        {
            if (invoke == null)
            {
                throw new ArgumentNullException(nameof(invoke));
            }

            if (handlers == null)
            {
                return;
            }

            foreach (var handler in HandlerCache<TDelegate>.Get(handlers))
            {
                try
                {
                    invoke(handler, arg1, arg2);
                }
                catch (Exception exception)
                {
                    failures.Add(exception);
                }
            }
        }

        // --------------------------------------------------------------------------------
        /// <summary>
        /// <br/> 표준 형태가 아닌 델리게이트의 구독자를 인자 셋로 하나씩 호출한다.
        /// <br/> 호출 방법은 invoke로 받으며 바깥 값을 캡처하지 않는 static 람다를 권한다.
        /// </summary>
        // --------------------------------------------------------------------------------
        public static void InvokeEach<TDelegate, T1, T2, T3>(this TDelegate handlers, T1 arg1, T2 arg2, T3 arg3, Action<TDelegate, T1, T2, T3> invoke, ref ExceptionBuffer failures)
        where TDelegate : Delegate
        {
            if (invoke == null)
            {
                throw new ArgumentNullException(nameof(invoke));
            }

            if (handlers == null)
            {
                return;
            }

            foreach (var handler in HandlerCache<TDelegate>.Get(handlers))
            {
                try
                {
                    invoke(handler, arg1, arg2, arg3);
                }
                catch (Exception exception)
                {
                    failures.Add(exception);
                }
            }
        }

        // --------------------------------------------------------------------------------
        /// <summary>
        /// <br/> 표준 형태가 아닌 델리게이트의 구독자를 인자 넷로 하나씩 호출한다.
        /// <br/> 호출 방법은 invoke로 받으며 바깥 값을 캡처하지 않는 static 람다를 권한다.
        /// </summary>
        // --------------------------------------------------------------------------------
        public static void InvokeEach<TDelegate, T1, T2, T3, T4>(this TDelegate handlers, T1 arg1, T2 arg2, T3 arg3, T4 arg4, Action<TDelegate, T1, T2, T3, T4> invoke, ref ExceptionBuffer failures)
        where TDelegate : Delegate
        {
            if (invoke == null)
            {
                throw new ArgumentNullException(nameof(invoke));
            }

            if (handlers == null)
            {
                return;
            }

            foreach (var handler in HandlerCache<TDelegate>.Get(handlers))
            {
                try
                {
                    invoke(handler, arg1, arg2, arg3, arg4);
                }
                catch (Exception exception)
                {
                    failures.Add(exception);
                }
            }
        }

    #endregion

    }
}
