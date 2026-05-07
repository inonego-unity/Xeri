/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : MonoEntitySpawnRegistry.cs
수정일 : 2026-05-07

# 설명
모노 엔티티 전용 스폰 레지스트리.
EntitySpawnRegistry 와 INeedToConnect 로 연결되어 엔티티 스폰/디스폰을 자동 동기화한다.
GameObject 는 IGameObjectProvider 로 획득·해제한다(기본은 PrefabGameObjectProvider).
========================================================================= BLOCK_HEADER_END */

using System;

using UnityEngine;

namespace inonego.Xeri.Game
{
    // ============================================================
    /// <summary>
    /// 모노 엔티티 스폰 레지스트리. 엔티티 레지스트리와 연결해 자동 동기화한다.
    /// </summary>
    // ============================================================
    [Serializable]
    public abstract class MonoEntitySpawnRegistry<TMonoEntity, TEntity>
        : SpawnRegistryBase<ulong, TMonoEntity>,
          INeedToConnect<EntitySpawnRegistryBase<TEntity>>
    where TMonoEntity : MonoEntity<TEntity>
    where TEntity : class, IEntity
    {

    #region 필드

        // ------------------------------------------------------------
        /// <summary>
        /// GameObject 획득·해제 프로바이더.
        /// </summary>
        // ------------------------------------------------------------
        public IGameObjectProvider GameObjectProvider => gameObjectProvider;

        [SerializeReference]
        private IGameObjectProvider gameObjectProvider = new PrefabGameObjectProvider();

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 연결된 엔티티 레지스트리.
        /// </summary>
        // ------------------------------------------------------------
        public EntitySpawnRegistryBase<TEntity> ConnectedRegistry => connectedRegistry;

        [SerializeReference, HideInInspector]
        private EntitySpawnRegistryBase<TEntity> connectedRegistry = null;

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// 기본 생성자. PrefabGameObjectProvider 를 사용한다.
        /// </summary>
        // ------------------------------------------------------------
        public MonoEntitySpawnRegistry() : base() {}

        // ------------------------------------------------------------
        /// <summary>
        /// 외부 GameObjectProvider 를 주입받는 생성자.
        /// </summary>
        // ------------------------------------------------------------
        public MonoEntitySpawnRegistry(IGameObjectProvider gameObjectProvider) : this()
        {
            if (gameObjectProvider == null)
            {
                throw new ArgumentNullException("GameObjectProvider 가 null입니다.");
            }

            this.gameObjectProvider = gameObjectProvider;
        }

    #endregion

    #region 자식 훅

        // ------------------------------------------------------------
        /// <summary>
        /// 자식 클래스의 Init 사전 작업 훅.
        /// </summary>
        // ------------------------------------------------------------
        protected virtual void OnInit(TMonoEntity spawnable, TEntity entity) {}

        // ------------------------------------------------------------
        /// <summary>
        /// 디스폰 직후 GameObject 를 풀로 반환한다. 자식 hook 은 _OnAfterDespawn.
        /// </summary>
        // ------------------------------------------------------------
        protected sealed override void OnAfterDespawn(TMonoEntity despawnable)
        {
            base.OnAfterDespawn(despawnable);

            _OnAfterDespawn(despawnable);

            var go = despawnable.gameObject;

            ReleaseGO(go);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 디스폰 직후 자식 hook.
        /// </summary>
        // ------------------------------------------------------------
        protected virtual void _OnAfterDespawn(TMonoEntity despawnable) {}

    #endregion

    #region 내부

        // ------------------------------------------------------------
        /// <summary>
        /// GameObject 를 비활성화 후 프로바이더로 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        private void ReleaseGO(GameObject go)
        {
            go.SetActive(false);

            gameObjectProvider.Release(go);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 프로바이더에서 GameObject 를 가져와 TMonoEntity 컴포넌트를 추출한다.
        /// </summary>
        // ------------------------------------------------------------
        protected TMonoEntity Acquire()
        {
            var go = GameObjectProvider.Acquire();

            go.SetActive(true);

            var monoEntity = go.GetComponent<TMonoEntity>();

            if (monoEntity == null)
            {
                ReleaseGO(go);

                throw new InvalidOperationException($"모노 엔티티 컴포넌트가 {go.name} 에 없습니다.");
            }

            return monoEntity;
        }

    #endregion

    #region 스폰

        // ----------------------------------------------------------------------
        /// <summary>
        /// 주어진 엔티티에 대응하는 모노 엔티티를 스폰한다. 동기화 진입점은 Connect.
        /// </summary>
        // ----------------------------------------------------------------------
        internal TMonoEntity Spawn(TEntity entity)
        {
            var monoEntity = Acquire();

            void InitAction(TMonoEntity spawnable)
            {
                OnInit(spawnable, entity);
                spawnable.Init(entity);
            }

            Spawn(monoEntity, InitAction);

            return monoEntity;
        }

    #endregion

    #region 엔티티 레지스트리 연결

        // ------------------------------------------------------------
        /// <summary>
        /// 엔티티 스폰 레지스트리와 연결해 엔티티 상태를 동기화한다.
        /// </summary>
        // ------------------------------------------------------------
        public virtual void Connect(EntitySpawnRegistryBase<TEntity> registry)
        {
            Disconnect();

            if (registry == null)
            {
                throw new ArgumentNullException("연결할 엔티티 스폰 레지스트리가 null입니다.");
            }

            registry.OnSpawn   += OnEntitySpawn;
            registry.OnDespawn += OnEntityDespawn;

            connectedRegistry = registry;

            SpawnAll();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 엔티티 스폰 레지스트리 연결을 해제하고 모든 모노 엔티티를 디스폰한다.
        /// </summary>
        // ------------------------------------------------------------
        public virtual void Disconnect()
        {
            if (connectedRegistry != null)
            {
                connectedRegistry.OnSpawn   -= OnEntitySpawn;
                connectedRegistry.OnDespawn -= OnEntityDespawn;

                connectedRegistry = null;
            }

            DespawnAll();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 연결된 엔티티 레지스트리의 현재 스폰된 엔티티 모두에 대응하는 모노 엔티티를 스폰한다.
        /// </summary>
        // ------------------------------------------------------------
        private void SpawnAll()
        {
            foreach (var (key, entity) in connectedRegistry.Spawned)
            {
                Spawn(entity);
            }
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// 모든 모노 엔티티를 디스폰한 뒤, 연결된 엔티티들에 대응하는 모노 엔티티를 다시 스폰한다.
        /// </summary>
        // ----------------------------------------------------------------------
        public void ReSpawnAll()
        {
            if (connectedRegistry == null)
            {
                throw new InvalidOperationException("엔티티 스폰 레지스트리와 연결되어 있지 않습니다.");
            }

            DespawnAll();

            SpawnAll();
        }

    #endregion

    #region 이벤트 핸들러

        // ------------------------------------------------------------
        /// <summary>
        /// 엔티티 레지스트리에서 엔티티가 스폰되면 대응 모노 엔티티를 스폰한다.
        /// </summary>
        // ------------------------------------------------------------
        protected virtual void OnEntitySpawn(ulong key, TEntity entity)
        {
            Spawn(entity);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 엔티티 레지스트리에서 엔티티가 디스폰되면 대응 모노 엔티티를 디스폰한다.
        /// </summary>
        // ------------------------------------------------------------
        protected virtual void OnEntityDespawn(ulong key, TEntity entity)
        {
            var monoEntity = Find(key);

            if (monoEntity != null)
            {
                monoEntity.Despawn();
            }
        }

    #endregion

    }
}
