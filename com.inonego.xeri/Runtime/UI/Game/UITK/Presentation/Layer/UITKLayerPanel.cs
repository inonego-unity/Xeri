/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : UITKLayerPanel.cs
수정일 : 2026-07-31

# 설명
PresentationLayerAsset의 공통 Screen Overlay 순서를 독립 Runtime Panel과 UIDocument에 적용한다.
========================================================================= BLOCK_HEADER_END */

using UnityEngine;
using UnityEngine.UIElements;

namespace inonego.Xeri.UI.Game
{
    // ============================================================
    /// <summary>
    /// UI Toolkit Screen Overlay Presentation Layer.
    /// </summary>
    // ============================================================
    public sealed class UITKLayerPanel : MonoBehaviour, IPresentationLayerDriver<VisualElement>
    {
    #region 필드

        // ------------------------------------------------------------
        /// <summary>
        /// 표시 View를 배치할 Layer Root.
        /// </summary>
        // ------------------------------------------------------------
        public VisualElement Root => FindRoot();

        [SerializeField]
        private UIDocument document = null;

        [SerializeField]
        private string rootName = "LayerRoot";

        private PanelSettings runtimePanelSettings = null;

    #endregion

    #region IPresentationLayerDriver

        // ------------------------------------------------------------
        /// <summary>
        /// UIDocument가 공통 Screen Overlay 정렬 공간에 속하는지 검증한다.
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

            if (document == null)
            {
                error = "Layer UIDocument가 연결되지 않았습니다.";
                return false;
            }

            if (!document.enabled)
            {
                error = "Layer UIDocument가 비활성 상태입니다.";
                return false;
            }

            if (document.panelSettings == null)
            {
                error = "Layer UIDocument PanelSettings가 연결되지 않았습니다.";
                return false;
            }

            if (document.panelSettings.targetTexture != null)
            {
                error = "Layer UIDocument PanelSettings는 Target Texture를 사용할 수 없습니다.";
                return false;
            }

            if (document.panelSettings.targetDisplay != 0)
            {
                error = "Layer UIDocument PanelSettings는 기본 Display를 사용해야 합니다.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(rootName))
            {
                error = "Layer Root 이름이 비어 있습니다.";
                return false;
            }

            error = "";
            return true;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Layer별 Runtime Panel과 UIDocument에 공통 Layer 순서를 적용한다.
        /// </summary>
        // ------------------------------------------------------------
        public void SetOrder(int order)
        {
            EnsureRuntimePanelSettings();
            runtimePanelSettings.sortingOrder = order;
            document.sortingOrder = order;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Layer GameObject와 Root 표시 상태를 함께 적용한다.
        /// </summary>
        // ------------------------------------------------------------
        public void SetActive(bool active)
        {
            gameObject.SetActive(active);

            if (!active) return;

            var root = FindRoot();

            if (root == null)
            {
                throw new MissingReferenceException
                (
                    $"UITK Layer Root '{rootName}'을 찾을 수 없습니다."
                );
            }

            root.style.display = DisplayStyle.Flex;
        }

    #endregion

    #region 내부 처리

        // ------------------------------------------------------------
        /// <summary>
        /// 공유 Asset을 변경하지 않는 Layer 전용 Runtime PanelSettings를 준비한다.
        /// </summary>
        // ------------------------------------------------------------
        private void EnsureRuntimePanelSettings()
        {
            if (runtimePanelSettings != null) return;

            var source = document.panelSettings;
            runtimePanelSettings = Instantiate(source);
            runtimePanelSettings.name = $"{source.name} ({name})";
            document.panelSettings = runtimePanelSettings;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 UIDocument Visual Tree에서 직렬화한 Layer Root를 찾는다.
        /// </summary>
        // ------------------------------------------------------------
        private VisualElement FindRoot()
        {
            var documentRoot = document != null ? document.rootVisualElement : null;
            return documentRoot?.Q<VisualElement>(rootName);
        }

    #endregion

    #region Unity 이벤트

        // ------------------------------------------------------------
        /// <summary>
        /// Layer가 소유한 Runtime PanelSettings를 함께 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        private void OnDestroy()
        {
            if (runtimePanelSettings == null) return;

            if (Application.isPlaying)
            {
                Destroy(runtimePanelSettings);
            }
            else
            {
                DestroyImmediate(runtimePanelSettings);
            }

            runtimePanelSettings = null;
        }

    #endregion

    }
}
