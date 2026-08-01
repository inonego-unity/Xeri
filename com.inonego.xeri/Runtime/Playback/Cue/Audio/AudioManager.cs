/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : AudioManager.cs
수정일 : 2026-08-01

# 설명
Audio Cue의 정면 재생 API와 Master·Bus 출력 정책을 제공한다.

# 적용 범위
UnityAudioCuePlayer를 통해 2D, 고정 위치 3D와 emitter 추적 3D를 재생한다.
게임별 BGM 선택, Fade, Adaptive Music과 외부 Audio Backend 정책은 소유하지 않는다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Audio;

using inonego.Xeri.Serializable;

namespace inonego.Xeri.Playback
{
    // ============================================================
    /// <summary>
    /// Unity Audio Cue의 전역 재생과 Bus 출력을 관리한다.
    /// </summary>
    // ============================================================
    [RequireComponent(typeof(UnityAudioCuePlayer))]
    public sealed class AudioManager : MonoSingleton<AudioManager>, ICuePlayer
    {
    #region 내부 데이터

        // ============================================================
        /// <summary>
        /// Audio Bus 하나의 출력 설정.
        /// </summary>
        // ============================================================
        [Serializable]
        private sealed class AudioBusSettings
        {
        #region 필드

            // ------------------------------------------------------------
            /// <summary>
            /// Bus에 적용되는 Volume.
            /// </summary>
            // ------------------------------------------------------------
            public float Volume
            {
                get => volume;
                set => volume = value;
            }

            [SerializeField]
            [Range(0.0f, 1.0f)]
            private float volume = 1.0f;

            // ------------------------------------------------------------
            /// <summary>
            /// Bus 출력의 음소거 여부.
            /// </summary>
            // ------------------------------------------------------------
            public bool IsMuted
            {
                get => isMuted;
                set => isMuted = value;
            }

            [SerializeField]
            private bool isMuted = false;

            // ------------------------------------------------------------
            /// <summary>
            /// Bus에 연결된 선택적 AudioMixerGroup.
            /// </summary>
            // ------------------------------------------------------------
            public AudioMixerGroup Output => output;

            [SerializeField]
            private AudioMixerGroup output = null;

        #endregion

        }

        // ============================================================
        /// <summary>
        /// Manager가 추적하는 Playback과 해당 Audio Bus.
        /// </summary>
        // ============================================================
        private readonly struct ManagedAudioPlayback
        {
        #region 필드

            // ------------------------------------------------------------
            /// <summary>
            /// 실제 Unity Audio Playback.
            /// </summary>
            // ------------------------------------------------------------
            public UnityAudioPlayback Playback { get; }

            // ------------------------------------------------------------
            /// <summary>
            /// Playback에 적용되는 Audio Bus.
            /// </summary>
            // ------------------------------------------------------------
            public AudioBus Bus { get; }

        #endregion

        #region 생성자

            // ------------------------------------------------------------
            /// <summary>
            /// Playback과 Bus로 Manager 추적 항목을 생성한다.
            /// </summary>
            // ------------------------------------------------------------
            public ManagedAudioPlayback(UnityAudioPlayback playback, AudioBus bus)
            {
                Playback = playback;
                Bus = bus;
            }

        #endregion

        }

    #endregion

    #region 필드

        [SerializeField]
        private string slotKey = DEFAULT_SLOT;

        // ------------------------------------------------------------
        /// <summary>
        /// 모든 Bus 위에 적용되는 Master Volume.
        /// </summary>
        // ------------------------------------------------------------
        public float MasterVolume => masterVolume;

        [SerializeField]
        [Range(0.0f, 1.0f)]
        private float masterVolume = 1.0f;

        // ------------------------------------------------------------
        /// <summary>
        /// 전체 Audio 출력 음소거 여부.
        /// </summary>
        // ------------------------------------------------------------
        public bool IsMuted => isMuted;

        [SerializeField]
        private bool isMuted = false;

        [SerializeField]
        private XDictionary_VV<AudioBus, AudioBusSettings> busSettings = new()
        {
            [AudioBus.Music]    = new(),
            [AudioBus.SFX]      = new(),
            [AudioBus.UI]       = new(),
            [AudioBus.Voice]    = new(),
            [AudioBus.Ambience] = new(),
        };

        private UnityAudioCuePlayer player = null;
        private readonly List<ManagedAudioPlayback> playbacks = new();

    #endregion

    #region 메서드

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 Unity Player가 지정 Cue를 처리할 수 있는지 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public bool CanPlay(IPlaybackCue cue)
        {
            return player != null && player.CanPlay(cue);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Audio Cue를 2D로 재생한다.
        /// </summary>
        // ------------------------------------------------------------
        public IAudioPlayback Play(AudioCue cue, float volumeScale = 1.0f)
        {
            ValidatePlay(cue, volumeScale);

            var bus = cue.Bus;
            var playback = player.Play
            (
                cue,
                cue.Volume * volumeScale,
                GetBusSettings(bus).Output,
                CalculateOutputVolume(bus)
            );

            playbacks.Add(new ManagedAudioPlayback(playback, bus));
            return playback;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Audio Cue를 지정 월드 위치에서 3D로 재생한다.
        /// </summary>
        // ------------------------------------------------------------
        public IAudioPlayback Play
        (
            AudioCue cue,
            Vector3 position,
            float volumeScale = 1.0f
        )
        {
            ValidatePlay(cue, volumeScale);

            var bus = cue.Bus;
            var playback = player.Play
            (
                cue,
                position,
                cue.Volume * volumeScale,
                GetBusSettings(bus).Output,
                CalculateOutputVolume(bus)
            );

            playbacks.Add(new ManagedAudioPlayback(playback, bus));
            return playback;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Audio Cue를 emitter의 월드 위치를 따라가는 3D로 재생한다.
        /// </summary>
        // ------------------------------------------------------------
        public IAudioPlayback Play
        (
            AudioCue cue,
            Transform emitter,
            float volumeScale = 1.0f
        )
        {
            if (emitter == null)
            {
                throw new ArgumentNullException(nameof(emitter));
            }

            ValidatePlay(cue, volumeScale);

            var bus = cue.Bus;
            var playback = player.Play
            (
                cue,
                emitter,
                cue.Volume * volumeScale,
                GetBusSettings(bus).Output,
                CalculateOutputVolume(bus)
            );

            playbacks.Add(new ManagedAudioPlayback(playback, bus));
            return playback;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Master Volume을 변경하고 모든 활성 Playback에 반영한다.
        /// </summary>
        // ------------------------------------------------------------
        public void SetMasterVolume(float volume)
        {
            if (float.IsNaN(volume) || float.IsInfinity(volume) || volume < 0.0f || volume > 1.0f)
            {
                throw new ArgumentOutOfRangeException
                (
                    nameof(volume),
                    "Master Volume은 0 이상 1 이하의 유한한 값이어야 합니다."
                );
            }

            masterVolume = volume;
            RefreshOutputs();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 전체 Audio 음소거 상태를 변경하고 모든 활성 Playback에 반영한다.
        /// </summary>
        // ------------------------------------------------------------
        public void SetMuted(bool isMuted)
        {
            this.isMuted = isMuted;
            RefreshOutputs();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 지정 Bus의 현재 Volume을 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public float GetBusVolume(AudioBus bus)
        {
            return GetBusSettings(bus).Volume;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 지정 Bus의 Volume을 변경하고 해당 활성 Playback에 반영한다.
        /// </summary>
        // ------------------------------------------------------------
        public void SetBusVolume(AudioBus bus, float volume)
        {
            if (float.IsNaN(volume) || float.IsInfinity(volume) || volume < 0.0f || volume > 1.0f)
            {
                throw new ArgumentOutOfRangeException
                (
                    nameof(volume),
                    "Bus Volume은 0 이상 1 이하의 유한한 값이어야 합니다."
                );
            }

            GetBusSettings(bus).Volume = volume;

            RefreshOutputs(bus);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 지정 Bus의 현재 음소거 여부를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public bool IsBusMuted(AudioBus bus)
        {
            return GetBusSettings(bus).IsMuted;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 지정 Bus의 음소거 상태를 변경하고 해당 활성 Playback에 반영한다.
        /// </summary>
        // ------------------------------------------------------------
        public void SetBusMuted(AudioBus bus, bool isMuted)
        {
            GetBusSettings(bus).IsMuted = isMuted;

            RefreshOutputs(bus);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Manager가 추적하는 모든 Audio Playback을 즉시 종료한다.
        /// </summary>
        // ------------------------------------------------------------
        public void StopAll()
        {
            for (var i = playbacks.Count - 1; i >= 0; i--)
            {
                playbacks[i].Playback.Dispose();
            }

            playbacks.Clear();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Manager가 추적하는 지정 Bus의 Audio Playback을 즉시 종료한다.
        /// </summary>
        // ------------------------------------------------------------
        public void StopAll(AudioBus bus)
        {
            GetBusSettings(bus);

            for (var i = playbacks.Count - 1; i >= 0; i--)
            {
                if (playbacks[i].Bus != bus) continue;

                playbacks[i].Playback.Dispose();
                playbacks.RemoveAt(i);
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Audio Cue와 호출별 Volume Scale을 검증한다.
        /// </summary>
        // ------------------------------------------------------------
        private void ValidatePlay(AudioCue cue, float volumeScale)
        {
            if (cue == null)
            {
                throw new ArgumentNullException(nameof(cue));
            }

            if
            (
                float.IsNaN(volumeScale) ||
                float.IsInfinity(volumeScale) ||
                volumeScale < 0.0f ||
                volumeScale > 1.0f
            )
            {
                throw new ArgumentOutOfRangeException
                (
                    nameof(volumeScale),
                    "Volume Scale은 0 이상 1 이하의 유한한 값이어야 합니다."
                );
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 지정 Bus의 직렬화 설정을 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        private AudioBusSettings GetBusSettings(AudioBus bus)
        {
            if (!busSettings.TryGetValue(bus, out var settings) || settings == null)
            {
                throw new ArgumentOutOfRangeException
                (
                    nameof(bus), bus,
                    "등록된 Audio Bus 설정이 아닙니다."
                );
            }

            return settings;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Master와 지정 Bus의 Volume·Mute를 하나의 출력 배율로 합성한다.
        /// </summary>
        // ------------------------------------------------------------
        private float CalculateOutputVolume(AudioBus bus)
        {
            var settings = GetBusSettings(bus);

            if (isMuted || settings.IsMuted) return 0.0f;

            return masterVolume * settings.Volume;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 Master·Bus 상태를 모든 활성 Playback에 다시 적용한다.
        /// </summary>
        // ------------------------------------------------------------
        private void RefreshOutputs()
        {
            for (var i = 0; i < playbacks.Count; i++)
            {
                var playback = playbacks[i];
                playback.Playback.SetOutputVolume(CalculateOutputVolume(playback.Bus));
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 Master·Bus 상태를 지정 Bus의 활성 Playback에 다시 적용한다.
        /// </summary>
        // ------------------------------------------------------------
        private void RefreshOutputs(AudioBus bus)
        {
            for (var i = 0; i < playbacks.Count; i++)
            {
                if (playbacks[i].Bus != bus) continue;

                playbacks[i].Playback.SetOutputVolume(CalculateOutputVolume(bus));
            }
        }

    #endregion

    #region ICuePlayer 구현

        // ------------------------------------------------------------
        /// <summary>
        /// 일반 Cue 경로의 Audio Cue를 동일한 Manager 정책으로 2D 재생한다.
        /// </summary>
        // ------------------------------------------------------------
        ICuePlayback ICuePlayer.Play(IPlaybackCue cue)
        {
            if (cue == null)
            {
                throw new ArgumentNullException(nameof(cue));
            }

            if (cue is not AudioCue audioCue)
            {
                throw new ArgumentException("AudioManager는 AudioCue만 재생할 수 있습니다.", nameof(cue));
            }

            return Play(audioCue);
        }

    #endregion

    #region Unity 생명주기

        // ------------------------------------------------------------
        /// <summary>
        /// Unity Audio Player를 가져오고 지정 singleton 슬롯에 등록한다.
        /// </summary>
        // ------------------------------------------------------------
        private void Awake()
        {
            player = GetComponent<UnityAudioCuePlayer>();

            if (!TryRegisterOrDestroy(slotKey, this)) return;

            if (Application.isPlaying)
            {
                DontDestroyOnLoad(gameObject);
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Released Playback을 Manager의 활성 추적 목록에서 제거한다.
        /// </summary>
        // ------------------------------------------------------------
        private void Update()
        {
            for (var i = playbacks.Count - 1; i >= 0; i--)
            {
                if (playbacks[i].Playback.State != CuePlaybackState.Released) continue;

                playbacks.RemoveAt(i);
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Manager가 비활성화되면 추적 중인 모든 Audio Playback을 종료한다.
        /// </summary>
        // ------------------------------------------------------------
        private void OnDisable()
        {
            StopAll();
        }

    #endregion

    }
}
