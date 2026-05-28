/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : EntitySpawnRegistry.cs
수정일 : 2026-05-28

# 설명
엔티티 전용 스폰 레지스트리 베이스·구현체.

- EntitySpawnRegistryBase<TEntity>     : 키 자동 생성·해제 + IDeepCloneableFrom 지원
- EntitySpawnRegistry<TEntity>         : 파라미터 없는 Spawn
- EntitySpawnRegistry<TEntity, TParam> : 파라미터 받는 Spawn (INeedToInit<TParam>)

CloneFrom 시 KeyGenerator 는 참조 공유한다 — 깊은 복제된 두 레지스트리가 동일 카운터를 공유해 키 충돌을 방지한다.
========================================================================= BLOCK_HEADER_END */

using System;

using UnityEngine;

namespace inonego.Xeri.Game
{
    // ============================================================
    /// <summary>
    /// 엔티티 스폰 레지스트리 베이스. 키 자동 생성·해제와 깊은 복제를 담당한다.
    /// </summary>
    // ============================================================
    [Serializable]
    public abstract class EntitySpawnRegistryBase<TEntity>
        : SpawnRegistryBase<ulong, TEntity>,
          IDeepCloneableFrom<EntitySpawnRegistryBase<TEntity>>
    where TEntity : class, IEntity
    {

    #region 필드

        // ------------------------------------------------------------
        /// <summary>
        /// 키 생성기. 기본은 IncreKeyGenerator.
        /// </summary>
        // ------------------------------------------------------------
        public IKeyGenerator<ulong> KeyGenerator => keyGenerator;

        [SerializeReference]
        protected IKeyGenerator<ulong> keyGenerator = new IncreKeyGenerator();

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// 기본 생성자. IncreKeyGenerator 를 사용한다.
        /// </summary>
        // ------------------------------------------------------------
        public EntitySpawnRegistryBase() : base() {}

        // ------------------------------------------------------------
        /// <summary>
        /// 외부 키 생성기를 주입받는 생성자.
        /// </summary>
        // ------------------------------------------------------------
        public EntitySpawnRegistryBase(IKeyGenerator<ulong> keyGenerator) : this()
        {
            if (keyGenerator == null)
            {
                throw new ArgumentNullException("KeyGenerator 가 null입니다.");
            }

            this.keyGenerator = keyGenerator;
        }

    #endregion

    #region 자식 훅

        // ------------------------------------------------------------
        /// <summary>
        /// 스폰 직전 키를 자동 생성해 엔티티에 설정한다. 자식 hook 은 OnPreSpawnEntity.
        /// </summary>
        // ------------------------------------------------------------
        protected sealed override void OnPreSpawnObject(TEntity spawnable)
        {
            var key = keyGenerator.Generate();

            spawnable.SetKey(key);

            OnPreSpawnEntity(spawnable);

            base.OnPreSpawnObject(spawnable);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 스폰 직전 자식 hook.
        /// </summary>
        // ------------------------------------------------------------
        protected virtual void OnPreSpawnEntity(TEntity spawnable) {}

        // ------------------------------------------------------------
        /// <summary>
        /// 디스폰 직후 엔티티의 키를 초기화한다. 자식 hook 은 OnDespawnEntity.
        /// </summary>
        // ------------------------------------------------------------
        protected sealed override void OnDespawnObject(TEntity despawnable)
        {
            base.OnDespawnObject(despawnable);

            OnDespawnEntity(despawnable);

            despawnable.ClearKey();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 디스폰 직후 자식 hook.
        /// </summary>
        // ------------------------------------------------------------
        protected virtual void OnDespawnEntity(TEntity despawnable) {}

    #endregion

    #region 깊은 복사

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> source 의 키 생성기·스폰된 엔티티들을 this 로 복사한다.
        /// <br/> 키 생성기는 참조 공유로 두 레지스트리 간 키 충돌을 방지한다.
        /// </summary>
        // ----------------------------------------------------------------------
        public void CloneFrom(EntitySpawnRegistryBase<TEntity> source)
        {
            if (source == null)
            {
                throw new ArgumentNullException("EntitySpawnRegistryBase<TEntity>.CloneFrom()의 인자가 null입니다.");
            }

            keyGenerator = source.keyGenerator;

            base.CloneFrom(source);
        }

    #endregion

    }

    // ============================================================
    /// <summary>
    /// 파라미터 없는 Acquire/Spawn 을 제공하는 엔티티 스폰 레지스트리.
    /// </summary>
    // ============================================================
    [Serializable]
    public abstract class EntitySpawnRegistry<TEntity> : EntitySpawnRegistryBase<TEntity>
    where TEntity : class, IEntity
    {

    #region 추상 메서드

        // ------------------------------------------------------------
        /// <summary>
        /// 새 엔티티를 풀·생성 등으로 가져온다. 자식이 구현.
        /// </summary>
        // ------------------------------------------------------------
        protected abstract TEntity Acquire();

    #endregion

    #region 스폰

        // ------------------------------------------------------------
        /// <summary>
        /// 엔티티를 스폰한다.
        /// </summary>
        // ------------------------------------------------------------
        protected TEntity Spawn()
        {
            var entity = Acquire();

            Spawn(entity);

            return entity;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 엔티티를 스폰한다(외부 호출용).
        /// </summary>
        // ------------------------------------------------------------
        public virtual bool TrySpawn(out TEntity spawned)
        {
            spawned = Spawn();

            return spawned != null;
        }

    #endregion

    }

    // =================================================================
    /// <summary>
    /// 파라미터를 받는 Acquire/Spawn 을 제공하는 엔티티 스폰 레지스트리.
    /// </summary>
    // =================================================================
    [Serializable]
    public abstract class EntitySpawnRegistry<TEntity, TParam> : EntitySpawnRegistryBase<TEntity>
    where TEntity : class, IEntity, INeedToInit<TParam>
    {

    #region 자식 훅

        // ------------------------------------------------------------
        /// <summary>
        /// 자식 클래스의 Init 사전 작업 훅.
        /// </summary>
        // ------------------------------------------------------------
        protected virtual void OnInit(TEntity spawnable, TParam param) {}

    #endregion

    #region 추상 메서드

        // ------------------------------------------------------------
        /// <summary>
        /// 새 엔티티를 가져온다. 자식이 구현.
        /// </summary>
        // ------------------------------------------------------------
        protected abstract TEntity Acquire(TParam param);

    #endregion

    #region 스폰

        // ------------------------------------------------------------
        /// <summary>
        /// 엔티티를 스폰한다.
        /// </summary>
        // ------------------------------------------------------------
        protected TEntity Spawn(TParam param)
        {
            var entity = Acquire(param);

            void InitAction(TEntity s)
            {
                OnInit(s, param);
                s.Init(param);
            }

            Spawn(entity, InitAction);

            return entity;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 엔티티를 스폰한다(외부 호출용).
        /// </summary>
        // ------------------------------------------------------------
        public virtual bool TrySpawn(TParam param, out TEntity spawned)
        {
            spawned = Spawn(param);

            return spawned != null;
        }

    #endregion

    }
}
