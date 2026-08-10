/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : UnityVFXGraphCue.cs
수정일 : 2026-08-10

# 설명
Unity VFX Graph VisualEffect Prefab을 재생하는 Visual Cue Asset을 정의한다.

# 적용 범위
재생 위치·추적 대상은 Runtime Binding이 제공한다.
Player는 Prefab 내부 VFX Graph 자산과 렌더링 설정을 변경하지 않는다.
========================================================================= BLOCK_HEADER_END */

using UnityEngine;
using UnityEngine.VFX;

namespace inonego.Xeri.Playback
{
    // ============================================================
    /// <summary>
    /// Unity VFX Graph Prefab 기반 Visual Cue.
    /// </summary>
    // ============================================================
    [CreateAssetMenu
    (
        fileName = "UnityVFXGraphCue",
        menuName = "Xeri/Playback/Unity VFX Graph Cue"
    )]
    public sealed class UnityVFXGraphCue : VisualCue
    {

    #region 필드

        // ------------------------------------------------------------
        /// <summary>
        /// 재생할 VisualEffect Prefab Root.
        /// </summary>
        // ------------------------------------------------------------
        public VisualEffect Prefab
        {
            get => prefab;
            set => prefab = value;
        }

        [SerializeField]
        private VisualEffect prefab = null;

    #endregion

    }
}
