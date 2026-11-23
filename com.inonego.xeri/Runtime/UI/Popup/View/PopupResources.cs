/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : PopupResources.cs
수정일 : 2026-11-23

# 설명
팝업 호스트가 쓰는 backdrop·프레임 레이어·프레임을 Resources의 UXML·USS로 만든다.

# 특이사항, 제약사항
리소스는 Resources/Xeri/UI/Popup/에 있고 로드·보관은 공용 XeriUIResources가 맡는다.
요소는 UXML 템플릿에서 꺼내 USS를 연결해 돌려주므로 호출자는 위치·크기 같은 배치 값만 지정한다.
========================================================================= BLOCK_HEADER_END */

using System;

using UnityEngine;
using UnityEngine.UIElements;

namespace inonego.Xeri.UI
{
    // ============================================================
    /// <summary>
    /// 팝업 호스트용 UXML·USS 리소스 로더.
    /// </summary>
    // ============================================================
    internal static class PopupResources
    {

    #region 필드

        private const string LAYOUT_ROOT = "Xeri/UI/Popup/";
        private const string STYLE_PATH = "Xeri/UI/Popup/XeriPopupStyle";
        private const string SUBJECT = "Popup";

    #endregion

    #region 요소 생성

        // ------------------------------------------------------------
        /// <summary>
        /// 부모 전체를 덮어 아래 요소로 입력이 가지 않게 하는 요소를 만든다.
        /// </summary>
        // ------------------------------------------------------------
        public static VisualElement CreateBackdrop()
        {
            return Create("XeriPopupBackdrop", "popup-backdrop");
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 입력을 받지 않고 팝업 프레임만 담는 전체 덮개 요소를 만든다.
        /// </summary>
        // ------------------------------------------------------------
        public static VisualElement CreateLayer()
        {
            return Create("XeriPopupLayer", "popup-layer");
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 팝업 한 단계의 내용을 담는 프레임 요소를 만든다.
        /// </summary>
        // ------------------------------------------------------------
        public static VisualElement CreateFrame()
        {
            return Create("XeriPopupFrame", "popup-frame");
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 템플릿에서 지정한 이름의 요소를 꺼내 USS를 연결한다.
        /// </summary>
        // ------------------------------------------------------------
        private static VisualElement Create(string layoutName, string name)
        {
            var layout = XeriUIResources.Require<VisualTreeAsset>(LAYOUT_ROOT + layoutName, SUBJECT);
            var element = XeriUIResources.Require<VisualElement>(layout.Instantiate(), name, SUBJECT);

            element.RemoveFromHierarchy();
            element.styleSheets.Add(XeriUIResources.Require<StyleSheet>(STYLE_PATH, SUBJECT));

            return element;
        }

    #endregion

    }
}
