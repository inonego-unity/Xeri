/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : Timer.cs
수정일 : 2026-11-02

# 설명
float 기반 타이머. 외부 Tick(deltaTime)으로 시간을 전진하거나 되돌린다.
지속 시간이 끝나면 Ready 상태로 전환한 뒤 완료 이벤트를 통지한다.
========================================================================= BLOCK_HEADER_END */

using System;
using TValue = System.Single;

using UnityEngine;

namespace inonego.Xeri.Utility
{
    // ============================================================
    /// <summary>
    /// 타이머 상태.
    /// </summary>
    // ============================================================
    public enum TimerState { Ready, Run, Pause }

    // ============================================================
    /// <summary>
    /// 타이머 종료 이벤트 인수.
    /// </summary>
    // ============================================================
    [Serializable]
    public struct TimerEndEventArgs
    {
        // NONE
    }

    // ============================================================
    /// <summary>
    /// float 기반 타이머.
    /// </summary>
    // ============================================================
    [Serializable]
    public partial class Timer : ITimer, IReadOnlyTimer
    {

    #region 필드

        // ------------------------------------------------------------
        /// <summary>
        /// 타이머 지속 시간.
        /// </summary>
        // ------------------------------------------------------------
        public TValue Duration
        {
            get => duration;
            set
            {
                var next = value;

                if (!IsRunning && !IsPaused)
                {
                    throw new InvalidOperationException("Duration은 Run 또는 Pause 상태일 때만 변경할 수 있습니다.");
                }

                ValidateNonNegativeTime(next);

                duration = next;

                elapsedTime = Mathf.Clamp(elapsedTime, 0.0f, duration);
            }
        }
        [SerializeField]
        private TValue duration = default;

        // ------------------------------------------------------------
        /// <summary>
        /// Start 시 사용할 duration 값. Stop/Reset 후에도 유지된다.
        /// </summary>
        // ------------------------------------------------------------
        [SerializeField]
        internal TValue cached = default;
        TValue ITimer.Cached
        {
            get => cached; 
            set => cached = value;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> 타이머가 경과한 시간.
        /// <br/> 중지 후에도 Reset 또는 Start 전까지 값이 유지된다.
        /// </summary>
        // ------------------------------------------------------------
        public TValue ElapsedTime
        {
            get => elapsedTime;
            set
            {
                var next = value;

                if (!IsRunning && !IsPaused)
                {
                    throw new InvalidOperationException("ElapsedTime은 Run 또는 Pause 상태일 때만 변경할 수 있습니다.");
                }

                ValidateNonNegativeTime(next);

                elapsedTime = Mathf.Clamp(next, 0.0f, duration);
            }
        }
        [SerializeField]
        private TValue elapsedTime = default;

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> 타이머의 남은 시간.
        /// <br/> 중지 후에도 Reset 또는 Start 전까지 값이 유지된다.
        /// </summary>
        // ------------------------------------------------------------
        public TValue RemainingTime
        {
            get => duration - elapsedTime;
            set
            {
                var next = value;

                if (!IsRunning && !IsPaused)
                {
                    throw new InvalidOperationException("RemainingTime은 Run 또는 Pause 상태일 때만 변경할 수 있습니다.");
                }

                ValidateNonNegativeTime(next);

                elapsedTime = Mathf.Clamp(duration - next, 0.0f, duration);
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 경과 시간 비율 (0.0 ~ 1.0). 지속 시간이 0이면 0을 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public TValue ElapsedTime01 => duration > 0.0f ? elapsedTime / duration : 0.0f;

        // ------------------------------------------------------------
        /// <summary>
        /// 잔여 시간 비율 (0.0 ~ 1.0). 지속 시간이 0이면 0을 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public TValue RemainingTime01 => duration > 0.0f ? RemainingTime / duration : 0.0f;

    #endregion

    #region 상태

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 타이머 상태.
        /// </summary>
        // ------------------------------------------------------------
        public TimerState Current
        {
            get => current;
            protected set
            {
                var (prev, next) = (current, value);

                if (prev == next) return;

                current = next;

                OnStateChange?.Invoke(this, new(prev, next));
            }
        }
        [SerializeField]
        private TimerState current = TimerState.Ready;

        // ------------------------------------------------------------
        /// <summary>
        /// 타이머가 실행 중인지 여부.
        /// </summary>
        // ------------------------------------------------------------
        public bool IsRunning => current == TimerState.Run;

        // ------------------------------------------------------------
        /// <summary>
        /// 타이머가 일시정지 중인지 여부.
        /// </summary>
        // ------------------------------------------------------------
        public bool IsPaused  => current == TimerState.Pause;

    #endregion

    #region 이벤트

        // ------------------------------------------------------------
        /// <summary>
        /// 타이머가 종료될 때 발생한다.
        /// </summary>
        // ------------------------------------------------------------
        public event EventHandler<TimerEndEventArgs> OnEnd = null;

        // ------------------------------------------------------------
        /// <summary>
        /// 타이머 상태가 변경될 때 발생한다.
        /// </summary>
        // ------------------------------------------------------------
        public event ValueChangeEventHandler<TimerState> OnStateChange = null;

        // ------------------------------------------------------------
        /// <summary>
        /// 종료 및 상태 변경의 구독 여부를 확인한다.
        /// </summary>
        // ------------------------------------------------------------
        internal bool HasObservers => OnEnd != null || OnStateChange != null;

    #endregion

    #region 메서드

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> 외부에서 전달된 시간만큼 진행하거나 되돌린다.
        /// <br/> 지속 시간에 도달하면 먼저 정지한 뒤 완료 이벤트를 통지한다.
        /// </summary>
        // ------------------------------------------------------------
        public void Tick(TValue deltaTime)
        {
            if (!IsRunning) return;

            ValidateFiniteTime(deltaTime);
            elapsedTime = Mathf.Clamp(elapsedTime + deltaTime, 0.0f, duration);

            if (elapsedTime >= duration)
            {
                Stop();
                OnEnd?.Invoke(this, new());
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> 타이머를 새 지속시간으로 초기화하고 실행을 시작한다.
        /// <br/> 상태 변경 이벤트는 새 시간이 설정된 뒤 발생한다.
        /// </summary>
        // ------------------------------------------------------------
        public void Start(TValue duration)
        {
            if (IsRunning || IsPaused)
            {
                throw new AlreadyRunningException();
            }

            ValidateNonNegativeTime(duration);

            // 상태 변경 통지 시 새 주기의 시간이 완성되어 있어야 한다.
            this.duration = duration;
            elapsedTime = default;
            Current = TimerState.Run;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 타이머 작동을 중지한다.
        /// </summary>
        // ------------------------------------------------------------
        public void Stop()
        {
            if (IsRunning || IsPaused)
            {
                Current = TimerState.Ready;
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 타이머 작동을 일시정지한다.
        /// </summary>
        // ------------------------------------------------------------
        public void Pause()
        {
            if (IsRunning)
            {
                Current = TimerState.Pause;
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 타이머 작동을 재개한다.
        /// </summary>
        // ------------------------------------------------------------
        public void Resume()
        {
            if (IsPaused)
            {
                Current = TimerState.Run;
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 타이머를 리셋한다.
        /// </summary>
        // ------------------------------------------------------------
        public void Reset()
        {
            if (IsRunning || IsPaused)
            {
                throw new FailedToResetException();
            }

            (duration, elapsedTime) = (default, default);
        }

    #endregion

    #region 시간 검증

        // ------------------------------------------------------------
        /// <summary>
        /// 시간 입력이 유한한 숫자인지 확인한다.
        /// </summary>
        // ------------------------------------------------------------
        private static void ValidateFiniteTime(TValue value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                throw new InvalidTimeException("시간 값은 유한해야 합니다.");
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 지속시간과 시간 설정값이 0 이상의 유한한 숫자인지 확인한다.
        /// </summary>
        // ------------------------------------------------------------
        private static void ValidateNonNegativeTime(TValue value)
        {
            ValidateFiniteTime(value);

            if (value < 0.0f)
            {
                throw new InvalidTimeException();
            }
        }

    #endregion

    }
}
