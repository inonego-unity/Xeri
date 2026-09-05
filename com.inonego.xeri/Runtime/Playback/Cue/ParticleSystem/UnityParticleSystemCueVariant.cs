/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : UnityParticleSystemCueVariant.cs
수정일 : 2026-09-05

# 설명
Unity ParticleSystem Prefab과 재생 설정을 하나의 Visual Cue Variant로 묶는다.
========================================================================= BLOCK_HEADER_END */

using System;

using UnityEngine;

namespace inonego.Xeri.Playback
{
    // ============================================================
    /// <summary>
    /// Unity ParticleSystem 기반 Visual Cue Variant.
    /// </summary>
    // ============================================================
    [Serializable]
    public sealed class UnityParticleSystemCueVariant
    {

    #region 필드

        // ------------------------------------------------------------
        /// <summary>
        /// 재생할 ParticleSystem Prefab.
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
        /// ParticleSystem이 unscaled time으로 진행될지 여부.
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
