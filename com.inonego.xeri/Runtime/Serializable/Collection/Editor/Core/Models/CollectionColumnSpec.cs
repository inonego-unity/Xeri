/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : CollectionColumnSpec.cs
수정일 : 2026-11-18

# 설명
Collection Editor table column의 ID, 표시명과 폭 정책(TableColumnLayout)을 정의한다.
========================================================================= BLOCK_HEADER_END */

using System;

using inonego.Xeri.UI;

namespace inonego.Xeri.Serializable.Editor
{
    // ============================================================
    /// <summary>
    /// Collection Editor table column 정의.
    /// </summary>
    // ============================================================
    internal sealed class CollectionColumnSpec
    {

    #region 프로퍼티

        // ------------------------------------------------------------
        /// <summary>
        /// 관리 참조의 식별자를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public string ID { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 열의 헤더에 표시할 문자열을 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public string Header { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 열의 기본·최소·최대 폭과 가변 여부, 가중치를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public TableColumnLayout Layout { get; }

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// CollectionColumnSpec 인스턴스를 초기화한다.
        /// </summary>
        // ------------------------------------------------------------
        public CollectionColumnSpec(string id, string header, TableColumnLayout layout)
        {
            ID = id ?? throw new ArgumentNullException(nameof(id));
            Header = header ?? string.Empty;
            Layout = layout ?? throw new ArgumentNullException(nameof(layout));
        }

    #endregion

    }
}
