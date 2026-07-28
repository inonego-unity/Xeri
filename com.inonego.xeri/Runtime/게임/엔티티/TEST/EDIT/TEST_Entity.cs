/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TEST_Entity.cs
수정일 : 2026-07-29

# 설명
EntityBase 추상 클래스 유닛 테스트.
TestEntity / TestRegistry 콘크리트를 파일 내부에 정의해 추상 동작을 검증한다.
Unity Test Runner (Edit Mode) 에서 실행한다.

# 테스트 구성
 E: 기본 기능 (생성/HP 노출/IReadOnlyEntity 노출)
 S: 스폰 상태 (자동 디스폰/제거 시 HP 보존/키 자동 처리)
 C: 복제 (CloneFrom)
========================================================================= BLOCK_HEADER_END */

using System;

using NUnit;
using NUnit.Framework;

using inonego.Xeri;
using inonego.Xeri.Serializable;

namespace inonego.Xeri.TEST.Game._Entity
{

    using inonego.Xeri.Game;

    // ============================================================
    /// <summary>
    /// EntityBase 핵심 기능 테스트.
    /// </summary>
    // ============================================================
    public class TEST_Entity
    {

    #region 헬퍼

        // ------------------------------------------------------------
        /// <summary>
        /// HP_I 를 주입받아 int 데미지/힐을 적용 가능한 테스트 엔티티.
        /// </summary>
        // ------------------------------------------------------------
        private class TestEntity : EntityBase
        {
            private readonly HP_I       hp    = new HP_I { MaxValue = 100 };
            private readonly Value<int> group = new Value<int>();

            public override IHP         HP    => hp;
            public override IValue<int> Group => group;

            public void Damage(int amount) => hp.ApplyDamage(amount);
            public void Heal(int amount)   => hp.ApplyHeal(amount);

            public HP_I       InternalHP    => hp;
            public Value<int> InternalGroup => group;

            // 자식이 자기 데이터(HP·Group) 깊은 복제 책임 — 베이스는 키·스폰만 처리
            public override void CloneFrom(EntityBase source)
            {
                base.CloneFrom(source);

                if (source is TestEntity src)
                {
                    group.CloneFrom(src.group);
                }
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 외부에서 미리 만든 TestEntity 를 Spawn 시 반환할 수 있는 테스트 레지스트리.
        /// </summary>
        // ------------------------------------------------------------
        private class TestRegistry : EntitySpawnRegistry<TestEntity>
        {
            public TestEntity NextEntity { get; set; }

            protected override TestEntity Acquire() => NextEntity ?? new TestEntity();

            public bool TrySpawnPublic(out TestEntity spawned) => TrySpawn(out spawned);
        }

    #endregion

    #region E-1: 기본 생성

        [Test]
        public void TEST_Entity_기본_생성_HP_Group_보유_미스폰_상태()
        {
            var entity = new TestEntity();

            Assert.IsNotNull(entity.HP);
            Assert.IsNotNull(entity.Group);
            Assert.AreEqual(SpawnState.Despawned, entity.SpawnState);
            Assert.IsFalse(entity.HasKey);
        }

    #endregion

    #region E-2: HP 상태 노출

        [Test]
        public void TEST_Entity_HP_상태_IReadOnlyHP_노출()
        {
            var entity = new TestEntity();

            // 초기 상태 (HP_I 기본값) — Dead
            Assert.IsTrue(entity.HP.IsDead);
            Assert.IsFalse(entity.HP.IsAlive);
            Assert.AreEqual(HPState.Dead, entity.HP.Current);

            entity.InternalHP.MakeAlive();

            Assert.IsTrue(entity.HP.IsAlive);
            Assert.IsFalse(entity.HP.IsDead);
            Assert.AreEqual(HPState.Alive, entity.HP.Current);
        }

    #endregion

    #region E-3: IReadOnlyEntity 노출

        [Test]
        public void TEST_Entity_IReadOnlyEntity_캐스트_읽기전용_노출()
        {
            var entity = new TestEntity();
            entity.InternalGroup.Base = 9;
            entity.InternalHP.MakeAlive();

            IReadOnlyEntity readOnly = entity;

            Assert.AreEqual(9, readOnly.Group.Base);
            Assert.IsTrue(readOnly.HP.IsAlive);
            Assert.AreEqual(SpawnState.Despawned, readOnly.SpawnState);
            Assert.IsFalse(readOnly.HasKey);
        }

    #endregion

    #region S-1: HP 사망 시 자동 디스폰

        [Test]
        public void TEST_Entity_HP_사망_시_자동_디스폰()
        {
            var registry = new TestRegistry();
            var entity   = new TestEntity();
            entity.InternalHP.MakeAlive();

            registry.NextEntity = entity;
            Assert.IsTrue(registry.TrySpawnPublic(out _));
            Assert.AreEqual(SpawnState.Spawned, entity.SpawnState);
            Assert.IsTrue(entity.HasKey);

            entity.Damage(100);

            Assert.IsTrue(entity.HP.IsDead);
            Assert.AreEqual(SpawnState.Despawned, entity.SpawnState);
            Assert.IsFalse(entity.HasKey);
        }

    #endregion

    #region S-2: 일반 제거 시 HP 보존

        [Test]
        public void TEST_Entity_Removed_디스폰_후_HP_생존_유지()
        {
            var registry = new TestRegistry();
            var entity   = new TestEntity();
            entity.InternalHP.MakeAlive();

            registry.NextEntity = entity;
            Assert.IsTrue(registry.TrySpawnPublic(out _));
            Assert.IsTrue(entity.HP.IsAlive);

            entity.Despawn();

            Assert.AreEqual(SpawnState.Despawned, entity.SpawnState);
            Assert.IsTrue(entity.HP.IsAlive);
        }

    #endregion

    #region S-3: 키 자동 처리

        [Test]
        public void TEST_Entity_스폰_디스폰_시_키_자동_부여_해제()
        {
            var registry = new TestRegistry();
            var entity   = new TestEntity();
            entity.InternalHP.MakeAlive();

            Assert.IsFalse(entity.HasKey);

            registry.NextEntity = entity;
            Assert.IsTrue(registry.TrySpawnPublic(out _));
            Assert.IsTrue(entity.HasKey);

            var key = entity.Key;
            Assert.AreEqual(0UL, key);

            entity.Despawn();

            Assert.IsFalse(entity.HasKey);
            Assert.Throws<InvalidOperationException>(() => { var _ = entity.Key; });
        }

    #endregion

    #region C-1: CloneFrom 깊은 복제

        [Test]
        public void TEST_Entity_CloneFrom_Group_깊은_복제_미스폰()
        {
            var src = new TestEntity();
            src.InternalGroup.Base = 4;

            var clone = new TestEntity();
            clone.CloneFrom(src);

            Assert.AreEqual(src.Group.Base, clone.Group.Base);
            Assert.AreNotSame(src.Group, clone.Group);

            // CloneFrom은 Registry 실행 상태를 복제하지 않는다.
            Assert.AreEqual(SpawnState.Despawned, clone.SpawnState);
        }

    #endregion

    }

}
