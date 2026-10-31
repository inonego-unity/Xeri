/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : SpatialSample.cs
수정일 : 2026-10-31

# 설명
동일 평가 시점의 위치·조준 방향·회전과 독립 유효성을 보존한다.
========================================================================= BLOCK_HEADER_END */

using UnityEngine;

namespace inonego.Xeri.Spatial
{
    // ============================================================
    /// <summary>
    /// 위치·조준 방향·회전의 독립적인 공간 관측 결과.
    /// </summary>
    // ============================================================
    public readonly struct SpatialSample
    {

    #region 필드

        public bool HasPosition { get; }
        public Vector3 Position { get; }

        public bool HasDirection { get; }
        public Vector3 Direction { get; }

        public bool HasRotation { get; }
        public Quaternion Rotation { get; }

    #endregion

    #region 생성자

        internal SpatialSample
        (
            bool hasPosition, Vector3 position,
            bool hasDirection, Vector3 direction,
            bool hasRotation, Quaternion rotation
        )
        {
            HasPosition = hasPosition;
            Position = hasPosition ? position : default;
            HasDirection = hasDirection;
            Direction = hasDirection ? direction : default;
            HasRotation = hasRotation;
            Rotation = hasRotation ? rotation : default;
        }

    #endregion

    }
}
