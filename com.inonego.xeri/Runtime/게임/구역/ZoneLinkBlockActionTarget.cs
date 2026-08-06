/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : ZoneLinkBlockActionTarget.cs
수정일 : 2026-08-05

# 설명
ZoneLinkBlocker의 제한 상태 변경을 Xeri Reaction Action Target으로 노출한다.

# 제약사항
프로젝트 도메인의 판정·행동은 소유하지 않는다.
========================================================================= BLOCK_HEADER_END */

using System;

using UnityEngine;

using inonego.Xeri;

namespace inonego.Xeri.Game
{
    // ============================================================
    /// <summary>
    /// Reaction Action이 요청할 Zone Link 제한 상태다.
    /// </summary>
    // ============================================================
    public enum ZoneLinkBlockState
    {
        Blocked   = 0,
        Passable  = 1,
    }

    // ============================================================
    /// <summary>
    /// 하나의 ZoneLinkBlocker 상태 전환을 ReactionBinding Target으로 제공하는 Adapter다.
    /// </summary>
    // ============================================================
    public sealed class ZoneLinkBlockActionTarget : MonoBehaviour, IActionTarget
    {
    #region 필드

        // ------------------------------------------------------------
        /// <summary>
        /// 제한 상태를 실제로 보유하는 Blocker다.
        /// </summary>
        // ------------------------------------------------------------
        [SerializeField]
        private ZoneLinkBlocker blocker = null;

        // ------------------------------------------------------------
        /// <summary>
        /// Signal 수신 시 이 Target이 만들 상태다.
        /// </summary>
        // ------------------------------------------------------------
        [SerializeField]
        private ZoneLinkBlockState targetState = ZoneLinkBlockState.Blocked;

    #endregion

    #region 메서드

        // ------------------------------------------------------------
        /// <summary>
        /// 설정된 제한 상태를 적용한다.
        /// </summary>
        // ------------------------------------------------------------
        public bool TryExecute(ReactionContext context)
        {
            if (blocker == null)
            {
                return false;
            }

            return blocker.SetBlocked(targetState == ZoneLinkBlockState.Blocked);
        }

    #endregion
    }
}
