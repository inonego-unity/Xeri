/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TRS_Q.cs
수정일 : 2026-08-22

# 설명
Quaternion 회전을 사용하는 위치·회전·스케일 공통 DTO를 정의한다.

# 특이사항, 제약사항
자체적으로 Local과 World 의미를 소유하지 않으며 소비하는 계약이 공간을 지정한다.
default(TRS_Q)는 유효한 회전을 갖지 않으므로 단위 TRS가 필요하면 Identity를 사용한다.
========================================================================= BLOCK_HEADER_END */

using System;

using UnityEngine;

namespace inonego.Xeri.Primitive
{
    // ================================================================================
    /// <summary>
    /// Quaternion 회전을 사용하는 직렬화 가능한 위치·회전·스케일 값.
    /// </summary>
    // ================================================================================
    [Serializable]
    public struct TRS_Q
    {

    #region 필드

        // ------------------------------------------------------------
        /// <summary>
        /// 위치 값.
        /// </summary>
        // ------------------------------------------------------------
        [SerializeField]
        public Vector3 Position;

        // ------------------------------------------------------------
        /// <summary>
        /// Quaternion 회전 값.
        /// </summary>
        // ------------------------------------------------------------
        [SerializeField]
        public Quaternion Rotation;

        // ------------------------------------------------------------
        /// <summary>
        /// 스케일 값.
        /// </summary>
        // ------------------------------------------------------------
        [SerializeField]
        public Vector3 Scale;

        // ------------------------------------------------------------
        /// <summary>
        /// 위치·회전·스케일이 유효한 Transform 값인지 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public bool IsValid
        {
            get
            {
                if
                (
                    !Position.IsFinite() ||
                    !Rotation.IsFinite() ||
                    !Scale.IsFinite()
                )
                {
                    return false;
                }

                var squared = Quaternion.Dot(Rotation, Rotation);
                return squared.IsFinite() && squared > Mathf.Epsilon;
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 변환을 적용하지 않는 단위 Quaternion TRS.
        /// </summary>
        // ------------------------------------------------------------
        public static TRS_Q Identity => new TRS_Q
        (
            Vector3.zero,
            Quaternion.identity,
            Vector3.one
        );

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// 위치·Quaternion 회전·스케일 값으로 TRS_Q를 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        public TRS_Q
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

    }
}
