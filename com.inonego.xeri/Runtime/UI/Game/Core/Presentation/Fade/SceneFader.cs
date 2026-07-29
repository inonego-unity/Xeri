/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : SceneFader.cs
수정일 : 2026-07-29

# 설명
App 기본 Layer의 Fade Overlay를 Cover부터 Reveal 또는 종료까지 소유하는 상태 머신이다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections.Generic;

using UnityEngine;

namespace inonego.Xeri.UI.Game
{
    // ============================================================
    /// <summary>
    /// App 수명의 Scene Fade 상태와 Overlay를 소유한다.
    /// </summary>
    // ============================================================
    public sealed class SceneFader : IDisposable
    {
    #region 필드

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 Scene Fade 상태.
        /// </summary>
        // ------------------------------------------------------------
        public SceneFadeState State { get; private set; } = SceneFadeState.Clear;

        // ------------------------------------------------------------
        /// <summary>
        /// 마지막 비동기 Fade 실패와 정리 실패를 함께 보존한 예외.
        /// </summary>
        // ------------------------------------------------------------
        public Exception LastFailure { get; private set; }

        private readonly PresentationLayerRegistry layerRegistry = null;
        private readonly string layerID = "";
        private readonly IOverlaySource<ISceneFadeDriver> source = null;
        private readonly IPresentationTransitioner transitioner = null;
        private readonly IPresentationTimeSource timeSource = null;

        private OverlayHandle<ISceneFadeDriver> overlay = null;
        private bool overlayInitialized = false;
        private PresentationTransitionHandle transition = null;
        private SceneFadeState stableState = SceneFadeState.Clear;
        private int generation = 0;
        private bool isDisposed = false;

    #endregion

    #region 이벤트

        // ------------------------------------------------------------
        /// <summary>
        /// Cover Transition이 완료됐을 때 발생한다.
        /// </summary>
        // ------------------------------------------------------------
        public event Action OnCovered = null;

        // ------------------------------------------------------------
        /// <summary>
        /// Reveal Transition과 Overlay 반환이 완료됐을 때 발생한다.
        /// </summary>
        // ------------------------------------------------------------
        public event Action OnRevealed = null;

        // ------------------------------------------------------------
        /// <summary>
        /// 비동기 Fade 또는 완료 정리가 실패해 마지막 안정 상태로 복원됐을 때 발생한다.
        /// </summary>
        // ------------------------------------------------------------
        public event Action<Exception> OnFailed = null;

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// Scene Fade 의존성을 명시적으로 주입해 상태 머신을 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        public SceneFader
        (
            PresentationLayerRegistry layerRegistry,
            string layerID,
            IOverlaySource<ISceneFadeDriver> source,
            IPresentationTransitioner transitioner,
            IPresentationTimeSource timeSource
        ) : base()
        {
            this.layerRegistry = layerRegistry ?? throw new ArgumentNullException(nameof(layerRegistry));

            if (string.IsNullOrWhiteSpace(layerID))
            {
                throw new ArgumentException("Scene Fade Layer ID가 비어 있습니다.", nameof(layerID));
            }

            this.layerID = layerID;
            this.source = source ?? throw new ArgumentNullException(nameof(source));
            this.transitioner = transitioner ?? throw new ArgumentNullException(nameof(transitioner));
            this.timeSource = timeSource ?? throw new ArgumentNullException(nameof(timeSource));
        }

    #endregion

    #region 메서드

        // ------------------------------------------------------------
        /// <summary>
        /// Fade Overlay를 불투명하게 전환하고 Covered 상태로 유지한다.
        /// </summary>
        // ------------------------------------------------------------
        public void Cover(SceneFadeParams parameters)
        {
            ThrowIfDisposed();
            ISceneFadeDriver driver = null;

            try
            {
                EnsureOverlay();
                driver = overlay.View;

                // 새 요청은 기존 실행을 끝낸 뒤 색상과 상태를 함께 교체한다.
                CancelTransition();
                driver.SetColor(parameters.Color);
                var currentGeneration = ++generation;
                State = SceneFadeState.Covering;
                LastFailure = null;

                Play
                (
                    driver,
                    driver.Alpha,
                    1.0f,
                    parameters.Duration,
                    currentGeneration,
                    CompleteCover
                );
            }
            catch (Exception exception)
            {
                if (ReferenceEquals(exception, LastFailure))
                {
                    throw;
                }

                if (driver == null)
                {
                    LastFailure = exception;
                    throw;
                }

                throw RollbackToStable(driver, exception);
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 보유한 Fade Overlay를 투명하게 전환한 뒤 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public void Reveal(SceneFadeParams parameters)
        {
            ThrowIfDisposed();

            if (overlay == null || !overlayInitialized)
            {
                throw new InvalidOperationException("Reveal할 초기화된 Fade Overlay가 없습니다.");
            }

            var driver = overlay.View;

            try
            {
                // Covering 또는 Covered 상태의 같은 Overlay를 재사용한다.
                CancelTransition();
                driver.SetColor(parameters.Color);
                var currentGeneration = ++generation;
                State = SceneFadeState.Revealing;
                LastFailure = null;

                Play
                (
                    driver,
                    driver.Alpha,
                    0.0f,
                    parameters.Duration,
                    currentGeneration,
                    CompleteReveal
                );
            }
            catch (Exception exception)
            {
                if (ReferenceEquals(exception, LastFailure))
                {
                    throw;
                }

                throw RollbackToStable(driver, exception);
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Fade Overlay가 없으면 지정 Layer에서 한 번 획득한다.
        /// </summary>
        // ------------------------------------------------------------
        private void EnsureOverlay()
        {
            if (overlay != null && overlayInitialized) return;

            if (overlay == null)
            {
                overlay = OverlayHandle<ISceneFadeDriver>.Acquire
                (
                    layerRegistry,
                    layerID,
                    source
                );
            }

            try
            {
                if (!overlay.View.IsValid)
                {
                    throw new InvalidOperationException("Scene Fade Driver가 유효하지 않습니다.");
                }

                overlay.View.Apply(0.0f);
                overlayInitialized = true;
            }
            catch (Exception exception)
            {
                try
                {
                    ReleaseOverlay();
                }
                catch (Exception cleanupException)
                {
                    throw new AggregateException
                    (
                        "Scene Fade Overlay 초기화와 반환이 실패했습니다.",
                        exception,
                        cleanupException
                    );
                }

                throw;
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 Fade 요청의 Transition을 시작한다.
        /// </summary>
        // ------------------------------------------------------------
        private void Play
        (
            ISceneFadeDriver driver,
            float startValue,
            float endValue,
            float duration,
            int currentGeneration,
            Action onCompleted
        )
        {
            driver.Apply(startValue);

            var parameters = new PresentationTransitionParams
            (
                driver,
                startValue,
                endValue,
                duration,
                timeSource
            );

            var playReturned = false;
            Exception synchronousFailure = null;

            void HandleFailure(Exception failure)
            {
                transition = null;

                if (!playReturned)
                {
                    synchronousFailure = RollbackToStable(driver, failure);
                    return;
                }

                HandleAsyncFailure(driver, failure);
            }

            var handle = transitioner.Play
            (
                parameters,
                () =>
                {
                    if (currentGeneration != generation) return;

                    transition = null;

                    try
                    {
                        onCompleted();
                    }
                    catch (Exception exception)
                    {
                        HandleFailure(exception);
                    }
                },
                exception =>
                {
                    if (currentGeneration != generation) return;

                    HandleFailure(exception);
                }
            );
            playReturned = true;

            if (synchronousFailure != null)
            {
                throw synchronousFailure;
            }

            // 동기 완료 backend는 callback에서 이미 상태를 확정했으므로 Handle을 보관하지 않는다.
            if (currentGeneration == generation &&
                (State == SceneFadeState.Covering || State == SceneFadeState.Revealing) &&
                handle.IsPending)
            {
                transition = handle;
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 진행 중 Transition callback을 무효화하고 backend 실행을 취소한다.
        /// </summary>
        // ------------------------------------------------------------
        private void CancelTransition()
        {
            generation++;

            var current = transition;
            current?.Cancel();
            transition = null;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Cover 완료 상태와 이벤트를 확정한다.
        /// </summary>
        // ------------------------------------------------------------
        private void CompleteCover()
        {
            stableState = SceneFadeState.Covered;
            State = SceneFadeState.Covered;
            LastFailure = null;
            InvokeSubscribers(OnCovered);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Reveal 완료 후 Overlay를 반환하고 이벤트를 발생시킨다.
        /// </summary>
        // ------------------------------------------------------------
        private void CompleteReveal()
        {
            ReleaseOverlay();
            stableState = SceneFadeState.Clear;
            State = SceneFadeState.Clear;
            LastFailure = null;
            InvokeSubscribers(OnRevealed);
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> 실패한 요청이 바꾼 Alpha와 Overlay 소유권을 마지막 완료 상태로 되돌리고,
        /// <br/> 최초 실패와 롤백 실패를 함께 보존한다.
        /// </summary>
        // ----------------------------------------------------------------------
        private Exception RollbackToStable
        (
            ISceneFadeDriver driver,
            Exception failure
        )
        {
            var errors = new List<Exception> { failure };
            var stableAlpha = stableState == SceneFadeState.Covered ? 1.0f : 0.0f;

            try
            {
                driver.Apply(stableAlpha);
            }
            catch (Exception exception)
            {
                errors.Add(exception);
            }

            if (stableState == SceneFadeState.Clear && transition == null)
            {
                try
                {
                    ReleaseOverlay();
                }
                catch (Exception exception)
                {
                    errors.Add(exception);
                }
            }

            State = stableState;
            LastFailure = errors.Count == 1
                ? failure
                : new AggregateException("Scene Fade 실패 롤백이 실패했습니다.", errors);
            return LastFailure;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 비동기 Fade 실패를 안정 상태로 복원하고 구독자에게 전달한다.
        /// </summary>
        // ------------------------------------------------------------
        private void HandleAsyncFailure
        (
            ISceneFadeDriver driver,
            Exception failure
        )
        {
            var reportedFailure = RollbackToStable(driver, failure);
            InvokeFailureSubscribers(OnFailed, reportedFailure);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 보유한 Fade Overlay를 정확히 한 번 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        private void ReleaseOverlay()
        {
            if (overlay == null) return;

            overlay.Dispose();
            overlay = null;
            overlayInitialized = false;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 해제된 SceneFader 사용을 거부한다.
        /// </summary>
        // ------------------------------------------------------------
        private void ThrowIfDisposed()
        {
            if (isDisposed)
            {
                throw new ObjectDisposedException(nameof(SceneFader));
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 완료 이벤트 구독자를 독립 호출해 상태 확정이 구독자 예외로 되돌아가지 않게 한다.
        /// </summary>
        // ------------------------------------------------------------
        private static void InvokeSubscribers(Action subscribers)
        {
            if (subscribers == null) return;

            var invocationList = subscribers.GetInvocationList();

            for (var i = 0; i < invocationList.Length; i++)
            {
                try
                {
                    ((Action)invocationList[i]).Invoke();
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                }
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 실패 구독자를 독립 호출해 한 구독자 예외가 다른 진단 전달을 막지 않게 한다.
        /// </summary>
        // ------------------------------------------------------------
        private static void InvokeFailureSubscribers
        (
            Action<Exception> subscribers,
            Exception failure
        )
        {
            if (subscribers == null) return;

            var invocationList = subscribers.GetInvocationList();

            for (var i = 0; i < invocationList.Length; i++)
            {
                try
                {
                    ((Action<Exception>)invocationList[i]).Invoke(failure);
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                }
            }
        }

    #endregion

    #region IDisposable

        // ------------------------------------------------------------
        /// <summary>
        /// 진행 중 Fade를 취소하고 보유 Overlay를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public void Dispose()
        {
            if (isDisposed) return;

            CancelTransition();
            ReleaseOverlay();
            State = SceneFadeState.Clear;
            stableState = SceneFadeState.Clear;
            LastFailure = null;
            OnCovered = null;
            OnRevealed = null;
            OnFailed = null;
            isDisposed = true;
        }

    #endregion

    }
}
