/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : PickerDefaultTextures.cs
수정일 : 2026-06-07

# 설명
Picker view에서 사용하는 기본 런타임 텍스처를 제공한다.
========================================================================= BLOCK_HEADER_END */

using UnityEngine;

namespace inonego.Xeri.UI.Picker
{
   // ============================================================
   /// <summary>
   /// Picker view 기본 텍스처 제공자.
   /// </summary>
   // ============================================================
   internal static class PickerDefaultTextures
   {

   #region 필드

      private const int CheckerSize = 48;
      private const int CheckerCellSize = 8;

      private static Texture2D grayChecker = null;

      // ------------------------------------------------------------
      /// <summary>
      /// 회색 checker 기본 이미지.
      /// </summary>
      // ------------------------------------------------------------
      public static Texture2D GrayChecker
      {
         get
         {
            if (grayChecker == null)
            {
               grayChecker = CreateGrayChecker();
            }

            return grayChecker;
         }
      }

   #endregion

   #region 생성

      // ------------------------------------------------------------
      /// <summary>
      /// preview와 table cell에서 공유할 회색 checker 텍스처를 생성한다.
      /// </summary>
      // ------------------------------------------------------------
      private static Texture2D CreateGrayChecker()
      {
         var texture = new Texture2D(CheckerSize, CheckerSize, TextureFormat.RGBA32, false)
         {
            name       = "Xeri Picker Gray Checker",
            hideFlags  = HideFlags.HideAndDontSave,
            filterMode = FilterMode.Point,
            wrapMode   = TextureWrapMode.Repeat,
         };

         var baseColor = new Color(0.33f, 0.35f, 0.38f, 1f);
         var panelDarker = new Color(0.12f, 0.12f, 0.12f, 1f);
         var textMuted = new Color(0.65f, 0.65f, 0.65f, 1f);
         var text = new Color(0.84f, 0.84f, 0.84f, 1f);

         for (var y = 0; y < CheckerSize; y++)
         {
            for (var x = 0; x < CheckerSize; x++)
            {
               var gradient = (x + y) / 96f;
               var color = Blend(baseColor, panelDarker, gradient * 0.35f);

               if (((x / CheckerCellSize) + (y / CheckerCellSize)) % 2 == 0)
               {
                  color = Blend(color, textMuted, 0.08f);
               }

               if (x > 30 && y < 16)
               {
                  color = Blend(color, text, 0.16f);
               }

               texture.SetPixel(x, y, color);
            }
         }

         texture.Apply(false, true);

         return texture;
      }

      // ------------------------------------------------------------
      /// <summary>
      /// 목업 텍스처와 같은 색상 보간을 수행한다.
      /// </summary>
      // ------------------------------------------------------------
      private static Color Blend(Color from, Color to, float weight)
      {
         return Color.Lerp(from, to, weight);
      }

   #endregion

   }
}
