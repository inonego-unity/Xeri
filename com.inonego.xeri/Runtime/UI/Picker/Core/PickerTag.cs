/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : PickerTag.cs
수정일 : 2026-09-17

# 설명
Picker entry와 preview에 표시할 짧은 key-value 태그 모델.
========================================================================= BLOCK_HEADER_END */

namespace inonego.Xeri.UI.Picker
{
    // ============================================================
    /// <summary>
    /// Picker entry와 preview에 표시할 짧은 key-value 태그 모델.
    /// </summary>
    // ============================================================
    public readonly struct PickerTag
    {

    #region 필드

        // ------------------------------------------------------------
        /// <summary>
        /// 태그 이름.
        /// </summary>
        // ------------------------------------------------------------
        public readonly string Label;

        // ------------------------------------------------------------
        /// <summary>
        /// 태그 값.
        /// </summary>
        // ------------------------------------------------------------
        public readonly string Value;

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// 태그 값을 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        public PickerTag(string label, string value) : this()
        {
            Label = label ?? string.Empty;
            Value = value ?? string.Empty;
        }

    #endregion

    }
}
