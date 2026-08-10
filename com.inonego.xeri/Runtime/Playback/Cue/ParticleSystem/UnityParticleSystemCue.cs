/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : UnityParticleSystemCue.cs
수정일 : 2026-08-10

# 설명
Unity ParticleSystem Prefab을 재생하는 Visual Cue Asset을 정의한다.

# 적용 범위
재생 위치·추적 대상은 Runtime Binding이 제공한다.
Player는 Prefab의 Renderer나 Material을 생성·교체하지 않으며 사용하는 Render Pipeline 호환성은 자산이 소유한다.
========================================================================= BLOCK_HEADER_END */

using UnityEngine;

namespace inonego.Xeri.Playback
{
    // ============================================================
    /// <summary>
    /// Unity ParticleSystem Prefab 기반 Visual Cue.
    /// </summary>
    // ============================================================
    [CreateAssetMenu
    (
        fileName = "UnityParticleSystemCue",
        menuName = "Xeri/Playback/Unity Particle System Cue"
    )]
    public sealed class UnityParticleSystemCue : VisualCue
    {

    #region 필드

        // ------------------------------------------------------------
        /// <summary>
        /// 재생할 ParticleSystem Prefab Root.
        /// </summary>
        // ------------------------------------------------------------
        public ParticleSystem Prefab
        {
            get => prefab;
            set => prefab = value;
        }

        [SerializeField]
        private ParticleSystem prefab = null;

        // ------------------------------------------------------------
        /// <summary>
        /// Particle Simulation이 Unity Time Scale과 독립된 시간을 사용할지 여부.
        /// </summary>
        // ------------------------------------------------------------
        public bool UsesUnscaledTime
        {
            get => usesUnscaledTime;
            set => usesUnscaledTime = value;
        }

        [SerializeField]
        private bool usesUnscaledTime = true;

    #endregion

    }
}
