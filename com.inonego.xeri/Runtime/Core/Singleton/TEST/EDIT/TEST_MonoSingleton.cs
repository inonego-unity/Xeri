/* BLOCK_HEADER_BEGIN ===================================================================================
파일명 : TEST_MonoSingleton.cs
수정일 : 2026-05-08

# 설명
MonoSingleton<T> 정적 API 위임 스모크 테스트.
슬롯 로직은 InstanceRegistry<T> 가 보유하며 별도 테스트가 존재한다.

# 테스트 구성
 E: 정적 API 위임 (Register/Current/TryCurrent/Named/Scope/Clear)
 R: TryRegisterOrDestroy (슬롯 점유 / 중복 시 자기 파괴)

# 특이사항
MonoSingleton<T> 의 static 레지스트리는 T 별로 영속 상태이다.
테스트 간 격리를 위해 [SetUp] 에서 Clear() 를, [TearDown] 에서 GameObject 를 정리한다.
OnDestroy 자동 해제는 EditMode 에서 MonoBehaviour 라이프사이클이 호출되지 않아 검증 불가하므로 본 파일 범위 외이다.
===================================================================================== BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using NUnit;
using NUnit.Framework;

namespace inonego.Xeri.TEST.Core._Singleton
{

    // ============================================================
    /// <summary>
    /// MonoSingleton&lt;T&gt; 정적 API 위임 동작 테스트 클래스.
    /// </summary>
    // ============================================================
    public class TEST_MonoSingleton
    {

    #region 헬퍼

        // ============================================================
        /// <summary>
        /// 테스트 전용 MonoSingleton 파생 더미 타입.
        /// </summary>
        // ============================================================
        private class MonoSingletonItem : MonoSingleton<MonoSingletonItem>
        {
            public string Name;
        }

    #endregion

    #region 픽스처

        private readonly List<GameObject> spawned = new();

        // ------------------------------------------------------------
        /// <summary>
        /// 헬퍼 — GameObject 와 MonoSingletonItem 컴포넌트를 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        private MonoSingletonItem CreateItem(string name)
        {
            var go = new GameObject(name);
            spawned.Add(go);

            var item = go.AddComponent<MonoSingletonItem>();
            item.Name = name;
            return item;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 각 테스트 전 정적 레지스트리를 초기화한다.
        /// </summary>
        // ------------------------------------------------------------
        [SetUp]
        public void SetUp()
        {
            MonoSingleton<MonoSingletonItem>.Clear();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 각 테스트 후 생성한 GameObject 를 정리한다.
        /// </summary>
        // ------------------------------------------------------------
        [TearDown]
        public void TearDown()
        {
            foreach (var go in spawned)
            {
                if (go != null) UnityEngine.Object.DestroyImmediate(go);
            }
            spawned.Clear();

            MonoSingleton<MonoSingletonItem>.Clear();
        }

    #endregion

    #region E-1: 정적 API 위임 스모크

        [Test]
        public void TEST_MonoSingleton_정적_위임_스모크()
        {
            var main = CreateItem("Main");
            var sub  = CreateItem("Sub");

            Assert.IsFalse(MonoSingleton<MonoSingletonItem>.TryCurrent(out _), "등록 전에는 false이어야 합니다");

            MonoSingleton<MonoSingletonItem>.Register(main);
            MonoSingleton<MonoSingletonItem>.Register("SUB", sub);

            Assert.AreSame(main, MonoSingleton<MonoSingletonItem>.Current);

            Assert.IsTrue(MonoSingleton<MonoSingletonItem>.TryCurrent(out var current));
            Assert.AreSame(main, current);

            Assert.AreSame(sub, MonoSingleton<MonoSingletonItem>.Named["SUB"]);
            Assert.IsTrue(MonoSingleton<MonoSingletonItem>.Named.Has("SUB"));

            using (MonoSingleton<MonoSingletonItem>.Scope("SUB"))
            {
                Assert.AreSame(sub, MonoSingleton<MonoSingletonItem>.Current);
            }

            Assert.AreSame(main, MonoSingleton<MonoSingletonItem>.Current);
        }

    #endregion

    #region E-2: Clear 위임

        [Test]
        public void TEST_MonoSingleton_Clear_모든_슬롯_제거()
        {
            MonoSingleton<MonoSingletonItem>.Register(CreateItem("Main"));
            MonoSingleton<MonoSingletonItem>.Register("SLOT", CreateItem("Slot"));

            MonoSingleton<MonoSingletonItem>.Clear();

            Assert.IsFalse(MonoSingleton<MonoSingletonItem>.TryCurrent(out _));
            Assert.IsFalse(MonoSingleton<MonoSingletonItem>.Named.Has("SLOT"));
        }

    #endregion

    #region R-1: TryRegisterOrDestroy — 빈 슬롯에 점유 성공

        [Test]
        public void TEST_MonoSingleton_TryRegisterOrDestroy_빈_슬롯_점유_성공()
        {
            var item = CreateItem("Main");

            bool registered = MonoSingleton<MonoSingletonItem>.TryRegisterOrDestroy(item);

            Assert.IsTrue (registered, "빈 슬롯에는 점유 성공해야 합니다");
            Assert.AreSame(item, MonoSingleton<MonoSingletonItem>.Current, "Current 가 등록한 인스턴스를 반환해야 합니다");
            Assert.IsTrue (item != null, "GameObject 는 살아있어야 합니다");
        }

    #endregion

    #region R-2: TryRegisterOrDestroy — 중복 시 자기 GameObject 파괴

        [Test]
        public void TEST_MonoSingleton_TryRegisterOrDestroy_중복_시_자기_파괴()
        {
            var first  = CreateItem("First");
            var second = CreateItem("Second");
            var secondGo = second.gameObject;

            MonoSingleton<MonoSingletonItem>.TryRegisterOrDestroy(first);

            // EditMode 라서 DestroyImmediate 경로 — 즉시 파괴되어야 한다.
            bool registered = MonoSingleton<MonoSingletonItem>.TryRegisterOrDestroy(second);
            spawned.Remove(secondGo);

            Assert.IsFalse(registered, "이미 다른 인스턴스가 점유했으면 false 를 반환해야 합니다");
            Assert.IsTrue (secondGo == null, "second.gameObject 는 파괴되어 fake-null 이어야 합니다");
            Assert.AreSame(first, MonoSingleton<MonoSingletonItem>.Current, "Current 는 first 그대로여야 합니다");
        }

    #endregion

    #region R-3: TryRegisterOrDestroy — 같은 인스턴스 재등록은 통과

        [Test]
        public void TEST_MonoSingleton_TryRegisterOrDestroy_같은_인스턴스_재등록_통과()
        {
            var item = CreateItem("Main");

            bool first  = MonoSingleton<MonoSingletonItem>.TryRegisterOrDestroy(item);
            bool second = MonoSingleton<MonoSingletonItem>.TryRegisterOrDestroy(item);

            Assert.IsTrue (first,  "첫 등록 성공");
            Assert.IsTrue (second, "같은 인스턴스 재등록은 자기 파괴 없이 통과해야 합니다");
            Assert.IsTrue (item != null, "동일 인스턴스는 파괴되지 않아야 합니다");
        }

    #endregion

    }

}
