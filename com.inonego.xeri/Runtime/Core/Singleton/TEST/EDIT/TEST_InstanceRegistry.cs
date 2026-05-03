/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TEST_InstanceRegistry.cs
수정일 : 2026-05-03

# 설명
InstanceRegistry<T> 핵심 슬롯 로직 테스트.
Register, Current, Named, Scope(중첩), null 키 예외, Unregister(key/instance), Clear, 예외 처리.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

using NUnit.Framework;

using inonego.Xeri;

// ============================================================
/// <summary>
/// <br/> InstanceRegistry 슬롯 로직의 핵심 기능 테스트 클래스.
/// <br/> OpenScope/CloseScope API를 포함한다.
/// </summary>
// ============================================================
public class TEST_InstanceRegistry
{

#region Helper Classes

    // ============================================================
    /// <summary>
    /// 테스트용 더미 클래스.
    /// </summary>
    // ============================================================
    private class Item
    {
        public string Name;
        public Item(string name) { Name = name; }
    }

#endregion

#region 픽스처

    private InstanceRegistry<Item> registry;

    // ------------------------------------------------------------
    /// <summary>
    /// 각 테스트 전 새로운 레지스트리 인스턴스를 생성한다.
    /// </summary>
    // ------------------------------------------------------------
    [SetUp]
    public void SetUp()
    {
        registry = new InstanceRegistry<Item>();
    }

#endregion

#region InstanceRegistry 기본 기능 테스트

    // ------------------------------------------------------------
    /// <summary>
    /// DEFAULT_SLOT 등록 및 Current 접근을 테스트한다.
    /// </summary>
    // ------------------------------------------------------------
    [Test]
    public void InstanceRegistry_01_기본_등록_테스트()
    {
        var item = new Item("Main");

        registry.Register(item);

        Assert.AreEqual(item, registry.Current);
    }

    // ------------------------------------------------------------
    /// <summary>
    /// <br/> Register, Named, Scope, Unregister를 통합 테스트한다.
    /// <br/> 슬롯 전환 후 복원, Named 직접 접근, 해제 후 예외 발생을 확인한다.
    /// </summary>
    // ------------------------------------------------------------
    [Test]
    public void InstanceRegistry_02_통합_기능_테스트()
    {
        var main = new Item("Main");
        var test = new Item("Test");

        // Register — 기본 슬롯과 명시적 슬롯
        registry.Register(main);
        registry.Register("TEST", test);

        Assert.AreEqual(main, registry.Current, "기본 슬롯은 main이어야 합니다");

        // Named — 이름으로 직접 접근
        Assert.AreEqual(test, registry.Named["TEST"]);

        // Scope — 전환 및 복원
        using (registry.Scope("TEST"))
        {
            Assert.AreEqual(test, registry.Current, "Scope 내에서는 test가 Current이어야 합니다");
        }

        Assert.AreEqual(main, registry.Current, "Scope 종료 후 main으로 복원되어야 합니다");

        // Unregister — 해제 후 Named 접근 시 예외
        registry.Unregister("TEST");
        Assert.Throws<KeyNotFoundException>(() => { var _ = registry.Named["TEST"]; });
    }

    // ------------------------------------------------------------
    /// <summary>
    /// Scope 중첩 시 진입·복원 순서를 테스트한다.
    /// </summary>
    // ------------------------------------------------------------
    [Test]
    public void InstanceRegistry_03_Scope_중첩_테스트()
    {
        var main  = new Item("Main");
        var slotA = new Item("A");
        var slotB = new Item("B");

        registry.Register(main);
        registry.Register("A", slotA);
        registry.Register("B", slotB);

        using (registry.Scope("A"))
        {
            Assert.AreEqual(slotA, registry.Current);

            using (registry.Scope("B"))
            {
                Assert.AreEqual(slotB, registry.Current);
            }

            Assert.AreEqual(slotA, registry.Current, "B 종료 후 A로 복원되어야 합니다");
        }

        Assert.AreEqual(main, registry.Current, "A 종료 후 기본 슬롯으로 복원되어야 합니다");
    }

    // ------------------------------------------------------------
    /// <summary>
    /// <br/> 한 using 블록에 여러 Scope를 선언하는 패턴을 테스트한다.
    /// <br/> 마지막 선언이 먼저 복원(LIFO)됨을 확인한다.
    /// </summary>
    // ------------------------------------------------------------
    [Test]
    public void InstanceRegistry_04_다중_Scope_선언_테스트()
    {
        var main  = new Item("Main");
        var slotA = new Item("A");
        var slotB = new Item("B");

        registry.Register(main);
        registry.Register("A", slotA);
        registry.Register("B", slotB);

        using (registry.Scope("A"))
        using (registry.Scope("B"))
        {
            Assert.AreEqual(slotB, registry.Current);
        }

        Assert.AreEqual(main, registry.Current, "모든 Scope 종료 후 기본 슬롯으로 복원되어야 합니다");
    }

    // ------------------------------------------------------------
    /// <summary>
    /// null 키 사용 시 ArgumentNullException이 발생함을 테스트한다.
    /// </summary>
    // ------------------------------------------------------------
    [Test]
    public void InstanceRegistry_05_null_키_예외_테스트()
    {
        var item = new Item("Main");

        Assert.Throws<ArgumentNullException>(() => registry.Register(null, item));
        Assert.Throws<ArgumentNullException>(() => registry.Unregister((string)null));
        Assert.Throws<ArgumentNullException>(() => registry.Scope(null));
        Assert.Throws<ArgumentNullException>(() => { var _ = registry.Named[null]; });
    }

    // ------------------------------------------------------------
    /// <summary>
    /// 미등록 슬롯 접근 시 예외 발생을 테스트한다.
    /// </summary>
    // ------------------------------------------------------------
    [Test]
    public void InstanceRegistry_06_미등록_슬롯_예외_테스트()
    {
        Assert.Throws<InvalidOperationException>(() => { var _ = registry.Current; });

        using (registry.Scope("UNKNOWN"))
        {
            Assert.Throws<InvalidOperationException>(() => { var _ = registry.Current; });
        }
    }

    // ------------------------------------------------------------
    /// <summary>
    /// Clear 후 모든 슬롯이 제거됨을 테스트한다.
    /// </summary>
    // ------------------------------------------------------------
    [Test]
    public void InstanceRegistry_07_Clear_테스트()
    {
        registry.Register(new Item("Main"));
        registry.Register("SLOT", new Item("Slot"));

        registry.Clear();

        Assert.Throws<InvalidOperationException>(() => { var _ = registry.Current; });

        using (registry.Scope("SLOT"))
        {
            Assert.Throws<InvalidOperationException>(() => { var _ = registry.Current; });
        }
    }

    // ------------------------------------------------------------
    /// <summary>
    /// <br/> 같은 인스턴스가 여러 슬롯에 등록된 경우
    /// <br/> Unregister(instance) 호출 시 모든 슬롯에서 해제됨을 테스트한다.
    /// </summary>
    // ------------------------------------------------------------
    [Test]
    public void InstanceRegistry_08_인스턴스_다중_슬롯_해제_테스트()
    {
        var item = new Item("Multi");

        registry.Register(item);
        registry.Register("A", item);
        registry.Register("B", item);

        registry.Unregister(item);

        Assert.Throws<InvalidOperationException>(() => { var _ = registry.Current; });
        Assert.Throws<KeyNotFoundException>(() => { var _ = registry.Named["A"]; });
        Assert.Throws<KeyNotFoundException>(() => { var _ = registry.Named["B"]; });
    }

#endregion

#region OpenScope / CloseScope 테스트

    // ------------------------------------------------------------
    /// <summary>
    /// OpenScope로 열고 CloseScope로 닫으면 이전 슬롯으로 복원됨을 테스트한다.
    /// </summary>
    // ------------------------------------------------------------
    [Test]
    public void InstanceRegistry_09_OpenScope_CloseScope_기본_테스트()
    {
        var main = new Item("Main");
        var test = new Item("Test");

        registry.Register(main);
        registry.Register("TEST", test);

        registry.OpenScope("TEST");

        Assert.AreEqual(test, registry.Current, "OpenScope 후 TEST가 Current이어야 합니다");

        registry.CloseScope();

        Assert.AreEqual(main, registry.Current, "CloseScope 후 main으로 복원되어야 합니다");
    }

    // ------------------------------------------------------------
    /// <summary>
    /// <br/> OpenScope 중첩 후 CloseScope LIFO 복원을 테스트한다.
    /// <br/> A 열고 B 열면, CloseScope → A, 다시 CloseScope → main.
    /// </summary>
    // ------------------------------------------------------------
    [Test]
    public void InstanceRegistry_10_OpenScope_CloseScope_중첩_테스트()
    {
        var main  = new Item("Main");
        var slotA = new Item("A");
        var slotB = new Item("B");

        registry.Register(main);
        registry.Register("A", slotA);
        registry.Register("B", slotB);

        registry.OpenScope("A");
        Assert.AreEqual(slotA, registry.Current);

        registry.OpenScope("B");
        Assert.AreEqual(slotB, registry.Current);

        registry.CloseScope();
        Assert.AreEqual(slotA, registry.Current, "B 닫기 후 A로 복원되어야 합니다");

        registry.CloseScope();
        Assert.AreEqual(main, registry.Current, "A 닫기 후 main으로 복원되어야 합니다");
    }

    // ------------------------------------------------------------
    /// <summary>
    /// 열린 스코프 없이 CloseScope 호출 시 InvalidOperationException이 발생함을 테스트한다.
    /// </summary>
    // ------------------------------------------------------------
    [Test]
    public void InstanceRegistry_11_CloseScope_초과_호출_예외_테스트()
    {
        Assert.Throws<InvalidOperationException>(() => registry.CloseScope());

        var item = new Item("X");
        registry.Register("X", item);

        registry.OpenScope("X");
        registry.CloseScope();

        Assert.Throws<InvalidOperationException>(() => registry.CloseScope());
    }

    // ------------------------------------------------------------
    /// <summary>
    /// <br/> Scope()와 OpenScope()가 같은 스택을 공유함을 테스트한다.
    /// <br/> 혼용 시에도 LIFO 복원이 보장되어야 한다.
    /// </summary>
    // ------------------------------------------------------------
    [Test]
    public void InstanceRegistry_12_Scope_OpenScope_혼용_테스트()
    {
        var main  = new Item("Main");
        var slotA = new Item("A");
        var slotB = new Item("B");

        registry.Register(main);
        registry.Register("A", slotA);
        registry.Register("B", slotB);

        registry.OpenScope("A");
        Assert.AreEqual(slotA, registry.Current);

        using (registry.Scope("B"))
        {
            Assert.AreEqual(slotB, registry.Current);
        }

        Assert.AreEqual(slotA, registry.Current, "Scope 종료 후 A로 복원되어야 합니다");

        registry.CloseScope();
        Assert.AreEqual(main, registry.Current, "CloseScope 후 main으로 복원되어야 합니다");
    }

#endregion

}
