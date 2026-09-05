/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : UnityVFXGraphCueAsset.cs
수정일 : 2026-09-05

# 설명
하나의 의미적 Visual Cue가 사용할 VFX Graph Variant들과 선택 정책을 Unity Asset으로 authoring한다.
Runtime 선택 이력과 Pool 상태는 CreateCue로 생성한 UnityVFXGraphCue가 소유한다.
========================================================================= BLOCK_HEADER_END */

using System;

using UnityEngine;

namespace inonego.Xeri.Playback
{
    // ============================================================
    /// <summary>
    /// Unity VFX Graph Variant를 authoring하는 Visual Cue Asset.
    /// </summary>
    // ============================================================
    [CreateAssetMenu
    (
        fileName = "UnityVFXGraphCue",
        menuName = "Xeri/Playback/Unity VFX Graph Cue"
    )]
    public sealed class UnityVFXGraphCueAsset : VisualCueAsset
    {

    #region 필드

        // ------------------------------------------------------------
        /// <summary>
        /// Authoring된 Variant 개수.
        /// </summary>
        // ------------------------------------------------------------
        public int VariantCount => variants != null ? variants.Length : 0;

        [SerializeField]
        private UnityVFXGraphCueVariant[] variants =
            Array.Empty<UnityVFXGraphCueVariant>();

    #endregion

    #region 생성

        // ------------------------------------------------------------
        /// <summary>
        /// 이 Asset의 Variant와 선택 정책을 사용하는 runtime Cue를 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        public override VisualCue CreateCue()
        {
            return new UnityVFXGraphCue(this);
        }

    #endregion

    #region Variant 조회

        // ------------------------------------------------------------
        /// <summary>
        /// 지정 인덱스의 authoring Variant를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public UnityVFXGraphCueVariant GetVariant(int index)
        {
            if (index < 0 || index >= VariantCount)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            return variants[index];
        }

    #endregion

    #region Unity 이벤트

        // ------------------------------------------------------------
        /// <summary>
        /// Inspector에서 null Variant 배열이 남지 않도록 정규화한다.
        /// </summary>
        // ------------------------------------------------------------
        private void OnValidate()
        {
            variants ??= Array.Empty<UnityVFXGraphCueVariant>();
        }

    #endregion

    }
}
