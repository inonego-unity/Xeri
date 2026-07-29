/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : DOTweenPresentationTransitioner.cs
수정일 : 2026-07-29

# 설명
Core Presentation Transition 계약을 DOTween float tween과 취소 Handle로 구현한다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections.Generic;

using DG.Tweening;

using UnityEngine;

namespace inonego.Xeri.UI.Game
{
    // ============================================================
    /// <summary>
    /// DOTween 기반 Presentation Transition backend.
    /// </summary>
    // ============================================================
    public sealed class DOTweenPresentationTransitioner : IPresentationTransitioner
    {
    #region 필드

        private readonly Dictionary<PresentationTransitionHandle, Tween> active =
            new Dictionary<PresentationTransitionHandle, Tween>();
        private bool isDisposed = false;

    #endregion

    #region IPresentationTransitioner

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> float 진행 값을 DOTween으로 Target에 적용하고,
        /// <br/> 정상 완료·오류·취소를 한 번만 종결하는 Handle을 반환한다.
        /// </summary>
        // ----------------------------------------------------------------------
        public PresentationTransitionHandle Play
        (
            PresentationTransitionParams parameters,
            Action onCompleted,
            Action<Exception> onFailed
        )
        {
            if (isDisposed)
            {
                throw new ObjectDisposedException(nameof(DOTweenPresentationTransitioner));
            }

            if (!parameters.Target.IsValid)
            {
                throw new InvalidOperationException("Presentation Transition Target이 유효하지 않습니다.");
            }

            parameters.Target.Apply(parameters.StartValue);

            if (parameters.Duration <= 0.0f)
            {
                var immediate = new PresentationTransitionHandle(null);
                parameters.Target.Apply(parameters.EndValue);
                immediate.Complete();
                InvokeCompleted(onCompleted);
                return immediate;
            }

            var value = parameters.StartValue;
            Tween tween = null;
            PresentationTransitionHandle handle = null;
            var failed = false;

            void CancelTween()
            {
                if (tween != null)
                {
                    tween.Kill(false);
                }

                if (handle != null)
                {
                    active.Remove(handle);
                }
            }

            void ApplyValue(float next)
            {
                if (failed) return;

                value = next;

                try
                {
                    parameters.Target.Apply(next);
                }
                catch (Exception exception)
                {
                    failed = true;
                    tween?.Kill(false);

                    if (handle != null && handle.Complete())
                    {
                        active.Remove(handle);
                        InvokeFailed(onFailed, exception);
                    }
                }
            }

            handle = new PresentationTransitionHandle(CancelTween);
            tween = DOTween.To
            (
                () => value,
                ApplyValue,
                parameters.EndValue,
                parameters.Duration
            )
            .SetEase(DG.Tweening.Ease.OutCubic)
            .SetUpdate(parameters.TimeSource.UseUnscaledTime)
            .OnComplete
            (
                () =>
                {
                    if (!handle.Complete()) return;

                    active.Remove(handle);

                    try
                    {
                        parameters.Target.Apply(parameters.EndValue);
                    }
                    catch (Exception exception)
                    {
                        InvokeFailed(onFailed, exception);
                        return;
                    }

                    InvokeCompleted(onCompleted);
                }
            );

            active.Add(handle, tween);
            return handle;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 완료 callback 예외를 Transition 실패로 재해석하지 않고 기록한다.
        /// </summary>
        // ------------------------------------------------------------
        private static void InvokeCompleted(Action onCompleted)
        {
            try
            {
                onCompleted?.Invoke();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 실패 callback 예외가 DOTween 갱신을 중단하지 않게 기록한다.
        /// </summary>
        // ------------------------------------------------------------
        private static void InvokeFailed
        (
            Action<Exception> onFailed,
            Exception failure
        )
        {
            try
            {
                onFailed?.Invoke(failure);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

    #endregion

    #region IDisposable

        // ------------------------------------------------------------
        /// <summary>
        /// 진행 중인 모든 DOTween Transition을 취소하고 backend를 해제한다.
        /// </summary>
        // ------------------------------------------------------------
        public void Dispose()
        {
            if (isDisposed) return;

            var handles = new List<PresentationTransitionHandle>(active.Keys);

            for (var i = handles.Count - 1; i >= 0; i--)
            {
                handles[i].Cancel();
            }

            active.Clear();
            isDisposed = true;
        }

    #endregion

    }
}
