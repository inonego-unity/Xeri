/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : SceneFader.cs
수정일 : 2026-07-30

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

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> Fade Overlay를 불투명하게 전환하고 Covered 상태로 유지한다.
        /// <br/> 완료와 비동기 실패는 이 요청에 전달된 callback으로만 알린다.
        /// </summary>
        // ----------------------------------------------------------------------
        public void Cover
        (
            SceneFadeParams parameters,
            Action onCompleted = null,
            Action<Exception> onFailed = null
        )
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
                    () => CompleteCover(onCompleted),
                    onFailed
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

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> 보유한 Fade Overlay를 투명하게 전환한 뒤 반환한다.
        /// <br/> 완료와 비동기 실패는 이 요청에 전달된 callback으로만 알린다.
        /// </summary>
        // ----------------------------------------------------------------------
        public void Reveal
        (
            SceneFadeParams parameters,
            Action onCompleted = null,
            Action<Exception> onFailed = null
        )
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
                    () => CompleteReveal(onCompleted),
                    onFailed
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
                var driver = overlay.View;

                if (!driver.IsValid)
                {
                    throw new InvalidOperationException("Scene Fade Driver가 유효하지 않습니다.");
                }

                driver.Apply(0.0f);
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
            Action onCompleted,
            Action<Exception> onFailed
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

                HandleAsyncFailure(driver, failure, onFailed);
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
            transition = null;
            current?.Cancel();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Cover 상태를 확정한 뒤 현재 요청의 완료 callback을 호출한다.
        /// </summary>
        // ------------------------------------------------------------
        private void CompleteCover(Action onCompleted)
        {
            stableState = SceneFadeState.Covered;
            State = SceneFadeState.Covered;
            LastFailure = null;

            // 확정된 Fade 상태를 소비자 callback 예외로 되돌리지 않는다.
            try
            {
                onCompleted?.Invoke();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// Reveal 상태와 Overlay 반환을 확정한 뒤 현재 요청의 완료 callback을 호출한다.
        /// </summary>
        // ----------------------------------------------------------------------
        private void CompleteReveal(Action onCompleted)
        {
            stableState = SceneFadeState.Clear;
            State = SceneFadeState.Clear;
            ReleaseOverlay();
            LastFailure = null;

            // Overlay 반환까지 끝난 요청을 소비자 callback 예외로 실패 처리하지 않는다.
            try
            {
                onCompleted?.Invoke();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
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

        // ----------------------------------------------------------------------
        /// <summary>
        /// 비동기 Fade 실패를 안정 상태로 복원하고 현재 요청의 실패 callback에 전달한다.
        /// </summary>
        // ----------------------------------------------------------------------
        private void HandleAsyncFailure
        (
            ISceneFadeDriver driver,
            Exception failure,
            Action<Exception> onFailed
        )
        {
            var reportedFailure = RollbackToStable(driver, failure);

            // 소비자 callback 예외가 원래 Fade 실패를 대체하지 않게 기록만 한다.
            try
            {
                onFailed?.Invoke(reportedFailure);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 보유한 Fade Overlay를 정확히 한 번 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        private void ReleaseOverlay()
        {
            if (overlay == null) return;

            var current = overlay;
            overlay = null;
            overlayInitialized = false;
            current.Dispose();
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

            isDisposed = true;
            State = SceneFadeState.Clear;
            stableState = SceneFadeState.Clear;
            LastFailure = null;

            var errors = new List<Exception>();

            try
            {
                CancelTransition();
            }
            catch (Exception exception)
            {
                errors.Add(exception);
            }

            try
            {
                ReleaseOverlay();
            }
            catch (Exception exception)
            {
                errors.Add(exception);
            }

            if (errors.Count > 0)
            {
                throw new AggregateException("Scene Fader 해제가 실패했습니다.", errors);
            }
        }

    #endregion

    }
}
