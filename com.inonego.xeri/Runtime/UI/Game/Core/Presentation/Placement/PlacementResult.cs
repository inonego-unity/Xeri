/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : PlacementResult.cs
수정일 : 2026-07-29

# 설명
UI Placement의 최종 로컬 위치와 clamp 적용 여부를 정의한다.
========================================================================= BLOCK_HEADER_END */

using UnityEngine;

namespace inonego.Xeri.UI.Game
{
    // ============================================================
    /// <summary>
    /// UI Placement 계산 결과.
    /// </summary>
    // ============================================================
    public readonly struct PlacementResult
    {
    #region 필드

        // ------------------------------------------------------------
        /// <summary>
        /// 계산된 로컬 위치.
        /// </summary>
        // ------------------------------------------------------------
        public Vector2 LocalPosition { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 배치 영역 clamp가 적용됐는지 여부.
        /// </summary>
        // ------------------------------------------------------------
        public bool WasClamped { get; }

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// UI Placement 결과를 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        public PlacementResult
        (
            Vector2 localPosition,
            bool wasClamped
        ) : this()
        {
            LocalPosition = localPosition;
            WasClamped = wasClamped;
        }

    #endregion

    }
}
