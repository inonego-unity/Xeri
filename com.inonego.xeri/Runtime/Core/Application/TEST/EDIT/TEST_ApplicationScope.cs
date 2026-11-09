/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TEST_ApplicationScope.cs
수정일 : 2026-11-03

# 설명
ApplicationScope의 소유권, 종료 순서와 비동기 작업 수명을 검증한다.

# 테스트 구성
 E: 소유권 이전과 종료 순서
 S: 진행 작업 종료
 X: 예외 격리와 정리 오류
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
    /// ApplicationScope 소유권과 작업 정리 계약 테스트.
    /// </summary>
    // ============================================================
    public sealed class TEST_ApplicationScope
    {

    #region 헬퍼

        private sealed class OwnedItem : IDisposable
        {
            private readonly Action onDispose;
            public OwnedItem(Action onDispose)
            {
                this.onDispose = onDispose;
            }

            public void Dispose()
            {
                onDispose();
            }
        }

    #endregion

    #region E-1: 소유권과 종료 순서

        // ------------------------------------------------------------
        /// <summary>
        /// 부모 종료 시 자식 작업을 모두 기다리고 자식부터 자원을 정리한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public async Task TEST_ApplicationScope_부모자식_자원역순종료()
        {
            var order = new List<string>();
            var root = new ApplicationScope();
            root.Own(new OwnedItem(() => order.Add("first")));
            var child = root.CreateChild();
            child.Own(new OwnedItem(() => order.Add("child")));
            root.Own(new OwnedItem(() => order.Add("last")));

            var operation = new TaskCompletionSource<bool>
            (
                TaskCreationOptions.RunContinuationsAsynchronously
            );
            _ = child.Track(operation.Task);

            var first = root.CloseAsync();
            var second = root.CloseAsync();
            Assert.AreSame(first, second);
            Assert.IsTrue(child.IsClosing);
            Assert.IsFalse(first.IsCompleted);
            Assert.AreEqual(0, order.Count);
            Assert.Throws<ObjectDisposedException>
            (
                () => child.Own(new OwnedItem(() => order.Add("late")))
            );

            operation.SetResult(true);
            await first;

            Assert.AreEqual("child,last,first", string.Join(",", order));
            Assert.Throws<ObjectDisposedException>(() => root.CreateChild());
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 이전된 자원은 원래 Scope가 아닌 대상 Scope에서 종료된다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public async Task TEST_ApplicationScope_자원이전후_소유자만해제()
        {
            var order = new List<string>();
            var parent = new ApplicationScope();
            var pending = parent.CreateChild();
            var provider = pending.Own(new OwnedItem(() => order.Add("provider")));
            var consumer = pending.Own(new OwnedItem(() => order.Add("consumer")));
            var closed = new ApplicationScope();
            await closed.CloseAsync();

            Assert.Throws<ObjectDisposedException>(() => pending.TransferTo(closed, provider));
            Assert.AreEqual(0, order.Count);

            // 이전 호출 순서와 무관하게 원래 자원 획득 순서의 역순으로 정리한다.
            pending.TransferTo(parent, consumer);
            pending.TransferTo(parent, provider);
            var operation = new TaskCompletionSource<bool>
            (
                TaskCreationOptions.RunContinuationsAsynchronously
            );
            _ = pending.Track(operation.Task);

            var closing = parent.CloseAsync();
            Assert.IsFalse(closing.IsCompleted);
            Assert.AreEqual(0, order.Count);

            operation.SetResult(true);
            await closing;
            Assert.AreEqual("consumer,provider", string.Join(",", order));
        }

    #endregion

    #region S-1: 진행 작업과 취소 수명

        // ------------------------------------------------------------
        /// <summary>
        /// 종료 시 취소를 알리고 추적된 작업이 끝난 뒤 자원을 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public async Task TEST_ApplicationScope_추적작업완료후_자원해제()
        {
            var pending = new TaskCompletionSource<bool>
            (
                TaskCreationOptions.RunContinuationsAsynchronously
            );
            var scope = new ApplicationScope();
            bool released = false;
            scope.Own(new OwnedItem(() => released = true));
            _ = scope.Track(pending.Task);

            var closing = scope.CloseAsync();
            Assert.IsTrue(scope.CancellationToken.IsCancellationRequested);
            Assert.IsFalse(closing.IsCompleted);
            Assert.IsFalse(released);

            pending.SetResult(true);
            await closing;
            Assert.IsTrue(released);
            Assert.AreEqual(0, scope.ActiveOperationCount);
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// Scope가 외부 작업의 중단을 요청한 후 완료를 기다리고 소유 자원을 해제한다.
        /// </summary>
        // ----------------------------------------------------------------------
        [Test]
        public async Task TEST_ApplicationScope_취소콜백이작업완료와_자원해제보다먼저실행()
        {
            var order = new List<string>();
            var completion = new TaskCompletionSource<bool>
            (
                TaskCreationOptions.RunContinuationsAsynchronously
            );
            var scope = new ApplicationScope();
            scope.Own(new OwnedItem(() => order.Add("dispose")));
            void CancelPending()
            {
                order.Add("cancel");
                completion.SetResult(true);
            }

            var registration = scope.CancellationToken.Register(CancelPending);
            scope.Own(new Lease(registration.Dispose));
            _ = scope.Track(completion.Task);

            await scope.CloseAsync();

            Assert.AreEqual("cancel,dispose", string.Join(",", order));
            Assert.AreEqual(0, scope.ActiveOperationCount);
            Assert.AreEqual(0, scope.OwnedEntryCount);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 취소 콜백에서 다시 취소와 종료를 요청해도 자원을 한 번만 정리한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public async Task TEST_ApplicationScope_취소콜백재진입시_단일종료()
        {
            var scope = new ApplicationScope();
            int cancellations = 0;
            int releases = 0;
            Task closing = null;
            scope.Own(new OwnedItem(() => releases++));
            var registration = scope.CancellationToken.Register
            (
                () =>
                {
                    cancellations++;
                    _ = scope.RequestCancellationAsync();
                    closing = scope.CloseAsync();
                }
            );
            scope.Own(new Lease(registration.Dispose));

            var first = scope.RequestCancellationAsync();
            Assert.AreSame(first, scope.RequestCancellationAsync());
            await first;
            Assert.IsNotNull(closing);
            await closing;

            Assert.AreEqual(1, cancellations);
            Assert.AreEqual(1, releases);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 다른 스레드의 취소 콜백이 끝나기 전에 Scope 자원을 해제하지 않는다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public async Task TEST_ApplicationScope_동시취소종료시_콜백완료후정리()
        {
            var entered = new TaskCompletionSource<bool>
            (
                TaskCreationOptions.RunContinuationsAsynchronously
            );
            var release = new TaskCompletionSource<bool>
            (
                TaskCreationOptions.RunContinuationsAsynchronously
            );
            var scope = new ApplicationScope();
            bool disposed = false;
            scope.Own(new OwnedItem(() => disposed = true));
            var registration = scope.CancellationToken.Register
            (
                () =>
                {
                    entered.TrySetResult(true);
                    release.Task.GetAwaiter().GetResult();
                }
            );
            scope.Own(new Lease(registration.Dispose));

            var requesting = Task.Run(scope.RequestCancellationAsync);
            Task closing = null;
            try
            {
                var reached = await Task.WhenAny(entered.Task, Task.Delay(TimeSpan.FromSeconds(5)));
                Assert.AreSame(entered.Task, reached);
                closing = scope.CloseAsync();
                Assert.IsFalse(closing.IsCompleted);
                Assert.IsFalse(disposed);
            }
            finally
            {
                release.TrySetResult(true);
            }

            await Task.WhenAll(requesting, closing);
            Assert.IsTrue(disposed);
        }

    #endregion

    #region X-1: 오류 격리와 보고

        // ------------------------------------------------------------
        /// <summary>
        /// 취소·작업·해제 일부가 실패해도 나머지 소유 자원을 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_ApplicationScope_복수실패시_나머지자원정리()
        {
            var order = new List<string>();
            var scope = new ApplicationScope();
            scope.Own(new OwnedItem(() => order.Add("first")));
            void ThrowOnClose()
            {
                order.Add("fault");
                throw new InvalidOperationException("cleanup");
            }

            scope.Own(new OwnedItem(ThrowOnClose));
            var child = scope.CreateChild();
            void FailChild()
            {
                order.Add("child-fault");
                throw new InvalidOperationException("child cleanup");
            }

            child.Own(new OwnedItem(FailChild));
            var registration = scope.CancellationToken.Register(() => throw new InvalidOperationException("cancel"));
            scope.Own(new Lease(registration.Dispose));
            scope.Track(Task.FromException(new InvalidOperationException("operation")));

            var cancellationError = Assert.ThrowsAsync<AggregateException>
            (
                async () => await scope.RequestCancellationAsync()
            );
            Assert.IsTrue(cancellationError.ToString().Contains("cancel"));

            var error = Assert.ThrowsAsync<AggregateException>
            (
                async () => await scope.CloseAsync()
            );

            Assert.AreEqual("child-fault,fault,first", string.Join(",", order));
            Assert.AreEqual(4, error.Flatten().InnerExceptions.Count);
        }

    #endregion

    }
}
