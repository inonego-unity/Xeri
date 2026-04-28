/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : ITimer.cs
수정일 : 2026-04-28

# 설명
타이머 전체 인터페이스. 상태 제어·시간 값 읽기/쓰기를 모두 노출한다.
========================================================================= BLOCK_HEADER_END */

using System;

using TValue = System.Single;

namespace inonego.Xeri.Utility
{
    // ============================================================
    /// <summary>
    /// 타이머 인터페이스.
    /// </summary>
    // ============================================================
    public interface ITimer
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

        public TValue Duration { get; set; }

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> 타이머가 경과한 시간.
        /// <br/> 중지 후에도 Reset 또는 Start 전까지 값이 유지된다.
        /// </summary>
        // ------------------------------------------------------------
        public TValue ElapsedTime { get; set; }

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> 타이머의 남은 시간.
        /// <br/> 중지 후에도 Reset 또는 Start 전까지 값이 유지된다.
        /// </summary>
        // ------------------------------------------------------------
        public TValue RemainingTime { get; set; }

        public TValue ElapsedTime01   { get; }
        public TValue RemainingTime01 { get; }

    #endregion

    #region 메서드

        // ------------------------------------------------------------
        /// <summary>
        /// 타이머를 업데이트한다.
        /// </summary>
        // ------------------------------------------------------------
        public void Tick(TValue deltaTime);

        // ------------------------------------------------------------
        /// <summary>
        /// 타이머 작동을 시작한다.
        /// </summary>
        // ------------------------------------------------------------
        public void Start(TValue duration);

        // ------------------------------------------------------------
        /// <summary>
        /// 타이머 작동을 중지한다.
        /// </summary>
        // ------------------------------------------------------------
        public void Stop();

        // ------------------------------------------------------------
        /// <summary>
        /// 타이머 작동을 일시정지한다.
        /// </summary>
        // ------------------------------------------------------------
        public void Pause();

        // ------------------------------------------------------------
        /// <summary>
        /// 타이머 작동을 재개한다.
        /// </summary>
        // ------------------------------------------------------------
        public void Resume();

        // ------------------------------------------------------------
        /// <summary>
        /// 타이머를 리셋한다.
        /// </summary>
        // ------------------------------------------------------------
        public void Reset();

    #endregion

    }
}
