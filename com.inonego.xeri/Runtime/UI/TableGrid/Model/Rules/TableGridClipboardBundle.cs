/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TableGridClipboardBundle.cs
수정일 : 2026-11-23

# 설명
클립보드의 셀(TableGridClipCell)과 값 묶음(TableGridClipboardBundle)을 정의한다.
값 묶음은 마지막 복사의 TSV·셀 값·복사한 자리를 보관하고, 밖에서 온 글자는 글자만 가진 값 묶음(FromText)으로 읽는다.

# 특이사항, 제약사항
값 묶음은 세션 메모리라 에디터를 다시 열면 표시 글자 경로로 붙여넣는다.
글자만 있는 셀은 값 규칙 TryParse로 읽고, 값이 있는 셀은 값 규칙 검증을 거쳐 복사해 쓴다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

namespace inonego.Xeri.UI.TableGrid
{
    // ============================================================
    /// <summary>
    /// 클립보드의 셀 하나. 표시 글자와(있으면) 값.
    /// </summary>
    // ============================================================
    internal readonly struct TableGridClipCell
    {
        // ------------------------------------------------------------
        /// <summary>
        /// 글자만 있는 셀을 만든다(밖에서 온 글자).
        /// </summary>
        // ------------------------------------------------------------
        public TableGridClipCell(string text)
        {
            Text = text ?? string.Empty;
            HasValue = false;
            Value = null;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 글자와 값이 있는 셀을 만든다(표 안에서 복사).
        /// </summary>
        // ------------------------------------------------------------
        public TableGridClipCell(string text, object value)
        {
            Text = text ?? string.Empty;
            HasValue = true;
            Value = value;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 값 규칙 ToText로 만든 표시 글자.
        /// </summary>
        // ------------------------------------------------------------
        public string Text { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 값이 있는지 여부.
        /// </summary>
        // ------------------------------------------------------------
        public bool HasValue { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 복사할 때 값 규칙 CopyValue로 복사한 값.
        /// </summary>
        // ------------------------------------------------------------
        public object Value { get; }
    }

    // ======================================================================
    /// <summary>
    /// <br/> 클립보드 내용의 값 묶음.
    /// <br/> 표 안에서 복사했으면 시스템 클립보드에 넣은 TSV와 셀 값, 복사한 자리를 함께 보관한다.
    /// <br/> 밖에서 온 글자는 글자만 가진 셀로 읽은 묶음이다(FromText).
    /// </summary>
    // ======================================================================
    internal sealed class TableGridClipboardBundle
    {
        // ------------------------------------------------------------
        /// <summary>
        /// <br/> 글자와 셀 값 행, 복사한 자리로 값 묶음을 만든다.
        /// <br/> Rows[i][j]는 sourceRowIDs[i] 행의
        /// <br/> sourceColumnIDs[j] 열 값이다.
        /// </summary>
        // ------------------------------------------------------------
        public TableGridClipboardBundle
        (
            string text, IReadOnlyList<IReadOnlyList<TableGridClipCell>> rows,
            TableGridSelectionKind sourceKind, IReadOnlyList<string> sourceRowIDs, IReadOnlyList<string> sourceColumnIDs
        )
        {
            Text = text ?? string.Empty;
            Rows = rows ?? throw new ArgumentNullException(nameof(rows));
            SourceKind = sourceKind;
            SourceRowIDs = sourceRowIDs ?? throw new ArgumentNullException(nameof(sourceRowIDs));
            SourceColumnIDs = sourceColumnIDs ?? throw new ArgumentNullException(nameof(sourceColumnIDs));
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> 밖에서 온 TSV 글자를 글자만 있는 셀의 값 묶음으로 읽는다.
        /// <br/> 복사한 자리가 없으므로 선택 종류는 None이고 행·열 ID는 비어 있다.
        /// </summary>
        // ----------------------------------------------------------------------
        public static TableGridClipboardBundle FromText(string text)
        {
            var rows = new List<IReadOnlyList<TableGridClipCell>>();
            foreach (var row in TableGridTsv.Parse(text))
            {
                var line = new List<TableGridClipCell>(row.Count);
                foreach (var value in row)
                {
                    line.Add(new TableGridClipCell(value));
                }

                rows.Add(line);
            }

            return new TableGridClipboardBundle
            (
                text, rows, TableGridSelectionKind.None, Array.Empty<string>(), Array.Empty<string>()
            );
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 시스템 클립보드에 넣은 TSV.
        /// </summary>
        // ------------------------------------------------------------
        public string Text { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 복사한 셀(행·열 순서). 열 이름 줄은 들지 않는다.
        /// </summary>
        // ------------------------------------------------------------
        public IReadOnlyList<IReadOnlyList<TableGridClipCell>> Rows { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 복사할 때의 선택 종류(행·셀·열).
        /// </summary>
        // ------------------------------------------------------------
        public TableGridSelectionKind SourceKind { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 복사한 행 ID(Rows 순서).
        /// </summary>
        // ------------------------------------------------------------
        public IReadOnlyList<string> SourceRowIDs { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 복사한 열 ID(Rows 각 행의 값 순서).
        /// </summary>
        // ------------------------------------------------------------
        public IReadOnlyList<string> SourceColumnIDs { get; }

        // ----------------------------------------------------------------------
        /// <summary>
        /// 클립보드 글자가 이 값 묶음의 TSV와 같은지 확인한다(줄바꿈 차이는 무시).
        /// </summary>
        // ----------------------------------------------------------------------
        public bool Matches(string text)
        {
            return text != null && string.Equals(Normalize(text), Normalize(Text), StringComparison.Ordinal);
        }

        private static string Normalize(string text)
        {
            return text.Replace("\r\n", "\n");
        }
    }
}
