/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : ListPicker.cs
수정일 : 2026-06-17

# 설명
IReadOnlyList 기반 picker spec 구성을 돕는 facade.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

namespace inonego.Xeri.UI.Picker
{
   // ============================================================
   /// <summary>
   /// IReadOnlyList 기반 picker spec 구성을 돕는 facade.
   /// </summary>
   // ============================================================
   public static class ListPicker
   {

   #region 생성

      // ------------------------------------------------------------
      /// <summary>
      /// entry 자체를 선택값으로 반환하는 list picker builder를 생성한다.
      /// </summary>
      // ------------------------------------------------------------
      public static PickerSpecBuilder<TEntry, TEntry> Spec<TEntry>(string title)
      {
         return Spec<TEntry, TEntry>(title, entry => entry);
      }

      // ------------------------------------------------------------
      /// <summary>
      /// entry에서 선택값을 추출하는 list picker builder를 생성한다.
      /// </summary>
      // ------------------------------------------------------------
      public static PickerSpecBuilder<TEntry, TValue> Spec<TEntry, TValue>
      (
         string title,
         Func<TEntry, TValue> valueGetter
      )
      {
         if (valueGetter == null)
         {
            throw new ArgumentNullException(nameof(valueGetter));
         }

         return PickerSpec<TEntry, TValue>
            .Create(title)
            .Value(valueGetter)
            .Preview(false)
            .Label(DefaultLabel)
            .Desc(DefaultDesc)
            .DefaultPreviewTags("Value")
            .Tag("Value", DefaultLabel)
            .Column("Value", entry => entry, PickerColumnOptions.Flexible());
      }

   #endregion

   #region 메서드

      // ------------------------------------------------------------
      /// <summary>
      /// null entry를 빈 문자열로 표시하기 위한 기본 label을 반환한다.
      /// </summary>
      // ------------------------------------------------------------
      private static string DefaultLabel<TEntry>(TEntry entry)
      {
         return entry?.ToString() ?? string.Empty;
      }

      // ------------------------------------------------------------
      /// <summary>
      /// list picker 기본 설명은 도메인 의미를 갖지 않도록 비워 둔다.
      /// </summary>
      // ------------------------------------------------------------
      private static string DefaultDesc<TEntry>(TEntry _)
      {
         return string.Empty;
      }

   #endregion

   }
}
