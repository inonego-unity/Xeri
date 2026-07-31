/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : PoolBase.cs
수정일 : 2026-07-31

# 설명
오브젝트 풀링을 위한 추상 베이스 클래스.
Released와 Acquired 소유권을 인스턴스 참조 동일성으로 구분한다.
새 항목 생성과 항목별 상태 적용은 하위 구현에 위임한다.
Lease 획득은 일반 소비자 경계이고 직접 획득·반환·이동은 명시적 Pool 관리 경계다.
Thread-safe 사용과 ReleaseAll의 반환 Hook에서 같은 Pool 구조를 변경하는 재진입은 지원하지 않는다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace inonego.Xeri.Pool
{
    // ============================================================
    /// <summary>
    /// <br/> 오브젝트 풀링을 위한 추상 클래스입니다.
    /// <br/> 일반 소비자는 Lease 획득을 사용하고, 직접 API 호출자는 반환과 이동을 직접 관리합니다.
    /// <br/> Cross-thread 사용과 ReleaseAll 반환 Hook의 동일 Pool 구조 변경 재진입은 지원하지 않습니다.
    /// </summary>
    // ============================================================
    [Serializable]
    public abstract class PoolBase<T> : IPool<T>
    where T : class
    {
    #region 필드

        // ------------------------------------------------------------
        /// <summary>
        /// 풀에서 대기 중인 항목입니다.
        /// </summary>
        // ------------------------------------------------------------
        public IReadOnlyCollection<T> Released => released;

        protected Queue<T> released = new();

        // ------------------------------------------------------------
        /// <summary>
        /// 풀에서 사용 중인 항목입니다.
        /// </summary>
        // ------------------------------------------------------------
        public IReadOnlyCollection<T> Acquired => acquiredGens.Keys;

        protected Dictionary<T, long> acquiredGens =
            new(ReferenceEqualityComparer<T>.Instance);

        private long nextAcquiredGen = 0;
        private bool removeFromAcquiredOnRelease = true;

    #endregion

    #region 조회

        // ------------------------------------------------------------
        /// <summary>
        /// 항목이 현재 풀에서 사용 중인지 반환합니다.
        /// </summary>
        // ------------------------------------------------------------
        public bool IsAcquired(T item)
        {
            if (ReferenceEquals(item, null))
            {
                return false;
            }

            return acquiredGens.ContainsKey(item);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 항목이 현재 풀에서 대기 중인지 반환합니다.
        /// </summary>
        // ------------------------------------------------------------
        public bool IsReleased(T item)
        {
            if (ReferenceEquals(item, null))
            {
                return false;
            }

            foreach (var releasedItem in released)
            {
                if (ReferenceEquals(releasedItem, item))
                {
                    return true;
                }
            }

            return false;
        }

    #endregion

    #region 획득과 반환

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> 풀에서 항목만 획득합니다.
        /// <br/> 호출자가 대응하는 직접 반환 또는 소유권 이동을 관리해야 합니다.
        /// </summary>
        // ------------------------------------------------------------
        public virtual T Acquire() => AcquireInternal();

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> 풀에서 항목만 비동기로 획득합니다.
        /// <br/> 호출자가 대응하는 직접 반환 또는 소유권 이동을 관리해야 합니다.
        /// </summary>
        // ------------------------------------------------------------
        public virtual async Awaitable<T> AcquireAsync()
        {
            return await AcquireInternalAsync();
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> 풀에서 항목을 획득하고 현재 Generation에만 유효한 Lease를 반환합니다.
        /// <br/> 일반 소비자는 이 경계로 항목과 일회 반환 책임을 함께 소유합니다.
        /// <br/> 직접 반환이나 이동 후 오래된 Lease는 현재 소유권을 변경하지 않습니다.
        /// </summary>
        // ----------------------------------------------------------------------
        public Lease<T> AcquireLease()
        {
            var item = Acquire();
            var generation = acquiredGens[item];

            return new Lease<T>
            (
                item,
                () => ReleaseAcquisition
                (
                    item,
                    generation,
                    pushToReleased: true
                )
            );
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> 풀에서 항목을 비동기로 획득하고 현재 Generation에만 유효한 Lease를 반환합니다.
        /// <br/> 일반 소비자는 이 경계로 항목과 일회 반환 책임을 함께 소유합니다.
        /// <br/> 비동기 획득 취소는 Provider가 제공하는 별도 Domain 계약에 속합니다.
        /// </summary>
        // ----------------------------------------------------------------------
        public async Awaitable<Lease<T>> AcquireLeaseAsync()
        {
            var item = await AcquireAsync();
            var generation = acquiredGens[item];

            return new Lease<T>
            (
                item,
                () => ReleaseAcquisition
                (
                    item,
                    generation,
                    pushToReleased: true
                )
            );
        }

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> 사용 중인 항목을 풀에 반환합니다.
        /// <br/> 파생 구현은 예외 가능한 작업을 base 호출 전에 완료해야 합니다.
        /// <br/> base 호출 뒤에는 예외 가능한 작업을 수행하지 않아야 합니다.
        /// </summary>
        // ------------------------------------------------------------
        public virtual void Release(T item, bool pushToReleased = true)
        {
            if (ReferenceEquals(item, null))
            {
                throw new ArgumentNullException(nameof(item));
            }

            if (!IsAcquired(item))
            {
                throw new InvalidOperationException("현재 풀에서 사용 중이지 않은 항목을 반환할 수 없습니다.");
            }

            ReleaseInternal
            (
                item,
                removeFromAcquired: removeFromAcquiredOnRelease,
                pushToReleased
            );
        }

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> 현재 사용 중인 모든 항목의 반환을 각각 한 번 시도합니다.
        /// <br/> 한 항목의 실패는 나머지 항목을 막지 않으며 모든 오류를 마지막에 집계합니다.
        /// <br/> 반환 Hook은 순회 중 같은 Pool의 Acquired 구조를 변경하는 재진입을 하지 않아야 합니다.
        /// </summary>
        // ------------------------------------------------------------
        public void ReleaseAll(bool pushToReleased = true)
        {
            List<Exception> exceptions = new();
            removeFromAcquiredOnRelease = false;

            try
            {
                foreach (KeyValuePair<T, long> pair in acquiredGens)
                {
                    try
                    {
                        ReleaseAcquisition
                        (
                            pair.Key,
                            pair.Value,
                            pushToReleased,
                            removeFromAcquired: false
                        );
                    }
                    catch (Exception exception)
                    {
                        // Bulk 반환은 한 항목 실패가 나머지 초기 대상을 막지 않도록 오류를 모은다.
                        exceptions.Add(exception);
                    }
                }
            }
            finally
            {
                // 순회 중 보존한 초기 획득 목록을 모든 항목 처리 뒤 한 번에 종료한다.
                removeFromAcquiredOnRelease = true;
                acquiredGens.Clear();
            }

            if (exceptions.Count > 0)
            {
                throw new AggregateException
                (
                    "일부 Pool Item 반환에 실패했습니다.",
                    exceptions
                );
            }
        }

    #endregion

    #region 풀 사이 이동

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> 사용 중인 항목을 다른 풀의 Acquired 소유권으로 이동합니다.
        /// <br/> 대상이 인수를 완료한 뒤 현재 풀의 소유권을 해제합니다.
        /// </summary>
        // ----------------------------------------------------------------------
        public virtual void MoveAcquiredOneTo(IPool<T> other, T item)
        {
            if (other == null)
            {
                throw new ArgumentNullException(nameof(other));
            }

            if (ReferenceEquals(item, null))
            {
                throw new ArgumentNullException(nameof(item));
            }

            if (ReferenceEquals(this, other))
            {
                return;
            }

            if (!IsAcquired(item))
            {
                throw new InvalidOperationException("현재 풀에서 사용 중이지 않은 항목을 이동할 수 없습니다.");
            }

            if (other.IsAcquired(item) || other.IsReleased(item))
            {
                throw new InvalidOperationException("대상 풀이 이미 같은 항목을 관리하고 있습니다.");
            }

            // 대상 인수가 실패하면 현재 풀의 기존 소유권은 바뀌지 않습니다.
            other.AcquireInternal(item);
            acquiredGens.Remove(item);
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> 대기 중인 항목을 다른 풀의 Released 소유권으로 이동합니다.
        /// <br/> 대상 인수가 실패하면 이번 요청이 꺼낸 항목을 현재 풀로 되돌립니다.
        /// </summary>
        // ----------------------------------------------------------------------
        public virtual void MoveReleasedOneTo(IPool<T> other)
        {
            if (other == null)
            {
                throw new ArgumentNullException(nameof(other));
            }

            if (ReferenceEquals(this, other))
            {
                return;
            }

            var item = PopFromReleased();

            try
            {
                other.PushToReleased(item);
            }
            catch
            {
                PushToReleased(item);
                throw;
            }
        }

    #endregion

    #region 풀 관리

        // ------------------------------------------------------------
        /// <summary>
        /// 외부 항목을 풀의 Released 소유권으로 인수합니다.
        /// </summary>
        // ------------------------------------------------------------
        public virtual void PushToReleased(T item)
        {
            if (ReferenceEquals(item, null))
            {
                throw new ArgumentNullException(nameof(item));
            }

            if (IsAcquired(item) || IsReleased(item))
            {
                throw new InvalidOperationException("이미 현재 풀에서 관리 중인 항목을 추가할 수 없습니다.");
            }

            ReleaseInternal
            (
                item,
                removeFromAcquired: false,
                pushToReleased: true
            );
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> 대기 중인 항목을 풀에서 제거하여 반환합니다.
        /// <br/> 대기 항목이 없으면 새 항목을 생성합니다.
        /// </summary>
        // ----------------------------------------------------------------------
        public virtual T PopFromReleased()
        {
            if (released.Count > 0)
            {
                return released.Dequeue();
            }

            var item = AcquireNew();

            if (ReferenceEquals(item, null))
            {
                throw new InvalidOperationException("풀의 항목 생성 결과가 null입니다.");
            }

            return item;
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> 대기 중인 항목을 풀에서 제거하여 비동기로 반환합니다.
        /// <br/> 대기 항목이 없으면 새 항목을 비동기로 생성합니다.
        /// </summary>
        // ----------------------------------------------------------------------
        public virtual async Awaitable<T> PopFromReleasedAsync()
        {
            if (released.Count > 0)
            {
                return released.Dequeue();
            }

            var item = await AcquireNewAsync();
            if (ReferenceEquals(item, null))
            {
                throw new InvalidOperationException("풀의 항목 생성 결과가 null입니다.");
            }

            return item;
        }

    #endregion

    #region 하위 구현

        // ------------------------------------------------------------
        /// <summary>
        /// 새 항목을 생성합니다.
        /// </summary>
        // ------------------------------------------------------------
        protected abstract T AcquireNew();

        // ------------------------------------------------------------
        /// <summary>
        /// 새 항목을 비동기로 생성합니다.
        /// </summary>
        // ------------------------------------------------------------
        protected abstract Awaitable<T> AcquireNewAsync();

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> 획득 후보를 Acquired 소유권으로 전환합니다.
        /// <br/> 실패한 하위 구현은 후보 소유권을 소비하지 않아야 합니다.
        /// </summary>
        // ----------------------------------------------------------------------
        protected virtual void AcquireInternal(T item)
        {
            if (ReferenceEquals(item, null))
            {
                throw new ArgumentNullException(nameof(item));
            }

            if (IsAcquired(item) || IsReleased(item))
            {
                throw new InvalidOperationException("이미 현재 풀에서 관리 중인 항목을 획득할 수 없습니다.");
            }

            var generation = ++nextAcquiredGen;
            acquiredGens.Add(item, generation);
        }

        // --------------------------------------------------------------------------------------------------------------
        /// <summary>
        /// <br/> 항목의 Acquired와 Released 소유권을 갱신합니다.
        /// <br/> 파생 구현은 반환 상태 전환 중 예외 가능한 작업을 base 호출 전에 모두 완료해야 합니다.
        /// <br/> base 호출 뒤에는 예외 가능한 파생 작업을 수행하지 않아야 합니다.
        /// <br/> ReleaseAll은 순회 중 Acquisition 기록을 보존하므로 이 계약에 의존합니다.
        /// </summary>
        // --------------------------------------------------------------------------------------------------------------
        protected virtual void ReleaseInternal
        (
            T item,
            bool removeFromAcquired = true,
            bool pushToReleased = true
        )
        {
            if (removeFromAcquired)
            {
                acquiredGens.Remove(item);
            }

            if (pushToReleased)
            {
                released.Enqueue(item);
            }
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> Pool이 같은 Generation을 소유한 채 반환에 실패한 항목을 최종 정리합니다.
        /// <br/> 기본 Pool은 별도 물리 자원을 소유하지 않으므로 아무 작업도 하지 않습니다.
        /// </summary>
        // ----------------------------------------------------------------------
        protected virtual void OnDiscard(T item) {}

    #endregion

    #region 내부 처리

        // ------------------------------------------------------------
        /// <summary>
        /// 획득 후보를 준비하고 Acquired 소유권으로 전환합니다.
        /// </summary>
        // ------------------------------------------------------------
        protected T AcquireInternal()
        {
            var item = PopFromReleased();

            try
            {
                AcquireInternal(item);
                return item;
            }
            catch (Exception acquireException)
            {
                CleanupAcquireFailure(item, acquireException);
                throw;
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 획득 후보를 비동기로 준비하고 Acquired 소유권으로 전환합니다.
        /// </summary>
        // ------------------------------------------------------------
        protected async Awaitable<T> AcquireInternalAsync()
        {
            var item = await PopFromReleasedAsync();

            try
            {
                AcquireInternal(item);
                return item;
            }
            catch (Exception acquireException)
            {
                CleanupAcquireFailure(item, acquireException);
                throw;
            }
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> 지정한 Generation을 현재 소유할 때만 항목 반환을 한 번 시도합니다.
        /// <br/> 반환 실패 뒤에도 같은 획득을 소유하면 Record를 종료하고 항목을 폐기합니다.
        /// </summary>
        // ----------------------------------------------------------------------
        private void ReleaseAcquisition
        (
            T item,
            long generation,
            bool pushToReleased,
            bool removeFromAcquired = true
        )
        {
            if (!acquiredGens.TryGetValue(item, out var currentGeneration) || currentGeneration != generation)
            {
                return;
            }

            try
            {
                // 개별·일괄 반환 모두 같은 public virtual 확장 경계를 사용한다.
                Release(item, pushToReleased);
            }
            catch (Exception primaryException)
            {
                if (removeFromAcquired)
                {
                    acquiredGens.Remove(item);
                }

                try
                {
                    OnDiscard(item);
                }
                catch (Exception cleanupException)
                {
                    throw new AggregateException
                    (
                        "Item 반환과 실패 Item 정리가 모두 실패했습니다.",
                        primaryException,
                        cleanupException
                    );
                }

                throw;
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 획득하지 못한 후보를 현재 풀의 Released 소유권으로 되돌립니다.
        /// </summary>
        // ------------------------------------------------------------
        private void CleanupAcquireFailure(T item, Exception acquireException)
        {
            try
            {
                PushToReleased(item);
            }
            catch (Exception cleanupException)
            {
                throw new AggregateException
                (
                    "항목 획득과 후보 반환이 모두 실패했습니다.",
                    acquireException,
                    cleanupException
                );
            }
        }

    #endregion

    #region IPool 구현

        // ------------------------------------------------------------
        /// <summary>
        /// 다른 풀에서 전달한 항목을 Acquired 소유권으로 인수합니다.
        /// </summary>
        // ------------------------------------------------------------
        void IPool<T>.AcquireInternal(T item)
        {
            AcquireInternal(item);
        }

    #endregion

    }
}
