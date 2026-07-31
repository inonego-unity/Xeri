/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : ICuePlayback.cs
수정일 : 2026-07-31

# 설명
Cue를 실행해 생성된 런타임 Playback의 상태와 종료 계약을 정의한다.

# 종료 계약
Released는 Terminal이며 같은 Playback의 자원 종료를 다시 실행하지 않는다.
자연 종료를 지원하지 않는 Playback은 Natural 요청을 즉시 종료로 처리한다.
각 구현은 소유 자원의 실제 수명을 State에 반영하고 자연 종료 후 Released로 전환한다.
Dispose는 Stop(Immediate)과 같은 즉시 종료 계약이다.
종료 상태는 외부 자원 정리 전에 확정하며, 정리 실패를 자동 재시도하지 않는다.
========================================================================= BLOCK_HEADER_END */

using System;

namespace inonego.Xeri.Playback
{
    // ============================================================
    /// <summary>
    /// Cue Playback의 런타임 수명 상태.
    /// </summary>
    // ============================================================
    public enum CuePlaybackState
    {
        Playing,
        Draining,
        Released,
    }

    // ============================================================
    /// <summary>
    /// Cue Playback을 종료하는 방식.
    /// </summary>
    // ============================================================
    public enum CueStopMode
    {
        Immediate,
        Natural,
    }

    // ==========================================================================================
    /// <summary>
    /// <br/> Cue를 실행해 생성된 단일 런타임 Playback.
    /// <br/> Dispose는 즉시 종료 요청이며 종료 정리 실패를 자동 재시도하지 않는다.
    /// </summary>
    // ==========================================================================================
    public interface ICuePlayback : IDisposable
    {
    #region 필드

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 Playback 수명 상태.
        /// </summary>
        // ------------------------------------------------------------
        CuePlaybackState State { get; }

    #endregion

    #region 메서드

        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// <br/> 지정 방식으로 Playback 종료를 요청한다.
        /// <br/> Playing의 Natural 요청은 남은 표현이 있으면 Draining으로 전환한다.
        /// <br/> 남은 표현이 없거나 Natural을 지원하지 않으면 Released로 전환한다.
        /// <br/> Draining의 Natural 요청은 반복하지 않고 Immediate 요청만 Released로 전환한다.
        /// <br/> Released 이후 호출은 같은 자원 종료를 다시 실행하지 않는다.
        /// </summary>
        // ----------------------------------------------------------------------------------------------------
        void Stop(CueStopMode mode = CueStopMode.Immediate);

    #endregion

    }
}
