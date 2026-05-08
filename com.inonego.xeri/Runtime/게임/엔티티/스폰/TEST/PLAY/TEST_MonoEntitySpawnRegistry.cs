/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TEST_MonoEntitySpawnRegistry.cs
수정일 : 2026-05-08

# 설명
MonoEntitySpawnRegistry 핵심 동작 테스트.
EntitySpawnRegistry 와 Connect 후 자동 동기화·디스폰을 검증한다.
Unity Test Runner (Play Mode) 에서 실행 — GameObject·Prefab 생성·소멸이 필요.

# 테스트 구성
 C: Connect / Disconnect (기존 엔티티 동기화 / 일괄 디스폰)
 S: 자동 동기화 (스폰/디스폰 전파)
 R: ReSpawnAll (재스폰)
========================================================================= BLOCK_HEADER_END */

using System.Collections;

using UnityEngine;
using UnityEngine.TestTools;

using NUnit.Framework;

using inonego.Xeri.Serializable;

namespace inonego.Xeri.TEST.Game._EntitySpawn
{

    using inonego.Xeri.Game;

    // ============================================================
    /// <summary>
    /// MonoEntitySpawnRegistry 핵심 기능 테스트.
    /// </summary>
    // ============================================================
    public class TEST_MonoEntitySpawnRegistry
    {

    #region 헬퍼

        // ------------------------------------------------------------
        /// <summary>
        /// HP_I 를 주입받는 테스트 엔티티.
        /// </summary>
        // ------------------------------------------------------------
        private class TestEntity : Entity
        {
            private readonly HP_I       hp    = new HP_I { MaxValue = 100 };
            private readonly Value<int> group = new Value<int>();

            public override IHP         HP    => hp;
            public override IValue<int> Group => group;

            public void Damage(int amount) => hp.ApplyDamage(amount);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 테스트용 모노 엔티티.
        /// </summary>
        // ------------------------------------------------------------
        private class TestMonoEntity : MonoEntity<TestEntity> {}

        // ------------------------------------------------------------
        /// <summary>
        /// 외부 인스턴스 주입 가능한 테스트 엔티티 레지스트리.
        /// </summary>
        // ------------------------------------------------------------
        private class TestEntityRegistry : EntitySpawnRegistry<TestEntity>
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

        // ------------------------------------------------------------
        /// <summary>
        /// PrefabGameObjectProvider 를 주입받는 테스트 모노 레지스트리.
        /// </summary>
        // ------------------------------------------------------------
        private class TestMonoRegistry : MonoEntitySpawnRegistry<TestMonoEntity, TestEntity>
        {
            public TestMonoRegistry(IGameObjectProvider provider) : base(provider) {}
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 코드로 prefab 대용 GameObject 를 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        private (TestMonoRegistry mono, TestEntityRegistry entity) CreateRegistries()
        {
            prefab = new GameObject("TestPrefab");
            prefab.AddComponent<TestMonoEntity>();
            prefab.SetActive(false);

            var provider = new PrefabGameObjectProvider();
            provider.Prefab = prefab;

            var mono   = new TestMonoRegistry(provider);
            var entity = new TestEntityRegistry();

            return (mono, entity);
        }

    #endregion

    #region 픽스처

        private GameObject prefab;

        // ------------------------------------------------------------
        /// <summary>
        /// 테스트 종료 시 prefab GameObject 를 정리한다.
        /// </summary>
        // ------------------------------------------------------------
        [TearDown]
        public void TearDown()
        {
            if (prefab != null)
            {
                Object.DestroyImmediate(prefab);

                prefab = null;
            }
        }

    #endregion

    #region C-1: Connect 시 기존 엔티티 동기화

        [UnityTest]
        public IEnumerator TEST_MonoEntitySpawnRegistry_Connect_기존_엔티티_자동_스폰()
        {
            var (mono, entity) = CreateRegistries();

            entity.TrySpawn(out _);
            entity.TrySpawn(out _);

            mono.Connect(entity);

            Assert.AreEqual(2, mono.Spawned.Count);

            mono.Disconnect();

            yield return null;
        }

    #endregion

    #region C-2: Disconnect 시 일괄 디스폰

        [UnityTest]
        public IEnumerator TEST_MonoEntitySpawnRegistry_Disconnect_DespawnAll()
        {
            var (mono, entity) = CreateRegistries();

            mono.Connect(entity);
            entity.TrySpawn(out _);
            entity.TrySpawn(out _);

            Assert.AreEqual(2, mono.Spawned.Count);

            mono.Disconnect();

            Assert.AreEqual(0, mono.Spawned.Count);

            yield return null;
        }

    #endregion

    #region S-1: 엔티티 스폰 시 모노 엔티티 자동 스폰

        [UnityTest]
        public IEnumerator TEST_MonoEntitySpawnRegistry_엔티티_스폰_시_모노엔티티_자동_스폰()
        {
            var (mono, entity) = CreateRegistries();

            mono.Connect(entity);

            entity.TrySpawn(out var spawned);

            Assert.AreEqual(1, mono.Spawned.Count);
            Assert.IsTrue(mono.Spawned.ContainsKey(spawned.Key));

            mono.Disconnect();

            yield return null;
        }

    #endregion

    #region S-2: 엔티티 디스폰 시 모노 엔티티 자동 디스폰

        [UnityTest]
        public IEnumerator TEST_MonoEntitySpawnRegistry_엔티티_디스폰_시_모노엔티티_자동_디스폰()
        {
            var (mono, entity) = CreateRegistries();

            mono.Connect(entity);

            entity.TrySpawn(out var spawned);
            Assert.AreEqual(1, mono.Spawned.Count);

            spawned.Damage(100);
            // HP=0 → 자동 디스폰 → mono 도 자동 디스폰

            Assert.AreEqual(0, mono.Spawned.Count);

            mono.Disconnect();

            yield return null;
        }

    #endregion

    #region R-1: ReSpawnAll 재스폰

        [UnityTest]
        public IEnumerator TEST_MonoEntitySpawnRegistry_ReSpawnAll_동일_엔티티_재대응()
        {
            var (mono, entity) = CreateRegistries();

            mono.Connect(entity);
            entity.TrySpawn(out _);
            entity.TrySpawn(out _);

            Assert.AreEqual(2, mono.Spawned.Count);

            mono.ReSpawnAll();

            Assert.AreEqual(2, mono.Spawned.Count);

            mono.Disconnect();

            yield return null;
        }

    #endregion

    }

}
