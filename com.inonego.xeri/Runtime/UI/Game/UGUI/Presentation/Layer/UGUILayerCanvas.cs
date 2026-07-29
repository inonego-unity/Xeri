/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : UGUILayerCanvas.cs
수정일 : 2026-07-29

# 설명
PresentationLayerAsset 구성을 Unity Canvas와 RectTransform Root로 검증하고 활성화한다.
========================================================================= BLOCK_HEADER_END */

using UnityEngine;

namespace inonego.Xeri.UI.Game
{
    // ============================================================
    /// <summary>
    /// UGUI Presentation Layer backend.
    /// </summary>
    // ============================================================
    public sealed class UGUILayerCanvas : MonoBehaviour, IPresentationLayerDriver
    {
    #region 필드

        // ------------------------------------------------------------
        /// <summary>
        /// 표시 View를 배치할 Layer Root.
        /// </summary>
        // ------------------------------------------------------------
        public Transform Root => root;

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

    #endregion

    #region IPresentationLayerDriver

        // ------------------------------------------------------------
        /// <summary>
        /// RectTransform과 Canvas 구성이 Layer Asset 정책과 일치하는지 검증한다.
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

            if (asset.Mode == PresentationLayerMode.Independent)
            {
                if (canvas == null)
                {
                    error = "독립 Layer Canvas가 연결되지 않았습니다.";
                    return false;
                }

                if (!canvas.overrideSorting)
                {
                    error = "독립 Layer Canvas의 overrideSorting이 꺼져 있습니다.";
                    return false;
                }

                if (canvas.sortingOrder != asset.Order)
                {
                    error = $"Canvas sortingOrder가 Asset Order({asset.Order})와 다릅니다.";
                    return false;
                }
            }
            else
            {
                if (root.parent == null)
                {
                    error = "공유 Layer Root에 부모 Transform이 없습니다.";
                    return false;
                }

                if (canvas != null && canvas.overrideSorting)
                {
                    error = "공유 Layer Canvas에 overrideSorting이 설정되어 있습니다.";
                    return false;
                }
            }

            error = "";
            return true;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Layer Root GameObject의 활성 상태를 적용한다.
        /// </summary>
        // ------------------------------------------------------------
        public void SetActive(bool active)
        {
            if (root != null)
            {
                root.gameObject.SetActive(active);
            }
        }

    #endregion

    }
}
