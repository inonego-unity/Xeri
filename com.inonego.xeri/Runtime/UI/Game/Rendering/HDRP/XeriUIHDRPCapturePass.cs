/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : XeriUIHDRPCapturePass.cs
수정일 : 2026-08-24

# 설명
Unity Screen Overlay RendererList를 기존 sortingOrder 그대로 FP16 UI Surface에 캡처하는 HDRP Custom Pass다.

# 특이사항, 제약사항
Capture 대상에는 UGUI Canvas와 Xeri GammaComposite UIDocument가 함께 포함된다.
========================================================================= BLOCK_HEADER_END */

using System;

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace inonego.Xeri.UI.Game
{
    // ============================================================
    /// <summary>
    /// 현재 Screen Overlay UI 전체를 FP16 Surface에 렌더링한다.
    /// </summary>
    // ============================================================
    [Serializable]
    internal sealed class XeriUIHDRPCapturePass : CustomPass
    {
        
    #region 필드

        private readonly XeriUIHDRPAdapter adapter = null;

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// Capture 결과를 소유할 HDRP Adapter를 연결한다.
        /// </summary>
        // ------------------------------------------------------------
        internal XeriUIHDRPCapturePass(XeriUIHDRPAdapter adapter)
        {
            this.adapter = adapter ?? throw new ArgumentNullException(nameof(adapter));
        }

    #endregion

    #region Overlay 캡처

        // ----------------------------------------------------------------------
        /// <summary>
        /// 현재 routing Camera의 Overlay RendererList를 투명 FP16 Surface에 기록한다.
        /// </summary>
        // ----------------------------------------------------------------------
        protected override void Execute(CustomPassContext ctx)
        {
            var camera = ctx.hdCamera?.camera;
            if (camera == null || !adapter.IsRoutingFrame(camera)) return;

            // RenderGraph 기록은 이미 끝났으므로 ownership을 복구해도 HDRP 기본 Overlay pass는 다시 생기지 않는다.
            SupportedRenderingFeatures.active.rendersUIOverlay = true;

            var surface = adapter.EnsureSurface();
            var rendererList = ctx.renderContext.CreateUIOverlayRendererList
            (
                camera,
                UISubset.All
            );
            if (!rendererList.isValid)
            {
                return;
            }

            // HDRP 자체 HDR UI 경로와 동일하게 Display 해상도 Viewport에서 투명 Surface를 새로 만든다.
            ctx.cmd.SetRenderTarget(surface);
            ctx.cmd.SetViewport(new Rect(0.0f, 0.0f, Screen.width, Screen.height));
            ctx.cmd.ClearRenderTarget(true, true, Color.clear);
            ctx.cmd.DrawRendererList(rendererList);

            adapter.MarkCaptured(camera);
        }

    #endregion

    }
}
