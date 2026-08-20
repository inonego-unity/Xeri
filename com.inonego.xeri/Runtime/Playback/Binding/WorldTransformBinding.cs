/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : WorldTransformBinding.cs
수정일 : 2026-08-19

# 설명
Playback 실행을 고정된 월드 위치·회전·스케일에 결합하는 불변 Runtime Binding을 정의한다.
========================================================================= BLOCK_HEADER_END */

using UnityEngine;

namespace inonego.Xeri.Playback
{
    // ============================================================
    /// <summary>
    /// 이번 Playback 실행에 사용할 고정 World Transform 값.
    /// </summary>
    // ============================================================
    public readonly struct WorldTransformBinding : ICueBinding
    {

    #region 필드

        // ------------------------------------------------------------
        /// <summary>
        /// Playback을 배치할 월드 위치.
        /// </summary>
        // ------------------------------------------------------------
        public Vector3 Position { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// Playback에 적용할 월드 회전.
        /// </summary>
        // ------------------------------------------------------------
        public Quaternion Rotation { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// Playback에 적용할 스케일.
        /// </summary>
        // ------------------------------------------------------------
        public Vector3 Scale { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// Position·Rotation·Scale이 유효한 Transform 값인지 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public bool IsValid =>
            IsFinite(Position) &&
            IsFinite(Rotation) &&
            IsFinite(Scale) &&
            IsValidRotationMagnitude(Rotation);

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// 고정 월드 위치·회전과 단위 스케일로 Binding을 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        public WorldTransformBinding
        (
            Vector3 position,
            Quaternion rotation
        ) : this(position, rotation, Vector3.one)
        {
            // NONE
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 고정 월드 위치·회전·스케일로 Binding을 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        public WorldTransformBinding
        (
            Vector3 position,
            Quaternion rotation,
            Vector3 scale
        )
        {
            Position = position;
            Rotation = rotation;
            Scale = scale;
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
