/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : UnityAudioPlayback.cs
수정일 : 2026-07-31

# 설명
Unity AudioSource로 실행한 단일 Audio Cue의 제어와 수명을 소유한다.

# 종료 계약
Released를 외부 Unity Object 정리 전에 확정하고 같은 AudioSource 종료를 다시 실행하지 않는다.
========================================================================= BLOCK_HEADER_END */

using UnityEngine;

namespace inonego.Xeri.Playback
{
    // ============================================================
    /// <summary>
    /// Unity AudioSource 기반 Audio Playback.
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
        public float Time => source != null ? source.time : lastTime;

        // ------------------------------------------------------------
        /// <summary>
        /// AudioClip 전체 길이.
        /// </summary>
        // ------------------------------------------------------------
        public float Duration { get; }

        private GameObject instance = null;
        private AudioSource source = null;
        private float lastTime = 0.0f;
        private bool isPaused = false;

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// 재생을 시작한 AudioSource와 소유 GameObject로 Playback을 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        internal UnityAudioPlayback(GameObject instance, AudioSource source) : base()
        {
            this.instance = instance;
            this.source = source;
            Duration = source.clip.length;
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

            source.UnPause();
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
                source.loop = false;
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
        /// AudioSource의 실제 완료를 관찰하고 생성한 재생 자원을 반환한다.
        /// </summary>
        // ----------------------------------------------------------------------
        internal void Tick()
        {
            if (State == CuePlaybackState.Released || isPaused) return;

            if (source != null && source.isPlaying)
            {
                lastTime = source.time;
                return;
            }

            Release(hasCompleted: true);
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// 종료 상태를 먼저 확정하고 소유한 AudioSource GameObject를 정리한다.
        /// </summary>
        // ----------------------------------------------------------------------
        private void Release(bool hasCompleted)
        {
            if (State == CuePlaybackState.Released) return;

            var instance = this.instance;
            var source = this.source;

            lastTime = hasCompleted || source == null
                ? Duration
                : source.time;

            // Unity Object 정리 실패 여부와 관계없이 Playback 수명은 Terminal로 확정한다.
            State = CuePlaybackState.Released;
            isPaused = false;
            this.instance = null;
            this.source = null;

            if (source != null)
            {
                source.Stop();
            }

            if (instance != null)
            {
                Object.Destroy(instance);
            }
        }

    #endregion

    }
}
