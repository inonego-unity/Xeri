/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TEST_DataPackage.cs
수정일 : 2026-05-03

# 설명
DataPackage 슬롯 시스템, 테이블 CRUD, REF 핵심 기능 테스트.
Register/Current/TryCurrent/Named/Scope/Clear/OnChange, 테이블 추가·읽기·제거, REF.ToValue().
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

using NUnit.Framework;

using UnityEngine;

using inonego.Xeri;

// ============================================================
/// <summary>
/// DataPackage 및 REF 핵심 기능 테스트 클래스.
/// </summary>
// ============================================================
public class TEST_DataPackage
{

#region Helper Classes

    // ============================================================
    /// <summary>
    /// 테스트용 데이터 클래스.
    /// </summary>
    // ============================================================
    [Serializable]
    private class TestData : ITableValue
    {
        [SerializeField] private string key;
        public string Key      { get => key; set => key = value; }
        public bool   HasKey   => !string.IsNullOrEmpty(key);
        public int    Value;
    }

    // ============================================================
    /// <summary>
    /// 테스트용 데이터 클래스 2.
    /// </summary>
    // ============================================================
    [Serializable]
    private class TestData2 : ITableValue
    {
        [SerializeField] private string key;
        public string Key    { get => key; set => key = value; }
        public bool   HasKey => !string.IsNullOrEmpty(key);
        public string Name;
    }

    [Serializable] private class TestTable  : Table_V<TestData>  {}
    [Serializable] private class TestTable2 : Table_V<TestData2> {}

#endregion

#region 픽스처

    // ------------------------------------------------------------
    /// <summary>
    /// 각 테스트 전에 슬롯과 이벤트 구독을 초기화한다.
    /// </summary>
    // ------------------------------------------------------------
    [SetUp]
    public void SetUp()
    {
        DataPackage.Clear();
    }

    // ------------------------------------------------------------
    /// <summary>
    /// 각 테스트 후에 슬롯을 정리한다.
    /// </summary>
    // ------------------------------------------------------------
    [TearDown]
    public void TearDown()
    {
        DataPackage.Clear();
    }

#endregion

#region 테이블 기본 기능 테스트

    // ------------------------------------------------------------
    /// <summary>
    /// DataPackage 기본 생성 및 등록 전 상태를 테스트한다.
    /// </summary>
    // ------------------------------------------------------------
    [Test]
    public void DataPackage_01_기본_생성_테스트()
    {
        var package = new DataPackage();

        Assert.IsNotNull(package);
        Assert.IsFalse(DataPackage.TryCurrent(out _), "Register 전에는 TryCurrent가 false이어야 합니다");
    }

    // ------------------------------------------------------------
    /// <summary>
    /// 테이블 추가 및 중복 추가 예외를 테스트한다.
    /// </summary>
    // ------------------------------------------------------------
    [Test]
    public void DataPackage_02_테이블_추가_테스트()
    {
        var package = new DataPackage();
        var table   = new TestTable();

        table.Dictionary.Add("key1", new TestData { Key = "key1", Value = 100 });
        table.Dictionary.Add("key2", new TestData { Key = "key2", Value = 200 });

        package.AddTable<TestTable, TestData>(table);

        Assert.AreEqual(100, package.Read<TestData>("key1").Value);
        Assert.AreEqual(200, package.Read<TestData>("key2").Value);

        Assert.Throws<InvalidOperationException>
        (
            () => package.AddTable<TestTable, TestData>(new TestTable()),
            "중복 추가 시 예외가 발생해야 합니다"
        );
    }

    // ------------------------------------------------------------
    /// <summary>
    /// Read / TryRead 및 없는 키·없는 테이블 동작을 테스트한다.
    /// </summary>
    // ------------------------------------------------------------
    [Test]
    public void DataPackage_03_테이블_읽기_테스트()
    {
        var package = new DataPackage();
        var table   = new TestTable();

        table.Dictionary.Add("key1", new TestData { Key = "key1", Value = 100 });
        package.AddTable<TestTable, TestData>(table);

        // 정상 읽기
        Assert.AreEqual(100, package.Read<TestData>("key1").Value);

        // 없는 키 → null 반환
        Assert.IsNull(package.Read<TestData>("none"), "없는 키는 null이어야 합니다");

        // TryRead — 테이블 없으면 null
        Assert.IsNull(package.TryRead<TestData2>("key1"), "없는 테이블은 null이어야 합니다");

        // 테이블 제거 후 Read → 예외
        package.RemoveTable<TestData>();
        Assert.Throws<InvalidOperationException>(() => package.Read<TestData>("key1"));
    }

    // ------------------------------------------------------------
    /// <summary>
    /// 테이블 제거 및 이중 제거 예외를 테스트한다.
    /// </summary>
    // ------------------------------------------------------------
    [Test]
    public void DataPackage_04_테이블_제거_테스트()
    {
        var package = new DataPackage();
        var table   = new TestTable();

        table.Dictionary.Add("key1", new TestData { Key = "key1", Value = 100 });
        package.AddTable<TestTable, TestData>(table);

        package.RemoveTable<TestData>();

        Assert.Throws<InvalidOperationException>(() => package.Read<TestData>("key1"));
        Assert.Throws<InvalidOperationException>
        (
            () => package.RemoveTable<TestData>(),
            "없는 테이블 제거 시 예외가 발생해야 합니다"
        );
    }

    // ------------------------------------------------------------
    /// <summary>
    /// 다중 타입 테이블을 동시에 관리함을 테스트한다.
    /// </summary>
    // ------------------------------------------------------------
    [Test]
    public void DataPackage_05_다중_타입_테스트()
    {
        var package = new DataPackage();
        var table1  = new TestTable();
        var table2  = new TestTable2();

        table1.Dictionary.Add("d1", new TestData  { Key = "d1", Value = 99   });
        table2.Dictionary.Add("d2", new TestData2 { Key = "d2", Name  = "검"  });

        package.AddTable<TestTable,  TestData> (table1);
        package.AddTable<TestTable2, TestData2>(table2);

        Assert.AreEqual(99,  package.Read<TestData> ("d1").Value);
        Assert.AreEqual("검", package.Read<TestData2>("d2").Name);
    }

#endregion

#region 슬롯 시스템 테스트

    // ------------------------------------------------------------
    /// <summary>
    /// Register / TryCurrent / Current / Unregister 기본 흐름을 테스트한다.
    /// </summary>
    // ------------------------------------------------------------
    [Test]
    public void DataPackage_06_Register_Unregister_테스트()
    {
        var package = new DataPackage();

        // 등록 전 — TryCurrent false
        Assert.IsFalse(DataPackage.TryCurrent(out _));

        DataPackage.Register(package);

        Assert.IsTrue(DataPackage.TryCurrent(out var current));
        Assert.AreSame(package, current);

        DataPackage.Unregister(InstanceRegistry.DEFAULT_SLOT);

        Assert.IsFalse(DataPackage.TryCurrent(out _), "Unregister 후 TryCurrent가 false이어야 합니다");
    }

    // ------------------------------------------------------------
    /// <summary>
    /// <br/> 슬롯 전환(Scope) 및 복원을 테스트한다.
    /// <br/> Dispose 후 이전 슬롯으로 자동 복원됨을 확인한다.
    /// </summary>
    // ------------------------------------------------------------
    [Test]
    public void DataPackage_07_Scope_테스트()
    {
        var main = new DataPackage();
        var sub  = new DataPackage();

        DataPackage.Register(main);
        DataPackage.Register("SUB", sub);

        Assert.AreSame(main, DataPackage.Current, "기본 슬롯은 main이어야 합니다");

        using (DataPackage.Scope("SUB"))
        {
            Assert.AreSame(sub, DataPackage.Current, "Scope 내에서는 sub가 Current이어야 합니다");
        }

        Assert.AreSame(main, DataPackage.Current, "Scope 종료 후 main으로 복원되어야 합니다");
    }

    // ------------------------------------------------------------
    /// <summary>
    /// Scope 중첩 시 진입·복원 순서(LIFO)를 테스트한다.
    /// </summary>
    // ------------------------------------------------------------
    [Test]
    public void DataPackage_08_Scope_중첩_테스트()
    {
        var main  = new DataPackage();
        var slotA = new DataPackage();
        var slotB = new DataPackage();

        DataPackage.Register(main);
        DataPackage.Register("A", slotA);
        DataPackage.Register("B", slotB);

        using (DataPackage.Scope("A"))
        {
            Assert.AreSame(slotA, DataPackage.Current);

            using (DataPackage.Scope("B"))
            {
                Assert.AreSame(slotB, DataPackage.Current);
            }

            Assert.AreSame(slotA, DataPackage.Current, "B 종료 후 A로 복원되어야 합니다");
        }

        Assert.AreSame(main, DataPackage.Current, "A 종료 후 기본 슬롯으로 복원되어야 합니다");
    }

    // ------------------------------------------------------------
    /// <summary>
    /// Named 접근자로 슬롯을 직접 조회함을 테스트한다.
    /// </summary>
    // ------------------------------------------------------------
    [Test]
    public void DataPackage_09_Named_테스트()
    {
        var main = new DataPackage();
        var sub  = new DataPackage();

        DataPackage.Register(main);
        DataPackage.Register("SUB", sub);

        Assert.AreSame(main, DataPackage.Named[InstanceRegistry.DEFAULT_SLOT]);
        Assert.AreSame(sub,  DataPackage.Named["SUB"]);
    }

    // ------------------------------------------------------------
    /// <summary>
    /// Clear 후 모든 슬롯이 제거됨을 테스트한다.
    /// </summary>
    // ------------------------------------------------------------
    [Test]
    public void DataPackage_10_Clear_테스트()
    {
        DataPackage.Register(new DataPackage());
        DataPackage.Register("SUB", new DataPackage());

        DataPackage.Clear();

        Assert.IsFalse(DataPackage.TryCurrent(out _), "Clear 후 TryCurrent가 false이어야 합니다");
        Assert.Throws<KeyNotFoundException>(() => { var _ = DataPackage.Named["SUB"]; });
    }

    // ------------------------------------------------------------
    /// <summary>
    /// <br/> OnChange 이벤트가 Register·Unregister·Clear 시 발생하고
    /// <br/> Scope 전환 시에는 발생하지 않음을 테스트한다.
    /// </summary>
    // ------------------------------------------------------------
    [Test]
    public void DataPackage_11_OnChange_이벤트_테스트()
    {
        var count = 0;
        DataPackage.OnChange += () => count++;

        try
        {
            var pkg = new DataPackage();

            DataPackage.Register(pkg);                          // +1
            DataPackage.Register("SUB", new DataPackage());     // +1

            using (DataPackage.Scope("SUB"))
            {
                // Scope는 이벤트 발생 안 함
            }

            DataPackage.Unregister("SUB");                      // +1
            DataPackage.Clear();                                // +1

            Assert.AreEqual(4, count, "Register×2, Unregister×1, Clear×1 = 4회이어야 합니다");
        }
        finally
        {
            DataPackage.OnChange -= () => count++;
            DataPackage.Clear();
        }
    }

#endregion

#region REF 테스트

    // ------------------------------------------------------------
    /// <summary>
    /// REF.ToValue()가 현재 슬롯의 DataPackage에서 값을 조회함을 테스트한다.
    /// </summary>
    // ------------------------------------------------------------
    [Test]
    public void DataPackage_12_REF_ToValue_테스트()
    {
        var package = new DataPackage();
        var table   = new TestTable();

        table.Dictionary.Add("hero", new TestData { Key = "hero", Value = 500 });
        package.AddTable<TestTable, TestData>(table);

        DataPackage.Register(package);

        var ref1 = new REF<TestData>("hero");
        var ref2 = new REF<TestData>("none");

        Assert.AreEqual(500,  ref1.ToValue().Value, "등록된 키는 값을 반환해야 합니다");
        Assert.IsNull(ref2.ToValue(),               "없는 키는 null을 반환해야 합니다");
    }

    // ------------------------------------------------------------
    /// <summary>
    /// DataPackage 미등록 시 REF.ToValue()가 null을 반환함을 테스트한다.
    /// </summary>
    // ------------------------------------------------------------
    [Test]
    public void DataPackage_13_REF_미등록_슬롯_테스트()
    {
        var ref1 = new REF<TestData>("hero");

        Assert.IsNull(ref1.ToValue(), "DataPackage 미등록 시 null을 반환해야 합니다");
    }

    // ------------------------------------------------------------
    /// <summary>
    /// REF의 Key 프로퍼티와 HasKey를 테스트한다.
    /// </summary>
    // ------------------------------------------------------------
    [Test]
    public void DataPackage_14_REF_Key_HasKey_테스트()
    {
        var ref1 = new REF<TestData>("hero");

        string key = ref1.Key;

        Assert.AreEqual("hero", key,       "Key가 생성자에 전달한 값을 반환해야 합니다");
        Assert.IsTrue(ref1.HasKey,         "키가 있으므로 HasKey가 true이어야 합니다");
        Assert.IsFalse(default(REF<TestData>).HasKey, "빈 REF의 HasKey는 false이어야 합니다");
    }

#endregion

#region 통합 테스트

    // ------------------------------------------------------------
    /// <summary>
    /// <br/> 다중 슬롯 전환과 REF 조회를 포함한 전체 시나리오를 테스트한다.
    /// <br/> 슬롯별로 다른 테이블이 로드된 상황에서 Current와 REF가 올바르게 동작함을 확인한다.
    /// </summary>
    // ------------------------------------------------------------
    [Test]
    public void DataPackage_15_통합_시나리오_테스트()
    {
        // 스테이지 A 패키지
        var pkgA  = new DataPackage();
        var tblA  = new TestTable();
        tblA.Dictionary.Add("boss", new TestData { Key = "boss", Value = 9999 });
        pkgA.AddTable<TestTable, TestData>(tblA);

        // 스테이지 B 패키지
        var pkgB  = new DataPackage();
        var tblB  = new TestTable();
        tblB.Dictionary.Add("boss", new TestData { Key = "boss", Value = 1234 });
        pkgB.AddTable<TestTable, TestData>(tblB);

        DataPackage.Register("A", pkgA);
        DataPackage.Register("B", pkgB);

        var bossRef = new REF<TestData>("boss");

        using (DataPackage.Scope("A"))
        {
            Assert.AreEqual(9999, bossRef.ToValue().Value, "슬롯 A에서는 9999이어야 합니다");
        }

        using (DataPackage.Scope("B"))
        {
            Assert.AreEqual(1234, bossRef.ToValue().Value, "슬롯 B에서는 1234이어야 합니다");
        }

        // Scope 종료 후 DEFAULT_SLOT 미등록 → null
        Assert.IsNull(bossRef.ToValue(), "기본 슬롯 미등록 시 null이어야 합니다");
    }

#endregion

}
