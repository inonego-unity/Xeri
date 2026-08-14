/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TEST_EntityViewController.cs
수정일 : 2026-08-14

# 설명
EntityViewController의 Entity Registry 연동 계약을 검증한다.
Bind 후 기존 Entity 동기화와 Spawn·Despawn 전파, Unbind·ReSpawnAll 수명을 확인한다.
Unity Test Runner (Play Mode)에서 실행한다.

# 테스트 구성
 C: Bind / Unbind (기존 Entity 동기화 / 일괄 Despawn)
 S: 자동 동기화 (Spawn / Despawn 전파)
 R: ReSpawnAll (재생성)
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;

using UnityEngine;
using UnityEngine.TestTools;

using NUnit;
using NUnit.Framework;

using inonego.Xeri;
using inonego.Xeri.Serializable;

namespace inonego.Xeri.TEST.Game._EntitySpawn
{

    using inonego.Xeri.Game;

    // ================================================================================
    /// <summary>
    /// EntityViewController의 Entity Registry 연동 수명 계약을 검증한다.
    /// </summary>
    // ================================================================================
    public class TEST_EntityViewController
    {

    #region 헬퍼

        // ------------------------------------------------------------
        /// <summary>
        /// HP_I를 주입받는 테스트 Entity.
        /// </summary>
        // ------------------------------------------------------------
        private class TestEntity : EntityBase
        {
            private readonly HP_I hp = new HP_I { MaxValue = 100 };
            private readonly Value<int> group = new Value<int>();

            // ------------------------------------------------------------
            /// <summary>
            /// 테스트 Entity의 HP 상태.
            /// </summary>
            // ------------------------------------------------------------
            public override IHP HP => hp;

            // ------------------------------------------------------------
            /// <summary>
            /// 테스트 Entity의 Group 값.
            /// </summary>
            // ------------------------------------------------------------
            public override IValue<int> Group => group;

            // ------------------------------------------------------------
            /// <summary>
            /// 생존 상태인 테스트 Entity를 생성한다.
            /// </summary>
            // ------------------------------------------------------------
            public TestEntity() : base()
            {
                hp.MakeAlive();
            }

            // ------------------------------------------------------------
            /// <summary>
            /// HP에 피해를 적용한다.
            /// </summary>
            // ------------------------------------------------------------
            public void Damage(int amount) => hp.ApplyDamage(amount);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 테스트용 Entity View.
        /// </summary>
        // ------------------------------------------------------------
        private class TestEntityView : EntityViewBase<TestEntity>
        {
            // NONE
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 외부 인스턴스를 주입할 수 있는 테스트 Entity Registry.
        /// </summary>
        // ------------------------------------------------------------
        private class TestEntityRegistry : EntitySpawnRegistry<TestEntity>
        {
            // ------------------------------------------------------------
            /// <summary>
            /// 다음 Spawn에서 사용할 Entity.
            /// </summary>
            // ------------------------------------------------------------
            public TestEntity NextEntity { get; set; }

            // ------------------------------------------------------------
            /// <summary>
            /// 지정된 다음 Entity 또는 새 Entity를 획득한다.
            /// </summary>
            // ------------------------------------------------------------
            protected override TestEntity Acquire()
            {
                var entity = NextEntity ?? new TestEntity();
                NextEntity = null;

                return entity;
            }

            // ------------------------------------------------------------
            /// <summary>
            /// 테스트에서 Entity Spawn 진입점을 노출한다.
            /// </summary>
            // ------------------------------------------------------------
            public new bool TrySpawn(out TestEntity spawned) => base.TrySpawn(out spawned);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 테스트용 Prefab, Controller와 Entity Registry를 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        private
        (
            EntityViewController<TestEntityView, TestEntity> controller,
            TestEntityRegistry entity
        ) CreateRegistries()
        {
            prefab = new GameObject("TestPrefab");
            prefab.AddComponent<TestEntityView>();
            prefab.SetActive(false);

            var provider = new PrefabGameObjectProvider(prefab, null);
            var factory = new EntityViewFactory<TestEntityView, TestEntity>(provider);
            var controller = new EntityViewController<TestEntityView, TestEntity>(factory);
            var entity = new TestEntityRegistry();

            return (controller, entity);
        }

    #endregion

    #region 픽스처

        private GameObject prefab = null;

        // ------------------------------------------------------------
        /// <summary>
        /// 테스트 종료 시 Prefab GameObject를 정리한다.
        /// </summary>
        // ------------------------------------------------------------
        [TearDown]
        public void TearDown()
        {
            if (prefab != null)
            {
                UnityEngine.Object.DestroyImmediate(prefab);
                prefab = null;
            }
        }

    #endregion

    #region C-1: Bind 시 기존 Entity 동기화

        // ------------------------------------------------------------
        /// <summary>
        /// Bind 시 이미 Spawn된 Entity의 View를 생성하는지 검증한다.
        /// </summary>
        // ------------------------------------------------------------
        [UnityTest]
        public IEnumerator TEST_EntityViewController_Bind_기존_Entity_View_생성()
        {
            var (controller, entity) = CreateRegistries();

            entity.TrySpawn(out var first);
            entity.TrySpawn(out var second);

            controller.Bind(entity);

            Assert.AreEqual(2, controller.Views.Count);
            Assert.AreSame(first, controller.Views[first.Key].Entity);
            Assert.AreSame(second, controller.Views[second.Key].Entity);

            controller.Unbind();

            yield return null;
        }

    #endregion

    #region C-2: Unbind 시 일괄 Despawn

        // ------------------------------------------------------------
        /// <summary>
        /// Unbind 시 Controller가 소유한 모든 View를 반환하는지 검증한다.
        /// </summary>
        // ------------------------------------------------------------
        [UnityTest]
        public IEnumerator TEST_EntityViewController_Unbind_DespawnAll()
        {
            var (controller, entity) = CreateRegistries();

            controller.Bind(entity);
            entity.TrySpawn(out _);
            entity.TrySpawn(out _);

            Assert.AreEqual(2, controller.Views.Count);

            controller.Unbind();

            Assert.AreEqual(0, controller.Views.Count);

            yield return null;
        }

    #endregion

    #region S-1: Entity Spawn 시 View 자동 생성

        // ------------------------------------------------------------
        /// <summary>
        /// 바인딩된 Entity가 Spawn되면 대응 View를 생성하는지 검증한다.
        /// </summary>
        // ------------------------------------------------------------
        [UnityTest]
        public IEnumerator TEST_EntityViewController_Entity_Spawn_View_생성()
        {
            var (controller, entity) = CreateRegistries();

            controller.Bind(entity);
            entity.TrySpawn(out var spawned);

            Assert.AreEqual(1, controller.Views.Count);
            Assert.IsTrue(controller.Views.ContainsKey(spawned.Key));
            Assert.AreSame(spawned, controller.Views[spawned.Key].Entity);

            controller.Unbind();

            yield return null;
        }

    #endregion

    #region S-2: Entity Despawn 시 View 자동 반환

        // ------------------------------------------------------------
        /// <summary>
        /// 바인딩된 Entity가 Despawn되면 대응 View를 반환하는지 검증한다.
        /// </summary>
        // ------------------------------------------------------------
        [UnityTest]
        public IEnumerator TEST_EntityViewController_Entity_Despawn_View_반환()
        {
            var (controller, entity) = CreateRegistries();

            controller.Bind(entity);
            entity.TrySpawn(out var spawned);
            Assert.AreEqual(1, controller.Views.Count);

            // HP 소진으로 Entity Registry의 Despawn 흐름을 발생시킨다.
            spawned.Damage(100);

            Assert.AreEqual(0, controller.Views.Count);

            controller.Unbind();

            yield return null;
        }

    #endregion

    #region R-1: ReSpawnAll

        // ------------------------------------------------------------
        /// <summary>
        /// ReSpawnAll 뒤에도 동일한 Entity 집합과 View 수를 유지하는지 검증한다.
        /// </summary>
        // ------------------------------------------------------------
        [UnityTest]
        public IEnumerator TEST_EntityViewController_ReSpawnAll_동일_Entity_재대응()
        {
            var (controller, entity) = CreateRegistries();

            controller.Bind(entity);
            entity.TrySpawn(out _);
            entity.TrySpawn(out _);

            Assert.AreEqual(2, controller.Views.Count);

            controller.ReSpawnAll();

            Assert.AreEqual(2, controller.Views.Count);

            controller.Unbind();

            yield return null;
        }

    #endregion

    }

}
