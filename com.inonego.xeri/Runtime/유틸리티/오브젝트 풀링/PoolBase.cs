/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : PoolBase.cs
수정일 : 2026-07-28

# 설명
오브젝트 풀링을 위한 추상 베이스 클래스.
Released와 Acquired 소유권을 인스턴스 참조 동일성으로 구분한다.
새 항목 생성과 항목별 상태 적용은 하위 구현에 위임한다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace inonego.Xeri.Pool
{
    // ============================================================
    /// <summary>
    /// 오브젝트 풀링을 위한 추상 클래스입니다.
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
        public IReadOnlyCollection<T> Acquired => acquired;

        protected HashSet<T> acquired = new(ReferenceEqualityComparer<T>.Instance);

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

            return acquired.Contains(item);
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
        /// 풀에서 항목을 획득합니다.
        /// </summary>
        // ------------------------------------------------------------
        public virtual T Acquire() => AcquireInternal();

        // ------------------------------------------------------------
        /// <summary>
        /// 풀에서 항목을 비동기로 획득합니다.
        /// </summary>
        // ------------------------------------------------------------
        public virtual async Awaitable<T> AcquireAsync()
        {
            return await AcquireInternalAsync();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 사용 중인 항목을 풀에 반환합니다.
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
                removeFromAcquired: true,
                pushToReleased
            );
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 사용 중인 모든 항목을 반환합니다.
        /// </summary>
        // ------------------------------------------------------------
        public void ReleaseAll(bool pushToReleased = true)
        {
            var snapshot = new List<T>(acquired);

            foreach (var item in snapshot)
            {
                Release(item, pushToReleased);
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
            acquired.Remove(item);
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

            acquired.Add(item);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 항목의 Acquired와 Released 소유권을 갱신합니다.
        /// </summary>
        // ------------------------------------------------------------
        protected virtual void ReleaseInternal
        (
            T item,
            bool removeFromAcquired = true,
            bool pushToReleased = true
        )
        {
            if (removeFromAcquired)
            {
                acquired.Remove(item);
            }

            if (pushToReleased)
            {
                released.Enqueue(item);
            }
        }

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
