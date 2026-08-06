/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : ZoneLinkTraversalTrigger.cs
수정일 : 2026-08-05

# 설명
Zone Link 이동 경계의 Trigger와 선택적 물리 Collider를 Zone Link 통행 상태에 연결한다.

# 제약사항
Link가 통과 가능해도 Actor의 실제 위치·애니메이션·문 외형은 변경하지 않는다.
========================================================================= BLOCK_HEADER_END */

using System;

using UnityEngine;

namespace inonego.Xeri.Game
{
    // ============================================================
    /// <summary>
    /// ZoneTraveler가 Link 경계를 넘을 때 Zone Graph 이동을 요청하는 공간 Trigger다.
    /// </summary>
    // ============================================================
    public sealed class ZoneLinkTraversalTrigger : MonoBehaviour
    {
    #region 필드

        // ------------------------------------------------------------
        /// <summary>
        /// 이동 감지에 사용할 Trigger Collider다.
        /// </summary>
        // ------------------------------------------------------------
        [SerializeField]
        private Collider traversalTrigger = null;

        // ------------------------------------------------------------
        /// <summary>
        /// 이 경계가 통과시킬 실제 Zone Link다.
        /// </summary>
        // ------------------------------------------------------------
        [SerializeField]
        private ZoneLink zoneLink = null;

        // ------------------------------------------------------------
        /// <summary>
        /// Link가 막힌 동안에만 켤 선택적 물리 Collider다.
        /// </summary>
        // ------------------------------------------------------------
        [SerializeField]
        private Collider blockingCollider = null;

    #endregion

    #region 유니티 이벤트

        // ------------------------------------------------------------
        /// <summary>
        /// 경계 Collider 설정을 확인하고 Link 상태 변화를 구독한다.
        /// </summary>
        // ------------------------------------------------------------
        private void OnEnable()
        {
            ValidateConfiguration();

            zoneLink.OnPassabilityChanged += HandlePassabilityChanged;
            ApplyPassability();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 비활성화된 경계가 Link 이벤트를 계속 수신하지 않게 한다.
        /// </summary>
        // ------------------------------------------------------------
        private void OnDisable()
        {
            if (zoneLink != null)
            {
                zoneLink.OnPassabilityChanged -= HandlePassabilityChanged;
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// ZoneTraveler가 경계를 넘었을 때 현재 Zone Graph에 Link 통과를 요청한다.
        /// </summary>
        // ------------------------------------------------------------
        private void OnTriggerEnter(Collider other)
        {
            var traveler = other.GetComponentInParent<ZoneTraveler>();

            if (traveler == null)
            {
                return;
            }

            // 실제 위치 이동은 Actor가 이미 수행했고, 여기서는 공간 상태만 같은 Link로 확정한다.
            traveler.TryTraverse(zoneLink);
        }

    #endregion

    #region 이벤트 핸들러

        // ------------------------------------------------------------
        /// <summary>
        /// 통행 제한 변화에 맞춰 선택한 물리 Collider만 켜거나 끈다.
        /// </summary>
        // ------------------------------------------------------------
        private void HandlePassabilityChanged(ZoneLink link)
        {
            ApplyPassability();
        }

    #endregion

    #region 메서드

        // ------------------------------------------------------------
        /// <summary>
        /// Link 통과 상태를 물리 Collider 상태로 반영한다.
        /// </summary>
        // ------------------------------------------------------------
        private void ApplyPassability()
        {
            if (blockingCollider != null)
            {
                blockingCollider.enabled = !zoneLink.IsPassable;
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Trigger와 차단 Collider가 서로 다른 목적에 맞게 설정됐는지 확인한다.
        /// </summary>
        // ------------------------------------------------------------
        private void ValidateConfiguration()
        {
            if (traversalTrigger == null || !traversalTrigger.isTrigger)
            {
                throw new InvalidOperationException("ZoneLinkTraversalTrigger에는 Is Trigger Collider가 필요합니다.");
            }

            if (zoneLink == null)
            {
                throw new InvalidOperationException("ZoneLinkTraversalTrigger에 Zone Link가 설정되어 있지 않습니다.");
            }

            if (blockingCollider != null && blockingCollider.isTrigger)
            {
                throw new InvalidOperationException("ZoneLinkTraversalTrigger의 Blocking Collider는 Trigger가 아니어야 합니다.");
            }
        }

    #endregion
    }
}
