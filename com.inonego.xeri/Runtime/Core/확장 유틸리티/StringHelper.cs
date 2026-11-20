/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : StringHelper.cs
수정일 : 2026-05-03

# 설명
문자열 배열 정제 및 숫자 → 문자열 변환 확장 메서드 모음.
TrimAndRemoveInvalid, ToOrdinal, ToRoman을 제공한다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Text;

namespace inonego.Xeri
{
    // ============================================================
    /// <summary>
    /// 문자열 확장 메서드 정적 헬퍼.
    /// </summary>
    // ============================================================
    public static class StringHelper
    {

    #region 필드

        private static readonly int[]    _romanValues   = { 1000, 900, 500, 400, 100, 90, 50, 40, 10, 9, 5, 4, 1 };
        private static readonly string[] _romanNumerals = { "M", "CM", "D", "CD", "C", "XC", "L", "XL", "X", "IX", "V", "IV", "I" };

    #endregion

    #region 정제

        // -----------------------------------------------------------------------
        /// <summary>
        /// <br/> 문자열 배열의 각 요소를 Trim하고 빈 요소를 제거한다.
        /// <br/> 원본 배열을 in-place로 수정하며, 유효 요소 수를 length로 반환한다.
        /// </summary>
        // -----------------------------------------------------------------------
        public static string[] TrimAndRemoveInvalid(this string[] source, out int length)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            int writeIndex = 0;

            for (int i = 0; i < source.Length; i++)
            {
                string entry = source[i]?.Trim();

                if (!string.IsNullOrEmpty(entry))
                {
                    source[writeIndex] = entry;
                    writeIndex++;
                }
            }

            for (int i = writeIndex; i < source.Length; i++)
            {
                source[i] = null;
            }

            length = writeIndex;

            return source;
        }

    #endregion

    #region 변환

        // ------------------------------------------------------------
        /// <summary>
        /// 정수를 서수형(1st, 2nd, 3rd …)으로 변환한다.
        /// </summary>
        // ------------------------------------------------------------
        public static string ToOrdinal(this int number)
        {
            if (number <= 0) return number.ToString();

            int lastDigit    = number % 10;
            int lastTwoDigit = number % 100;

            // 11·12·13은 항상 th (eleven-th, twelve-th, thirteen-th)
            if (11 <= lastTwoDigit && lastTwoDigit <= 13) return number + "th";

            return lastDigit switch
            {
                1 => number + "st",
                2 => number + "nd",
                3 => number + "rd",
                _ => number + "th"
            };
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 정수를 로마 숫자로 변환한다. 유효 범위는 1–3999이다.
        /// </summary>
        // ------------------------------------------------------------
        public static string ToRoman(this int number)
        {
            if (number <= 0 || number > 3999) return number.ToString();

            var result = new StringBuilder();

            for (int i = 0; i < _romanValues.Length; i++)
            {
                int count = number / _romanValues[i];

                for (int j = 0; j < count; j++)
                {
                    result.Append(_romanNumerals[i]);
                }

                number %= _romanValues[i];
            }

            return result.ToString();
        }

    #endregion

    }
}
