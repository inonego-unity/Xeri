/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TEST_UnityAudioCuePlayer.cs
수정일 : 2026-08-01

# 설명
UnityAudioCuePlayer의 AudioSource Pool, 공간 재생과 Playback 수명 계약을 검증한다.

# 테스트 구성
 C: Audio Playback 제어
 S: 동시·공간 재생과 voice 재사용
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
        private static AudioClip CreateClip(string name, float duration)
        {
            const int frequency = 44100;
            var samples = Mathf.CeilToInt(duration * frequency);

            return AudioClip.Create
            (
                name,
                samples,
                channels: 1,
                frequency,
                stream: false
            );
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Pool이 복제할 비활성 AudioSource 원본을 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        private static AudioSource CreateSourcePrefab()
        {
            var gameObject = new GameObject("TEST_AudioSourcePrefab");
            gameObject.SetActive(false);

            var source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            return source;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 비활성 Host에서 source 설정을 완료한 뒤 실제 Awake 경로로 Player를 초기화한다.
        /// </summary>
        // ------------------------------------------------------------
        private static UnityAudioCuePlayer CreatePlayer(GameObject root, AudioSource sourcePrefab)
        {
            root.SetActive(false);

            var player = root.AddComponent<UnityAudioCuePlayer>();
            player.SetSourceConfiguration(sourcePrefab, initialVoiceCount: 0);

            root.SetActive(true);
            return player;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 지정 Clip을 재생 중인 활성 voice를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        private static AudioSource FindActiveSource(GameObject root, AudioClip clip)
        {
            var sources = root.GetComponentsInChildren<AudioSource>(includeInactive: true);

            for (var i = 0; i < sources.Length; i++)
            {
                if (!sources[i].gameObject.activeSelf || sources[i].clip != clip) continue;

                return sources[i];
            }

            return null;
        }

    #endregion

    #region C-1: Audio Playback 제어

        // ----------------------------------------------------------------------
        /// <summary>
        /// 재생 결과는 Clock과 Pause·Resume을 제공하고 즉시 종료하면 voice를 Pool로 반환한다.
        /// </summary>
        // ----------------------------------------------------------------------
        [Test]
        public void TEST_UnityAudioCuePlayer_재생제어와_즉시종료가_같은Playback에_반영()
        {
            var root = new GameObject("TEST_UnityAudioCuePlayer");
            var sourcePrefab = CreateSourcePrefab();
            var clip = CreateClip("TEST_Control", 1.0f);
            var cue = ScriptableObject.CreateInstance<UnityAudioClipCue>();

            try
            {
                cue.Clip = clip;
                cue.Volume = 0.5f;
                cue.Pitch = -2.0f;

                var player = CreatePlayer(root, sourcePrefab);
                var service = new CuePlaybackService(new ICuePlayer[] { player });
                var playback = service.Play(cue) as IAudioPlayback;
                var source = FindActiveSource(root, clip);

                Assert.IsNotNull(playback);
                Assert.IsNotNull(source);
                Assert.AreEqual(CuePlaybackState.Playing, playback.State);
                Assert.AreEqual(PlaybackState.Playing, playback.Clock.State);
                Assert.AreEqual(clip.length, playback.Clock.Duration, 0.001f);
                Assert.AreEqual(0.5f, source.volume, 0.001f);
                Assert.AreEqual(-2.0f, source.pitch);
                Assert.AreEqual(0.0f, source.spatialBlend);

                playback.Pause();

                Assert.AreEqual(CuePlaybackState.Playing, playback.State);
                Assert.AreEqual(PlaybackState.Paused, playback.Clock.State);

                playback.Resume();

                Assert.AreEqual(PlaybackState.Playing, playback.Clock.State);

                playback.Dispose();

                Assert.AreEqual(CuePlaybackState.Released, playback.State);
                Assert.AreEqual(PlaybackState.Stopped, playback.Clock.State);
                Assert.IsFalse(source.gameObject.activeSelf);
                Assert.IsNull(source.clip);

                Assert.AreEqual(1, root.transform.childCount);
            }
            finally
            {
                Object.DestroyImmediate(cue);
                Object.DestroyImmediate(clip);
                Object.DestroyImmediate(sourcePrefab.gameObject);
                Object.DestroyImmediate(root);
            }
        }

    #endregion

    #region S-1: 동시·공간 재생과 voice 재사용

        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// <br/> 동시 position·emitter 재생은 서로 다른 voice를 사용하고 emitter 이동을 반영한다.
        /// <br/> 반환된 voice는 후속 2D 재생에 재사용되며 이전 Playback은 새 재생을 변경하지 않는다.
        /// </summary>
        // ----------------------------------------------------------------------------------------------------
        [UnityTest]
        public IEnumerator TEST_UnityAudioCuePlayer_공간동시재생후_voice를_안전하게_재사용()
        {
            var root = new GameObject("TEST_UnityAudioCuePlayer");
            var emitter = new GameObject("TEST_Emitter");
            var sourcePrefab = CreateSourcePrefab();
            var positionClip = CreateClip("TEST_Position", 1.0f);
            var emitterClip = CreateClip("TEST_Emitter", 1.0f);
            var positionCue = ScriptableObject.CreateInstance<UnityAudioClipCue>();
            var emitterCue = ScriptableObject.CreateInstance<UnityAudioClipCue>();

            try
            {
                positionCue.Clip = positionClip;
                positionCue.SpatialBlend = 0.75f;
                positionCue.RolloffMode = AudioRolloffMode.Linear;
                positionCue.MinDistance = 2.0f;
                positionCue.MaxDistance = 15.0f;

                emitterCue.Clip = emitterClip;
                emitterCue.Volume = 0.6f;
                emitterCue.Pitch = 0.8f;
                emitterCue.SpatialBlend = 0.5f;

                var player = CreatePlayer(root, sourcePrefab);
                var positionPlayback = player.Play(positionCue, new Vector3(1.0f, 2.0f, 3.0f));

                emitter.transform.position = new Vector3(4.0f, 5.0f, 6.0f);
                var emitterPlayback = player.Play(emitterCue, emitter.transform);
                var positionSource = FindActiveSource(root, positionClip);
                var emitterSource = FindActiveSource(root, emitterClip);

                Assert.IsNotNull(positionSource);
                Assert.IsNotNull(emitterSource);
                Assert.AreNotSame(positionSource, emitterSource);
                Assert.AreEqual(new Vector3(1.0f, 2.0f, 3.0f), positionSource.transform.position);
                Assert.AreEqual(0.75f, positionSource.spatialBlend);
                Assert.AreEqual(AudioRolloffMode.Linear, positionSource.rolloffMode);
                Assert.AreEqual(2.0f, positionSource.minDistance);
                Assert.AreEqual(15.0f, positionSource.maxDistance);

                emitter.transform.position = new Vector3(7.0f, 8.0f, 9.0f);
                yield return null;

                Assert.AreEqual(emitter.transform.position, emitterSource.transform.position);

                positionPlayback.Dispose();
                emitterPlayback.Dispose();

                var reusedPlayback = player.Play(emitterCue);
                var reusedSource = FindActiveSource(root, emitterClip);

                Assert.IsNotNull(reusedSource);
                Assert.IsTrue
                (
                    ReferenceEquals(reusedSource, positionSource) ||
                    ReferenceEquals(reusedSource, emitterSource)
                );
                Assert.AreEqual(0.0f, reusedSource.spatialBlend);
                Assert.AreEqual(0.6f, reusedSource.volume, 0.001f);
                Assert.AreEqual(0.8f, reusedSource.pitch);

                positionPlayback.Volume = 0.1f;
                positionPlayback.Pitch = 3.0f;

                Assert.AreEqual(0.6f, reusedSource.volume, 0.001f);
                Assert.AreEqual(0.8f, reusedSource.pitch);

                reusedPlayback.Dispose();
            }
            finally
            {
                Object.DestroyImmediate(positionCue);
                Object.DestroyImmediate(emitterCue);
                Object.DestroyImmediate(positionClip);
                Object.DestroyImmediate(emitterClip);
                Object.DestroyImmediate(sourcePrefab.gameObject);
                Object.DestroyImmediate(emitter);
                Object.DestroyImmediate(root);
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
        public IEnumerator TEST_UnityAudioCuePlayer_Loop의_Natural종료는_Draining후_voice를_반환()
        {
            var root = new GameObject("TEST_UnityAudioCuePlayer");
            var sourcePrefab = CreateSourcePrefab();
            var clip = CreateClip("TEST_Natural", 0.1f);
            var cue = ScriptableObject.CreateInstance<UnityAudioClipCue>();

            try
            {
                cue.Clip = clip;
                cue.IsLooping = true;

                var player = CreatePlayer(root, sourcePrefab);
                var playback = player.Play(cue);
                var source = FindActiveSource(root, clip);

                playback.Stop(CueStopMode.Natural);

                Assert.AreEqual(CuePlaybackState.Draining, playback.State);

                for (var i = 0; i < 120 && playback.State != CuePlaybackState.Released; i++)
                {
                    yield return null;
                }

                Assert.AreEqual(CuePlaybackState.Released, playback.State);
                Assert.AreEqual(PlaybackState.Stopped, playback.Clock.State);
                Assert.AreEqual(playback.Clock.Duration, playback.Clock.Time, 0.001f);
                Assert.IsFalse(source.gameObject.activeSelf);
                Assert.IsNull(source.clip);
            }
            finally
            {
                Object.DestroyImmediate(cue);
                Object.DestroyImmediate(clip);
                Object.DestroyImmediate(sourcePrefab.gameObject);
                Object.DestroyImmediate(root);
            }
        }

    #endregion

    }
}
