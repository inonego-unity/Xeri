/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : PickerViewResourceLoader.cs
수정일 : 2026-06-07

# 설명
Picker runtime view가 사용할 UXML/USS asset을 Resources에서 로드한다.
========================================================================= BLOCK_HEADER_END */

using System;

using UnityEngine;
using UnityEngine.UIElements;

namespace inonego.Xeri.UI.Picker
{
   // ============================================================
   /// <summary>
   /// Picker view Resources loader.
   /// </summary>
   // ============================================================
   public static class PickerViewResourceLoader
   {

   #region 필드

      private const string LAYOUT_PATH = "XeriUI/Picker/PickerView";
      private const string THEME_STYLE_PATH = "XeriUI/Picker/PickerTheme";
      private const string VIEW_STYLE_PATH = "XeriUI/Picker/PickerViewStyle";

   #endregion

   #region 메서드

      // ------------------------------------------------------------
      /// <summary>
      /// PickerView UXML을 로드한다.
      /// </summary>
      // ------------------------------------------------------------
      public static VisualTreeAsset LoadLayout()
      {
         return LoadRequired<VisualTreeAsset>(LAYOUT_PATH, "PickerView UXML");
      }

      // ------------------------------------------------------------
      /// <summary>
      /// Picker theme USS를 로드한다.
      /// </summary>
      // ------------------------------------------------------------
      public static StyleSheet LoadThemeStyle()
      {
         return LoadRequired<StyleSheet>(THEME_STYLE_PATH, "PickerTheme USS");
      }

      // ------------------------------------------------------------
      /// <summary>
      /// Picker view USS를 로드한다.
      /// </summary>
      // ------------------------------------------------------------
      public static StyleSheet LoadViewStyle()
      {
         return LoadRequired<StyleSheet>(VIEW_STYLE_PATH, "PickerView USS");
      }

      // ------------------------------------------------------------
      /// <summary>
      /// Resources 경로에서 필수 asset을 로드한다.
      /// </summary>
      // ------------------------------------------------------------
      private static T LoadRequired<T>(string path, string label) where T : UnityEngine.Object
      {
         var asset = Resources.Load<T>(path);

         if (asset == null)
         {
            throw new InvalidOperationException($"{label}을 Resources에서 로드할 수 없습니다. Path: {path}");
         }

         return asset;
      }

   #endregion

   }
}
