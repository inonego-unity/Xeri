/* BLOCK_HEADER_BEGIN ===================================================================================
파일명 : TEST_MonoSingleton.cs
수정일 : 2026-05-08

# 설명
MonoSingleton<T> 정적 API 위임 스모크 테스트.
슬롯 로직은 InstanceRegistry<T> 가 보유하며 별도 테스트가 존재한다.

# 테스트 구성
 E: 정적 API 위임 (Register/Current/TryCurrent/Named/Scope/Clear)

# 특이사항
MonoSingleton<T> 의 static 레지스트리는 T 별로 영속 상태이다.
테스트 간 격리를 위해 [SetUp] 에서 Clear() 를, [TearDown] 에서 GameObject 를 정리한다.
OnDestroy 자동 해제는 EditMode 에서 MonoBehaviour 라이프사이클이 호출되지 않아 검증 불가하므로 본 파일 범위 외이다.
===================================================================================== BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

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
            Assert.IsTrue(MonoSingleton<MonoSingletonItem>.Named.Contains("SUB"));

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
            Assert.IsFalse(MonoSingleton<MonoSingletonItem>.Named.Contains("SLOT"));
        }

    #endregion

    }

}
