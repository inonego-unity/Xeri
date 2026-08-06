/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : ZoneLinkSignalSource.cs
수정일 : 2026-08-05

# 설명
Zone Link 통과 사실을 Xeri Reaction Signal로 변환한다.

# 제약사항
이동 제약의 원인·조건과 통과 뒤의 도메인 효과는 소유하지 않는다.
========================================================================= BLOCK_HEADER_END */

using System;

using UnityEngine;

using inonego.Xeri;

namespace inonego.Xeri.Game
{
    // ============================================================
    /// <summary>
    /// 확정된 Zone Link 통과를 ReactionBinding이 구독할 Signal로 변환하는 Adapter다.
    /// </summary>
    // ============================================================
    public sealed class ZoneLinkSignalSource : MonoBehaviour, ISignalSource
    {
    #region 필드

        // ------------------------------------------------------------
        /// <summary>
        /// 통과 사실을 제공하는 Zone Link다.
        /// </summary>
        // ------------------------------------------------------------
        [SerializeField]
        private ZoneLink zoneLink = null;

        // ------------------------------------------------------------
        /// <summary>
        /// Zone Link 통과가 확정됐을 때 전달된다.
        /// </summary>
        // ------------------------------------------------------------
        public event Action<ReactionContext> OnSignal = null;

    #endregion

    #region 유니티 이벤트

        // ------------------------------------------------------------
        /// <summary>
        /// Zone Link의 통과 확정 Event를 구독한다.
        /// </summary>
        // ------------------------------------------------------------
        private void OnEnable()
        {
            if (zoneLink == null)
            {
                throw new InvalidOperationException("ZoneLinkSignalSource에 Zone Link가 설정되어 있지 않습니다.");
            }

            zoneLink.OnTraversed += HandleTraversed;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 비활성화된 Adapter가 Link 통과를 계속 Signal로 전달하지 않게 한다.
        /// </summary>
        // ------------------------------------------------------------
        private void OnDisable()
        {
            if (zoneLink != null)
            {
                zoneLink.OnTraversed -= HandleTraversed;
            }
        }

    #endregion

    #region 이벤트 핸들러

        // ------------------------------------------------------------
        /// <summary>
        /// 확정된 Link 통과를 Link GameObject 기준 Reaction Signal로 전달한다.
        /// </summary>
        // ------------------------------------------------------------
        private void HandleTraversed(ZoneLink link, Zone source, Zone destination)
        {
            OnSignal?.Invoke(new ReactionContext(this, link.gameObject));
        }

    #endregion
    }
}
