/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : AudioCueAsset.cs
수정일 : 2026-09-04

# 설명
Unity Asset으로 authoring한 AudioCue runtime 정의를 제공하는 공통 wrapper를 선언한다.
========================================================================= BLOCK_HEADER_END */

using UnityEngine;

namespace inonego.Xeri.Playback
{
    // ============================================================
    /// <summary>
    /// Unity Asset으로 보관되는 Audio Cue authoring wrapper.
    /// </summary>
    // ============================================================
    public abstract class AudioCueAsset : ScriptableObject
    {

    #region 필드

        // ------------------------------------------------------------
        /// <summary>
        /// 이 Asset이 제공하는 runtime Audio Cue 정의.
        /// </summary>
        // ------------------------------------------------------------
        public AudioCue Cue => _cue;

        // ------------------------------------------------------------
        /// <summary>
        /// Concrete Asset이 보관하는 Audio Cue 정의를 내부 공통 계약으로 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        protected abstract AudioCue _cue { get; }

    #endregion

    }
}
