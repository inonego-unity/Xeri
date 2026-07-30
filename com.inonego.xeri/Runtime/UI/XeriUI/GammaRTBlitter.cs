/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : GammaRTBlitter.cs
수정일 : 2026-07-30

# 설명
UI Toolkit PanelSettings 를 offscreen RenderTexture 로 렌더링한 뒤 카메라 출력 위에 gamma→linear blit 합성하는 컴포넌트 비종속 헬퍼.
PanelSettings 슬롯을 인스턴스 단위로 점유하며 OnDestroy/Release 시 원래 상태로 복원한다.

# 특이사항
한 PanelSettings 는 동시에 한 인스턴스만 점유할 수 있다 (중복 시 경고).
Linear color space 환경에서 sRGB UI 콘텐츠를 정확히 표시하기 위함.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Rendering;

using Object = UnityEngine.Object;

namespace inonego.Xeri.UI
{
    // ======================================================================================
    /// <summary>
    /// <br/> PanelSettings 를 offscreen RT 로 렌더링 후 화면에 gamma→linear blit 합성한다.
    /// <br/> MonoBehaviour 가 아닌 일반 클래스로, 호스트 컴포넌트가 라이프사이클을 관리한다.
    /// </summary>
    // ======================================================================================
    public class GammaRTBlitter
    {

    #region 필드

        private const string BLIT_SHADER_NAME = "XeriUI/_GammaToLinearBlit";

        private static readonly int BLIT_TEXTURE_ID    = Shader.PropertyToID("_BlitTexture");
        private static readonly int BLIT_SCALE_BIAS_ID = Shader.PropertyToID("_BlitScaleBias");

        private static readonly List<GammaRTBlitter>                 ManagedBlitters = new();
        private static readonly Dictionary<PanelSettings, GammaRTBlitter> PanelOwners = new();

        private static Material blitMaterial;
        private static bool     hasMissingShaderWarning;

        // ------------------------------------------------------------
        /// <summary>
        /// 호스트 GameObject 이름 (RT 명명용 + 경고 메시지용).
        /// </summary>
        // ------------------------------------------------------------
        public string HostName { get; set; } = "XeriUIDocument";

        // ------------------------------------------------------------
        /// <summary>
        /// gamma→linear 강제 적용 여부.
        /// </summary>
        // ------------------------------------------------------------
        public bool ForceGammaRendering { get; set; } = true;

        // ------------------------------------------------------------
        /// <summary>
        /// 합성 시 정렬용 호스트 sortingOrder (큰 쪽이 위로). 호스트가 매 프레임 갱신.
        /// </summary>
        // ------------------------------------------------------------
        public float HostDocumentSortOrder { get; set; }

        private PanelSettings managedPanelSettings;
        private RenderTexture targetTexture;
        private RenderTexture previousTargetTexture;
        private bool          previousForceGammaRendering;
        private bool          isPanelOwner;
        private int           lastWidth;
        private int           lastHeight;
        private string        lastValidationWarning;
        private bool          hasDuplicateOwnerWarning;

    #endregion

    #region 메서드

        // ------------------------------------------------------------
        /// <summary>
        /// PanelSettings 를 점유하고 RT 합성 준비를 시작한다.
        /// </summary>
        // ------------------------------------------------------------
        public void TryAcquire(PanelSettings panelSettings, Object warnContext)
        {
            string unsupportedReason = GetUnsupportedReason(panelSettings);

            if (unsupportedReason != null)
            {
                WarnOnce(unsupportedReason, warnContext);
                return;
            }

            lastValidationWarning    = null;
            hasDuplicateOwnerWarning = false;

            if (PanelOwners.TryGetValue(panelSettings, out GammaRTBlitter owner) && owner != null && owner != this)
            {
                if (!hasDuplicateOwnerWarning)
                {
                    Debug.LogWarning(
                        $"[GammaRTBlitter] PanelSettings '{panelSettings.name}' already owned by '{owner.HostName}'. " +
                        "Only one blitter can own a PanelSettings asset at a time.",
                        warnContext);

                    hasDuplicateOwnerWarning = true;
                }
                return;
            }

            PanelOwners[panelSettings] = this;
            ManagedBlitters.Add(this);

            managedPanelSettings = panelSettings;

            // Release 시 원래 상태로 복원하기 위해 인수 시점의 값을 보관.
            // 다른 시스템이 동일 PanelSettings 를 사용하는 경우 본 컴포넌트 제거 후에도 정상 동작해야 한다.
            previousTargetTexture       = panelSettings.targetTexture;
            previousForceGammaRendering = panelSettings.forceGammaRendering;
            isPanelOwner                = true;

            PrepareBlitMaterial();
            EnsureRenderTexture();
            ApplyPanelOverrides();

            // 첫 인스턴스가 등록될 때만 SRP 콜백을 구독한다 (중복 구독 방지).
            // 마지막 인스턴스가 Release 될 때 Release() 에서 해지한다.
            if (ManagedBlitters.Count == 1)
            {
                RenderPipelineManager.endCameraRendering += HandleEndCameraRendering;
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 매 프레임 호출 — PanelSettings 변경/유효성 점검 후 RT 갱신 및 오버라이드 재적용.
        /// </summary>
        // ------------------------------------------------------------
        public void Refresh(PanelSettings currentPanelSettings, Object warnContext)
        {
            // PanelSettings 가 런타임에 교체되거나 무효 상태가 되면 점유를 해제하고 재인수.
            // 이전 panel 의 targetTexture/forceGammaRendering 을 원래 상태로 돌려놔야 누수가 없다.
            if (isPanelOwner)
            {
                if (currentPanelSettings != managedPanelSettings || GetUnsupportedReason(currentPanelSettings) != null)
                {
                    Release();
                }
            }

            if (!isPanelOwner)
            {
                TryAcquire(currentPanelSettings, warnContext);
                return;
            }

            // 화면 해상도 변동 추적 — 변동 시 RT 를 새 크기로 재생성한다.
            EnsureRenderTexture();
            ApplyPanelOverrides();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 점유 해제하고 PanelSettings 를 원래 상태로 복원한다.
        /// </summary>
        // ------------------------------------------------------------
        public void Release()
        {
            if (isPanelOwner && managedPanelSettings != null)
            {
                // 점유자가 본인일 때만 복원 — TryAcquire 직후 다른 인스턴스가 덮어쓴 경우 우리 책임 아님.
                if (PanelOwners.TryGetValue(managedPanelSettings, out GammaRTBlitter owner) && owner == this)
                {
                    PanelOwners.Remove(managedPanelSettings);

                    // 인수 시점의 원본 값으로 복원 — PanelSettings 는 에셋이라 변경이 영구히 남는다.
                    managedPanelSettings.targetTexture       = previousTargetTexture;
                    managedPanelSettings.forceGammaRendering = previousForceGammaRendering;
                }

                ManagedBlitters.Remove(this);
            }

            // 남은 인스턴스 0 개일 때만 SRP 콜백을 해지 — 이벤트 누수 방지.
            if (ManagedBlitters.Count == 0)
            {
                RenderPipelineManager.endCameraRendering -= HandleEndCameraRendering;
            }

            isPanelOwner          = false;
            managedPanelSettings  = null;
            previousTargetTexture = null;

            ReleaseRenderTexture();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// OnValidate 등 외부에서 오버라이드만 재적용해야 할 때 호출.
        /// </summary>
        // ------------------------------------------------------------
        public void ApplyPanelOverridesIfOwner()
        {
            if (isPanelOwner)
            {
                ApplyPanelOverrides();
            }
        }

    #endregion

    #region 내부 메서드

        // ------------------------------------------------------------
        /// <summary>
        /// PanelSettings 가 합성 대상으로 적합한지 검사한다.
        /// </summary>
        // ------------------------------------------------------------
        private string GetUnsupportedReason(PanelSettings panelSettings)
        {
            if (panelSettings == null)
            {
                return "[GammaRTBlitter] Assign PanelSettings before enabling managed compositing.";
            }

            return null;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 동일 메시지 중복 출력 방지 + 컨텍스트 제공.
        /// </summary>
        // ------------------------------------------------------------
        private void WarnOnce(string message, Object warnContext)
        {
            if (lastValidationWarning == message) return;

            lastValidationWarning = message;

            Debug.LogWarning(message, warnContext);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 화면 해상도에 맞춰 RT 를 (재)생성한다.
        /// </summary>
        // ------------------------------------------------------------
        private void EnsureRenderTexture()
        {
            int width  = Mathf.Max(1, Screen.width);
            int height = Mathf.Max(1, Screen.height);

            if (targetTexture != null && lastWidth == width && lastHeight == height) return;

            ReleaseRenderTexture();

            // ReadWrite.Linear: PanelSettings 의 forceGammaRendering 가 sRGB 데이터를 그대로 쓰도록 강제하므로
            //                  RT 도 linear 해석 없이 raw 바이트를 받아야 blit 시 SRGBToLinear 가 정확히 작동한다.
            // Point filter:    UI 텍스트 픽셀 정합성 확보 (Bilinear 시 글자 가장자리 흐림).
            // HideAndDontSave: 씬 저장/Inspector 노출 방지 — 런타임 전용 자원이라 직렬화 불필요.
            targetTexture = new RenderTexture(width, height, 32, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear)
            {
                name             = $"{HostName}_ManagedGammaPanelRT",
                filterMode       = FilterMode.Point,
                wrapMode         = TextureWrapMode.Clamp,
                autoGenerateMips = false,
                useMipMap        = false,
                hideFlags        = HideFlags.HideAndDontSave,
            };

            targetTexture.Create();

            lastWidth  = width;
            lastHeight = height;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// PanelSettings 의 targetTexture / forceGammaRendering 를 강제 적용한다.
        /// </summary>
        // ------------------------------------------------------------
        private void ApplyPanelOverrides()
        {
            if (!isPanelOwner || managedPanelSettings == null || targetTexture == null) return;

            if (managedPanelSettings.targetTexture != targetTexture)
            {
                managedPanelSettings.targetTexture = targetTexture;
            }

            if (managedPanelSettings.forceGammaRendering != ForceGammaRendering)
            {
                managedPanelSettings.forceGammaRendering = ForceGammaRendering;
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// RT 를 안전하게 해제한다 (Editor/Play 모두).
        /// </summary>
        // ------------------------------------------------------------
        private void ReleaseRenderTexture()
        {
            if (targetTexture == null) return;

            targetTexture.Release();

            if (Application.isPlaying)
            {
                Object.Destroy(targetTexture);
            }
            else
            {
                Object.DestroyImmediate(targetTexture);
            }

            targetTexture = null;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// blit 머티리얼을 한 번만 생성한다. 셰이더 누락 시 경고 1회.
        /// </summary>
        // ------------------------------------------------------------
        private static void PrepareBlitMaterial()
        {
            if (blitMaterial != null) return;

            Shader shader = Shader.Find(BLIT_SHADER_NAME);

            if (shader == null)
            {
                if (!hasMissingShaderWarning)
                {
                    Debug.LogWarning($"[GammaRTBlitter] Shader not found: {BLIT_SHADER_NAME}");

                    hasMissingShaderWarning = true;
                }
                return;
            }

            blitMaterial = new Material(shader)
            {
                hideFlags = HideFlags.HideAndDontSave,
            };
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 합성 가능한 상태인지 (활성 + 점유 + RT 생성됨).
        /// </summary>
        // ------------------------------------------------------------
        private bool IsReadyToComposite()
        {
            return isPanelOwner
                && managedPanelSettings != null
                && targetTexture        != null
                && targetTexture.IsCreated();
        }

        private float GetPanelSortOrder()
        {
            return managedPanelSettings != null ? managedPanelSettings.sortingOrder : 0f;
        }

    #endregion

    #region 정적 — 카메라 합성

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> 모든 등록된 blitter 를 순회하며 카메라 출력 위에 RT 를 blit 합성한다.
        /// <br/> Game / SceneView 카메라에서만 동작한다 (Edit/Play 공통).
        /// </summary>
        // ------------------------------------------------------------
        private static void HandleEndCameraRendering(ScriptableRenderContext context, Camera camera)
        {
            if (ManagedBlitters.Count == 0) return;

            if (camera.cameraType != CameraType.Game && camera.cameraType != CameraType.SceneView) return;

            PrepareBlitMaterial();

            if (blitMaterial == null) return;

            ManagedBlitters.Sort(static (a, b) =>
            {
                int panelOrderCompare = a.GetPanelSortOrder().CompareTo(b.GetPanelSortOrder());

                if (panelOrderCompare != 0) return panelOrderCompare;

                int documentOrderCompare = a.HostDocumentSortOrder.CompareTo(b.HostDocumentSortOrder);

                if (documentOrderCompare != 0) return documentOrderCompare;

                return a.GetHashCode().CompareTo(b.GetHashCode());
            });

            RenderTargetIdentifier target = camera.targetTexture != null
                ? new RenderTargetIdentifier(camera.targetTexture)
                : BuiltinRenderTextureType.CameraTarget;

            CommandBuffer commandBuffer = new CommandBuffer { name = "UI Toolkit Managed Gamma Panels" };
            commandBuffer.SetRenderTarget(target);

            foreach (GammaRTBlitter blitter in ManagedBlitters)
            {
                if (!blitter.IsReadyToComposite()) continue;

                commandBuffer.SetGlobalTexture(BLIT_TEXTURE_ID,    blitter.targetTexture);
                commandBuffer.SetGlobalVector (BLIT_SCALE_BIAS_ID, new Vector4(1f, -1f, 0f, 1f));

                commandBuffer.DrawProcedural(Matrix4x4.identity, blitMaterial, 1, MeshTopology.Triangles, 3);
            }

            context.ExecuteCommandBuffer(commandBuffer);
            context.Submit();

            commandBuffer.Release();
        }

    #endregion

    }
}
