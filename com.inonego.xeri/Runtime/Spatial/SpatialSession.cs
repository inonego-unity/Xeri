/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : SpatialSession.cs
수정일 : 2026-10-31

# 설명
공간 요청을 외부 실행 시각에 평가하고 추적·고정 결과를 실행별로 유지한다.
========================================================================= BLOCK_HEADER_END */

using System;

using UnityEngine;

using inonego;
using inonego.Xeri;
using inonego.Xeri.Primitive;

namespace inonego.Xeri.Spatial
{
    // ============================================================
    /// <summary>
    /// 공간 값을 포착하거나 시간에 따라 추적·고정한다.
    /// </summary>
    // ============================================================
    public enum SpatialTimeMode { Capture, Follow, LockAtTime }

    // ============================================================
    /// <summary>
    /// 한 실행의 공간 관측과 시간 고정 상태를 소유한다.
    /// </summary>
    // ============================================================
    public sealed class SpatialSession<TReference>
    {

    #region 필드

        public SpatialSample Current { get; private set; }
        public bool HasBegun { get; private set; }
        public bool IsLocked { get; private set; }

        private readonly SpatialRequest<TReference> request;
        private readonly SpatialTimeMode mode;
        private readonly ISpatialSourceProvider<TReference> provider;
        private readonly float lockTime;
        private float currentTime;

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// 실행별 공간 해석과 시간 정책을 준비한다.
        /// </summary>
        // ------------------------------------------------------------
        public SpatialSession
        (
            in SpatialRequest<TReference> request,
            SpatialTimeMode mode,
            ISpatialSourceProvider<TReference> provider = null,
            float lockTime = 0f
        )
        {
            if
            (
                mode != SpatialTimeMode.Capture &&
                mode != SpatialTimeMode.Follow &&
                mode != SpatialTimeMode.LockAtTime
            )
            {
                throw new ArgumentOutOfRangeException(nameof(mode));
            }

            if (!lockTime.IsFinite() || lockTime < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(lockTime));
            }

            if
            (
                request.Source.Kind == SpatialSourceKind.Reference &&
                provider == null
            )
            {
                throw new ArgumentNullException(nameof(provider));
            }

            this.request = request;
            this.mode = mode;
            this.provider = provider;
            this.lockTime = lockTime;
        }

    #endregion

    #region 실행 수명

        // ------------------------------------------------------------
        /// <summary>
        /// 과거 실행 상태를 초기화하고 시작 시점에 한 번 관측한다.
        /// </summary>
        // ------------------------------------------------------------
        public void Begin(float time, Vector3 origin)
        {
            ValidateTime(time);

            Current = default;
            HasBegun = false;
            IsLocked = false;
            currentTime = time;

            Current = Resolve(origin);
            IsLocked =
                mode == SpatialTimeMode.Capture ||
                (mode == SpatialTimeMode.LockAtTime && time >= lockTime);
            HasBegun = true;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> 실행 시각이 전진하면 공간 값을 새로 관측한다.
        /// <br/> 고정된 실행에서는 시각만 전진하고 샘플은 유지한다.
        /// </summary>
        // ------------------------------------------------------------
        public void Evaluate(float time, Vector3 origin)
        {
            if (!HasBegun)
            {
                throw new InvalidOperationException("Begin 이전에는 공간을 평가할 수 없습니다.");
            }

            ValidateTime(time);

            if (time < currentTime)
            {
                throw new InvalidOperationException("시간을 되감으려면 Begin으로 재시작해야 합니다.");
            }

            if (time == currentTime) return;

            if (!IsLocked)
            {
                var sample = Resolve(origin);
                Current = sample;

                if (mode == SpatialTimeMode.LockAtTime && time >= lockTime)
                {
                    IsLocked = true;
                }
            }

            currentTime = time;
        }

    #endregion

    #region 공간 해석

        // ------------------------------------------------------------
        /// <summary>
        /// 동일한 공급자 관측으로 위치와 조준을 계산한다.
        /// </summary>
        // ------------------------------------------------------------
        private SpatialSample Resolve(Vector3 origin)
        {
            var source = request.Source;
            var hasPosition = false;
            var position = default(Vector3);
            var hasRotation = false;
            var rotation = default(Quaternion);

            if (source.Kind == SpatialSourceKind.Point)
            {
                hasPosition = source.Position.IsFinite();
                position = source.Position;
            }
            else if (source.Kind == SpatialSourceKind.Reference)
            {
                if
                (
                    provider.TryResolve
                    (
                        source.Reference,
                        out var observedPosition,
                        out var observedRotation,
                        out var observedRotationAvailable
                    ) &&
                    observedPosition.IsFinite()
                )
                {
                    hasPosition = true;
                    position = observedPosition;
                    hasRotation =
                        observedRotationAvailable &&
                        TryNormalize(observedRotation, out rotation);
                }
            }

            var hasDirection = false;
            var direction = default(Vector3);

            if (request.AimMode == SpatialAimMode.Explicit)
            {
                hasDirection = TryNormalize(request.Direction, out direction);
            }
            else if
            (
                request.AimMode == SpatialAimMode.TowardSource &&
                hasPosition &&
                origin.IsFinite()
            )
            {
                hasDirection = TryNormalize(position - origin, out direction);
            }

            return new SpatialSample
            (
                hasPosition, position,
                hasDirection, direction,
                hasRotation, rotation
            );
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 유효한 방향을 단위 벡터로 변환한다.
        /// </summary>
        // ------------------------------------------------------------
        private static bool TryNormalize(Vector3 value, out Vector3 normalized)
        {
            normalized = default;
            if (!value.IsFinite() || value.sqrMagnitude <= Mathf.Epsilon)
            {
                return false;
            }

            normalized = value.normalized;
            return normalized.IsFinite();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 유효한 Quaternion을 단위 회전으로 변환한다.
        /// </summary>
        // ------------------------------------------------------------
        private static bool TryNormalize(Quaternion value, out Quaternion normalized)
        {
            normalized = default;
            if (!value.IsFinite()) return false;

            var squared = Quaternion.Dot(value, value);
            if (!squared.IsFinite() || squared <= Mathf.Epsilon) return false;

            var scale = 1f / Mathf.Sqrt(squared);
            normalized = new Quaternion
            (
                value.x * scale,
                value.y * scale,
                value.z * scale,
                value.w * scale
            );
            return normalized.IsFinite();
        }

    #endregion

    #region 시간 검증

        // ------------------------------------------------------------
        /// <summary>
        /// 입력된 실행 시각의 유효성을 검증한다.
        /// </summary>
        // ------------------------------------------------------------
        private static void ValidateTime(float time)
        {
            if (!time.IsFinite() || time < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(time));
            }
        }

    #endregion

    }
}
