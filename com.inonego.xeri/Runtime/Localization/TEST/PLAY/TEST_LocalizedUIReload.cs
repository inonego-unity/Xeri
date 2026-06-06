/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TEST_LocalizedUIReload.cs
수정일 : 2026-05-09

# 설명
LangCode 변경 시 ILocalizedUI 구현 MonoBehaviour 의 ReloadLocalizedUI 자동 호출 검증.

# 테스트 구성
 R: 씬 순회 reload — ILocalizedUI 구현체 모두 호출되는지

# 특이사항
PlayMode 필요 — 씬에 GameObject 부착 + Awake/OnEnable 이 정상 호출되어야 ILocalizedUI 가 잡힌다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.TestTools;

using NUnit;
using NUnit.Framework;

namespace inonego.Xeri.TEST._LocalizedUI
{
    using inonego.Xeri.Localization;

    // ============================================================
    /// <summary>
    /// ILocalizedUI 씬 순회 reload PlayMode 테스트.
    /// </summary>
    // ============================================================
    public class TEST_LocalizedUIReload
    {

    #region 헬퍼

        // ============================================================
        /// <summary>
        /// 테스트용 ILocalizedUI 구현 — Reload 호출 횟수와 마지막 코드를 기록.
        /// </summary>
        // ============================================================
        private class TestLocalizedUI : MonoBehaviour, ILocalizedUI
        {
            public int    ReloadCount      { get; private set; }
            public string LastCodeAtReload { get; private set; }

            public void ReloadLocalizedUI()
            {
                ReloadCount++;
                LastCodeAtReload = Localization.CurrentLangCode;
            }
        }

    #endregion

    #region 픽스처

        private readonly List<GameObject> spawned = new();

        [SetUp]
        public void SetUp()
        {
            Singleton<Localization>.Clear();

            var loc = new Localization(new InMemoryLocaleStorage("ko"));
            Singleton<Localization>.Register(loc);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var go in spawned)
            {
                if (go != null) UnityEngine.Object.DestroyImmediate(go);
            }
            spawned.Clear();

            Singleton<Localization>.Clear();
        }

        private TestLocalizedUI CreateTarget(string name)
        {
            var go = new GameObject(name);
            spawned.Add(go);
            return go.AddComponent<TestLocalizedUI>();
        }

    #endregion

    #region R-1: LangCode 변경 → 모든 ILocalizedUI Reload 호출

        [UnityTest]
        public IEnumerator TEST_LocalizedUI_LangCode_변경시_모든_구현체_Reload()
        {
            var a = CreateTarget("LocA");
            var b = CreateTarget("LocB");

            // 한 프레임 흘려 OnEnable 등 라이프사이클 진행.
            yield return null;

            Localization.CurrentLangCode = "en";

            yield return null;

            Assert.GreaterOrEqual(a.ReloadCount, 1, "A 가 최소 1회 Reload 받아야 합니다");
            Assert.GreaterOrEqual(b.ReloadCount, 1, "B 가 최소 1회 Reload 받아야 합니다");
            Assert.AreEqual("en", a.LastCodeAtReload);
            Assert.AreEqual("en", b.LastCodeAtReload);
        }

    #endregion

    #region R-2: 동일 LangCode 재설정 시 Reload 호출 안 됨

        [UnityTest]
        public IEnumerator TEST_LocalizedUI_동일_LangCode시_Reload_미호출()
        {
            var ui = CreateTarget("Loc");

            yield return null;

            int initial = ui.ReloadCount;

            Localization.CurrentLangCode = "ko"; // 동일

            yield return null;

            Assert.AreEqual(initial, ui.ReloadCount, "동일 LangCode 는 Reload 호출 안 됨");
        }

    #endregion

    }
}
