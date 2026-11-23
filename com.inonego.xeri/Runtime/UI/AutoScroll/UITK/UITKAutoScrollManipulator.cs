/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : UITKAutoScrollManipulator.cs
수정일 : 2026-11-22

# 설명
UI Toolkit 포인터·키 입력을 Core AutoScroller에 연결하는 Manipulator. 붙인 요소 위에서 가운데 버튼(기본)을
누르면 자동 스크롤을 시작하고, 주기마다 AutoScroller.Tick으로 대상을 움직이며 표시기를 갱신한다.

# 특이사항, 제약사항
- 시작은 붙인 요소 위에서 누를 때만이고, 대상에 움직일 내용이 없으면(CanScroll이 false) 시작하지 않고 누름을 넘긴다.
- 버튼을 누르고 있는 동안(Pressed·Holding)은 포인터를 잡아 요소 밖에서도 위치를 받는다.
- 고정 상태(Latched)에서는 포인터를 잡지 않고 패널 최상위에서 위치·누름·Escape를 받는다.
  그래서 다른 요소의 입력을 가로채지 않는다.
- 고정 상태에서 아무 버튼이나 누르면 끝난다. 붙인 요소 위의 누름은 아래 요소로 넘기지 않고(셀 선택 등이 일어나지 않게),
  밖의 누름은 그대로 그 요소로 간다(다른 표에서 가운데 버튼을 누르면 이것은 끝나고 그 표가 시작한다).
- 끝내기는 모두 Scroller.End(또는 끌던 중 뗄 때의 Release)로 하고, 정리(표시·패널 등록·포인터)는 Scroller.Ended 한 곳에서 한다.
- Editor 창과 Runtime 패널 모두 UI Toolkit 기본 API만 써서 같은 방식으로 동작한다.
========================================================================= BLOCK_HEADER_END */

using System;

using UnityEngine;
using UnityEngine.UIElements;

using inonego;
using inonego.Xeri;
using inonego.Xeri.UI;
using inonego.Xeri.UI.DragDrop;

namespace inonego.Xeri.UI.AutoScroll
{
    // ============================================================
    /// <summary>
    /// UI Toolkit 자동 스크롤 Manipulator.
    /// </summary>
    // ============================================================
    public sealed class UITKAutoScrollManipulator : Manipulator
    {

    #region 필드

        private const long TICK_INTERVAL_MS = 16;
        private const int MIDDLE_BUTTON = 2;

        private readonly IAutoScrollTarget scrollTarget;
        private IVisualElementScheduledItem ticker;
        private VisualElement panelRoot;

        // 붙인 요소가 잡은 포인터. 잡지 않았으면 -1이다. 포인터를 잡고 놓는 것은 이 Manipulator의 몫이다.
        private int capturedPointerID = -1;

        // ------------------------------------------------------------
        /// <summary>
        /// 자동 스크롤 상태.
        /// </summary>
        // ------------------------------------------------------------
        public AutoScroller Scroller { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 자동 스크롤을 시작할 버튼. 기본은 가운데 버튼(2)이다.
        /// </summary>
        // ------------------------------------------------------------
        public int Button { get; set; } = MIDDLE_BUTTON;

        // --------------------------------------------------------------------------------
        /// <summary>
        /// <br/> 기준점 표시기. null이면 붙일 때 기본 표시기(UITKAutoScrollIndicator)를 만든다.
        /// <br/> 표시가 필요 없으면 아무것도 하지 않는 구현을 넣는다.
        /// </summary>
        // --------------------------------------------------------------------------------
        public IAutoScrollIndicator Indicator { get; set; }

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// 대상과 설정으로 Manipulator를 만든다. 설정이 없으면 기본값이다.
        /// </summary>
        // ------------------------------------------------------------
        public UITKAutoScrollManipulator(IAutoScrollTarget scrollTarget, AutoScrollSettings settings = null)
        {
            this.scrollTarget = scrollTarget ?? throw new ArgumentNullException(nameof(scrollTarget));
            Scroller = new AutoScroller(scrollTarget, settings);

            // 어떤 경로로 끝나든(뗌·누름·Escape·포인터 상실·외부 End) 정리는 한 곳에서 한다.
            Scroller.Ended += _ => CleanUp();
        }

    #endregion

    #region 콜백 등록

        // ------------------------------------------------------------
        /// <summary>
        /// target 요소에 포인터 이벤트와 주기 실행을 등록한다.
        /// </summary>
        // ------------------------------------------------------------
        protected override void RegisterCallbacksOnTarget()
        {
            Indicator ??= new UITKAutoScrollIndicator(target);

            // 아래 요소(셀 등)보다 먼저 받아 시작·종료 누름이 그쪽으로 가지 않게 한다.
            target.RegisterCallback<PointerDownEvent>(HandlePointerDown, TrickleDown.TrickleDown);
            target.RegisterCallback<PointerMoveEvent>(HandlePointerMove);
            target.RegisterCallback<PointerUpEvent>(HandlePointerUp);
            target.RegisterCallback<PointerCaptureOutEvent>(HandleCaptureOut);
            target.RegisterCallback<DetachFromPanelEvent>(HandleDetach);

            ticker = target.schedule.Execute(HandleTick).Every(TICK_INTERVAL_MS);
            ticker.Pause();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// target 요소에서 등록을 해제하고 진행 중인 자동 스크롤을 끝낸다.
        /// </summary>
        // ------------------------------------------------------------
        protected override void UnregisterCallbacksFromTarget()
        {
            Scroller.End();
            target.UnregisterCallback<PointerDownEvent>(HandlePointerDown, TrickleDown.TrickleDown);
            target.UnregisterCallback<PointerMoveEvent>(HandlePointerMove);
            target.UnregisterCallback<PointerUpEvent>(HandlePointerUp);
            target.UnregisterCallback<PointerCaptureOutEvent>(HandleCaptureOut);
            target.UnregisterCallback<DetachFromPanelEvent>(HandleDetach);
            ticker?.Pause();
            ticker = null;
        }

    #endregion

    #region 시작과 종료

        private void Start(PointerDownEvent evt)
        {
            if (!Scroller.Begin(new InputPoint(evt.pointerId, evt.position)))
            {
                return;
            }

            // 누르고 있는 동안은 포인터를 잡아 붙인 요소가 위치를 받고, 고정 상태의 위치·누름과 Escape는 패널 최상위에서 받는다.
            target.CapturePointer(evt.pointerId);
            capturedPointerID = evt.pointerId;
            panelRoot = target.panel?.visualTree;
            panelRoot?.RegisterCallback<PointerMoveEvent>(HandlePanelPointerMove, TrickleDown.TrickleDown);
            panelRoot?.RegisterCallback<PointerDownEvent>(HandlePanelPointerDown, TrickleDown.TrickleDown);
            panelRoot?.RegisterCallback<KeyDownEvent>(HandleKeyDown, TrickleDown.TrickleDown);
            Indicator?.Show(evt.position);
            ticker?.Resume();
        }

        // --------------------------------------------------------------------------------
        /// <summary>
        /// 자동 스크롤이 끝나면 주기 실행·표시·패널 등록·포인터를 정리한다(Scroller.Ended).
        /// </summary>
        // --------------------------------------------------------------------------------
        private void CleanUp()
        {
            ticker?.Pause();
            Indicator?.Hide();

            panelRoot?.UnregisterCallback<PointerMoveEvent>(HandlePanelPointerMove, TrickleDown.TrickleDown);
            panelRoot?.UnregisterCallback<PointerDownEvent>(HandlePanelPointerDown, TrickleDown.TrickleDown);
            panelRoot?.UnregisterCallback<KeyDownEvent>(HandleKeyDown, TrickleDown.TrickleDown);
            panelRoot = null;

            ReleaseCapture();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 붙인 요소가 잡은 포인터를 놓는다. 잡지 않았으면 아무것도 하지 않는다.
        /// </summary>
        // ------------------------------------------------------------
        private void ReleaseCapture()
        {
            var pointerID = capturedPointerID;
            capturedPointerID = -1;
            if (target != null && pointerID >= 0 && target.HasPointerCapture(pointerID))
            {
                target.ReleasePointer(pointerID);
            }
        }

    #endregion

    #region 이벤트 핸들러

        private void HandlePointerDown(PointerDownEvent evt)
        {
            // 누르고 있는 동안(포인터를 잡은 상태) 다른 버튼을 누르면 끝내고 아래 요소에 넘기지 않는다.
            if (Scroller.IsActive)
            {
                Scroller.End();
                evt.StopPropagation();
                return;
            }

            // 움직일 내용이 없으면 시작하지 않고 누름을 그대로 넘긴다(행이 높이에 다 들어간 표 등).
            if (evt.button != Button || !scrollTarget.CanScroll)
            {
                return;
            }

            Start(evt);
            evt.StopPropagation();
        }

        // --------------------------------------------------------------------------------
        /// <summary>
        /// <br/> 고정 상태에서 패널 어디든 누르면 끝낸다. 붙인 요소 위의 누름만 아래로 넘기지 않고,
        /// <br/> 밖의 누름은 원래 요소로 보낸다.
        /// </summary>
        // --------------------------------------------------------------------------------
        private void HandlePanelPointerDown(PointerDownEvent evt)
        {
            if (Scroller.Phase != AutoScrollPhase.Latched)
            {
                return;
            }

            // 영역 겹침이 아니라 실제로 누른 요소가 붙인 요소 안에 있는지로 본다(위에 뜬 팝업 등은 밖이다).
            var inside = evt.target is VisualElement pressed && (pressed == target || target.Contains(pressed));
            Scroller.End();
            if (inside)
            {
                evt.StopPropagation();
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 누르고 있는 동안의 위치. 포인터를 잡았으므로 붙인 요소가 받는다.
        /// </summary>
        // ------------------------------------------------------------
        private void HandlePointerMove(PointerMoveEvent evt)
        {
            if (Scroller.Phase == AutoScrollPhase.Latched)
            {
                return;
            }

            MovePointer(evt);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 고정 상태의 위치. 포인터를 잡지 않으므로 패널 최상위에서 받는다.
        /// </summary>
        // ------------------------------------------------------------
        private void HandlePanelPointerMove(PointerMoveEvent evt)
        {
            if (Scroller.Phase != AutoScrollPhase.Latched)
            {
                return;
            }

            MovePointer(evt);
        }

        private void MovePointer(PointerMoveEvent evt)
        {
            if (!Scroller.IsActive || evt.pointerId != Scroller.PointerID)
            {
                return;
            }

            Scroller.Move(new InputPoint(evt.pointerId, evt.position));
            Indicator?.SetVelocity(Scroller.Velocity);
        }

        private void HandlePointerUp(PointerUpEvent evt)
        {
            if (!Scroller.IsActive || evt.pointerId != Scroller.PointerID || evt.button != Button)
            {
                return;
            }

            // 끌던 중 뗐으면 Release가 끝내고 Ended가 정리한다.
            // 고정 상태가 되면 포인터만 놓아 다른 요소의 입력을 가로채지 않는다.
            if (!Scroller.Release(new InputPoint(evt.pointerId, evt.position)))
            {
                ReleaseCapture();
            }

            evt.StopPropagation();
        }

        private void HandleKeyDown(KeyDownEvent evt)
        {
            if (Scroller.IsActive && evt.keyCode == KeyCode.Escape)
            {
                Scroller.End();
                evt.StopPropagation();
            }
        }

        private void HandleCaptureOut(PointerCaptureOutEvent evt)
        {
            // 누르고 있는 동안 다른 요소가 포인터를 가져가면 위치를 더 받을 수 없으므로 끝낸다.
            // 고정 상태는 스스로 포인터를 놓은 것이므로 계속한다.
            if (Scroller.IsActive && Scroller.Phase != AutoScrollPhase.Latched)
            {
                Scroller.End();
            }
        }

        private void HandleDetach(DetachFromPanelEvent evt)
        {
            Scroller.End();
        }

        private void HandleTick(TimerState state)
        {
            Scroller.Tick(state.deltaTime / 1000f);
            Indicator?.SetVelocity(Scroller.Velocity);
        }

    #endregion

    }
}
