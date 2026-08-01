/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : UGUIFocusDriver.cs
수정일 : 2026-08-01

# 설명
명시적으로 연결한 EventSystem으로 Screen Focus 선택과 유효성 검사를 수행한다.
========================================================================= BLOCK_HEADER_END */

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace inonego.Xeri.UI.Game
{
    // ============================================================
    /// <summary>
    /// UGUI EventSystem Focus backend.
    /// </summary>
    // ============================================================
    public sealed class UGUIFocusDriver : MonoBehaviour, IFocusDriver
    {
    #region 필드

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 EventSystem 선택 GameObject.
        /// </summary>
        // ------------------------------------------------------------
        public object Current => eventSystem != null ? eventSystem.currentSelectedGameObject : null;

        // ------------------------------------------------------------
        /// <summary>
        /// Focus 선택에 사용하는 EventSystem.
        /// </summary>
        // ------------------------------------------------------------
        public EventSystem EventSystem => eventSystem;

        [SerializeField]
        private EventSystem eventSystem = null;

        [SerializeField]
        private GameObject fallback = null;

    #endregion

    #region IFocusDriver

        // ------------------------------------------------------------
        /// <summary>
        /// GameObject가 활성 상태이고 선택 가능한지 확인한다.
        /// </summary>
        // ------------------------------------------------------------
        public bool IsValid(object target)
        {
            if (!(target is GameObject gameObject) || gameObject == null || !gameObject.activeInHierarchy)
            {
                return false;
            }

            var selectable = gameObject.GetComponent<Selectable>();
            return selectable == null ||
                (selectable.isActiveAndEnabled && selectable.IsInteractable());
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 유효한 GameObject를 EventSystem 현재 선택으로 적용한다.
        /// </summary>
        // ------------------------------------------------------------
        public void Select(object target)
        {
            // EventSystem은 선택 callback 안의 중첩 선택을 거부하므로 바깥 선택이 끝난 뒤 다시 요청하게 둔다.
            if (eventSystem == null || eventSystem.alreadySelecting) return;

            eventSystem.SetSelectedGameObject(IsValid(target) ? (GameObject)target : null);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 직렬화한 fallback이 유효하면 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public object FindFallback()
        {
            return IsValid(fallback) ? fallback : null;
        }

    #endregion

    }
}
