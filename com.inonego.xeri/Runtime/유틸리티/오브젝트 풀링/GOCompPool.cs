/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : GOCompPool.cs
수정일 : 2026-07-29

# 설명
Unity Component를 대상으로 하는 오브젝트 풀.
IGameObjectProvider에서 GameObject를 공급받아 획득 시 위치 정책과 활성 상태를 적용한다.
동일 풀의 비동기 공급 대기는 서로 중첩될 수 있다.
========================================================================= BLOCK_HEADER_END */

using System;

using UnityEngine;

using inonego.Xeri;

namespace inonego.Xeri.Pool
{
    // ================================================================================================
    /// <summary>
    /// <br/> Unity Component를 참조 동일성 기준으로 풀링합니다.
    /// <br/> T Component는 공급되는 GameObject의 최상단에 존재해야 합니다.
    /// </summary>
    // ================================================================================================
    [Serializable]
    public class GOCompPool<T> : PoolBase<T>, IGOCompPool<T>
    where T : Component
    {
    #region 필드

        // ------------------------------------------------------------
        /// <summary>
        /// 새 GameObject를 공급하는 Provider입니다.
        /// </summary>
        // ------------------------------------------------------------
        public IGameObjectProvider GameObjectProvider => gameObjectProvider;

        [SerializeReference]
        protected IGameObjectProvider gameObjectProvider = new PrefabGameObjectProvider();

        // ------------------------------------------------------------
        /// <summary>
        /// Released Component가 배치되는 부모 Transform입니다.
        /// </summary>
        // ------------------------------------------------------------
        public Transform Pool
        {
            get => pool;
            set => pool = value;
        }

        [SerializeField]
        protected Transform pool = null;

        // ------------------------------------------------------------
        /// <summary>
        /// Acquired Component가 배치되는 부모 Transform입니다.
        /// </summary>
        // ------------------------------------------------------------
        public Transform Parent
        {
            get => RequiredProvider.Parent;
            set => RequiredProvider.Parent = value;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 설정된 유효한 Provider입니다.
        /// </summary>
        // ------------------------------------------------------------
        private IGameObjectProvider RequiredProvider
        {
            get
            {
                return gameObjectProvider
                    ?? throw new InvalidOperationException("GameObjectProvider가 설정되지 않았습니다.");
            }
        }

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// 기본 Provider로 풀을 생성합니다.
        /// </summary>
        // ------------------------------------------------------------
        public GOCompPool() : base() {}

        // ------------------------------------------------------------
        /// <summary>
        /// 지정한 Provider로 풀을 생성합니다.
        /// </summary>
        // ------------------------------------------------------------
        public GOCompPool(IGameObjectProvider gameObjectProvider) : base()
        {
            this.gameObjectProvider = gameObjectProvider
                ?? throw new ArgumentNullException(nameof(gameObjectProvider));
        }

    #endregion

    #region 획득

        // ------------------------------------------------------------
        /// <summary>
        /// 기본 위치 유지 정책으로 활성 Component를 획득합니다.
        /// </summary>
        // ------------------------------------------------------------
        public override T Acquire() => Acquire(worldPositionStays: true);

        // ------------------------------------------------------------
        /// <summary>
        /// 지정한 위치 유지 정책으로 활성 Component를 획득합니다.
        /// </summary>
        // ------------------------------------------------------------
        public T Acquire(bool worldPositionStays)
        {
            var item = TakeForAcquire(worldPositionStays);

            try
            {
                AcquireInternal(item, worldPositionStays);
                return item;
            }
            catch
            {
                // 현재 풀에서 꺼낸 후보는 획득 실패 시 같은 풀의 Released 소유권으로 되돌린다.
                PushToReleased(item, worldPositionStays);
                throw;
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 기본 위치 유지 정책으로 활성 Component를 비동기 획득합니다.
        /// </summary>
        // ------------------------------------------------------------
        public override async Awaitable<T> AcquireAsync()
        {
            return await AcquireAsync(worldPositionStays: true);
        }

        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// <br/> 지정한 위치 유지 정책으로 활성 Component를 비동기 획득합니다.
        /// <br/> Provider 대기는 다른 획득 요청을 막지 않으며 완료된 요청별로 소유권을 전환합니다.
        /// </summary>
        // ----------------------------------------------------------------------------------------------------
        public async Awaitable<T> AcquireAsync(bool worldPositionStays)
        {
            var item = await TakeForAcquireAsync(worldPositionStays);

            try
            {
                AcquireInternal(item, worldPositionStays);
                return item;
            }
            catch
            {
                // 현재 풀에서 꺼낸 후보는 획득 실패 시 같은 풀의 Released 소유권으로 되돌린다.
                PushToReleased(item, worldPositionStays);
                throw;
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 기본 위치 유지 정책으로 새 Component를 생성합니다.
        /// </summary>
        // ------------------------------------------------------------
        protected override T AcquireNew() => AcquireNew(worldPositionStays: true);

        // ------------------------------------------------------------
        /// <summary>
        /// 기본 위치 유지 정책으로 새 Component를 비동기 생성합니다.
        /// </summary>
        // ------------------------------------------------------------
        protected override async Awaitable<T> AcquireNewAsync()
        {
            return await AcquireNewAsync(worldPositionStays: true);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Provider에서 새 Component를 획득합니다.
        /// </summary>
        // ------------------------------------------------------------
        private T AcquireNew(bool worldPositionStays)
        {
            var provider = RequiredProvider;
            var gameObject = provider.Acquire(worldPositionStays);

            return GetComponentOrRelease
            (
                provider,
                gameObject,
                worldPositionStays
            );
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Provider에서 새 Component를 비동기 획득합니다.
        /// </summary>
        // ------------------------------------------------------------
        private async Awaitable<T> AcquireNewAsync(bool worldPositionStays)
        {
            var provider = RequiredProvider;
            var gameObject = await provider.AcquireAsync(worldPositionStays);

            await Awaitable.MainThreadAsync();

            return GetComponentOrRelease
            (
                provider,
                gameObject,
                worldPositionStays
            );
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> Component에 요청 상태를 적용한 뒤 Acquired 소유권으로 인수합니다.
        /// <br/> 예외가 발생하면 현재 풀의 소유권은 소비하지 않습니다.
        /// </summary>
        // ----------------------------------------------------------------------
        protected virtual void AcquireInternal
        (
            T item,
            bool worldPositionStays
        )
        {
            ValidateIncoming(item);
            base.AcquireInternal(item);

            try
            {
                // 활성화 콜백이 현재 풀 소유권을 조회할 수 있도록 Acquired 확정 뒤 상태를 적용한다.
                ApplyAcquiredState(item, worldPositionStays);
            }
            catch
            {
                // 외부 인수에도 사용하는 경계이므로 이번 호출이 추가한 Acquired 소유권만 되돌린다.
                base.ReleaseInternal
                (
                    item,
                    removeFromAcquired: true,
                    pushToReleased: false
                );

                throw;
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 다른 풀이 전달한 Component를 기본 활성 상태로 인수합니다.
        /// </summary>
        // ------------------------------------------------------------
        protected override void AcquireInternal(T item)
        {
            AcquireInternal(item, worldPositionStays: true);
        }

    #endregion

    #region 반환

        // ------------------------------------------------------------
        /// <summary>
        /// 기본 위치 유지 정책으로 Component를 반환합니다.
        /// </summary>
        // ------------------------------------------------------------
        public override void Release(T item, bool pushToReleased = true)
        {
            Release
            (
                item,
                pushToReleased,
                worldPositionStays: true
            );
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 지정한 위치 유지 정책으로 Component를 반환합니다.
        /// </summary>
        // ------------------------------------------------------------
        public void Release
        (
            T item,
            bool pushToReleased = true,
            bool worldPositionStays = true
        )
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            if (!IsAcquired(item))
            {
                throw new InvalidOperationException("현재 풀에서 사용 중이지 않은 Component를 반환할 수 없습니다.");
            }

            ReleaseInternal
            (
                item,
                removeFromAcquired: true,
                pushToReleased,
                worldPositionStays
            );
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 기본 위치 유지 정책으로 외부 Component를 Released 소유권에 인수합니다.
        /// </summary>
        // ------------------------------------------------------------
        public override void PushToReleased(T item)
        {
            PushToReleased(item, worldPositionStays: true);
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> 지정한 위치 유지 정책으로 외부 Component를 Released 소유권에 인수합니다.
        /// <br/> 상태 적용이 실패하면 현재 풀의 소유권은 소비하지 않습니다.
        /// </summary>
        // ----------------------------------------------------------------------
        public void PushToReleased(T item, bool worldPositionStays)
        {
            ValidateIncoming(item);

            ReleaseInternal
            (
                item,
                removeFromAcquired: false,
                pushToReleased: true,
                worldPositionStays
            );
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 기본 위치 유지 정책으로 Component의 반환 상태를 적용합니다.
        /// </summary>
        // ------------------------------------------------------------
        protected override void ReleaseInternal
        (
            T item,
            bool removeFromAcquired = true,
            bool pushToReleased = true
        )
        {
            ReleaseInternal
            (
                item,
                removeFromAcquired,
                pushToReleased,
                worldPositionStays: true
            );
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> Component에 반환 상태를 적용한 뒤 풀 소유권을 갱신합니다.
        /// <br/> 상태 적용이 실패하면 기존 소유권을 유지합니다.
        /// </summary>
        // ----------------------------------------------------------------------
        protected virtual void ReleaseInternal
        (
            T item,
            bool removeFromAcquired,
            bool pushToReleased,
            bool worldPositionStays
        )
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            // Unity 비활성 상태를 적용한 뒤 Acquired와 Released 소유권을 갱신한다.
            ApplyReleasedState(item, worldPositionStays);

            base.ReleaseInternal
            (
                item,
                removeFromAcquired,
                pushToReleased
            );
        }

    #endregion

    #region 풀 사이 이동

        // ------------------------------------------------------------
        /// <summary>
        /// 기본 위치 유지 정책으로 Acquired Component를 다른 풀로 이동합니다.
        /// </summary>
        // ------------------------------------------------------------
        public override void MoveAcquiredOneTo(IPool<T> other, T item)
        {
            MoveAcquiredOneTo
            (
                other,
                item,
                worldPositionStays: true
            );
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> 지정한 위치 유지 정책으로 Acquired Component를 다른 풀로 이동합니다.
        /// <br/> 대상 인수가 완료된 뒤 현재 풀의 컬렉션 소유권을 해제합니다.
        /// </summary>
        // ----------------------------------------------------------------------
        public void MoveAcquiredOneTo
        (
            IPool<T> other,
            T item,
            bool worldPositionStays
        )
        {
            if (other == null)
            {
                throw new ArgumentNullException(nameof(other));
            }

            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            if (ReferenceEquals(this, other))
            {
                return;
            }

            if (!IsAcquired(item))
            {
                throw new InvalidOperationException("현재 풀에서 사용 중이지 않은 Component를 이동할 수 없습니다.");
            }

            if (other.IsAcquired(item) || other.IsReleased(item))
            {
                throw new InvalidOperationException("대상 풀이 이미 같은 Component를 관리하고 있습니다.");
            }

            try
            {
                if (other is GOCompPool<T> otherGoPool)
                {
                    otherGoPool.AcquireInternal(item, worldPositionStays);
                }
                else
                {
                    other.AcquireInternal(item);
                }
            }
            catch
            {
                // 대상이 인수를 완료하지 못하면 원본 풀의 활성 상태와 부모를 다시 적용한다.
                ApplyAcquiredState(item, worldPositionStays);
                throw;
            }

            acquired.Remove(item);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 기본 위치 유지 정책으로 Released Component를 다른 풀로 이동합니다.
        /// </summary>
        // ------------------------------------------------------------
        public override void MoveReleasedOneTo(IPool<T> other)
        {
            MoveReleasedOneTo(other, worldPositionStays: true);
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> 지정한 위치 유지 정책으로 Released Component를 다른 풀로 이동합니다.
        /// <br/> 대상 인수가 실패하면 이번 요청이 꺼낸 Component를 현재 풀로 반환합니다.
        /// </summary>
        // ----------------------------------------------------------------------
        public void MoveReleasedOneTo(IPool<T> other, bool worldPositionStays)
        {
            if (other == null)
            {
                throw new ArgumentNullException(nameof(other));
            }

            if (ReferenceEquals(this, other))
            {
                return;
            }

            var item = TakeForAcquire(worldPositionStays);

            try
            {
                if (other is GOCompPool<T> otherGoPool)
                {
                    otherGoPool.PushToReleased(item, worldPositionStays);
                }
                else
                {
                    other.PushToReleased(item);
                }
            }
            catch
            {
                PushToReleased(item, worldPositionStays);
                throw;
            }
        }

    #endregion

    #region 내부 처리

        // ------------------------------------------------------------
        /// <summary>
        /// 요청의 위치 정책으로 획득 후보를 준비합니다.
        /// </summary>
        // ------------------------------------------------------------
        private T TakeForAcquire(bool worldPositionStays)
        {
            if (released.Count > 0)
            {
                return released.Dequeue();
            }

            return AcquireNew(worldPositionStays);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 요청의 위치 정책으로 획득 후보를 비동기 준비합니다.
        /// </summary>
        // ------------------------------------------------------------
        private async Awaitable<T> TakeForAcquireAsync(bool worldPositionStays)
        {
            if (released.Count > 0)
            {
                return released.Dequeue();
            }

            return await AcquireNewAsync(worldPositionStays);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Acquired Component의 부모와 활성 상태를 적용합니다.
        /// </summary>
        // ------------------------------------------------------------
        private void ApplyAcquiredState(T item, bool worldPositionStays)
        {
            if (item.transform.parent != Parent)
            {
                item.transform.SetParent(Parent, worldPositionStays);
            }

            item.gameObject.SetActive(true);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Released Component를 비활성화하고 Pool 부모에 배치합니다.
        /// </summary>
        // ------------------------------------------------------------
        private void ApplyReleasedState(T item, bool worldPositionStays)
        {
            item.gameObject.SetActive(false);

            if (item.transform.parent != Pool)
            {
                item.transform.SetParent(Pool, worldPositionStays);
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 외부에서 전달된 Component와 현재 풀 소유권을 검증합니다.
        /// </summary>
        // ------------------------------------------------------------
        private void ValidateIncoming(T item)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            if (IsAcquired(item) || IsReleased(item))
            {
                throw new InvalidOperationException("이미 현재 풀에서 관리 중인 Component를 인수할 수 없습니다.");
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 공급된 GameObject에서 필수 Component를 반환합니다.
        /// </summary>
        // ------------------------------------------------------------
        private static T GetComponentOrRelease
        (
            IGameObjectProvider provider,
            GameObject gameObject,
            bool worldPositionStays
        )
        {
            if (gameObject == null)
            {
                throw new InvalidOperationException("GameObjectProvider가 유효한 GameObject를 반환하지 않았습니다.");
            }

            if (gameObject.TryGetComponent(out T component))
            {
                return component;
            }

            var componentException = new InvalidOperationException
            (
                $"GameObject '{gameObject.name}'에서 Component '{typeof(T).Name}'을 찾을 수 없습니다."
            );

            try
            {
                provider.Release(gameObject, worldPositionStays);
            }
            catch (Exception releaseException)
            {
                throw new AggregateException
                (
                    "필수 Component 확인 실패 후 GameObject를 Provider에 반환하지 못했습니다.",
                    componentException,
                    releaseException
                );
            }

            throw componentException;
        }

    #endregion

    #region IGameObjectProvider 구현

        // ------------------------------------------------------------
        /// <summary>
        /// GameObject를 풀에서 획득합니다.
        /// </summary>
        // ------------------------------------------------------------
        GameObject IGameObjectProvider.Acquire(bool worldPositionStays)
        {
            return Acquire(worldPositionStays).gameObject;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// GameObject를 풀에서 비동기 획득합니다.
        /// </summary>
        // ------------------------------------------------------------
        async Awaitable<GameObject> IGameObjectProvider.AcquireAsync
        (
            bool worldPositionStays
        )
        {
            var component = await AcquireAsync(worldPositionStays);
            return component.gameObject;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// GameObject를 풀의 Released 소유권으로 반환합니다.
        /// </summary>
        // ------------------------------------------------------------
        void IGameObjectProvider.Release(GameObject gameObject, bool worldPositionStays)
        {
            if (gameObject == null)
            {
                throw new ArgumentNullException(nameof(gameObject));
            }

            if (!gameObject.TryGetComponent(out T component))
            {
                throw new InvalidOperationException
                (
                    $"GameObject '{gameObject.name}'에서 Component '{typeof(T).Name}'을 찾을 수 없습니다."
                );
            }

            Release
            (
                component,
                pushToReleased: true,
                worldPositionStays
            );
        }

    #endregion

    }
}
