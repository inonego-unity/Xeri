/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TEST_SpawnRegistry.cs
수정일 : 2026-07-29

# 설명
SpawnRegistryBase / SpawnRegistry 핵심 동작 테스트.
ISpawnRegistryObject<ulong> 단순 구현체와 SpawnRegistry<ulong, T> 콘크리트 레지스트리로 검증한다.
Unity Test Runner (Edit Mode) 에서 실행한다.

# 테스트 구성
 E: 기본 기능 (스폰/디스폰/DespawnAll/Find)
 V: 이벤트 / 콜백 (스폰 상태 전환 / DespawnFromRegistry)
 X: 예외 처리 (중복 키)
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

using NUnit;
using NUnit.Framework;

namespace inonego.Xeri.TEST.Game._Spawn
{

    using inonego.Xeri.Game;

    // ============================================================
    /// <summary>
    /// SpawnRegistry 핵심 기능 테스트.
    /// </summary>
    // ============================================================
    public class TEST_SpawnRegistry
    {

    #region 헬퍼

        // ------------------------------------------------------------
        /// <summary>
        /// ulong 키 기반 단순 스폰 객체.
        /// </summary>
        // ------------------------------------------------------------
        private class TestObject : ISpawnRegistryObject<ulong>
        {
            public ulong      Key        { get; private set; }
            public bool       HasKey     { get; private set; }
            public SpawnState SpawnState => spawnState;

            private SpawnState spawnState = SpawnState.Despawned;

            SpawnState ISpawnRegistryObject<ulong>.SpawnState
            {
                get => spawnState;
                set => spawnState = value;
            }

            Action<DespawnReason> IDespawnable.DespawnFromRegistry { get; set; }

            public void SetKey(ulong key)
            {
                Key    = key;
                HasKey = true;
            }

            public void ClearKey()
            {
                HasKey = false;
            }

            void ISpawnable.OnSpawning() {}
            void ISpawnable.OnSpawned() {}
            void IDespawnable.OnDespawning(DespawnReason reason) {}
            void IDespawnable.OnDespawned(DespawnReason reason) {}
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 키 자동 부여 + 외부 인스턴스 주입이 가능한 테스트 레지스트리.
        /// </summary>
        // ------------------------------------------------------------
        private class TestRegistry : SpawnRegistry<ulong, TestObject>
        {
            private ulong nextKey = 0;

            public TestObject NextObject { get; set; }

            protected override TestObject Acquire()
            {
                // 외부 주입 객체는 키를 그대로 사용 — 중복 키 시나리오 등에서 미리 설정한 키 보존.
                if (NextObject != null)
                {
                    var injected = NextObject;
                    NextObject = null;

                    return injected;
                }

                var obj = new TestObject();
                obj.SetKey(nextKey++);

                return obj;
            }

            public new bool TrySpawn(out TestObject spawned) => base.TrySpawn(out spawned);

            public void Despawn(TestObject obj)
            {
                base.Despawn(obj, DespawnReason.Removed);
            }
        }

    #endregion

    #region E-1: 기본 스폰

        [Test]
        public void TEST_SpawnRegistry_TrySpawn_상태_및_딕셔너리_갱신()
        {
            var registry = new TestRegistry();

            Assert.IsTrue(registry.TrySpawn(out var obj));

            Assert.AreEqual(SpawnState.Spawned, obj.SpawnState);
            Assert.IsTrue(obj.HasKey);
            Assert.AreEqual(1, registry.Spawned.Count);
            Assert.IsTrue(registry.Spawned.ContainsKey(obj.Key));
        }

    #endregion

    #region E-2: 디스폰

        [Test]
        public void TEST_SpawnRegistry_Despawn_상태_해제()
        {
            var registry = new TestRegistry();
            registry.TrySpawn(out var obj);

            registry.Despawn(obj);

            Assert.AreEqual(SpawnState.Despawned, obj.SpawnState);
            Assert.AreEqual(0, registry.Spawned.Count);
        }

    #endregion

    #region E-3: DespawnAll 일괄 해제

        [Test]
        public void TEST_SpawnRegistry_DespawnAll_모든_객체_해제()
        {
            var registry = new TestRegistry();

            registry.TrySpawn(out var a);
            registry.TrySpawn(out var b);
            registry.TrySpawn(out var c);

            Assert.AreEqual(3, registry.Spawned.Count);

            registry.DespawnAll();

            Assert.AreEqual(0, registry.Spawned.Count);
            Assert.AreEqual(SpawnState.Despawned, a.SpawnState);
            Assert.AreEqual(SpawnState.Despawned, b.SpawnState);
            Assert.AreEqual(SpawnState.Despawned, c.SpawnState);
        }

    #endregion

    #region E-4: Find 검색

        [Test]
        public void TEST_SpawnRegistry_Find_키_및_IKeyable_조회()
        {
            var registry = new TestRegistry();
            registry.TrySpawn(out var obj);

            Assert.AreSame(obj, registry.Find(obj.Key));
            Assert.AreSame(obj, registry.Find((IKeyable<ulong>)obj));
            Assert.IsNull(registry.Find(9999UL));
        }

    #endregion

    #region V-1: 스폰 상태 전환 이벤트

        [Test]
        public void TEST_SpawnRegistry_스폰_상태_전환_이벤트_발화()
        {
            var registry = new TestRegistry();

            ulong spawnKey = 0;
            TestObject spawnObj = null;
            SpawnState spawningState = SpawnState.Despawned;
            SpawnState spawnedState = SpawnState.Despawned;
            SpawnState despawningState = SpawnState.Despawned;
            SpawnState despawnedState = SpawnState.Spawned;
            DespawnReason despawningReason = default;
            DespawnReason despawnedReason = default;

            registry.OnSpawning += (key, obj) =>
            {
                spawnKey = key;
                spawnObj = obj;
                spawningState = obj.SpawnState;
            };
            registry.OnSpawned += (_, obj) => spawnedState = obj.SpawnState;
            registry.OnDespawning += (_, obj, reason) =>
            {
                despawningState = obj.SpawnState;
                despawningReason = reason;
            };
            registry.OnDespawned += (_, obj, reason) =>
            {
                despawnedState = obj.SpawnState;
                despawnedReason = reason;
            };

            registry.TrySpawn(out var obj);

            Assert.AreSame(obj, spawnObj);
            Assert.AreEqual(obj.Key, spawnKey);
            Assert.AreEqual(SpawnState.Spawning, spawningState);
            Assert.AreEqual(SpawnState.Spawned, spawnedState);

            registry.Despawn(obj);

            Assert.AreEqual(SpawnState.Despawning, despawningState);
            Assert.AreEqual(SpawnState.Despawned, despawnedState);
            Assert.AreEqual(DespawnReason.Removed, despawningReason);
            Assert.AreEqual(DespawnReason.Removed, despawnedReason);
        }

    #endregion

    #region V-2: DespawnFromRegistry 콜백

        [Test]
        public void TEST_SpawnRegistry_DespawnFromRegistry_콜백_역방향_디스폰()
        {
            var registry = new TestRegistry();
            registry.TrySpawn(out var obj);

            ((IDespawnable)obj).Despawn();

            Assert.AreEqual(SpawnState.Despawned, obj.SpawnState);
            Assert.AreEqual(0, registry.Spawned.Count);
        }

    #endregion

    #region X-1: 중복 키 예외

        [Test]
        public void TEST_SpawnRegistry_중복_키_등록_InvalidOperationException()
        {
            var registry = new TestRegistry();
            registry.TrySpawn(out var first);

            // 같은 키를 가진 객체를 강제로 만들어 두 번째 스폰 시도
            var duplicate = new TestObject();
            duplicate.SetKey(first.Key);

            registry.NextObject = duplicate;

            Assert.Throws<InvalidOperationException>(() => registry.TrySpawn(out _));
            Assert.AreEqual(SpawnState.Despawned, duplicate.SpawnState);
            Assert.AreEqual(1, registry.Spawned.Count);
            Assert.AreSame(first, registry.Find(first.Key));
        }

    #endregion

    }

}
