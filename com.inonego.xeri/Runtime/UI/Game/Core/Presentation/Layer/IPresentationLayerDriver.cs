/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : IPresentationLayerDriver.cs
수정일 : 2026-07-29

# 설명
Presentation Layer의 실제 Root와 활성 상태를 다루는 backend 계약을 정의한다.
========================================================================= BLOCK_HEADER_END */

using UnityEngine;

namespace inonego.Xeri.UI.Game
{
    // ============================================================
    /// <summary>
    /// Presentation Layer backend 계약.
    /// </summary>
    // ============================================================
    public interface IPresentationLayerDriver
    {
        // ------------------------------------------------------------
        /// <summary>
        /// 표시 View를 배치할 Layer Root.
        /// </summary>
        // ------------------------------------------------------------
        Transform Root { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// backend 구성이 Layer Asset과 일치하는지 검증한다.
        /// </summary>
        // ------------------------------------------------------------
        bool Validate(PresentationLayerAsset asset, out string error);

        // ------------------------------------------------------------
        /// <summary>
        /// Layer Root의 활성 상태를 적용한다.
        /// </summary>
        // ------------------------------------------------------------
        void SetActive(bool active);
    }
}
