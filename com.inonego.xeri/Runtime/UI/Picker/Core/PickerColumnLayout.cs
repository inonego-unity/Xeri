/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : PickerColumnLayout.cs
수정일 : 2026-06-17

# 설명
Picker table column의 폭과 stretch 정책을 표현하는 layout 모델.
========================================================================= BLOCK_HEADER_END */

using System;

namespace inonego.Xeri.UI.Picker
{
   // ============================================================
   /// <summary>
   /// Picker table column의 layout mode.
   /// </summary>
   // ============================================================
   public enum PickerColumnLayoutMode
   {
      Fixed,
      Flexible,
   }

   // ============================================================
   /// <summary>
   /// Picker table column의 폭과 stretch 정책.
   /// </summary>
   // ============================================================
   [Serializable]
   public sealed class PickerColumnLayout
   {

   #region 필드

      public readonly PickerColumnLayoutMode Mode;
      public readonly float Width;
      public readonly float MinWidth;
      public readonly float MaxWidth;
      public readonly float StretchWeight;

   #endregion

   #region 생성자

      // ------------------------------------------------------------
      /// <summary>
      /// column layout 정책을 생성한다.
      /// </summary>
      // ------------------------------------------------------------
      public PickerColumnLayout
      (
         PickerColumnLayoutMode mode,
         float width,
         float minWidth,
         float maxWidth,
         float stretchWeight
      ) : base()
      {
         Mode          = mode;
         Width         = Math.Max(1f, width);
         MinWidth      = Math.Max(1f, minWidth);
         MaxWidth      = Math.Max(0f, maxWidth);
         StretchWeight = Math.Max(0f, stretchWeight);
      }

   #endregion

   #region 생성

      // ------------------------------------------------------------
      /// <summary>
      /// 고정 폭 column layout을 생성한다.
      /// </summary>
      // ------------------------------------------------------------
      public static PickerColumnLayout Fixed(float width)
      {
         var safeWidth = Math.Max(1f, width);

         return new PickerColumnLayout
         (
            PickerColumnLayoutMode.Fixed,
            safeWidth,
            safeWidth,
            safeWidth,
            0f
         );
      }

      // ------------------------------------------------------------
      /// <summary>
      /// 가변 폭 column layout을 생성한다.
      /// </summary>
      // ------------------------------------------------------------
      public static PickerColumnLayout Flexible
      (
         float width,
         float minWidth,
         float maxWidth,
         float stretchWeight
      )
      {
         return new PickerColumnLayout
         (
            PickerColumnLayoutMode.Flexible,
            width,
            minWidth,
            maxWidth,
            stretchWeight
         );
      }

   #endregion

   }
}
