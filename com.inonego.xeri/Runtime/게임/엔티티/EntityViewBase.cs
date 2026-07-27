/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : EntityViewBase.cs
수정일 : 2026-07-29

# 설명
Entity 모델을 Unity GameObject로 표현하는 View 베이스 클래스.
Entity 참조, Key 위임, View 상태 전환 훅을 제공한다.
========================================================================= BLOCK_HEADER_END */

using System;

using UnityEngine;

namespace inonego.Xeri.Game
{
    // --------------------------------------------------------------------------------
    /// <summary>
    /// <br/> Entity 모델을 Unity View로 표현하기 위한 베이스 클래스.
    /// <br/> Awake와 OnEnable은 Entity 연결을 전제로 하지 않는다.
    /// <br/> Entity 의존 초기화와 정리는 전용 View 훅에서 수행한다.
    /// </summary>
    // --------------------------------------------------------------------------------
    [Serializable]
    public abstract class EntityViewBase<TEntity> : MonoBehaviour, IEntityView
    where TEntity : class, IEntity
    {

    #region 키 설정

        // ------------------------------------------------------------
        /// <summary>
        /// Entity의 키.
        /// </summary>
        // ------------------------------------------------------------
        public ulong Key
        {
            get
            {
                if (entity != null && entity.HasKey)
                {
                    return entity.Key;
                }

                throw new InvalidOperationException("키가 설정되어 있지 않습니다.");
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Entity 키 설정 여부.
        /// </summary>
        // ------------------------------------------------------------
        public bool HasKey => entity != null && entity.HasKey;

    #endregion

    #region 필드

        // ------------------------------------------------------------
        /// <summary>
        /// 대응되는 논리 Entity.
        /// </summary>
        // ------------------------------------------------------------
        public TEntity Entity => entity;

        [NonSerialized]
        private TEntity entity = null;

        // ------------------------------------------------------------
        /// <summary>
        /// View가 스폰 정리를 필요로 하는지 여부.
        /// </summary>
        // ------------------------------------------------------------
        internal bool RequiresSpawnCleanup => requiresSpawnCleanup;

        [NonSerialized]
        private bool requiresSpawnCleanup = false;

    #endregion

    #region 인터페이스 구현

        // ------------------------------------------------------------
        /// <summary>
        /// 외부 읽기 전용 계약으로 연결된 Entity를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        IReadOnlyEntity IReadOnlyEntityView.Entity => entity;

        // ------------------------------------------------------------
        /// <summary>
        /// 내부 View 계약으로 연결된 Entity를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        IEntity IEntityView.Entity => entity;

    #endregion

    #region Entity 연결

        // ------------------------------------------------------------
        /// <summary>
        /// Entity를 View에 연결한다.
        /// </summary>
        // ------------------------------------------------------------
        internal void BindEntity(TEntity entity)
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            if (this.entity != null)
            {
                throw new InvalidOperationException
                (
                    "이미 Entity가 연결된 View에는 새 Entity를 연결할 수 없습니다."
                );
            }

            this.entity = entity;

            // 연결 훅 실패 시 Factory 정리 경로가 같은 Entity를 해제할 수 있도록 참조를 유지한다.
            OnBindEntity(entity);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Entity 연결을 해제한다.
        /// </summary>
        // ------------------------------------------------------------
        internal void UnbindEntity()
        {
            var previous = entity;

            if (previous != null)
            {
                // 연결 해제 훅이 완료된 뒤에만 참조를 비워 실패한 정리 상태를 보존합니다.
                OnUnbindEntity(previous);
                entity = null;
            }
        }

    #endregion

    #region 상태 전환

        // ------------------------------------------------------------------------------------------
        /// <summary>
        /// <br/> View의 Spawning 진입을 알리는 훅.
        /// <br/> 이 호출에 진입한 뒤 취소·실패하면 OnSpawned 호출 여부와 관계없이 디스폰 훅과 연결 해제를 수행한다.
        /// </summary>
        // ------------------------------------------------------------------------------------------
        internal void OnSpawning()
        {
            requiresSpawnCleanup = true;

            if (entity != null)
            {
                OnSpawningView(entity);
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// View 스폰 완료 훅.
        /// </summary>
        // ------------------------------------------------------------
        internal void OnSpawned()
        {
            if (entity != null)
            {
                OnSpawnedView(entity);
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// View 디스폰 직전 훅.
        /// </summary>
        // ------------------------------------------------------------
        internal void OnDespawning(DespawnReason reason)
        {
            if (entity != null)
            {
                OnDespawningView(entity, reason);
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// View 디스폰 완료 훅.
        /// </summary>
        // ------------------------------------------------------------
        internal void OnDespawned(DespawnReason reason)
        {
            var previous = entity;

            try
            {
                try
                {
                    if (previous != null)
                    {
                        OnDespawnedView(previous, reason);
                    }
                }
                catch (Exception despawnException)
                {
                    try
                    {
                        UnbindEntity();
                    }
                    catch (Exception unbindException)
                    {
                        throw new AggregateException
                        (
                            "Entity View 디스폰 완료 훅과 연결 해제가 모두 실패했습니다.",
                            despawnException,
                            unbindException
                        );
                    }

                    throw;
                }

                UnbindEntity();
            }
            finally
            {
                // 연결 해제까지 끝난 View만 새 Spawning 단계에 진입할 수 있다.
                if (entity == null)
                {
                    requiresSpawnCleanup = false;
                }
            }
        }

    #endregion

    #region 확장 훅

        // ------------------------------------------------------------
        /// <summary>
        /// Entity 참조가 설정된 뒤 Entity 의존 초기화를 수행한다.
        /// </summary>
        // ------------------------------------------------------------
        protected virtual void OnBindEntity(TEntity entity) {}

        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// <br/> View의 Spawning 단계에 진입할 때 호출된다.
        /// <br/> 부분 초기화 중 실패할 수 있으므로 OnSpawnedView가 호출되지 않아도 이후 디스폰 훅에서 정리 가능해야 한다.
        /// </summary>
        // ----------------------------------------------------------------------------------------------------
        protected virtual void OnSpawningView(TEntity entity) {}

        // ------------------------------------------------------------
        /// <summary>
        /// View 스폰 완료 후 호출된다.
        /// </summary>
        // ------------------------------------------------------------
        protected virtual void OnSpawnedView(TEntity entity) {}

        // ------------------------------------------------------------------------------------------
        /// <summary>
        /// <br/> View 디스폰 직전 호출된다.
        /// <br/> OnSpawningView에 진입했지만 OnSpawnedView가 완료되지 않은 실패 롤백에서도 호출될 수 있다.
        /// </summary>
        // ------------------------------------------------------------------------------------------
        protected virtual void OnDespawningView(TEntity entity, DespawnReason reason) {}

        // --------------------------------------------------------------------------------
        /// <summary>
        /// <br/> View 디스폰 완료 후 호출된다.
        /// <br/> OnSpawningView의 부분 초기화만 존재하는 실패 롤백에서도 안전하게 정리해야 한다.
        /// </summary>
        // --------------------------------------------------------------------------------
        protected virtual void OnDespawnedView(TEntity entity, DespawnReason reason) {}

        // ------------------------------------------------------------
        /// <summary>
        /// Entity 참조를 비우기 전에 연결 해제 처리를 수행한다.
        /// </summary>
        // ------------------------------------------------------------
        protected virtual void OnUnbindEntity(TEntity previousEntity) {}

    #endregion

    }
}
