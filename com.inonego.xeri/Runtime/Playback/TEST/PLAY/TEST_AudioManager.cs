/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TEST_AudioManager.cs
수정일 : 2026-08-01

# 설명
AudioManager의 Master·Bus 출력, 동시 재생과 Bus별 종료 계약을 검증한다.

# 테스트 구성
 B: Master·Bus 출력
 L: 동시 재생과 종료
========================================================================= BLOCK_HEADER_END */

using UnityEngine;

using NUnit.Framework;

namespace inonego.Xeri.TEST._Playback
{
    using inonego.Xeri.Playback;

    // ============================================================
    /// <summary>
    /// Audio Manager 공개 계약 테스트.
    /// </summary>
    // ============================================================
    public sealed class TEST_AudioManager
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
        /// 같은 Host의 Player와 Manager를 실제 Awake 경로로 초기화한다.
        /// </summary>
        // ------------------------------------------------------------
        private static AudioManager CreateManager(GameObject root, AudioSource sourcePrefab)
        {
            AudioManager.Clear();
            root.SetActive(false);

            var player = root.AddComponent<UnityAudioCuePlayer>();
            player.SetSourceConfiguration(sourcePrefab, initialVoiceCount: 0);

            var manager = root.AddComponent<AudioManager>();

            root.SetActive(true);
            return manager;
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

        // ------------------------------------------------------------
        /// <summary>
        /// 지정 Clip을 사용하는 활성 voice 수를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        private static int CountActiveSources(GameObject root, AudioClip clip)
        {
            var count = 0;
            var sources = root.GetComponentsInChildren<AudioSource>(includeInactive: true);

            for (var i = 0; i < sources.Length; i++)
            {
                if (sources[i].gameObject.activeSelf && sources[i].clip == clip)
                {
                    count++;
                }
            }

            return count;
        }

    #endregion

    #region B-1: Master·Bus 출력

        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// Master·Bus Volume과 Mute는 해당 출력에 합성되고 Playback 개별 Volume은 유지된다.
        /// </summary>
        // ----------------------------------------------------------------------------------------------------
        [Test]
        public void TEST_AudioManager_Master와_Bus출력을_활성Playback에_반영()
        {
            var root = new GameObject("TEST_AudioManager");
            var sourcePrefab = CreateSourcePrefab();
            var sfxClip = CreateClip("TEST_SFX", 1.0f);
            var musicClip = CreateClip("TEST_Music", 1.0f);
            var sfxCue = new UnityAudioClipCue();
            var musicCue = new UnityAudioClipCue();

            try
            {
                sfxCue.Clip = sfxClip;
                sfxCue.Bus = AudioBus.SFX;
                sfxCue.Volume = 0.8f;

                musicCue.Clip = musicClip;
                musicCue.Bus = AudioBus.Music;
                musicCue.Volume = 0.5f;

                var manager = CreateManager(root, sourcePrefab);
                var sfxPlayback = manager.Play(sfxCue, volumeScale: 0.5f);
                var musicPlayback = manager.Play(musicCue);
                var sfxSource = FindActiveSource(root, sfxClip);
                var musicSource = FindActiveSource(root, musicClip);

                Assert.IsNotNull(sfxSource);
                Assert.IsNotNull(musicSource);
                Assert.AreEqual(0.4f, sfxPlayback.Volume, 0.001f);
                Assert.AreEqual(0.4f, sfxSource.volume, 0.001f);
                Assert.AreEqual(0.5f, musicSource.volume, 0.001f);
                Assert.AreEqual(0.8f, sfxCue.Volume, 0.001f);

                manager.SetMasterVolume(0.5f);
                manager.SetBusVolume(AudioBus.SFX, 0.5f);

                Assert.AreEqual(0.1f, sfxSource.volume, 0.001f);
                Assert.AreEqual(0.25f, musicSource.volume, 0.001f);

                sfxPlayback.Volume = 0.6f;

                Assert.AreEqual(0.6f, sfxPlayback.Volume, 0.001f);
                Assert.AreEqual(0.15f, sfxSource.volume, 0.001f);

                manager.SetBusMuted(AudioBus.SFX, isMuted: true);

                Assert.AreEqual(0.0f, sfxSource.volume, 0.001f);
                Assert.AreEqual(0.25f, musicSource.volume, 0.001f);

                manager.SetBusMuted(AudioBus.SFX, isMuted: false);
                manager.SetMuted(isMuted: true);

                Assert.AreEqual(0.0f, sfxSource.volume, 0.001f);
                Assert.AreEqual(0.0f, musicSource.volume, 0.001f);

                manager.SetMuted(isMuted: false);

                Assert.AreEqual(0.15f, sfxSource.volume, 0.001f);
                Assert.AreEqual(0.25f, musicSource.volume, 0.001f);

            }
            finally
            {
                Object.DestroyImmediate(root);
                AudioManager.Clear();
                Object.DestroyImmediate(sfxClip);
                Object.DestroyImmediate(musicClip);
                Object.DestroyImmediate(sourcePrefab.gameObject);
            }
        }

    #endregion

    #region L-1: 동시 재생과 종료

        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// CuePlaybackService 경로도 Manager 정책을 사용하며 Bus별 종료는 다른 Bus를 유지한다.
        /// </summary>
        // ----------------------------------------------------------------------------------------------------
        [Test]
        public void TEST_AudioManager_같은Bus를_동시재생하고_Bus별로_종료()
        {
            var root = new GameObject("TEST_AudioManager");
            var sourcePrefab = CreateSourcePrefab();
            var sfxClip = CreateClip("TEST_SFX", 1.0f);
            var musicClip = CreateClip("TEST_Music", 1.0f);
            var sfxCue = new UnityAudioClipCue();
            var musicCue = new UnityAudioClipCue();

            try
            {
                sfxCue.Clip = sfxClip;
                sfxCue.Bus = AudioBus.SFX;
                sfxCue.Volume = 0.8f;

                musicCue.Clip = musicClip;
                musicCue.Bus = AudioBus.Music;

                var manager = CreateManager(root, sourcePrefab);
                manager.SetBusVolume(AudioBus.SFX, 0.25f);

                var service = new CuePlaybackService(new ICuePlayer[] { manager });
                var firstSFX = service.Play(sfxCue) as IAudioPlayback;
                var secondSFX = manager.Play(sfxCue);
                var music = manager.Play(musicCue);

                Assert.IsNotNull(firstSFX);
                Assert.AreNotSame(firstSFX, secondSFX);
                Assert.AreEqual(2, CountActiveSources(root, sfxClip));

                var sources = root.GetComponentsInChildren<AudioSource>(includeInactive: true);
                for (var i = 0; i < sources.Length; i++)
                {
                    if (!sources[i].gameObject.activeSelf || sources[i].clip != sfxClip) continue;

                    Assert.AreEqual(0.2f, sources[i].volume, 0.001f);
                }

                manager.StopAll(AudioBus.SFX);

                Assert.AreEqual(CuePlaybackState.Released, firstSFX.State);
                Assert.AreEqual(CuePlaybackState.Released, secondSFX.State);
                Assert.AreEqual(CuePlaybackState.Playing, music.State);
                Assert.AreEqual(0, CountActiveSources(root, sfxClip));

                manager.StopAll();

                Assert.AreEqual(CuePlaybackState.Released, music.State);

            }
            finally
            {
                Object.DestroyImmediate(root);
                AudioManager.Clear();
                Object.DestroyImmediate(sfxClip);
                Object.DestroyImmediate(musicClip);
                Object.DestroyImmediate(sourcePrefab.gameObject);
            }
        }

    #endregion

    }
}
