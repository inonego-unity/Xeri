/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : ApplicationHost.cs
수정일 : 2026-11-03

# 설명
Unity Play Session의 시작과 종료를 ApplicationRuntime에 연결한다.
이전 실행 세대가 종료된 뒤 새 세대를 공개하고 Bootstrapper 초기화를 허용한다.
Play Session별 시작 완료 작업을 시작 호출자와 외부 관찰자에게 공유한다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using UnityEngine;
using UnityApplication = UnityEngine.Application;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace inonego.Xeri.Application
{
    // ============================================================
    /// <summary>
    /// <br/> Unity Play Session별 Application 실행 세대를 제공한다.
    /// <br/> 강제 종료에서는 비동기 정리가 끝날 때까지 기다릴 수 없다.
    /// </summary>
    // ============================================================
    public static class ApplicationHost
    {

    #region 필드

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 Unity Play Session의 실행 세대.
        /// </summary>
        // ------------------------------------------------------------
        public static ApplicationRuntime Current { get; private set; }

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> 현재 Play Session의 Application 시작 완료 작업.
        /// <br/> Running에 진입하면 완료된다.
        /// <br/> 시작 실패는 예외, 종료 중단은 취소로 전달한다.
        /// <br/> Session을 열기 전에는 접근할 수 없다.
        /// </summary>
        // ------------------------------------------------------------
        public static Task StartupTask
        {
            get
            {
                lock (sync)
                {
                    return startupPromise?.Task ??
                        throw new InvalidOperationException("Application Play Session이 아직 열리지 않았습니다.");
                }
            }
        }

        private static readonly object sync = new();
        private static TaskCompletionSource<bool> startupPromise;
        private static Task previousStops = Task.CompletedTask;
        private static int sessionVersion;
        private static bool startRequested;
        private static bool sessionClosing = true;

    #endregion

    #region Unity 시작 및 종료

        // ------------------------------------------------------------
        /// <summary>
        /// Unity 실행 세대의 시작에 맞춰 Runtime을 새로 구성한다.
        /// </summary>
        // ------------------------------------------------------------
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Open()
        {
            UnityApplication.quitting -= OnQuitting;

            ApplicationRuntime previous;
            lock (sync)
            {
                // 이전 세대의 대기자는 새로운 Play Session으로 넘어가지 않는다.
                startupPromise?.TrySetCanceled();
                startupPromise = new TaskCompletionSource<bool>
                (
                    TaskCreationOptions.RunContinuationsAsynchronously
                );
                previous = Current;
                Current = null;
                sessionVersion++;
                startRequested = false;
                sessionClosing = true;
            }

            // StopAsync의 취소 콜백이 재진입해도 새 세대가 부분 초기화되지 않게 한다.
            if (previous != null)
            {
                var stopping = previous.StopAsync();
                lock (sync)
                {
                    previousStops = Task.WhenAll(previousStops, stopping);
                }
            }

            lock (sync)
            {
                sessionClosing = false;
            }

            UnityApplication.quitting += OnQuitting;
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> 이전 세대의 종료가 끝난 뒤 새 Runtime을 생성하고 초기화를 실행한다.
        /// <br/> 종료가 실패했거나 Play Session이 바뀌면 새 세대를 시작하지 않는다.
        /// <br/> 수락된 요청은 StartupTask와 동일한 완료 작업을 반환한다.
        /// </summary>
        // ----------------------------------------------------------------------
        public static Task StartAsync(IReadOnlyList<IApplicationModule> modules)
        {
            if (modules == null)
            {
                return Task.FromException(new ArgumentNullException(nameof(modules)));
            }

            int version;
            Task completedStops;
            TaskCompletionSource<bool> completion;
            lock (sync)
            {
                if (sessionClosing)
                {
                    return Task.FromCanceled(new CancellationToken(true));
                }

                if (startRequested)
                {
                    return Task.FromException
                    (
                        new InvalidOperationException("Application Play Session은 한 번만 시작할 수 있습니다.")
                    );
                }

                // 이전 세대가 종료되는 중에도 시작 예약을 선점해 중복 요청을 차단한다.
                version = sessionVersion;
                startRequested = true;
                completedStops = previousStops;
                completion = startupPromise;
            }

            // 시작 호출자와 외부 관찰자는 하나의 완료 작업을 공유한다.
            _ = StartCoreAsync(modules, version, completedStops, completion);
            return completion.Task;
        }

        private static async Task StartCoreAsync
        (
            IReadOnlyList<IApplicationModule> modules,
            int version,
            Task completedStops,
            TaskCompletionSource<bool> completion
        )
        {
            try
            {
                await completedStops;

                ApplicationRuntime runtime;
                lock (sync)
                {
                    if (version != sessionVersion || sessionClosing)
                    {
                        throw new OperationCanceledException("Application Play Session이 종료되었습니다.");
                    }

                    runtime = new ApplicationRuntime();
                    Current = runtime;
                }

                await runtime.StartAsync(modules);

                lock (sync)
                {
                    if (version != sessionVersion || sessionClosing)
                    {
                        completion.TrySetCanceled();
                    }
                    else
                    {
                        completion.TrySetResult(true);
                    }
                }
            }
            catch (OperationCanceledException exception)
            {
                if (exception.CancellationToken.CanBeCanceled)
                {
                    completion.TrySetCanceled(exception.CancellationToken);
                }
                else
                {
                    completion.TrySetCanceled();
                }
            }
            catch (Exception exception)
            {
                completion.TrySetException(exception);
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> 사전 구성 오류를 현재 Session의 시작 실패로 전달한다.
        /// <br/> 실패한 Session은 다시 시작하지 않는다.
        /// </summary>
        // ------------------------------------------------------------
        internal static void FailStartup(Exception exception)
        {
            if (exception == null)
            {
                throw new ArgumentNullException(nameof(exception));
            }

            lock (sync)
            {
                if (sessionClosing || startRequested) return;

                startRequested = true;
                startupPromise?.TrySetException(exception);
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Unity 종료 신호에서 현재 Runtime 종료를 요청한다.
        /// </summary>
        // ------------------------------------------------------------
        private static void OnQuitting()
        {
            ApplicationRuntime runtime;
            lock (sync)
            {
                if (sessionClosing) return;

                sessionClosing = true;
                sessionVersion++;
                startupPromise?.TrySetCanceled();
                runtime = Current;
            }

            if (runtime != null)
            {
                _ = StopAndReportAsync(runtime);
            }
        }

        private static async Task StopAndReportAsync(ApplicationRuntime runtime)
        {
            try
            {
                await runtime.StopAsync();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

    #if UNITY_EDITOR

        // ------------------------------------------------------------
        /// <summary>
        /// Editor Play 종료 경계를 중복 등록 없이 관찰한다.
        /// </summary>
        // ------------------------------------------------------------
        [InitializeOnLoadMethod]
        private static void InitializeEditor()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        private static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingPlayMode)
            {
                OnQuitting();
            }
        }

    #endif

    #endregion

    }
}
