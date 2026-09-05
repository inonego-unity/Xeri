/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : VariantCueAsset.cs
수정일 : 2026-09-05

# 설명
Variant 기반 Cue가 공통으로 사용하는 선택 정책을 Unity Asset authoring 데이터로 정의한다.
선택 이력은 Asset에 저장하지 않고 생성된 runtime Cue가 소유한다.
========================================================================= BLOCK_HEADER_END */

using UnityEngine;

namespace inonego.Xeri.Playback
{
    // ============================================================
    /// <summary>
    /// Variant 선택 정책을 authoring하는 Cue Asset의 공통 기반.
    /// </summary>
    // ============================================================
    public abstract class VariantCueAsset : ScriptableObject
    {

    #region 필드

        // ------------------------------------------------------------
        /// <summary>
        /// 직전 선택 Variant를 다음 무작위 선택에서 제외할지 여부.
        /// </summary>
        // ------------------------------------------------------------
        public bool ExcludePrevious => excludePrevious;

        [SerializeField]
        private bool excludePrevious = false;

    #endregion

    }
}
