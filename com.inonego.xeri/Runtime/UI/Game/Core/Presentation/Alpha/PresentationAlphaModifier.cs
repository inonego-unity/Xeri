/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : PresentationAlphaModifier.cs
수정일 : 2026-09-03

# 설명
하나의 Numeric Alpha Modifier를 여러 PresentationAlpha에 연결하고 동일 값으로 평가한다.
외부 presentation 효과의 Modifier, target Lease와 Refresh 수명을 한 경계에서 소유한다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

using inonego;
using inonego.Xeri;
using inonego.Xeri.Serializable;

namespace inonego.Xeri.UI.Game
{
    // ================================================================================
    /// <summary>
    /// 여러 Presentation Alpha에 같은 Numeric Modifier를 적용하는 외부 효과 수명.
    /// </summary>
    // ================================================================================
    [Serializable]
    public sealed class PresentationAlphaModifier :
        IPresentationTransitionTarget,
        IDisposable
    {

    #region 필드

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 Modifier에 적용된 값.
        /// </summary>
        // ------------------------------------------------------------
        public float Value => modifier.Value;

        private readonly NumericFModifier modifier = null;
        private readonly string key = "";
        private readonly int order = 0;
        private readonly List<PresentationAlpha> targets = new();
        private readonly List<Lease> leases = new();
        private bool isDisposed = false;

    #endregion

    #region 생성자

        // ----------------------------------------------------------------------
        /// <summary>
        /// 지정 key, Numeric 연산과 초기값으로 외부 Alpha Modifier 수명을 생성한다.
        /// </summary>
        // ----------------------------------------------------------------------
        public PresentationAlphaModifier
        (
            string key,
            NumericFOperation operation,
            float initialValue,
            int order = 0
        ) : base()
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException("Presentation Alpha Modifier Key가 비어 있습니다.", nameof(key));
            }

            if (!Enum.IsDefined(typeof(NumericFOperation), operation))
            {
                throw new ArgumentOutOfRangeException(nameof(operation));
            }

            if (float.IsNaN(initialValue) || float.IsInfinity(initialValue))
            {
                throw new ArgumentOutOfRangeException(nameof(initialValue));
            }

            this.key = key;
            this.order = order;
            modifier = new NumericFModifier(operation, initialValue);
        }

    #endregion

    #region 대상 연결

        // ------------------------------------------------------------
        /// <summary>
        /// 유효한 Presentation Alpha에 현재 Modifier를 한 번 연결한다.
        /// </summary>
        // ------------------------------------------------------------
        public void Add(PresentationAlpha alpha)
        {
            ThrowIfDisposed();

            if (alpha == null || !alpha.IsValid)
            {
                throw new InvalidOperationException("연결할 Presentation Alpha가 유효하지 않습니다.");
            }

            if (targets.Contains(alpha)) return;

            var lease = alpha.AcquireModifier(key, modifier, order);
            targets.Add(alpha);
            leases.Add(lease);
        }

    #endregion

    #region IPresentationTransitionTarget

        // ----------------------------------------------------------------------
        /// <summary>
        /// 연결된 모든 Presentation Alpha가 현재 Modifier 값을 적용할 수 있는지 여부.
        /// </summary>
        // ----------------------------------------------------------------------
        public bool IsValid
        {
            get
            {
                if (isDisposed || targets.Count == 0) return false;

                for (var index = 0; index < targets.Count; index++)
                {
                    if (!targets[index].IsValid) return false;
                }

                return true;
            }
        }

        // --------------------------------------------------------------------------------
        /// <summary>
        /// <br/> Modifier 값을 갱신한 뒤 연결된 모든 Presentation Alpha를 다시 평가한다.
        /// <br/> 모든 Target은 같은 적용 지점에서 최종 Alpha를 갱신한다.
        /// </summary>
        // --------------------------------------------------------------------------------
        public void Apply(float value)
        {
            ThrowIfDisposed();

            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            modifier.Value = value;
            List<Exception> errors = null;

            for (var index = 0; index < targets.Count; index++)
            {
                try
                {
                    targets[index].Refresh();
                }
                catch (Exception exception)
                {
                    errors ??= new List<Exception>();
                    errors.Add(exception);
                }
            }

            if (errors != null)
            {
                throw new AggregateException
                (
                    "Presentation Alpha Modifier 적용 중 하나 이상의 Target 갱신이 실패했습니다.",
                    errors
                );
            }
        }

    #endregion

    #region 검증

        // ------------------------------------------------------------
        /// <summary>
        /// 종료된 Modifier operation 사용을 거부한다.
        /// </summary>
        // ------------------------------------------------------------
        private void ThrowIfDisposed()
        {
            if (isDisposed)
            {
                throw new ObjectDisposedException(nameof(PresentationAlphaModifier));
            }
        }

    #endregion

    #region IDisposable

        // ----------------------------------------------------------------------
        /// <summary>
        /// 연결된 Modifier Lease를 역순으로 반환하고 모든 Target 참조를 종료한다.
        /// </summary>
        // ----------------------------------------------------------------------
        public void Dispose()
        {
            if (isDisposed) return;

            isDisposed = true;
            List<Exception> errors = null;

            for (var index = leases.Count - 1; index >= 0; index--)
            {
                try
                {
                    leases[index]?.Dispose();
                }
                catch (Exception exception)
                {
                    errors ??= new List<Exception>();
                    errors.Add(exception);
                }
            }

            leases.Clear();
            targets.Clear();

            if (errors != null)
            {
                throw new AggregateException
                (
                    "Presentation Alpha Modifier 반환 중 하나 이상의 Target 정리가 실패했습니다.",
                    errors
                );
            }
        }

    #endregion

    }
}
