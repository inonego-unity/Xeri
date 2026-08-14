/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : EntityViewFactory.cs
수정일 : 2026-08-14

# 설명
Entity View GameObject의 획득, Context 기반 준비, Entity 연결과 terminal 반환을 담당한다.
요청별 Provider를 선택하고 획득 View와 source Provider 관계를 반환 시도까지 유지한다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

namespace inonego.Xeri.Game
{
    // ================================================================================
    /// <summary>
    /// <br/> Entity와 생성 Context에 대응하는 Unity View를 준비하고 반환한다.
    /// <br/> concrete Factory는 요청별 GameObject Provider와 준비 정책만 정의한다.
    /// </summary>
    // ================================================================================
    [Serializable]
    public abstract class EntityViewFactory<TEntityView, TEntity, TContext> :
        IEntityViewFactory<TEntityView, TEntity, TContext>
    where TEntityView : EntityViewBase<TEntity>
    where TEntity : class, IEntity
    {

    #region 필드

        private readonly Dictionary<TEntityView, IGameObjectProvider> sourceProviders = new
        (
            ReferenceEqualityComparer<TEntityView>.Instance
        );

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// 파생 Factory 초기화를 위한 기본 생성자.
        /// </summary>
        // ------------------------------------------------------------
        protected EntityViewFactory() : base()
        {
            // NONE
        }

    #endregion

    #region 공개 생성과 반환

        // ------------------------------------------------------------
        /// <summary>
        /// Entity와 생성 Context에 대응하는 View를 동기 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        public virtual TEntityView Create(TEntity entity, in TContext context)
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            var provider = ResolveProvider(entity, context) ??
                throw new InvalidOperationException("Entity View GameObject Provider를 해석할 수 없습니다.");
            var view = AcquireView(provider);

            try
            {
                // 반환 출처를 먼저 고정해 이후 모든 부분 생성 상태를 같은 Provider로 정리한다.
                sourceProviders.Add(view, provider);

                // Provider가 공급한 활성 상태는 유지하고 Entity 연결에 필요한 표현만 준비한다.
                PrepareView(view, entity, context);
                view.BindEntity(entity);

                // Entity 연결을 마친 View의 논리적 Spawn 단계를 순서대로 알린다.
                view.OnSpawning();
                view.OnSpawned();
                return view;
            }
            catch (Exception spawnException)
            {
                var failures = CleanupFailedCreate(view, entity, provider);

                if (failures == null)
                {
                    throw;
                }

                failures.Insert(0, spawnException);
                throw new AggregateException("Entity View 생성과 rollback 중 오류가 발생했습니다.", failures);
            }
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> Entity View를 지정 사유로 terminal 반환한다.
        /// <br/> 정리와 Provider 반환을 모두 시도하고 source routing을 종료한다.
        /// </summary>
        // ----------------------------------------------------------------------
        public virtual void Release(TEntityView view, DespawnReason reason)
        {
            if (view == null) return;

            if (!sourceProviders.TryGetValue(view, out var provider))
            {
                throw new InvalidOperationException("현재 Factory가 획득한 Entity View가 아닙니다.");
            }

            List<Exception> failures = null;

            try
            {
                if (view.RequiresSpawnCleanup)
                {
                    CleanupSpawnView(view, reason);
                }
            }
            catch (Exception exception)
            {
                failures = new List<Exception> { exception };
            }

            try
            {
                // Provider 호출은 terminal 반환 경계이므로 앞선 정리 실패와 무관하게 한 번 수행한다.
                provider.Release(view.gameObject, worldPositionStays: false);
            }
            catch (Exception exception)
            {
                failures ??= new List<Exception>();
                failures.Add(exception);
            }
            finally
            {
                sourceProviders.Remove(view);
            }

            if (failures == null) return;

            throw failures.Count == 1
                ? failures[0]
                : new AggregateException("Entity View 정리와 Provider 반환 중 오류가 발생했습니다.", failures);
        }

    #endregion

    #region 준비

        // ------------------------------------------------------------
        /// <summary>
        /// GameObject와 View 컴포넌트를 동기 획득한다.
        /// </summary>
        // ------------------------------------------------------------
        private static TEntityView AcquireView(IGameObjectProvider provider)
        {
            var gameObject = provider.Acquire(worldPositionStays: false);

            if (gameObject == null)
            {
                throw new InvalidOperationException("Entity View GameObject를 가져올 수 없습니다.");
            }

            if (gameObject.TryGetComponent(out TEntityView view))
            {
                return view;
            }

            var componentName = typeof(TEntityView).Name;
            var gameObjectName = gameObject.name;

            try
            {
                provider.Release(gameObject, worldPositionStays: false);
            }
            catch (Exception releaseException)
            {
                var componentException = new InvalidOperationException
                (
                    $"게임 오브젝트 '{gameObjectName}'에서 Entity View 컴포넌트({componentName})를 찾을 수 없습니다."
                );
                throw new AggregateException
                (
                    "Entity View 컴포넌트 확인과 Provider 반환이 모두 실패했습니다.",
                    componentException,
                    releaseException
                );
            }

            throw new InvalidOperationException
            (
                $"게임 오브젝트 '{gameObjectName}'에서 Entity View 컴포넌트({componentName})를 찾을 수 없습니다."
            );
        }

    #endregion

    #region 반환

        // --------------------------------------------------------------------------------
        /// <summary>
        /// <br/> 생성 중 실패한 View의 부분 상태와 Provider 반환을 끝까지 처리한다.
        /// <br/> 반환된 목록에는 Spawn 원인을 제외한 rollback 오류만 담긴다.
        /// </summary>
        // --------------------------------------------------------------------------------
        private List<Exception> CleanupFailedCreate
        (
            TEntityView view,
            TEntity entity,
            IGameObjectProvider provider
        )
        {
            List<Exception> failures = null;

            try
            {
                // OnSpawning 진입 여부에 맞는 훅으로 부분 생성 상태를 정리한다.
                if (view.RequiresSpawnCleanup)
                {
                    CleanupSpawnView(view, DespawnReason.SpawnRollback);
                }
                else
                {
                    try
                    {
                        CleanupView(view, entity, DespawnReason.SpawnRollback);
                    }
                    catch (Exception exception)
                    {
                        failures = new List<Exception> { exception };
                    }

                    try
                    {
                        view.UnbindEntity();
                    }
                    catch (Exception exception)
                    {
                        failures ??= new List<Exception>();
                        failures.Add(exception);
                    }
                }
            }
            catch (Exception exception)
            {
                failures = new List<Exception> { exception };
            }

            try
            {
                // 생성 실패의 Provider 반환도 terminal 경계로 한 번 수행한다.
                provider.Release(view.gameObject, worldPositionStays: false);
            }
            catch (Exception exception)
            {
                failures ??= new List<Exception>();
                failures.Add(exception);
            }
            finally
            {
                sourceProviders.Remove(view);
            }

            return failures;
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// 스폰 처리를 시작한 View와 종속 표현을 순서대로 정리한다.
        /// </summary>
        // ----------------------------------------------------------------------
        private void CleanupSpawnView(TEntityView view, DespawnReason reason)
        {
            var entity = view.Entity;
            List<Exception> failures = null;

            try
            {
                view.OnDespawning(reason);
            }
            catch (Exception exception)
            {
                failures = new List<Exception> { exception };
            }

            try
            {
                CleanupView(view, entity, reason);
            }
            catch (Exception exception)
            {
                failures ??= new List<Exception>();
                failures.Add(exception);
            }

            try
            {
                view.OnDespawned(reason);
            }
            catch (Exception exception)
            {
                failures ??= new List<Exception>();
                failures.Add(exception);
            }

            if (failures == null) return;

            throw failures.Count == 1
                ? failures[0]
                : new AggregateException("Entity View 수명 정리 중 오류가 발생했습니다.", failures);
        }

    #endregion

    #region 확장 훅

        // ----------------------------------------------------------------------
        /// <summary>
        /// 요청에 사용할 GameObject Provider를 반환한다.
        /// </summary>
        // ----------------------------------------------------------------------
        protected abstract IGameObjectProvider ResolveProvider
        (
            TEntity entity,
            in TContext context
        );

        // --------------------------------------------------------------------------------
        /// <summary>
        /// <br/> Factory가 Entity를 연결하기 전에 필수 종속 표현을 동기 준비한다.
        /// <br/> 분리된 VisualRoot가 필요하면 이 단계에서 연결한다.
        /// </summary>
        // --------------------------------------------------------------------------------
        protected virtual void PrepareView
        (
            TEntityView view,
            TEntity entity,
            in TContext context
        )
        {
            // NONE
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// Provider 반환 전에 Factory가 공급한 종속 표현을 정리한다.
        /// </summary>
        // ----------------------------------------------------------------------
        protected virtual void CleanupView
        (
            TEntityView view,
            TEntity entity,
            DespawnReason reason
        )
        {
            // NONE
        }

    #endregion

    }

    // ================================================================================
    /// <summary>
    /// 생성 Context 없이 하나의 GameObject Provider를 사용하는 Entity View Factory.
    /// </summary>
    // ================================================================================
    [Serializable]
    public class EntityViewFactory<TEntityView, TEntity> :
        EntityViewFactory<TEntityView, TEntity, EntityViewNoContext>
    where TEntityView : EntityViewBase<TEntity>
    where TEntity : class, IEntity
    {

    #region 필드

        private readonly IGameObjectProvider provider = null;

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// GameObject Provider를 주입받는다.
        /// </summary>
        // ------------------------------------------------------------
        public EntityViewFactory(IGameObjectProvider provider) : base()
        {
            this.provider = provider ?? throw new ArgumentNullException(nameof(provider));
        }

    #endregion

    #region 공개 생성과 반환

        // ------------------------------------------------------------
        /// <summary>
        /// Entity에 대응하는 View를 동기 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        public virtual TEntityView Create(TEntity entity)
        {
            var context = default(EntityViewNoContext);
            return base.Create(entity, context);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Entity View를 일반 제거 사유로 terminal 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public virtual void Release(TEntityView view)
        {
            base.Release(view, DespawnReason.Removed);
        }

    #endregion

    #region 확장 훅

        // ------------------------------------------------------------
        /// <summary>
        /// 생성자에서 고정한 GameObject Provider를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        protected sealed override IGameObjectProvider ResolveProvider
        (
            TEntity entity,
            in EntityViewNoContext context
        )
        {
            return provider;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Context 없는 기존 View 준비 훅을 호출한다.
        /// </summary>
        // ------------------------------------------------------------
        protected sealed override void PrepareView
        (
            TEntityView view,
            TEntity entity,
            in EntityViewNoContext context
        )
        {
            PrepareView(view, entity);
        }

        // --------------------------------------------------------------------------------
        /// <summary>
        /// <br/> Factory가 Entity를 연결하기 전에 필수 종속 표현을 동기 준비한다.
        /// <br/> 분리된 VisualRoot가 필요하면 이 단계에서 연결한다.
        /// </summary>
        // --------------------------------------------------------------------------------
        protected virtual void PrepareView(TEntityView view, TEntity entity)
        {
            // NONE
        }

    #endregion

    }
}
