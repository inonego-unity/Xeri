/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : PickerViewOptions.cs
수정일 : 2026-06-08

# 설명
Picker 표시 방식에 대한 view 전용 옵션 값.
========================================================================= BLOCK_HEADER_END */

using System;

namespace inonego.Xeri.UI.Picker
{
   // ============================================================
   /// <summary>
   /// Picker 표시 방식에 대한 view 전용 옵션 값.
   /// </summary>
   // ============================================================
   [Serializable]
   public readonly struct PickerViewOptions
   {

   #region 필드

      // ------------------------------------------------------------
      /// <summary>
      /// preview 영역 표시 여부.
      /// </summary>
      // ------------------------------------------------------------
      public readonly bool ShowPreview;

   #endregion

   #region 생성자

      // ------------------------------------------------------------
      /// <summary>
      /// view option 값을 생성한다.
      /// </summary>
      // ------------------------------------------------------------
      public PickerViewOptions(bool showPreview) : this()
      {
         ShowPreview = showPreview;
      }

   #endregion

   #region 생성

      // ------------------------------------------------------------
      /// <summary>
      /// 기본 view option 값을 생성한다.
      /// </summary>
      // ------------------------------------------------------------
      public static PickerViewOptions Default()
      {
         return new PickerViewOptions(true);
      }

   #endregion

   #region 메서드

      // ------------------------------------------------------------
      /// <summary>
      /// preview 표시 여부만 변경한 새 option 값을 반환한다.
      /// </summary>
      // ------------------------------------------------------------
      public PickerViewOptions WithPreview(bool isVisible)
      {
         return new PickerViewOptions(isVisible);
      }

   #endregion

   }
}
