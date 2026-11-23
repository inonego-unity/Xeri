/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TableGridTsv.cs
수정일 : 2026-11-23

# 설명
TSV(탭으로 나눈 글자) 읽기·쓰기 규칙. 셀 하나를 TSV에 덧붙이고(Append), TSV를 행·열로 나눈다(Parse).

# 특이사항, 제약사항
탭·줄바꿈·큰따옴표가 든 값은 큰따옴표로 감싸고 안의 큰따옴표는 두 번 쓴다. 읽을 때도 같은 규칙이다.
감싸지 않은 값 안의 큰따옴표는 글자로 남기고, 감싼 빈 값("")도 한 셀이다.
선택·열·값 규칙을 모르는 글자 변환만 맡는다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace inonego.Xeri.UI.TableGrid
{
    // ============================================================
    /// <summary>
    /// TSV 읽기·쓰기 규칙.
    /// </summary>
    // ============================================================
    internal static class TableGridTsv
    {

    #region 필드

        private static readonly char[] quoteTriggers =
        {
            '\t', '\r', '\n', '"',
        };

    #endregion

    #region 쓰기

        // ------------------------------------------------------------
        /// <summary>
        /// 셀 하나를 덧붙인다. 첫 셀이 아니면 앞에 탭을 넣고, 특수 글자가 있으면 큰따옴표로 감싼다.
        /// </summary>
        // ------------------------------------------------------------
        public static void Append(StringBuilder builder, string value, bool first)
        {
            if (!first)
            {
                builder.Append('\t');
            }

            var quoted = value.IndexOfAny(quoteTriggers) >= 0;
            if (!quoted)
            {
                builder.Append(value);
                return;
            }

            builder.Append('"').Append(value.Replace("\"", "\"\"")).Append('"');
        }

    #endregion

    #region 읽기

        // ------------------------------------------------------------
        /// <summary>
        /// 큰따옴표로 감싼 셀을 지키며 TSV를 행·열로 나눈다.
        /// </summary>
        // ------------------------------------------------------------
        public static List<List<string>> Parse(string text)
        {
            var result = new List<List<string>>();
            if (string.IsNullOrEmpty(text))
            {
                return result;
            }

            var row = new List<string>();
            var cell = new StringBuilder();
            var quoted = false;
            var started = false;
            for (var index = 0; index < text.Length; index++)
            {
                var current = text[index];
                if (current == '"')
                {
                    if (quoted && index + 1 < text.Length && text[index + 1] == '"')
                    {
                        cell.Append('"');
                        index++;
                    }
                    else if (quoted)
                    {
                        quoted = false;
                    }
                    else if (!started)
                    {
                        quoted = true;
                    }
                    else
                    {
                        // 감싸지 않은 값 안의 큰따옴표는 글자로 남긴다.
                        cell.Append('"');
                    }

                    started = true;
                    continue;
                }

                if (!quoted && (current == '\t' || current == '\r' || current == '\n'))
                {
                    row.Add(cell.ToString());
                    cell.Clear();
                    started = false;
                    if (current != '\t')
                    {
                        result.Add(row);
                        row = new List<string>();
                        if (current == '\r' && index + 1 < text.Length && text[index + 1] == '\n')
                        {
                            index++;
                        }
                    }

                    continue;
                }

                cell.Append(current);
                started = true;
            }

            // 끝에 남은 셀을 넣는다. 감싼 빈 값("")도 한 셀이다.
            if (started || row.Count > 0 || text[text.Length - 1] == '\t')
            {
                row.Add(cell.ToString());
                result.Add(row);
            }

            return result;
        }

    #endregion

    }
}
