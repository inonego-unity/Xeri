/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : ICueBinding.cs
수정일 : 2026-08-10

# 설명
Cue 실행에 전달할 Runtime Binding의 공통 marker 계약을 정의한다.

# 적용 범위
Binding은 Cue Asset에 저장되는 정의가 아니라 개별 Playback 호출 시점의 대상·값만 표현한다.
========================================================================= BLOCK_HEADER_END */

namespace inonego.Xeri.Playback
{
    // ============================================================
    /// <summary>
    /// Cue 실행에 사용할 Runtime Binding의 공통 marker.
    /// </summary>
    // ============================================================
    public interface ICueBinding
    {
        // NONE
    }
}
