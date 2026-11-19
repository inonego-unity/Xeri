/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : CollectionDraft.cs
수정일 : 2026-11-19

# 설명
확정 전에 검증해야 하는 셀 값(키·순서, 새 행의 값)을 모은다.
원본이 바뀌었는지는 초안을 편집하는 창이 확인한다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

using inonego;
using inonego.Xeri;

namespace inonego.Xeri.Serializable.Editor
{
    // ============================================================
    /// <summary>
    /// 확정 전 셀 값 모음.
    /// </summary>
    // ============================================================
    internal sealed class CollectionDraft
    {

    #region 필드

        // ------------------------------------------------------------
        /// <summary>
        /// 기존 행 인덱스를 반환하며 새 행이면 null이다.
        /// </summary>
        // ------------------------------------------------------------
        public int? SourceIndex { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 열 식별자별 초안 값을 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public IReadOnlyDictionary<string, object> Values => values;
        private readonly Dictionary<string, object> values = new(StringComparer.Ordinal);

    #endregion

    #region 생성

        private CollectionDraft(int? sourceIndex)
        {
            SourceIndex = sourceIndex;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 새 행 초안을 만든다.
        /// </summary>
        // ------------------------------------------------------------
        public static CollectionDraft ForNewRow()
        {
            return new CollectionDraft(null);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 기존 행 초안을 만든다.
        /// </summary>
        // ------------------------------------------------------------
        public static CollectionDraft ForRow(int sourceIndex)
        {
            return new CollectionDraft(sourceIndex);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 같은 대상과 값을 가진 복사본을 만든다.
        /// </summary>
        // ------------------------------------------------------------
        public CollectionDraft Clone()
        {
            var copy = new CollectionDraft(SourceIndex);
            foreach (var pair in values)
            {
                copy.values[pair.Key] = pair.Value;
            }

            return copy;
        }

    #endregion

    #region 값

        // ------------------------------------------------------------
        /// <summary>
        /// 열의 초안 값을 정한다.
        /// </summary>
        // ------------------------------------------------------------
        public CollectionDraft Set(string columnID, object value)
        {
            values[columnID ?? throw new ArgumentNullException(nameof(columnID))] = value;
            return this;
        }

    #endregion

    }
}
