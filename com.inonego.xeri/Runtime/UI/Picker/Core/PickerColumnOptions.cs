/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : PickerColumnOptions.cs
수정일 : 2026-11-21

# 설명
Picker table column의 layout, 정렬, 검색, 표시 정책을 한곳에 모으는 옵션 모델.
========================================================================= BLOCK_HEADER_END */

using inonego;
using inonego.Xeri;
using inonego.Xeri.UI;
using inonego.Xeri.UI.TableGrid;

namespace inonego.Xeri.UI.Picker
{
    // ============================================================
    /// <summary>
    /// Picker column text overflow 정책.
    /// </summary>
    // ============================================================
    public enum PickerColumnOverflow
    {
        Ellipsis,
        Clip,
        Wrap,
    }

    // ============================================================
    /// <summary>
    /// Picker table column 표시/동작 옵션.
    /// </summary>
    // ============================================================
    public sealed class PickerColumnOptions
    {

    #region 필드

        private const float DEFAULT_WIDTH = 120f;
        private const float DEFAULT_MIN_WIDTH = 40f;

        // ------------------------------------------------------------
        /// <summary>
        /// 열의 폭 정책이다.
        /// </summary>
        // ------------------------------------------------------------
        public readonly TableGridColumnLayout Layout;

        // ------------------------------------------------------------
        /// <summary>
        /// 정렬 가능 여부다.
        /// </summary>
        // ------------------------------------------------------------
        public readonly bool Sortable;

        // ------------------------------------------------------------
        /// <summary>
        /// 검색 대상 여부다.
        /// </summary>
        // ------------------------------------------------------------
        public readonly bool Searchable;

        // ------------------------------------------------------------
        /// <summary>
        /// 텍스트 정렬 방식이다.
        /// </summary>
        // ------------------------------------------------------------
        public readonly TableGridAlignment Alignment;

        // ------------------------------------------------------------
        /// <summary>
        /// 텍스트 넘침 처리 방식이다.
        /// </summary>
        // ------------------------------------------------------------
        public readonly PickerColumnOverflow Overflow;

        // ----------------------------------------------------------------------
        /// <summary>
        /// 목록 표에 열로 보일지 여부다. 숨긴 열도 검색 글자에는 들어간다(Searchable이면).
        /// </summary>
        // ----------------------------------------------------------------------
        public readonly bool Visible;

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// column 표시/동작 옵션을 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        public PickerColumnOptions
        (
            TableGridColumnLayout layout,
            bool sortable,
            bool searchable,
            TableGridAlignment alignment,
            PickerColumnOverflow overflow,
            bool visible
        ) : base()
        {
            Layout     = layout ?? TableGridColumnLayout.Flexible(DEFAULT_WIDTH, DEFAULT_MIN_WIDTH, 0f, 1f);
            Sortable   = sortable;
            Searchable = searchable;
            Alignment  = alignment;
            Overflow   = overflow;
            Visible    = visible;
        }

    #endregion

    #region 생성

        // ------------------------------------------------------------
        /// <summary>
        /// 고정 폭 column 옵션을 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        public static PickerColumnOptions Fixed
        (
            float width,
            bool sortable = true,
            bool searchable = true,
            TableGridAlignment alignment = TableGridAlignment.Left,
            PickerColumnOverflow overflow = PickerColumnOverflow.Ellipsis,
            bool visible = true
        )
        {
            return new PickerColumnOptions
            (
                TableGridColumnLayout.Fixed(width),
                sortable,
                searchable,
                alignment,
                overflow,
                visible
            );
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 가변 폭 column 옵션을 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        public static PickerColumnOptions Flexible
        (
            float width = DEFAULT_WIDTH,
            float minWidth = DEFAULT_MIN_WIDTH,
            float maxWidth = 0f,
            float stretchWeight = 1f,
            bool sortable = true,
            bool searchable = true,
            TableGridAlignment alignment = TableGridAlignment.Left,
            PickerColumnOverflow overflow = PickerColumnOverflow.Ellipsis,
            bool visible = true
        )
        {
            return new PickerColumnOptions
            (
                TableGridColumnLayout.Flexible(width, minWidth, maxWidth, stretchWeight),
                sortable,
                searchable,
                alignment,
                overflow,
                visible
            );
        }

    #endregion

    }
}
