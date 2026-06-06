/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : PickerEntry.cs
수정일 : 2026-06-07

# 설명
Picker 내부에서 사용하는 표시 entry 모델.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

using UnityEngine;

namespace inonego.Xeri.UI.Picker
{
   // ============================================================
   /// <summary>
   /// Picker 내부에서 사용하는 표시 entry 모델.
   /// </summary>
   // ============================================================
   [Serializable]
   public sealed class PickerEntry<TEntry, TValue>
   {

   #region 필드

      // ------------------------------------------------------------
      /// <summary>
      /// 원본 entry.
      /// </summary>
      // ------------------------------------------------------------
      public readonly TEntry Entry;

      // ------------------------------------------------------------
      /// <summary>
      /// 선택 완료 시 반환할 값.
      /// </summary>
      // ------------------------------------------------------------
      public readonly TValue Value;

      // ------------------------------------------------------------
      /// <summary>
      /// 목록과 preview에 표시할 대표 문자열.
      /// </summary>
      // ------------------------------------------------------------
      public readonly string Label;

      // ------------------------------------------------------------
      /// <summary>
      /// preview 설명.
      /// </summary>
      // ------------------------------------------------------------
      public readonly string Desc;

      // ------------------------------------------------------------
      /// <summary>
      /// preview 이미지.
      /// </summary>
      // ------------------------------------------------------------
      public readonly Texture2D Image;

      // ------------------------------------------------------------
      /// <summary>
      /// 태그 목록.
      /// </summary>
      // ------------------------------------------------------------
      public readonly IReadOnlyList<PickerTag> Tags;

      // ------------------------------------------------------------
      /// <summary>
      /// column 값 목록.
      /// </summary>
      // ------------------------------------------------------------
      public readonly IReadOnlyList<PickerColumnValue> Columns;

      // ------------------------------------------------------------
      /// <summary>
      /// 선택 가능 여부.
      /// </summary>
      // ------------------------------------------------------------
      public readonly bool IsEnabled;

      // ------------------------------------------------------------
      /// <summary>
      /// 선택 불가 사유.
      /// </summary>
      // ------------------------------------------------------------
      public readonly string DisabledReason;

      // ------------------------------------------------------------
      /// <summary>
      /// 검색에 사용할 통합 문자열.
      /// </summary>
      // ------------------------------------------------------------
      public readonly string SearchText;

   #endregion

   #region 생성자

      // ------------------------------------------------------------
      /// <summary>
      /// Picker 표시 entry를 생성한다.
      /// </summary>
      // ------------------------------------------------------------
      public PickerEntry
      (
         TEntry entry,
         TValue value,
         string label,
         string desc,
         Texture2D image,
         IReadOnlyList<PickerTag> tags,
         IReadOnlyList<PickerColumnValue> columns,
         bool isEnabled,
         string disabledReason
      ) : this
      (
         entry,
         value,
         label,
         desc,
         image,
         CopyTags(tags),
         CopyColumns(columns),
         isEnabled,
         disabledReason
      )
      {
      }

      // ------------------------------------------------------------
      /// <summary>
      /// 이미 배열로 구성된 내부 entry 데이터를 복사 없이 보관한다.
      /// </summary>
      // ------------------------------------------------------------
      internal PickerEntry
      (
         TEntry entry,
         TValue value,
         string label,
         string desc,
         Texture2D image,
         PickerTag[] tags,
         PickerColumnValue[] columns,
         bool isEnabled,
         string disabledReason
      ) : base()
      {
         Entry          = entry;
         Value          = value;
         Label          = label ?? string.Empty;
         Desc           = desc ?? string.Empty;
         Image          = image;
         Tags           = tags ?? Array.Empty<PickerTag>();
         Columns        = columns ?? Array.Empty<PickerColumnValue>();
         IsEnabled      = isEnabled;
         DisabledReason = disabledReason ?? string.Empty;
         SearchText     = BuildSearchText();
      }

   #endregion

   #region 메서드

      // ------------------------------------------------------------
      /// <summary>
      /// 지정한 column 값을 반환한다.
      /// </summary>
      // ------------------------------------------------------------
      public PickerColumnValue GetColumn(string columnID)
      {
         foreach (var column in Columns)
         {
            if (column.ColumnID == columnID)
            {
               return column;
            }
         }

         return default;
      }

      // ------------------------------------------------------------
      /// <summary>
      /// entry 기본 preview 모델을 생성한다.
      /// </summary>
      // ------------------------------------------------------------
      public PickerPreviewModel CreateDefaultPreview()
      {
         return new PickerPreviewModel(Image, Label, Value?.ToString() ?? string.Empty, Desc, Tags);
      }

      // ------------------------------------------------------------
      /// <summary>
      /// label, desc, tag, column 값을 하나의 검색 문자열로 만든다.
      /// </summary>
      // ------------------------------------------------------------
      private string BuildSearchText()
      {
         var builder = new StringBuilder();

         AppendSearchPart(builder, Label);
         AppendSearchPart(builder, Desc);
         AppendSearchPart(builder, Value?.ToString());

         foreach (var tag in Tags)
         {
            AppendSearchPart(builder, tag.Label);
            AppendSearchPart(builder, tag.Value);
         }

         foreach (var column in Columns)
         {
            AppendSearchPart(builder, column.DisplayText);
         }

         return builder.ToString();
      }

      // ------------------------------------------------------------
      /// <summary>
      /// 빈 값은 검색 문자열에 포함하지 않고 의미 있는 token만 추가한다.
      /// </summary>
      // ------------------------------------------------------------
      private static void AppendSearchPart(StringBuilder builder, string value)
      {
         if (string.IsNullOrEmpty(value)) return;

         if (builder.Length > 0)
         {
            builder.Append(' ');
         }

         builder.Append(value);
      }

      // ------------------------------------------------------------
      /// <summary>
      /// 외부에서 받은 tag 목록을 entry 소유 배열로 복사한다.
      /// </summary>
      // ------------------------------------------------------------
      private static PickerTag[] CopyTags(IReadOnlyList<PickerTag> tags)
      {
         if (tags == null || tags.Count == 0) return Array.Empty<PickerTag>();

         var result = new PickerTag[tags.Count];
         for (var i = 0; i < tags.Count; i++)
         {
            result[i] = tags[i];
         }

         return result;
      }

      // ------------------------------------------------------------
      /// <summary>
      /// 외부에서 받은 column 목록을 entry 소유 배열로 복사한다.
      /// </summary>
      // ------------------------------------------------------------
      private static PickerColumnValue[] CopyColumns(IReadOnlyList<PickerColumnValue> columns)
      {
         if (columns == null || columns.Count == 0) return Array.Empty<PickerColumnValue>();

         var result = new PickerColumnValue[columns.Count];
         for (var i = 0; i < columns.Count; i++)
         {
            result[i] = columns[i];
         }

         return result;
      }

   #endregion

   }
}
