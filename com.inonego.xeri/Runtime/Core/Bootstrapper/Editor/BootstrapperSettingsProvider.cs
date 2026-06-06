/* BLOCK_HEADER_BEGIN =======================================================================
파일명: BootstrapperSettingsProvider.cs
수정일: 2026-05-20

# 설명
Project Settings에서 BootStrapperSettings 에셋을 편집하기 위한 UI Toolkit 기반 SettingsProvider.
SerializedObjectHelper.CreateAll을 사용해 설정 에셋의 직렬화 필드를 표시한다.
========================================================================= BLOCK_HEADER_END */

using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace inonego.Xeri.Bootstrapper.Editor
{
    // ============================================================
    /// <summary>
    /// BootStrapper Project Settings Provider.
    /// </summary>
    // ============================================================
    public class BootstrapperSettingsProvider : SettingsProvider
    {

    #region 상수

        private const string SETTINGS_MENU_PATH = "Project/Bootstrapper";

    #endregion

    #region 필드

        private SerializedObject serializedObject;

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// SettingsProvider를 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        public BootstrapperSettingsProvider(string path, SettingsScope scope = SettingsScope.Project) : base(path, scope) {}

    #endregion

    #region 생성

        // ------------------------------------------------------------
        /// <summary>
        /// Project Settings 메뉴에 BootStrapper 설정 항목을 등록한다.
        /// </summary>
        // ------------------------------------------------------------
        [SettingsProvider]
        public static SettingsProvider CreateSettingsProvider()
        {
            var provider = new BootstrapperSettingsProvider(SETTINGS_MENU_PATH, SettingsScope.Project)
            {
                keywords = GetSearchKeywordsFromGUIContentProperties<BootstrapperSettings>()
            };

            return provider;
        }

    #endregion

    #region 생명주기

        // ------------------------------------------------------------
        /// <summary>
        /// SettingsProvider가 활성화될 때 UI Toolkit 요소를 구성한다.
        /// </summary>
        // ------------------------------------------------------------
        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            base.OnActivate(searchContext, rootElement);

            serializedObject = new SerializedObject(BootstrapperSettings.CreateAsset());

            rootElement.Clear();
            rootElement.Add(new InspectorElement(serializedObject));
        }

    #endregion

    }
}
