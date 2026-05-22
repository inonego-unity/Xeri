/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : UGUIRaycastPolicy.cs
수정일 : 2026-05-22

# 설명
UGUI 드래그 중 CanvasGroup raycast 차단 상태를 처리한다.
========================================================================= BLOCK_HEADER_END */

using UnityEngine;

namespace inonego.Xeri.UI.DragDrop
{
    // ============================================================
    /// <summary>
    /// UGUI raycast 전환 정책.
    /// </summary>
    // ============================================================
    public sealed class UGUIRaycastPolicy
    {

    #region 필드

        // ------------------------------------------------------------
        /// <summary>
        /// 드래그 중 raycast 차단 여부를 바꿀 CanvasGroup.
        /// </summary>
        // ------------------------------------------------------------
        private readonly CanvasGroup canvasGroup;

        // ------------------------------------------------------------
        /// <summary>
        /// 드래그 중 raycast를 끌지 여부.
        /// </summary>
        // ------------------------------------------------------------
        public bool DisableRaycastDuringDrag
        {
            get => disableRaycastDuringDrag;
            set => disableRaycastDuringDrag = value;
        }

        private bool disableRaycastDuringDrag;

        private bool originalBlocksRaycasts = true;
        private bool hasOriginalValue = false;

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// UGUI raycast 전환 정책을 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        public UGUIRaycastPolicy(CanvasGroup canvasGroup, bool disableRaycastDuringDrag) : base()
        {
            this.canvasGroup = canvasGroup;
            this.disableRaycastDuringDrag = disableRaycastDuringDrag;
        }

    #endregion

    #region 메서드

        // ------------------------------------------------------------
        /// <summary>
        /// 드래그 시작 시 CanvasGroup raycast 상태를 변경한다.
        /// </summary>
        // ------------------------------------------------------------
        public void Begin()
        {
            if (canvasGroup == null) return;

            originalBlocksRaycasts = canvasGroup.blocksRaycasts;
            hasOriginalValue       = true;

            if (disableRaycastDuringDrag)
            {
                canvasGroup.blocksRaycasts = false;
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 드래그 종료 시 CanvasGroup raycast 상태를 복원한다.
        /// </summary>
        // ------------------------------------------------------------
        public void End()
        {
            if (canvasGroup == null) return;
            if (!hasOriginalValue) return;

            canvasGroup.blocksRaycasts = originalBlocksRaycasts;
            hasOriginalValue = false;
        }

    #endregion

    }
}
