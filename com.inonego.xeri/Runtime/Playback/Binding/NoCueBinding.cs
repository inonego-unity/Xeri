/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : NoCueBinding.cs
수정일 : 2026-08-10

# 설명
추가 Runtime Binding이 필요 없는 Cue 실행을 동일한 Generic Player 계약으로 표현한다.
========================================================================= BLOCK_HEADER_END */

namespace inonego.Xeri.Playback
{
    // ============================================================
    /// <summary>
    /// 별도 Runtime Binding이 없는 Cue 실행 값.
    /// </summary>
    // ============================================================
    public readonly struct NoCueBinding : ICueBinding
    {
        
    #region 필드

        // ------------------------------------------------------------
        /// <summary>
        /// 상태 없는 No Cue Binding의 공용 기본값.
        /// </summary>
        // ------------------------------------------------------------
        public static readonly NoCueBinding Default = default;

    #endregion

    }
}
