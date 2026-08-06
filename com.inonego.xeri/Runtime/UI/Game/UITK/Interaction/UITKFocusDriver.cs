/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : UITKFocusDriver.cs
수정일 : 2026-08-06

# 설명
UI Toolkit Panel의 VisualElement Focus 선택, 유효성 검사와 native Focus 변경 보고를 수행한다.
========================================================================= BLOCK_HEADER_END */

using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UIElements;

namespace inonego.Xeri.UI.Game
{
    // ============================================================
    /// <summary>
    /// UI Toolkit Panel Focus backend.
    /// </summary>
    // ============================================================
    public sealed class UITKFocusDriver : FocusDriverBehaviour
    {
    #region 필드

        // ------------------------------------------------------------
        /// <summary>
        /// 마지막으로 Focus가 이동한 Panel의 현재 Focus Element.
        /// </summary>
        // ------------------------------------------------------------
        public override object Current
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

        private readonly List<VisualElement> panelRoots = new List<VisualElement>();
        private VisualElement current = null;

    #endregion

    #region FocusDriverBehaviour

        // ------------------------------------------------------------
        /// <summary>
        /// VisualElement Focus 대상을 다룬다.
        /// </summary>
        // ------------------------------------------------------------
        public override bool CanSelect(object target) => target is VisualElement;

        // ------------------------------------------------------------
        /// <summary>
        /// UI Toolkit Layer Panel을 사용자 Focus 추적 범위에 등록한다.
        /// </summary>
        // ------------------------------------------------------------
        protected override void HandleLayerRegistered(IPresentationLayerDriver driver)
        {
            if (driver is IPresentationLayerDriver<VisualElement> layer)
            {
                RegisterPanel(layer.Root);
            }
        }

    #endregion

    #region IFocusDriver

        // ------------------------------------------------------------
        /// <summary>
        /// VisualElement가 현재 Panel에서 Focus를 받을 수 있는지 확인한다.
        /// </summary>
        // ------------------------------------------------------------
        public override bool IsValid(object target)
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
        public override void Select(object target)
        {
            if (!IsValid(target))
            {
                ClearTrackedFocus(null);
                current = null;
                return;
            }

            var element = (VisualElement)target;
            RegisterPanel(element);
            ClearTrackedFocus(element.panel);
            current = element;
            current.Focus();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 직렬화한 fallback Element가 유효하면 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public override object FindFallback()
        {
            if (fallbackDocument == null || string.IsNullOrWhiteSpace(fallbackName))
            {
                return null;
            }

            var fallback = fallbackDocument.rootVisualElement?.Q<VisualElement>(fallbackName);
            return IsValid(fallback) ? fallback : null;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Focus 대상이 속한 Panel의 실제 Focus 이동을 추적한다.
        /// </summary>
        // ------------------------------------------------------------
        internal void RegisterPanel(VisualElement element)
        {
            if (element == null)
            {
                throw new System.ArgumentNullException(nameof(element));
            }

            var panelRoot = element.panel?.visualTree;

            if (panelRoot == null)
            {
                throw new System.InvalidOperationException
                (
                    "Focus 추적 대상이 UI Toolkit Panel에 연결되지 않았습니다."
                );
            }

            if (panelRoots.Contains(panelRoot)) return;

            panelRoots.Add(panelRoot);
            panelRoot.RegisterCallback<FocusInEvent>
            (
                HandleFocusIn,
                TrickleDown.TrickleDown
            );
            panelRoot.RegisterCallback<FocusOutEvent>
            (
                HandleFocusOut,
                TrickleDown.TrickleDown
            );
            panelRoot.RegisterCallback<DetachFromPanelEvent>(HandlePanelDetached);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 지정 Panel을 제외한 추적 Panel의 native Focus를 비운다.
        /// </summary>
        // ------------------------------------------------------------
        private void ClearTrackedFocus(IPanel exceptPanel)
        {
            for (var i = 0; i < panelRoots.Count; i++)
            {
                var panel = panelRoots[i].panel;

                if (panel == null || ReferenceEquals(panel, exceptPanel)) continue;

                if (panel.focusController?.focusedElement is VisualElement focused)
                {
                    focused.Blur();
                }
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 사용자 입력으로 이동한 실제 Panel Focus를 현재 대상으로 기록한다.
        /// </summary>
        // ------------------------------------------------------------
        private void HandleFocusIn(FocusInEvent eventData)
        {
            if (!(eventData.target is VisualElement focused) || focused.panel == null) return;

            RegisterPanel(focused);
            ClearTrackedFocus(focused.panel);
            current = focused;
            NotifyFocusChanged();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 추적 중인 native Focus가 빠져나가면 공통 Driver에 변경을 보고한다.
        /// </summary>
        // ------------------------------------------------------------
        private void HandleFocusOut(FocusOutEvent eventData)
        {
            if (!(eventData.target is VisualElement focused)) return;

            // 다른 Element의 FocusOut은 현재 Runtime 선택 상태를 바꾸지 않는다.
            if (!ReferenceEquals(current, focused)) return;

            NotifyFocusChanged();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 종료된 Panel의 Focus 추적 callback을 제거한다.
        /// </summary>
        // ------------------------------------------------------------
        private void HandlePanelDetached(DetachFromPanelEvent eventData)
        {
            if (!(eventData.target is VisualElement panelRoot)) return;

            var index = panelRoots.IndexOf(panelRoot);

            if (index < 0) return;

            panelRoots.RemoveAt(index);
            panelRoot.UnregisterCallback<FocusInEvent>
            (
                HandleFocusIn,
                TrickleDown.TrickleDown
            );
            panelRoot.UnregisterCallback<FocusOutEvent>
            (
                HandleFocusOut,
                TrickleDown.TrickleDown
            );
            panelRoot.UnregisterCallback<DetachFromPanelEvent>(HandlePanelDetached);

            if (current != null && current.panel == null)
            {
                current = null;
                NotifyFocusChanged();
            }
        }

    #endregion

    #region Unity 이벤트

        // ------------------------------------------------------------
        /// <summary>
        /// Host 종료 시 추적 중인 Panel callback을 모두 제거한다.
        /// </summary>
        // ------------------------------------------------------------
        private void OnDestroy()
        {
            for (var i = panelRoots.Count - 1; i >= 0; i--)
            {
                var panelRoot = panelRoots[i];
                panelRoots.RemoveAt(i);
                panelRoot.UnregisterCallback<FocusInEvent>
                (
                    HandleFocusIn,
                    TrickleDown.TrickleDown
                );
                panelRoot.UnregisterCallback<FocusOutEvent>
                (
                    HandleFocusOut,
                    TrickleDown.TrickleDown
                );
                panelRoot.UnregisterCallback<DetachFromPanelEvent>(HandlePanelDetached);
            }

            current = null;
        }

    #endregion

    }
}
