/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : VisualCueAsset.cs
수정일 : 2026-09-05

# 설명
Unity Asset으로 authoring한 Visual Cue 구성에서 독립적인 runtime Visual Cue를 생성하는 공통 계약을 선언한다.
========================================================================= BLOCK_HEADER_END */

namespace inonego.Xeri.Playback
{
    // ============================================================
    /// <summary>
    /// Unity Asset으로 보관되는 Visual Cue authoring 단위.
    /// </summary>
    // ============================================================
    public abstract class VisualCueAsset : VariantCueAsset
    {

    #region 생성

        // ------------------------------------------------------------
        /// <summary>
        /// 이 Asset의 authoring 데이터로 독립적인 runtime Visual Cue를 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        public abstract VisualCue CreateCue();

    #endregion

    }
}
