/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : UnityAudioCuePlayer.cs
수정일 : 2026-07-31

# 설명
UnityAudioClipCue를 AudioSource로 실행하고 생성된 Playback을 갱신한다.

# 적용 범위
Player Transform 아래에 재생별 AudioSource GameObject를 생성한다.
Bus, Mixer, Fade, 3D 배치와 Pool은 후속 Audio 구성의 책임이다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections.Generic;

using UnityEngine;

namespace inonego.Xeri.Playback
{
    // ============================================================
    /// <summary>
    /// Unity AudioClip Cue Player.
    /// </summary>
    // ============================================================
    public sealed class UnityAudioCuePlayer : MonoBehaviour, ICuePlayer
    {
    #region 필드

        private readonly List<UnityAudioPlayback> playbacks = new();

    #endregion

    #region 메서드

        // ------------------------------------------------------------
        /// <summary>
        /// UnityAudioClipCue를 처리할 수 있는지 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public bool CanPlay(IPlaybackCue cue)
        {
            return cue is UnityAudioClipCue;
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> UnityAudioClipCue를 새 AudioSource로 실행한다.
        /// <br/> 생성한 Playback이 AudioSource와 GameObject 종료를 소유한다.
        /// </summary>
        // ----------------------------------------------------------------------
        public ICuePlayback Play(IPlaybackCue cue)
        {
            if (!isActiveAndEnabled)
            {
                throw new InvalidOperationException("활성화된 UnityAudioCuePlayer만 Cue를 재생할 수 있습니다.");
            }

            if (cue is not UnityAudioClipCue audioCue)
            {
                throw new ArgumentException
                (
                    "UnityAudioCuePlayer는 UnityAudioClipCue만 재생할 수 있습니다.",
                    nameof(cue)
                );
            }

            if (audioCue.Clip == null)
            {
                throw new InvalidOperationException("Unity Audio Clip Cue에 AudioClip이 설정되지 않았습니다.");
            }

            if
            (
                float.IsNaN(audioCue.Volume) ||
                float.IsInfinity(audioCue.Volume) ||
                audioCue.Volume < 0.0f ||
                audioCue.Volume > 1.0f
            )
            {
                throw new InvalidOperationException("Unity Audio Clip Cue의 Volume이 유효하지 않습니다.");
            }

            if
            (
                float.IsNaN(audioCue.Pitch) ||
                float.IsInfinity(audioCue.Pitch) ||
                audioCue.Pitch < -3.0f ||
                audioCue.Pitch > 3.0f
            )
            {
                throw new InvalidOperationException("Unity Audio Clip Cue의 Pitch가 유효하지 않습니다.");
            }

            GameObject instance = null;

            try
            {
                var instanceName = string.IsNullOrEmpty(audioCue.name)
                    ? "Unity Audio Cue"
                    : $"{audioCue.name} Audio";

                instance = new GameObject(instanceName);
                instance.transform.SetParent(transform, false);

                var source = instance.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.spatialBlend = 0.0f;
                source.clip = audioCue.Clip;
                source.volume = audioCue.Volume;
                source.pitch = audioCue.Pitch;
                source.loop = audioCue.IsLooping;
                source.Play();

                var playback = new UnityAudioPlayback(instance, source);
                playbacks.Add(playback);
                return playback;
            }
            catch
            {
                // Playback이 공개되기 전 실패하면 이 호출에서 생성한 GameObject만 정리한다.
                if (instance != null)
                {
                    Destroy(instance);
                }

                throw;
            }
        }

    #endregion

    #region Unity 생명주기

        // ------------------------------------------------------------
        /// <summary>
        /// 생성한 Audio Playback의 자연 완료와 추적 제거를 진행한다.
        /// </summary>
        // ------------------------------------------------------------
        private void Update()
        {
            for (var i = playbacks.Count - 1; i >= 0; i--)
            {
                playbacks[i].Tick();

                if (playbacks[i].State != CuePlaybackState.Released) continue;

                playbacks.RemoveAt(i);
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Player가 비활성화되면 소유한 모든 Audio Playback을 즉시 종료한다.
        /// </summary>
        // ------------------------------------------------------------
        private void OnDisable()
        {
            for (var i = playbacks.Count - 1; i >= 0; i--)
            {
                playbacks[i].Dispose();
            }

            playbacks.Clear();
        }

    #endregion

    }
}
