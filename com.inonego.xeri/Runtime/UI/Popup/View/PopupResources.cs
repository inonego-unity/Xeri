/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : PopupResources.cs
수정일 : 2026-10-28

# 설명
팝업 호스트가 쓰는 backdrop·프레임 레이어·프레임을 Resources의 UXML·USS로 만든다.

# 특이사항, 제약사항
리소스는 Resources/XeriUI/Popup/에 있고 컨텍스트 메뉴 리소스와 같은 방식으로 한 번만 불러 공유한다.
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

        private const string resourceRoot = "XeriUI/Popup/";

        private static VisualTreeAsset backdropLayout;
        private static VisualTreeAsset layerLayout;
        private static VisualTreeAsset frameLayout;
        private static StyleSheet popupStyle;

    #endregion

    #region 요소 생성

        // ------------------------------------------------------------
        /// <summary>
        /// 부모 전체를 덮어 아래 요소로 입력이 가지 않게 하는 요소를 만든다.
        /// </summary>
        // ------------------------------------------------------------
        public static VisualElement CreateBackdrop()
        {
            EnsureLoaded();

            return Create(backdropLayout, "popup-backdrop");
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 입력을 받지 않고 팝업 프레임만 담는 전체 덮개 요소를 만든다.
        /// </summary>
        // ------------------------------------------------------------
        public static VisualElement CreateLayer()
        {
            EnsureLoaded();

            return Create(layerLayout, "popup-layer");
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 팝업 한 단계의 내용을 담는 프레임 요소를 만든다.
        /// </summary>
        // ------------------------------------------------------------
        public static VisualElement CreateFrame()
        {
            EnsureLoaded();

            return Create(frameLayout, "popup-frame");
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 템플릿에서 지정한 이름의 요소를 꺼내 USS를 연결한다.
        /// </summary>
        // ------------------------------------------------------------
        private static VisualElement Create(VisualTreeAsset layout, string name)
        {
            var container = layout.Instantiate();
            var element = container.Q<VisualElement>(name)
                ?? throw new InvalidOperationException
                (
                    $"팝업 리소스에서 요소를 찾을 수 없습니다: {name}"
                );

            element.RemoveFromHierarchy();
            element.styleSheets.Add(popupStyle);

            return element;
        }

    #endregion

    #region 리소스 로드

        // ------------------------------------------------------------
        /// <summary>
        /// UXML·USS 리소스를 모든 호스트가 공유하도록 한 번만 불러온다.
        /// </summary>
        // ------------------------------------------------------------
        private static void EnsureLoaded()
        {
            if (backdropLayout == null)
            {
                backdropLayout = LoadResource<VisualTreeAsset>("XeriPopupBackdrop");
            }

            if (layerLayout == null)
            {
                layerLayout = LoadResource<VisualTreeAsset>("XeriPopupLayer");
            }

            if (frameLayout == null)
            {
                frameLayout = LoadResource<VisualTreeAsset>("XeriPopupFrame");
            }

            if (popupStyle == null)
            {
                popupStyle = LoadResource<StyleSheet>("XeriPopupStyle");
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 필수 UI 리소스를 Resources에서 불러오고 없으면 예외를 던진다.
        /// </summary>
        // ------------------------------------------------------------
        private static T LoadResource<T>(string name)
        where T : UnityEngine.Object
        {
            var path = $"{resourceRoot}{name}";
            var asset = Resources.Load<T>(path);

            if (asset == null)
            {
                throw new InvalidOperationException($"팝업 리소스를 찾을 수 없습니다: {path}");
            }

            return asset;
        }

    #endregion

    }
}
