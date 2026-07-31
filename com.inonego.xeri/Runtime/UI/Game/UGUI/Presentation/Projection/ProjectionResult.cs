/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : ProjectionResult.cs
수정일 : 2026-07-31

# 설명
World Anchor를 현재 Camera와 Layer RectTransform에 투영한 결과를 정의한다.
========================================================================= BLOCK_HEADER_END */

using UnityEngine;

namespace inonego.Xeri.UI.Game
{
    // ============================================================
    /// <summary>
    /// UI Projection 결과.
    /// </summary>
    // ============================================================
    public readonly struct ProjectionResult
    {
    #region 필드

        // ------------------------------------------------------------
        /// <summary>
        /// Projection이 성공했는지 여부.
        /// </summary>
        // ------------------------------------------------------------
        public bool Succeeded { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// Layer RectTransform 기준 로컬 위치.
        /// </summary>
        // ------------------------------------------------------------
        public Vector2 LocalPosition { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// World Anchor가 Camera 뒤에 있는지 여부.
        /// </summary>
        // ------------------------------------------------------------
        public bool IsBehindCamera { get; }

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// UI Projection 결과를 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        public ProjectionResult
        (
            bool succeeded,
            Vector2 localPosition,
            bool isBehindCamera
        ) : this()
        {
            Succeeded = succeeded;
            LocalPosition = localPosition;
            IsBehindCamera = isBehindCamera;
        }

    #endregion

    }
}
