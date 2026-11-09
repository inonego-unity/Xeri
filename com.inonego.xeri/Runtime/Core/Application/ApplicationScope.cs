/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : ApplicationScope.cs
수정일 : 2026-11-03

# 설명
Application 실행 범위의 소유 자원과 진행 작업을 관리한다.
종료 시 하위 Scope 전체의 작업 완료를 기다리고 자식 Scope부터 소유 자원을 역순으로 해제한다.
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
    /// <br/> 명시적으로 소유한 자원과 진행 작업의 수명을 관리한다.
    /// <br/> Unity 메인 스레드 또는 호출자가 직렬화한 문맥에서 해제한다.
    /// </summary>
    // ============================================================
    public sealed class ApplicationScope
    {

    #region 내부 데이터

        private sealed class Ownership
        {
            public long Order;
            public IDisposable Resource;
            public Func<Task> CloseAsync;
        }

        private sealed class Operation
        {
            public Task Observation;
        }

    #endregion

    #region 필드

        // ------------------------------------------------------------
        /// <summary>
        /// Scope가 닫히기 시작했거나 이미 종료되었는지 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public bool IsClosing
        {
            get
            {
                lock (lifecycleSync)
                {
                    for (var scope = this; scope != null; scope = scope.parent)
                    {
                        if (scope.closePromise != null) return true;
                    }

                    return false;
                }
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 완료되지 않은 등록 작업 수를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public int ActiveOperationCount
        {
            get
            {
                lock (lifecycleSync)
                {
                    return operations.Count;
                }
            }
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> 등록된 자원·자식 Scope·종료 콜백의 전체 소유 항목 수를 반환한다.
        /// <br/> 진행 작업은 ActiveOperationCount로 별도 확인한다.
        /// </summary>
        // ----------------------------------------------------------------------
        public int OwnedEntryCount
        {
            get
            {
                lock (lifecycleSync)
                {
                    return owned.Count + children.Count;
                }
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Scope 종료를 요청할 때 전달되는 취소 신호.
        /// </summary>
        // ------------------------------------------------------------
        public CancellationToken CancellationToken => cancellationToken;

        private readonly CancellationToken cancellationToken;
        private readonly ApplicationScope parent;

        private static readonly object lifecycleSync = new();
        private static long nextOwnershipOrder;
        private readonly CancellationTokenSource cancellationSource;
        private readonly List<ApplicationScope> children = new();
        private readonly List<Ownership> owned = new();
        private readonly List<Operation> operations = new();
        private readonly List<Exception> operationFailures = new();
        private TaskCompletionSource<bool> cancellationPromise = null;
        private TaskCompletionSource<bool> closePromise = null;

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// 부모와 독립적인 최상위 실행 Scope를 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        public ApplicationScope()
        {
            cancellationSource = new CancellationTokenSource();
            cancellationToken = cancellationSource.Token;
        }

        private ApplicationScope(ApplicationScope parent)
        {
            this.parent = parent;
            cancellationSource = CancellationTokenSource.CreateLinkedTokenSource(parent.CancellationToken);
            cancellationToken = cancellationSource.Token;
        }

    #endregion

    #region 자원 소유

        // ------------------------------------------------------------
        /// <summary>
        /// 전달받은 객체의 Dispose 책임을 Scope에 등록한다.
        /// </summary>
        // ------------------------------------------------------------
        public T Own<T>(T resource) where T : class, IDisposable
        {
            if (resource == null)
            {
                throw new ArgumentNullException(nameof(resource));
            }

            lock (lifecycleSync)
            {
                RequireOpen();
                if (owned.Exists(entry => ReferenceEquals(entry.Resource, resource)))
                {
                    throw new InvalidOperationException("이미 소유한 자원입니다.");
                }

                var ownership = new Ownership();
                ownership.Order = ++nextOwnershipOrder;
                ownership.Resource = resource;
                ownership.CloseAsync = () => CloseOwnedResource(resource);
                owned.Add(ownership);
            }

            return resource;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 비동기 정리 작업의 종료 책임을 등록한다.
        /// </summary>
        // ------------------------------------------------------------
        public void OnClose(Func<Task> closeAsync)
        {
            if (closeAsync == null)
            {
                throw new ArgumentNullException(nameof(closeAsync));
            }

            lock (lifecycleSync)
            {
                RequireOpen();
                var ownership = new Ownership();
                ownership.Order = ++nextOwnershipOrder;
                ownership.CloseAsync = closeAsync;
                owned.Add(ownership);
            }
        }

    #endregion

    #region 자식 Scope

        // ------------------------------------------------------------
        /// <summary>
        /// 부모와 함께 종료되는 자식 Scope를 만든다.
        /// </summary>
        // ------------------------------------------------------------
        public ApplicationScope CreateChild()
        {
            lock (lifecycleSync)
            {
                RequireOpen();
                var child = new ApplicationScope(this);
                children.Add(child);
                return child;
            }
        }

    #endregion

    #region 소유권 이전

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> 등록된 자원의 종료 책임을 다른 열린 Scope로 이전한다.
        /// <br/> 이전한 항목의 원래 등록 순서와 해제 책임을 보존한다.
        /// <br/> 실패한 이전은 원래 Scope의 소유권을 변경하지 않는다.
        /// <br/> 자원에 연결된 취소 토큰은 이전되지 않으므로 호출자가 수명을 확인한다.
        /// </summary>
        // ----------------------------------------------------------------------
        public void TransferTo<T>(ApplicationScope destination, T resource) where T : class, IDisposable
        {
            if (destination == null)
            {
                throw new ArgumentNullException(nameof(destination));
            }

            if (resource == null)
            {
                throw new ArgumentNullException(nameof(resource));
            }

            if (ReferenceEquals(this, destination))
            {
                throw new ArgumentException("동일한 Scope로 이전할 수 없습니다.", nameof(destination));
            }

            // 모든 Scope의 소유권 변경과 종료 진입은 같은 경계에서 직렬화한다.
            lock (lifecycleSync)
            {
                RequireOpen();
                destination.RequireOpen();
                int index = owned.FindIndex(entry => ReferenceEquals(entry.Resource, resource));
                if (index < 0)
                {
                    throw new InvalidOperationException("현재 Scope가 해당 자원을 소유하지 않습니다.");
                }

                if (destination.owned.Exists(entry => ReferenceEquals(entry.Resource, resource)))
                {
                    throw new InvalidOperationException("대상 Scope가 이미 소유한 자원입니다.");
                }

                var ownership = owned[index];
                int destinationIndex = destination.owned.FindIndex(entry => entry.Order > ownership.Order);
                if (destinationIndex < 0)
                {
                    destination.owned.Add(ownership);
                }
                else
                {
                    destination.owned.Insert(destinationIndex, ownership);
                }

                owned.RemoveAt(index);
            }
        }

    #endregion

    #region 취소 경계

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> 취소를 요청하고 취소 콜백의 완료 결과를 반환한다.
        /// <br/> 중복 요청과 Scope 종료는 동일한 취소 작업을 관찰한다.
        /// </summary>
        // ------------------------------------------------------------
        public Task RequestCancellationAsync()
        {
            TaskCompletionSource<bool> promise;
            lock (lifecycleSync)
            {
                if (cancellationPromise != null) return cancellationPromise.Task;

                cancellationPromise = new TaskCompletionSource<bool>
                (
                    TaskCreationOptions.RunContinuationsAsynchronously
                );
                promise = cancellationPromise;
            }

            // 취소 콜백은 잠금 밖에서 실행된다. 재진입 호출은 위 Task를 공유한다.
            try
            {
                cancellationSource.Cancel();
                promise.TrySetResult(true);
            }
            catch (Exception exception)
            {
                promise.TrySetException(exception);
            }

            return promise.Task;
        }

    #endregion

    #region 진행 작업

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> 진행 중인 작업을 Scope에 등록하고 종료 시 완료를 관찰한다.
        /// <br/> 작업 자체는 호출자가 시작하며 Scope가 스케줄링하지 않는다.
        /// </summary>
        // ------------------------------------------------------------
        public Task Track(Task task)
        {
            if (task == null)
            {
                throw new ArgumentNullException(nameof(task));
            }

            lock (lifecycleSync)
            {
                RequireOpen();
                if (cancellationPromise != null || cancellationSource.IsCancellationRequested)
                {
                    throw new OperationCanceledException(CancellationToken);
                }

                var operation = new Operation();
                operations.Add(operation);
                operation.Observation = task.ContinueWith
                (
                    completed =>
                    {
                        lock (lifecycleSync)
                        {
                            if (completed.IsFaulted)
                            {
                                operationFailures.AddRange(completed.Exception.Flatten().InnerExceptions);
                            }

                            operations.Remove(operation);
                        }
                    },
                    CancellationToken.None,
                    TaskContinuationOptions.ExecuteSynchronously,
                    TaskScheduler.Default
                );
            }

            return task;
        }

    #endregion

    #region 종료

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> 하위 Scope 등록을 막고 모든 작업 완료를 기다린다.
        /// <br/> 자식 Scope부터, 자원은 등록 역순으로 정리한다.
        /// <br/> 중복 종료는 동일한 완료 결과를 공유한다.
        /// </summary>
        // ------------------------------------------------------------
        public Task CloseAsync()
        {
            HashSet<ApplicationScope> closing;
            Task completion;

            lock (lifecycleSync)
            {
                if (closePromise != null) return closePromise.Task;

                // 전체 하위 트리를 한 번에 닫아 작업 등록과 소유권 변경을 차단한다.
                closing = new HashSet<ApplicationScope>();
                MarkClosingSubtree(this, closing);
                completion = closePromise.Task;
            }

            _ = CloseCoreAsync(closing);
            return completion;
        }

        private static void MarkClosingSubtree
        (
            ApplicationScope scope,
            HashSet<ApplicationScope> closing
        )
        {
            // 독립적으로 종료 중인 자식은 그 종료 작업이 이미 하위 트리를 소유한다.
            if (scope.closePromise != null) return;

            scope.closePromise = new TaskCompletionSource<bool>
            (
                TaskCreationOptions.RunContinuationsAsynchronously
            );
            closing.Add(scope);

            foreach (var child in scope.children)
            {
                MarkClosingSubtree(child, closing);
            }
        }

        private async Task CloseCoreAsync(HashSet<ApplicationScope> closing)
        {
            // 연결된 토큰을 통해 하위 Scope 전체에 취소를 전파한다.
            Exception cancellationFailure = null;
            try
            {
                await RequestCancellationAsync();
            }
            catch (Exception exception)
            {
                cancellationFailure = exception;
            }

            Exception drainFailure = null;
            try
            {
                // 트리 전체 작업이 끝나기 전에는 어느 Scope의 자원도 해제하지 않는다.
                Task[] pending;
                lock (lifecycleSync)
                {
                    var tasks = new List<Task>();
                    var scopes = new Stack<ApplicationScope>();
                    scopes.Push(this);
                    while (scopes.Count > 0)
                    {
                        var scope = scopes.Pop();
                        foreach (var operation in scope.operations)
                        {
                            tasks.Add(operation.Observation);
                        }

                        foreach (var child in scope.children)
                        {
                            scopes.Push(child);
                        }
                    }

                    pending = tasks.ToArray();
                }

                if (pending.Length > 0)
                {
                    await Task.WhenAll(pending);
                }
            }
            catch (Exception exception)
            {
                drainFailure = exception;
            }

            await FinishCloseAsync(closing, cancellationFailure, drainFailure);
        }

        private async Task FinishCloseAsync
        (
            HashSet<ApplicationScope> closing,
            Exception cancellationFailure = null,
            Exception drainFailure = null
        )
        {
            var failures = new List<Exception>();
            if (drainFailure != null)
            {
                failures.Add(drainFailure);
            }

            try
            {
                if (cancellationFailure != null)
                {
                    failures.Add(cancellationFailure);
                }
                else
                {
                    try
                    {
                        await RequestCancellationAsync();
                    }
                    catch (Exception exception)
                    {
                        failures.Add(exception);
                    }
                }

                // 자식이 종료·정리된 뒤 부모 Scope의 소유 자원을 반환한다.
                for (int index = children.Count - 1; index >= 0; index--)
                {
                    var child = children[index];

                    try
                    {
                        if (closing.Contains(child))
                        {
                            await child.FinishCloseAsync(closing);
                        }

                        // 자식이 이미 자체 종료 중이어도 완료 결과와 실패를 동일하게 관찰한다.
                        await child.CloseAsync();
                    }
                    catch (Exception exception)
                    {
                        failures.Add(exception);
                    }
                }

                lock (lifecycleSync)
                {
                    failures.AddRange(operationFailures);
                    operationFailures.Clear();
                }

                for (int index = owned.Count - 1; index >= 0; index--)
                {
                    var entry = owned[index];

                    try
                    {
                        var cleanup = entry.CloseAsync() ??
                            throw new InvalidOperationException("종료 작업이 null Task를 반환했습니다.");
                        await cleanup;
                    }
                    catch (Exception exception)
                    {
                        failures.Add(exception);
                    }
                }

                lock (lifecycleSync)
                {
                    owned.Clear();
                    children.Clear();
                }
            }
            catch (Exception exception)
            {
                failures.Add(exception);
            }
            finally
            {
                try
                {
                    cancellationSource.Dispose();
                }
                catch (Exception exception)
                {
                    failures.Add(exception);
                }

                if (failures.Count == 0)
                {
                    closePromise.TrySetResult(true);
                }
                else
                {
                    closePromise.TrySetException(new AggregateException("Application Scope 종료 실패.", failures));
                }
            }
        }

        private static Task CloseOwnedResource(IDisposable resource)
        {
            resource.Dispose();
            return Task.CompletedTask;
        }

        private void RequireOpen()
        {
            for (var scope = this; scope != null; scope = scope.parent)
            {
                if (scope.closePromise == null) continue;

                throw new ObjectDisposedException(nameof(ApplicationScope));
            }
        }

    #endregion

    }
}
