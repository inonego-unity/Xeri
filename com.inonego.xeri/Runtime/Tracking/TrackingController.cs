/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TrackingController.cs
수정일 : 2026-08-08

# 설명
서로 다른 값 타입의 Tracking Binding을 등록 순서로 갱신하고 Lease 수명으로 소유한다.

# 종료 계약
개별 Lease 해제와 전체 종료는 Binding을 먼저 종료 상태로 확정한다.
전체 종료는 모든 Binding 정리를 시도한 뒤 수집한 오류를 전달한다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections.Generic;

namespace inonego.Xeri
{
    // ============================================================
    /// <summary>
    /// Tracking Binding의 갱신 순서와 Lease 수명을 소유하는 Controller.
    /// </summary>
    // ============================================================
    public sealed class TrackingController : IDisposable
    {
    #region 필드

        private readonly List<ITrackingBinding> bindings = new List<ITrackingBinding>();
        private bool isTicking = false;
        private bool isDisposed = false;

    #endregion

    #region 메서드

        // ------------------------------------------------------------
        /// <summary>
        /// Binding의 갱신과 종료를 소유하고 해제 Lease를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public Lease Track<T>(TrackingBinding<T> binding)
        {
            if (isDisposed)
            {
                throw new ObjectDisposedException(nameof(TrackingController));
            }

            if (binding == null)
            {
                throw new ArgumentNullException(nameof(binding));
            }

            var tracked = (ITrackingBinding)binding;
            tracked.Attach();
            bindings.Add(tracked);

            return new Lease(() => Release(tracked));
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> Tick 시작 시 소유한 Binding만 등록 순서로 한 번씩 갱신한다.
        /// <br/> Tick 중 등록된 Binding은 다음 Tick부터 갱신한다.
        /// </summary>
        // ----------------------------------------------------------------------
        public void Tick(float deltaTime)
        {
            if (isDisposed)
            {
                throw new ObjectDisposedException(nameof(TrackingController));
            }

            if (isTicking)
            {
                throw new InvalidOperationException("Tracking Controller는 중첩 Tick을 허용하지 않습니다.");
            }

            isTicking = true;
            var count = bindings.Count;

            try
            {
                for (var i = 0; i < count; i++)
                {
                    var binding = bindings[i];
                    if (binding.IsReleased) continue;

                    binding.Tick(deltaTime);

                    // Callback이 Controller 전체를 종료했으면 고정한 다음 index에 접근하지 않는다.
                    if (isDisposed) break;
                }
            }
            finally
            {
                isTicking = false;

                if (isDisposed)
                {
                    bindings.Clear();
                }
                else
                {
                    RemoveReleasedBindings();
                }
            }
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> 지정 Binding을 종료하고 소유 목록에서 제거한다.
        /// <br/> Tick과 전체 종료 중에는 원본 목록을 안정적으로 순회하도록 물리 제거를 미룬다.
        /// </summary>
        // ----------------------------------------------------------------------
        private void Release(ITrackingBinding binding)
        {
            try
            {
                binding.Release();
            }
            finally
            {
                if (!isTicking && !isDisposed)
                {
                    bindings.Remove(binding);
                }
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 종료된 Binding을 현재 소유 목록에서 제거한다.
        /// </summary>
        // ------------------------------------------------------------
        private void RemoveReleasedBindings()
        {
            for (var i = bindings.Count - 1; i >= 0; i--)
            {
                if (bindings[i].IsReleased)
                {
                    bindings.RemoveAt(i);
                }
            }
        }

    #endregion

    #region IDisposable

        // ------------------------------------------------------------
        /// <summary>
        /// 모든 Binding과 마지막 적용 상태를 종료한다.
        /// </summary>
        // ------------------------------------------------------------
        public void Dispose()
        {
            if (isDisposed) return;

            // 종료 상태에서는 Lease callback이 목록을 변경하지 않으므로 원본 소유 목록을 직접 순회한다.
            isDisposed = true;
            List<Exception> errors = null;

            try
            {
                for (var i = 0; i < bindings.Count; i++)
                {
                    try
                    {
                        bindings[i].Release();
                    }
                    catch (Exception exception)
                    {
                        errors ??= new List<Exception>();
                        errors.Add(exception);
                    }
                }
            }
            finally
            {
                // Tick callback 안에서 종료됐다면 바깥 Tick의 finally가 현재 목록을 비운다.
                if (!isTicking)
                {
                    bindings.Clear();
                }
            }

            if (errors != null)
            {
                throw new AggregateException("Tracking Controller 종료가 실패했습니다.", errors);
            }
        }

    #endregion

    }
}
