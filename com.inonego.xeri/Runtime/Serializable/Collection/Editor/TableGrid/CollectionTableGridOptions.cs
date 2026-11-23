/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : CollectionTableGridOptions.cs
수정일 : 2026-11-21

# 설명
컬렉션 표(CollectionTableGrid)의 표시 행 수와 제목처럼 편집 규칙과 무관한 표시 설정을 전달한다.

# 특이사항, 제약사항
부모 높이를 채우는 배치는 이 설정이 아니라 호스트 USS의 --xeri-collection-fill 값으로 정한다.
========================================================================= BLOCK_HEADER_END */

using System;

namespace inonego.Xeri.Serializable.Editor
{
    // ============================================================
    /// <summary>
    /// 컬렉션 표의 표시 설정.
    /// </summary>
    // ============================================================
    internal readonly struct CollectionTableGridOptions
    {

    #region 상수

        // ------------------------------------------------------------
        /// <summary>
        /// 별도 지정이 없을 때 처음 표시할 행 수다.
        /// </summary>
        // ------------------------------------------------------------
        public const int DefaultVisibleRows = 5;

        // ------------------------------------------------------------
        /// <summary>
        /// 사용자가 늘릴 수 있는 최대 표시 행 수다.
        /// </summary>
        // ------------------------------------------------------------
        public const int MaxVisibleRows = 50;

    #endregion

    #region 프로퍼티

        // ------------------------------------------------------------
        /// <summary>
        /// 처음 표시할 행 수를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public int VisibleRows { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 직렬화 이름 대신 표시할 제목을 반환한다. 없으면 null이다.
        /// </summary>
        // ------------------------------------------------------------
        public string Label { get; }

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// 표시 행 수와 제목으로 설정을 만든다.
        /// </summary>
        // ------------------------------------------------------------
        public CollectionTableGridOptions(int visibleRows, string label)
        {
            VisibleRows = Math.Clamp(visibleRows, 1, MaxVisibleRows);
            Label = string.IsNullOrWhiteSpace(label) ? null : label;
        }

    #endregion

    #region 생성

        // ------------------------------------------------------------
        /// <summary>
        /// 기본 표시 설정을 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public static CollectionTableGridOptions Default()
        {
            return new CollectionTableGridOptions(DefaultVisibleRows, null);
        }

    #endregion

    }
}
