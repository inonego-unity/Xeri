/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : IProjectionAnchor.cs
수정일 : 2026-07-31

# 설명
Projection 시점에 현재 World 위치를 제공하는 Anchor 계약을 정의한다.
========================================================================= BLOCK_HEADER_END */

using UnityEngine;

namespace inonego.Xeri.UI.Game
{
    // ============================================================
    /// <summary>
    /// UI Projection이 소비하는 World Anchor.
    /// </summary>
    // ============================================================
    public interface IProjectionAnchor
    {
        // ------------------------------------------------------------
        /// <summary>
        /// 현재 World 위치를 조회한다.
        /// </summary>
        // ------------------------------------------------------------
        bool TryGetWorldPosition(out Vector3 position);
    }
}
