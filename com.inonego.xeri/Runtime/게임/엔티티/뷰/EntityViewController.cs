/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : EntityViewController.cs
수정일 : 2026-08-14

# 설명
Entity View Factory와 Registry를 조립해 명시적 또는 Entity 연동 View 수명을 관리한다.
두 진입 방식은 같은 SpawnCore·DespawnCore를 사용하며 Bound 상태에서는 명시적 변경을 막는다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

namespace inonego.Xeri.Game
{
    // ================================================================================
    /// <summary>
    /// <br/> Entity View의 생성·등록·반환 수명을 관리하는 공용 Controller.
    /// <br/> 선택적으로 EntitySpawnRegistry에 연결해 같은 흐름을 자동 동기화한다.
    /// </summary>
    // ================================================================================
    [Serializable]
    public class EntityViewController<TEntityView, TEntity, TContext> :
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

        private readonly IEntityViewFactory<TEntityView, TEntity, TContext> factory = null;
        private readonly EntityViewRegistry<TEntityView, TEntity> registry = null;

        private EntitySpawnRegistryBase<TEntity> boundRegistry = null;
        private Func<ulong, TEntity, TContext> contextResolver = null;

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// Factory와 View Registry를 주입받는다.
        /// </summary>
        // ------------------------------------------------------------
        public EntityViewController
        (
            IEntityViewFactory<TEntityView, TEntity, TContext> factory,
            EntityViewRegistry<TEntityView, TEntity> registry = null
        ) : base()
        {
            this.factory = factory ?? throw new ArgumentNullException(nameof(factory));
            this.registry = registry ?? new EntityViewRegistry<TEntityView, TEntity>();
        }

    #endregion

    #region 명시적 수명

        // ------------------------------------------------------------
        /// <summary>
        /// Unbound 상태에서 Entity View를 명시적으로 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        public TEntityView Spawn
        (
            ulong key,
            TEntity entity,
            in TContext context
        )
        {
            EnsureUnbound();
            return SpawnCore(key, entity, context);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Unbound 상태에서 Key에 대응하는 View를 명시적으로 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public bool Despawn(ulong key, DespawnReason reason)
        {
            EnsureUnbound();
            return DespawnCore(key, reason);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Unbound 상태에서 등록된 모든 View를 명시적으로 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public void DespawnAll(DespawnReason reason)
        {
            EnsureUnbound();
            DespawnAllCore(reason);
        }

    #endregion

    #region 바인딩

        // ------------------------------------------------------------
        /// <summary>
        /// 기본 Context로 Entity Registry에 바인딩한다.
        /// </summary>
        // ------------------------------------------------------------
        public void Bind(EntitySpawnRegistryBase<TEntity> registry)
        {
            Bind(registry, contextResolver: null);
        }

        // --------------------------------------------------------------------------------
        /// <summary>
        /// <br/> Entity Registry에 바인딩하고 현재 Spawned 항목을 동기화한다.
        /// <br/> Context Resolver가 null이면 TContext 기본값을 사용한다.
        /// </summary>
        // --------------------------------------------------------------------------------
        public void Bind
        (
            EntitySpawnRegistryBase<TEntity> registry,
            Func<ulong, TEntity, TContext> contextResolver
        )
        {
            if (registry == null)
            {
                throw new ArgumentNullException(nameof(registry));
            }

            if (boundRegistry != null)
            {
                throw new InvalidOperationException("Entity View Controller가 이미 Entity Registry에 바인딩되어 있습니다.");
            }

            if (this.registry.Count > 0)
            {
                throw new InvalidOperationException("명시적으로 소유 중인 View가 남아 있어 Entity Registry에 바인딩할 수 없습니다.");
            }

            registry.AttachBinding(this);
            boundRegistry = registry;
            this.contextResolver = contextResolver;

            try
            {
                foreach (var (key, entity) in registry.Spawned)
                {
                    var context = ResolveContext(key, entity);
                    SpawnCore(key, entity, context);
                }
            }
            catch (Exception bindException)
            {
                // 초기 동기화가 실패하면 이벤트 연결을 먼저 끊고 이번 Bind가 만든 View를 반환한다.
                registry.DetachBinding(this);
                boundRegistry = null;
                this.contextResolver = null;

                try
                {
                    DespawnAllCore(DespawnReason.RegistryCleanup);
                }
                catch (Exception rollbackException)
                {
                    throw new AggregateException
                    (
                        "Entity View Registry 바인딩과 rollback이 모두 실패했습니다.",
                        bindException,
                        rollbackException
                    );
                }

                throw;
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Entity Registry 바인딩을 해제하고 현재 View를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public void Unbind()
        {
            var registry = boundRegistry;

            if (registry != null)
            {
                registry.DetachBinding(this);
            }

            // 자동 콜백을 먼저 차단한 뒤 남아 있는 View를 terminal 반환한다.
            boundRegistry = null;
            contextResolver = null;
            DespawnAllCore(DespawnReason.RegistryCleanup);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 Entity Registry 상태를 기준으로 View를 다시 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        public void ReSpawnAll()
        {
            var registry = boundRegistry ??
                throw new InvalidOperationException("Entity Registry가 바인딩되어 있지 않습니다.");

            DespawnAllCore(DespawnReason.RegistryCleanup);

            foreach (var (key, entity) in registry.Spawned)
            {
                var context = ResolveContext(key, entity);
                SpawnCore(key, entity, context);
            }
        }

    #endregion

    #region 조회

        // ------------------------------------------------------------
        /// <summary>
        /// Key에 대응하는 View를 조회한다.
        /// </summary>
        // ------------------------------------------------------------
        public bool TryFindView(ulong key, out TEntityView view)
        {
            return registry.TryFind(key, out view);
        }

    #endregion

    #region 공통 수명

        // ------------------------------------------------------------
        /// <summary>
        /// Entity에 대응하는 View를 생성하고 Registry에 등록한다.
        /// </summary>
        // ------------------------------------------------------------
        private TEntityView SpawnCore
        (
            ulong key,
            TEntity entity,
            in TContext context
        )
        {
            if (registry.Contains(key))
            {
                throw new InvalidOperationException($"Entity Key {key}에는 이미 View가 등록되어 있습니다.");
            }

            var view = factory.Create(entity, context);

            try
            {
                registry.Register(key, view);
                return view;
            }
            catch (Exception registerException)
            {
                try
                {
                    factory.Release(view, DespawnReason.SpawnRollback);
                }
                catch (Exception releaseException)
                {
                    throw new AggregateException
                    (
                        "Entity View 등록과 rollback 반환이 모두 실패했습니다.",
                        registerException,
                        releaseException
                    );
                }

                throw;
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Key에 대응하는 View를 terminal 반환하고 Registry 매핑을 제거한다.
        /// </summary>
        // ------------------------------------------------------------
        private bool DespawnCore(ulong key, DespawnReason reason)
        {
            if (!registry.TryFind(key, out var view))
            {
                return false;
            }

            DespawnCore(key, view, reason);
            return true;
        }

        // --------------------------------------------------------------------------------
        /// <summary>
        /// <br/> 지정 View를 terminal 반환하고 필요하면 Registry 매핑을 제거한다.
        /// <br/> 일괄 반환은 원 컬렉션 순회를 위해 항목별 제거를 생략한다.
        /// </summary>
        // --------------------------------------------------------------------------------
        private void DespawnCore
        (
            ulong key,
            TEntityView view,
            DespawnReason reason,
            bool removeFromRegistry = true
        )
        {
            try
            {
                factory.Release(view, reason);
            }
            finally
            {
                if (removeFromRegistry)
                {
                    // terminal 반환 호출 뒤에는 같은 View의 stale 매핑을 남기지 않는다.
                    registry.Unregister(key);
                }
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 등록된 모든 View의 terminal 반환을 끝까지 시도한다.
        /// </summary>
        // ------------------------------------------------------------
        private void DespawnAllCore(DespawnReason reason)
        {
            List<Exception> failures = null;

            foreach (var (key, view) in registry.Views)
            {
                try
                {
                    DespawnCore
                    (
                        key,
                        view,
                        reason,
                        removeFromRegistry: false
                    );
                }
                catch (Exception exception)
                {
                    failures ??= new List<Exception>();
                    failures.Add(exception);
                }
            }

            // 모든 terminal 반환 시도 뒤 Controller 소유 매핑을 한 번에 종료한다.
            registry.Clear();

            if (failures == null) return;

            throw failures.Count == 1
                ? failures[0]
                : new AggregateException("일부 Entity View 반환에 실패했습니다.", failures);
        }

    #endregion

    #region 상태 검증

        // ------------------------------------------------------------
        /// <summary>
        /// 명시적 수명 API를 사용할 수 있는 Unbound 상태인지 확인한다.
        /// </summary>
        // ------------------------------------------------------------
        private void EnsureUnbound()
        {
            if (boundRegistry != null)
            {
                throw new InvalidOperationException("Entity Registry에 바인딩된 동안 명시적 View 수명을 변경할 수 없습니다.");
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 Resolver 또는 기본값으로 Entity View 생성 Context를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        private TContext ResolveContext(ulong key, TEntity entity)
        {
            return contextResolver != null
                ? contextResolver.Invoke(key, entity)
                : default;
        }

    #endregion

    #region ISpawnRegistryBinding 구현

        // ------------------------------------------------------------
        /// <summary>
        /// Entity Spawned 단계를 View 생성으로 동기화한다.
        /// </summary>
        // ------------------------------------------------------------
        void ISpawnRegistryBinding<ulong, TEntity>.OnSpawned(ulong key, TEntity entity)
        {
            var context = ResolveContext(key, entity);
            SpawnCore(key, entity, context);
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

            if
            (
                !boundRegistry.Spawned.TryGetValue(key, out var current) ||
                !ReferenceEquals(current, entity)
            )
            {
                return;
            }

            DespawnCore(key, reason);
        }

    #endregion

    }

    // ================================================================================
    /// <summary>
    /// 생성 Context가 필요 없는 Entity View 수명을 관리하는 Controller.
    /// </summary>
    // ================================================================================
    [Serializable]
    public sealed class EntityViewController<TEntityView, TEntity> :
        EntityViewController<TEntityView, TEntity, EntityViewNoContext>
    where TEntityView : EntityViewBase<TEntity>
    where TEntity : class, IEntity
    {

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// Context 없는 Factory와 View Registry를 주입받는다.
        /// </summary>
        // ------------------------------------------------------------
        public EntityViewController
        (
            IEntityViewFactory<TEntityView, TEntity, EntityViewNoContext> factory,
            EntityViewRegistry<TEntityView, TEntity> registry = null
        ) : base(factory, registry)
        {
            // NONE
        }

    #endregion

    #region 명시적 수명

        // ------------------------------------------------------------
        /// <summary>
        /// Unbound 상태에서 Entity View를 명시적으로 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        public TEntityView Spawn(ulong key, TEntity entity)
        {
            var context = default(EntityViewNoContext);
            return base.Spawn(key, entity, context);
        }

    #endregion

    }
}
