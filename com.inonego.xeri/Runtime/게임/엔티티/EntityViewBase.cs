/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : EntityViewBase.cs
수정일 : 2026-05-28

# 설명
Entity 모델을 Unity GameObject로 표현하는 view 베이스 클래스.
Entity 참조, key 위임, view 생명주기 hook을 제공한다.
========================================================================= BLOCK_HEADER_END */

using System;

using UnityEngine;

namespace inonego.Xeri.Game
{
    // ============================================================
    /// <summary>
    /// Entity 모델을 Unity view로 표현하기 위한 베이스 클래스.
    /// </summary>
    // ============================================================
    [Serializable]
    public abstract class EntityViewBase<TEntity> : MonoBehaviour, IEntityView, INeedToInit<TEntity>
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
        /// 현재 view가 스폰되어 있는지 여부.
        /// </summary>
        // ------------------------------------------------------------
        public bool IsSpawned => isSpawned;

        [SerializeField, ReadOnly]
        private bool isSpawned = false;

        // ------------------------------------------------------------
        /// <summary>
        /// 대응되는 논리 Entity.
        /// </summary>
        // ------------------------------------------------------------
        public TEntity Entity => entity;

        [SerializeReference, HideInInspector]
        private TEntity entity = null;

    #endregion

    #region 인터페이스 구현

        IReadOnlyEntity IReadOnlyEntityView.Entity => entity;
        IEntity         IEntityView        .Entity => entity;

        bool ISpawnRegistryObject<ulong>.IsSpawned
        {
            get => isSpawned;
            set => isSpawned = value;
        }

        Action IDespawnable.DespawnFromRegistry { get; set; }

    #endregion

    #region 초기화

        // ------------------------------------------------------------
        /// <summary>
        /// Entity를 view에 연결한다.
        /// </summary>
        // ------------------------------------------------------------
        public virtual void Init(TEntity entity)
        {
            if (entity == null)
            {
                throw new ArgumentNullException("엔티티가 null입니다.");
            }

            if (this.entity != null)
            {
                var previous = this.entity;

                OnClearEntity(previous);
            }

            this.entity = entity;

            OnAssignEntity(entity);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Entity 연결을 해제한다.
        /// </summary>
        // ------------------------------------------------------------
        public virtual void Release()
        {
            if (isSpawned)
            {
                OnPreDespawn();
                OnDespawn();

                return;
            }

            var previous = entity;
            entity = null;

            if (previous != null)
            {
                OnClearEntity(previous);
            }
        }

    #endregion

    #region 생명주기

        // ------------------------------------------------------------
        /// <summary>
        /// view 스폰 직전 hook.
        /// </summary>
        // ------------------------------------------------------------
        public virtual void OnPreSpawn()
        {
            if (entity != null)
            {
                OnPreSpawnView(entity);
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// view 스폰 완료 hook.
        /// </summary>
        // ------------------------------------------------------------
        public virtual void OnSpawn()
        {
            isSpawned = true;

            if (entity != null)
            {
                OnSpawnView(entity);
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// view 디스폰 직전 hook.
        /// </summary>
        // ------------------------------------------------------------
        public virtual void OnPreDespawn()
        {
            if (entity != null)
            {
                OnPreDespawnView(entity);
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// view 디스폰 완료 hook.
        /// </summary>
        // ------------------------------------------------------------
        public virtual void OnDespawn()
        {
            var previous = entity;

            if (previous != null)
            {
                OnDespawnView(previous);
            }

            isSpawned = false;
            entity    = null;

            if (previous != null)
            {
                OnClearEntity(previous);
            }
        }

    #endregion

    #region 확장 Hook

        // ------------------------------------------------------------
        /// <summary>
        /// Entity가 view에 연결된 직후 호출된다.
        /// </summary>
        // ------------------------------------------------------------
        protected virtual void OnAssignEntity(TEntity entity) {}

        // ------------------------------------------------------------
        /// <summary>
        /// view 스폰 직전 호출된다.
        /// </summary>
        // ------------------------------------------------------------
        protected virtual void OnPreSpawnView(TEntity entity) {}

        // ------------------------------------------------------------
        /// <summary>
        /// view 스폰 완료 후 호출된다.
        /// </summary>
        // ------------------------------------------------------------
        protected virtual void OnSpawnView(TEntity entity) {}

        // ------------------------------------------------------------
        /// <summary>
        /// view 디스폰 직전 호출된다.
        /// </summary>
        // ------------------------------------------------------------
        protected virtual void OnPreDespawnView(TEntity entity) {}

        // ------------------------------------------------------------
        /// <summary>
        /// view 디스폰 완료 후 호출된다.
        /// </summary>
        // ------------------------------------------------------------
        protected virtual void OnDespawnView(TEntity entity) {}

        // ------------------------------------------------------------
        /// <summary>
        /// Entity 연결이 해제된 직후 호출된다.
        /// </summary>
        // ------------------------------------------------------------
        protected virtual void OnClearEntity(TEntity previousEntity) {}

    #endregion

    }
}
