/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TEST_EntitySpawnRegistry.cs
수정일 : 2026-05-28

# 설명
EntitySpawnRegistry 핵심 동작 테스트.
키 자동 생성·해제, HP 연동 자동 디스폰, 복제를 검증한다.
Unity Test Runner (Edit Mode) 에서 실행한다.

# 테스트 구성
 E: 기본 기능 (키 자동 생성/디스폰 시 키 클리어)
 H: HP 연동 (HP 사망 자동 디스폰)
========================================================================= BLOCK_HEADER_END */

using NUnit.Framework;

using inonego.Xeri;
using inonego.Xeri.Serializable;

namespace inonego.Xeri.TEST.Game._EntitySpawn
{

    using inonego.Xeri.Game;

    // ============================================================
    /// <summary>
    /// EntitySpawnRegistry 핵심 기능 테스트.
    /// </summary>
    // ============================================================
    public class TEST_EntitySpawnRegistry
    {

    #region 헬퍼

        // ------------------------------------------------------------
        /// <summary>
        /// HP_I 를 주입받는 테스트 엔티티.
        /// </summary>
        // ------------------------------------------------------------
        private class TestEntity : EntityBase
        {
            private readonly HP_I       hp    = new HP_I { MaxValue = 100 };
            private readonly Value<int> group = new Value<int>();

            public override IHP         HP    => hp;
            public override IValue<int> Group => group;

            public void Damage(int amount) => hp.ApplyDamage(amount);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 외부 인스턴스 주입 + TrySpawn 노출 테스트 레지스트리.
        /// </summary>
        // ------------------------------------------------------------
        private class TestRegistry : EntitySpawnRegistry<TestEntity>
        {
            public TestEntity NextEntity { get; set; }

            protected override TestEntity Acquire()
            {
                var e = NextEntity ?? new TestEntity();
                NextEntity = null;

                return e;
            }

            public new bool TrySpawn(out TestEntity spawned) => base.TrySpawn(out spawned);
        }

    #endregion

    #region E-1: 키 자동 생성

        [Test]
        public void TEST_EntitySpawnRegistry_TrySpawn_키_0_1_2_순차_부여()
        {
            var registry = new TestRegistry();

            registry.TrySpawn(out var a);
            registry.TrySpawn(out var b);
            registry.TrySpawn(out var c);

            Assert.AreEqual(0UL, a.Key);
            Assert.AreEqual(1UL, b.Key);
            Assert.AreEqual(2UL, c.Key);
        }

    #endregion

    #region E-2: 디스폰 시 키 클리어

        [Test]
        public void TEST_EntitySpawnRegistry_Despawn_시_키_및_스폰_상태_클리어()
        {
            var registry = new TestRegistry();

            registry.TrySpawn(out var entity);
            Assert.IsTrue(entity.HasKey);

            entity.Despawn();

            Assert.IsFalse(entity.HasKey);
            Assert.IsFalse(entity.IsSpawned);
        }

    #endregion

    #region H-1: HP 사망 자동 디스폰

        [Test]
        public void TEST_EntitySpawnRegistry_HP_사망_시_자동_디스폰_및_딕셔너리_제거()
        {
            var registry = new TestRegistry();
            var entity   = new TestEntity();

            registry.NextEntity = entity;
            Assert.IsTrue(registry.TrySpawn(out _));

            entity.Damage(100);

            Assert.IsTrue(entity.HP.IsDead);
            Assert.IsFalse(entity.IsSpawned);
            Assert.AreEqual(0, registry.Spawned.Count);
        }

    #endregion

    }

}
