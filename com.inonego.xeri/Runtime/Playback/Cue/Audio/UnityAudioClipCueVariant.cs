/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : UnityAudioClipCueVariant.cs
수정일 : 2026-09-05

# 설명
Unity AudioClip과 공통 AudioCueVariant 설정을 묶는 authoring Variant를 제공한다.
========================================================================= BLOCK_HEADER_END */

using System;

using UnityEngine;

namespace inonego.Xeri.Playback
{
    // ============================================================
    /// <summary>
    /// Unity AudioClip 기반 Audio Cue Variant.
    /// </summary>
    // ============================================================
    [Serializable]
    public sealed class UnityAudioClipCueVariant : AudioCueVariant
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
