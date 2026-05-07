/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TEST_SpawnRegistry.cs
수정일 : 2026-05-07

# 설명
SpawnRegistryBase / SpawnRegistry 핵심 동작 테스트.
ISpawnRegistryObject<ulong> 단순 구현체와 SpawnRegistry<ulong, T> 콘크리트 레지스트리로 검증한다.
Unity Test Runner (Edit Mode) 에서 실행한다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections.Generic;

using NUnit.Framework;

using inonego.Xeri;
using inonego.Xeri.Game;

// ============================================================
/// <summary>
/// SpawnRegistry 핵심 기능 테스트.
/// </summary>
// ============================================================
public class TEST_SpawnRegistry
{

#region 테스트용 콘크리트

    // ------------------------------------------------------------
    /// <summary>
    /// ulong 키 기반 단순 스폰 객체.
    /// </summary>
    // ------------------------------------------------------------
    private class TestObject : ISpawnRegistryObject<ulong>
    {
        public ulong Key       { get; private set; }
        public bool  HasKey    { get; private set; }
        public bool  IsSpawned => isSpawned;

        private bool isSpawned;

        bool ISpawnRegistryObject<ulong>.IsSpawned
        {
            get => isSpawned;
            set => isSpawned = value;
        }

        Action IDespawnable.DespawnFromRegistry { get; set; }

        public void SetKey(ulong key)
        {
            Key    = key;
            HasKey = true;
        }

        public void ClearKey()
        {
            HasKey = false;
        }

        public void OnBeforeSpawn()   {}
        public void OnAfterSpawn()    {}
        public void OnBeforeDespawn() {}
        public void OnAfterDespawn()  {}
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

        public new bool      TrySpawn(out TestObject spawned)                              => base.TrySpawn(out spawned);
        public new void      Despawn(TestObject obj, bool removeFromDictionary = true)     => base.Despawn(obj, removeFromDictionary);
    }

#endregion

#region 기본 스폰 / 디스폰

    // ------------------------------------------------------------
    /// <summary>
    /// Spawn 후 IsSpawned, HasKey, Spawned dict 갱신 확인.
    /// </summary>
    // ------------------------------------------------------------
    [Test]
    public void SpawnRegistry_01_기본_스폰_테스트()
    {
        var registry = new TestRegistry();

        Assert.IsTrue(registry.TrySpawn(out var obj));

        Assert.IsTrue(obj.IsSpawned);
        Assert.IsTrue(obj.HasKey);
        Assert.AreEqual(1, registry.Spawned.Count);
        Assert.IsTrue(registry.Spawned.ContainsKey(obj.Key));
    }

    // ------------------------------------------------------------
    /// <summary>
    /// Despawn 후 상태 해제 확인.
    /// </summary>
    // ------------------------------------------------------------
    [Test]
    public void SpawnRegistry_02_디스폰_테스트()
    {
        var registry = new TestRegistry();
        registry.TrySpawn(out var obj);

        registry.Despawn(obj);

        Assert.IsFalse(obj.IsSpawned);
        Assert.AreEqual(0, registry.Spawned.Count);
    }

#endregion

#region 예외 처리

    // ------------------------------------------------------------
    /// <summary>
    /// 동일 키 중복 등록 시 예외 발생 확인.
    /// </summary>
    // ------------------------------------------------------------
    [Test]
    public void SpawnRegistry_03_중복_키_예외_테스트()
    {
        var registry = new TestRegistry();
        registry.TrySpawn(out var first);

        // 같은 키를 가진 객체를 강제로 만들어 두 번째 스폰 시도
        var duplicate = new TestObject();
        duplicate.SetKey(first.Key);

        registry.NextObject = duplicate;

        Assert.Throws<InvalidOperationException>(() => registry.TrySpawn(out _));
    }

#endregion

#region 일괄 처리

    // ------------------------------------------------------------
    /// <summary>
    /// DespawnAll 호출 시 모든 객체가 해제되는지 확인.
    /// </summary>
    // ------------------------------------------------------------
    [Test]
    public void SpawnRegistry_04_DespawnAll_테스트()
    {
        var registry = new TestRegistry();

        registry.TrySpawn(out var a);
        registry.TrySpawn(out var b);
        registry.TrySpawn(out var c);

        Assert.AreEqual(3, registry.Spawned.Count);

        registry.DespawnAll();

        Assert.AreEqual(0, registry.Spawned.Count);
        Assert.IsFalse(a.IsSpawned);
        Assert.IsFalse(b.IsSpawned);
        Assert.IsFalse(c.IsSpawned);
    }

#endregion

#region 검색

    // ------------------------------------------------------------
    /// <summary>
    /// 키 / IKeyable 양쪽으로 Find 가 동작하는지 확인.
    /// </summary>
    // ------------------------------------------------------------
    [Test]
    public void SpawnRegistry_05_Find_테스트()
    {
        var registry = new TestRegistry();
        registry.TrySpawn(out var obj);

        Assert.AreSame(obj, registry.Find(obj.Key));
        Assert.AreSame(obj, registry.Find((IKeyable<ulong>)obj));
        Assert.IsNull(registry.Find(9999UL));
    }

#endregion

#region 이벤트

    // ------------------------------------------------------------
    /// <summary>
    /// OnSpawn / OnDespawn 이벤트가 정상 발화하는지 확인.
    /// </summary>
    // ------------------------------------------------------------
    [Test]
    public void SpawnRegistry_06_이벤트_테스트()
    {
        var registry = new TestRegistry();

        ulong       spawnKey = 0;
        TestObject  spawnObj = null;
        ulong       despawnKey = 0;
        TestObject  despawnObj = null;

        registry.OnSpawn   += (k, o) => { spawnKey   = k; spawnObj   = o; };
        registry.OnDespawn += (k, o) => { despawnKey = k; despawnObj = o; };

        registry.TrySpawn(out var obj);

        Assert.AreSame(obj, spawnObj);
        Assert.AreEqual(obj.Key, spawnKey);

        registry.Despawn(obj);

        Assert.AreSame(obj, despawnObj);
        Assert.AreEqual(obj.Key, despawnKey);
    }

#endregion

#region DespawnFromRegistry 콜백

    // ----------------------------------------------------------------------
    /// <summary>
    /// 객체 자체의 Despawn() 호출 시 레지스트리가 등록한 콜백으로 디스폰되는지 확인.
    /// </summary>
    // ----------------------------------------------------------------------
    [Test]
    public void SpawnRegistry_07_DespawnFromRegistry_콜백_테스트()
    {
        var registry = new TestRegistry();
        registry.TrySpawn(out var obj);

        ((IDespawnable)obj).Despawn();

        Assert.IsFalse(obj.IsSpawned);
        Assert.AreEqual(0, registry.Spawned.Count);
    }

#endregion

}
