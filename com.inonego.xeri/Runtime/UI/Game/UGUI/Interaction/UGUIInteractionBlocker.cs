/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : UGUIInteractionBlocker.cs
수정일 : 2026-07-29

# 설명
중첩 점유 수에 따라 명시적 UGUI Blocker Root와 CanvasGroup raycast 상태를 적용한다.
========================================================================= BLOCK_HEADER_END */

using System;

using UnityEngine;

namespace inonego.Xeri.UI.Game
{
    // ============================================================
    /// <summary>
    /// UGUI Interaction Blocker 점유 수명을 소유한다.
    /// </summary>
    // ============================================================
    public sealed class UGUIInteractionBlocker : MonoBehaviour
    {
    #region 필드

        [SerializeField]
        private GameObject root = null;

        [SerializeField]
        private CanvasGroup canvasGroup = null;

        private int count = 0;

    #endregion

    #region 메서드

        // ------------------------------------------------------------
        /// <summary>
        /// Interaction Blocker를 한 번 점유하고 Handle을 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public InteractionBlockerHandle Acquire()
        {
            if (root == null && canvasGroup == null)
            {
                throw new InvalidOperationException
                (
                    "UGUI Interaction Blocker Root 또는 CanvasGroup이 필요합니다."
                );
            }

            var nextCount = count + 1;
            Apply(nextCount > 0);
            count = nextCount;
            return new InteractionBlockerHandle(Release);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Interaction Blocker 점유를 한 번 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        private void Release()
        {
            if (count <= 0)
            {
                throw new InvalidOperationException("Interaction Blocker 점유 수가 이미 0입니다.");
            }

            var nextCount = count - 1;
            Apply(nextCount > 0);
            count = nextCount;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 점유 수를 UGUI 활성·raycast 상태에 적용한다.
        /// </summary>
        // ------------------------------------------------------------
        private void Apply(bool active)
        {
            if (root != null)
            {
                root.SetActive(active);
            }

            if (canvasGroup != null)
            {
                canvasGroup.interactable = active;
                canvasGroup.blocksRaycasts = active;
            }
        }

    #endregion

    }
}
