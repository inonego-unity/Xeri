/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : AutoScroller.cs
수정일 : 2026-11-20

# 설명
UI 시스템에 독립적인 휠 클릭 자동 스크롤 상태. 버튼을 누른 기준점과 지금 포인터 위치의 거리로 속도를 정하고,
Tick마다 대상(IAutoScrollTarget)을 그만큼 움직인다.

# 특이사항, 제약사항
- 누르고 거의 움직이지 않은 채 떼면 고정(Latched) 상태로 남아 계속 스크롤한다. 끝내기는 어댑터가 End로 한다.
- 누른 채 HoldDistance 이상 움직이면(Holding) 뗄 때 끝난다.
- 속도는 초 단위라 Tick 주기와 프레임률에 영향을 받지 않는다.
입력·좌표 변환·표시는 어댑터(UITK 매니퓰레이터 등)가 맡는다.
========================================================================= BLOCK_HEADER_END */

using System;

using UnityEngine;

using inonego;
using inonego.Xeri;
using inonego.Xeri.UI;
using inonego.Xeri.UI.DragDrop;

namespace inonego.Xeri.UI.AutoScroll
{
    // ============================================================
    /// <summary>
    /// 자동 스크롤 진행 단계.
    /// </summary>
    // ============================================================
    public enum AutoScrollPhase
    {
        // ------------------------------------------------------------
        /// <summary>
        /// 자동 스크롤 중이 아니다.
        /// </summary>
        // ------------------------------------------------------------
        Idle,

        // ------------------------------------------------------------
        /// <summary>
        /// 버튼을 누른 채 아직 많이 움직이지 않았다.
        /// </summary>
        // ------------------------------------------------------------
        Pressed,

        // ------------------------------------------------------------
        /// <summary>
        /// 버튼을 누른 채 끌고 있다. 떼면 끝난다.
        /// </summary>
        // ------------------------------------------------------------
        Holding,

        // ------------------------------------------------------------
        /// <summary>
        /// 버튼을 떼고도 계속 스크롤한다. 다시 누르면 끝난다.
        /// </summary>
        // ------------------------------------------------------------
        Latched,
    }

    // ============================================================
    /// <summary>
    /// 휠 클릭 자동 스크롤 상태.
    /// </summary>
    // ============================================================
    public sealed class AutoScroller
    {

    #region 필드

        private readonly IAutoScrollTarget target;

        // ------------------------------------------------------------
        /// <summary>
        /// 속도와 판정 기준.
        /// </summary>
        // ------------------------------------------------------------
        public AutoScrollSettings Settings { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 지금 단계.
        /// </summary>
        // ------------------------------------------------------------
        public AutoScrollPhase Phase { get; private set; } = AutoScrollPhase.Idle;

        // ------------------------------------------------------------
        /// <summary>
        /// 자동 스크롤 중인지 여부.
        /// </summary>
        // ------------------------------------------------------------
        public bool IsActive => Phase != AutoScrollPhase.Idle;

        // ------------------------------------------------------------
        /// <summary>
        /// 자동 스크롤을 시작한 입력 식별자. 자동 스크롤 중이 아니면 -1이다.
        /// </summary>
        // ------------------------------------------------------------
        public int PointerID { get; private set; } = -1;

        // ------------------------------------------------------------
        /// <summary>
        /// 버튼을 누른 기준점.
        /// </summary>
        // ------------------------------------------------------------
        public Vector2 Origin { get; private set; }

        // ------------------------------------------------------------
        /// <summary>
        /// 지금 포인터 위치.
        /// </summary>
        // ------------------------------------------------------------
        public Vector2 Pointer { get; private set; }

    #endregion

    #region 이벤트

        // ------------------------------------------------------------
        /// <summary>
        /// 자동 스크롤을 시작했을 때 알린다.
        /// </summary>
        // ------------------------------------------------------------
        public event Action<AutoScroller> Began;

        // ------------------------------------------------------------
        /// <summary>
        /// 자동 스크롤이 끝났을 때 알린다.
        /// </summary>
        // ------------------------------------------------------------
        public event Action<AutoScroller> Ended;

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// 대상과 설정으로 자동 스크롤 상태를 만든다. 설정이 없으면 기본값이다.
        /// </summary>
        // ------------------------------------------------------------
        public AutoScroller(IAutoScrollTarget target, AutoScrollSettings settings = null)
        {
            this.target = target ?? throw new ArgumentNullException(nameof(target));
            Settings = settings ?? new AutoScrollSettings();
        }

    #endregion

    #region 입력

        // ----------------------------------------------------------------------
        /// <summary>
        /// 버튼을 누른 위치를 기준점으로 자동 스크롤을 시작한다. 이미 하고 있으면 false다.
        /// </summary>
        // ----------------------------------------------------------------------
        public bool Begin(InputPoint input)
        {
            if (IsActive)
            {
                return false;
            }

            PointerID = input.ID;
            Origin = input.Pos;
            Pointer = input.Pos;
            Phase = AutoScrollPhase.Pressed;
            Began?.Invoke(this);
            return true;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 포인터 위치를 갱신한다. 누른 채 멀리 움직이면 끌기로 본다.
        /// </summary>
        // ------------------------------------------------------------
        public void Move(InputPoint input)
        {
            if (!IsActive || input.ID != PointerID)
            {
                return;
            }

            Pointer = input.Pos;
            if (Phase == AutoScrollPhase.Pressed && Vector2.Distance(Origin, Pointer) >= Settings.HoldDistance)
            {
                Phase = AutoScrollPhase.Holding;
            }
        }

        // --------------------------------------------------------------------------------
        /// <summary>
        /// <br/> 버튼을 뗐다. 끌고 있었으면 끝내고, 거의 움직이지 않았으면 고정 상태로 남는다.
        /// <br/> 끝났으면 true다.
        /// </summary>
        // --------------------------------------------------------------------------------
        public bool Release(InputPoint input)
        {
            if (!IsActive || input.ID != PointerID)
            {
                return false;
            }

            Pointer = input.Pos;
            if (Phase == AutoScrollPhase.Pressed)
            {
                Phase = AutoScrollPhase.Latched;
                return false;
            }

            End();
            return true;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 자동 스크롤을 끝낸다. 하고 있지 않으면 아무것도 하지 않는다.
        /// </summary>
        // ------------------------------------------------------------
        public void End()
        {
            if (!IsActive)
            {
                return;
            }

            Phase = AutoScrollPhase.Idle;
            PointerID = -1;
            Ended?.Invoke(this);
        }

    #endregion

    #region 진행

        // ------------------------------------------------------------
        /// <summary>
        /// 지금 스크롤 속도(px/s, 화면 방향). 정지 거리 안이면 0이다.
        /// </summary>
        // ------------------------------------------------------------
        public Vector2 Velocity
        {
            get
            {
                if (!IsActive)
                {
                    return Vector2.zero;
                }

                var offset = Pointer - Origin;
                var x = Settings.Horizontal ? AxisSpeed(offset.x) : 0f;
                var y = Settings.Vertical ? AxisSpeed(offset.y) : 0f;
                return new Vector2(x, y);
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 지난 시간(초)만큼 대상을 움직인다. 실제로 움직였으면 true다.
        /// </summary>
        // ------------------------------------------------------------
        public bool Tick(float deltaTime)
        {
            if (!IsActive || deltaTime <= 0f)
            {
                return false;
            }

            var delta = Velocity * deltaTime;
            return delta != Vector2.zero && target.ScrollBy(delta);
        }

        private float AxisSpeed(float offset)
        {
            var distance = Mathf.Abs(offset) - Settings.DeadZone;
            if (distance <= 0f)
            {
                return 0f;
            }

            return Mathf.Sign(offset) * Mathf.Min(distance * Settings.SpeedPerPixel, Settings.MaxSpeed);
        }

    #endregion

    }
}
