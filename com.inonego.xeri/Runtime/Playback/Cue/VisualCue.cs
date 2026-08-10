/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : VisualCue.cs
수정일 : 2026-08-10

# 설명
ParticleSystem, VFX Graph 등 시각 표현 Cue의 공통 authoring 기반을 정의한다.
구체 렌더링 기술과 Runtime Binding은 하위 Cue와 Player가 소유한다.
========================================================================= BLOCK_HEADER_END */

using UnityEngine;

namespace inonego.Xeri.Playback
{
    // ============================================================
    /// <summary>
    /// 시각 표현을 재생하는 Cue의 공통 기반 타입.
    /// </summary>
    // ============================================================
    public abstract class VisualCue : ScriptableObject, IPlaybackCue
    {
    }
}
