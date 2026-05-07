/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : MonoEntity.cs
수정일 : 2026-05-07

# 설명
엔티티(Entity)와 MonoBehaviour 를 연결하는 어댑터 베이스 클래스.
키·스폰 상태를 엔티티에 위임하고 시각 표현 분리용 visualGO 슬롯을 제공한다.
INeedToInit<TEntity> 로 콘크리트 엔티티를 주입받는다.
========================================================================= BLOCK_HEADER_END */

using System;

using UnityEngine;

namespace inonego.Xeri.Game
{
    // ============================================================
    /// <summary>
    /// 엔티티와 MonoBehaviour 를 묶는 어댑터 베이스 클래스.
    /// </summary>
    // ============================================================
    [Serializable]
    public abstract class MonoEntity<TEntity> : MonoBehaviour, IMonoEntity, INeedToInit<TEntity>
    where TEntity : class, IEntity
    {

    #region 키 설정

        // ------------------------------------------------------------
        /// <summary>
        /// 엔티티의 키. 미설정 시 접근하면 예외.
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
        /// 키 설정 여부.
        /// </summary>
        // ------------------------------------------------------------
        public bool HasKey => entity != null && entity.HasKey;

    #endregion

    #region 필드

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 스폰되어 있는지 여부.
        /// </summary>
        // ------------------------------------------------------------
        public bool IsSpawned => isSpawned;

        [SerializeField, ReadOnly]
        protected bool isSpawned = false;

        // ------------------------------------------------------------
        /// <summary>
        /// 대응되는 논리 엔티티(콘크리트 타입 노출).
        /// </summary>
        // ------------------------------------------------------------
        public TEntity Entity => entity;

        [SerializeReference, HideInInspector]
        private TEntity entity = null;

    #endregion

    #region 비주얼 게임 오브젝트

        // ------------------------------------------------------------
        /// <summary>
        /// 시각 표현용으로 분리된 자식 GameObject.
        /// </summary>
        // ------------------------------------------------------------
        public GameObject VisualGO => visualGO;

        [Header("비주얼 게임 오브젝트")]
        [SerializeField]
        private GameObject visualGO = null;

        // ------------------------------------------------------------
        /// <summary>
        /// 시각적 표현을 부모에서 분리한다.
        /// </summary>
        // ------------------------------------------------------------
        public void DetachVisualGO()
        {
            if (visualGO != null)
            {
                visualGO.transform.SetParent(null);

                visualGO = null;
            }
        }

    #endregion

    #region 인터페이스 구현

        IReadOnlyEntity IReadOnlyMonoEntity.Entity => entity;
        IEntity         IMonoEntity        .Entity => entity;

        bool ISpawnRegistryObject<ulong>.IsSpawned
        {
            get => isSpawned;
            set => isSpawned = value;
        }

        Action IDespawnable.DespawnFromRegistry { get; set; }

        // ------------------------------------------------------------
        /// <summary>
        /// 스폰 직전 훅. 자식이 override 가능.
        /// </summary>
        // ------------------------------------------------------------
        public virtual void OnBeforeSpawn() {}

        // ------------------------------------------------------------
        /// <summary>
        /// 엔티티를 주입받아 보관한다.
        /// </summary>
        // ------------------------------------------------------------
        public virtual void Init(TEntity entity)
        {
            if (entity == null)
            {
                throw new ArgumentNullException("엔티티가 null입니다.");
            }

            this.entity = entity;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 스폰 직후 훅. 자식이 override 가능.
        /// </summary>
        // ------------------------------------------------------------
        public virtual void OnAfterSpawn() {}

        // ------------------------------------------------------------
        /// <summary>
        /// 디스폰 직전 훅. 자식이 override 가능.
        /// </summary>
        // ------------------------------------------------------------
        public virtual void OnBeforeDespawn() {}

        // ------------------------------------------------------------
        /// <summary>
        /// 디스폰 직후 엔티티 참조를 끊는다. 자식 hook 은 _OnAfterDespawn.
        /// </summary>
        // ------------------------------------------------------------
        void IDespawnable.OnAfterDespawn()
        {
            _OnAfterDespawn();

            entity = null;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 디스폰 직후 자식 hook.
        /// </summary>
        // ------------------------------------------------------------
        protected virtual void _OnAfterDespawn() {}

        // ------------------------------------------------------------
        /// <summary>
        /// 엔티티 해제 훅. 자식이 override 가능.
        /// </summary>
        // ------------------------------------------------------------
        public virtual void Release() {}

    #endregion

    }
}
