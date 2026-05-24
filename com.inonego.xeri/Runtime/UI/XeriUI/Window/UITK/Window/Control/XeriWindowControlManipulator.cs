/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : XeriWindowControlManipulator.cs
수정일 : 2026-05-23

# 설명
XeriWindowPanel control button 입력을 controller 명령으로 연결한다.
========================================================================= BLOCK_HEADER_END */

using UnityEngine.UIElements;

namespace inonego.Xeri.UI.Window
{
    // ============================================================
    /// <summary>
    /// Window control button 상호작용 wrapper.
    /// </summary>
    // ============================================================
    public sealed class XeriWindowControlManipulator
    {

    #region 필드

        private readonly XeriWindowPanel panel = null;
        private readonly XeriWindowController controller = null;

        private bool isAttached = false;

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// Window control 상호작용 wrapper를 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        public XeriWindowControlManipulator
        (
            XeriWindowPanel panel,
            XeriWindowController controller
        ) : base()
        {
            this.panel = panel;
            this.controller = controller;
        }

    #endregion

    #region 메서드

        // ------------------------------------------------------------
        /// <summary>
        /// Button callback을 부착한다.
        /// </summary>
        // ------------------------------------------------------------
        public void Attach()
        {
            if (isAttached) return;
            if (panel == null || controller == null) return;

            panel.MinimizeButton.clicked += controller.Minimize;
            panel.MaximizeButton.clicked += OnMaximizeClick;
            panel.CloseButton.clicked += controller.Close;
            panel.TitleActions.RegisterCallback<PointerDownEvent>(OnTitleActionsPointerDown);

            isAttached = true;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Button callback을 해제한다.
        /// </summary>
        // ------------------------------------------------------------
        public void Detach()
        {
            if (!isAttached) return;
            if (panel == null || controller == null) return;

            panel.MinimizeButton.clicked -= controller.Minimize;
            panel.MaximizeButton.clicked -= OnMaximizeClick;
            panel.CloseButton.clicked -= controller.Close;
            panel.TitleActions.UnregisterCallback<PointerDownEvent>(OnTitleActionsPointerDown);

            isAttached = false;
        }

    #endregion

    #region 이벤트 핸들러

        // ------------------------------------------------------------
        /// <summary>
        /// Maximize button으로 maximize/show normal을 토글한다.
        /// </summary>
        // ------------------------------------------------------------
        private void OnMaximizeClick()
        {
            if (controller.Driver.State == XeriWindowState.Maximized)
            {
                controller.ShowNormal();
                return;
            }

            controller.Maximize();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Control button 영역 pointer 입력이 titlebar drag로 전파되지 않게 한다.
        /// </summary>
        // ------------------------------------------------------------
        private void OnTitleActionsPointerDown(PointerDownEvent evt)
        {
            evt.StopPropagation();
        }

    #endregion

    }
}
