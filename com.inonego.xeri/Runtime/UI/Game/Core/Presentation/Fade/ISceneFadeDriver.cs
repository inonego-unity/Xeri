/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : ISceneFadeDriver.cs
수정일 : 2026-07-29

# 설명
Scene Fade 색상과 Alpha를 적용하는 backend 계약을 정의한다.
========================================================================= BLOCK_HEADER_END */

using UnityEngine;

namespace inonego.Xeri.UI.Game
{
    // ============================================================
    /// <summary>
    /// Scene Fade 표시 backend 계약.
    /// </summary>
    // ============================================================
    public interface ISceneFadeDriver : IPresentationTransitionTarget
    {
        // ------------------------------------------------------------
        /// <summary>
        /// 현재 Fade Alpha.
        /// </summary>
        // ------------------------------------------------------------
        float Alpha { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// Fade Overlay 색상을 적용한다.
        /// </summary>
        // ------------------------------------------------------------
        void SetColor(Color color);
    }
}
