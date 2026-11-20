/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : SearchTextUtility.cs
수정일 : 2026-09-24

# 설명
표시 항목의 searchable text 조립과 case-insensitive 검색 판정을 제공하는 공통 유틸리티.
Picker와 Collection Editor가 동일한 검색 규칙을 재사용할 수 있게 한다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Text;

namespace inonego.Xeri.UI
{
    // ============================================================
    /// <summary>
    /// UI 목록 검색 문자열 공통 유틸리티.
    /// </summary>
    // ============================================================
    internal static class SearchTextUtility
    {

    #region 메서드

        // ------------------------------------------------------------
        /// <summary>
        /// 비어 있지 않은 값을 공백으로 구분해 검색 문자열 builder에 추가한다.
        /// </summary>
        // ------------------------------------------------------------
        public static void Append(StringBuilder builder, string value)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (string.IsNullOrEmpty(value))
            {
                return;
            }

            if (builder.Length > 0)
            {
                builder.Append(' ');
            }

            builder.Append(value);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// indexed text가 입력 검색어와 일치하는지 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public static bool Matches(string indexedText, string searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText))
            {
                return true;
            }

            return (indexedText ?? string.Empty).IndexOf
            (
                searchText,
                StringComparison.OrdinalIgnoreCase
            ) >= 0;
        }

    #endregion

    }
}
