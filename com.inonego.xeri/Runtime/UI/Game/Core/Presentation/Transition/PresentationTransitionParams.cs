/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : PresentationTransitionParams.cs
수정일 : 2026-07-29

# 설명
Presentation Transition Target과 시작·종료 값, 시간 정책을 불변 호출 인자로 묶는다.
========================================================================= BLOCK_HEADER_END */

using System;

namespace inonego.Xeri.UI.Game
{
    // ============================================================
    /// <summary>
    /// Presentation Transition 실행 인자.
    /// </summary>
    // ============================================================
    public readonly struct PresentationTransitionParams
    {
    #region 필드

        // ------------------------------------------------------------
        /// <summary>
        /// 진행 값을 적용할 Target.
        /// </summary>
        // ------------------------------------------------------------
        public IPresentationTransitionTarget Target { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// Transition 시작 값.
        /// </summary>
        // ------------------------------------------------------------
        public float StartValue { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// Transition 종료 값.
        /// </summary>
        // ------------------------------------------------------------
        public float EndValue { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// Transition 재생 시간.
        /// </summary>
        // ------------------------------------------------------------
        public float Duration { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 시간 공급 정책.
        /// </summary>
        // ------------------------------------------------------------
        public IPresentationTimeSource TimeSource { get; }

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// Transition 실행 인자를 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        public PresentationTransitionParams
        (
            IPresentationTransitionTarget target,
            float startValue,
            float endValue,
            float duration,
            IPresentationTimeSource timeSource
        ) : this()
        {
            if (duration < 0.0f)
            {
                throw new ArgumentOutOfRangeException(nameof(duration));
            }

            Target = target ?? throw new ArgumentNullException(nameof(target));
            StartValue = startValue;
            EndValue = endValue;
            Duration = duration;
            TimeSource = timeSource ?? throw new ArgumentNullException(nameof(timeSource));
        }

    #endregion

    }
}
