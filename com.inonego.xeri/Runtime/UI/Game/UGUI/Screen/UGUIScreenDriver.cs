/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : UGUIScreenDriver.cs
수정일 : 2026-07-29

# 설명
UGUI Screen Root, CanvasGroup 표시·상호작용과 기본 Focus를 Core Screen 계약에 연결한다.
========================================================================= BLOCK_HEADER_END */

using UnityEngine;

namespace inonego.Xeri.UI.Game
{
    // ============================================================
    /// <summary>
    /// UGUI Screen 표시 backend.
    /// </summary>
    // ============================================================
    public sealed class UGUIScreenDriver : MonoBehaviour, IScreenDriver, IVisibilityTarget
    {
    #region 필드

        // ------------------------------------------------------------
        /// <summary>
        /// backend 참조가 현재 유효한지 여부.
        /// </summary>
        // ------------------------------------------------------------
        public bool IsValid => root != null && canvasGroup != null;

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 Screen 표시 진행 값.
        /// </summary>
        // ------------------------------------------------------------
        public float Visibility => canvasGroup != null ? canvasGroup.alpha : 0.0f;

        // ------------------------------------------------------------
        /// <summary>
        /// 직렬화한 기본 Focus GameObject.
        /// </summary>
        // ------------------------------------------------------------
        public object DefaultFocus => defaultFocus;

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 Screen Root 표시 상태.
        /// </summary>
        // ------------------------------------------------------------
        public bool IsVisible => root != null && root.activeSelf;

        [SerializeField]
        private GameObject root = null;

        [SerializeField]
        private CanvasGroup canvasGroup = null;

        [SerializeField]
        private GameObject defaultFocus = null;

    #endregion

    #region IScreenDriver

        // ------------------------------------------------------------
        /// <summary>
        /// Screen Root 활성 상태를 적용한다.
        /// </summary>
        // ------------------------------------------------------------
        public void SetVisible(bool visible)
        {
            if (root != null)
            {
                root.SetActive(visible);
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// CanvasGroup 상호작용과 raycast 상태를 적용한다.
        /// </summary>
        // ------------------------------------------------------------
        public void SetInteractable(bool interactable)
        {
            if (canvasGroup == null) return;

            canvasGroup.interactable = interactable;
            canvasGroup.blocksRaycasts = interactable;
        }

    #endregion

    #region IPresentationTransitionTarget

        // ------------------------------------------------------------
        /// <summary>
        /// Transition 값을 CanvasGroup alpha로 적용한다.
        /// </summary>
        // ------------------------------------------------------------
        public void Apply(float value)
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = Mathf.Clamp01(value);
            }
        }

    #endregion

    }
}
