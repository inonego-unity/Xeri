/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : ZoneGraphRuntime.cs
수정일 : 2026-08-05

# 설명
같은 Scene에 직접 배치된 Zone과 Zone Link의 활성·이동 확정 수명을 관리한다.

# 제약사항
비동기 로딩, 콘텐츠 해제 정책, 진행 판정, 조건과 저장·복원은 소유하지 않는다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections.Generic;

using UnityEngine;

namespace inonego.Xeri.Game
{
    // ============================================================
    /// <summary>
    /// 직접 참조로 구성한 Zone Graph의 Zone 활성 상태와 Actor 이동을 조정한다.
    /// </summary>
    // ============================================================
    public sealed class ZoneGraphRuntime : MonoBehaviour
    {
    #region 필드

        // ------------------------------------------------------------
        /// <summary>
        /// 이 Zone Graph에 배치된 Zone 목록이다.
        /// </summary>
        // ------------------------------------------------------------
        [SerializeField]
        private Zone[] zones = Array.Empty<Zone>();

        // ------------------------------------------------------------
        /// <summary>
        /// 이 Zone Graph에서 사용할 Zone Link 목록이다.
        /// </summary>
        // ------------------------------------------------------------
        [SerializeField]
        private ZoneLink[] zoneLinks = Array.Empty<ZoneLink>();

        // ------------------------------------------------------------
        /// <summary>
        /// Zone Graph 시작 시 Actor가 진입할 Zone이다.
        /// </summary>
        // ------------------------------------------------------------
        [SerializeField]
        private Zone initialZone = null;

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 플레이어가 위치한 Zone이다.
        /// </summary>
        // ------------------------------------------------------------
        public Zone CurrentZone => currentZone;

        private Zone currentZone = null;

        // ------------------------------------------------------------
        /// <summary>
        /// Zone Graph가 현재 Zone 수명을 관리 중인지 나타낸다.
        /// </summary>
        // ------------------------------------------------------------
        public bool IsRunning => isRunning;

        private bool isRunning = false;

        private readonly Dictionary<string, Zone> zonesByID = new();
        private readonly HashSet<ZoneLink> registeredLinks = new();

        // ------------------------------------------------------------
        /// <summary>
        /// 시작 Zone 진입이 확정된 뒤 호출된다.
        /// </summary>
        // ------------------------------------------------------------
        public event Action<ZoneGraphRuntime, Zone> OnStarted = null;

        // ------------------------------------------------------------
        /// <summary>
        /// Zone 이동이 확정된 뒤 이전 및 현재 Zone과 함께 호출된다.
        /// </summary>
        // ------------------------------------------------------------
        public event Action<ZoneGraphRuntime, Zone, Zone> OnZoneChanged = null;

        // ------------------------------------------------------------
        /// <summary>
        /// Zone Graph가 모든 Zone 콘텐츠 활성 상태를 해제한 뒤 호출된다.
        /// </summary>
        // ------------------------------------------------------------
        public event Action<ZoneGraphRuntime> OnStopped = null;

    #endregion

    #region 유니티 이벤트

        // ------------------------------------------------------------
        /// <summary>
        /// Scene에 배치된 Zone ID를 Zone Graph 실행 전에 검증하고 등록한다.
        /// </summary>
        // ------------------------------------------------------------
        private void Awake()
        {
            RebuildZoneRegistry();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Zone Graph Host가 활성화되면 초기 Zone을 즉시 활성화한다.
        /// </summary>
        // ------------------------------------------------------------
        private void Start()
        {
            StartGraph();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Zone Graph Host가 비활성화되면 자신이 활성화한 Zone 콘텐츠만 정리한다.
        /// </summary>
        // ------------------------------------------------------------
        private void OnDisable()
        {
            StopGraph();
        }

    #endregion

    #region 메서드

        // ------------------------------------------------------------
        /// <summary>
        /// 초기 Zone만 활성화하고 Zone Graph 이동 수명을 시작한다.
        /// </summary>
        // ------------------------------------------------------------
        public void StartGraph()
        {
            if (isRunning)
            {
                return;
            }

            RebuildZoneRegistry();

            if (initialZone == null || !zonesByID.ContainsValue(initialZone))
            {
                throw new InvalidOperationException("ZoneGraphRuntime의 Initial Zone은 Zones 목록에 포함되어야 합니다.");
            }

            // 시작 전에는 이전 편집 상태를 제거해 시작 Zone만 플레이 가능하게 만든다.
            foreach (var zone in zones)
            {
                zone.DeactivateContent();
            }

            isRunning = true;
            EnterZone(initialZone);
            OnStarted?.Invoke(this, initialZone);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 Zone에서 지정한 Link를 통과해 다음 Zone으로 이동을 시도한다.
        /// </summary>
        // ------------------------------------------------------------
        public bool TryTraverse(ZoneLink link)
        {
            if (!isRunning || link == null || !registeredLinks.Contains(link))
            {
                return false;
            }

            if (!link.isActiveAndEnabled || !link.IsPassable)
            {
                return false;
            }

            var source = currentZone;

            if (!link.TryGetDestination(source, out var destination))
            {
                return false;
            }

            // 목적지 콘텐츠가 먼저 활성화된 뒤에만 현재 Zone의 이탈 사실을 전달한다.
            EnterZone(destination);
            link.NotifyTraversed(source, destination);
            return true;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Zone Graph가 소유한 Zone 콘텐츠를 비활성화하고 이동 상태를 끝낸다.
        /// </summary>
        // ------------------------------------------------------------
        public void StopGraph()
        {
            if (!isRunning)
            {
                return;
            }

            // 현재 Zone의 이탈을 먼저 알리고, 이후 콘텐츠 활성 상태를 해제한다.
            currentZone?.Exit();

            foreach (var zone in zones)
            {
                zone.DeactivateContent();
            }

            currentZone = null;
            isRunning = false;
            OnStopped?.Invoke(this);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Zone ID로 현재 Zone Graph에 등록된 Zone을 찾는다.
        /// </summary>
        // ------------------------------------------------------------
        public bool TryGetZone(string zoneID, out Zone zone)
        {
            return zonesByID.TryGetValue(zoneID, out zone);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 이전 Zone을 유지한 채 목적지 콘텐츠 활성 및 플레이어 위치를 확정한다.
        /// </summary>
        // ------------------------------------------------------------
        private void EnterZone(Zone destination)
        {
            var previousZone = currentZone;

            // 이전 Zone은 복귀와 연출 요구가 생길 수 있으므로 이 1차 구현에서 즉시 해제하지 않는다.
            destination.ActivateContent();
            previousZone?.Exit();

            currentZone = destination;
            destination.Enter();
            OnZoneChanged?.Invoke(this, previousZone, destination);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 직렬화된 Zone 목록을 ID 기준으로 검증하고 조회 Registry를 재구성한다.
        /// </summary>
        // ------------------------------------------------------------
        private void RebuildZoneRegistry()
        {
            zonesByID.Clear();
            registeredLinks.Clear();

            foreach (var zone in zones)
            {
                if (zone == null)
                {
                    throw new InvalidOperationException("ZoneGraphRuntime Zones 목록에는 null을 포함할 수 없습니다.");
                }

                if (string.IsNullOrWhiteSpace(zone.ZoneID))
                {
                    throw new InvalidOperationException("ZoneGraphRuntime Zone에는 비어 있지 않은 Zone ID가 필요합니다.");
                }

                if (!zonesByID.TryAdd(zone.ZoneID, zone))
                {
                    throw new InvalidOperationException($"ZoneGraphRuntime에 중복 Zone ID가 있습니다: {zone.ZoneID}");
                }
            }

            foreach (var link in zoneLinks)
            {
                if (link == null)
                {
                    throw new InvalidOperationException("ZoneGraphRuntime Zone Links 목록에는 null을 포함할 수 없습니다.");
                }

                if (!link.HasDistinctEndpoints())
                {
                    throw new InvalidOperationException("ZoneGraphRuntime Zone Link에는 서로 다른 두 Zone 끝점이 필요합니다.");
                }

                if (!zonesByID.ContainsValue(link.Zone0) || !zonesByID.ContainsValue(link.Zone1))
                {
                    throw new InvalidOperationException("ZoneGraphRuntime Zone Link의 양 끝점은 Zones 목록에 포함되어야 합니다.");
                }

                registeredLinks.Add(link);
            }
        }

    #endregion
    }
}
