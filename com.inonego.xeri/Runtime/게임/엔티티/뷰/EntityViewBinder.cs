/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : EntityViewBinder.cs
수정일 : 2026-07-31

# 설명
EntitySpawnRegistry의 필수 소유권 바인딩을 Entity View 생성·회수 흐름으로 동기화한다.
등록된 Entity View는 Factory 반환이 완료될 때까지 Binder가 소유한다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections.Generic;

namespace inonego.Xeri.Game
{
    // ============================================================
    /// <summary>
    /// Entity Registry와 Entity View 생성·반환을 연결한다.
    /// </summary>
    // ============================================================
    [Serializable]
    public sealed class EntityViewBinder<TEntityView, TEntity> :
        IBindable<EntitySpawnRegistryBase<TEntity>>,
        ISpawnRegistryBinding<ulong, TEntity>
    where TEntityView : EntityViewBase<TEntity>
    where TEntity : class, IEntity
    {

    #region 필드

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 바인딩된 Entity Registry.
        /// </summary>
        // ------------------------------------------------------------
        public EntitySpawnRegistryBase<TEntity> BoundRegistry => boundRegistry;

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 등록된 View 매핑.
        /// </summary>
        // ------------------------------------------------------------
        public IReadOnlyDictionary<ulong, TEntityView> Views => registry.Views;

        private readonly EntityViewFactory<TEntityView, TEntity> factory = null;
        private readonly EntityViewRegistry<TEntityView, TEntity> registry = null;

        private EntitySpawnRegistryBase<TEntity> boundRegistry = null;

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// Factory와 View Registry를 주입받는다.
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
        /// Entity Registry에 바인딩하고 현재 Spawned 항목을 동기화한다.
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

            registry.AttachBinding(this);
            boundRegistry = registry;

            try
            {
                foreach (var (key, entity) in registry.Spawned)
                {
                    SpawnView(key, entity);
                }
            }
            catch
            {
                // 초기 동기화가 실패하면 바인딩을 먼저 해제하고 생성된 View를 회수한다.
                registry.DetachBinding(this);
                boundRegistry = null;
                DespawnViewAll(DespawnReason.RegistryCleanup);
                throw;
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Entity Registry 바인딩과 현재 View를 해제한다.
        /// </summary>
        // ------------------------------------------------------------
        public void Unbind()
        {
            var registry = boundRegistry;

            if (registry != null)
            {
                registry.DetachBinding(this);
            }

            // 필수 바인딩이 끝난 상태를 먼저 공개하고 남은 View 소유권은 반환 성공까지 Registry에 유지한다.
            boundRegistry = null;
            DespawnViewAll(DespawnReason.RegistryCleanup);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 Entity Registry 상태를 기준으로 View를 다시 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        public void ReSpawnAll()
        {
            var registry = boundRegistry
                ?? throw new InvalidOperationException("Entity Registry가 바인딩되어 있지 않습니다.");

            DespawnViewAll(DespawnReason.RegistryCleanup);

            foreach (var (key, entity) in registry.Spawned)
            {
                SpawnView(key, entity);
            }
        }

    #endregion

    #region 조회

        // ------------------------------------------------------------
        /// <summary>
        /// Key에 대응하는 View를 조회한다.
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
        /// Entity에 대응하는 View를 동기 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        private void SpawnView
        (
            ulong key,
            TEntity entity
        )
        {
            if (registry.Contains(key))
            {
                DespawnView(key, DespawnReason.SpawnRollback);
            }

            var view = factory.Create(entity);
            registry.Register(key, view);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Key에 대응하는 View를 동기 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        private void DespawnView
        (
            ulong key,
            DespawnReason reason
        )
        {
            if (!registry.TryGet(key, out var view))
            {
                return;
            }

            // Factory가 반환을 완료한 뒤 View Registry의 소유권을 해제한다.
            factory.Release(view, reason);
            registry.Unregister(key);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 등록된 모든 View를 동기 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        private void DespawnViewAll(DespawnReason reason)
        {
            var keys = new List<ulong>(registry.Views.Keys);

            foreach (var key in keys)
            {
                DespawnView(key, reason);
            }
        }

    #endregion

    #region ISpawnRegistryBinding 구현

        // ------------------------------------------------------------
        /// <summary>
        /// Entity Spawned 단계를 View 생성으로 동기화한다.
        /// </summary>
        // ------------------------------------------------------------
        void ISpawnRegistryBinding<ulong, TEntity>.OnSpawned
        (
            ulong key,
            TEntity entity
        )
        {
            SpawnView(key, entity);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Entity Despawning 단계를 View 반환으로 동기화한다.
        /// </summary>
        // ------------------------------------------------------------
        void ISpawnRegistryBinding<ulong, TEntity>.OnDespawning
        (
            ulong key,
            TEntity entity,
            DespawnReason reason
        )
        {
            if (boundRegistry == null) return;

            if (!boundRegistry.Spawned.TryGetValue(key, out var current) || !ReferenceEquals(current, entity))
            {
                return;
            }

            DespawnView(key, reason);
        }

    #endregion

    }
}
