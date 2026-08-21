/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TransformBinding_Tracked.cs
수정일 : 2026-08-22

# 설명
Playback 실행을 Unity Transform의 현재 World TRS와 수명에 결합하는 Runtime Binding을 정의한다.
Local TRS를 Target 기준으로 합성해 매 조회 시 현재 World TRS를 계산한다.

# 특이사항, 제약사항
Target이 파괴되면 Binding은 유효하지 않으며, 재생 주체가 해당 Playback 수명을 종료한다.
========================================================================= BLOCK_HEADER_END */

using System;

using UnityEngine;

using inonego;
using inonego.Xeri;
using inonego.Xeri.Primitive;

namespace inonego.Xeri.Playback
{
    // ======================================================================
    /// <summary>
    /// 이번 Playback 실행이 추적할 Target과 Target 기준 Local TRS.
    /// </summary>
    // ======================================================================
    public readonly struct TransformBinding_Tracked : ICueBinding
    {

    #region 필드

        // ------------------------------------------------------------
        /// <summary>
        /// Playback이 현재 Transform을 추적할 대상.
        /// </summary>
        // ------------------------------------------------------------
        public Transform Target { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// Target 로컬 공간에서 적용할 상대 TRS.
        /// </summary>
        // ------------------------------------------------------------
        public TRS_Q Local { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// Target의 현재 Transform과 Local을 합성한 World TRS.
        /// </summary>
        // ------------------------------------------------------------
        public TRS_Q World
        {
            get
            {
                return new TRS_Q
                (
                    Target.TransformPoint(Local.Position),
                    Target.rotation * Local.Rotation,
                    Vector3.Scale(Target.lossyScale, Local.Scale)
                );
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 추적 Target과 Local TRS가 현재 유효한지 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public bool IsValid => Target != null && Local.IsValid;

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// Target의 현재 World TRS를 그대로 추적하는 Binding을 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        public TransformBinding_Tracked(Transform target) : this(target, TRS_Q.Identity)
        {
            // NONE
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 추적 Target과 Target 기준 Local TRS로 Binding을 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        public TransformBinding_Tracked
        (
            Transform target,
            in TRS_Q local
        )
        {
            Target = target ?? throw new ArgumentNullException(nameof(target));
            Local = local;
        }

    #endregion

    }
}
