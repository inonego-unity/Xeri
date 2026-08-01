/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : UnityAudioClipCue.cs
수정일 : 2026-07-31

# 설명
Unity AudioClip을 재생하는 Audio Cue Asset을 정의한다.
========================================================================= BLOCK_HEADER_END */

using UnityEngine;

namespace inonego.Xeri.Playback
{
    // ============================================================
    /// <summary>
    /// Unity AudioClip 기반 Audio Cue.
    /// </summary>
    // ============================================================
    [CreateAssetMenu(menuName = "Xeri/Playback/Unity Audio Clip Cue", fileName = "AudioCue")]
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
