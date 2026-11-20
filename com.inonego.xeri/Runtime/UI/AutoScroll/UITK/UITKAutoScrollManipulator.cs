/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : UITKAutoScrollManipulator.cs
수정일 : 2026-11-20

# 설명
UI Toolkit 포인터·키 입력을 Core AutoScroller에 연결하는 Manipulator. 붙인 요소 위에서 가운데 버튼(기본)을
누르면 자동 스크롤을 시작하고, 주기마다 AutoScroller.Tick으로 대상을 움직이며 표시기를 갱신한다.

# 특이사항, 제약사항
- 자동 스크롤 중에는 포인터를 잡아 요소 밖에서도 위치를 받는다.
- 고정 상태에서 아무 버튼이나 누르면 끝나고, 그 누름은 아래 요소로 넘기지 않는다(셀 선택 등이 일어나지 않게).
- Escape는 포커스와 관계없이 받도록 자동 스크롤 중에만 패널 최상위에 등록한다.
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

        private IVisualElementScheduledItem ticker;
        private VisualElement keyRoot;

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
            Scroller = new AutoScroller(scrollTarget, settings);
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
            Stop();
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

            target.CapturePointer(evt.pointerId);
            keyRoot = target.panel?.visualTree;
            keyRoot?.RegisterCallback<KeyDownEvent>(HandleKeyDown, TrickleDown.TrickleDown);
            Indicator?.Show(evt.position);
            ticker?.Resume();
        }

        private void Stop()
        {
            var pointerID = Scroller.PointerID;
            Scroller.End();
            ticker?.Pause();
            Indicator?.Hide();

            keyRoot?.UnregisterCallback<KeyDownEvent>(HandleKeyDown, TrickleDown.TrickleDown);
            keyRoot = null;

            if (target != null && pointerID >= 0 && target.HasPointerCapture(pointerID))
            {
                target.ReleasePointer(pointerID);
            }
        }

    #endregion

    #region 이벤트 핸들러

        private void HandlePointerDown(PointerDownEvent evt)
        {
            // 고정 상태에서 누른 버튼은 종료로만 쓰고 아래 요소에 넘기지 않는다.
            if (Scroller.IsActive)
            {
                Stop();
                evt.StopPropagation();
                return;
            }

            if (evt.button != Button)
            {
                return;
            }

            Start(evt);
            evt.StopPropagation();
        }

        private void HandlePointerMove(PointerMoveEvent evt)
        {
            if (!Scroller.IsActive || evt.pointerId != Scroller.PointerID)
            {
                return;
            }

            Scroller.Move(new InputPoint(evt.pointerId, evt.position));
            Indicator?.SetVelocity(Scroller.Velocity);
            evt.StopPropagation();
        }

        private void HandlePointerUp(PointerUpEvent evt)
        {
            if (!Scroller.IsActive || evt.pointerId != Scroller.PointerID || evt.button != Button)
            {
                return;
            }

            if (Scroller.Release(new InputPoint(evt.pointerId, evt.position)))
            {
                Stop();
            }

            evt.StopPropagation();
        }

        private void HandleKeyDown(KeyDownEvent evt)
        {
            if (Scroller.IsActive && evt.keyCode == KeyCode.Escape)
            {
                Stop();
                evt.StopPropagation();
            }
        }

        private void HandleCaptureOut(PointerCaptureOutEvent evt)
        {
            // 다른 요소가 포인터를 가져가면 위치를 더 받을 수 없으므로 끝낸다.
            if (Scroller.IsActive)
            {
                Stop();
            }
        }

        private void HandleDetach(DetachFromPanelEvent evt)
        {
            Stop();
        }

        private void HandleTick(TimerState state)
        {
            if (!Scroller.IsActive)
            {
                ticker?.Pause();
                return;
            }

            Scroller.Tick(state.deltaTime / 1000f);
            Indicator?.SetVelocity(Scroller.Velocity);
        }

    #endregion

    }
}
