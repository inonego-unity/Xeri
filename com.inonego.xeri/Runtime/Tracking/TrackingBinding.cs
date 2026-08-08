/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TrackingBinding.cs
수정일 : 2026-08-08

# 설명
외부 값을 조회하고 선택적으로 전이한 뒤 실제 대상에 적용하는 범용 Tracking 관계를 정의한다.

# 종료 계약
Binding은 Controller에 한 번만 등록할 수 있으며, 해제된 뒤 다시 등록하거나 갱신하지 않는다.
Clear callback은 적용 상태를 먼저 소비한 뒤 최대 한 번 호출한다.
========================================================================= BLOCK_HEADER_END */

using System;

namespace inonego.Xeri
{
    // ============================================================
    /// <summary>
    /// 서로 다른 값 타입의 Tracking Binding을 같은 소유 목록에서 갱신하기 위한 내부 계약.
    /// </summary>
    // ============================================================
    internal interface ITrackingBinding
    {
        // ------------------------------------------------------------
        /// <summary>
        /// Binding 수명이 종료됐는지 여부.
        /// </summary>
        // ------------------------------------------------------------
        bool IsReleased { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// Binding을 하나의 Controller 소유로 확정한다.
        /// </summary>
        // ------------------------------------------------------------
        void Attach();

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 값을 조회하고 실제 대상에 반영한다.
        /// </summary>
        // ------------------------------------------------------------
        void Tick(float deltaTime);

        // ------------------------------------------------------------
        /// <summary>
        /// Binding과 마지막 적용 상태를 종료한다.
        /// </summary>
        // ------------------------------------------------------------
        void Release();
    }

    // ============================================================
    /// <summary>
    /// <br/> 외부에서 원하는 값을 조회하고 실제 대상에 계속 반영하는 범용 Binding.
    /// <br/> 갱신 순서와 수명은 등록된 <see cref="TrackingController"/>가 소유한다.
    /// </summary>
    /// <typeparam name="T">추적하고 적용할 값 타입.</typeparam>
    // ============================================================
    public sealed class TrackingBinding<T> : ITrackingBinding
    {
    #region 필드

        // ------------------------------------------------------------
        /// <summary>
        /// Binding 수명이 종료됐는지 여부.
        /// </summary>
        // ------------------------------------------------------------
        public bool IsReleased => isReleased;

        private readonly Func<(bool Available, T Value)> resolve = null;
        private readonly Func<T, T> commit = null;
        private readonly Func<T, T, float, T> transition = null;
        private readonly Action clear = null;

        private T current = default;
        private bool hasCurrent = false;
        private bool isAttached = false;
        private bool isReleased = false;

    #endregion

    #region 생성자

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> 값 조회, 실제 적용, 선택적 전이와 적용 상태 정리로 Tracking Binding을 생성한다.
        /// <br/> Commit 반환값은 다음 갱신의 현재값으로 사용된다.
        /// </summary>
        // ----------------------------------------------------------------------
        public TrackingBinding
        (
            Func<(bool Available, T Value)> resolve,
            Func<T, T> commit,
            Func<T, T, float, T> transition = null,
            Action clear = null
        ) : base()
        {
            this.resolve = resolve ?? throw new ArgumentNullException(nameof(resolve));
            this.commit = commit ?? throw new ArgumentNullException(nameof(commit));
            this.transition = transition;
            this.clear = clear;
        }

    #endregion

    #region ITrackingBinding

        bool ITrackingBinding.IsReleased => isReleased;

        // ------------------------------------------------------------
        /// <summary>
        /// Binding을 하나의 Controller 소유로 확정한다.
        /// </summary>
        // ------------------------------------------------------------
        void ITrackingBinding.Attach()
        {
            if (isReleased)
            {
                throw new ObjectDisposedException(nameof(TrackingBinding<T>));
            }

            if (isAttached)
            {
                throw new InvalidOperationException
                (
                    "Tracking Binding은 하나의 Controller에 한 번만 등록할 수 있습니다."
                );
            }

            isAttached = true;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 값을 조회하고 실제 대상에 반영한다.
        /// </summary>
        // ------------------------------------------------------------
        void ITrackingBinding.Tick(float deltaTime)
        {
            Tick(deltaTime);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Binding과 마지막 적용 상태를 종료한다.
        /// </summary>
        // ------------------------------------------------------------
        void ITrackingBinding.Release()
        {
            Release();
        }

    #endregion

    #region 메서드

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> 현재 원하는 값을 조회하고 선택적 전이를 거쳐 실제 대상에 반영한다.
        /// <br/> 외부 callback에서 Binding이 해제되면 남은 단계를 실행하지 않는다.
        /// </summary>
        // ----------------------------------------------------------------------
        private void Tick(float deltaTime)
        {
            if (isReleased) return;

            var resolved = resolve();
            if (isReleased) return;

            if (!resolved.Available)
            {
                ClearCurrent();
                return;
            }

            var candidate = resolved.Value;

            if (hasCurrent && transition != null)
            {
                candidate = transition(current, resolved.Value, deltaTime);
                if (isReleased) return;
            }

            // Commit이 Unity callback을 발생시켜 Lease를 해제해도 Clear가 적용 상태를 찾을 수 있게 한다.
            current = candidate;
            hasCurrent = true;

            var committed = commit(candidate);
            if (isReleased) return;

            // Clamp처럼 실제 적용값이 후보와 다른 경우 다음 Frame은 화면에 적용된 값에서 시작한다.
            current = committed;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Binding과 마지막 적용 상태를 종료한다.
        /// </summary>
        // ------------------------------------------------------------
        private void Release()
        {
            if (isReleased) return;

            // 종료 상태를 먼저 확정해 Clear callback의 재진입이 같은 Binding을 다시 종료하지 않게 한다.
            isReleased = true;
            isAttached = false;
            ClearCurrent();
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> 마지막 적용 상태를 먼저 소비하고 선택적 Clear callback을 한 번 호출한다.
        /// <br/> Callback 실패는 다음 갱신이나 해제에서 같은 Clear를 재시도할 근거가 되지 않는다.
        /// </summary>
        // ----------------------------------------------------------------------
        private void ClearCurrent()
        {
            if (!hasCurrent) return;

            hasCurrent = false;
            current = default;
            clear?.Invoke();
        }

    #endregion

    }
}
