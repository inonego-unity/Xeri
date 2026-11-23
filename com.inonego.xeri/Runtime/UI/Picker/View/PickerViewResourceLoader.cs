/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : PickerViewResourceLoader.cs
수정일 : 2026-11-23

# 설명
Picker runtime view가 사용할 UXML/USS asset을 Resources에서 로드한다.
========================================================================= BLOCK_HEADER_END */

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

        private const string layoutPath = "Xeri/UI/Picker/PickerView";
        private const string themeStylePath = "Xeri/UI/Picker/PickerTheme";
        private const string viewStylePath = "Xeri/UI/Picker/PickerViewStyle";

    #endregion

    #region 메서드

        // ------------------------------------------------------------
        /// <summary>
        /// PickerView UXML을 로드한다.
        /// </summary>
        // ------------------------------------------------------------
        public static VisualTreeAsset LoadLayout()
        {
            return XeriUIResources.Require<VisualTreeAsset>(layoutPath, "Picker");
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Picker theme USS를 로드한다.
        /// </summary>
        // ------------------------------------------------------------
        public static StyleSheet LoadThemeStyle()
        {
            return XeriUIResources.Require<StyleSheet>(themeStylePath, "Picker");
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Picker view USS를 로드한다.
        /// </summary>
        // ------------------------------------------------------------
        public static StyleSheet LoadViewStyle()
        {
            return XeriUIResources.Require<StyleSheet>(viewStylePath, "Picker");
        }

    #endregion

    }
}
