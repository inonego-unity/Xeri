/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : VectorHelper.cs
수정일 : 2026-05-03

# 설명
Vector2·Vector3·Vector4·Vector2Int·Vector3Int에 대한 확장 메서드 모음.
Abs 등 컴포넌트 단위 수학 연산을 각 벡터 타입별로 제공한다.
========================================================================= BLOCK_HEADER_END */

using UnityEngine;

namespace inonego.Xeri
{
    // ============================================================
    /// <summary>
    /// 벡터 타입 확장 메서드 정적 헬퍼.
    /// </summary>
    // ============================================================
    public static class VectorHelper
    {

    #region Abs

        // ------------------------------------------------------------
        /// <summary>
        /// 각 컴포넌트의 절댓값으로 이루어진 Vector2를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public static Vector2 Abs(this Vector2 v)
        {
            return new Vector2(Mathf.Abs(v.x), Mathf.Abs(v.y));
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 각 컴포넌트의 절댓값으로 이루어진 Vector3을 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public static Vector3 Abs(this Vector3 v)
        {
            return new Vector3(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 각 컴포넌트의 절댓값으로 이루어진 Vector4를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public static Vector4 Abs(this Vector4 v)
        {
            return new Vector4(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z), Mathf.Abs(v.w));
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 각 컴포넌트의 절댓값으로 이루어진 Vector2Int를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public static Vector2Int Abs(this Vector2Int v)
        {
            return new Vector2Int(Mathf.Abs(v.x), Mathf.Abs(v.y));
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 각 컴포넌트의 절댓값으로 이루어진 Vector3Int를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public static Vector3Int Abs(this Vector3Int v)
        {
            return new Vector3Int(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));
        }

    #endregion

    }
}
