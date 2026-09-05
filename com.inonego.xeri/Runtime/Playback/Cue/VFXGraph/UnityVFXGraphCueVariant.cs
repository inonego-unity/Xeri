/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : UnityVFXGraphCueVariant.cs
수정일 : 2026-09-05

# 설명
Unity VisualEffect Prefab을 하나의 VFX Graph Cue Variant로 묶는다.
========================================================================= BLOCK_HEADER_END */

using System;

using UnityEngine;
using UnityEngine.VFX;

namespace inonego.Xeri.Playback
{
    // ============================================================
    /// <summary>
    /// Unity VFX Graph 기반 Visual Cue Variant.
    /// </summary>
    // ============================================================
    [Serializable]
    public sealed class UnityVFXGraphCueVariant
    {

    #region 필드

        // ------------------------------------------------------------
        /// <summary>
        /// 재생할 VisualEffect Prefab.
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
