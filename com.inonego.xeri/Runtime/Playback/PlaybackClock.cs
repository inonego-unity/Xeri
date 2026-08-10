/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : PlaybackClock.cs
수정일 : 2026-07-31

# 설명
외부 delta로 진행하는 재생 상태와 위치, Seek, Speed와 Loop를 관리한다.

# 적용 범위
Clock은 Unity 시간과 실제 재생 대상이나 리소스 수명을 소유하지 않는다.
Animation, Cue, Audio와 VFX의 Sampling 및 정리는 Clock을 소유한 Controller나 Session이 담당한다.
========================================================================= BLOCK_HEADER_END */

using System;

using UnityEngine;

namespace inonego.Xeri.Playback
{
    // ============================================================
    /// <summary>
    /// Playback Clock의 재생 상태.
    /// </summary>
    // ============================================================
    public enum PlaybackState
    {
        Stopped,
        Playing,
        Paused,
    }

    // ============================================================
    /// <summary>
    /// 외부 delta로 재생 상태와 위치를 진행하는 Playback Clock.
    /// </summary>
    // ============================================================
    public sealed class PlaybackClock : IPlaybackClock
    {

    #region 필드

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 재생 상태.
        /// </summary>
        // ------------------------------------------------------------
        public PlaybackState State => state;

        private PlaybackState state = PlaybackState.Stopped;

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 재생 중인지 여부.
        /// </summary>
        // ------------------------------------------------------------
        public bool IsPlaying => state == PlaybackState.Playing;

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 일시정지 중인지 여부.
        /// </summary>
        // ------------------------------------------------------------
        public bool IsPaused => state == PlaybackState.Paused;

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 재생 위치.
        /// </summary>
        // ------------------------------------------------------------
        public float Time => time;

        private float time = 0.0f;

        // ------------------------------------------------------------
        /// <summary>
        /// 전체 재생 길이.
        /// </summary>
        // ------------------------------------------------------------
        public float Duration => duration;

        private float duration = 0.0f;

        // ------------------------------------------------------------
        /// <summary>
        /// 정방향 재생 속도.
        /// </summary>
        // ------------------------------------------------------------
        public float Speed
        {
            get => speed;
            set
            {
                if (float.IsNaN(value) || float.IsInfinity(value) || value <= 0.0f)
                {
                    throw new ArgumentOutOfRangeException
                    (
                        nameof(value),
                        "Speed는 유한한 0보다 큰 값이어야 합니다."
                    );
                }

                speed = value;
            }
        }

        private float speed = 1.0f;

        // ------------------------------------------------------------
        /// <summary>
        /// Duration 경계에서 재생을 반복할지 여부.
        /// </summary>
        // ------------------------------------------------------------
        public bool IsLooping { get; set; } = false;

    #endregion

    #region 이벤트

        // ------------------------------------------------------------
        /// <summary>
        /// 재생 상태가 실제로 변경될 때 발생한다.
        /// </summary>
        // ------------------------------------------------------------
        public event ValueChangeEventHandler<PlaybackState> OnStateChange = null;

        // ------------------------------------------------------------
        /// <summary>
        /// 재생 위치가 변경되거나 Loop 경계 뒤 재평가가 필요할 때 발생한다.
        /// </summary>
        // ------------------------------------------------------------
        public event ValueChangeEventHandler<float> OnTimeChange = null;

        // ------------------------------------------------------------
        /// <summary>
        /// 비 Loop 재생이 자연 완료될 때 발생한다.
        /// </summary>
        // ------------------------------------------------------------
        public event Action OnCompleted = null;

        // ------------------------------------------------------------
        /// <summary>
        /// Loop 재생이 하나 이상의 Duration 경계를 통과한 Tick에 한 번 발생한다.
        /// </summary>
        // ------------------------------------------------------------
        public event Action OnLooped = null;

    #endregion

    #region 메서드

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> 전체 재생 길이를 변경하고 현재 위치를 새 범위에 맞춘다.
        /// <br/> Loop가 아닌 재생 중 현재 위치 이하로 줄어들면 최종 위치를 확정하고 자연 완료한다.
        /// <br/> Loop 재생은 새 Duration 끝으로 위치를 맞추고 재생을 유지한다.
        /// </summary>
        // ----------------------------------------------------------------------
        public void SetDuration(float duration)
        {
            if (float.IsNaN(duration) || float.IsInfinity(duration) || duration < 0.0f)
            {
                throw new ArgumentOutOfRangeException
                (
                    nameof(duration),
                    "Duration은 유한한 0 이상의 값이어야 합니다."
                );
            }

            var previousDuration = this.duration;
            if (previousDuration == duration) return;

            var previousState = state;
            var previousTime = time;

            // Duration 0은 재생 구간이 제거된 상태이므로 완료가 아닌 명시적 정지로 수렴한다.
            if (duration == 0.0f)
            {
                this.duration = 0.0f;
                state = PlaybackState.Stopped;
                time = 0.0f;

                if (previousState != state)
                {
                    OnStateChange?.Invoke(this, new(previousState, state));
                }

                if (previousTime != time)
                {
                    OnTimeChange?.Invoke(this, new(previousTime, time));
                }

                return;
            }

            this.duration = duration;
            time = Mathf.Clamp(time, 0.0f, duration);

            // Loop가 아닌 재생에서 Duration 축소가 playhead를 따라잡으면 마지막 위치를 알린 뒤 완료한다.
            var hasCompleted =
                previousState == PlaybackState.Playing &&
                !IsLooping &&
                duration < previousDuration &&
                duration <= previousTime;

            if (hasCompleted)
            {
                state = PlaybackState.Stopped;
            }

            if (previousState != state)
            {
                OnStateChange?.Invoke(this, new(previousState, state));
            }

            if (previousTime != time || hasCompleted)
            {
                OnTimeChange?.Invoke(this, new(previousTime, time));
            }

            if (hasCompleted)
            {
                OnCompleted?.Invoke();
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 유한한 재생 위치를 Duration 범위 안으로 이동한다.
        /// </summary>
        // ------------------------------------------------------------
        public void SetTime(float time)
        {
            if (float.IsNaN(time) || float.IsInfinity(time))
            {
                throw new ArgumentOutOfRangeException(nameof(time), "Time은 유한한 값이어야 합니다.");
            }

            var previousTime = this.time;
            var nextTime = Mathf.Clamp(time, 0.0f, duration);
            if (previousTime == nextTime) return;

            this.time = nextTime;
            OnTimeChange?.Invoke(this, new(previousTime, nextTime));
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> 현재 위치에서 재생을 시작하거나 재개한다.
        /// <br/> 자연 완료 뒤에는 시작 위치로 이동한 후 재생한다.
        /// </summary>
        // ----------------------------------------------------------------------
        public void Play()
        {
            if (duration == 0.0f || state == PlaybackState.Playing) return;

            var previousState = state;
            var previousTime = time;

            // 완료 위치에서 새 재생을 시작할 때만 playhead를 처음으로 되돌린다.
            if (state == PlaybackState.Stopped && time == duration)
            {
                time = 0.0f;
            }

            state = PlaybackState.Playing;

            OnStateChange?.Invoke(this, new(previousState, state));

            if (previousTime != time)
            {
                OnTimeChange?.Invoke(this, new(previousTime, time));
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 재생 중인 Clock을 현재 위치에서 일시정지한다.
        /// </summary>
        // ------------------------------------------------------------
        public void Pause()
        {
            if (state != PlaybackState.Playing) return;

            var previousState = state;
            state = PlaybackState.Paused;
            OnStateChange?.Invoke(this, new(previousState, state));
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Clock을 정지하고 재생 위치를 처음으로 되돌린다.
        /// </summary>
        // ------------------------------------------------------------
        public void Stop()
        {
            var previousState = state;
            var previousTime = time;
            if (previousState == PlaybackState.Stopped && previousTime == 0.0f) return;

            state = PlaybackState.Stopped;
            time = 0.0f;

            if (previousState != state)
            {
                OnStateChange?.Invoke(this, new(previousState, state));
            }

            if (previousTime != time)
            {
                OnTimeChange?.Invoke(this, new(previousTime, time));
            }
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> 유한한 외부 delta만큼 재생 위치를 진행한다.
        /// <br/> delta의 Time Domain 선택은 호출자가 담당한다.
        /// </summary>
        // ----------------------------------------------------------------------
        public void Tick(float deltaTime)
        {
            if (state != PlaybackState.Playing) return;
            if (float.IsNaN(deltaTime) || float.IsInfinity(deltaTime) || deltaTime <= 0.0f) return;

            // 유한한 float 입력끼리의 곱셈이 overflow하지 않도록 중간 위치를 double로 계산한다.
            var previousState = state;
            var previousTime = time;
            var advancedTime = time + (double)deltaTime * speed;

            if (advancedTime < duration)
            {
                time = (float)advancedTime;
                OnTimeChange?.Invoke(this, new(previousTime, time));
                return;
            }

            // Loop 경계를 먼저 알린 뒤 새 주기 위치를 Sample할 수 있도록 시간 변경을 알린다.
            if (IsLooping)
            {
                time = (float)(advancedTime % duration);
                if (time >= duration)
                {
                    // double 나머지의 float 반올림이 끝점을 만들면 가장 가까운 Loop 내부 위치를 유지한다.
                    time = BitConverter.Int32BitsToSingle(BitConverter.SingleToInt32Bits(duration) - 1);
                }

                OnLooped?.Invoke();
                OnTimeChange?.Invoke(this, new(previousTime, time));
                return;
            }

            // 자연 완료 상태를 먼저 확정해 모든 callback이 최종 상태를 조회하도록 한다.
            state = PlaybackState.Stopped;
            time = duration;

            OnStateChange?.Invoke(this, new(previousState, state));
            OnTimeChange?.Invoke(this, new(previousTime, time));
            OnCompleted?.Invoke();
        }

    #endregion

    }
}
