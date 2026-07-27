/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : XeriUIDocument.cs
수정일 : 2026-05-08

# 설명
GammaRTBlitter + XeriLoopAnimator 두 시스템을 조립하는 컴포지션 루트 컴포넌트.
UIDocument 가 부착된 GameObject 에 함께 부여하면 PanelSettings 의 gamma 합성과 USS 루프 애니메이션이 동시에 활성화된다.

# 책임 분리
- 본 컴포넌트: MonoBehaviour 라이프사이클 → 두 헬퍼에 위임
- GammaRTBlitter:  PanelSettings 점유 / RT 합성 / 카메라 콜백
- XeriLoopAnimator: USS --xeri-next 기반 무한 루프 애니메이션
========================================================================= BLOCK_HEADER_END */

using UnityEngine;
using UnityEngine.UIElements;

namespace inonego.Xeri.UI
{
    // ====================================================================================
    /// <summary>
    /// <br/> UI Toolkit 문서에 gamma 합성 + USS 루프 애니메이션을 동시 활성화하는 컴포지션 루트.
    /// <br/> 두 책임은 각각 헬퍼 클래스로 분리되어 있다.
    /// </summary>
    // ====================================================================================
    [ExecuteAlways]
    [RequireComponent(typeof(UIDocument))]
    public class XeriUIDocument : MonoBehaviour
    {

    #region 필드

        // ------------------------------------------------------------
        /// <summary>
        /// gamma→linear 강제 적용 여부 (Inspector 노출).
        /// </summary>
        // ------------------------------------------------------------
        [SerializeField]
        private bool forceGammaRendering = true;

        private UIDocument        document;
        private GammaRTBlitter    blitter  = new GammaRTBlitter();
        private XeriLoopAnimator  animator = new XeriLoopAnimator();

    #endregion

    #region 유니티 이벤트

        private void OnEnable()
        {
            CacheDocument();

            blitter.HostName            = gameObject.name;
            blitter.ForceGammaRendering = forceGammaRendering;
            blitter.TryAcquire(GetCurrentPanelSettings(), this);

            SetupAnimator();
        }

        private void OnDisable()
        {
            blitter.Release();

            animator.Cleanup();
        }

        private void OnDestroy()
        {
            blitter.Release();
        }

        private void OnValidate()
        {
            blitter.ForceGammaRendering = forceGammaRendering;
            blitter.ApplyPanelOverridesIfOwner();
        }

        private void Update()
        {
            CacheDocument();

            blitter.HostName              = gameObject.name;
            blitter.ForceGammaRendering   = forceGammaRendering;
            blitter.HostDocumentSortOrder = document != null ? document.sortingOrder : 0f;

            blitter.Refresh(GetCurrentPanelSettings(), this);
        }

    #endregion

    #region 내부 메서드

        // ------------------------------------------------------------
        /// <summary>
        /// 같은 GameObject 의 UIDocument 를 캐시한다.
        /// </summary>
        // ------------------------------------------------------------
        private void CacheDocument()
        {
            if (document == null)
            {
                document = GetComponent<UIDocument>();
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 UIDocument 가 가리키는 PanelSettings 를 반환한다 (없으면 null).
        /// </summary>
        // ------------------------------------------------------------
        private PanelSettings GetCurrentPanelSettings()
        {
            return document != null ? document.panelSettings : null;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// rootVisualElement 가 준비되었으면 애니메이터를 셋업한다.
        /// </summary>
        // ------------------------------------------------------------
        private void SetupAnimator()
        {
            if (document == null || document.rootVisualElement == null) return;

            animator.Setup(document.rootVisualElement);
        }

    #endregion

    }
}
