/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : NumericHelper.cs
수정일 : 2026-08-21

# 설명
스칼라 수치와 Unity 수치 구성 타입의 유한성을 판정하는 확장 메서드를 제공한다.

# 특이사항, 제약사항
값의 부호나 범위 등 도메인 유효성은 판정하지 않고 NaN과 Infinity 여부만 판정한다.
========================================================================= BLOCK_HEADER_END */

using UnityEngine;

namespace inonego.Xeri.Primitive
{
    // ======================================================================
    /// <summary>
    /// 스칼라 수치와 Unity 수치 구성 타입의 공통 확장 메서드.
    /// </summary>
    // ======================================================================
    public static class NumericHelper
    {

    #region 유한성

        // ------------------------------------------------------------
        /// <summary>
        /// float 값이 NaN이나 Infinity가 아닌지 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public static bool IsFinite(this float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// double 값이 NaN이나 Infinity가 아닌지 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public static bool IsFinite(this double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Vector2의 모든 성분이 유한한지 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public static bool IsFinite(this Vector2 value)
        {
            return IsFinite(value.x) && IsFinite(value.y);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Vector3의 모든 성분이 유한한지 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public static bool IsFinite(this Vector3 value)
        {
            return IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Vector4의 모든 성분이 유한한지 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public static bool IsFinite(this Vector4 value)
        {
            return IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z) && IsFinite(value.w);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Quaternion의 모든 성분이 유한한지 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public static bool IsFinite(this Quaternion value)
        {
            return IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z) && IsFinite(value.w);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Color의 모든 성분이 유한한지 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public static bool IsFinite(this Color value)
        {
            return IsFinite(value.r) && IsFinite(value.g) && IsFinite(value.b) && IsFinite(value.a);
        }

    #endregion

    }
}
