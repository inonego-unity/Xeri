/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : IReadOnlyTimer.cs
수정일 : 2026-11-19

# 설명
읽기 전용 타이머 인터페이스. ITimerProgress의 상태·시간 값 조회에 종료·상태 변경 관찰을 더한다.
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
    public interface IReadOnlyTimer : ITimerProgress
    {

    #region 이벤트

        // ------------------------------------------------------------
        /// <summary>
        /// 타이머가 종료될 때 발생한다.
        /// </summary>
        // ------------------------------------------------------------
        public event EventHandler<TimerEndEventArgs> OnEnd;

        // ------------------------------------------------------------
        /// <summary>
        /// 타이머 상태가 변경될 때 발생한다.
        /// </summary>
        // ------------------------------------------------------------
        public event ValueChangeEventHandler<TimerState> OnStateChange;

    #endregion

    }
}
