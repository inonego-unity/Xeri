/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TEST_EntitySpawnRegistry.cs
수정일 : 2026-05-07

# 설명
EntitySpawnRegistry 핵심 동작 테스트.
키 자동 생성·해제, HP 연동 자동 디스폰, 복제를 검증한다.
Unity Test Runner (Edit Mode) 에서 실행한다.
========================================================================= BLOCK_HEADER_END */

using NUnit.Framework;

using inonego.Xeri;
using inonego.Xeri.Game;
using inonego.Xeri.Serializable;

// ============================================================
/// <summary>
/// EntitySpawnRegistry 핵심 기능 테스트.
/// </summary>
// ============================================================
public class TEST_EntitySpawnRegistry
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

#region 키 자동 처리

    // ------------------------------------------------------------
    /// <summary>
    /// IncreKeyGenerator 로 키가 0,1,2 순으로 부여되는지 확인.
    /// </summary>
    // ------------------------------------------------------------
    [Test]
    public void EntitySpawnRegistry_01_키_자동_생성_테스트()
    {
        var registry = new TestRegistry();

        registry.TrySpawn(out var a);
        registry.TrySpawn(out var b);
        registry.TrySpawn(out var c);

        Assert.AreEqual(0UL, a.Key);
        Assert.AreEqual(1UL, b.Key);
        Assert.AreEqual(2UL, c.Key);
    }

    // ------------------------------------------------------------
    /// <summary>
    /// 디스폰 시 키가 클리어되는지 확인.
    /// </summary>
    // ------------------------------------------------------------
    [Test]
    public void EntitySpawnRegistry_02_디스폰_시_키_클리어_테스트()
    {
        var registry = new TestRegistry();

        registry.TrySpawn(out var entity);
        Assert.IsTrue(entity.HasKey);

        entity.Despawn();

        Assert.IsFalse(entity.HasKey);
        Assert.IsFalse(entity.IsSpawned);
    }

#endregion

#region HP 연동

    // ------------------------------------------------------------
    /// <summary>
    /// HP 가 사망(0)으로 떨어지면 자동으로 디스폰되는지 확인.
    /// </summary>
    // ------------------------------------------------------------
    [Test]
    public void EntitySpawnRegistry_03_HP_연동_자동_디스폰_테스트()
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
