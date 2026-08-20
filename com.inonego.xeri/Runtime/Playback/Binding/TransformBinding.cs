/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TransformBinding.cs
수정일 : 2026-08-19

# 설명
Playback 실행을 Unity Transform의 현재 위치·회전·스케일과 수명에 결합하는 Runtime Binding을 정의한다.
선택적 로컬 위치·회전·스케일 보정을 추적 대상 Transform 기준으로 적용한다.
========================================================================= BLOCK_HEADER_END */

using System;

using UnityEngine;

namespace inonego.Xeri.Playback
{
    // ============================================================
    /// <summary>
    /// 이번 Playback 실행이 추적할 Unity Transform과 상대 Transform 보정.
    /// </summary>
    // ============================================================
    public readonly struct TransformBinding : ICueBinding
    {

    #region 필드

        // ------------------------------------------------------------
        /// <summary>
        /// Playback이 현재 Transform을 추적할 대상.
        /// </summary>
        // ------------------------------------------------------------
        public Transform Transform { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 대상 Transform 로컬 공간에서 적용할 위치 보정.
        /// </summary>
        // ------------------------------------------------------------
        public Vector3 PositionOffset { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 대상 Transform 회전에 곱할 상대 회전 보정.
        /// </summary>
        // ------------------------------------------------------------
        public Quaternion RotationOffset { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 대상 Transform의 World Scale에 곱할 스케일 보정.
        /// </summary>
        // ------------------------------------------------------------
        public Vector3 ScaleMultiplier { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 추적 결과의 월드 위치.
        /// </summary>
        // ------------------------------------------------------------
        public Vector3 Position => Transform.TransformPoint(PositionOffset);

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 추적 결과의 월드 회전.
        /// </summary>
        // ------------------------------------------------------------
        public Quaternion Rotation => Transform.rotation * RotationOffset;

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 추적 결과에 적용할 스케일.
        /// </summary>
        // ------------------------------------------------------------
        public Vector3 Scale => Vector3.Scale(Transform.lossyScale, ScaleMultiplier);

        // ------------------------------------------------------------
        /// <summary>
        /// 추적 대상과 상대 Transform 보정값이 현재 유효한지 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public bool IsValid =>
            Transform != null &&
            IsFinite(PositionOffset) &&
            IsFinite(RotationOffset) &&
            IsFinite(ScaleMultiplier) &&
            IsValidRotationMagnitude(RotationOffset);

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// 추적 대상 Transform을 보정 없이 그대로 사용하는 Binding을 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        public TransformBinding(Transform transform) : this
        (
            transform,
            Vector3.zero,
            Quaternion.identity,
            Vector3.one
        )
        {
            // NONE
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 추적 대상 Transform과 상대 위치·회전·스케일 보정으로 Binding을 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        public TransformBinding
        (
            Transform transform,
            Vector3 positionOffset,
            Quaternion rotationOffset,
            Vector3 scaleMultiplier
        )
        {
            Transform = transform ?? throw new ArgumentNullException(nameof(transform));
            PositionOffset = positionOffset;
            RotationOffset = rotationOffset;
            ScaleMultiplier = scaleMultiplier;
        }

    #endregion

    #region 검증

        // ------------------------------------------------------------
        /// <summary>
        /// Vector3의 모든 성분이 유한한지 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        private static bool IsFinite(Vector3 value) =>
            IsFinite(value.x) &&
            IsFinite(value.y) &&
            IsFinite(value.z);

        // ------------------------------------------------------------
        /// <summary>
        /// Quaternion의 모든 성분이 유한한지 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        private static bool IsFinite(Quaternion value) =>
            IsFinite(value.x) &&
            IsFinite(value.y) &&
            IsFinite(value.z) &&
            IsFinite(value.w);

        // ------------------------------------------------------------
        /// <summary>
        /// Quaternion 제곱합이 유한하고 회전을 표현할 수 있는 크기인지 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        private static bool IsValidRotationMagnitude(Quaternion rotation)
        {
            var squaredMagnitude = Quaternion.Dot(rotation, rotation);
            return IsFinite(squaredMagnitude) && squaredMagnitude > Mathf.Epsilon;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 단일 부동소수 값이 NaN이나 Infinity가 아닌지 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

    #endregion

    }
}
