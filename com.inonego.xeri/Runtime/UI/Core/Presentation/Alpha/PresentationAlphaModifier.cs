/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : PresentationAlphaModifier.cs
수정일 : 2026-09-18

# 설명
동일한 Numeric Alpha 효과를 여러 PresentationAlpha에 연결하는 수명을 관리한다.
각 Target은 독립 Modifier binding을 가지며 값 변경은 MValue의 reactive 갱신으로 즉시 반영된다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

using inonego;
using inonego.Xeri;
using inonego.Xeri.Serializable;

namespace inonego.Xeri.UI
{
    // ======================================================================
    /// <summary>
    /// 여러 Presentation Alpha에 같은 Numeric 효과를 적용하는 외부 효과 수명.
    /// </summary>
    // ======================================================================
    [Serializable]
    public sealed class PresentationAlphaModifier :
        IPresentationTransitionTarget,
        IDisposable
    {

    #region 내부 데이터

        // ======================================================================
        /// <summary>
        /// 하나의 Presentation Alpha와 전용 Modifier Lease를 묶는 binding.
        /// </summary>
        // ======================================================================
        private sealed class Binding
        {
            private PresentationAlpha Target { get; }
            private NumericFModifier Modifier { get; }
            private Lease Lease { get; }

            // ------------------------------------------------------------
            /// <summary>
            /// 지정 Target과 Modifier Lease를 하나의 binding으로 묶는다.
            /// </summary>
            // ------------------------------------------------------------
            public Binding
            (
                PresentationAlpha target,
                NumericFModifier modifier,
                Lease lease
            )
            {
                Target = target;
                Modifier = modifier;
                Lease = lease;
            }

            // ------------------------------------------------------------
            /// <summary>
            /// 현재 Target이 유효한지 여부.
            /// </summary>
            // ------------------------------------------------------------
            public bool IsValid => Target != null && Target.IsValid;

            // ------------------------------------------------------------
            /// <summary>
            /// Modifier 값을 변경한다.
            /// </summary>
            // ------------------------------------------------------------
            public void SetValue(float value)
            {
                if (!IsValid)
                {
                    throw new InvalidOperationException("Presentation Alpha Target이 유효하지 않습니다.");
                }

                Modifier.Value = value;
            }

            // ------------------------------------------------------------
            /// <summary>
            /// 연결된 Modifier Lease를 반환한다.
            /// </summary>
            // ------------------------------------------------------------
            public void Dispose()
            {
                var wasValid = IsValid;

                Lease?.Dispose();

                if (!wasValid)
                {
                    throw new InvalidOperationException("Presentation Alpha Target이 유효하지 않습니다.");
                }
            }

            // ------------------------------------------------------------
            /// <summary>
            /// 지정 Presentation Alpha와 같은 Target을 가리키는지 확인한다.
            /// </summary>
            // ------------------------------------------------------------
            public bool Matches(PresentationAlpha target)
            {
                return ReferenceEquals(Target, target);
            }
        }

    #endregion

    #region 필드

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 Modifier에 적용된 값.
        /// </summary>
        // ------------------------------------------------------------
        public float Value => value;

        private float value = 0.0f;

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 하나 이상의 Presentation Alpha가 연결되어 있는지 여부.
        /// </summary>
        // ------------------------------------------------------------
        public bool HasTargets => !isDisposed && bindings.Count > 0;

        private readonly string key = "";
        private readonly NumericFOperation operation = NumericFOperation.SET;
        private readonly int order = 0;
        private readonly List<Binding> bindings = new();
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
            this.operation = operation;
            this.order = order;
            value = initialValue;
        }

    #endregion

    #region 대상 연결

        // ----------------------------------------------------------------------
        /// <summary>
        /// 유효한 Presentation Alpha에 현재 효과의 전용 Modifier를 한 번 연결한다.
        /// </summary>
        // ----------------------------------------------------------------------
        public void Add(PresentationAlpha alpha)
        {
            ThrowIfDisposed();

            if (alpha == null || !alpha.IsValid)
            {
                throw new InvalidOperationException("연결할 Presentation Alpha가 유효하지 않습니다.");
            }

            for (var index = 0; index < bindings.Count; index++)
            {
                if (bindings[index].Matches(alpha))
                {
                    return;
                }
            }

            var modifier = new NumericFModifier(operation, value);
            var lease = alpha.AcquireModifier(key, modifier, order);

            bindings.Add(new Binding(alpha, modifier, lease));
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
                if (isDisposed || bindings.Count == 0) return false;

                for (var index = 0; index < bindings.Count; index++)
                {
                    if (!bindings[index].IsValid) return false;
                }

                return true;
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 연결된 각 Target의 전용 Modifier 값을 갱신한다.
        /// </summary>
        // ------------------------------------------------------------
        public void Apply(float value)
        {
            ThrowIfDisposed();

            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            this.value = value;
            List<Exception> errors = null;

            for (var index = 0; index < bindings.Count; index++)
            {
                try
                {
                    bindings[index].SetValue(value);
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
        /// 연결된 Modifier Lease를 역순으로 반환하고 모든 Target binding을 종료한다.
        /// </summary>
        // ----------------------------------------------------------------------
        public void Dispose()
        {
            if (isDisposed) return;

            isDisposed = true;
            List<Exception> errors = null;

            for (var index = bindings.Count - 1; index >= 0; index--)
            {
                try
                {
                    bindings[index].Dispose();
                }
                catch (Exception exception)
                {
                    errors ??= new List<Exception>();
                    errors.Add(exception);
                }
            }

            bindings.Clear();

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
