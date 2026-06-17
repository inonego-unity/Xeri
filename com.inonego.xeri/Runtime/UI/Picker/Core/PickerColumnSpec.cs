/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : PickerColumnSpec.cs
수정일 : 2026-06-17

# 설명
Picker table column의 header, 표시/동작 옵션, 값 생성 규칙.
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
      /// column 표시/동작 옵션.
      /// </summary>
      // ------------------------------------------------------------
      public readonly PickerColumnOptions Options;

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
         PickerColumnOptions options,
         Func<TEntry, object> valueGetter
      ) : base()
      {
         ID               = string.IsNullOrEmpty(id) ? header ?? string.Empty : id;
         Header           = header ?? string.Empty;
         Options          = options ?? PickerColumnOptions.Flexible();
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

         return new PickerColumnValue(ID, Header, value, Options.Searchable);
      }

   #endregion

   }
}
