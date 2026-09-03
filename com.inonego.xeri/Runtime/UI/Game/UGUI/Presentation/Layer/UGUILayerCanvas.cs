/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : UGUILayerCanvas.cs
수정일 : 2026-09-03

# 설명
PresentationLayerAsset의 공통 Screen Overlay 순서와 합성 Alpha를 Canvas에 적용한다.
RectTransform은 View 배치 Root를, CanvasGroup은 Layer Canvas 전체 Alpha 경계를 제공한다.
========================================================================= BLOCK_HEADER_END */

using UnityEngine;

namespace inonego.Xeri.UI.Game
{
    // ============================================================
    /// <summary>
    /// UGUI Presentation Layer backend.
    /// </summary>
    // ============================================================
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class UGUILayerCanvas :
        MonoBehaviour,
        IPresentationLayerDriver<RectTransform>,
        IPresentationAlphaLayerDriver,
        IPresentationTransitionTarget
    {

    #region 필드

        // ------------------------------------------------------------
        /// <summary>
        /// 표시 View를 배치할 Layer Root.
        /// </summary>
        // ------------------------------------------------------------
        public RectTransform Root => root;

        [SerializeField]
        private RectTransform root = null;

        // ------------------------------------------------------------
        /// <summary>
        /// Layer가 사용하는 Canvas.
        /// </summary>
        // ------------------------------------------------------------
        public Canvas Canvas => canvas;

        [SerializeField]
        private Canvas canvas = null;

        // ------------------------------------------------------------
        /// <summary>
        /// Layer Canvas 전체에 합성되는 Presentation Alpha.
        /// </summary>
        // ------------------------------------------------------------
        public PresentationAlpha Alpha => presentationAlpha ??= new PresentationAlpha(this);

        private PresentationAlpha presentationAlpha = null;
        private CanvasGroup canvasGroup = null;
        private int order = 0;

    #endregion

    #region IPresentationLayerDriver

        // ------------------------------------------------------------
        /// <summary>
        /// RectTransform과 Canvas가 공통 Screen Overlay 정렬 공간에 속하는지 검증한다.
        /// </summary>
        // ------------------------------------------------------------
        public bool Validate
        (
            PresentationLayerAsset asset,
            out string error
        )
        {
            if (asset == null)
            {
                error = "Layer Asset이 null입니다.";
                return false;
            }

            if (root == null)
            {
                error = "Layer Root RectTransform이 연결되지 않았습니다.";
                return false;
            }

            if (canvas == null)
            {
                error = "Layer Canvas가 연결되지 않았습니다.";
                return false;
            }

            if (root != canvas.transform && !root.IsChildOf(canvas.transform))
            {
                error = "Layer Root는 Layer Canvas 자신이거나 하위에 있어야 합니다.";
                return false;
            }

            if (canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                error = "Layer Canvas는 Screen Space - Overlay여야 합니다.";
                return false;
            }

            if (canvas.targetDisplay != 0)
            {
                error = "Layer Canvas는 기본 Display를 사용해야 합니다.";
                return false;
            }

            if (canvas.sortingLayerID != 0)
            {
                error = "Layer Canvas는 Default Sorting Layer를 사용해야 합니다.";
                return false;
            }

            CacheCanvasGroup();

            if (canvasGroup == null || canvasGroup.transform != canvas.transform)
            {
                error = "Layer Canvas에 CanvasGroup이 필요합니다.";
                return false;
            }

            error = "";
            return true;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Canvas를 독립 정렬 단위로 설정하고 공통 Layer 순서를 적용한다.
        /// </summary>
        // ------------------------------------------------------------
        public void SetOrder(int order)
        {
            this.order = order;
            ApplyOrder();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Layer Prefab Root GameObject의 활성 상태를 적용한다.
        /// </summary>
        // ------------------------------------------------------------
        public void SetActive(bool active)
        {
            gameObject.SetActive(active);

            if (active)
            {
                // 비활성 Canvas가 무시한 정렬과 Alpha를 활성화가 끝난 실제 backend에 다시 적용한다.
                ApplyOrder();
                presentationAlpha?.Refresh();
            }
        }

    #endregion

    #region IPresentationTransitionTarget

        // ------------------------------------------------------------
        /// <summary>
        /// Layer CanvasGroup이 현재 합성 Alpha를 적용할 수 있는지 여부.
        /// </summary>
        // ------------------------------------------------------------
        bool IPresentationTransitionTarget.IsValid
        {
            get
            {
                if (this == null) return false;

                CacheCanvasGroup();
                return canvasGroup != null;
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 합성된 Alpha를 Layer Root CanvasGroup에 적용한다.
        /// </summary>
        // ------------------------------------------------------------
        void IPresentationTransitionTarget.Apply(float value)
        {
            CacheCanvasGroup();

            if (canvasGroup == null)
            {
                throw new MissingReferenceException("UGUI Layer CanvasGroup이 없습니다.");
            }

            canvasGroup.alpha = Mathf.Clamp01(value);
        }

    #endregion

    #region 내부 처리

        // ------------------------------------------------------------
        /// <summary>
        /// Layer Canvas GameObject의 CanvasGroup을 현재 backend에 연결한다.
        /// </summary>
        // ------------------------------------------------------------
        private void CacheCanvasGroup()
        {
            if (canvasGroup != null) return;

            canvasGroup = GetComponent<CanvasGroup>();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 Canvas에 보관한 공통 Layer 순서를 적용한다.
        /// </summary>
        // ------------------------------------------------------------
        private void ApplyOrder()
        {
            canvas.overrideSorting = true;
            canvas.sortingOrder = order;
        }

    #endregion

    }
}
