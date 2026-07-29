/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : IOverlaySource.cs
수정일 : 2026-07-29

# 설명
Presentation Layer 아래에 Overlay View를 획득하고 반환하는 Source 계약을 정의한다.
========================================================================= BLOCK_HEADER_END */

using UnityEngine;

namespace inonego.Xeri.UI.Game
{
    // ============================================================
    /// <summary>
    /// Overlay View 획득과 반환을 소유하는 Source.
    /// </summary>
    // ============================================================
    public interface IOverlaySource<TView>
    where TView : class
    {
        // ------------------------------------------------------------
        /// <summary>
        /// 지정한 Layer Root 아래에 Overlay View를 획득한다.
        /// </summary>
        // ------------------------------------------------------------
        TView Acquire(Transform parent);

        // ------------------------------------------------------------
        /// <summary>
        /// Source가 획득한 Overlay View를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        void Release(TView view);
    }
}
