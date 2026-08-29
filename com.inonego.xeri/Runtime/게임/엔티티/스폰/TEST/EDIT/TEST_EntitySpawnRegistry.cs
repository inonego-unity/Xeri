/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TEST_EntitySpawnRegistry.cs
수정일 : 2026-08-29

# 설명
EntitySpawnRegistry 핵심 동작 테스트.
키 자동 생성·해제, HP 연동 자동 디스폰, 복제를 검증한다.
Unity Test Runner (Edit Mode) 에서 실행한다.

# 테스트 구성
 E: 기본 기능 (키 자동 생성/디스폰 시 키 클리어)
 H: HP 연동 (HP 사망 자동 디스폰)
 S: 직렬화 (Registry/Entity/HP/KeyGenerator JSON round-trip)
 X: 예외 처리 (사망 엔티티 스폰 롤백)
========================================================================= BLOCK_HEADER_END */

using System;

using UnityEngine;

using NUnit;
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
        [Serializable]
        private class TestEntity : EntityBase
        {
            [SerializeField]
            private HP_I hp = new HP_I { MaxValue = 100 };

            [SerializeField]
            private Value<int> group = new Value<int>();

            public override IHP         HP    => hp;
            public override IValue<int> Group => group;

            public TestEntity() : base()
            {
                hp.MakeAlive();
            }

            public void Damage(int amount) => hp.ApplyDamage(amount);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 외부 인스턴스 주입 + TrySpawn 노출 테스트 레지스트리.
        /// </summary>
        // ------------------------------------------------------------
        [Serializable]
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
            Assert.AreEqual(SpawnState.Despawned, entity.SpawnState);
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
            Assert.AreEqual(SpawnState.Despawned, entity.SpawnState);
            Assert.AreEqual(0, registry.Spawned.Count);
        }

    #endregion

    #region S-1: JSON Round-trip

        // ------------------------------------------------------------------------------------------
        /// <summary>
        /// JsonUtility round-trip 뒤 Entity membership, HP 자동 디스폰 연결과 KeyGenerator 연속성을 확인한다.
        /// </summary>
        // ------------------------------------------------------------------------------------------
        [Test]
        public void TEST_EntitySpawnRegistry_JsonUtility_RoundTrip_HP연결과_KeyGenerator_복원()
        {
            var registry = new TestRegistry();

            Assert.IsTrue(registry.TrySpawn(out var first));
            Assert.IsTrue(registry.TrySpawn(out var second));

            var firstKey = first.Key;
            var secondKey = second.Key;
            var json = JsonUtility.ToJson(registry);
            var restored = JsonUtility.FromJson<TestRegistry>(json);

            Assert.IsNotNull(restored);
            Assert.AreEqual(2, restored.Spawned.Count);
            Assert.IsNotNull(restored.Find(firstKey));
            Assert.IsNotNull(restored.Find(secondKey));

            var restoredFirst = restored.Find(firstKey);
            restoredFirst.Damage(100);

            Assert.IsTrue(restoredFirst.HP.IsDead);
            Assert.AreEqual(SpawnState.Despawned, restoredFirst.SpawnState);
            Assert.AreEqual(1, restored.Spawned.Count);
            Assert.IsNull(restored.Find(firstKey));

            Assert.IsTrue(restored.TrySpawn(out var next));
            Assert.AreEqual(2UL, next.Key);
        }

    #endregion

    #region X-1: 사망 엔티티 스폰 롤백

        [Test]
        public void TEST_EntitySpawnRegistry_Dead_엔티티_스폰_실패_상태_롤백()
        {
            var registry = new TestRegistry();
            var entity   = new TestEntity();
            entity.Damage(100);

            registry.NextEntity = entity;

            Assert.Throws<InvalidOperationException>(() => registry.TrySpawn(out _));
            Assert.AreEqual(SpawnState.Despawned, entity.SpawnState);
            Assert.IsFalse(entity.HasKey);
            Assert.AreEqual(0, registry.Spawned.Count);
        }

    #endregion

    }

}
