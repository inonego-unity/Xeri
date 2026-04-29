/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : IReadOnlyTimer.cs
수정일 : 2026-04-28

# 설명
읽기 전용 타이머 인터페이스. 상태·시간 값 조회만 노출한다.
========================================================================= BLOCK_HEADER_END */

using System;

using TValue = System.Single;

namespace inonego.Xeri.Utility
{
    // ============================================================
    /// <summary>
    /// 읽기 전용 타이머 인터페이스.
    /// </summary>
    // ============================================================
    public interface IReadOnlyTimer
    {

    #region 필드

        public bool IsRunning { get; }
        public bool IsPaused  { get; }

        public TimerState Current { get; }

    #endregion

    #region 이벤트

        public event EventHandler<TimerEndEventArgs> OnEnd;
        public event ValueChangeEventHandler<TimerState> OnStateChange;

    #endregion

    #region 시간

        public TValue Duration      { get; }
        public TValue ElapsedTime   { get; }
        public TValue RemainingTime { get; }

        public TValue ElapsedTime01   { get; }
        public TValue RemainingTime01 { get; }

    #endregion

    }
}
