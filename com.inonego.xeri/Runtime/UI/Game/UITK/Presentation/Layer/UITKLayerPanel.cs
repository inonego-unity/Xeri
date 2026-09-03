/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : UITKLayerPanel.cs
수정일 : 2026-09-03

# 설명
PresentationLayerAsset의 공통 Screen Overlay 순서와 합성 Alpha를 독립 Runtime Panel에 적용한다.
Linear Color Space에서는 USS gamma 색을 보존하는 offscreen 합성을 Layer 수명에 맞춰 기본 제공한다.
Layer Root에 Xeri Runtime Control Baseline을 Theme과 무관하게 자동 적용한다.
========================================================================= BLOCK_HEADER_END */

using System;

using UnityEngine;
using UnityEngine.UIElements;

using inonego;
using inonego.Xeri;
using inonego.Xeri.UI;

namespace inonego.Xeri.UI.Game
{
    // ============================================================
    /// <summary>
    /// UI Toolkit Screen Overlay Presentation Layer.
    /// </summary>
    // ============================================================
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIDocument))]
    public sealed class UITKLayerPanel :
        MonoBehaviour,
        IPresentationLayerDriver<VisualElement>,
        IPresentationAlphaLayerDriver,
        IPresentationTransitionTarget
    {

    #region 필드

        private const string RootUssClassName = "xeri-game-ui";
        private const string RuntimeBaselineResourcePath =
            "Xeri/Game/GameUIRuntimeBaseline";

        private static StyleSheet runtimeBaseline = null;

        // ------------------------------------------------------------
        /// <summary>
        /// 표시 View를 배치할 Layer Root.
        /// </summary>
        // ------------------------------------------------------------
        public VisualElement Root => FindRoot();

        // ------------------------------------------------------------
        /// <summary>
        /// Layer Root 전체에 합성되는 Presentation Alpha.
        /// </summary>
        // ------------------------------------------------------------
        public PresentationAlpha Alpha => presentationAlpha ??= new PresentationAlpha(this);

        private PresentationAlpha presentationAlpha = null;
        private UIDocument document = null;

        [SerializeField]
        private string rootName = "";

        [SerializeField]
        private bool useGammaCompositing = true;

        private PanelSettings runtimePanelSettings = null;
        private readonly UITKPanelGammaCompositor gammaCompositor =
            new UITKPanelGammaCompositor();

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

            CacheDocument();

            if (document == null)
            {
                error = "Layer UIDocument가 없습니다.";
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

            if
            (
                document.panelSettings.targetTexture != null &&
                !gammaCompositor.Owns(document.panelSettings)
            )
            {
                error = "Layer UIDocument PanelSettings는 Target Texture를 사용할 수 없습니다.";
                return false;
            }

            if (document.panelSettings.targetDisplay != 0)
            {
                error = "Layer UIDocument PanelSettings는 기본 Display를 사용해야 합니다.";
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
            gammaCompositor.HostDocumentSortOrder = order;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Layer GameObject와 Root 표시 상태를 함께 적용한다.
        /// </summary>
        // ------------------------------------------------------------
        public void SetActive(bool active)
        {
            // Play Mode 종료에서는 Unity가 Driver를 먼저 파괴할 수 있으므로 이미 사라진 Layer는 정리 완료로 취급한다.
            if (this == null) return;

            if (!active)
            {
                // 비활성 Layer는 화면 크기 RT를 계속 소유하지 않도록 합성 자원을 함께 반환한다.
                gammaCompositor.Release();
                gameObject.SetActive(false);
                return;
            }

            gameObject.SetActive(active);

            var root = FindRoot();

            if (root == null)
            {
                var rootDescription = string.IsNullOrWhiteSpace(rootName)
                    ? "UIDocument Root"
                    : $"UITK Layer Root '{rootName}'";

                throw new MissingReferenceException
                (
                    $"{rootDescription}를 찾을 수 없습니다."
                );
            }

            // Theme 구성과 무관하게 Xeri Control Baseline을 이 Layer에 적용한다.
            ApplyRuntimeBaseline(root);
            root.style.display = DisplayStyle.Flex;
            presentationAlpha?.Refresh();
            ApplyGammaCompositing();
        }

    #endregion

    #region IPresentationTransitionTarget

        // ------------------------------------------------------------
        /// <summary>
        /// Layer Root가 현재 Panel에 연결되어 Alpha를 적용할 수 있는지 여부.
        /// </summary>
        // ------------------------------------------------------------
        bool IPresentationTransitionTarget.IsValid
        {
            get
            {
                if (this == null) return false;

                var root = FindRoot();
                return root != null && root.panel != null;
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 합성된 Alpha를 Layer Root opacity에 적용한다.
        /// </summary>
        // ------------------------------------------------------------
        void IPresentationTransitionTarget.Apply(float value)
        {
            var root = FindRoot() ??
                throw new MissingReferenceException("UITK Layer Root를 찾을 수 없습니다.");
            root.style.opacity = Mathf.Clamp01(value);
        }

    #endregion

    #region 내부 처리

        // ------------------------------------------------------------
        /// <summary>
        /// Layer Root 범위에 Xeri Control Baseline을 연결한다.
        /// </summary>
        // ------------------------------------------------------------
        private static void ApplyRuntimeBaseline(VisualElement root)
        {
            // Package Resources의 단일 Baseline Asset을 Layer 간 공유한다.
            if (runtimeBaseline == null)
            {
                runtimeBaseline = Resources.Load<StyleSheet>
                (
                    RuntimeBaselineResourcePath
                );
            }

            if (runtimeBaseline == null)
            {
                throw new MissingReferenceException
                (
                    $"Game UI Runtime Baseline '{RuntimeBaselineResourcePath}'을 " +
                    "찾을 수 없습니다."
                );
            }

            // 재활성화에서 같은 StyleSheet를 중복 연결하지 않는다.
            root.AddToClassList(RootUssClassName);

            if (!root.styleSheets.Contains(runtimeBaseline))
            {
                root.styleSheets.Add(runtimeBaseline);
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 공유 Asset을 변경하지 않는 Layer 전용 Runtime PanelSettings를 준비한다.
        /// </summary>
        // ------------------------------------------------------------
        private void EnsureRuntimePanelSettings()
        {
            if (runtimePanelSettings != null) return;

            CacheDocument();
            var source = document.panelSettings;
            runtimePanelSettings = Instantiate(source);
            runtimePanelSettings.name = $"{source.name} ({name})";
            document.panelSettings = runtimePanelSettings;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// UIDocument Root 또는 선택적으로 지정한 하위 Layer Root를 찾는다.
        /// </summary>
        // ------------------------------------------------------------
        private VisualElement FindRoot()
        {
            CacheDocument();
            var documentRoot = document != null ? document.rootVisualElement : null;

            if (documentRoot == null || string.IsNullOrWhiteSpace(rootName))
            {
                return documentRoot;
            }

            return documentRoot.Q<VisualElement>(rootName);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 같은 Layer GameObject의 UIDocument를 현재 Driver에 연결한다.
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
        /// Linear Color Space에서 Layer 전용 gamma 합성을 준비한다.
        /// </summary>
        // ------------------------------------------------------------
        private void ApplyGammaCompositing()
        {
            var requiresGammaComposite =
                useGammaCompositing &&
                QualitySettings.activeColorSpace == ColorSpace.Linear;

            if (!requiresGammaComposite)
            {
                gammaCompositor.Release();
                return;
            }

            gammaCompositor.HostName = gameObject.name;
            gammaCompositor.HostDocumentSortOrder = document.sortingOrder;

            if
            (
                !gammaCompositor.TryAcquire
                (
                    runtimePanelSettings,
                    this,
                    out var error
                )
            )
            {
                throw new InvalidOperationException(error);
            }
        }

    #endregion

    #region Unity 이벤트

        // ------------------------------------------------------------
        /// <summary>
        /// 활성 Layer의 RenderTexture 크기와 합성 출력을 현재 프레임에 맞춘다.
        /// </summary>
        // ------------------------------------------------------------
        private void Update()
        {
            if (!gammaCompositor.Owns(runtimePanelSettings)) return;

            gammaCompositor.HostDocumentSortOrder = document.sortingOrder;
            gammaCompositor.Refresh();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Layer가 소유한 Runtime PanelSettings를 함께 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        private void OnDestroy()
        {
            gammaCompositor.Release();

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
