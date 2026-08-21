/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TRS_E.cs
수정일 : 2026-08-22

# 설명
Euler 회전을 사용하는 위치·회전·스케일 공통 DTO를 정의한다.

# 특이사항, 제약사항
자체적으로 Local과 World 의미를 소유하지 않으며 소비하는 계약이 공간을 지정한다.
Quaternion으로 변환한 뒤에는 작성된 Euler 축 값을 역변환으로 복원할 수 없다.
========================================================================= BLOCK_HEADER_END */

using System;

using UnityEngine;

namespace inonego.Xeri.Primitive
{
    // ================================================================================
    /// <summary>
    /// Euler 회전을 사용하는 직렬화 가능한 위치·회전·스케일 값.
    /// </summary>
    // ================================================================================
    [Serializable]
    public struct TRS_E
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
        /// 각 축의 Euler 각도 값.
        /// </summary>
        // ------------------------------------------------------------
        [SerializeField]
        public Vector3 Rotation;

        // ------------------------------------------------------------
        /// <summary>
        /// 스케일 값.
        /// </summary>
        // ------------------------------------------------------------
        [SerializeField]
        public Vector3 Scale;

        // ------------------------------------------------------------
        /// <summary>
        /// 위치·Euler 회전·스케일의 모든 성분이 유한한지 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public bool IsValid
        {
            get
            {
                return
                    Position.IsFinite() &&
                    Rotation.IsFinite() &&
                    Scale.IsFinite();
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 변환을 적용하지 않는 단위 Euler TRS.
        /// </summary>
        // ------------------------------------------------------------
        public static TRS_E Identity => new TRS_E
        (
            Vector3.zero,
            Vector3.zero,
            Vector3.one
        );

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// 위치·Euler 회전·스케일 값으로 TRS_E를 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        public TRS_E
        (
            Vector3 position,
            Vector3 rotation,
            Vector3 scale
        )
        {
            Position = position;
            Rotation = rotation;
            Scale = scale;
        }

    #endregion

    #region 변환

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> 작성된 Euler 각도를 Unity Quaternion으로 평가해
        /// <br/> 동일한 위치·스케일을 가진 TRS_Q로 변환한다.
        /// </summary>
        // ----------------------------------------------------------------------
        public TRS_Q ToTRS_Q()
        {
            return new TRS_Q
            (
                Position,
                Quaternion.Euler(Rotation),
                Scale
            );
        }

    #endregion

    }
}
