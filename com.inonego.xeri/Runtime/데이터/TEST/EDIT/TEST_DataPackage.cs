/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TEST_DataPackage.cs
수정일 : 2026-05-08

# 설명
DataPackage 슬롯 시스템, 테이블 CRUD, REF 핵심 기능 테스트.

# 테스트 구성
 T: 테이블 CRUD (생성/추가/읽기/제거/다중 타입)
 S: 슬롯 시스템 (Register/Unregister/Scope/Named/Clear/OnChange)
 R: REF (ToValue/Key/HasKey)
 I: 통합 시나리오
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

using NUnit.Framework;

using UnityEngine;

namespace inonego.Xeri.TEST._DataPackage
{

    // ============================================================
    /// <summary>
    /// DataPackage 및 REF 핵심 기능 테스트 클래스.
    /// </summary>
    // ============================================================
    public class TEST_DataPackage
    {

    #region 헬퍼

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
        /// 각 테스트 전에 정적 슬롯 레지스트리를 초기화한다.
        /// </summary>
        // ------------------------------------------------------------
        [SetUp]
        public void SetUp()
        {
            DataPackage.Clear();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 각 테스트 후에 정적 슬롯 레지스트리를 정리한다.
        /// </summary>
        // ------------------------------------------------------------
        [TearDown]
        public void TearDown()
        {
            DataPackage.Clear();
        }

    #endregion

    #region T-1: 기본 생성

        [Test]
        public void TEST_DataPackage_기본_생성()
        {
            var package = new DataPackage();

            Assert.IsNotNull(package);
            Assert.IsFalse(DataPackage.TryCurrent(out _), "Register 전에는 TryCurrent가 false이어야 합니다");
        }

    #endregion

    #region T-2: 테이블 추가

        [Test]
        public void TEST_DataPackage_AddTable_등록_및_중복_예외()
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

    #endregion

    #region T-3: 테이블 읽기

        [Test]
        public void TEST_DataPackage_Read_TryRead_기본_및_미등록()
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

    #endregion

    #region T-4: 테이블 제거

        [Test]
        public void TEST_DataPackage_RemoveTable_제거_및_이중_제거_예외()
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

    #endregion

    #region T-5: 다중 타입 테이블

        [Test]
        public void TEST_DataPackage_다중_타입_테이블_동시_관리()
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

    #region S-1: Register / Unregister

        [Test]
        public void TEST_DataPackage_Register_Unregister_기본()
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

    #endregion

    #region S-2-1: Scope 기본 전환

        [Test]
        public void TEST_DataPackage_Scope_전환_및_복원()
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

    #endregion

    #region S-2-2: Scope 중첩

        [Test]
        public void TEST_DataPackage_Scope_중첩_LIFO_복원()
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

    #endregion

    #region S-3: Named 인덱서

        [Test]
        public void TEST_DataPackage_Named_인덱서_접근()
        {
            var main = new DataPackage();
            var sub  = new DataPackage();

            DataPackage.Register(main);
            DataPackage.Register("SUB", sub);

            Assert.AreSame(main, DataPackage.Named[InstanceRegistry.DEFAULT_SLOT]);
            Assert.AreSame(sub,  DataPackage.Named["SUB"]);
        }

    #endregion

    #region S-4: Clear

        [Test]
        public void TEST_DataPackage_Clear_모든_슬롯_제거()
        {
            DataPackage.Register(new DataPackage());
            DataPackage.Register("SUB", new DataPackage());

            DataPackage.Clear();

            Assert.IsFalse(DataPackage.TryCurrent(out _), "Clear 후 TryCurrent가 false이어야 합니다");
            Assert.Throws<KeyNotFoundException>(() => { var _ = DataPackage.Named["SUB"]; });
        }

    #endregion

    #region S-5: OnChange 이벤트

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> OnChange 이벤트가 Register·Unregister·Clear 시 발생하고
        /// <br/> Scope 전환 시에는 발생하지 않음을 검증한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_DataPackage_OnChange_Register_Unregister_Clear()
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

    #region R-1: REF.ToValue 기본

        [Test]
        public void TEST_DataPackage_REF_ToValue_등록키_미등록키()
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

    #endregion

    #region R-2: REF.ToValue 미등록 슬롯

        [Test]
        public void TEST_DataPackage_REF_ToValue_미등록_슬롯_null()
        {
            var ref1 = new REF<TestData>("hero");

            Assert.IsNull(ref1.ToValue(), "DataPackage 미등록 시 null을 반환해야 합니다");
        }

    #endregion

    #region R-3: REF.Key / HasKey

        [Test]
        public void TEST_DataPackage_REF_Key_HasKey()
        {
            var ref1 = new REF<TestData>("hero");

            string key = ref1.Key;

            Assert.AreEqual("hero", key,       "Key가 생성자에 전달한 값을 반환해야 합니다");
            Assert.IsTrue(ref1.HasKey,         "키가 있으므로 HasKey가 true이어야 합니다");
            Assert.IsFalse(default(REF<TestData>).HasKey, "빈 REF의 HasKey는 false이어야 합니다");
        }

    #endregion

    #region I-1: 다중 슬롯 + REF 통합

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> 다중 슬롯 전환과 REF 조회를 포함한 전체 시나리오를 검증한다.
        /// <br/> 슬롯별로 다른 테이블이 로드된 상황에서 Current와 REF가 올바르게 동작함을 확인한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_DataPackage_다중_슬롯_REF_통합_시나리오()
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

}
