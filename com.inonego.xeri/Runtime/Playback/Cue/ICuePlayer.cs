/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : ICuePlayer.cs
수정일 : 2026-08-10

# 설명
Cue 실행 Player의 공통 marker와 Runtime Binding 기반 실행 계약을 정의한다.

# 적용 범위
Player가 반환한 ICuePlayback 자체가 해당 Cue 실행의 런타임 인스턴스다.
Binding은 Cue 정의에 포함되지 않는 이번 실행의 런타임 대상·값만 전달한다.
========================================================================= BLOCK_HEADER_END */

namespace inonego.Xeri.Playback
{
    // ============================================================
    /// <summary>
    /// Cue Playback Service에 등록할 Player의 공통 marker.
    /// </summary>
    // ============================================================
    public interface ICuePlayer
    {
    }

    // ============================================================
    /// <summary>
    /// 지정 Runtime Binding으로 Cue를 실행하는 Player.
    /// </summary>
    /// <typeparam name="TBinding">이번 Cue 실행에 결합할 Runtime 값 타입.</typeparam>
    // ============================================================
    public interface ICuePlayer<TBinding> : ICuePlayer
        where TBinding : ICueBinding
    {

    #region 메서드

        // ------------------------------------------------------------
        /// <summary>
        /// 지정 Cue와 Runtime Binding 조합을 이 Player가 처리할 수 있는지 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        bool CanPlay
        (
            IPlaybackCue cue,
            in TBinding binding
        );

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> 지정 Runtime Binding으로 Cue를 실행하고 해당 실행의 종료 책임을 반환한다.
        /// <br/> 실행이 실패하면 Player가 부분 생성 자원을 직접 정리한 뒤 예외를 전달해야 한다.
        /// </summary>
        // ----------------------------------------------------------------------
        ICuePlayback Play
        (
            IPlaybackCue cue,
            in TBinding binding
        );

    #endregion

    }
}
