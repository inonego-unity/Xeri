/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TransformProjectionAnchor.cs
수정일 : 2026-07-29

# 설명
Unity Transform의 현재 World 위치를 UI Projection Anchor로 제공한다.
========================================================================= BLOCK_HEADER_END */

using System;

using UnityEngine;

namespace inonego.Xeri.UI.Game
{
    // ============================================================
    /// <summary>
    /// Transform 기반 Projection Anchor.
    /// </summary>
    // ============================================================
    public sealed class TransformProjectionAnchor : IProjectionAnchor
    {
    #region 필드

        private readonly Transform target = null;
        private readonly Vector3 offset = Vector3.zero;

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// Transform과 World offset으로 Anchor를 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        public TransformProjectionAnchor
        (
            Transform target,
            Vector3 offset = default
        ) : base()
        {
            this.target = target ?? throw new ArgumentNullException(nameof(target));
            this.offset = offset;
        }

    #endregion

    #region IProjectionAnchor

        // ------------------------------------------------------------
        /// <summary>
        /// 살아 있는 Transform의 현재 World 위치를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public bool TryGetWorldPosition(out Vector3 position)
        {
            if (target == null)
            {
                position = default;
                return false;
            }

            position = target.position + offset;
            return true;
        }

    #endregion

    }
}
