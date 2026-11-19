/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : IReadOnlyValueRegistry.cs
수정일 : 2026-11-19

# 설명
읽기 전용 ValueRegistry 인터페이스. IValueLookup의 값 조회에 변경 관찰을 더한다.
========================================================================= BLOCK_HEADER_END */

using System;

namespace inonego.Xeri.Serializable
{
    // ============================================================
    /// <summary>
    /// 읽기 전용 ValueRegistry 인터페이스.
    /// </summary>
    // ============================================================
    public interface IReadOnlyValueRegistry : IValueLookup
    {

    #region 이벤트

        // ------------------------------------------------------------
        /// <summary>
        /// Registry의 실제 값 구성이 변경된 뒤 발생한다.
        /// </summary>
        // ------------------------------------------------------------
        public event Action OnChange;

    #endregion

    }
}
