/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TEST_PickerViewResourceLoader.cs
수정일 : 2026-06-07

# 설명
Picker UXML/USS Resources loader 계약 테스트.

# 테스트 구성
 A: Asset Loader
========================================================================= BLOCK_HEADER_END */

using UnityEngine;
using UnityEngine.UIElements;

using NUnit;
using NUnit.Framework;

using inonego.Xeri;
using inonego.Xeri.UI;
using inonego.Xeri.UI.Picker;

namespace inonego.Xeri.TEST.UI._Picker
{
   // ============================================================
   /// <summary>
   /// Picker view asset loader 테스트 클래스.
   /// </summary>
   // ============================================================
   public class TEST_PickerViewResourceLoader
   {

   #region A-1: Asset Loader

      // ------------------------------------------------------------
      /// <summary>
      /// Picker 필수 UXML/USS asset은 Resources 경로에서 로드된다.
      /// </summary>
      // ------------------------------------------------------------
      [Test]
      public void TEST_PickerViewResourceLoader_Load_필수_Asset_반환()
      {
         var layout = PickerViewResourceLoader.LoadLayout();

         Assert.NotNull(layout, "PickerView.uxml must be loadable from Resources path.");
         Assert.NotNull(PickerViewResourceLoader.LoadThemeStyle(), "PickerTheme.uss must be loadable from Resources path.");
         Assert.NotNull(PickerViewResourceLoader.LoadViewStyle(), "PickerViewStyle.uss must be loadable from Resources path.");

         var root = layout.CloneTree();
         Assert.NotNull(root.Q<Image>("preview-image"));
         Assert.NotNull(root.Q<Button>("select-button"));
         Assert.NotNull(root.Q<VisualElement>("search-field"));
         Assert.NotNull(root.Q<MultiColumnListView>("entry-table"));
         Assert.NotNull(root.Q<Label>("empty-label"));
         Assert.NotNull(root.Q<Button>("first-page-button"));
      }

   #endregion

   }
}
