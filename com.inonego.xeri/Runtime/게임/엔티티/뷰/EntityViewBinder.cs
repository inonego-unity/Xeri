/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : EntityViewBinder.cs
수정일 : 2026-06-17

# 설명
EntitySpawnRegistry 이벤트를 Entity view 생성/회수 흐름으로 바인딩한다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

namespace inonego.Xeri.Game
{
    // ============================================================
    /// <summary>
    /// Entity 모델 registry와 Entity view 계층을 바인딩한다.
    /// </summary>
    // ============================================================
    [Serializable]
    public sealed class EntityViewBinder<TEntityView, TEntity> : IBindable<EntitySpawnRegistryBase<TEntity>>
    where TEntityView : EntityViewBase<TEntity>
    where TEntity : class, IEntity
    {

    #region 필드

        private readonly EntityViewFactory<TEntityView, TEntity> factory = null;
        private readonly EntityViewRegistry<TEntityView, TEntity> registry = null;

        private EntitySpawnRegistryBase<TEntity> boundRegistry = null;

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 바인딩된 Entity registry.
        /// </summary>
        // ------------------------------------------------------------
        public EntitySpawnRegistryBase<TEntity> BoundRegistry => boundRegistry;

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

    #region 바인딩

        // ------------------------------------------------------------
        /// <summary>
        /// Entity registry에 바인딩하고 기존 스폰 상태를 view로 동기화한다.
        /// </summary>
        // ------------------------------------------------------------
        public void Bind(EntitySpawnRegistryBase<TEntity> registry)
        {
            if (registry == null)
            {
                throw new ArgumentNullException(nameof(registry));
            }

            if (boundRegistry != null)
            {
                Unbind();
            }

            boundRegistry = registry;

            foreach (var (key, entity) in registry.Spawned)
            {
                SpawnView(key, entity);
            }

            registry.OnSpawn   += OnEntitySpawn;
            registry.OnDespawn += OnEntityDespawn;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Entity registry 바인딩을 해제하고 모든 view를 회수한다.
        /// </summary>
        // ------------------------------------------------------------
        public void Unbind()
        {
            if (boundRegistry != null)
            {
                boundRegistry.OnSpawn   -= OnEntitySpawn;
                boundRegistry.OnDespawn -= OnEntityDespawn;
            }

            DespawnViewAll();

            boundRegistry = null;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 Entity registry 상태 기준으로 모든 view를 재생성한다.
        /// </summary>
        // ------------------------------------------------------------
        public void ReSpawnAll()
        {
            if (boundRegistry == null)
            {
                throw new InvalidOperationException("Entity registry가 바인딩되어 있지 않습니다.");
            }

            DespawnViewAll();

            foreach (var (key, entity) in boundRegistry.Spawned)
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
