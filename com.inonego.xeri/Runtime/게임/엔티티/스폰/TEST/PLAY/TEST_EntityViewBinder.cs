/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TEST_EntityViewBinder.cs
수정일 : 2026-06-17

# 설명
EntityViewBinder 핵심 동작 테스트.
EntitySpawnRegistry 와 Bind 후 자동 view 동기화·회수를 검증한다.
Unity Test Runner (Play Mode) 에서 실행 — GameObject·Prefab 생성·소멸이 필요.

# 테스트 구성
 C: Bind / Unbind (기존 엔티티 동기화 / 일괄 디스폰)
 S: 자동 동기화 (스폰/디스폰 전파)
 R: ReSpawnAll (재스폰)
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

    // ============================================================
    /// <summary>
    /// EntityViewBinder 핵심 기능 테스트.
    /// </summary>
    // ============================================================
    public class TEST_EntityViewBinder
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
        /// 테스트용 엔티티 view.
        /// </summary>
        // ------------------------------------------------------------
        private class TestEntityView : EntityViewBase<TestEntity> {}

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
        /// 코드로 prefab 대용 GameObject 를 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        private (EntityViewBinder<TestEntityView, TestEntity> view, TestEntityRegistry entity) CreateRegistries()
        {
            prefab = new GameObject("TestPrefab");
            prefab.AddComponent<TestEntityView>();
            prefab.SetActive(false);

            var provider = new PrefabGameObjectProvider(prefab, null);
            var factory  = new EntityViewFactory<TestEntityView, TestEntity>(provider);
            var view     = new EntityViewBinder<TestEntityView, TestEntity>(factory);
            var entity   = new TestEntityRegistry();

            return (view, entity);
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
                UnityEngine.Object.DestroyImmediate(prefab);

                prefab = null;
            }
        }

    #endregion

    #region C-1: Bind 시 기존 엔티티 동기화

        [UnityTest]
        public IEnumerator TEST_EntityViewBinder_Bind_기존_엔티티_자동_view_생성()
        {
            var (view, entity) = CreateRegistries();

            entity.TrySpawn(out _);
            entity.TrySpawn(out _);

            view.Bind(entity);

            Assert.AreEqual(2, view.Views.Count);

            view.Unbind();

            yield return null;
        }

    #endregion

    #region C-2: Unbind 시 일괄 디스폰

        [UnityTest]
        public IEnumerator TEST_EntityViewBinder_Unbind_ReleaseAll()
        {
            var (view, entity) = CreateRegistries();

            view.Bind(entity);
            entity.TrySpawn(out _);
            entity.TrySpawn(out _);

            Assert.AreEqual(2, view.Views.Count);

            view.Unbind();

            Assert.AreEqual(0, view.Views.Count);

            yield return null;
        }

    #endregion

    #region S-1: 엔티티 스폰 시 모노 엔티티 자동 스폰

        [UnityTest]
        public IEnumerator TEST_EntityViewBinder_엔티티_스폰_시_view_자동_생성()
        {
            var (view, entity) = CreateRegistries();

            view.Bind(entity);

            entity.TrySpawn(out var spawned);

            Assert.AreEqual(1, view.Views.Count);
            Assert.IsTrue(view.Views.ContainsKey(spawned.Key));

            view.Unbind();

            yield return null;
        }

    #endregion

    #region S-2: 엔티티 디스폰 시 모노 엔티티 자동 디스폰

        [UnityTest]
        public IEnumerator TEST_EntityViewBinder_엔티티_디스폰_시_view_자동_회수()
        {
            var (view, entity) = CreateRegistries();

            view.Bind(entity);

            entity.TrySpawn(out var spawned);
            Assert.AreEqual(1, view.Views.Count);

            spawned.Damage(100);
            // HP=0 → 자동 디스폰 → view 도 자동 회수

            Assert.AreEqual(0, view.Views.Count);

            view.Unbind();

            yield return null;
        }

    #endregion

    #region R-1: ReSpawnAll 재스폰

        [UnityTest]
        public IEnumerator TEST_EntityViewBinder_ReSpawnAll_동일_엔티티_재대응()
        {
            var (view, entity) = CreateRegistries();

            view.Bind(entity);
            entity.TrySpawn(out _);
            entity.TrySpawn(out _);

            Assert.AreEqual(2, view.Views.Count);

            view.ReSpawnAll();

            Assert.AreEqual(2, view.Views.Count);

            view.Unbind();

            yield return null;
        }

    #endregion

    }

}
