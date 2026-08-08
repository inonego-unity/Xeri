/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TrackingRunner.cs
수정일 : 2026-08-08

# 설명
Tracking Controller를 소유하고 Unity LateUpdate에서 갱신하는 선택적 Scene Component다.

# 수명 계약
비활성화는 갱신만 일시 중지하며, GameObject 파괴가 Controller와 Binding 수명을 종료한다.
========================================================================= BLOCK_HEADER_END */

using System;

using UnityEngine;

namespace inonego.Xeri
{
    // ============================================================
    /// <summary>
    /// Tracking Controller를 Unity LateUpdate에 연결하는 Scene Component.
    /// </summary>
    // ============================================================
    [DisallowMultipleComponent]
    public sealed class TrackingRunner : MonoBehaviour
    {
    #region 필드

        // ------------------------------------------------------------
        /// <summary>
        /// Tracking 전이에 Unscaled delta time을 사용할지 여부.
        /// </summary>
        // ------------------------------------------------------------
        public bool UsesUnscaledTime
        {
            get => usesUnscaledTime;
            set => usesUnscaledTime = value;
        }

        [SerializeField]
        private bool usesUnscaledTime = false;

        private TrackingController controller = null;
        private bool isReleased = false;

    #endregion

    #region 메서드

        // ------------------------------------------------------------
        /// <summary>
        /// Binding의 갱신과 종료를 현재 Runner에 연결하고 해제 Lease를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public Lease Track<T>(TrackingBinding<T> binding)
        {
            if (isReleased)
            {
                throw new ObjectDisposedException(nameof(TrackingRunner));
            }

            controller ??= new TrackingController();
            return controller.Track(binding);
        }

    #endregion

    #region Unity 이벤트

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 Runner가 소유한 Binding을 Frame 종료 시점에 갱신한다.
        /// </summary>
        // ------------------------------------------------------------
        private void LateUpdate()
        {
            if (isReleased || controller == null) return;

            var deltaTime = usesUnscaledTime
                ? Time.unscaledDeltaTime
                : Time.deltaTime;
            controller.Tick(deltaTime);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// GameObject 수명과 함께 Controller와 모든 Binding을 종료한다.
        /// </summary>
        // ------------------------------------------------------------
        private void OnDestroy()
        {
            if (isReleased) return;

            isReleased = true;
            var controller = this.controller;
            this.controller = null;
            controller?.Dispose();
        }

    #endregion

    }
}
