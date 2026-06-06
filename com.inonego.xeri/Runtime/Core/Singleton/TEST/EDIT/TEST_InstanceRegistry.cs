/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TEST_InstanceRegistry.cs
수정일 : 2026-05-08

# 설명
InstanceRegistry<T> 핵심 슬롯 로직 테스트.

# 테스트 구성
 E: 기본 기능 (Register/Current/TryCurrent/Named/Unregister/Clear)
 S: 슬롯 컨텍스트 (Scope/OpenScope/CloseScope, 중첩/혼용)
 X: 예외 처리 (null 키/미등록 슬롯/CloseScope 초과)
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

using NUnit;
using NUnit.Framework;

namespace inonego.Xeri.TEST.Core._Singleton
{

    // ============================================================
    /// <summary>
    /// <br/> InstanceRegistry 슬롯 로직의 핵심 기능 테스트 클래스.
    /// </summary>
    // ============================================================
    public class TEST_InstanceRegistry
    {

    #region 헬퍼

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

    #region E-1: Register / Current 기본

        [Test]
        public void TEST_InstanceRegistry_Register_DEFAULT_SLOT_Current_접근()
        {
            var item = new Item("Main");

            registry.Register(item);

            Assert.AreEqual(item, registry.Current);
        }

    #endregion

    #region E-2: TryCurrent

        [Test]
        public void TEST_InstanceRegistry_TryCurrent_등록전후_미등록컨텍스트()
        {
            Assert.IsFalse(registry.TryCurrent(out var none), "등록 전에는 false이어야 합니다");
            Assert.IsNull(none);

            var main = new Item("Main");
            registry.Register(main);

            Assert.IsTrue(registry.TryCurrent(out var current));
            Assert.AreSame(main, current);

            using (registry.Scope("UNKNOWN"))
            {
                Assert.IsFalse(registry.TryCurrent(out _), "미등록 슬롯 컨텍스트에서도 예외 없이 false이어야 합니다");
            }
        }

    #endregion

    #region E-3-1: Named 인덱서 — Register/Scope/Unregister 통합 흐름

        [Test]
        public void TEST_InstanceRegistry_Register_Named_Scope_Unregister_통합()
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

    #endregion

    #region E-3-2: Named.TryGet

        [Test]
        public void TEST_InstanceRegistry_Named_TryGet_등록_미등록_null키()
        {
            var test = new Item("Test");
            registry.Register("TEST", test);

            Assert.IsTrue(registry.Named.TryGet("TEST", out var got));
            Assert.AreSame(test, got);

            Assert.IsFalse(registry.Named.TryGet("NONE", out var missing));
            Assert.IsNull(missing);

            Assert.Throws<ArgumentNullException>(() => registry.Named.TryGet(null, out _));
        }

    #endregion

    #region E-3-3: Named.Contains

        [Test]
        public void TEST_InstanceRegistry_Named_Contains_등록_미등록_Unregister후()
        {
            var test = new Item("Test");

            Assert.IsFalse(registry.Named.Has("TEST"));

            registry.Register("TEST", test);
            Assert.IsTrue(registry.Named.Has("TEST"));

            registry.Unregister("TEST");
            Assert.IsFalse(registry.Named.Has("TEST"));

            Assert.Throws<ArgumentNullException>(() => registry.Named.Has(null));
        }

    #endregion

    #region E-4: Unregister(instance) — 다중 슬롯 해제

        [Test]
        public void TEST_InstanceRegistry_Unregister_instance_모든_슬롯_해제()
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

    #region E-5: Clear — 모든 슬롯 초기화

        [Test]
        public void TEST_InstanceRegistry_Clear_모든_슬롯_제거()
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

    #endregion

    #region S-1-1: Scope 중첩

        [Test]
        public void TEST_InstanceRegistry_Scope_중첩_LIFO_복원()
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

    #endregion

    #region S-1-2: 다중 Scope 선언

        [Test]
        public void TEST_InstanceRegistry_Scope_다중_using_LIFO_복원()
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

    #endregion

    #region S-2-1: OpenScope / CloseScope 기본

        [Test]
        public void TEST_InstanceRegistry_OpenScope_CloseScope_기본()
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

    #endregion

    #region S-2-2: OpenScope / CloseScope 중첩

        [Test]
        public void TEST_InstanceRegistry_OpenScope_CloseScope_중첩_LIFO()
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

    #endregion

    #region S-3: Scope() / OpenScope() 혼용

        [Test]
        public void TEST_InstanceRegistry_Scope_OpenScope_혼용_LIFO()
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

    #region X-1: null 키 예외

        [Test]
        public void TEST_InstanceRegistry_null_키_ArgumentNullException()
        {
            var item = new Item("Main");

            Assert.Throws<ArgumentNullException>(() => registry.Register(null, item));
            Assert.Throws<ArgumentNullException>(() => registry.Unregister((string)null));
            Assert.Throws<ArgumentNullException>(() => registry.Scope(null));
            Assert.Throws<ArgumentNullException>(() => { var _ = registry.Named[null]; });
        }

    #endregion

    #region X-2: 미등록 슬롯 접근 예외

        [Test]
        public void TEST_InstanceRegistry_미등록_슬롯_Current_InvalidOperationException()
        {
            Assert.Throws<InvalidOperationException>(() => { var _ = registry.Current; });

            using (registry.Scope("UNKNOWN"))
            {
                Assert.Throws<InvalidOperationException>(() => { var _ = registry.Current; });
            }
        }

    #endregion

    #region X-3: CloseScope 초과 호출 예외

        [Test]
        public void TEST_InstanceRegistry_CloseScope_초과_호출_InvalidOperationException()
        {
            Assert.Throws<InvalidOperationException>(() => registry.CloseScope());

            var item = new Item("X");
            registry.Register("X", item);

            registry.OpenScope("X");
            registry.CloseScope();

            Assert.Throws<InvalidOperationException>(() => registry.CloseScope());
        }

    #endregion

    }

}
