/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : VFXGraphBinding.cs
수정일 : 2026-08-22

# 설명
Unity VFX Graph Playback의 추적 Transform과 최초 Play 전 입력 준비 동작을 함께 전달한다.

# 특이사항, 제약사항
준비 동작은 Player가 획득한 VisualEffect에 한 번 적용되고 Reinit과 Play보다 먼저 완료된다.
Playback 진행 중 입력 갱신과 종료 수명은 호출자가 반환된 UnityVFXGraphPlayback으로 관리한다.
========================================================================= BLOCK_HEADER_END */

using System;

using UnityEngine.VFX;

namespace inonego.Xeri.Playback
{
    // ================================================================================
    /// <summary>
    /// <br/> VFX Graph Playback이 추적할 Transform과 최초 Play 전에 실행할
    /// <br/> VisualEffect 입력 준비 동작을 함께 전달하는 Runtime Binding.
    /// </summary>
    // ================================================================================
    public readonly struct VFXGraphBinding : ICueBinding
    {

    #region 필드

        // ------------------------------------------------------------
        /// <summary>
        /// VFX Graph Root가 Playback 수명 동안 추적할 Transform Binding.
        /// </summary>
        // ------------------------------------------------------------
        public TransformBinding_Tracked Tracking { get; }

        // ----------------------------------------------------------------------
        /// <summary>
        /// Reinit과 최초 Play 전에 VisualEffect의 노출 입력을 준비하는 동작.
        /// </summary>
        // ----------------------------------------------------------------------
        public Action<VisualEffect> PrepareVFX { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 추적 Transform과 입력 준비 동작이 모두 유효한지 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public bool IsValid => Tracking.IsValid && PrepareVFX != null;

    #endregion

    #region 생성자

        // ----------------------------------------------------------------------
        /// <summary>
        /// 추적 Transform과 최초 Play 전 VisualEffect 준비 동작으로 Binding을 생성한다.
        /// </summary>
        // ----------------------------------------------------------------------
        public VFXGraphBinding
        (
            in TransformBinding_Tracked _TransformBinding,
            Action<VisualEffect> prepareVFX
        )
        {
            Tracking = _TransformBinding;
            PrepareVFX = prepareVFX ??
                throw new ArgumentNullException(nameof(prepareVFX));
        }

    #endregion

    }
}
