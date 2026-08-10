/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : WorldPoseBinding.cs
수정일 : 2026-08-10

# 설명
Playback 실행을 특정 월드 위치와 회전에 결합하는 불변 Runtime Binding을 정의한다.
========================================================================= BLOCK_HEADER_END */

using UnityEngine;

namespace inonego.Xeri.Playback
{
    // ============================================================
    /// <summary>
    /// 이번 Playback 실행에 사용할 고정 World Pose.
    /// </summary>
    // ============================================================
    public readonly struct WorldPoseBinding : ICueBinding
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
        /// Position과 Rotation이 유효한 World Pose 값인지 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public bool IsValid
        {
            get
            {
                return
                    IsFinite(Position.x) &&
                    IsFinite(Position.y) &&
                    IsFinite(Position.z) &&
                    IsFinite(Rotation.x) &&
                    IsFinite(Rotation.y) &&
                    IsFinite(Rotation.z) &&
                    IsFinite(Rotation.w) &&
                    IsValidRotationMagnitude(Rotation);
            }
        }

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// 고정 월드 위치와 회전으로 Binding을 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        public WorldPoseBinding
        (
            Vector3 position,
            Quaternion rotation
        )
        {
            Position = position;
            Rotation = rotation;
        }

    #endregion

    #region 검증

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
