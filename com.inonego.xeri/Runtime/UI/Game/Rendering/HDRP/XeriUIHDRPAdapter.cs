/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : XeriUIHDRPAdapter.cs
수정일 : 2026-08-24

# 설명
HDRP에서 Screen Overlay UI를 FP16 Surface로 우회하고 AfterPostProcess 합성까지의 수명을 소유한다.

# 특이사항, 제약사항
기존 UGUI와 Gamma Composite UIDocument의 전역 sortingOrder를 Unity Overlay RendererList가 그대로 사용한다.
Composite Post Process가 실제 준비된 프레임부터만 HDRP 기본 Overlay 출력을 차단한다.
========================================================================= BLOCK_HEADER_END */

using System;

using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace inonego.Xeri.UI.Game
{
    // ================================================================================
    /// <summary>
    /// <br/> HDRP Screen Overlay ownership과 FP16 UI Surface 수명을 관리한다.
    /// <br/> 기존 UI 입력·정렬·Gamma Panel 계약은 변경하지 않는다.
    /// </summary>
    // ================================================================================
    [Serializable]
    internal sealed class XeriUIHDRPAdapter : IDisposable
    {
        
    #region 필드

        private const int DEPTH_STENCIL_BITS = 24;
        private const GraphicsFormat COLOR_FORMAT = GraphicsFormat.R16G16B16A16_SFloat;

        private static XeriUIHDRPAdapter activeAdapter = null;

        private readonly XeriUIHDRPCapturePass capturePass = null;
        private readonly GameObject volumeObject = null;
        private readonly VolumeProfile volumeProfile = null;

        private RenderTexture uiSurface = null;
        private Camera targetCamera = null;
        private Camera routedCamera = null;
        private int routingFrame = -1;
        private int capturedFrame = -1;
        private bool isDisposed = false;

    #endregion

    #region 상태

        // ------------------------------------------------------------
        /// <summary>
        /// HDRP UI 합성 Adapter가 현재 살아 있는지 확인한다.
        /// </summary>
        // ------------------------------------------------------------
        internal static bool IsActive => activeAdapter != null && !activeAdapter.isDisposed;

    #endregion

    #region 생성자

        // ----------------------------------------------------------------------
        /// <summary>
        /// HDRP Overlay Capture Pass, Composite Volume과 ownership callback을 연결한다.
        /// </summary>
        // ----------------------------------------------------------------------
        internal XeriUIHDRPAdapter()
        {
            if (activeAdapter != null)
            {
                throw new InvalidOperationException("HDRP Game UI Adapter가 이미 활성화되어 있습니다.");
            }

            volumeProfile = CreateCompositeVolumeProfile();
            volumeObject = CreateCompositeVolume(volumeProfile);
            capturePass = new XeriUIHDRPCapturePass(this)
            {
                name = "Xeri Game UI Overlay Capture",
            };

            try
            {
                RegisterCapturePass();
                RenderPipelineManager.beginCameraRendering += HandleBeginCameraRendering;
                RenderPipelineManager.endCameraRendering += HandleEndCameraRendering;
                activeAdapter = this;
            }
            catch
            {
                ReleaseVolume();
                throw;
            }
        }

    #endregion

    #region 합성 Volume 수명

        // ----------------------------------------------------------------------
        /// <summary>
        /// Custom Post Process가 Volume Stack에 존재하도록 숨김 Global Volume Profile을 만든다.
        /// </summary>
        // ----------------------------------------------------------------------
        private static VolumeProfile CreateCompositeVolumeProfile()
        {
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            profile.name = "Xeri Game UI HDRP Composite Profile";
            profile.hideFlags = HideFlags.HideAndDontSave;

            var component = profile.Add<XeriUIHDRPCompositePostProcess>(true);
            component.active = true;

            return profile;
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// Layer 0의 숨김 Global Volume으로 Composite Post Process를 모든 Game Camera Stack에 제공한다.
        /// </summary>
        // ----------------------------------------------------------------------
        private static GameObject CreateCompositeVolume(VolumeProfile profile)
        {
            var host = new GameObject("Xeri Game UI HDRP Composite Volume")
            {
                hideFlags = HideFlags.HideAndDontSave,
                layer = 0,
            };

            var volume = host.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = float.MaxValue;
            volume.weight = 1.0f;
            volume.sharedProfile = profile;

            UnityEngine.Object.DontDestroyOnLoad(host);
            return host;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 숨김 Global Volume과 Profile을 해제한다.
        /// </summary>
        // ------------------------------------------------------------
        private void ReleaseVolume()
        {
            if (volumeObject != null)
            {
                UnityEngine.Object.Destroy(volumeObject);
            }

            if (volumeProfile != null)
            {
                UnityEngine.Object.Destroy(volumeProfile);
            }
        }

    #endregion

    #region Overlay 캡처 수명

        // ------------------------------------------------------------
        /// <summary>
        /// Scene 오브젝트 없이 HDRP Global Custom Pass를 등록한다.
        /// </summary>
        // ------------------------------------------------------------
        private void RegisterCapturePass()
        {
            if
            (
                !CustomPassVolume.RegisterUniqueGlobalCustomPass
                (
                    CustomPassInjectionPoint.BeforePostProcess,
                    capturePass,
                    float.MaxValue
                )
            )
            {
                throw new InvalidOperationException("Xeri HDRP UI Capture Pass를 등록할 수 없습니다.");
            }
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// Overlay RendererList를 받을 FP16 Surface를 현재 Display 해상도에 맞춰 보장한다.
        /// </summary>
        // ----------------------------------------------------------------------
        internal RenderTexture EnsureSurface()
        {
            var width = Mathf.Max(1, Screen.width);
            var height = Mathf.Max(1, Screen.height);

            if
            (
                uiSurface != null &&
                uiSurface.width == width &&
                uiSurface.height == height &&
                uiSurface.IsCreated()
            )
            {
                return uiSurface;
            }

            ReleaseSurface();
            uiSurface = new RenderTexture
            (
                width,
                height,
                DEPTH_STENCIL_BITS,
                COLOR_FORMAT
            )
            {
                name = "Xeri_GameUI_HDRP_Overlay_FP16",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                autoGenerateMips = false,
                useMipMap = false,
                hideFlags = HideFlags.HideAndDontSave,
            };
            uiSurface.Create();

            return uiSurface;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 routing Camera의 Overlay Surface가 이번 프레임에 완성됐음을 기록한다.
        /// </summary>
        // ------------------------------------------------------------
        internal void MarkCaptured(Camera camera)
        {
            if (!IsRoutingFrame(camera)) return;

            capturedFrame = Time.frameCount;
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// 현재 HDCamera에 합성할 같은 프레임의 FP16 Overlay Surface를 반환한다.
        /// </summary>
        // ----------------------------------------------------------------------
        internal static bool TryGetCapturedSurface
        (
            HDCamera hdCamera,
            out RenderTexture surface
        )
        {
            surface = null;

            var adapter = activeAdapter;
            if (adapter == null || adapter.isDisposed || hdCamera == null) return false;

            var camera = hdCamera.camera;
            if (camera == null || !adapter.IsRoutingFrame(camera)) return false;
            if (adapter.capturedFrame != Time.frameCount) return false;
            if (adapter.uiSurface == null || !adapter.uiSurface.IsCreated()) return false;

            surface = adapter.uiSurface;
            return true;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 FP16 Overlay Surface를 해제한다.
        /// </summary>
        // ------------------------------------------------------------
        private void ReleaseSurface()
        {
            if (uiSurface == null) return;

            uiSurface.Release();
            UnityEngine.Object.Destroy(uiSurface);
            uiSurface = null;
            capturedFrame = -1;
        }

    #endregion

    #region 카메라 라우팅

        // ----------------------------------------------------------------------
        /// <summary>
        /// 현재 Camera가 이번 프레임에 Xeri Overlay routing을 소유하는지 확인한다.
        /// </summary>
        // ----------------------------------------------------------------------
        internal bool IsRoutingFrame(Camera camera)
        {
            if (camera == null) return false;

            return
                routingFrame == Time.frameCount &&
                ReferenceEquals(routedCamera, camera);
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// 현재 Runtime이 사용할 Screen Game Camera를 고정하고 대상 여부를 확인한다.
        /// </summary>
        // ----------------------------------------------------------------------
        private bool ShouldHandleCamera(Camera camera)
        {
            if
            (
                camera == null ||
                camera.cameraType != CameraType.Game ||
                camera.targetTexture != null ||
                camera.targetDisplay != 0
            )
            {
                return false;
            }

            var mainCamera = Camera.main;
            if
            (
                mainCamera != null &&
                mainCamera.cameraType == CameraType.Game &&
                mainCamera.targetTexture == null &&
                mainCamera.targetDisplay == 0
            )
            {
                targetCamera = mainCamera;
            }
            else if (targetCamera == null || !targetCamera.isActiveAndEnabled)
            {
                targetCamera = camera;
            }

            return ReferenceEquals(camera, targetCamera);
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> HDRP RenderGraph 기록 전에 기본 Screen Overlay pass 기록을 차단한다.
        /// <br/> Composite Post Process가 준비되지 않았으면 기존 Overlay 경로를 유지한다.
        /// </summary>
        // ----------------------------------------------------------------------
        private void HandleBeginCameraRendering
        (
            ScriptableRenderContext context,
            Camera camera
        )
        {
            if (isDisposed || !ShouldHandleCamera(camera)) return;
            if (!XeriUIHDRPCompositePostProcess.IsReady) return;

            routingFrame = Time.frameCount;
            routedCamera = camera;
            capturedFrame = -1;
            SupportedRenderingFeatures.active.rendersUIOverlay = false;
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// Camera 종료 시 다음 Render Pipeline 평가를 위해 Overlay ownership을 기본 상태로 복구한다.
        /// </summary>
        // ----------------------------------------------------------------------
        private void HandleEndCameraRendering
        (
            ScriptableRenderContext context,
            Camera camera
        )
        {
            if (isDisposed || !ReferenceEquals(camera, routedCamera)) return;

            SupportedRenderingFeatures.active.rendersUIOverlay = true;
            routedCamera = null;
            routingFrame = -1;
        }

    #endregion

    #region Adapter 정리

        // ------------------------------------------------------------
        /// <summary>
        /// Global Capture Pass, callback, Volume과 FP16 Surface를 해제한다.
        /// </summary>
        // ------------------------------------------------------------
        public void Dispose()
        {
            if (isDisposed) return;

            isDisposed = true;
            RenderPipelineManager.beginCameraRendering -= HandleBeginCameraRendering;
            RenderPipelineManager.endCameraRendering -= HandleEndCameraRendering;

            SupportedRenderingFeatures.active.rendersUIOverlay = true;

            if (capturePass != null)
            {
                CustomPassVolume.UnregisterGlobalCustomPass(capturePass);
            }

            ReleaseSurface();
            ReleaseVolume();
            targetCamera = null;
            routedCamera = null;
            routingFrame = -1;

            if (ReferenceEquals(activeAdapter, this))
            {
                activeAdapter = null;
            }
        }

    #endregion

    }
}
