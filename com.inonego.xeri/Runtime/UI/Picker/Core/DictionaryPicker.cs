/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : DictionaryPicker.cs
수정일 : 2026-06-07

# 설명
IReadOnlyDictionary 기반 picker spec 구성과 entry 변환을 돕는 facade.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

namespace inonego.Xeri.UI.Picker
{
   // ============================================================
   /// <summary>
   /// IReadOnlyDictionary 기반 picker spec 구성과 entry 변환을 돕는 facade.
   /// </summary>
   // ============================================================
   public static class DictionaryPicker
   {

   #region 생성

      // ------------------------------------------------------------
      /// <summary>
      /// dictionary 기본 선택값을 반환하는 picker builder를 생성한다.
      /// </summary>
      // ------------------------------------------------------------
      public static PickerSpecBuilder<KeyValuePair<TKey, TValue>, TKey> Spec<TKey, TValue>(string title)
      {
         return BaseSpec<TKey, TValue, TKey>(title, entry => entry.Key);
      }

      // ------------------------------------------------------------
      /// <summary>
      /// dictionary value를 선택값으로 반환하는 picker builder를 생성한다.
      /// </summary>
      // ------------------------------------------------------------
      public static PickerSpecBuilder<KeyValuePair<TKey, TValue>, TValue> ValueSpec<TKey, TValue>(string title)
      {
         return BaseSpec<TKey, TValue, TValue>(title, entry => entry.Value);
      }

      // ------------------------------------------------------------
      /// <summary>
      /// dictionary pair를 선택값으로 반환하는 picker builder를 생성한다.
      /// </summary>
      // ------------------------------------------------------------
      public static PickerSpecBuilder<KeyValuePair<TKey, TValue>, KeyValuePair<TKey, TValue>> PairSpec<TKey, TValue>
      (
         string title
      )
      {
         return BaseSpec<TKey, TValue, KeyValuePair<TKey, TValue>>(title, entry => entry);
      }

      // ----------------------------------------------------------------------
      /// <summary>
      /// <br/> dictionary 표시 규칙을 공유하는 picker builder를 생성한다.
      /// <br/> 선택값만 호출자가 지정한 getter로 분리한다.
      /// </summary>
      // ----------------------------------------------------------------------
      private static PickerSpecBuilder<KeyValuePair<TKey, TValue>, TSelected> BaseSpec<TKey, TValue, TSelected>
      (
         string title,
         Func<KeyValuePair<TKey, TValue>, TSelected> valueGetter
      )
      {
         return PickerSpec<KeyValuePair<TKey, TValue>, TSelected>
            .Create(title)
            .Value(valueGetter)
            .Preview(false)
            .Label(entry => ToDisplayText(entry.Key))
            .Desc(entry => ToDisplayText(entry.Value))
            .DefaultPreviewTags("Key", "Value")
            .Tag("Key", entry => ToDisplayText(entry.Key))
            .Tag("Value", entry => ToDisplayText(entry.Value))
            .Column("Key", entry => entry.Key)
            .Column("Value", entry => entry.Value);
      }

   #endregion

   #region 메서드

      // ------------------------------------------------------------
      /// <summary>
      /// dictionary를 picker에 전달 가능한 list entry로 복사한다.
      /// </summary>
      // ------------------------------------------------------------
      public static IReadOnlyList<KeyValuePair<TKey, TValue>> Entries<TKey, TValue>
      (
         IReadOnlyDictionary<TKey, TValue> dictionary
      )
      {
         if (dictionary == null || dictionary.Count == 0) return Array.Empty<KeyValuePair<TKey, TValue>>();

         var result = new List<KeyValuePair<TKey, TValue>>(dictionary.Count);
         foreach (var entry in dictionary)
         {
            result.Add(entry);
         }

         return result;
      }

      // ------------------------------------------------------------
      /// <summary>
      /// null 값을 빈 문자열로 표시하기 위한 공통 변환을 수행한다.
      /// </summary>
      // ------------------------------------------------------------
      private static string ToDisplayText<T>(T value)
      {
         return value?.ToString() ?? string.Empty;
      }

   #endregion

   }
}
