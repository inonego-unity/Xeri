/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : PoolBase.cs
수정일 : 2026-05-01

# 설명
오브젝트 풀링을 위한 추상 베이스 클래스.
released(Queue)와 acquired(HashSet)로 대기/사용 중 상태를 분리 관리한다.
실제 오브젝트 생성 로직(AcquireNew/AcquireNewAsync)만 하위 클래스에서 구현한다.
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
    public abstract class PoolBase<T> : IPool<T> where T : class
    {

    #region 필드

        // ------------------------------------------------------------
        /// <summary>
        /// 풀에 남아있는 오브젝트 목록입니다.
        /// </summary>
        // ------------------------------------------------------------
        protected Queue<T> released = new();
        public IReadOnlyCollection<T> Released => released;

        // ------------------------------------------------------------
        /// <summary>
        /// 풀에 사용중인 오브젝트 목록입니다.
        /// </summary>
        // ------------------------------------------------------------
        protected HashSet<T> acquired = new();
        public IReadOnlyCollection<T> Acquired => acquired;

    #endregion

    #region 메서드

        // ------------------------------------------------------------
        /// <summary>
        /// 풀에 오브젝트가 사용중인지 여부를 반환합니다.
        /// </summary>
        // ------------------------------------------------------------
        public bool IsAcquired(T item)
        {
            return acquired.Contains(item);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 풀에 오브젝트가 반환된 상태인지 여부를 반환합니다.
        /// </summary>
        // ------------------------------------------------------------
        public bool IsReleased(T item)
        {
            // TODO - O(N)에 대해서 최적화 필요
            return released.Contains(item);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 풀에서 오브젝트를 가져옵니다.
        /// </summary>
        // ------------------------------------------------------------
        public virtual T Acquire()
        {
            return AcquireInternal();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 풀에서 오브젝트를 비동기로 가져옵니다.
        /// </summary>
        // ------------------------------------------------------------
        public virtual async Awaitable<T> AcquireAsync()
        {
            return await AcquireInternalAsync();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 풀에 오브젝트를 반환합니다.
        /// </summary>
        // ------------------------------------------------------------
        public virtual void Release(T item, bool pushToReleased = true)
        {
            if (item == null)
            {
                throw new ArgumentNullException();
            }

            if (!IsAcquired(item))
            {
                throw new Exception($"풀에 존재하지 않는 아이템 '{item}'을 제거하려고 했습니다.");
            }

            ReleaseInternal(item, pushToReleased: pushToReleased);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 풀에 있는 모든 오브젝트를 반환합니다.
        /// </summary>
        // ------------------------------------------------------------
        public void ReleaseAll(bool pushToReleased = true)
        {
            foreach (var item in acquired)
            {
                ReleaseInternal(item, removeFromAcquired: false, pushToReleased: pushToReleased);
            }

            acquired.Clear();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Acquired된 아이템을 다른 풀의 Acquired로 이동합니다.
        /// </summary>
        // ------------------------------------------------------------
        public virtual void MoveAcquiredOneTo(IPool<T> other, T item)
        {
            if (other == null || item == null)
            {
                throw new ArgumentNullException();
            }

            if (!IsAcquired(item))
            {
                throw new Exception($"풀에 존재하지 않는 아이템을 다른 풀로 이동하려고 했습니다.");
            }

            ReleaseInternal(item, pushToReleased: false);
            other.AcquireInternal(item);
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/>Released된 아이템을 다른 풀의 Released로 이동합니다.
        /// <br/>풀에 남아있는 오브젝트가 없으면 새로운 오브젝트를 생성하여 이동합니다.
        /// </summary>
        // ----------------------------------------------------------------------
        public virtual void MoveReleasedOneTo(IPool<T> other)
        {
            if (other == null)
            {
                throw new ArgumentNullException();
            }

            var item = PopFromReleased();
            other.PushToReleased(item);
        }

    #endregion

    #region 풀 관리 메서드

        // ------------------------------------------------------------
        /// <summary>
        /// 오브젝트를 풀에 추가합니다.
        /// </summary>
        // ------------------------------------------------------------
        public virtual void PushToReleased(T item)
        {
            if (item == null)
            {
                throw new ArgumentNullException();
            }

            if (IsAcquired(item))
            {
                throw new Exception($"사용중인 아이템을 풀에 추가하려고 했습니다.");
            }

            // TODO - O(N)에 대해서 최적화 필요
            if (IsReleased(item))
            {
                throw new Exception($"풀에 이미 존재하는 아이템을 추가하려고 했습니다.");
            }

            ReleaseInternal(item, removeFromAcquired: false);
        }

        // -------------------------------------------------------------------------
        /// <summary>
        /// <br/>풀에 남아있는 오브젝트를 풀에서 제거하고 반환합니다.
        /// <br/>풀에 남아있는 오브젝트가 없으면 새로운 오브젝트를 생성하여 반환합니다.
        /// </summary>
        // -------------------------------------------------------------------------
        public virtual T PopFromReleased()
        {
            if (released.Count > 0)
            {
                return released.Dequeue();
            }

            return AcquireNew();
        }

        // -------------------------------------------------------------------------
        /// <summary>
        /// <br/>풀에 남아있는 오브젝트를 풀에서 비동기로 제거하고 반환합니다.
        /// <br/>풀에 남아있는 오브젝트가 없으면 새로운 오브젝트를 생성하여 반환합니다.
        /// </summary>
        // -------------------------------------------------------------------------
        public virtual async Awaitable<T> PopFromReleasedAsync()
        {
            if (released.Count > 0)
            {
                // TODO - 비동기 처리 시 Lock 처리 필요
                return released.Dequeue();
            }

            return await AcquireNewAsync();
        }

    #endregion

    #region 내부 구현용 메서드

        // ------------------------------------------------------------
        /// <summary>
        /// 새로운 오브젝트를 생성합니다.
        /// </summary>
        // ------------------------------------------------------------
        protected abstract T AcquireNew();

        // ------------------------------------------------------------
        /// <summary>
        /// 새로운 오브젝트를 비동기로 생성합니다.
        /// </summary>
        // ------------------------------------------------------------
        protected abstract Awaitable<T> AcquireNewAsync();

        // ------------------------------------------------------------
        /// <summary>
        /// 아이템을 풀에서 가져왔을 때의 내부 처리입니다.
        /// </summary>
        // ------------------------------------------------------------
        protected T AcquireInternal()
        {
            T item = PopFromReleased();
            AcquireInternal(item);
            return item;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 아이템을 비동기로 풀에서 가져왔을 때의 내부 처리입니다.
        /// </summary>
        // ------------------------------------------------------------
        protected async Awaitable<T> AcquireInternalAsync()
        {
            T item = await PopFromReleasedAsync();
            AcquireInternal(item);
            return item;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 아이템을 Acquired 상태로 전환합니다.
        /// </summary>
        // ------------------------------------------------------------
        protected virtual void AcquireInternal(T item)
        {
            acquired.Add(item);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 아이템을 Released 상태로 전환합니다.
        /// </summary>
        // ------------------------------------------------------------
        protected virtual void ReleaseInternal(T item, bool removeFromAcquired = true, bool pushToReleased = true)
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

    #region IPool<T> 구현

        // ------------------------------------------------------------
        /// <summary>
        /// 아이템을 풀에서 가져오기 위한 내부 처리입니다.
        /// </summary>
        // ------------------------------------------------------------
        void IPool<T>.AcquireInternal(T item)
        {
            AcquireInternal(item);
        }

    #endregion

    }
}
