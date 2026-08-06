/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : Zone.cs
수정일 : 2026-08-05

# 설명
공간 그래프에 배치된 하나의 구역과 그 콘텐츠 활성 상태를 표현한다.

# 제약사항
콘텐츠 로드·해제, 진행 완료 판정과 프로젝트 도메인 행동은 소유하지 않는다.
========================================================================= BLOCK_HEADER_END */

using System;

using UnityEngine;

namespace inonego.Xeri.Game
{
    // ============================================================
    /// <summary>
    /// Zone 콘텐츠가 현재 플레이 가능한 상태인지 나타낸다.
    /// </summary>
    // ============================================================
    public enum ZoneContentState
    {
        Inactive = 0,
        Active   = 1,
    }

    // ============================================================
    /// <summary>
    /// 공간 그래프에 배치된 구역의 식별자와 콘텐츠 활성 상태를 관리한다.
    /// </summary>
    // ============================================================
    public sealed class Zone : MonoBehaviour
    {
    #region 필드

        // ------------------------------------------------------------
        /// <summary>
        /// 공간 그래프 안에서 Zone을 식별하는 안정 문자열이다.
        /// </summary>
        // ------------------------------------------------------------
        public string ZoneID => zoneID;

        [SerializeField]
        private string zoneID = "";

        // ------------------------------------------------------------
        /// <summary>
        /// Zone이 제어할 콘텐츠 Root다.
        /// </summary>
        // ------------------------------------------------------------
        [SerializeField]
        private GameObject contentRoot = null;

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 콘텐츠 활성 상태다.
        /// </summary>
        // ------------------------------------------------------------
        public ZoneContentState ContentState => contentState;

        private ZoneContentState contentState = ZoneContentState.Inactive;

        // ------------------------------------------------------------
        /// <summary>
        /// Zone 콘텐츠가 활성화된 뒤 호출된다.
        /// </summary>
        // ------------------------------------------------------------
        public event Action<Zone> OnActivated = null;

        // ------------------------------------------------------------
        /// <summary>
        /// Zone 콘텐츠가 비활성화된 뒤 호출된다.
        /// </summary>
        // ------------------------------------------------------------
        public event Action<Zone> OnDeactivated = null;

        // ------------------------------------------------------------
        /// <summary>
        /// 플레이어가 이 Zone으로 이동 확정된 뒤 호출된다.
        /// </summary>
        // ------------------------------------------------------------
        public event Action<Zone> OnEntered = null;

        // ------------------------------------------------------------
        /// <summary>
        /// 플레이어가 다른 Zone으로 이동 확정되기 전 호출된다.
        /// </summary>
        // ------------------------------------------------------------
        public event Action<Zone> OnExited = null;

        // ------------------------------------------------------------
    #endregion

    #region 메서드

        // ------------------------------------------------------------
        /// <summary>
        /// Zone Graph가 이 Zone의 콘텐츠를 플레이 가능한 상태로 전환한다.
        /// </summary>
        // ------------------------------------------------------------
        internal void ActivateContent()
        {
            if (contentState == ZoneContentState.Active)
            {
                return;
            }

            // Zone Host는 살아 있게 두고, 명시적으로 지정된 콘텐츠만 전환한다.
            if (contentRoot != null)
            {
                contentRoot.SetActive(true);
            }

            contentState = ZoneContentState.Active;
            OnActivated?.Invoke(this);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Zone Graph 종료 시 이 Zone의 콘텐츠 활성 상태를 해제한다.
        /// </summary>
        // ------------------------------------------------------------
        internal void DeactivateContent()
        {
            if (contentState == ZoneContentState.Inactive)
            {
                return;
            }

            // Zone이 직접 자기 자신을 비활성화하지 않아 Zone Graph가 계속 수명을 조정할 수 있다.
            if (contentRoot != null)
            {
                contentRoot.SetActive(false);
            }

            contentState = ZoneContentState.Inactive;
            OnDeactivated?.Invoke(this);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Zone Graph가 Actor의 Zone 진입 사실을 전달한다.
        /// </summary>
        // ------------------------------------------------------------
        internal void Enter()
        {
            OnEntered?.Invoke(this);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Zone Graph가 Actor의 Zone 이탈 사실을 전달한다.
        /// </summary>
        // ------------------------------------------------------------
        internal void Exit()
        {
            OnExited?.Invoke(this);
        }

    #endregion
    }
}
