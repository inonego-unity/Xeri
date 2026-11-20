/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : ApplicationRuntime.cs
수정일 : 2026-11-03

# 설명
Application 실행 세대의 순차 초기화와 종료 경계를 관리한다.
모듈 완료, 시작 실패 및 정리 실패를 호출자에게 전달한다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace inonego.Xeri.Application
{
    // ============================================================
    /// <summary>
    /// Application 실행 세대의 수명 상태.
    /// </summary>
    // ============================================================
    public enum ApplicationState
    {
        Created,
        Starting,
        Running,
        Stopping,
        Stopped,
        Failed
    }

    // ============================================================
    /// <summary>
    /// <br/> 한 번만 시작할 수 있는 Application 실행 세대.
    /// <br/> 게임의 씬 구성이나 세션 정책은 알지 않는다.
    /// </summary>
    // ============================================================
    public sealed class ApplicationRuntime
    {

    #region 필드

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 실행 세대의 초기화·종료 상태.
        /// </summary>
        // ------------------------------------------------------------
        public ApplicationState State
        {
            get
            {
                lock (sync)
                {
                    return state;
                }
            }
        }
        private ApplicationState state = ApplicationState.Created;

        // ------------------------------------------------------------
        /// <summary>
        /// 실행 세대가 소유하는 최상위 자원 Scope.
        /// </summary>
        // ------------------------------------------------------------
        public ApplicationScope Scope { get; } = new();

        // ------------------------------------------------------------
        /// <summary>
        /// 모듈에 전달할 공통 Application 문맥.
        /// </summary>
        // ------------------------------------------------------------
        public ApplicationContext Context { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 시작 또는 종료 단계에서 발생한 오류.
        /// </summary>
        // ------------------------------------------------------------
        public Exception Failure
        {
            get
            {
                lock (sync)
                {
                    return failure;
                }
            }
        }
        private Exception failure;

        private readonly object sync = new();
        private Task initializationTask;
        private TaskCompletionSource<bool> stopPromise;

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// 새 Application 실행 세대와 문맥을 구성한다.
        /// </summary>
        // ------------------------------------------------------------
        public ApplicationRuntime()
        {
            Context = new ApplicationContext(this);
        }

    #endregion

    #region 시작

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> 초기화 모듈을 등록된 순서대로 실행한다.
        /// <br/> 한 번 시작한 Runtime은 종료 후 다시 시작하지 않는다.
        /// </summary>
        // ------------------------------------------------------------
        public Task StartAsync(IReadOnlyList<IApplicationModule> modules)
        {
            if (modules == null)
            {
                throw new ArgumentNullException(nameof(modules));
            }

            TaskCompletionSource<bool> completion;
            IApplicationModule[] snapshot;
            lock (sync)
            {
                if (state != ApplicationState.Created)
                {
                    throw new InvalidOperationException("Application Runtime은 한 번만 시작할 수 있습니다.");
                }

                // 초기화 중 호출자가 모듈 목록을 변경해도 실행 계약이 달라지지 않도록 고정한다.
                snapshot = new IApplicationModule[modules.Count];
                for (int index = 0; index < snapshot.Length; index++)
                {
                    snapshot[index] = modules[index];
                }

                state = ApplicationState.Starting;

                // 모듈 내부에서 종료를 요청하더라도 초기화 완료 작업을 먼저 공개한다.
                completion = new TaskCompletionSource<bool>
                (
                    TaskCreationOptions.RunContinuationsAsynchronously
                );
                initializationTask = completion.Task;
            }

            _ = InitializeAndPublishAsync(snapshot, completion);
            return ObserveStartupAsync(completion.Task);
        }

        private async Task InitializeAndPublishAsync
        (
            IReadOnlyList<IApplicationModule> modules,
            TaskCompletionSource<bool> completion
        )
        {
            try
            {
                await InitializeAsync(modules);
                completion.TrySetResult(true);
            }
            catch (Exception exception)
            {
                // 초기화의 종료 결과는 여기에서 한 번만 확정한다.
                // Runtime 종료 요청과 동일한 취소 토큰이 아니면 모듈 실패로 취급한다.
                bool normalCancellation;
                Exception initialFailure = exception;

                lock (sync)
                {
                    normalCancellation = exception is OperationCanceledException cancellation &&
                        stopPromise != null &&
                        Scope.CancellationToken.IsCancellationRequested &&
                        cancellation.CancellationToken == Scope.CancellationToken;

                    if (!normalCancellation)
                    {
                        if (exception is OperationCanceledException)
                        {
                            initialFailure = new InvalidOperationException
                            (
                                "Application 초기화 모듈이 실행 세대의 종료와 무관하게 취소되었습니다.",
                                exception
                            );
                        }

                        failure ??= initialFailure;
                    }
                }

                if (normalCancellation)
                {
                    completion.TrySetCanceled(Scope.CancellationToken);
                }
                else
                {
                    completion.TrySetException(initialFailure);
                }
            }
        }

        private async Task InitializeAsync(IReadOnlyList<IApplicationModule> modules)
        {
            for (int index = 0; index < modules.Count; index++)
            {
                Scope.CancellationToken.ThrowIfCancellationRequested();

                var module = modules[index] ??
                    throw new ArgumentException("초기화 모듈 목록에 null이 포함되어 있습니다.", nameof(modules));
                var task = module.StartAsync(Context, Scope.CancellationToken) ??
                    throw new InvalidOperationException("초기화 모듈이 null Task를 반환했습니다.");
                await task;
            }

            Scope.CancellationToken.ThrowIfCancellationRequested();
            lock (sync)
            {
                if (state == ApplicationState.Starting)
                {
                    state = ApplicationState.Running;
                }
            }
        }

        private async Task ObserveStartupAsync(Task startup)
        {
            try
            {
                await startup;
            }
            catch (Exception exception)
            {
                // 초기화 결과와 최초 실패 원인은 InitializeAndPublishAsync가 확정한다.
                // 부분 초기화에서 이미 소유한 자원은 완료를 기다린 뒤 해제한다.
                try
                {
                    await StopAsync();
                }
                catch (Exception closeException)
                {
                    var combined = new AggregateException
                    (
                        "Application 시작 실패와 종료 실패가 함께 발생했습니다.",
                        exception,
                        closeException
                    );
                    throw combined;
                }

                throw;
            }
        }

    #endregion

    #region 종료

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> 초기화 중인 모듈에 취소를 알리고 완료를 기다린다.
        /// <br/> 종료 요청이 중복되면 동일한 정리 완료 결과를 공유한다.
        /// </summary>
        // ------------------------------------------------------------
        public Task StopAsync()
        {
            TaskCompletionSource<bool> promise;
            lock (sync)
            {
                if (stopPromise != null) return stopPromise.Task;

                stopPromise = new TaskCompletionSource<bool>
                (
                    TaskCreationOptions.RunContinuationsAsynchronously
                );
                state = ApplicationState.Stopping;
                promise = stopPromise;
            }

            _ = StopCoreAsync(promise);
            return promise.Task;
        }

        private async Task StopCoreAsync(TaskCompletionSource<bool> promise)
        {
            Exception closeFailure = null;

            // 종료가 취소 완료를 기다리므로 여기서는 중단 신호만 시작한다.
            _ = Scope.RequestCancellationAsync();

            // 초기화 모듈이 끝나기 전에는 그 모듈의 자원을 해제하지 않는다.
            if (initializationTask != null)
            {
                try
                {
                    await initializationTask;
                }
                catch (Exception)
                {
                    // NONE
                }
            }

            try
            {
                await Scope.CloseAsync();
            }
            catch (Exception exception)
            {
                closeFailure = exception;
            }

            AggregateException stopError = null;
            lock (sync)
            {
                if (closeFailure != null)
                {
                    stopError = new AggregateException("Application 종료 중 오류가 발생했습니다.", closeFailure);
                    failure = failure == null ? stopError :
                        new AggregateException("Application 실행과 종료 오류.", failure, stopError);
                }

                state = failure == null ? ApplicationState.Stopped : ApplicationState.Failed;
            }

            if (stopError != null)
            {
                promise.TrySetException(stopError);
            }
            else
            {
                promise.TrySetResult(true);
            }
        }

    #endregion

    }
}
