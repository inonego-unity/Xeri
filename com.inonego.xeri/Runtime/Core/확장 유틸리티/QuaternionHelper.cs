/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : QuaternionHelper.cs
수정일 : 2026-11-22

# 설명
Quaternion에 대한 확장 메서드 모음.
회전 값의 안전한 정규화를 제공한다.

# 특이사항, 제약사항
TryNormalize는 VectorHelper.TryNormalize와 같은 규칙으로 크기를 double로 계산한다.
float 크기 계산은 런타임(Mono·.NET)마다 중간 정밀도가 달라 큰 유한 값이나 아주 작은 값의 판정이 갈리므로,
실행 환경과 관계없이 같은 결과를 내도록 한다.
실패하면 회전 없음(Quaternion.identity)을 돌려준다. 성분이 모두 0인 값은 회전이 아니므로 거절한다.
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
    /// Quaternion 확장 메서드 정적 헬퍼.
    /// </summary>
    // ============================================================
    public static class QuaternionHelper
    {

    #region 정규화

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> 유한하고 크기가 0이 아닌 값을 단위 Quaternion으로 바꾼다.
        /// <br/> 크기를 double로 계산해 실행 환경과 관계없이 같은 결과를 낸다.
        /// <br/> 실패하면 Quaternion.identity를 돌려준다.
        /// </summary>
        // ------------------------------------------------------------
        public static bool TryNormalize(this Quaternion value, out Quaternion normalized)
        {
            normalized = Quaternion.identity;

            if (!value.IsFinite())
            {
                return false;
            }

            var x = (double)value.x;
            var y = (double)value.y;
            var z = (double)value.z;
            var w = (double)value.w;
            var magnitude = Math.Sqrt(x * x + y * y + z * z + w * w);

            if (!(magnitude > 0d) || double.IsInfinity(magnitude))
            {
                return false;
            }

            var unit = new Quaternion((float)(x / magnitude), (float)(y / magnitude), (float)(z / magnitude), (float)(w / magnitude));

            if (!unit.IsFinite())
            {
                return false;
            }

            normalized = unit;
            return true;
        }

    #endregion

    }
}
