/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : XeriUIHDRPIntegration.cs
수정일 : 2026-08-24

# 설명
HDRP package가 존재할 때 공통 Game UI Render Pipeline Adapter Registry에 HDRP Adapter Resolver를 연결한다.
========================================================================= BLOCK_HEADER_END */

using System;

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace inonego.Xeri.UI.Game
{
    // ================================================================================
    /// <summary>
    /// HDRP optional Assembly를 공통 Game UI Render Pipeline Adapter Registry에 연결한다.
    /// </summary>
    // ================================================================================
    internal static class XeriUIHDRPIntegration
    {

    #region 연결

        // ----------------------------------------------------------------------
        /// <summary>
        /// Assembly 로드 뒤 HDRP Adapter Resolver를 공통 Registry에 등록한다.
        /// </summary>
        // ----------------------------------------------------------------------
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        private static void Register()
        {
            GameUIRenderPipelineAdapterRegistry.Register(Acquire);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 활성 Pipeline이 HDRP일 때만 HDRP UI Adapter를 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        private static IDisposable Acquire()
        {
            if (GraphicsSettings.currentRenderPipeline is not HDRenderPipelineAsset)
            {
                return null;
            }

            return new XeriUIHDRPAdapter();
        }

    #endregion

    }
}
