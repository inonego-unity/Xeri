/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : ZoneSignalSource.cs
수정일 : 2026-08-05

# 설명
Zone의 공간 수명 사실 하나를 Xeri Reaction Signal로 변환한다.

# 제약사항
조건 판정과 후속 효과 실행은 소유하지 않으며 ReactionBinding이 별도로 연결한다.
========================================================================= BLOCK_HEADER_END */

using System;

using UnityEngine;

using inonego.Xeri;

namespace inonego.Xeri.Game
{
    // ============================================================
    /// <summary>
    /// Zone에서 Signal로 노출할 하나의 사실을 지정한다.
    /// </summary>
    // ============================================================
    public enum ZoneSignalKind
    {
        Activated   = 0,
        Deactivated = 1,
        Entered     = 2,
        Exited      = 3,
    }

    // ============================================================
    /// <summary>
    /// 선택한 Zone 사실을 ReactionBinding이 구독할 Signal로 변환하는 부착형 Adapter다.
    /// </summary>
    // ============================================================
    public sealed class ZoneSignalSource : MonoBehaviour, ISignalSource
    {
    #region 필드

        // ------------------------------------------------------------
        /// <summary>
        /// Signal로 변환할 사실을 제공하는 Zone이다.
        /// </summary>
        // ------------------------------------------------------------
        [SerializeField]
        private Zone zone = null;

        // ------------------------------------------------------------
        /// <summary>
        /// 구독할 Zone 사실이다.
        /// </summary>
        // ------------------------------------------------------------
        [SerializeField]
        private ZoneSignalKind signalKind = ZoneSignalKind.Entered;

        // ------------------------------------------------------------
        /// <summary>
        /// 선택한 Zone 사실이 발생했을 때 전달된다.
        /// </summary>
        // ------------------------------------------------------------
        public event Action<ReactionContext> OnSignal = null;

    #endregion

    #region 유니티 이벤트

        // ------------------------------------------------------------
        /// <summary>
        /// 선택한 Zone 사실을 구독한다.
        /// </summary>
        // ------------------------------------------------------------
        private void OnEnable()
        {
            if (zone == null)
            {
                throw new InvalidOperationException("ZoneSignalSource에 Zone이 설정되어 있지 않습니다.");
            }

            Subscribe();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 비활성화된 Adapter가 Zone 사실을 계속 Signal로 전달하지 않게 한다.
        /// </summary>
        // ------------------------------------------------------------
        private void OnDisable()
        {
            if (zone != null)
            {
                Unsubscribe();
            }
        }

    #endregion

    #region 메서드

        // ------------------------------------------------------------
        /// <summary>
        /// 선택한 Zone Event에 Signal 변환 Handler를 연결한다.
        /// </summary>
        // ------------------------------------------------------------
        private void Subscribe()
        {
            switch (signalKind)
            {
                case ZoneSignalKind.Activated:
                    zone.OnActivated += HandleZoneSignal;
                    break;

                case ZoneSignalKind.Deactivated:
                    zone.OnDeactivated += HandleZoneSignal;
                    break;

                case ZoneSignalKind.Entered:
                    zone.OnEntered += HandleZoneSignal;
                    break;

                case ZoneSignalKind.Exited:
                    zone.OnExited += HandleZoneSignal;
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 선택한 Zone Event에서 Signal 변환 Handler를 제거한다.
        /// </summary>
        // ------------------------------------------------------------
        private void Unsubscribe()
        {
            switch (signalKind)
            {
                case ZoneSignalKind.Activated:
                    zone.OnActivated -= HandleZoneSignal;
                    break;

                case ZoneSignalKind.Deactivated:
                    zone.OnDeactivated -= HandleZoneSignal;
                    break;

                case ZoneSignalKind.Entered:
                    zone.OnEntered -= HandleZoneSignal;
                    break;

                case ZoneSignalKind.Exited:
                    zone.OnExited -= HandleZoneSignal;
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

    #endregion

    #region 이벤트 핸들러

        // ------------------------------------------------------------
        /// <summary>
        /// Zone 사실을 현재 Adapter와 Zone GameObject를 담은 Reaction Signal로 전달한다.
        /// </summary>
        // ------------------------------------------------------------
        private void HandleZoneSignal(Zone source)
        {
            OnSignal?.Invoke(new ReactionContext(this, source.gameObject));
        }

    #endregion
    }
}
