/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : GOCompPool.cs
수정일 : 2026-05-01

# 설명
Unity 컴포넌트에 대한 오브젝트 풀링 클래스.
IGameObjectProvider를 구현하므로 풀 자체를 MonoBoardBase 등 프로바이더 슬롯에 직접 연결할 수 있다.
Acquire 시 SetActive(true) + SetParent(Parent), Release 시 SetActive(false) + SetParent(Pool)을 자동 처리.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using inonego.Xeri;

namespace inonego.Xeri.Pool
{

    // ===============================================================================
    /// <summary>
    /// <br/>유니티에서 사용하는 컴포넌트에 대하여 오브젝트 풀링을 할 수 있습니다.
    /// <br/>컴포넌트를 풀링하기 위해서는 해당 T 컴포넌트가 최상단에 포함된 프리팹이 필요합니다.
    /// </summary>
    // ===============================================================================
    [Serializable]
    public class GOCompPool<T> : PoolBase<T>, IGOCompPool<T> where T : Component
    {

    #region 필드

        // ------------------------------------------------------------
        /// <summary>
        /// 게임 오브젝트를 생성하는데 사용하는 게임 오브젝트 프로바이더입니다.
        /// </summary>
        // ------------------------------------------------------------
        [SerializeReference]
        protected IGameObjectProvider gameObjectProvider = new PrefabGameObjectProvider();
        public IGameObjectProvider GameObjectProvider => gameObjectProvider;

        // ------------------------------------------------------------
        /// <summary>
        /// Release되는 경우 게임 오브젝트가 Pool의 하위에 위치합니다.
        /// </summary>
        // ------------------------------------------------------------
        [SerializeField]
        protected Transform pool = null;
        public Transform Pool
        {
            get => pool;
            set => pool = value;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Acquire되는 경우 게임 오브젝트가 Parent의 하위에 위치합니다.
        /// </summary>
        // ------------------------------------------------------------
        public Transform Parent
        {
            get => gameObjectProvider.Parent;
            set => gameObjectProvider.Parent = value;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 다음 Acquire/Release 시 적용할 위치 유지 여부 플래그입니다.
        /// </summary>
        // ------------------------------------------------------------
        protected bool nextWorldPositionStays = true;

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// 기본 생성자입니다.
        /// </summary>
        // ------------------------------------------------------------
        public GOCompPool() : base() {}

        // ------------------------------------------------------------
        /// <summary>
        /// 게임 오브젝트 프로바이더를 지정하는 생성자입니다.
        /// </summary>
        // ------------------------------------------------------------
        public GOCompPool(IGameObjectProvider gameObjectProvider) : base()
        {
            if (gameObjectProvider == null)
            {
                throw new ArgumentNullException("게임 오브젝트 프로바이더가 null입니다.");
            }

            this.gameObjectProvider = gameObjectProvider;
        }

    #endregion

    #region PoolBase 오버라이드

        // ------------------------------------------------------------
        /// <summary>
        /// 새로운 컴포넌트를 생성합니다.
        /// </summary>
        // ------------------------------------------------------------
        protected override T AcquireNew()
        {
            if (GameObjectProvider == null)
            {
                throw new NullReferenceException("GameObjectProvider가 설정되지 않았습니다.");
            }

            var gameObject = GameObjectProvider.Acquire();
            var comp = gameObject.GetComponent<T>();

            if (comp == null)
            {
                throw new Exception($"게임 오브젝트 '{gameObject.name}'에서 컴포넌트 '{typeof(T).Name}'을(를) 찾을 수 없습니다.");
            }

            return comp;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 새로운 컴포넌트를 비동기로 생성합니다.
        /// </summary>
        // ------------------------------------------------------------
        protected override async Awaitable<T> AcquireNewAsync()
        {
            if (GameObjectProvider == null)
            {
                throw new NullReferenceException("GameObjectProvider가 설정되지 않았습니다.");
            }

            var gameObject = await GameObjectProvider.AcquireAsync();
            var comp = gameObject.GetComponent<T>();

            if (comp == null)
            {
                throw new Exception($"게임 오브젝트 '{gameObject.name}'에서 컴포넌트 '{typeof(T).Name}'을(를) 찾을 수 없습니다.");
            }

            return comp;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 풀에서 컴포넌트를 가져옵니다.
        /// </summary>
        // ------------------------------------------------------------
        public override T Acquire()
        {
            return Acquire(worldPositionStays: true);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 풀에서 컴포넌트를 가져옵니다.
        /// </summary>
        // ------------------------------------------------------------
        public T Acquire(bool worldPositionStays)
        {
            nextWorldPositionStays = worldPositionStays;
            return base.Acquire();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 풀에서 컴포넌트를 비동기로 가져옵니다.
        /// </summary>
        // ------------------------------------------------------------
        public override async Awaitable<T> AcquireAsync()
        {
            return await AcquireAsync(worldPositionStays: true);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 풀에서 컴포넌트를 비동기로 가져옵니다.
        /// </summary>
        // ------------------------------------------------------------
        public async Awaitable<T> AcquireAsync(bool worldPositionStays)
        {
            return await AcquireInternalAsync(worldPositionStays);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 풀에 컴포넌트를 반환합니다.
        /// </summary>
        // ------------------------------------------------------------
        public override void Release(T item, bool pushToReleased = true)
        {
            Release(item, pushToReleased, worldPositionStays: true);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 풀에 컴포넌트를 반환합니다.
        /// </summary>
        // ------------------------------------------------------------
        public void Release(T item, bool pushToReleased = true, bool worldPositionStays = true)
        {
            nextWorldPositionStays = worldPositionStays;
            base.Release(item, pushToReleased);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 풀에 이미 존재하는 아이템을 추가합니다.
        /// </summary>
        // ------------------------------------------------------------
        public override void PushToReleased(T item)
        {
            PushToReleased(item, worldPositionStays: true);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 풀에 이미 존재하는 아이템을 추가합니다.
        /// </summary>
        // ------------------------------------------------------------
        public void PushToReleased(T item, bool worldPositionStays)
        {
            nextWorldPositionStays = worldPositionStays;
            base.PushToReleased(item);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Acquired된 아이템을 다른 풀의 Acquired로 이동합니다.
        /// </summary>
        // ------------------------------------------------------------
        public override void MoveAcquiredOneTo(IPool<T> other, T item)
        {
            MoveAcquiredOneTo(other, item, worldPositionStays: true);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Acquired된 아이템을 다른 풀의 Acquired로 이동합니다.
        /// </summary>
        // ------------------------------------------------------------
        public void MoveAcquiredOneTo(IPool<T> other, T item, bool worldPositionStays)
        {
            if (other is GOCompPool<T> otherGoPool)
            {
                otherGoPool.nextWorldPositionStays = worldPositionStays;
            }

            nextWorldPositionStays = worldPositionStays;
            base.MoveAcquiredOneTo(other, item);
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/>Released된 아이템을 다른 풀의 Released로 이동합니다.
        /// <br/>풀에 남아있는 오브젝트가 없으면 새로운 오브젝트를 생성하여 이동합니다.
        /// </summary>
        // ----------------------------------------------------------------------
        public override void MoveReleasedOneTo(IPool<T> other)
        {
            MoveReleasedOneTo(other, worldPositionStays: true);
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/>Released된 아이템을 다른 풀의 Released로 이동합니다.
        /// <br/>풀에 남아있는 오브젝트가 없으면 새로운 오브젝트를 생성하여 이동합니다.
        /// </summary>
        // ----------------------------------------------------------------------
        public void MoveReleasedOneTo(IPool<T> other, bool worldPositionStays)
        {
            if (other is GOCompPool<T> otherGoPool)
            {
                otherGoPool.nextWorldPositionStays = worldPositionStays;
            }

            nextWorldPositionStays = worldPositionStays;
            base.MoveReleasedOneTo(other);
        }

    #endregion

    #region 내부 처리

        // ------------------------------------------------------------
        /// <summary>
        /// 비동기 Acquire의 내부 처리입니다.
        /// </summary>
        // ------------------------------------------------------------
        protected async Awaitable<T> AcquireInternalAsync(bool worldPositionStays)
        {
            T item = await PopFromReleasedAsync();
            AcquireInternal(item, worldPositionStays);
            return item;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 아이템을 풀에서 가져왔을 때의 내부 처리입니다.
        /// </summary>
        // ------------------------------------------------------------
        protected override void AcquireInternal(T item)
        {
            AcquireInternal(item, nextWorldPositionStays);

            // 플래그 초기화
            nextWorldPositionStays = true;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 아이템을 풀에서 가져왔을 때의 내부 처리입니다.
        /// </summary>
        // ------------------------------------------------------------
        protected void AcquireInternal(T item, bool worldPositionStays)
        {
            base.AcquireInternal(item);

            if (item.transform.parent != Parent)
            {
                item.transform.SetParent(Parent, worldPositionStays);
            }

            item.gameObject.SetActive(true);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 아이템을 풀에 반환했을 때의 내부 처리입니다.
        /// </summary>
        // ------------------------------------------------------------
        protected override void ReleaseInternal(T item, bool removeFromAcquired = true, bool pushToReleased = true)
        {
            base.ReleaseInternal(item, removeFromAcquired, pushToReleased);

            if (item.transform.parent != Pool)
            {
                item.transform.SetParent(Pool, nextWorldPositionStays);
            }

            item.gameObject.SetActive(false);

            // 플래그 초기화
            nextWorldPositionStays = true;
        }

    #endregion

    #region IGameObjectProvider 구현

        // ------------------------------------------------------------
        /// <summary>
        /// 게임 오브젝트를 가져옵니다.
        /// </summary>
        // ------------------------------------------------------------
        GameObject IGameObjectProvider.Acquire(bool worldPositionStays)
        {
            var comp = Acquire(worldPositionStays);
            return comp.gameObject;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 게임 오브젝트를 비동기로 가져옵니다.
        /// </summary>
        // ------------------------------------------------------------
        async Awaitable<GameObject> IGameObjectProvider.AcquireAsync(bool worldPositionStays)
        {
            var comp = await AcquireAsync(worldPositionStays);
            return comp.gameObject;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 게임 오브젝트를 반환합니다.
        /// </summary>
        // ------------------------------------------------------------
        void IGameObjectProvider.Release(GameObject go, bool worldPositionStays)
        {
            if (go == null)
            {
                throw new ArgumentNullException("타겟이 null입니다.");
            }

            if (go.TryGetComponent(out T comp))
            {
                Release(comp, true, worldPositionStays);
            }
        }

    #endregion

    }
}
