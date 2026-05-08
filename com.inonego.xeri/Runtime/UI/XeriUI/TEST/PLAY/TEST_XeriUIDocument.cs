/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TEST_XeriUIDocument.cs
수정일 : 2026-05-08

# 설명
XeriUIDocument 통합 동작 PlayMode 검증 테스트.
실제 Unity UI Toolkit 패널이 띄워진 상태에서 GammaRTBlitter 의 PanelSettings 점유 + RT 생성을 확인한다.

# 테스트 구성
 I: 통합 동작 (PanelSettings 점유 / RT 생성 / forceGammaRendering 적용 / 해제 시 복원)

# 특이사항
GammaRTBlitter 의 정적 레지스트리는 테스트 간 공유되므로 [TearDown] 에서 GameObject 를 확실히 파괴해 점유를 해제한다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.TestTools;

using NUnit.Framework;

using inonego.Xeri.UI;

using Object = UnityEngine.Object;

namespace inonego.Xeri.TEST.UI._XeriUI
{

    // ============================================================
    /// <summary>
    /// XeriUIDocument 통합 동작 PlayMode 테스트 클래스.
    /// </summary>
    // ============================================================
    public class TEST_XeriUIDocument
    {

    #region 픽스처

        private readonly List<Object> spawned = new();

        // ------------------------------------------------------------
        /// <summary>
        /// 각 테스트 후 생성한 GameObject / PanelSettings 를 정리한다.
        /// </summary>
        // ------------------------------------------------------------
        [TearDown]
        public void TearDown()
        {
            foreach (var obj in spawned)
            {
                if (obj != null) Object.DestroyImmediate(obj);
            }

            spawned.Clear();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 헬퍼 — UIDocument + XeriUIDocument + PanelSettings 가 부착된 GameObject 를 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        private (GameObject go, UIDocument doc, PanelSettings panel) CreateXeriUIDocumentGO()
        {
            var panel = ScriptableObject.CreateInstance<PanelSettings>();
            panel.name = "TEST_PanelSettings";
            spawned.Add(panel);

            var go = new GameObject("TEST_XeriUIDocument");
            spawned.Add(go);

            var doc = go.AddComponent<UIDocument>();
            doc.panelSettings = panel;

            // XeriUIDocument 부착 → OnEnable 에서 GammaRTBlitter.TryAcquire 호출
            go.AddComponent<XeriUIDocument>();

            return (go, doc, panel);
        }

    #endregion

    #region I-1: PanelSettings 점유 + RT 생성

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> XeriUIDocument 가 부착되면 GammaRTBlitter 가 PanelSettings 를 점유하고
        /// <br/> targetTexture 에 RT 를 할당하며 forceGammaRendering 을 활성화한다.
        /// </summary>
        // ----------------------------------------------------------------------
        [UnityTest]
        public IEnumerator TEST_XeriUIDocument_부착_시_RT_생성_및_panel_점유()
        {
            var (go, doc, panel) = CreateXeriUIDocumentGO();

            // OnEnable + 첫 Update 가 흘러야 점유 완료
            yield return null;
            yield return null;

            Assert.IsNotNull(panel.targetTexture, "blitter 가 RT 를 생성해 PanelSettings 에 할당해야 합니다");
            Assert.IsTrue(panel.forceGammaRendering, "forceGammaRendering 이 true 로 강제되어야 합니다");
        }

    #endregion

    #region I-2: 해제 시 PanelSettings 원상 복원

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> XeriUIDocument 가 파괴되면 GammaRTBlitter 가 점유를 해제하고
        /// <br/> PanelSettings 의 targetTexture / forceGammaRendering 이 원래 값으로 복원된다.
        /// </summary>
        // ----------------------------------------------------------------------
        [UnityTest]
        public IEnumerator TEST_XeriUIDocument_파괴_시_panel_원상_복원()
        {
            var (go, doc, panel) = CreateXeriUIDocumentGO();

            yield return null;
            yield return null;

            // 사전: 점유 상태 확인
            Assert.IsNotNull(panel.targetTexture);

            // 파괴 → OnDestroy → blitter.Release() → panel 복원
            Object.DestroyImmediate(go);
            spawned.Remove(go);

            yield return null;

            Assert.IsNull   (panel.targetTexture,       "Release 후 targetTexture 가 원래 null 로 복원되어야 합니다");
            Assert.IsFalse  (panel.forceGammaRendering, "Release 후 forceGammaRendering 이 원래 false 로 복원되어야 합니다");
        }

    #endregion

    }

}
