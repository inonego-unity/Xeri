/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : XeriWindowController.cs
수정일 : 2026-05-23

# 설명
Xeri 커스텀 윈도우 상태 전환, 명령, 이벤트를 관리하는 controller.
========================================================================= BLOCK_HEADER_END */

using System;

using UnityEngine;

using inonego.Xeri;

namespace inonego.Xeri.UI.Window
{
    // ============================================================
    /// <summary>
    /// Xeri 커스텀 윈도우 상태와 명령을 관리하는 controller.
    /// </summary>
    // ============================================================
    public sealed class XeriWindowController
    {

    #region 필드

        // ------------------------------------------------------------
        /// <summary>
        /// 표시 계층 driver.
        /// </summary>
        // ------------------------------------------------------------
        public IXeriWindowDriver Driver => driver;

        private readonly IXeriWindowDriver driver = null;

        // ------------------------------------------------------------
        /// <summary>
        /// 윈도우 동작 옵션.
        /// </summary>
        // ------------------------------------------------------------
        public XeriWindowOptions Options
        {
            get => options;
            set => options = value;
        }

        private XeriWindowOptions options = XeriWindowOptions.Default();

    #endregion

    #region 이벤트

        // ------------------------------------------------------------
        /// <summary>
        /// 윈도우 이동 시 호출된다.
        /// </summary>
        // ------------------------------------------------------------
        public event EventHandler<XeriWindowEventArgs> OnMove = null;

        // ------------------------------------------------------------
        /// <summary>
        /// 윈도우 크기 변경 시 호출된다.
        /// </summary>
        // ------------------------------------------------------------
        public event EventHandler<XeriWindowEventArgs> OnResize = null;

        // ------------------------------------------------------------
        /// <summary>
        /// 윈도우 위치 값 변경 시 호출된다.
        /// </summary>
        // ------------------------------------------------------------
        public event ValueChangeEventHandler<Vector2> OnPosChange = null;

        // ------------------------------------------------------------
        /// <summary>
        /// 윈도우 크기 값 변경 시 호출된다.
        /// </summary>
        // ------------------------------------------------------------
        public event ValueChangeEventHandler<Vector2> OnSizeChange = null;

        // ------------------------------------------------------------
        /// <summary>
        /// 윈도우 상태 값 변경 시 호출된다.
        /// </summary>
        // ------------------------------------------------------------
        public event ValueChangeEventHandler<XeriWindowState> OnStateChange = null;

        // ------------------------------------------------------------
        /// <summary>
        /// 윈도우 최소화 시 호출된다.
        /// </summary>
        // ------------------------------------------------------------
        public event EventHandler<XeriWindowEventArgs> OnMinimize = null;

        // ------------------------------------------------------------
        /// <summary>
        /// 윈도우 최소화 요청 전에 호출된다.
        /// </summary>
        // ------------------------------------------------------------
        public event EventHandler<XeriWindowCancelEventArgs> OnPreMinimize = null;

        // ------------------------------------------------------------
        /// <summary>
        /// 윈도우 최대화 시 호출된다.
        /// </summary>
        // ------------------------------------------------------------
        public event EventHandler<XeriWindowEventArgs> OnMaximize = null;

        // ------------------------------------------------------------
        /// <summary>
        /// 윈도우 최대화 요청 전에 호출된다.
        /// </summary>
        // ------------------------------------------------------------
        public event EventHandler<XeriWindowCancelEventArgs> OnPreMaximize = null;

        // ------------------------------------------------------------
        /// <summary>
        /// 윈도우 normal 표시 복귀 시 호출된다.
        /// </summary>
        // ------------------------------------------------------------
        public event EventHandler<XeriWindowEventArgs> OnShowNormal = null;

        // ------------------------------------------------------------
        /// <summary>
        /// 윈도우 normal 표시 복귀 요청 전에 호출된다.
        /// </summary>
        // ------------------------------------------------------------
        public event EventHandler<XeriWindowCancelEventArgs> OnPreShowNormal = null;

        // ------------------------------------------------------------
        /// <summary>
        /// 윈도우 닫기 시 호출된다.
        /// </summary>
        // ------------------------------------------------------------
        public event EventHandler<XeriWindowEventArgs> OnClose = null;

        // ------------------------------------------------------------
        /// <summary>
        /// 윈도우 닫기 요청 전에 호출된다.
        /// </summary>
        // ------------------------------------------------------------
        public event EventHandler<XeriWindowCancelEventArgs> OnPreClose = null;

        // ------------------------------------------------------------
        /// <summary>
        /// 윈도우 포커스 시 호출된다.
        /// </summary>
        // ------------------------------------------------------------
        public event EventHandler<XeriWindowEventArgs> OnFocus = null;

        // ------------------------------------------------------------
        /// <summary>
        /// 윈도우 포커스 해제 시 호출된다.
        /// </summary>
        // ------------------------------------------------------------
        public event EventHandler<XeriWindowEventArgs> OnLoseFocus = null;

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// 윈도우 controller를 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        public XeriWindowController
        (
            IXeriWindowDriver driver,
            XeriWindowOptions? options = null
        ) : base()
        {
            this.driver  = driver ?? throw new ArgumentNullException(nameof(driver));
            this.options = options ?? XeriWindowOptions.Default();

        }

    #endregion

    #region 명령

        // ------------------------------------------------------------
        /// <summary>
        /// 윈도우를 이동한다.
        /// </summary>
        // ------------------------------------------------------------
        public void Move(Vector2 pos)
        {
            if (!options.CanMove) return;
            if (!CanChangeBounds()) return;

            var previous = driver.Pos;
            if (previous == pos) return;

            driver.Pos = pos;

            OnMove?.Invoke(this, CreateEventArgs());
            OnPosChange?.Invoke(this, new ValueChangeEventArgs<Vector2>(previous, pos));
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 윈도우 크기를 변경한다.
        /// </summary>
        // ------------------------------------------------------------
        public void Resize(Vector2 size)
        {
            if (!options.CanResize) return;
            if (!CanChangeBounds()) return;

            var previous = driver.Size;
            var clamped  = ClampSize(size);
            if (previous == clamped) return;

            driver.Size = clamped;

            OnResize?.Invoke(this, CreateEventArgs());
            OnSizeChange?.Invoke(this, new ValueChangeEventArgs<Vector2>(previous, clamped));
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 윈도우를 최소화한다.
        /// </summary>
        // ------------------------------------------------------------
        public void Minimize()
        {
            if (!options.CanMinimize) return;
            if (driver.State == XeriWindowState.Minimized) return;
            if (driver.State == XeriWindowState.Closed) return;
            if (IsCancelled(OnPreMinimize)) return;

            SetState(XeriWindowState.Minimized, OnMinimize);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 윈도우를 최대화한다.
        /// </summary>
        // ------------------------------------------------------------
        public void Maximize()
        {
            if (!options.CanMaximize) return;
            if (driver.State == XeriWindowState.Maximized) return;
            if (driver.State == XeriWindowState.Closed) return;
            if (IsCancelled(OnPreMaximize)) return;

            SetState(XeriWindowState.Maximized, OnMaximize);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 윈도우를 normal 상태로 되돌린다.
        /// </summary>
        // ------------------------------------------------------------
        public void ShowNormal()
        {
            if (driver.State == XeriWindowState.Normal) return;
            if (driver.State == XeriWindowState.Closed) return;
            if (IsCancelled(OnPreShowNormal)) return;

            SetState(XeriWindowState.Normal, OnShowNormal);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 윈도우를 닫는다.
        /// </summary>
        // ------------------------------------------------------------
        public void Close()
        {
            if (!options.CanClose) return;
            if (driver.State == XeriWindowState.Closed) return;
            if (IsCancelled(OnPreClose)) return;

            SetState(XeriWindowState.Closed, OnClose);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 윈도우에 포커스를 부여한다.
        /// </summary>
        // ------------------------------------------------------------
        public void Focus()
        {
            if (!options.CanFocus) return;
            if (driver.State == XeriWindowState.Closed) return;

            OnFocus?.Invoke(this, CreateEventArgs());
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 윈도우 포커스를 해제한다.
        /// </summary>
        // ------------------------------------------------------------
        public void LoseFocus()
        {
            if (driver.State == XeriWindowState.Closed) return;

            OnLoseFocus?.Invoke(this, CreateEventArgs());
        }

    #endregion

    #region 내부 메서드

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 상태에서 위치와 크기를 변경할 수 있는지 확인한다.
        /// </summary>
        // ------------------------------------------------------------
        private bool CanChangeBounds()
        {
            return driver.State == XeriWindowState.Normal;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 옵션 범위 안으로 크기를 보정한다.
        /// </summary>
        // ------------------------------------------------------------
        private Vector2 ClampSize(Vector2 size)
        {
            return new Vector2
            (
                Mathf.Clamp(size.x, options.MinSize.x, options.MaxSize.x),
                Mathf.Clamp(size.y, options.MinSize.y, options.MaxSize.y)
            );
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 취소 가능한 이벤트를 호출하고 취소 여부를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        private bool IsCancelled(EventHandler<XeriWindowCancelEventArgs> preEvent)
        {
            if (preEvent == null) return false;

            var eventArgs = new XeriWindowCancelEventArgs
            {
                Pos    = driver.Pos,
                Size   = driver.Size,
                State  = driver.State,
                Cancel = false,
            };

            preEvent.Invoke(this, eventArgs);

            return eventArgs.Cancel;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 윈도우 상태를 변경하고 관련 이벤트를 호출한다.
        /// </summary>
        // ------------------------------------------------------------
        private void SetState
        (
            XeriWindowState state,
            EventHandler<XeriWindowEventArgs> stateEvent
        )
        {
            var previous = driver.State;
            if (previous == state) return;

            driver.State = state;

            var eventArgs = CreateEventArgs();

            stateEvent?.Invoke(this, eventArgs);
            OnStateChange?.Invoke(this, new ValueChangeEventArgs<XeriWindowState>(previous, state));
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 driver 상태로 이벤트 인자를 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        private XeriWindowEventArgs CreateEventArgs()
        {
            return new XeriWindowEventArgs
            {
                Pos   = driver.Pos,
                Size  = driver.Size,
                State = driver.State,
            };
        }

    #endregion

    }
}
