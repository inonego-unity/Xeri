/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : UnityAudioClipCue.cs
수정일 : 2026-09-04

# 설명
Unity AudioClip과 공통 AudioCue 설정을 묶는 runtime 재생 정의를 제공한다.
========================================================================= BLOCK_HEADER_END */

using System;

using UnityEngine;

namespace inonego.Xeri.Playback
{
    // ============================================================
    /// <summary>
    /// Unity AudioClip 기반 runtime Audio Cue.
    /// </summary>
    // ============================================================
    [Serializable]
    public sealed class UnityAudioClipCue : AudioCue
    {

    #region 필드

        // ------------------------------------------------------------
        /// <summary>
        /// 재생할 Unity AudioClip.
        /// </summary>
        // ------------------------------------------------------------
        public AudioClip Clip
        {
            get => clip;
            set => clip = value;
        }

        [SerializeField]
        private AudioClip clip = null;

    #endregion

    }
}
