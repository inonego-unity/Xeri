/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : PickerSpec.cs
수정일 : 2026-06-07

# 설명
Picker가 entry를 표시하고 선택값을 만들기 위해 사용하는 불변 설정.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace inonego.Xeri.UI.Picker
{
   // ============================================================
   /// <summary>
   /// Picker가 entry를 표시하고 선택값을 만들기 위해 사용하는 불변 설정.
   /// </summary>
   // ============================================================
   [Serializable]
   public sealed class PickerSpec<TEntry, TValue>
   {

   #region 필드

      // ------------------------------------------------------------
      /// <summary>
      /// picker 제목.
      /// </summary>
      // ------------------------------------------------------------
      public readonly string Title;

      // ------------------------------------------------------------
      /// <summary>
      /// column 정의 목록.
      /// </summary>
      // ------------------------------------------------------------
      public readonly IReadOnlyList<PickerColumnSpec<TEntry>> Columns;

      // ------------------------------------------------------------
      /// <summary>
      /// filter 정의 목록.
      /// </summary>
      // ------------------------------------------------------------
      public readonly IReadOnlyList<PickerFilterSpec<TEntry, TValue>> Filters;

      // ------------------------------------------------------------
      /// <summary>
      /// 선택 없음 preview에 표시할 기본 tag label 목록.
      /// </summary>
      // ------------------------------------------------------------
      public readonly IReadOnlyList<string> DefaultPreviewTags;

      // ------------------------------------------------------------
      /// <summary>
      /// view 전용 표시 옵션.
      /// </summary>
      // ------------------------------------------------------------
      public readonly PickerViewOptions ViewOptions;

      private readonly Func<TEntry, TValue> valueGetter;
      private readonly Func<TEntry, string> labelGetter;
      private readonly Func<TEntry, string> descGetter;
      private readonly Func<TEntry, Texture2D> imageGetter;
      private readonly IReadOnlyList<(string Label, Func<TEntry, string> Getter)> tagGetters;
      private readonly Func<TEntry, bool> disabledPredicate;
      private readonly Func<TEntry, string> disabledReasonGetter;

   #endregion

   #region 생성자

      // ------------------------------------------------------------
      /// <summary>
      /// Picker spec을 생성한다.
      /// </summary>
      // ------------------------------------------------------------
      internal PickerSpec
      (
         string title,
         Func<TEntry, TValue> valueGetter,
         Func<TEntry, string> labelGetter,
         Func<TEntry, string> descGetter,
         Func<TEntry, Texture2D> imageGetter,
         IReadOnlyList<(string Label, Func<TEntry, string> Getter)> tagGetters,
         IReadOnlyList<string> defaultPreviewTags,
         IReadOnlyList<PickerColumnSpec<TEntry>> columns,
         IReadOnlyList<PickerFilterSpec<TEntry, TValue>> filters,
         PickerViewOptions viewOptions,
         Func<TEntry, bool> disabledPredicate,
         Func<TEntry, string> disabledReasonGetter
      ) : base()
      {
         Title                     = title ?? string.Empty;
         this.valueGetter          = valueGetter ?? throw new ArgumentNullException(nameof(valueGetter));
         this.labelGetter          = labelGetter ?? (_ => string.Empty);
         this.descGetter           = descGetter ?? (_ => string.Empty);
         this.imageGetter          = imageGetter ?? (_ => null);
         this.tagGetters           = CopyItems(tagGetters);
         DefaultPreviewTags        = CopyNonEmptyStrings(defaultPreviewTags);
         Columns                   = CopyItems(columns);
         Filters                   = CopyItems(filters);
         ViewOptions               = viewOptions;
         this.disabledPredicate    = disabledPredicate ?? (_ => false);
         this.disabledReasonGetter = disabledReasonGetter ?? (_ => string.Empty);
      }

   #endregion

   #region 생성

      // ------------------------------------------------------------
      /// <summary>
      /// builder를 생성한다.
      /// </summary>
      // ------------------------------------------------------------
      public static PickerSpecBuilder<TEntry, TValue> Create(string title)
      {
         return new PickerSpecBuilder<TEntry, TValue>(title);
      }

   #endregion

   #region 메서드

      // ------------------------------------------------------------
      /// <summary>
      /// 원본 entry를 picker 표시 entry로 변환한다.
      /// </summary>
      // ------------------------------------------------------------
      public PickerEntry<TEntry, TValue> CreateEntry(TEntry entry)
      {
         var isDisabled     = disabledPredicate(entry);
         var disabledReason = isDisabled ? disabledReasonGetter(entry) : string.Empty;
         var tags           = CreateTags(entry);
         var columns        = CreateColumns(entry);

         return new PickerEntry<TEntry, TValue>
         (
            entry,
            valueGetter(entry),
            labelGetter(entry),
            descGetter(entry),
            imageGetter(entry),
            tags,
            columns,
            !isDisabled,
            disabledReason
         );
      }

      // ------------------------------------------------------------
      /// <summary>
      /// entry preview와 검색에 사용할 tag 배열을 생성한다.
      /// </summary>
      // ------------------------------------------------------------
      private PickerTag[] CreateTags(TEntry entry)
      {
         if (tagGetters.Count == 0) return Array.Empty<PickerTag>();

         var result = new PickerTag[tagGetters.Count];
         for (var i = 0; i < tagGetters.Count; i++)
         {
            var tag = tagGetters[i];
            result[i] = new PickerTag(tag.Label, tag.Getter(entry));
         }

         return result;
      }

      // ------------------------------------------------------------
      /// <summary>
      /// entry table에 표시할 column 값 배열을 생성한다.
      /// </summary>
      // ------------------------------------------------------------
      private PickerColumnValue[] CreateColumns(TEntry entry)
      {
         if (Columns.Count == 0) return Array.Empty<PickerColumnValue>();

         var result = new PickerColumnValue[Columns.Count];
         for (var i = 0; i < Columns.Count; i++)
         {
            result[i] = Columns[i].CreateValue(entry);
         }

         return result;
      }

      // ------------------------------------------------------------
      /// <summary>
      /// 외부 목록을 spec 소유 배열로 복사한다.
      /// </summary>
      // ------------------------------------------------------------
      private static T[] CopyItems<T>(IReadOnlyList<T> items)
      {
         if (items == null || items.Count == 0) return Array.Empty<T>();

         var result = new T[items.Count];
         for (var i = 0; i < items.Count; i++)
         {
            result[i] = items[i];
         }

         return result;
      }

      // ------------------------------------------------------------
      /// <summary>
      /// 빈 문자열을 제외한 기본 preview tag label 배열을 생성한다.
      /// </summary>
      // ------------------------------------------------------------
      private static string[] CopyNonEmptyStrings(IReadOnlyList<string> items)
      {
         if (items == null || items.Count == 0) return Array.Empty<string>();

         var count = 0;
         for (var i = 0; i < items.Count; i++)
         {
            if (!string.IsNullOrEmpty(items[i]))
            {
               count++;
            }
         }

         if (count == 0) return Array.Empty<string>();

         var result = new string[count];
         var index = 0;
         for (var i = 0; i < items.Count; i++)
         {
            if (string.IsNullOrEmpty(items[i])) continue;

            result[index] = items[i];
            index++;
         }

         return result;
      }

   #endregion

   }
}
