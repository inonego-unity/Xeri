/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : Entity.cs
수정일 : 2026-05-07

# 설명
엔티티 추상 베이스 클래스. SpawnRegistry 등록 가능한 키·스폰 상태를 보유한다.
HP·Group 은 abstract 프로퍼티로 자식 클래스가 콘크리트 인스턴스를 보유·override 한다.
HP 사망 시 IReadOnlyHP.OnStateChange 구독으로 자동 디스폰한다.
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
    public abstract class Entity : IEntity, IDeepCloneableFrom<Entity>
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
        /// 키를 설정한다(레지스트리 내부 호출).
        /// </summary>
        // ------------------------------------------------------------
        protected internal virtual void SetKey(ulong key) => this.key = key;

        // ------------------------------------------------------------
        /// <summary>
        /// 키를 초기화한다(레지스트리 내부 호출).
        /// </summary>
        // ------------------------------------------------------------
        protected internal virtual void ClearKey() => this.key = null;

        void IEntity.SetKey(ulong key) => SetKey(key);
        void IEntity.ClearKey() => ClearKey();

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
        /// 디스폰 시 HP 를 죽음 상태로 설정할지 여부.
        /// </summary>
        // ------------------------------------------------------------
        public bool MakeDeadOnDespawn = true;

        // ------------------------------------------------------------
        /// <summary>
        /// 그룹 인덱스(시스템 내부 mutable 노출).
        /// </summary>
        // ------------------------------------------------------------
        public abstract IValue<int> Group { get; }

    #endregion

    #region HP 관련

        // ------------------------------------------------------------
        /// <summary>
        /// 체력(시스템 내부 mutable 노출).
        /// </summary>
        // ------------------------------------------------------------
        public abstract IHP HP { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// HP 이벤트를 구독하고 사망 상태였다면 살아있는 상태로 전환한다.
        /// </summary>
        // ------------------------------------------------------------
        private void InitHP()
        {
            var hp = HP;

            if (hp == null)
            {
                throw new InvalidOperationException("HP 가 설정되어 있지 않습니다.");
            }

            hp.OnStateChange += _OnHPStateChange;

            if (hp.IsDead)
            {
                hp.MakeAlive();
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// HP 이벤트 구독을 해제하고 옵션에 따라 사망 상태로 전환한다.
        /// </summary>
        // ------------------------------------------------------------
        private void ReleaseHP()
        {
            var hp = HP;

            if (hp == null)
            {
                throw new InvalidOperationException("HP 가 설정되어 있지 않습니다.");
            }

            // ------------------------------------------------------------
            // 먼저 이벤트를 해제해 디스폰이 중복 호출되는 것을 방지한다.
            // ------------------------------------------------------------
            hp.OnStateChange -= _OnHPStateChange;

            // ------------------------------------------------------------
            // 그 다음 옵션에 따라 HP 를 사망 상태로 전환한다.
            // ------------------------------------------------------------
            if (MakeDeadOnDespawn)
            {
                if (hp.IsAlive)
                {
                    hp.MakeDead();
                }
            }
        }

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// 기본 생성자.
        /// </summary>
        // ------------------------------------------------------------
        protected Entity() {}

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

        void ISpawnable.OnAfterSpawn()
        {
            InitHP();
        }

        void IDespawnable.OnBeforeDespawn()
        {
            ReleaseHP();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 디스폰 직후 훅. 자식이 override 가능.
        /// </summary>
        // ------------------------------------------------------------
        public virtual void OnAfterDespawn() {}

    #endregion

    #region 깊은 복사

        // ----------------------------------------------------------------------
        /// <summary>
        /// source 의 키·스폰 상태를 this 로 복사한다. HP·Group 의 깊은 복제는 자식 책임.
        /// </summary>
        // ----------------------------------------------------------------------
        public virtual void CloneFrom(Entity source)
        {
            if (source == null)
            {
                throw new ArgumentNullException("Entity.CloneFrom()의 인자가 null입니다.");
            }

            key       = source.key;
            isSpawned = false;
        }

    #endregion

    #region 이벤트 핸들러

        // ------------------------------------------------------------
        /// <summary>
        /// HP 상태가 사망으로 전환되면 자동으로 디스폰한다. 자식 hook 은 OnHPStateChange.
        /// </summary>
        // ------------------------------------------------------------
        private void _OnHPStateChange(object sender, ValueChangeEventArgs<HPState> e)
        {
            OnHPStateChange(sender, e);

            if (e.Current == HPState.Dead)
            {
                if (isSpawned)
                {
                    this.Despawn();
                }
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// HP 상태 변경 자식 hook. 자동 디스폰 직전에 호출된다.
        /// </summary>
        // ------------------------------------------------------------
        protected virtual void OnHPStateChange(object sender, ValueChangeEventArgs<HPState> e) {}

    #endregion

    }
}
