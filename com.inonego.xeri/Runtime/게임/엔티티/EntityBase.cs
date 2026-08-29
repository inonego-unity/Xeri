/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : EntityBase.cs
수정일 : 2026-08-29

# 설명
엔티티 추상 베이스 클래스.
Registry가 관리하는 Key·SpawnState와 Entity가 소유하는 HP·Group 계약을 제공한다.
HP가 Dead 상태가 되면 Registry에 Dead 사유의 디스폰을 요청한다.
Key는 Inspector에서 확인할 수 있도록 직렬화하며 SpawnState는 현재 실행에만 속한다.
========================================================================= BLOCK_HEADER_END */

using System;

using UnityEngine;

namespace inonego.Xeri.Game
{
    using Xeri.Serializable;

    // ============================================================
    /// <summary>
    /// 엔티티 추상 베이스 클래스.
    /// </summary>
    // ============================================================
    [Serializable]
    public abstract class EntityBase : IEntity, IDeepCloneableFrom<EntityBase>
    {

    #region 키 설정

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 키. 미설정 시 접근하면 예외.
        /// </summary>
        // ------------------------------------------------------------
        public ulong Key
        {
            get
            {
                if (key.HasValue)
                {
                    return key.Value;
                }

                throw new InvalidOperationException("키가 설정되어 있지 않습니다.");
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 키 설정 여부.
        /// </summary>
        // ------------------------------------------------------------
        public bool HasKey => key.HasValue;

        [SerializeField, ReadOnly]
        protected XNullable<ulong> key = null;

        // ------------------------------------------------------------
        /// <summary>
        /// Registry가 확정한 Entity key를 설정한다.
        /// </summary>
        // ------------------------------------------------------------
        protected internal virtual void SetKey(ulong key) => this.key = key;

        // ------------------------------------------------------------
        /// <summary>
        /// Registry가 관리하던 Entity key를 해제한다.
        /// </summary>
        // ------------------------------------------------------------
        protected internal virtual void ClearKey() => key = null;

        // ------------------------------------------------------------
        /// <summary>
        /// IEntity 계약을 통해 Registry가 확정한 Entity key를 설정한다.
        /// </summary>
        // ------------------------------------------------------------
        void IEntity.SetKey(ulong key) => SetKey(key);

        // ------------------------------------------------------------
        /// <summary>
        /// IEntity 계약을 통해 Registry가 관리하던 Entity key를 해제한다.
        /// </summary>
        // ------------------------------------------------------------
        void IEntity.ClearKey() => ClearKey();

    #endregion

    #region 필드

        // ------------------------------------------------------------
        /// <summary>
        /// Registry가 관리하는 현재 스폰 처리 상태.
        /// </summary>
        // ------------------------------------------------------------
        public SpawnState SpawnState => spawnState;

        [NonSerialized]
        protected SpawnState spawnState = SpawnState.Despawned;

        [NonSerialized]
        private Action<DespawnReason> despawnFromRegistry = null;

        // ------------------------------------------------------------
        /// <summary>
        /// 시스템 내부에서 변경할 수 있는 그룹 인덱스.
        /// </summary>
        // ------------------------------------------------------------
        public abstract IValue<int> Group { get; }

    #endregion

    #region HP 관련

        // ------------------------------------------------------------
        /// <summary>
        /// 시스템 내부에서 변경할 수 있는 체력.
        /// </summary>
        // ------------------------------------------------------------
        public abstract IHP HP { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// HP 상태 변경 이벤트를 구독한다.
        /// </summary>
        // ------------------------------------------------------------
        private void BindHP()
        {
            if (spawnState != SpawnState.Spawned)
            {
                throw new InvalidOperationException
                (
                    $"Spawned 상태에서만 HP 이벤트 구독을 시작할 수 있습니다. 현재 상태: {spawnState}"
                );
            }

            var hp = HP;

            if (hp == null)
            {
                throw new InvalidOperationException("HP 가 설정되어 있지 않습니다.");
            }

            if (!hp.IsAlive)
            {
                throw new InvalidOperationException
                (
                    "사망한 엔티티는 명시적인 부활 또는 HP 초기화 없이 스폰할 수 없습니다."
                );
            }

            // Deserialize callback이 중복 호출돼도 같은 handler가 중복 등록되지 않게 정규화한다.
            hp.OnStateChange -= _OnHPStateChange;
            hp.OnStateChange += _OnHPStateChange;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// HP 이벤트 구독을 해제하고 Dead Reason이면 사망 상태를 보장한다.
        /// </summary>
        // ------------------------------------------------------------
        private void UnbindHP(DespawnReason reason)
        {
            if (spawnState != SpawnState.Despawning)
            {
                throw new InvalidOperationException
                (
                    $"Despawning 상태에서만 HP 이벤트 구독을 종료할 수 있습니다. 현재 상태: {spawnState}"
                );
            }

            var hp = HP;

            if (hp == null)
            {
                throw new InvalidOperationException("HP 가 설정되어 있지 않습니다.");
            }

            // 이벤트를 먼저 해제하여 Dead Reason 보정이 중복 디스폰으로 이어지지 않게 한다.
            hp.OnStateChange -= _OnHPStateChange;

            // 사망 사유만 HP를 사망으로 맞추며 Removed와 Cleanup은 현재 HP를 보존한다.
            if (reason.Kind == DespawnKind.Dead && hp.IsAlive)
            {
                hp.MakeDead();
            }
        }

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// 기본 생성자.
        /// </summary>
        // ------------------------------------------------------------
        protected EntityBase() : base() {}

    #endregion

    #region 인터페이스 구현

        // ------------------------------------------------------------
        /// <summary>
        /// 외부 노출용 IReadOnlyEntity.HP 명시 구현.
        /// </summary>
        // ------------------------------------------------------------
        IReadOnlyHP IReadOnlyEntity.HP => HP;

        // ------------------------------------------------------------
        /// <summary>
        /// 외부 노출용 IReadOnlyEntity.Group 명시 구현.
        /// </summary>
        // ------------------------------------------------------------
        IReadOnlyValue<int> IReadOnlyEntity.Group => Group;

        // ------------------------------------------------------------
        /// <summary>
        /// SpawnRegistry가 관리하는 스폰 처리 상태를 읽거나 변경한다.
        /// </summary>
        // ------------------------------------------------------------
        SpawnState ISpawnRegistryObject<ulong>.SpawnState
        {
            get => spawnState;
            set => spawnState = value;
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// Entity의 디스폰 요청을 현재 Registry에 전달하는 콜백을 읽거나 변경한다.
        /// </summary>
        // ----------------------------------------------------------------------
        Action<DespawnReason> IDespawnable.DespawnFromRegistry
        {
            get => despawnFromRegistry;
            set => despawnFromRegistry = value;
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// Registry가 Spawned 소유 관계를 확정할 때 HP 런타임 연결을 구성한다.
        /// </summary>
        // ----------------------------------------------------------------------
        void IEntity.OnRegistrationAttached()
        {
            BindHP();
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// Registry가 Spawned 소유 관계를 해제할 때 HP 런타임 연결을 정리한다.
        /// </summary>
        // ----------------------------------------------------------------------
        void IEntity.OnRegistrationDetached(DespawnReason reason)
        {
            UnbindHP(reason);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Spawning 상태에서 호출되는 파생 Entity 훅.
        /// </summary>
        // ------------------------------------------------------------
        protected virtual void OnSpawning() {}

        // ------------------------------------------------------------
        /// <summary>
        /// Registry 등록 전에 파생 Entity의 Spawning 훅을 호출한다.
        /// </summary>
        // ------------------------------------------------------------
        void ISpawnable.OnSpawning()
        {
            OnSpawning();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Spawned 상태에서 HP 구독이 준비된 뒤 호출되는 파생 Entity 훅.
        /// </summary>
        // ------------------------------------------------------------
        protected virtual void OnSpawned() {}

        // ------------------------------------------------------------
        /// <summary>
        /// Registry 등록 완료 뒤 파생 Entity의 실제 Spawn 완료 훅을 호출한다.
        /// </summary>
        // ------------------------------------------------------------
        void ISpawnable.OnSpawned()
        {
            OnSpawned();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Despawning 상태에서 HP 구독을 해제하기 전에 호출되는 파생 Entity 훅.
        /// </summary>
        // ------------------------------------------------------------
        protected virtual void OnDespawning(DespawnReason reason) {}

        // ------------------------------------------------------------
        /// <summary>
        /// Registry 등록 해제 전에 파생 Entity 훅을 호출한다.
        /// </summary>
        // ------------------------------------------------------------
        void IDespawnable.OnDespawning(DespawnReason reason)
        {
            OnDespawning(reason);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Registry 등록 해제 뒤 Despawning 상태에서 호출되는 파생 Entity 훅.
        /// </summary>
        // ------------------------------------------------------------
        protected virtual void OnDespawned(DespawnReason reason) {}

        // ------------------------------------------------------------
        /// <summary>
        /// Registry 등록 해제 뒤 파생 Entity의 완료 훅과 Key 해제를 수행한다.
        /// </summary>
        // ------------------------------------------------------------
        void IDespawnable.OnDespawned(DespawnReason reason)
        {
            try
            {
                OnDespawned(reason);
            }
            finally
            {
                // 파생 훅이 실패해도 Despawned Entity에 이전 Registry Key를 남기지 않는다.
                ClearKey();
            }
        }

    #endregion

    #region 깊은 복사

        // ----------------------------------------------------------------------
        /// <summary>
        /// 파생 클래스의 영속 데이터 복제 전에 Registry 소유 상태를 초기화한다.
        /// </summary>
        // ----------------------------------------------------------------------
        public virtual void CloneFrom(EntityBase source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (spawnState != SpawnState.Despawned)
            {
                throw new InvalidOperationException
                (
                    $"Despawned 상태의 Entity에만 복제할 수 있습니다. 현재 상태: {spawnState}"
                );
            }

            // 독립 복제 객체가 원본 Registry의 key와 콜백을 소유하지 않도록 런타임 상태를 초기화한다.
            ClearKey();
            spawnState          = SpawnState.Despawned;
            despawnFromRegistry = null;
        }

    #endregion

    #region 이벤트 핸들러

        // ----------------------------------------------------------------------
        /// <summary>
        /// HP 상태가 사망으로 전환되면 파생 훅 이후 Dead 사유로 디스폰한다.
        /// </summary>
        // ----------------------------------------------------------------------
        private void _OnHPStateChange(object sender, ValueChangeEventArgs<HPState> e)
        {
            OnHPStateChange(sender, e);

            // 파생 훅이 다른 사유로 먼저 디스폰했다면 해당 결정을 보존한다.
            if (e.Current == HPState.Dead && spawnState == SpawnState.Spawned)
            {
                this.Despawn(DespawnReason.Dead);
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 자동 디스폰 요청 전에 호출되는 HP 상태 변경 파생 훅.
        /// </summary>
        // ------------------------------------------------------------
        protected virtual void OnHPStateChange(object sender, ValueChangeEventArgs<HPState> e) {}

    #endregion

    }
}
