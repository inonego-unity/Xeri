/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TransformBinding_Fixed.cs
수정일 : 2026-08-22

# 설명
Playback 실행을 재생 시작 시 확정된 World TRS에 결합하는 Runtime Binding을 정의한다.

# 특이사항, 제약사항
World 값은 생성 이후 추적 대상 없이 고정되며, 이동 대상을 따라가야 하는 실행에는 TransformBinding_Tracked를 사용한다.
========================================================================= BLOCK_HEADER_END */

using UnityEngine;

using inonego;
using inonego.Xeri;
using inonego.Xeri.Primitive;

namespace inonego.Xeri.Playback
{
    // ======================================================================
    /// <summary>
    /// 이번 Playback 실행에 사용할 고정 World TRS Binding.
    /// </summary>
    // ======================================================================
    public readonly struct TransformBinding_Fixed : ICueBinding
    {

    #region 필드

        // ------------------------------------------------------------
        /// <summary>
        /// Playback에 적용할 고정 World TRS.
        /// </summary>
        // ------------------------------------------------------------
        public TRS_Q World { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 고정 World TRS가 현재 유효한지 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public bool IsValid => World.IsValid;

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// 고정 World TRS로 Binding을 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        public TransformBinding_Fixed(in TRS_Q world)
        {
            World = world;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 고정 World 위치·회전과 단위 스케일로 Binding을 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        public TransformBinding_Fixed
        (
            Vector3 position,
            Quaternion rotation
        ) : this(new TRS_Q(position, rotation, Vector3.one))
        {
            // NONE
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 고정 World 위치·회전·스케일로 Binding을 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        public TransformBinding_Fixed
        (
            Vector3 position,
            Quaternion rotation,
            Vector3 scale
        ) : this(new TRS_Q(position, rotation, scale))
        {
            // NONE
        }

    #endregion

    }
}
