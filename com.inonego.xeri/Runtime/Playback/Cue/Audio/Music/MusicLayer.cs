/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : MusicLayer.cs
수정일 : 2026-08-19

# 설명
동기 Music Layer Group 안에서 재생할 단일 Audio Cue 참조를 정의한다.
========================================================================= BLOCK_HEADER_END */

using System;

using UnityEngine;

namespace inonego.Xeri.Playback
{
    // ============================================================
    /// <summary>
    /// Music Layer Group을 구성하는 단일 Audio Cue 항목.
    /// </summary>
    // ============================================================
    [Serializable]
    public sealed class MusicLayer
    {

    #region 필드

        // ------------------------------------------------------------
        /// <summary>
        /// 이 Layer에서 재생할 Audio Cue.
        /// </summary>
        // ------------------------------------------------------------
        public AudioCue Cue
        {
            get => cue;
            set => cue = value;
        }

        [SerializeField]
        private AudioCue cue = null;

    #endregion

    }
}
