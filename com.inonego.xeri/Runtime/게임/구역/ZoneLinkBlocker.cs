/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : ZoneLinkBlocker.cs
수정일 : 2026-08-05

# 설명
하나의 공간 원인이 Zone Link 통행 제한 Lease를 소유하도록 한다.

# 제약사항
제한 원인의 조건 판정과 어떤 Signal이 상태를 바꿀지는 소유하지 않는다.
========================================================================= BLOCK_HEADER_END */

using System;

using UnityEngine;

namespace inonego.Xeri.Game
{
    // ============================================================
    /// <summary>
    /// Zone Link에 대한 하나의 독립 통행 제한 원인을 보유한다.
    /// </summary>
    // ============================================================
    public sealed class ZoneLinkBlocker : MonoBehaviour
    {
    #region 필드

        // ------------------------------------------------------------
        /// <summary>
        /// 이 원인이 제한을 추가할 Zone Link다.
        /// </summary>
        // ------------------------------------------------------------
        [SerializeField]
        private ZoneLink zoneLink = null;

        // ------------------------------------------------------------
        /// <summary>
        /// Link 진단에 남길 이 제한 원인의 이름이다.
        /// </summary>
        // ------------------------------------------------------------
        [SerializeField]
        private string cause = "";

        // ------------------------------------------------------------
        /// <summary>
        /// 이 Component가 활성화될 때 제한 상태로 시작할지 나타낸다.
        /// </summary>
        // ------------------------------------------------------------
        [SerializeField]
        private bool startsBlocked = false;

        // ------------------------------------------------------------
        /// <summary>
        /// 이 Component가 현재 보유한 통행 제한 Lease다.
        /// </summary>
        // ------------------------------------------------------------
        public bool IsBlocked => blockLease != null;

        private ZoneLinkBlockLease blockLease = null;

    #endregion

    #region 유니티 이벤트

        // ------------------------------------------------------------
        /// <summary>
        /// 초기 통행 제한이 필요한 경우 이 Component 소유 Lease를 만든다.
        /// </summary>
        // ------------------------------------------------------------
        private void OnEnable()
        {
            ValidateConfiguration();

            if (startsBlocked)
            {
                SetBlocked(true);
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Component 수명이 끝나면 자신이 보유한 제한만 해제한다.
        /// </summary>
        // ------------------------------------------------------------
        private void OnDisable()
        {
            SetBlocked(false);
        }

    #endregion

    #region 메서드

        // ------------------------------------------------------------
        /// <summary>
        /// 이 원인이 보유한 Zone Link 제한 상태를 변경한다.
        /// </summary>
        // ------------------------------------------------------------
        public bool SetBlocked(bool blocked)
        {
            if (blocked)
            {
                if (blockLease != null)
                {
                    return true;
                }

                // 다른 원인의 제한과 독립되도록 이 Blocker 전용 Lease를 보관한다.
                blockLease = zoneLink.AcquireBlock(cause);
                return true;
            }

            if (blockLease == null)
            {
                return true;
            }

            // 이 Blocker가 만든 Lease만 해제해 다른 원인의 통행 제한을 보존한다.
            blockLease.Dispose();
            blockLease = null;
            return true;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Zone Link 참조와 진단용 제한 원인이 설정됐는지 확인한다.
        /// </summary>
        // ------------------------------------------------------------
        private void ValidateConfiguration()
        {
            if (zoneLink == null)
            {
                throw new InvalidOperationException("ZoneLinkBlocker에 Zone Link가 설정되어 있지 않습니다.");
            }

            if (string.IsNullOrWhiteSpace(cause))
            {
                throw new InvalidOperationException("ZoneLinkBlocker의 Cause를 비워 둘 수 없습니다.");
            }
        }

    #endregion
    }
}
