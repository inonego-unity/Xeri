/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TEST_MonoEntitySpawnRegistry.cs
수정일 : 2026-05-07

# 설명
MonoEntitySpawnRegistry 핵심 동작 테스트.
EntitySpawnRegistry 와 Connect 후 자동 동기화·디스폰을 검증한다.
Unity Test Runner (Play Mode) 에서 실행 — GameObject·Prefab 생성·소멸이 필요.
========================================================================= BLOCK_HEADER_END */

using System.Collections;

using UnityEngine;
using UnityEngine.TestTools;

using NUnit.Framework;

using inonego.Xeri;
using inonego.Xeri.Game;
using inonego.Xeri.Serializable;

// ============================================================
/// <summary>
/// MonoEntitySpawnRegistry 핵심 기능 테스트.
/// </summary>
// ============================================================
public class TEST_MonoEntitySpawnRegistry
{

#region 테스트용 콘크리트

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

#endregion

#region 헬퍼

    private GameObject prefab;

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

#region Connect 동기화

    // ----------------------------------------------------------------------
    /// <summary>
    /// Connect 시 이미 스폰된 엔티티에 대응하는 모노 엔티티가 자동 스폰되는지 확인.
    /// </summary>
    // ----------------------------------------------------------------------
    [UnityTest]
    public IEnumerator MonoEntitySpawnRegistry_01_Connect_시_기존_엔티티_동기화_테스트()
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

#region 자동 동기화

    // ------------------------------------------------------------
    /// <summary>
    /// Connect 후 엔티티 신규 스폰 시 모노 엔티티가 자동 스폰되는지 확인.
    /// </summary>
    // ------------------------------------------------------------
    [UnityTest]
    public IEnumerator MonoEntitySpawnRegistry_02_엔티티_스폰_시_모노엔티티_자동_스폰_테스트()
    {
        var (mono, entity) = CreateRegistries();

        mono.Connect(entity);

        entity.TrySpawn(out var spawned);

        Assert.AreEqual(1, mono.Spawned.Count);
        Assert.IsTrue(mono.Spawned.ContainsKey(spawned.Key));

        mono.Disconnect();

        yield return null;
    }

    // ------------------------------------------------------------
    /// <summary>
    /// 엔티티 디스폰 시 대응 모노 엔티티가 자동 디스폰되는지 확인.
    /// </summary>
    // ------------------------------------------------------------
    [UnityTest]
    public IEnumerator MonoEntitySpawnRegistry_03_엔티티_디스폰_시_모노엔티티_자동_디스폰_테스트()
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

#region Disconnect

    // ------------------------------------------------------------
    /// <summary>
    /// Disconnect 시 모든 모노 엔티티가 디스폰되는지 확인.
    /// </summary>
    // ------------------------------------------------------------
    [UnityTest]
    public IEnumerator MonoEntitySpawnRegistry_04_Disconnect_시_DespawnAll_테스트()
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

#region ReSpawnAll

    // ----------------------------------------------------------------------
    /// <summary>
    /// ReSpawnAll 시 모노 엔티티들이 디스폰 후 다시 동일 엔티티에 대응해 스폰되는지 확인.
    /// </summary>
    // ----------------------------------------------------------------------
    [UnityTest]
    public IEnumerator MonoEntitySpawnRegistry_05_ReSpawnAll_테스트()
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
