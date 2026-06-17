/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : PickerColumnOptions.cs
수정일 : 2026-06-17

# 설명
Picker table column의 layout, 정렬, 검색, 표시 정책을 한곳에 모으는 옵션 모델.
========================================================================= BLOCK_HEADER_END */

using System;

namespace inonego.Xeri.UI.Picker
{
   // ============================================================
   /// <summary>
   /// Picker column text 정렬.
   /// </summary>
   // ============================================================
   public enum PickerColumnAlignment
   {
      Left,
      Center,
      Right,
   }

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
   /// Picker column 표시 여부.
   /// </summary>
   // ============================================================
   public enum PickerColumnVisibility
   {
      Visible,
      Hidden,
   }

   // ============================================================
   /// <summary>
   /// Picker table column 표시/동작 옵션.
   /// </summary>
   // ============================================================
   [Serializable]
   public sealed class PickerColumnOptions
   {

   #region 필드

      public readonly PickerColumnLayout Layout;
      public readonly bool Sortable;
      public readonly bool Searchable;
      public readonly PickerColumnAlignment Alignment;
      public readonly PickerColumnOverflow Overflow;
      public readonly PickerColumnVisibility Visibility;

   #endregion

   #region 생성자

      // ------------------------------------------------------------
      /// <summary>
      /// column 표시/동작 옵션을 생성한다.
      /// </summary>
      // ------------------------------------------------------------
      public PickerColumnOptions
      (
         PickerColumnLayout layout,
         bool sortable,
         bool searchable,
         PickerColumnAlignment alignment,
         PickerColumnOverflow overflow,
         PickerColumnVisibility visibility
      ) : base()
      {
         Layout     = layout ?? PickerColumnLayout.Flexible(120f, 40f, 0f, 1f);
         Sortable   = sortable;
         Searchable = searchable;
         Alignment  = alignment;
         Overflow   = overflow;
         Visibility = visibility;
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
         PickerColumnAlignment alignment = PickerColumnAlignment.Left,
         PickerColumnOverflow overflow = PickerColumnOverflow.Ellipsis,
         PickerColumnVisibility visibility = PickerColumnVisibility.Visible
      )
      {
         return new PickerColumnOptions
         (
            PickerColumnLayout.Fixed(width),
            sortable,
            searchable,
            alignment,
            overflow,
            visibility
         );
      }

      // ------------------------------------------------------------
      /// <summary>
      /// 가변 폭 column 옵션을 생성한다.
      /// </summary>
      // ------------------------------------------------------------
      public static PickerColumnOptions Flexible
      (
         float width = 120f,
         float minWidth = 40f,
         float maxWidth = 0f,
         float stretchWeight = 1f,
         bool sortable = true,
         bool searchable = true,
         PickerColumnAlignment alignment = PickerColumnAlignment.Left,
         PickerColumnOverflow overflow = PickerColumnOverflow.Ellipsis,
         PickerColumnVisibility visibility = PickerColumnVisibility.Visible
      )
      {
         return new PickerColumnOptions
         (
            PickerColumnLayout.Flexible(width, minWidth, maxWidth, stretchWeight),
            sortable,
            searchable,
            alignment,
            overflow,
            visibility
         );
      }

   #endregion

   }
}
