/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : UnityAudioClipCueAsset.cs
수정일 : 2026-09-04

# 설명
UnityAudioClipCue runtime 정의를 Unity Asset으로 authoring하고 보관한다.
========================================================================= BLOCK_HEADER_END */

using UnityEngine;

namespace inonego.Xeri.Playback
{
    // ============================================================
    /// <summary>
    /// UnityAudioClipCue를 직렬화하는 Audio Cue Asset.
    /// </summary>
    // ============================================================
    [CreateAssetMenu(menuName = "Xeri/Playback/Unity Audio Clip Cue", fileName = "AudioCue")]
    public sealed class UnityAudioClipCueAsset : AudioCueAsset
    {

    #region 필드

        // ------------------------------------------------------------
        /// <summary>
        /// Asset에 authoring된 runtime Audio Cue 정의.
        /// </summary>
        // ------------------------------------------------------------
        public new UnityAudioClipCue Cue => cue;

        // ------------------------------------------------------------
        /// <summary>
        /// Base AudioCueAsset의 내부 공통 계약에 concrete Cue를 제공한다.
        /// </summary>
        // ------------------------------------------------------------
        protected override AudioCue _cue => cue;

        [SerializeField]
        private UnityAudioClipCue cue = new();

    #endregion

    }
}
