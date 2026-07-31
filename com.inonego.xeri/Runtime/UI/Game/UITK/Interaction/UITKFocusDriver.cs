/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : UITKFocusDriver.cs
수정일 : 2026-07-31

# 설명
UI Toolkit Panel의 VisualElement Focus 선택, 유효성 검사와 fallback 조회를 수행한다.
========================================================================= BLOCK_HEADER_END */

using UnityEngine;
using UnityEngine.UIElements;

namespace inonego.Xeri.UI.Game
{
    // ============================================================
    /// <summary>
    /// UI Toolkit Panel Focus backend.
    /// </summary>
    // ============================================================
    public sealed class UITKFocusDriver : MonoBehaviour, IFocusDriver
    {
    #region 필드

        // ------------------------------------------------------------
        /// <summary>
        /// 마지막으로 선택한 Panel의 현재 Focus Element.
        /// </summary>
        // ------------------------------------------------------------
        public object Current
        {
            get
            {
                if (current == null || current.panel == null) return null;

                return current.focusController?.focusedElement as VisualElement;
            }
        }

        [SerializeField]
        private UIDocument fallbackDocument = null;

        [SerializeField]
        private string fallbackName = "";

        private VisualElement current = null;

    #endregion

    #region IFocusDriver

        // ------------------------------------------------------------
        /// <summary>
        /// VisualElement가 현재 Panel에서 Focus를 받을 수 있는지 확인한다.
        /// </summary>
        // ------------------------------------------------------------
        public bool IsValid(object target)
        {
            if (!(target is VisualElement element) || element.panel == null)
            {
                return false;
            }

            return
                element.focusable &&
                element.canGrabFocus &&
                element.enabledInHierarchy &&
                element.resolvedStyle.display != DisplayStyle.None &&
                element.resolvedStyle.visibility == Visibility.Visible;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 유효한 VisualElement에 Focus를 적용한다.
        /// </summary>
        // ------------------------------------------------------------
        public void Select(object target)
        {
            if (!IsValid(target))
            {
                if (Current is VisualElement selected)
                {
                    selected.Blur();
                }
                else
                {
                    current?.Blur();
                }

                current = null;
                return;
            }

            current = (VisualElement)target;
            current.Focus();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 직렬화한 fallback Element가 유효하면 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public object FindFallback()
        {
            if (fallbackDocument == null || string.IsNullOrWhiteSpace(fallbackName))
            {
                return null;
            }

            var fallback = fallbackDocument.rootVisualElement?.Q<VisualElement>(fallbackName);
            return IsValid(fallback) ? fallback : null;
        }

    #endregion

    }
}
