/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : XeriUIHDRPCompositePostProcess.cs
수정일 : 2026-08-24

# 설명
AfterPostProcess에서 FP16 Screen Overlay Surface를 HDRP Scene Color에 합성한다.

# 특이사항, 제약사항
UI Surface는 Linear Premultiplied Alpha 계약이며 UI가 준비되지 않은 Camera에서는 Source를 그대로 복사한다.
========================================================================= BLOCK_HEADER_END */

using System;

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace inonego.Xeri.UI.Game
{
    // ================================================================================
    /// <summary>
    /// Post Process가 끝난 Scene 위에 Xeri FP16 Overlay Surface를 합성한다.
    /// </summary>
    // ================================================================================
    [Serializable]
    [SupportedOnRenderPipeline(typeof(HDRenderPipelineAsset))]
    [VolumeComponentMenu("Xeri/Game UI HDRP Composite")]
    public sealed class XeriUIHDRPCompositePostProcess :
        CustomPostProcessVolumeComponent,
        IPostProcessComponent
    {
        
    #region 필드

        private const string SHADER_NAME = "Hidden/XeriUI/HDRPOverlayComposite";

        private static readonly int UI_TEXTURE_ID = Shader.PropertyToID("_XeriUITexture");
        private static readonly int UI_ENABLED_ID = Shader.PropertyToID("_XeriUIEnabled");
        private static readonly int UI_VIEWPORT_PARAMS_ID = Shader.PropertyToID("_XeriUIViewportParams");

        private static bool isReady = false;

        private Material material = null;

    #endregion

    #region HDRP 설정과 상태

        // ------------------------------------------------------------
        /// <summary>
        /// HDRP가 이 Custom Post Process를 실제 목록에서 Setup했는지 여부.
        /// </summary>
        // ------------------------------------------------------------
        internal static bool IsReady => isReady;

        // ------------------------------------------------------------
        /// <summary>
        /// UI 합성을 Tone Mapping 뒤, HDRP Final Output 전에 수행한다.
        /// </summary>
        // ------------------------------------------------------------
        public override CustomPostProcessInjectionPoint injectionPoint =>
            CustomPostProcessInjectionPoint.AfterPostProcess;

        // ------------------------------------------------------------
        /// <summary>
        /// Scene View에는 Game UI Overlay를 합성하지 않는다.
        /// </summary>
        // ------------------------------------------------------------
        public override bool visibleInSceneView => false;

    #endregion

    #region HDRP 합성 수명

        // ------------------------------------------------------------
        /// <summary>
        /// HDRP 합성 Material을 준비하고 routing 가능 상태를 공개한다.
        /// </summary>
        // ------------------------------------------------------------
        public override void Setup()
        {
            var shader = Shader.Find(SHADER_NAME);
            if (shader == null)
            {
                Debug.LogError($"Xeri HDRP UI 합성 Shader '{SHADER_NAME}'를 찾을 수 없습니다.");
                isReady = false;
                return;
            }

            material = new Material(shader)
            {
                name = "Xeri Game UI HDRP Composite",
                hideFlags = HideFlags.HideAndDontSave,
            };
            isReady = true;
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// 현재 Scene Color와 같은 프레임의 FP16 Overlay Surface를 Destination에 합성한다.
        /// </summary>
        // ----------------------------------------------------------------------
        public override void Render
        (
            CommandBuffer cmd,
            HDCamera camera,
            RTHandle source,
            RTHandle destination
        )
        {
            if (material == null)
            {
                return;
            }

            var hasSurface = XeriUIHDRPAdapter.TryGetCapturedSurface(camera, out var surface);
            var width = Mathf.Max(1, camera.actualWidth);
            var height = Mathf.Max(1, camera.actualHeight);

            // 같은 Shader로 미대상 Camera의 Source 복사까지 처리해 별도 Blit 경로와 상태 차이를 만들지 않는다.
            material.SetFloat(UI_ENABLED_ID, hasSurface ? 1.0f : 0.0f);
            material.SetTexture(UI_TEXTURE_ID, hasSurface ? surface : Texture2D.blackTexture);
            material.SetVector
            (
                UI_VIEWPORT_PARAMS_ID,
                new Vector4(width, height, 1.0f / width, 1.0f / height)
            );

            HDUtils.DrawFullScreen
            (
                cmd,
                material,
                destination,
                shaderPassId: 0
            );
        }

        // ------------------------------------------------------------
        /// <summary>
        /// HDRP 합성 Material과 routing 준비 상태를 해제한다.
        /// </summary>
        // ------------------------------------------------------------
        public override void Cleanup()
        {
            isReady = false;

            if (material == null) return;

            CoreUtils.Destroy(material);
            material = null;
        }

    #endregion

    #region 합성 활성 상태

        // ------------------------------------------------------------
        /// <summary>
        /// Game UI HDRP Adapter가 살아 있고 합성 Material이 준비됐을 때 실행한다.
        /// </summary>
        // ------------------------------------------------------------
        public bool IsActive()
        {
            return XeriUIHDRPAdapter.IsActive && material != null;
        }

    #endregion

    }
}
