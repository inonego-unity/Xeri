/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : PickerSpecBuilder.cs
수정일 : 2026-06-17

# 설명
Provider 직접 구현 없이 PickerSpec을 구성하기 위한 fluent builder.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using inonego.Xeri;

namespace inonego.Xeri.UI.Picker
{
   // ============================================================
   /// <summary>
   /// Provider 직접 구현 없이 PickerSpec을 구성하기 위한 fluent builder.
   /// </summary>
   // ============================================================
   public sealed class PickerSpecBuilder<TEntry, TValue>
   {

   #region 필드

      private readonly string title;
      private readonly List<(string Label, Func<TEntry, string> Getter)> tagGetters = new();
      private readonly List<string> defaultPreviewTags = new();
      private readonly List<PickerColumnSpec<TEntry>> columns = new();
      private readonly List<PickerFilterSpec<TEntry, TValue>> filters = new();
      private Func<TEntry, TValue> valueGetter = null;
      private Func<TEntry, string> labelGetter = entry => entry?.ToString() ?? string.Empty;
      private Func<TEntry, string> descGetter = _ => string.Empty;
      private Func<TEntry, Texture2D> imageGetter = _ => null;
      private Func<TEntry, bool> disabledPredicate = _ => false;
      private Func<TEntry, string> disabledReasonGetter = _ => string.Empty;
      private PickerViewOptions viewOptions = PickerViewOptions.Default();

   #endregion

   #region 생성자

      // ------------------------------------------------------------
      /// <summary>
      /// builder를 생성한다.
      /// </summary>
      // ------------------------------------------------------------
      internal PickerSpecBuilder(string title) : base()
      {
         this.title = title ?? string.Empty;
      }

   #endregion

   #region 메서드

      // ------------------------------------------------------------
      /// <summary>
      /// 선택 완료 시 반환할 값을 지정한다.
      /// </summary>
      // ------------------------------------------------------------
      public PickerSpecBuilder<TEntry, TValue> Value(Func<TEntry, TValue> getter)
      {
         valueGetter = getter ?? throw new ArgumentNullException(nameof(getter));

         return this;
      }

      // ------------------------------------------------------------
      /// <summary>
      /// entry 대표 라벨을 지정한다.
      /// </summary>
      // ------------------------------------------------------------
      public PickerSpecBuilder<TEntry, TValue> Label(Func<TEntry, string> getter)
      {
         labelGetter = getter ?? (_ => string.Empty);

         return this;
      }

      // ------------------------------------------------------------
      /// <summary>
      /// entry 설명을 지정한다.
      /// </summary>
      // ------------------------------------------------------------
      public PickerSpecBuilder<TEntry, TValue> Desc(Func<TEntry, string> getter)
      {
         descGetter = getter ?? (_ => string.Empty);

         return this;
      }

      // ------------------------------------------------------------
      /// <summary>
      /// entry preview 이미지를 지정한다.
      /// </summary>
      // ------------------------------------------------------------
      public PickerSpecBuilder<TEntry, TValue> Image(Func<TEntry, Texture2D> getter)
      {
         imageGetter = getter ?? (_ => null);

         return this;
      }

      // ------------------------------------------------------------
      /// <summary>
      /// preview 영역 표시 여부를 지정한다.
      /// </summary>
      // ------------------------------------------------------------
      public PickerSpecBuilder<TEntry, TValue> Preview(bool isVisible)
      {
         viewOptions = viewOptions.WithPreview(isVisible);

         return this;
      }

      // ------------------------------------------------------------
      /// <summary>
      /// preview와 검색에 사용할 tag를 추가한다.
      /// </summary>
      // ------------------------------------------------------------
      public PickerSpecBuilder<TEntry, TValue> Tag(string label, Func<TEntry, string> getter)
      {
         tagGetters.Add((label ?? string.Empty, getter ?? (_ => string.Empty)));

         return this;
      }

      // ------------------------------------------------------------
      /// <summary>
      /// 선택 없음 preview에 표시할 기본 tag label 목록을 지정한다.
      /// </summary>
      // ------------------------------------------------------------
      public PickerSpecBuilder<TEntry, TValue> DefaultPreviewTags(params string[] labels)
      {
         defaultPreviewTags.Clear();

         if (labels == null) return this;

         foreach (var label in labels)
         {
            if (string.IsNullOrEmpty(label)) continue;

            defaultPreviewTags.Add(label);
         }

         return this;
      }

      // ------------------------------------------------------------
      /// <summary>
      /// table column을 추가한다.
      /// </summary>
      // ------------------------------------------------------------
      public PickerSpecBuilder<TEntry, TValue> Column<TColumnValue>
      (
         string header,
         Func<TEntry, TColumnValue> getter,
         PickerColumnOptions options
      )
      {
         return Column(header, header, getter, options);
      }

      // ------------------------------------------------------------
      /// <summary>
      /// table column을 식별자와 header를 분리해 추가한다.
      /// </summary>
      // ------------------------------------------------------------
      public PickerSpecBuilder<TEntry, TValue> Column<TColumnValue>
      (
         string id,
         string header,
         Func<TEntry, TColumnValue> getter,
         PickerColumnOptions options
      )
      {
         Func<TEntry, object> valueGetter = entry => getter == null ? null : getter(entry);
         columns.Add
         (
            new PickerColumnSpec<TEntry>
            (
               id,
               header,
               options,
               valueGetter
            )
         );

         return this;
      }

      // ------------------------------------------------------------
      /// <summary>
      /// picker entry 전체를 보는 filter를 추가한다.
      /// </summary>
      // ------------------------------------------------------------
      public PickerSpecBuilder<TEntry, TValue> Filter
      (
         string id,
         string label,
         bool defaultEnabled,
         Func<PickerEntry<TEntry, TValue>, bool> predicate
      )
      {
         return Filter
         (
            id,
            label,
            defaultEnabled,
            new PredicateFilter<PickerEntry<TEntry, TValue>>(predicate)
         );
      }

      // ------------------------------------------------------------
      /// <summary>
      /// picker entry 전체를 보는 filter를 추가한다.
      /// </summary>
      // ------------------------------------------------------------
      public PickerSpecBuilder<TEntry, TValue> Filter
      (
         string id,
         string label,
         bool defaultEnabled,
         IFilter<PickerEntry<TEntry, TValue>> filter
      )
      {
         filters.Add
         (
            new PickerFilterSpec<TEntry, TValue>
            (
               id,
               label,
               defaultEnabled,
               filter
            )
         );

         return this;
      }

      // ------------------------------------------------------------
      /// <summary>
      /// 원본 entry를 보는 filter를 추가한다.
      /// </summary>
      // ------------------------------------------------------------
      public PickerSpecBuilder<TEntry, TValue> FilterByEntry
      (
         string id,
         string label,
         bool defaultEnabled,
         Func<TEntry, bool> predicate
      )
      {
         return FilterByEntry
         (
            id,
            label,
            defaultEnabled,
            new PredicateFilter<TEntry>(predicate)
         );
      }

      // ------------------------------------------------------------
      /// <summary>
      /// 원본 entry를 보는 filter를 추가한다.
      /// </summary>
      // ------------------------------------------------------------
      public PickerSpecBuilder<TEntry, TValue> FilterByEntry
      (
         string id,
         string label,
         bool defaultEnabled,
         IFilter<TEntry> filter
      )
      {
         var entryFilter = filter ?? new PredicateFilter<TEntry>(_ => true);

         return Filter
         (
            id,
            label,
            defaultEnabled,
            new PredicateFilter<PickerEntry<TEntry, TValue>>(entry => entryFilter.IsMatch(entry.Entry))
         );
      }

      // ------------------------------------------------------------
      /// <summary>
      /// 선택 불가 조건과 사유를 지정한다.
      /// </summary>
      // ------------------------------------------------------------
      public PickerSpecBuilder<TEntry, TValue> DisabledWhen
      (
         Func<TEntry, bool> predicate,
         Func<TEntry, string> reason
      )
      {
         disabledPredicate    = predicate ?? (_ => false);
         disabledReasonGetter = reason ?? (_ => string.Empty);

         return this;
      }

      // ------------------------------------------------------------
      /// <summary>
      /// 불변 picker spec을 생성한다.
      /// </summary>
      // ------------------------------------------------------------
      public PickerSpec<TEntry, TValue> Build()
      {
         if (valueGetter == null)
         {
            throw new InvalidOperationException("PickerSpec requires Value selector.");
         }

         return new PickerSpec<TEntry, TValue>
         (
            title,
            valueGetter,
            labelGetter,
            descGetter,
            imageGetter,
            tagGetters,
            defaultPreviewTags,
            columns,
            filters,
            viewOptions,
            disabledPredicate,
            disabledReasonGetter
         );
      }

   #endregion

   }
}
