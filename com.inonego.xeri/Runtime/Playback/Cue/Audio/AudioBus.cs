/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : AudioBus.cs
수정일 : 2026-08-01

# 설명
AudioManager가 출력 정책을 구분하는 고정 Audio Bus를 정의한다.
========================================================================= BLOCK_HEADER_END */

namespace inonego.Xeri.Playback
{
    // ============================================================
    /// <summary>
    /// Audio Cue의 출력 용도와 Manager 제어 단위.
    /// </summary>
    // ============================================================
    public enum AudioBus
    {
        Music,
        SFX,
        UI,
        Voice,
        Ambience,
    }
}
