/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TransformBinding.cs
수정일 : 2026-08-10

# 설명
Playback 실행을 특정 Unity Transform의 현재 Pose와 수명에 결합하는 Runtime Binding을 정의한다.
========================================================================= BLOCK_HEADER_END */

using System;

using UnityEngine;

namespace inonego.Xeri.Playback
{
    // ============================================================
    /// <summary>
    /// 이번 Playback 실행이 추적할 Unity Transform.
    /// </summary>
    // ============================================================
    public readonly struct TransformBinding : ICueBinding
    {
        
    #region 필드

        // ------------------------------------------------------------
        /// <summary>
        /// Playback이 현재 Pose를 추적할 Transform.
        /// </summary>
        // ------------------------------------------------------------
        public Transform Transform { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 추적 대상 Transform이 현재 유효한지 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public bool IsValid => Transform != null;

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// 추적 대상 Transform으로 Binding을 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        public TransformBinding(Transform transform)
        {
            Transform = transform ?? throw new ArgumentNullException(nameof(transform));
        }

    #endregion

    }
}
