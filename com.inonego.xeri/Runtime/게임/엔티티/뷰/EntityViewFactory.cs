/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : EntityViewFactory.cs
수정일 : 2026-07-29

# 설명
Entity View GameObject의 획득, 종속 표현 준비, Entity 연결, Spawn 훅과 최종 반환을 담당한다.
획득한 View는 Provider 반환이 완료될 때까지 호출자가 소유한다.
========================================================================= BLOCK_HEADER_END */

using System;

namespace inonego.Xeri.Game
{
    // ============================================================
    /// <summary>
    /// Entity에 대응하는 Unity View를 준비하고 회수한다.
    /// </summary>
    // ============================================================
    [Serializable]
    public class EntityViewFactory<TEntityView, TEntity>
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
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            var view = AcquireView();

            try
            {
                // Provider가 공급한 활성 상태는 유지하고 Entity 연결에 필요한 표현만 준비한다.
                PrepareView(view, entity);
                view.BindEntity(entity);

                // Entity 연결을 마친 View의 논리적 Spawn 단계를 순서대로 알린다.
                view.OnSpawning();
                view.OnSpawned();
                return view;
            }
            catch
            {
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
                        finally
                        {
                            view.UnbindEntity();
                        }
                    }
                }
                finally
                {
                    // 생성에 실패한 View는 Binder 소유가 아니므로 이 호출에서 Provider에 반환한다.
                    provider.Release(view.gameObject, worldPositionStays: false);
                }

                throw;
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Entity View를 일반 제거 사유로 회수한다.
        /// </summary>
        // ------------------------------------------------------------
        public virtual void Release(TEntityView view)
        {
            Release(view, DespawnReason.Removed);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Entity View를 지정한 제거 사유로 회수한다.
        /// </summary>
        // ------------------------------------------------------------
        internal void Release
        (
            TEntityView view,
            DespawnReason reason
        )
        {
            if (view == null) return;

            if (view.RequiresSpawnCleanup)
            {
                CleanupSpawnView(view, reason);
            }

            // 정리가 이미 끝난 View는 Provider 반환만 다시 시도할 수 있다.
            provider.Release(view.gameObject, worldPositionStays: false);
        }

    #endregion

    #region 준비

        // ------------------------------------------------------------
        /// <summary>
        /// GameObject와 View 컴포넌트를 동기 획득한다.
        /// </summary>
        // ------------------------------------------------------------
        private TEntityView AcquireView()
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

            provider.Release(gameObject, worldPositionStays: false);

            throw new InvalidOperationException
            (
                $"게임 오브젝트 '{gameObjectName}'에서 Entity View 컴포넌트({componentName})를 찾을 수 없습니다."
            );
        }

    #endregion

    #region 반환

        // ----------------------------------------------------------------------
        /// <summary>
        /// 스폰 처리를 시작한 View와 종속 표현을 순서대로 정리한다.
        /// </summary>
        // ----------------------------------------------------------------------
        private void CleanupSpawnView(TEntityView view, DespawnReason reason)
        {
            var entity = view.Entity;

            try
            {
                view.OnDespawning(reason);
            }
            finally
            {
                try
                {
                    CleanupView(view, entity, reason);
                }
                finally
                {
                    view.OnDespawned(reason);
                }
            }
        }

    #endregion

    #region 확장 훅

        // --------------------------------------------------------------------------------
        /// <summary>
        /// <br/> Factory가 Entity를 연결하기 전에 필수 종속 표현을 동기 준비한다.
        /// <br/> 분리된 VisualRoot가 필요하면 이 단계에서 연결한다.
        /// </summary>
        // --------------------------------------------------------------------------------
        protected virtual void PrepareView(TEntityView view, TEntity entity) {}

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
        ) {}

    #endregion

    }
}
