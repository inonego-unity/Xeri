/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TEST_ApplicationRuntime.cs
수정일 : 2026-11-03

# 설명
ApplicationRuntime의 순차 초기화, 준비 경계와 실패 종료를 검증한다.

# 테스트 구성
 E: 순차 초기화와 Readiness
 S: 종료 중 초기화 경합
 X: 시작 실패와 정리 실패
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using NUnit;
using NUnit.Framework;

using inonego;
using inonego.Xeri;
using inonego.Xeri.Application;

namespace inonego.Xeri.TEST.Core._Application
{
    // ============================================================
    /// <summary>
    /// ApplicationRuntime의 시작·종료 계약 테스트.
    /// </summary>
    // ============================================================
    public sealed class TEST_ApplicationRuntime
    {

    #region 헬퍼

        private sealed class Module : IApplicationModule
        {
            private readonly Func<ApplicationContext, CancellationToken, Task> start;

            public Module(Func<ApplicationContext, CancellationToken, Task> start)
            {
                this.start = start;
            }

            public Task StartAsync(ApplicationContext context, CancellationToken cancellationToken)
            {
                return start(context, cancellationToken);
            }
        }

        private sealed class OwnedItem : IDisposable
        {
            private readonly Action close;

            public OwnedItem(Action close)
            {
                this.close = close;
            }

            public void Dispose()
            {
                close();
            }
        }

    #endregion

    #region E-1: 초기화 완료와 준비 완료 구분

        // ------------------------------------------------------------
        /// <summary>
        /// 초기화 순서·목록 고정·재시작 금지와 별도 Readiness 계약을 검증한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public async Task TEST_ApplicationRuntime_순차초기화와_준비경계분리()
        {
            var order = new List<int>();
            var readiness = new TaskCompletionSource<bool>
            (
                TaskCreationOptions.RunContinuationsAsynchronously
            );
            var runtime = new ApplicationRuntime();
            var modules = new List<IApplicationModule>();
            Task Late(ApplicationContext context, CancellationToken token)
            {
                order.Add(3);
                return Task.CompletedTask;
            }

            Task Prepare(ApplicationContext context, CancellationToken token)
            {
                order.Add(1);
                context.Track(readiness.Task);
                modules.Add(new Module(Late));
                return Task.CompletedTask;
            }

            Task Complete(ApplicationContext context, CancellationToken token)
            {
                order.Add(2);
                return Task.CompletedTask;
            }

            modules.Add(new Module(Prepare));
            modules.Add(new Module(Complete));

            await runtime.StartAsync(modules);
            Assert.AreEqual(ApplicationState.Running, runtime.State);
            Assert.AreEqual("1,2", string.Join(",", order));
            Assert.IsFalse(readiness.Task.IsCompleted);
            Assert.Throws<InvalidOperationException>(() => runtime.StartAsync(modules));

            readiness.SetResult(true);
            await runtime.StopAsync();
            Assert.AreEqual(ApplicationState.Stopped, runtime.State);
            Assert.Throws<InvalidOperationException>(() => runtime.StartAsync(modules));
        }

    #endregion

    #region S-1: 시작 도중 종료

        // ------------------------------------------------------------
        /// <summary>
        /// 초기화 도중 종료하면 취소를 알리고 완료까지 기다린다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public async Task TEST_ApplicationRuntime_시작중종료시_초기화완료대기()
        {
            var pending = new TaskCompletionSource<bool>
            (
                TaskCreationOptions.RunContinuationsAsynchronously
            );
            var runtime = new ApplicationRuntime();
            var modules = new List<IApplicationModule>();
            modules.Add(new Module((context, token) => pending.Task));
            var starting = runtime.StartAsync(modules);

            var requests = new Task[8];
            Parallel.For(0, requests.Length, index => requests[index] = runtime.StopAsync());
            var stopping = requests[0];
            foreach (var request in requests)
            {
                Assert.AreSame(stopping, request);
            }

            Assert.IsTrue(runtime.Scope.CancellationToken.IsCancellationRequested);
            Assert.IsFalse(stopping.IsCompleted);
            pending.SetResult(true);

            var completed = await Task.WhenAny(starting, Task.Delay(TimeSpan.FromSeconds(5)));
            Assert.AreSame(starting, completed);
            Assert.IsTrue(starting.IsCanceled);
            await stopping;
            Assert.AreEqual(ApplicationState.Stopped, runtime.State);

            // 같은 종료 요청 중이라도 외부 토큰으로 취소된 모듈은 실패를 보존한다.
            using var unrelated = new CancellationTokenSource();
            var otherPending = new TaskCompletionSource<bool>
            (
                TaskCreationOptions.RunContinuationsAsynchronously
            );
            var otherRuntime = new ApplicationRuntime();
            var otherModules = new List<IApplicationModule>();
            otherModules.Add(new Module((context, token) => otherPending.Task));

            var otherStarting = otherRuntime.StartAsync(otherModules);
            var otherStopping = otherRuntime.StopAsync();
            unrelated.Cancel();
            otherPending.SetException(new OperationCanceledException(unrelated.Token));

            var failure = Assert.ThrowsAsync<InvalidOperationException>
            (
                async () => await otherStarting
            );
            Assert.IsInstanceOf<OperationCanceledException>(failure.InnerException);

            await otherStopping;
            Assert.AreEqual(ApplicationState.Failed, otherRuntime.State);
            Assert.AreSame(failure, otherRuntime.Failure);
        }

    #endregion

    #region S-2: 초기화 내부 종료 요청

        // ------------------------------------------------------------
        /// <summary>
        /// 초기화 모듈의 동기 종료 요청도 기존 초기화 완료를 기다린다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public async Task TEST_ApplicationRuntime_모듈내부종료요청시_초기화소유권유지()
        {
            var pending = new TaskCompletionSource<bool>
            (
                TaskCreationOptions.RunContinuationsAsynchronously
            );
            var runtime = new ApplicationRuntime();
            Task stopping = null;
            bool released = false;

            Task Begin(ApplicationContext context, CancellationToken token)
            {
                context.Scope.Own(new OwnedItem(() => released = true));
                stopping = context.Runtime.StopAsync();
                return pending.Task;
            }

            var modules = new List<IApplicationModule>();
            modules.Add(new Module(Begin));
            var starting = runtime.StartAsync(modules);

            Assert.IsNotNull(stopping);
            Assert.IsFalse(stopping.IsCompleted);
            Assert.IsFalse(released);

            pending.SetResult(true);
            var completed = await Task.WhenAny(starting, Task.Delay(TimeSpan.FromSeconds(5)));
            Assert.AreSame(starting, completed);
            Assert.IsTrue(starting.IsCanceled);
            await stopping;
            Assert.IsTrue(released);
            Assert.AreEqual(ApplicationState.Stopped, runtime.State);
        }

    #endregion

    #region X-1: 실패 초기화의 자원 회수

        // ------------------------------------------------------------
        /// <summary>
        /// 모듈 실패와 Dispose 실패를 동시에 보존한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_ApplicationRuntime_시작실패시_정리오류까지반환()
        {
            int releases = 0;
            var runtime = new ApplicationRuntime();

            void ReleaseWithError()
            {
                releases++;
                throw new InvalidOperationException("cleanup");
            }

            Task First(ApplicationContext context, CancellationToken token)
            {
                context.Scope.Own(new OwnedItem(ReleaseWithError));
                var registration = token.Register(() => throw new InvalidOperationException("cancel"));
                context.Scope.Own(new Lease(registration.Dispose));
                return Task.CompletedTask;
            }

            Task Second(ApplicationContext context, CancellationToken token)
            {
                return Task.FromException(new InvalidOperationException("startup"));
            }

            var modules = new List<IApplicationModule>();
            modules.Add(new Module(First));
            modules.Add(new Module(Second));
            var starting = runtime.StartAsync(modules);

            var error = Assert.ThrowsAsync<AggregateException>
            (
                async () => await starting
            );

            Assert.AreEqual(1, releases);
            Assert.AreEqual(ApplicationState.Failed, runtime.State);
            StringAssert.Contains("startup", error.ToString());
            StringAssert.Contains("cleanup", error.ToString());
            StringAssert.Contains("cancel", error.ToString());
            Assert.AreEqual(3, error.Flatten().InnerExceptions.Count);

            // 종료 요청 없이 발생한 모듈 자체 취소 역시 성공한 종료로 취급하지 않는다.
            using var unrelated = new CancellationTokenSource();
            unrelated.Cancel();

            var cancelledRuntime = new ApplicationRuntime();
            var cancelledModules = new List<IApplicationModule>();
            cancelledModules.Add(new Module((context, token) => Task.FromCanceled(unrelated.Token)));
            var cancelled = cancelledRuntime.StartAsync(cancelledModules);

            var failure = Assert.ThrowsAsync<InvalidOperationException>
            (
                async () => await cancelled
            );
            Assert.IsInstanceOf<OperationCanceledException>(failure.InnerException);
            Assert.AreSame(failure, cancelledRuntime.Failure);
            Assert.AreEqual(ApplicationState.Failed, cancelledRuntime.State);
        }

    #endregion

    }
}
