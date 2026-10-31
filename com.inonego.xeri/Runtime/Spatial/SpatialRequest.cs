/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : SpatialRequest.cs
수정일 : 2026-10-31

# 설명
공간 출처와 명시적인 방향 또는 원점 기준 조준 의도를 결합한다.
========================================================================= BLOCK_HEADER_END */

using System;

using UnityEngine;

namespace inonego.Xeri.Spatial
{
    // ============================================================
    /// <summary>
    /// 조준 방향의 해석 방식을 구별한다.
    /// </summary>
    // ============================================================
    public enum SpatialAimMode { None, Explicit, TowardSource }

    // ============================================================
    /// <summary>
    /// 공간 출처와 조준 의도를 독립적으로 표현한다.
    /// </summary>
    // ============================================================
    public readonly struct SpatialRequest<TReference>
    {

    #region 필드

        public SpatialSource<TReference> Source { get; }
        public SpatialAimMode AimMode { get; }
        public Vector3 Direction { get; }

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// 출처와 조준 규칙을 하나의 요청으로 결합한다.
        /// </summary>
        // ------------------------------------------------------------
        public SpatialRequest
        (
            SpatialSource<TReference> source,
            SpatialAimMode aimMode = SpatialAimMode.None,
            Vector3 direction = default
        )
        {
            if
            (
                aimMode != SpatialAimMode.None &&
                aimMode != SpatialAimMode.Explicit &&
                aimMode != SpatialAimMode.TowardSource
            )
            {
                throw new ArgumentOutOfRangeException(nameof(aimMode));
            }

            Source = source;
            AimMode = aimMode;
            Direction = direction;
        }

    #endregion

    }
}
