/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TEST_Entity.cs
수정일 : 2026-05-07

# 설명
Entity 추상 클래스 유닛 테스트.
TestEntity / TestRegistry 콘크리트를 파일 내부에 정의해 추상 동작을 검증한다.
Unity Test Runner (Edit Mode) 에서 실행한다.
========================================================================= BLOCK_HEADER_END */

using System;

using NUnit.Framework;

using inonego.Xeri;
using inonego.Xeri.Game;
using inonego.Xeri.Serializable;

// ============================================================
/// <summary>
/// Entity 핵심 기능 테스트.
/// </summary>
// ============================================================
public class TEST_Entity
{

#region 테스트용 콘크리트

    // ------------------------------------------------------------
    /// <summary>
    /// HP_I 를 주입받아 int 데미지/힐을 적용 가능한 테스트 엔티티.
    /// </summary>
    // ------------------------------------------------------------
    private class TestEntity : Entity
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
        public override void CloneFrom(Entity source)
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

#region 기본 기능

    // ------------------------------------------------------------
    /// <summary>
    /// 기본 생성 시 HP·Group 인스턴스 보유, IsSpawned=false, HasKey=false 확인.
    /// </summary>
    // ------------------------------------------------------------
    [Test]
    public void Entity_01_기본_생성_테스트()
    {
        var entity = new TestEntity();

        Assert.IsNotNull(entity.HP);
        Assert.IsNotNull(entity.Group);
        Assert.IsFalse(entity.IsSpawned);
        Assert.IsFalse(entity.HasKey);
    }

    // ------------------------------------------------------------
    /// <summary>
    /// HP 사망/생존 상태가 IReadOnlyHP 로 노출되는지 확인.
    /// </summary>
    // ------------------------------------------------------------
    [Test]
    public void Entity_02_HP_상태_노출_테스트()
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

#region 자동 디스폰

    // ------------------------------------------------------------
    /// <summary>
    /// 레지스트리 등록 후 HP 사망 시 자동 디스폰되는지 확인.
    /// </summary>
    // ------------------------------------------------------------
    [Test]
    public void Entity_03_HP_사망_시_자동_디스폰_테스트()
    {
        var registry = new TestRegistry();
        var entity   = new TestEntity();

        registry.NextEntity = entity;
        Assert.IsTrue(registry.TrySpawnPublic(out _));
        Assert.IsTrue(entity.IsSpawned);
        Assert.IsTrue(entity.HasKey);

        entity.Damage(100);

        Assert.IsTrue(entity.HP.IsDead);
        Assert.IsFalse(entity.IsSpawned);
        Assert.IsFalse(entity.HasKey);
    }

    // ----------------------------------------------------------------------
    /// <summary>
    /// MakeDeadOnDespawn=false 면 디스폰해도 HP 가 살아있는 상태로 남는지 확인.
    /// </summary>
    // ----------------------------------------------------------------------
    [Test]
    public void Entity_04_MakeDeadOnDespawn_테스트()
    {
        var registry = new TestRegistry();
        var entity   = new TestEntity();
        entity.MakeDeadOnDespawn = false;

        registry.NextEntity = entity;
        Assert.IsTrue(registry.TrySpawnPublic(out _));
        Assert.IsTrue(entity.HP.IsAlive);

        entity.Despawn();

        Assert.IsFalse(entity.IsSpawned);
        Assert.IsTrue(entity.HP.IsAlive);
    }

#endregion

#region 깊은 복사

    // ------------------------------------------------------------
    /// <summary>
    /// CloneFrom 으로 키가 복사되고 자식이 Group 깊은 복제하는지 확인.
    /// </summary>
    // ------------------------------------------------------------
    [Test]
    public void Entity_05_복제_테스트()
    {
        var src = new TestEntity();
        src.InternalGroup.Base = 4;

        var clone = new TestEntity();
        clone.CloneFrom(src);

        Assert.AreEqual(src.Group.Base, clone.Group.Base);
        Assert.AreNotSame(src.Group, clone.Group);

        // CloneFrom 직후엔 IsSpawned 항상 false
        Assert.IsFalse(clone.IsSpawned);
    }

#endregion

#region 키 자동 처리

    // ------------------------------------------------------------
    /// <summary>
    /// 스폰 시 HasKey=true, 디스폰 시 HasKey=false 가 되는지 확인.
    /// </summary>
    // ------------------------------------------------------------
    [Test]
    public void Entity_06_키_자동_처리_테스트()
    {
        var registry = new TestRegistry();
        var entity   = new TestEntity();

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

#region IReadOnlyEntity 노출

    // ------------------------------------------------------------------------------
    /// <summary>
    /// IReadOnlyEntity 캐스트 시 IReadOnlyHP·IReadOnlyValue<int> 만 노출되는지 확인.
    /// </summary>
    // ------------------------------------------------------------------------------
    [Test]
    public void Entity_07_IReadOnlyEntity_노출_테스트()
    {
        var entity = new TestEntity();
        entity.InternalGroup.Base = 9;
        entity.InternalHP.MakeAlive();

        IReadOnlyEntity readOnly = entity;

        Assert.AreEqual(9, readOnly.Group.Base);
        Assert.IsTrue(readOnly.HP.IsAlive);
        Assert.IsFalse(readOnly.IsSpawned);
        Assert.IsFalse(readOnly.HasKey);
    }

#endregion

}
