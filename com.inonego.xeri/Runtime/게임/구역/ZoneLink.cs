/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : ZoneLink.cs
수정일 : 2026-08-05

# 설명
두 Zone 사이의 이동 관계와 원인별 통행 제한을 관리한다.

# 제약사항
제한 원인의 의미와 해제 조건은 이 Link가 아니라 외부 도메인 또는 Binding이 소유한다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections.Generic;

using UnityEngine;

namespace inonego.Xeri.Game
{
    // ============================================================
    /// <summary>
    /// Zone Link에 추가한 하나의 통행 제한을 소유하고 해제한다.
    /// </summary>
    // ============================================================
    public sealed class ZoneLinkBlockLease : IDisposable
    {
    #region 필드

        private ZoneLink link = null;
        private readonly int blockID;

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// 특정 Link가 소유한 통행 제한 Lease를 만든다.
        /// </summary>
        // ------------------------------------------------------------
        internal ZoneLinkBlockLease(ZoneLink link, int blockID)
        {
            this.link = link;
            this.blockID = blockID;
        }

    #endregion

    #region 메서드

        // ------------------------------------------------------------
        /// <summary>
        /// 이 Lease가 추가한 통행 제한만 해제한다.
        /// </summary>
        // ------------------------------------------------------------
        public void Dispose()
        {
            if (link == null)
            {
                return;
            }

            // Lease 소유자만 자신의 제한을 해제하도록 Link와의 연결을 즉시 끊는다.
            var ownedLink = link;
            link = null;
            ownedLink.ReleaseBlock(blockID);
        }

    #endregion
    }

    // ============================================================
    /// <summary>
    /// 두 Zone 사이의 실제 이동 관계와 통행 가능 상태를 제공한다.
    /// </summary>
    // ============================================================
    public sealed class ZoneLink : MonoBehaviour
    {
    #region 필드

        // ------------------------------------------------------------
        /// <summary>
        /// Link의 한쪽 끝 Zone이다.
        /// </summary>
        // ------------------------------------------------------------
        [SerializeField]
        private Zone zone0 = null;

        // ------------------------------------------------------------
        /// <summary>
        /// Link의 반대쪽 끝 Zone이다.
        /// </summary>
        // ------------------------------------------------------------
        [SerializeField]
        private Zone zone1 = null;

        // ------------------------------------------------------------
        /// <summary>
        /// Link의 0번 Zone 끝점이다.
        /// </summary>
        // ------------------------------------------------------------
        internal Zone Zone0 => zone0;

        // ------------------------------------------------------------
        /// <summary>
        /// Link의 1번 Zone 끝점이다.
        /// </summary>
        // ------------------------------------------------------------
        internal Zone Zone1 => zone1;

        // ------------------------------------------------------------
        /// <summary>
        /// Zone 1에서 Zone 0 방향 이동을 허용할지 나타낸다.
        /// </summary>
        // ------------------------------------------------------------
        [SerializeField]
        private bool isBidirectional = true;

        // ------------------------------------------------------------
        /// <summary>
        /// 모든 원인이 해제되어 이 Link를 통과할 수 있는지 나타낸다.
        /// </summary>
        // ------------------------------------------------------------
        public bool IsPassable => blocks.Count == 0;

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 이 Link를 막고 있는 원인의 수다.
        /// </summary>
        // ------------------------------------------------------------
        public int BlockCount => blocks.Count;

        private readonly Dictionary<int, string> blocks = new();
        private int nextBlockID = 1;

        // ------------------------------------------------------------
        /// <summary>
        /// 통행 가능 상태가 변경된 뒤 호출된다.
        /// </summary>
        // ------------------------------------------------------------
        public event Action<ZoneLink> OnPassabilityChanged = null;

        // ------------------------------------------------------------
        /// <summary>
        /// Zone Graph가 이 Link를 통한 Zone 이동을 확정한 뒤 호출된다.
        /// </summary>
        // ------------------------------------------------------------
        public event Action<ZoneLink, Zone, Zone> OnTraversed = null;

    #endregion

    #region 메서드

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 Zone에서 이 Link를 통해 이동할 목적지 Zone을 찾는다.
        /// </summary>
        // ------------------------------------------------------------
        public bool TryGetDestination(Zone source, out Zone destination)
        {
            if (source == zone0)
            {
                destination = zone1;
                return destination != null;
            }

            if (isBidirectional && source == zone1)
            {
                destination = zone0;
                return destination != null;
            }

            destination = null;
            return false;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Zone Graph가 이 Link의 양 끝점이 서로 다른 Zone으로 설정됐는지 확인한다.
        /// </summary>
        // ------------------------------------------------------------
        internal bool HasDistinctEndpoints()
        {
            return zone0 != null && zone1 != null && zone0 != zone1;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Zone Graph가 확정한 이 Link의 이동 사실을 외부 구독자에게 전달한다.
        /// </summary>
        // ------------------------------------------------------------
        internal void NotifyTraversed(Zone source, Zone destination)
        {
            OnTraversed?.Invoke(this, source, destination);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 통행을 막는 새 원인을 추가하고 해당 원인 전용 Lease를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public ZoneLinkBlockLease AcquireBlock(string cause)
        {
            if (string.IsNullOrWhiteSpace(cause))
            {
                throw new ArgumentException("통행 제한 원인을 비워 둘 수 없습니다.", nameof(cause));
            }

            var wasPassable = IsPassable;
            var blockID = nextBlockID++;

            // 같은 원인 이름도 별도 Lease로 유지해 서로의 제한을 해제하지 못하게 한다.
            blocks.Add(blockID, cause);

            if (wasPassable)
            {
                OnPassabilityChanged?.Invoke(this);
            }

            return new ZoneLinkBlockLease(this, blockID);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 지정한 Lease가 소유한 통행 제한을 제거한다.
        /// </summary>
        // ------------------------------------------------------------
        internal void ReleaseBlock(int blockID)
        {
            if (!blocks.Remove(blockID) || !IsPassable)
            {
                return;
            }

            // 마지막 제한이 해제된 순간만 외부에 통행 가능 전환을 알린다.
            OnPassabilityChanged?.Invoke(this);
        }

    #endregion
    }
}
