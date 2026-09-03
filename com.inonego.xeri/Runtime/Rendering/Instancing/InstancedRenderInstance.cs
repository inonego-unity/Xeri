/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : InstancedRenderInstance.cs
수정일 : 2026-09-03

# 설명
Graphics.RenderMeshInstanced에 전달할 per-instance transform과 rendering layer mask를 정의한다.

# 제약사항
Unity RenderMeshInstanced 계약이 요구하는 필드 이름을 유지한다.
========================================================================= BLOCK_HEADER_END */

using UnityEngine;

namespace inonego.Xeri.Rendering
{
    // ============================================================
    /// <summary>
    /// RenderMeshInstanced가 직접 소비하는 최소 per-instance 데이터.
    /// </summary>
    // ============================================================
    internal struct InstancedRenderInstance
    {

    #region 필드

        public Matrix4x4 objectToWorld;
        public uint renderingLayerMask;

    #endregion

    #region 생성자

        // --------------------------------------------------------------------------------
        /// <summary>
        /// Object-to-world matrix와 rendering layer mask로 instance 데이터를 만든다.
        /// </summary>
        // --------------------------------------------------------------------------------
        public InstancedRenderInstance
        (
            Matrix4x4 objectToWorld,
            uint renderingLayerMask
        )
        {
            this.objectToWorld = objectToWorld;
            this.renderingLayerMask = renderingLayerMask;
        }

    #endregion

    }
}
