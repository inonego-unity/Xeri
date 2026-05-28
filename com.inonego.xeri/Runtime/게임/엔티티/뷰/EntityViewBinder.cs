/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : EntityViewBinder.cs
수정일 : 2026-05-28

# 설명
EntitySpawnRegistry 이벤트를 Entity view 생성/회수 흐름으로 연결한다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

namespace inonego.Xeri.Game
{
    // ============================================================
    /// <summary>
    /// Entity 모델 registry와 Entity view 계층을 연결한다.
    /// </summary>
    // ============================================================
    [Serializable]
    public sealed class EntityViewBinder<TEntityView, TEntity> : INeedToConnect<EntitySpawnRegistryBase<TEntity>>
    where TEntityView : EntityViewBase<TEntity>
    where TEntity : class, IEntity
    {

    #region 필드

        private readonly EntityViewFactory<TEntityView, TEntity> factory = null;
        private readonly EntityViewRegistry<TEntityView, TEntity> registry = null;

        private EntitySpawnRegistryBase<TEntity> connectedRegistry = null;

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 연결된 Entity registry.
        /// </summary>
        // ------------------------------------------------------------
        public EntitySpawnRegistryBase<TEntity> ConnectedRegistry => connectedRegistry;

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 등록된 view map.
        /// </summary>
        // ------------------------------------------------------------
        public IReadOnlyDictionary<ulong, TEntityView> Views => registry.Views;

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// Entity view 생성기와 registry를 주입받는다.
        /// </summary>
        // ------------------------------------------------------------
        public EntityViewBinder
        (
            EntityViewFactory<TEntityView, TEntity> factory,
            EntityViewRegistry<TEntityView, TEntity> registry = null
        ) : base()
        {
            this.factory  = factory ?? throw new ArgumentNullException(nameof(factory));
            this.registry = registry ?? new EntityViewRegistry<TEntityView, TEntity>();
        }

    #endregion

    #region 연결

        // ------------------------------------------------------------
        /// <summary>
        /// Entity registry에 연결하고 기존 스폰 상태를 view로 동기화한다.
        /// </summary>
        // ------------------------------------------------------------
        public void Connect(EntitySpawnRegistryBase<TEntity> registry)
        {
            if (registry == null)
            {
                throw new ArgumentNullException(nameof(registry));
            }

            if (connectedRegistry != null)
            {
                Disconnect();
            }

            connectedRegistry = registry;

            foreach (var (key, entity) in registry.Spawned)
            {
                SpawnView(key, entity);
            }

            registry.OnSpawn   += OnEntitySpawn;
            registry.OnDespawn += OnEntityDespawn;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Entity registry 연결을 해제하고 모든 view를 회수한다.
        /// </summary>
        // ------------------------------------------------------------
        public void Disconnect()
        {
            if (connectedRegistry != null)
            {
                connectedRegistry.OnSpawn   -= OnEntitySpawn;
                connectedRegistry.OnDespawn -= OnEntityDespawn;
            }

            DespawnViewAll();

            connectedRegistry = null;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 Entity registry 상태 기준으로 모든 view를 재생성한다.
        /// </summary>
        // ------------------------------------------------------------
        public void ReSpawnAll()
        {
            if (connectedRegistry == null)
            {
                throw new InvalidOperationException("Entity registry가 연결되어 있지 않습니다.");
            }

            DespawnViewAll();

            foreach (var (key, entity) in connectedRegistry.Spawned)
            {
                SpawnView(key, entity);
            }
        }

    #endregion

    #region 조회

        // ------------------------------------------------------------
        /// <summary>
        /// key에 대응하는 view를 조회한다.
        /// </summary>
        // ------------------------------------------------------------
        public bool TryGetView(ulong key, out TEntityView view)
        {
            return registry.TryGet(key, out view);
        }

    #endregion

    #region View 동기화

        // ------------------------------------------------------------
        /// <summary>
        /// Entity에 대응하는 view를 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        private void SpawnView(ulong key, TEntity entity)
        {
            if (registry.Contains(key))
            {
                DespawnView(key);
            }

            var view = factory.Create(entity);

            registry.Register(key, view);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// key에 대응하는 view를 회수한다.
        /// </summary>
        // ------------------------------------------------------------
        private void DespawnView(ulong key)
        {
            if (!registry.TryGet(key, out var view))
            {
                return;
            }

            registry.Unregister(key);

            factory.Release(view);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 모든 view를 회수한다.
        /// </summary>
        // ------------------------------------------------------------
        private void DespawnViewAll()
        {
            var keys = new List<ulong>(registry.Views.Keys);

            foreach (var key in keys)
            {
                DespawnView(key);
            }
        }

    #endregion

    #region 이벤트 핸들러

        // ------------------------------------------------------------
        /// <summary>
        /// Entity spawn 이벤트를 view spawn으로 변환한다.
        /// </summary>
        // ------------------------------------------------------------
        private void OnEntitySpawn(ulong key, TEntity entity)
        {
            SpawnView(key, entity);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Entity despawn 이벤트를 view despawn으로 변환한다.
        /// </summary>
        // ------------------------------------------------------------
        private void OnEntityDespawn(ulong key, TEntity entity)
        {
            DespawnView(key);
        }

    #endregion

    }
}
