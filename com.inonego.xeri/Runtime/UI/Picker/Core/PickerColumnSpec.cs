/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : PickerColumnSpec.cs
수정일 : 2026-06-06

# 설명
Picker table column의 header, width, 정렬 가능 여부, 값 생성 규칙.
========================================================================= BLOCK_HEADER_END */

using System;

namespace inonego.Xeri.UI.Picker
{
   // ============================================================
   /// <summary>
   /// Picker table column 정의.
   /// </summary>
   // ============================================================
   [Serializable]
   public sealed class PickerColumnSpec<TEntry>
   {

   #region 필드

      // ------------------------------------------------------------
      /// <summary>
      /// column 식별자.
      /// </summary>
      // ------------------------------------------------------------
      public readonly string ID;

      // ------------------------------------------------------------
      /// <summary>
      /// column header.
      /// </summary>
      // ------------------------------------------------------------
      public readonly string Header;

      // ------------------------------------------------------------
      /// <summary>
      /// column 너비.
      /// </summary>
      // ------------------------------------------------------------
      public readonly float Width;

      // ------------------------------------------------------------
      /// <summary>
      /// 정렬 가능 여부.
      /// </summary>
      // ------------------------------------------------------------
      public readonly bool Sortable;

      private readonly Func<TEntry, object> valueGetter;

   #endregion

   #region 생성자

      // ------------------------------------------------------------
      /// <summary>
      /// column 정의를 생성한다.
      /// </summary>
      // ------------------------------------------------------------
      public PickerColumnSpec
      (
         string id,
         string header,
         float width,
         bool sortable,
         Func<TEntry, object> valueGetter
      ) : base()
      {
         ID               = string.IsNullOrEmpty(id) ? header ?? string.Empty : id;
         Header           = header ?? string.Empty;
         Width            = width;
         Sortable         = sortable;
         this.valueGetter = valueGetter ?? (_ => null);
      }

   #endregion

   #region 메서드

      // ------------------------------------------------------------
      /// <summary>
      /// 원본 entry에서 column 값을 생성한다.
      /// </summary>
      // ------------------------------------------------------------
      public PickerColumnValue CreateValue(TEntry entry)
      {
         var value = valueGetter(entry);

         return new PickerColumnValue(ID, Header, value);
      }

   #endregion

   }
}
