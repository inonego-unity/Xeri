/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : WorldProjectionAnchor.cs
수정일 : 2026-07-31

# 설명
고정 World 위치를 UI Projection Anchor로 제공한다.
========================================================================= BLOCK_HEADER_END */

using UnityEngine;

namespace inonego.Xeri.UI.Game
{
    // ============================================================
    /// <summary>
    /// 고정 World 위치 Projection Anchor.
    /// </summary>
    // ============================================================
    public sealed class WorldProjectionAnchor : IProjectionAnchor
    {
    #region 필드

        // ------------------------------------------------------------
        /// <summary>
        /// Projection할 World 위치.
        /// </summary>
        // ------------------------------------------------------------
        public Vector3 Position { get; set; }

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// 고정 World 위치 Anchor를 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        public WorldProjectionAnchor(Vector3 position) : base()
        {
            Position = position;
        }

    #endregion

    #region IProjectionAnchor

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 고정 World 위치를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public bool TryGetWorldPosition(out Vector3 position)
        {
            position = Position;
            return true;
        }

    #endregion

    }
}
