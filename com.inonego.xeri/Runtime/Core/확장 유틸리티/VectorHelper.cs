/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : VectorHelper.cs
수정일 : 2026-11-21

# 설명
Vector2·Vector3·Vector4·Vector2Int·Vector3Int에 대한 확장 메서드 모음.
Abs 등 컴포넌트 단위 수학 연산과 방향 벡터의 안전한 정규화를 제공한다.

# 특이사항, 제약사항
TryNormalize는 크기를 double로 계산한다. float 크기 계산은 런타임(Mono·.NET)마다 중간 정밀도가 달라
큰 유한 벡터나 아주 작은 벡터의 판정이 갈리므로, 실행 환경과 관계없이 같은 결과를 내도록 한다.
========================================================================= BLOCK_HEADER_END */

using System;

using UnityEngine;

using inonego;
using inonego.Xeri;
using inonego.Xeri.Primitive;

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

    #region 정규화

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> 유한하고 길이가 0이 아닌 벡터를 단위 벡터로 바꾼다.
        /// <br/> 크기를 double로 계산해 실행 환경과 관계없이 같은 결과를 낸다.
        /// </summary>
        // ------------------------------------------------------------
        public static bool TryNormalize(this Vector3 value, out Vector3 normalized)
        {
            normalized = Vector3.zero;

            if (!value.IsFinite())
            {
                return false;
            }

            var x = (double)value.x;
            var y = (double)value.y;
            var z = (double)value.z;
            var magnitude = Math.Sqrt(x * x + y * y + z * z);

            if (!(magnitude > 0d) || double.IsInfinity(magnitude))
            {
                return false;
            }

            var unit = new Vector3((float)(x / magnitude), (float)(y / magnitude), (float)(z / magnitude));

            if (!unit.IsFinite() || unit.sqrMagnitude <= 0f)
            {
                return false;
            }

            normalized = unit;
            return true;
        }

    #endregion

    }
}
