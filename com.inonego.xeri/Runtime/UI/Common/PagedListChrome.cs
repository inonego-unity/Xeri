/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : PagedListChrome.cs
수정일 : 2026-10-18

# 설명
Picker와 Collection Editor가 공유하는 검색·콘텐츠·페이지네이션 UI의 배치 셸.
호스트별 편집 정책은 소유하지 않으며 각 영역에 View 콘텐츠를 삽입한다.
========================================================================= BLOCK_HEADER_END */

using System;

using UnityEngine;
using UnityEngine.UIElements;

namespace inonego.Xeri.UI
{
    // ============================================================
    /// <summary>
    /// 검색·목록·페이지네이션을 배치하고 호스트별 콘텐츠 삽입 위치를 제공한다.
    /// </summary>
    // ============================================================
    internal sealed class PagedListChrome : VisualElement
    {

    #region 필드

        private const string layoutPath = "XeriUI/Common/PagedListChrome";

        // ------------------------------------------------------------
        /// <summary>
        /// 헤더를 배치할 UI 영역을 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public VisualElement HeaderHost { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 검색 UI를 배치할 영역을 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public VisualElement SearchHost { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 도구 모음의 주요 명령을 배치할 영역을 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public VisualElement ToolbarActionHost { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 도구 모음의 추가 명령을 배치할 영역을 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public VisualElement ToolbarExtraHost { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 목록 콘텐츠를 배치할 영역을 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public VisualElement ContentHost { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 상태 메시지를 배치할 영역을 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public VisualElement StatusHost { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 페이지 이동 UI를 배치할 영역을 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public VisualElement PaginationHost { get; }

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// 공통 UI 셸을 구성하고 호스트별 콘텐츠의 삽입 위치를 제공한다.
        /// </summary>
        // ------------------------------------------------------------
        public PagedListChrome()
        {
            var layout = Resources.Load<VisualTreeAsset>(layoutPath);
            var stylesheet = Resources.Load<StyleSheet>(layoutPath);
            if (layout == null || stylesheet == null)
            {
                throw new InvalidOperationException("PagedListChrome UXML/USS를 로드할 수 없습니다.");
            }

            AddToClassList("xeri-paged-list");
            layout.CloneTree(this);
            var template = this.Q<TemplateContainer>();
            if (template != null)
            {
                template.style.flexGrow = 1;
            }

            styleSheets.Add(stylesheet);

            HeaderHost = Require("paged-list-header");
            SearchHost = Require("paged-list-search");
            ToolbarActionHost = Require("paged-list-toolbar-actions");
            ToolbarExtraHost = Require("paged-list-toolbar-extra");
            ContentHost = Require("paged-list-content");
            StatusHost = Require("paged-list-status");
            PaginationHost = Require("paged-list-pagination");

            ToolbarExtraHost.style.display = DisplayStyle.None;
        }

    #endregion

    #region 메서드

        // ------------------------------------------------------------
        /// <summary>
        /// 검색바 아래의 추가 콘텐츠를 실제로 사용할 때만 표시한다.
        /// </summary>
        // ------------------------------------------------------------
        public void AddToolbarExtra(VisualElement element)
        {
            ToolbarExtraHost.Add(element ?? throw new ArgumentNullException(nameof(element)));
            ToolbarExtraHost.style.display = DisplayStyle.Flex;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 이름으로 공통 레이아웃 필수 영역을 확인한다.
        /// </summary>
        // ------------------------------------------------------------
        private VisualElement Require(string name)
        {
            return this.Q<VisualElement>(name)
                ?? throw new InvalidOperationException($"PagedListChrome의 {name} 요소가 없습니다.");
        }

    #endregion

    }
}
