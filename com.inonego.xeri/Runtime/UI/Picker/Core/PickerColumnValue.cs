/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : PickerColumnValue.cs
수정일 : 2026-06-17

# 설명
Picker column의 원본 값, 표시 문자열, 검색 포함 여부를 함께 보관하는 모델.
========================================================================= BLOCK_HEADER_END */

using System;

namespace inonego.Xeri.UI.Picker
{
   // ============================================================
   /// <summary>
   /// Picker column의 원본 값과 표시 문자열을 함께 보관하는 모델.
   /// </summary>
   // ============================================================
   [Serializable]
   public readonly struct PickerColumnValue
   {

   #region 필드

      // ------------------------------------------------------------
      /// <summary>
      /// column 식별자.
      /// </summary>
      // ------------------------------------------------------------
      public readonly string ColumnID;

      // ------------------------------------------------------------
      /// <summary>
      /// column header 표시 문자열.
      /// </summary>
      // ------------------------------------------------------------
      public readonly string Header;

      // ------------------------------------------------------------
      /// <summary>
      /// 정렬에 사용할 원본 값.
      /// </summary>
      // ------------------------------------------------------------
      public readonly object Value;

      // ------------------------------------------------------------
      /// <summary>
      /// table cell과 검색에 사용할 표시 문자열.
      /// </summary>
      // ------------------------------------------------------------
      public readonly string DisplayText;

      // ------------------------------------------------------------
      /// <summary>
      /// 검색 문자열에 포함할지 여부.
      /// </summary>
      // ------------------------------------------------------------
      public readonly bool Searchable;

   #endregion

   #region 생성자

      // ------------------------------------------------------------
      /// <summary>
      /// 원본 값과 표시 문자열을 생성한다.
      /// </summary>
      // ------------------------------------------------------------
      public PickerColumnValue(string columnID, string header, object value, bool searchable = true) : this()
      {
         ColumnID    = columnID ?? string.Empty;
         Header      = header ?? string.Empty;
         Value       = value;
         DisplayText = value?.ToString() ?? string.Empty;
         Searchable  = searchable;
      }

   #endregion

   }
}
