/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : UnityAudioPlayback.cs
수정일 : 2026-08-01

# 설명
Pool에서 획득한 Unity AudioSource voice로 실행한 단일 Audio Cue의 제어와 수명을 소유한다.

# 종료 계약
Released와 Lease 참조 해제를 외부 Unity Object 정리 전에 확정한다.
반환된 voice는 다른 Playback에 재사용될 수 있으므로 오래된 Playback에서 다시 접근하지 않는다.
========================================================================= BLOCK_HEADER_END */

using System;

using UnityEngine;

namespace inonego.Xeri.Playback
{
    // ============================================================
    /// <summary>
    /// Unity AudioSource voice 기반 Audio Playback.
    /// </summary>
    // ============================================================
    internal sealed class UnityAudioPlayback : IAudioPlayback, IPlaybackClock
    {

    #region 필드

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 Cue Playback 수명 상태.
        /// </summary>
        // ------------------------------------------------------------
        public CuePlaybackState State { get; private set; } = CuePlaybackState.Playing;

        // ------------------------------------------------------------
        /// <summary>
        /// 실제 Audio 재생 위치와 상태를 조회하는 Clock.
        /// </summary>
        // ------------------------------------------------------------
        public IPlaybackClock Clock => this;

        // ------------------------------------------------------------
        /// <summary>
        /// Playback 하나에 적용되는 개별 볼륨.
        /// </summary>
        // ------------------------------------------------------------
        public float Volume
        {
            get => volume;
            set
            {
                if (State == CuePlaybackState.Released) return;

                if (float.IsNaN(value) || float.IsInfinity(value) || value < 0.0f || value > 1.0f)
                {
                    throw new ArgumentOutOfRangeException
                    (
                        nameof(value),
                        "Volume은 0 이상 1 이하의 유한한 값이어야 합니다."
                    );
                }

                volume = value;
                ApplyVolume();
            }
        }

        private float volume = 1.0f;

        // ------------------------------------------------------------
        /// <summary>
        /// Playback 하나에 적용되는 재생 Pitch.
        /// </summary>
        // ------------------------------------------------------------
        public float Pitch
        {
            get => pitch;
            set
            {
                if (State == CuePlaybackState.Released) return;

                if
                (
                    float.IsNaN(value) ||
                    float.IsInfinity(value) ||
                    value < -3.0f ||
                    value > 3.0f
                )
                {
                    throw new ArgumentOutOfRangeException
                    (
                        nameof(value),
                        "Pitch는 -3 이상 3 이하인 유한한 값이어야 합니다."
                    );
                }

                pitch = value;
                sourceLease.Value.pitch = value;
            }
        }

        private float pitch = 1.0f;

        private float outputVolume = 1.0f;

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 Audio 재생 상태.
        /// </summary>
        // ------------------------------------------------------------
        PlaybackState IPlaybackClock.State
        {
            get
            {
                if (State == CuePlaybackState.Released)
                {
                    return PlaybackState.Stopped;
                }

                return isPaused
                    ? PlaybackState.Paused
                    : PlaybackState.Playing;
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 Audio가 재생 중인지 여부.
        /// </summary>
        // ------------------------------------------------------------
        public bool IsPlaying => ((IPlaybackClock)this).State == PlaybackState.Playing;

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 Audio가 일시정지 중인지 여부.
        /// </summary>
        // ------------------------------------------------------------
        public bool IsPaused => ((IPlaybackClock)this).State == PlaybackState.Paused;

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 Clip 재생 위치.
        /// </summary>
        // ------------------------------------------------------------
        public float Time
        {
            get
            {
                var source = sourceLease?.Value;
                return source != null ? source.time : lastTime;
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// AudioClip 전체 길이.
        /// </summary>
        // ------------------------------------------------------------
        public float Duration { get; }

        private Lease<AudioSource> sourceLease = null;
        private Transform emitter = null;
        private float lastTime = 0.0f;
        private bool isPaused = false;

    #endregion

    #region 생성자

        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// <br/> Pool에서 획득한 AudioSource Lease와 초기 제어값으로 Playback을 생성한다.
        /// <br/> AudioSource.Play 호출 전에 Volume과 Pitch가 실제 voice에 적용된다.
        /// </summary>
        // ----------------------------------------------------------------------------------------------------
        internal UnityAudioPlayback
        (
            Lease<AudioSource> sourceLease,
            float volume,
            float pitch,
            float outputVolume,
            Transform emitter = null
        ) : base()
        {
            var source = sourceLease.Value;
            this.sourceLease = sourceLease;
            this.emitter = emitter;
            Duration = source.clip.length;

            Volume = volume;
            Pitch = pitch;
            SetOutputVolume(outputVolume);
        }

    #endregion

    #region 메서드

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 위치를 보존하고 Audio Playback을 일시정지한다.
        /// </summary>
        // ------------------------------------------------------------
        public void Pause()
        {
            if (State == CuePlaybackState.Released || isPaused) return;

            var source = sourceLease.Value;
            lastTime = source.time;
            source.Pause();
            isPaused = true;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 일시정지된 Audio Playback을 같은 위치에서 재개한다.
        /// </summary>
        // ------------------------------------------------------------
        public void Resume()
        {
            if (State == CuePlaybackState.Released || !isPaused) return;

            sourceLease.Value.UnPause();
            isPaused = false;
        }

        // --------------------------------------------------------------------------------
        /// <summary>
        /// <br/> Immediate는 AudioSource를 즉시 종료한다.
        /// <br/> Natural은 Loop를 해제하고 현재 Clip 재생이 끝날 때까지 Draining한다.
        /// </summary>
        // --------------------------------------------------------------------------------
        public void Stop(CueStopMode mode = CueStopMode.Immediate)
        {
            if (State == CuePlaybackState.Released) return;

            if (mode == CueStopMode.Natural)
            {
                if (State == CuePlaybackState.Draining) return;

                State = CuePlaybackState.Draining;
                sourceLease.Value.loop = false;
                return;
            }

            Release(hasCompleted: false);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Playback을 즉시 종료한다.
        /// </summary>
        // ------------------------------------------------------------
        public void Dispose()
        {
            Stop();
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// AudioSource의 실제 완료와 선택적 emitter 위치를 갱신한다.
        /// </summary>
        // ----------------------------------------------------------------------
        internal void Tick()
        {
            if (State == CuePlaybackState.Released || isPaused) return;

            if (!ReferenceEquals(emitter, null))
            {
                if (emitter == null)
                {
                    Release(hasCompleted: false);
                    return;
                }

                sourceLease.Value.transform.position = emitter.position;
            }

            var source = sourceLease.Value;
            if (source.isPlaying)
            {
                lastTime = source.time;
                return;
            }

            Release(hasCompleted: true);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Manager의 Master와 Bus 정책을 합성한 출력 배율을 적용한다.
        /// </summary>
        // ------------------------------------------------------------
        internal void SetOutputVolume(float value)
        {
            if (State == CuePlaybackState.Released) return;

            if (float.IsNaN(value) || float.IsInfinity(value) || value < 0.0f || value > 1.0f)
            {
                throw new ArgumentOutOfRangeException
                (
                    nameof(value),
                    "Output Volume은 0 이상 1 이하의 유한한 값이어야 합니다."
                );
            }

            outputVolume = value;
            ApplyVolume();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 개별 볼륨과 Manager 출력 배율을 실제 AudioSource에 반영한다.
        /// </summary>
        // ------------------------------------------------------------
        private void ApplyVolume()
        {
            sourceLease.Value.volume = volume * outputVolume;
        }

        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// <br/> 종료 상태와 Lease 참조 해제를 먼저 확정한 뒤 voice를 Pool에 반환한다.
        /// <br/> 오래된 Playback은 반환된 AudioSource를 이후 다시 제어하지 않는다.
        /// </summary>
        // ----------------------------------------------------------------------------------------------------
        private void Release(bool hasCompleted)
        {
            if (State == CuePlaybackState.Released) return;

            var sourceLease = this.sourceLease;
            var source = sourceLease.Value;

            lastTime = hasCompleted
                ? Duration
                : source.time;

            // voice 반환 전에 Terminal을 확정하여 같은 Playback의 재진입과 후속 제어를 차단한다.
            State = CuePlaybackState.Released;
            isPaused = false;
            emitter = null;
            this.sourceLease = null;

            // Pool 대기 중 외부 Audio Asset을 붙잡지 않도록 참조를 비운 뒤 Lease를 반환한다.
            source.Stop();
            source.clip = null;
            source.outputAudioMixerGroup = null;
            sourceLease.Dispose();
        }

    #endregion

    }
}
