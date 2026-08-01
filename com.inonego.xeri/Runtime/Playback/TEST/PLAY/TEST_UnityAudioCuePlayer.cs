/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TEST_UnityAudioCuePlayer.cs
수정일 : 2026-07-31

# 설명
UnityAudioCuePlayer의 실제 AudioSource 재생 제어와 종료 수명 계약을 검증한다.

# 테스트 구성
 C: Audio Playback 제어
 L: Audio Playback 자연 종료
========================================================================= BLOCK_HEADER_END */

using System.Collections;

using UnityEngine;
using UnityEngine.TestTools;

using NUnit.Framework;

namespace inonego.Xeri.TEST._Playback
{
    using inonego.Xeri.Playback;

    // ============================================================
    /// <summary>
    /// Unity Audio Cue Player 공개 계약 테스트.
    /// </summary>
    // ============================================================
    public sealed class TEST_UnityAudioCuePlayer
    {
    #region 헬퍼

        // ------------------------------------------------------------
        /// <summary>
        /// 지정 길이의 무음 AudioClip을 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        private static AudioClip CreateClip(float duration)
        {
            const int frequency = 44100;
            var samples = Mathf.CeilToInt(duration * frequency);

            return AudioClip.Create
            (
                "TEST_UnityAudioCue",
                samples,
                channels: 1,
                frequency,
                stream: false
            );
        }

    #endregion

    #region C-1: Audio Playback 제어

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> Audio Cue 재생 결과는 Audio Clock과 Pause·Resume을 제공하고,
        /// <br/> 즉시 종료하면 생성한 AudioSource GameObject를 정리한다.
        /// </summary>
        // ----------------------------------------------------------------------
        [UnityTest]
        public IEnumerator TEST_UnityAudioCuePlayer_재생제어와_즉시종료가_같은Playback에_반영()
        {
            var root = new GameObject("TEST_UnityAudioCuePlayer");
            var clip = CreateClip(1.0f);
            var cue = ScriptableObject.CreateInstance<UnityAudioClipCue>();

            try
            {
                cue.Clip = clip;
                cue.Volume = 0.5f;
                cue.Pitch = -3.0f;

                var player = root.AddComponent<UnityAudioCuePlayer>();
                var service = new CuePlaybackService(new ICuePlayer[] { player });
                var playback = service.Play(cue) as IAudioPlayback;

                Assert.IsNotNull(playback);
                Assert.AreEqual(CuePlaybackState.Playing, playback.State);
                Assert.AreEqual(PlaybackState.Playing, playback.Clock.State);
                Assert.AreEqual(clip.length, playback.Clock.Duration, 0.001f);
                Assert.AreEqual(1, root.transform.childCount);
                Assert.AreEqual(-3.0f, root.GetComponentInChildren<AudioSource>().pitch);

                playback.Pause();

                Assert.AreEqual(CuePlaybackState.Playing, playback.State);
                Assert.AreEqual(PlaybackState.Paused, playback.Clock.State);
                Assert.IsTrue(playback.Clock.IsPaused);

                playback.Resume();

                Assert.AreEqual(PlaybackState.Playing, playback.Clock.State);
                Assert.IsTrue(playback.Clock.IsPlaying);

                playback.Dispose();

                Assert.AreEqual(CuePlaybackState.Released, playback.State);
                Assert.AreEqual(PlaybackState.Stopped, playback.Clock.State);

                yield return null;

                service.Tick();
                service.StopAll();
                Assert.AreEqual(0, root.transform.childCount);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(cue);
                UnityEngine.Object.DestroyImmediate(clip);
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

    #endregion

    #region L-1: Audio Playback 자연 종료

        // ----------------------------------------------------------------------
        /// <summary>
        /// Loop Audio의 Natural 종료는 현재 반복을 마친 뒤 Released로 수렴한다.
        /// </summary>
        // ----------------------------------------------------------------------
        [UnityTest]
        public IEnumerator TEST_UnityAudioCuePlayer_Loop의_Natural종료는_Draining후_자원을정리()
        {
            var root = new GameObject("TEST_UnityAudioCuePlayer");
            var clip = CreateClip(0.1f);
            var cue = ScriptableObject.CreateInstance<UnityAudioClipCue>();

            try
            {
                cue.Clip = clip;
                cue.IsLooping = true;

                var player = root.AddComponent<UnityAudioCuePlayer>();
                var service = new CuePlaybackService(new ICuePlayer[] { player });
                var playback = service.Play(cue) as IAudioPlayback;

                Assert.IsNotNull(playback);

                playback.Stop(CueStopMode.Natural);

                Assert.AreEqual(CuePlaybackState.Draining, playback.State);

                for
                (
                    var i = 0;
                    i < 120 && playback.State != CuePlaybackState.Released;
                    i++
                )
                {
                    yield return null;
                }

                Assert.AreEqual(CuePlaybackState.Released, playback.State);
                Assert.AreEqual(PlaybackState.Stopped, playback.Clock.State);
                Assert.AreEqual(playback.Clock.Duration, playback.Clock.Time, 0.001f);

                yield return null;

                Assert.AreEqual(0, root.transform.childCount);

                service.Tick();
                service.StopAll();
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(cue);
                UnityEngine.Object.DestroyImmediate(clip);
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

    #endregion

    }
}
