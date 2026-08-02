/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : UITKPanelGammaCompositor.cs
수정일 : 2026-08-02

# 설명
UI Toolkit PanelSettings를 offscreen RenderTexture로 렌더링한 뒤 화면용 Panel에서
gamma→linear 합성하는 Panel 단위 컴포지터다.

# 특이사항
한 PanelSettings는 동시에 한 인스턴스만 점유할 수 있다.
화면용 합성 Panel은 원본 Panel보다 작은 Order Offset만큼 위에 배치해 UGUI와의 공통 Layer 순서를 유지한다.
========================================================================= BLOCK_HEADER_END */

using UnityEngine;
using UnityEngine.UIElements;

using Object = UnityEngine.Object;

namespace inonego.Xeri.UI
{
    // ======================================================================================
    /// <summary>
    /// <br/> PanelSettings를 offscreen RT로 렌더링한 뒤 화면용 UI Toolkit Panel에서 합성한다.
    /// <br/> MonoBehaviour가 아닌 일반 클래스로, 호스트 컴포넌트가 수명을 관리한다.
    /// </summary>
    // ======================================================================================
    internal sealed class UITKPanelGammaCompositor
    {
    #region 내부 데이터

        // ============================================================
        /// <summary>
        /// 화면용 Panel에서 offscreen UI를 합성하는 요소.
        /// </summary>
        // ============================================================
        private sealed class GammaCompositeElement : VisualElement
        {
        #region 필드

            // ------------------------------------------------------------
            /// <summary>
            /// 합성할 offscreen UI RenderTexture.
            /// </summary>
            // ------------------------------------------------------------
            public RenderTexture Texture
            {
                get => texture;
                set
                {
                    texture = value;

                    if (texture == null)
                    {
                        style.backgroundImage = new StyleBackground(StyleKeyword.None);
                    }
                    else
                    {
                        style.backgroundImage = Background.FromRenderTexture(texture);
                    }

                    MarkDirtyRepaint();
                }
            }

            private RenderTexture texture = null;

            // ------------------------------------------------------------
            /// <summary>
            /// gamma→linear 합성 Material.
            /// </summary>
            // ------------------------------------------------------------
            public Material Material
            {
                get => material;
                set
                {
                    material = value;
                    style.unityMaterial = material;
                    MarkDirtyRepaint();
                }
            }

            private Material material = null;

        #endregion

        #region 생성자

            // ------------------------------------------------------------
            /// <summary>
            /// 화면 전체를 채우되 입력 대상에는 포함되지 않는 합성 요소를 생성한다.
            /// </summary>
            // ------------------------------------------------------------
            public GammaCompositeElement() : base()
            {
                pickingMode = PickingMode.Ignore;
                style.position = Position.Absolute;
                style.left = 0f;
                style.top = 0f;
                style.right = 0f;
                style.bottom = 0f;
            }

        #endregion
        }

    #endregion

    #region 필드

        private const string BLIT_SHADER_NAME = "Hidden/XeriUI/UITKGammaComposite";
        private const float COMPOSITE_ORDER_OFFSET = 0.25f;
        private const int DEPTH_STENCIL_BITS = 24;

        // ------------------------------------------------------------
        /// <summary>
        /// 호스트 GameObject 이름.
        /// </summary>
        // ------------------------------------------------------------
        public string HostName { get; set; } = "UITK Panel";

        // ------------------------------------------------------------
        /// <summary>
        /// 합성 Panel이 따라갈 호스트 UIDocument Order.
        /// </summary>
        // ------------------------------------------------------------
        public float HostDocumentSortOrder
        {
            get => hostDocumentSortOrder;
            set
            {
                hostDocumentSortOrder = value;
                ApplyCompositeOrder();
            }
        }

        private float hostDocumentSortOrder = 0f;
        private PanelSettings managedPanelSettings = null;
        private PanelSettings compositePanelSettings = null;
        private RenderTexture targetTexture = null;
        private RenderTexture previousTargetTexture = null;
        private GameObject compositeHost = null;
        private UIDocument compositeDocument = null;
        private GammaCompositeElement compositeElement = null;
        private Material blitMaterial = null;
        private bool previousForceGammaRendering = false;
        private bool previousClearDepthStencil = false;
        private bool previousClearColor = false;
        private Color previousColorClearValue = Color.clear;
        private bool isPanelOwner = false;
        private int lastWidth = 0;
        private int lastHeight = 0;

    #endregion

    #region 메서드

        // ------------------------------------------------------------
        /// <summary>
        /// PanelSettings를 점유하고 실패 이유를 호출자에게 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public bool TryAcquire
        (
            PanelSettings panelSettings,
            Component host,
            out string error
        )
        {
            error = GetUnsupportedReason(panelSettings, host);

            if (error != null) return false;

            if (isPanelOwner && managedPanelSettings == panelSettings)
            {
                RefreshOwnedPanel();
                return true;
            }

            if (isPanelOwner)
            {
                Release();
            }

            var shader = Shader.Find(BLIT_SHADER_NAME);

            if (shader == null)
            {
                error = $"[UITKPanelGammaCompositor] Shader not found: {BLIT_SHADER_NAME}";
                return false;
            }

            // 전제 조건이 확인된 뒤 Panel과 원래 값을 이 컴포지터의 소유로 기록한다.
            managedPanelSettings = panelSettings;
            previousTargetTexture = panelSettings.targetTexture;
            previousForceGammaRendering = panelSettings.forceGammaRendering;
            previousClearDepthStencil = panelSettings.clearDepthStencil;
            previousClearColor = panelSettings.clearColor;
            previousColorClearValue = panelSettings.colorClearValue;
            isPanelOwner = true;

            try
            {
                blitMaterial = new Material(shader)
                {
                    name = $"{HostName} Gamma Composite Material",
                    hideFlags = HideFlags.HideAndDontSave,
                };

                EnsureRenderTexture();
                CreateCompositeOutput(host.transform);
                ApplyPanelOverrides();
                ApplyCompositeOrder();
                compositeElement.MarkDirtyRepaint();
                error = "";
                return true;
            }
            catch
            {
                // 준비 중 만든 자원과 Panel 오버라이드만 되돌린 뒤 최초 오류를 그대로 전달한다.
                Release();
                throw;
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 화면 크기와 Panel 오버라이드를 현재 상태에 맞게 갱신한다.
        /// </summary>
        // ------------------------------------------------------------
        public void Refresh()
        {
            if (!isPanelOwner) return;

            RefreshOwnedPanel();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 지정 PanelSettings가 현재 이 합성기의 소유 대상인지 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public bool Owns(PanelSettings panelSettings)
        {
            return isPanelOwner && managedPanelSettings == panelSettings;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 점유를 종료하고 PanelSettings와 생성 자원을 원래 상태로 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public void Release()
        {
            // 화면 출력부터 중단해 지연 파괴되는 자원이 다음 UI 프레임에 다시 그려지지 않게 한다.
            if (compositeHost != null)
            {
                compositeHost.SetActive(false);
            }

            if (isPanelOwner && managedPanelSettings != null)
            {
                managedPanelSettings.targetTexture = previousTargetTexture;
                managedPanelSettings.forceGammaRendering = previousForceGammaRendering;
                managedPanelSettings.clearDepthStencil = previousClearDepthStencil;
                managedPanelSettings.clearColor = previousClearColor;
                managedPanelSettings.colorClearValue = previousColorClearValue;
            }

            ReleaseCompositeOutput();
            ReleaseRenderTexture();
            ReleaseMaterial();

            isPanelOwner = false;
            managedPanelSettings = null;
            previousTargetTexture = null;
            lastWidth = 0;
            lastHeight = 0;
        }

    #endregion

    #region 내부 처리

        // ------------------------------------------------------------
        /// <summary>
        /// 합성 준비에 필요한 입력 계약을 검증한다.
        /// </summary>
        // ------------------------------------------------------------
        private static string GetUnsupportedReason
        (
            PanelSettings panelSettings,
            Component host
        )
        {
            if (panelSettings == null)
            {
                return "[UITKPanelGammaCompositor] Assign PanelSettings before enabling compositing.";
            }

            if (host == null)
            {
                return "[UITKPanelGammaCompositor] A Component host is required.";
            }

            return null;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 소유 중인 Panel과 합성 출력을 현재 화면 상태로 갱신한다.
        /// </summary>
        // ------------------------------------------------------------
        private void RefreshOwnedPanel()
        {
            EnsureRenderTexture();
            ApplyPanelOverrides();
            ApplyCompositeOrder();
            compositeElement?.MarkDirtyRepaint();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 화면 해상도에 맞는 offscreen UI RenderTexture를 보장한다.
        /// </summary>
        // ------------------------------------------------------------
        private void EnsureRenderTexture()
        {
            var width = Mathf.Max(1, Screen.width);
            var height = Mathf.Max(1, Screen.height);

            if (targetTexture != null && lastWidth == width && lastHeight == height) return;

            ReleaseRenderTexture();

            // UNORM Linear RT는 forceGammaRendering이 기록한 USS gamma 값을 변환 없이 보존한다.
            targetTexture = new RenderTexture
            (
                width,
                height,
                DEPTH_STENCIL_BITS,
                RenderTextureFormat.ARGB32,
                RenderTextureReadWrite.Linear
            )
            {
                name = $"{HostName}_ManagedGammaPanelRT",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                autoGenerateMips = false,
                useMipMap = false,
                hideFlags = HideFlags.HideAndDontSave,
            };

            targetTexture.Create();

            lastWidth = width;
            lastHeight = height;

            if (compositeElement != null)
            {
                compositeElement.Texture = targetTexture;
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 원본 Panel을 화면에서 분리하고 gamma 값을 offscreen RT에 기록하도록 설정한다.
        /// </summary>
        // ------------------------------------------------------------
        private void ApplyPanelOverrides()
        {
            if (!isPanelOwner || managedPanelSettings == null || targetTexture == null) return;

            managedPanelSettings.targetTexture = targetTexture;
            managedPanelSettings.forceGammaRendering = true;
            managedPanelSettings.clearDepthStencil = true;
            managedPanelSettings.clearColor = true;
            managedPanelSettings.colorClearValue = Color.clear;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 원본 RT를 공통 Screen Overlay 정렬 공간에 다시 그릴 화면용 Panel을 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        private void CreateCompositeOutput(Transform hostTransform)
        {
            compositePanelSettings = Object.Instantiate(managedPanelSettings);
            compositePanelSettings.name = $"{HostName} Gamma Composite Panel";
            compositePanelSettings.targetTexture = null;
            compositePanelSettings.forceGammaRendering = false;
            compositePanelSettings.clearColor = false;

            compositeHost = new GameObject($"{HostName} Gamma Composite");
            compositeHost.SetActive(false);

            // 원본 UIDocument의 자식이 되면 Unity가 같은 Panel의 Child Document로 묶으므로 형제로 둔다.
            compositeHost.transform.SetParent(hostTransform.parent, false);
            compositeDocument = compositeHost.AddComponent<UIDocument>();
            compositeDocument.panelSettings = compositePanelSettings;
            compositeHost.SetActive(true);

            var root = compositeDocument.rootVisualElement;

            if (root == null)
            {
                throw new MissingReferenceException("Gamma Composite UIDocument Root를 생성하지 못했습니다.");
            }

            // 합성 Panel은 시각 출력만 담당하고 원본 Panel의 입력 경로를 가리지 않는다.
            root.pickingMode = PickingMode.Ignore;
            root.style.flexGrow = 1f;
            compositeElement = new GammaCompositeElement
            {
                Texture = targetTexture,
                Material = blitMaterial,
            };
            root.Add(compositeElement);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 합성 Panel을 원본 Layer 바로 위이면서 다음 정수 Layer보다 아래인 순서에 배치한다.
        /// </summary>
        // ------------------------------------------------------------
        private void ApplyCompositeOrder()
        {
            var compositeOrder = hostDocumentSortOrder + COMPOSITE_ORDER_OFFSET;

            if (compositePanelSettings != null)
            {
                compositePanelSettings.sortingOrder = compositeOrder;
            }

            if (compositeDocument != null)
            {
                compositeDocument.sortingOrder = compositeOrder;
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 화면용 합성 Panel과 VisualElement를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        private void ReleaseCompositeOutput()
        {
            if (compositeElement != null)
            {
                compositeElement.Texture = null;
                compositeElement.Material = null;
                compositeElement.RemoveFromHierarchy();
                compositeElement = null;
            }

            if (compositeHost != null)
            {
                DestroyObject(compositeHost);
                compositeHost = null;
            }

            compositeDocument = null;

            if (compositePanelSettings != null)
            {
                DestroyObject(compositePanelSettings);
                compositePanelSettings = null;
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// offscreen UI RenderTexture를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        private void ReleaseRenderTexture()
        {
            if (targetTexture == null) return;

            if (compositeElement != null)
            {
                compositeElement.Texture = null;
            }

            targetTexture.Release();
            DestroyObject(targetTexture);
            targetTexture = null;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 이 합성기가 소유한 Material을 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        private void ReleaseMaterial()
        {
            if (blitMaterial == null) return;

            DestroyObject(blitMaterial);
            blitMaterial = null;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Play Mode 여부에 맞는 Unity Object 파괴 방식을 적용한다.
        /// </summary>
        // ------------------------------------------------------------
        private static void DestroyObject(Object target)
        {
            if (target == null) return;

            if (Application.isPlaying)
            {
                Object.Destroy(target);
            }
            else
            {
                Object.DestroyImmediate(target);
            }
        }

    #endregion
    }
}
