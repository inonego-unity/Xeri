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
        private BaseListView pageListView;
        private ScrollView pageScrollView;
        private PageScrollAnchor? pendingScrollAnchor;

    #endregion

    #region 프로퍼티

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
            RegisterCallback<DetachFromPanelEvent>(HandleDetached);
        }

    #endregion

    #region 공개 메서드

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
        /// 공통 페이지 스크롤에 사용할 목록과 레이아웃 이벤트를 연결한다.
        /// </summary>
        // ------------------------------------------------------------
        public void BindPageListView(BaseListView listView)
        {
            if (listView == null)
            {
                throw new ArgumentNullException(nameof(listView));
            }

            if (pageListView != null)
            {
                throw new InvalidOperationException("페이지 목록은 한 번만 연결할 수 있습니다.");
            }

            var scrollView = listView.Q<ScrollView>()
                ?? throw new InvalidOperationException("페이지 목록의 ScrollView를 찾을 수 없습니다.");

            pageListView = listView;
            pageScrollView = scrollView;
            pageScrollView.contentContainer.RegisterCallback<GeometryChangedEvent>(HandlePageGeometryChanged);
            pageScrollView.contentViewport.RegisterCallback<GeometryChangedEvent>(HandlePageGeometryChanged);
            pageListView.RegisterCallback<WheelEvent>(HandlePageWheel, TrickleDown.TrickleDown);
            pageListView.RegisterCallback<PointerDownEvent>(HandlePagePointerDown, TrickleDown.TrickleDown);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 페이지 목록과 연결된 스크롤 이벤트를 해제한다.
        /// </summary>
        // ------------------------------------------------------------
        public void UnbindPageListView()
        {
            CancelPageScroll();
            if (pageListView == null) return;

            pageScrollView.contentContainer.UnregisterCallback<GeometryChangedEvent>(HandlePageGeometryChanged);
            pageScrollView.contentViewport.UnregisterCallback<GeometryChangedEvent>(HandlePageGeometryChanged);
            pageListView.UnregisterCallback<WheelEvent>(HandlePageWheel, TrickleDown.TrickleDown);
            pageListView.UnregisterCallback<PointerDownEvent>(HandlePagePointerDown, TrickleDown.TrickleDown);
            pageListView = null;
            pageScrollView = null;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 페이지의 시작 또는 끝 경계로 목록을 스크롤한다.
        /// </summary>
        // ------------------------------------------------------------
        public void ScrollToPageBoundary(PageScrollAnchor anchor)
        {
            if (pageListView == null)
            {
                throw new InvalidOperationException("페이지 목록이 연결되지 않았습니다.");
            }

            if (pageListView.itemsSource == null || pageListView.itemsSource.Count == 0)
            {
                pendingScrollAnchor = null;
                return;
            }

            pendingScrollAnchor = anchor;
            ApplyPageScroll(anchor);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 목록 데이터가 다시 구성될 때 이전 스크롤 요청을 취소한다.
        /// </summary>
        // ------------------------------------------------------------
        public void CancelPageScroll()
        {
            pendingScrollAnchor = null;
        }

    #endregion

    #region 비공개 메서드

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

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 페이지의 스크롤을 시작 또는 끝에 맞춘다.
        /// </summary>
        // ------------------------------------------------------------
        private void ApplyPageScroll(PageScrollAnchor anchor)
        {
            if (pageListView.panel == null) return;
            var count = pageListView.itemsSource?.Count ?? 0;
            if (count == 0) return;

            pageListView.ScrollToItem(anchor == PageScrollAnchor.Start ? 0 : count - 1);

            var offset = pageScrollView.scrollOffset;
            offset.y = anchor == PageScrollAnchor.Start
                ? 0f
                : Mathf.Max(0f, pageScrollView.verticalScroller.highValue);
            pageScrollView.scrollOffset = offset;
        }

    #endregion

    #region 이벤트 핸들러

        // ------------------------------------------------------------
        /// <summary>
        /// 사용자 휠 스크롤이 시작되면 대기 중인 자동 스크롤을 취소한다.
        /// </summary>
        // ------------------------------------------------------------
        private void HandlePageWheel(WheelEvent evt)
        {
            CancelPageScroll();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 사용자가 목록을 직접 조작하면 대기 중인 자동 스크롤을 취소한다.
        /// </summary>
        // ------------------------------------------------------------
        private void HandlePagePointerDown(PointerDownEvent evt)
        {
            CancelPageScroll();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 셸이 패널에서 분리되면 예약된 스크롤 요청을 취소한다.
        /// </summary>
        // ------------------------------------------------------------
        private void HandleDetached(DetachFromPanelEvent evt)
        {
            CancelPageScroll();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 목록의 가상화 레이아웃이 변경되면 요청한 경계 위치를 확정한다.
        /// </summary>
        // ------------------------------------------------------------
        private void HandlePageGeometryChanged(GeometryChangedEvent evt)
        {
            if (!pendingScrollAnchor.HasValue) return;

            ApplyPageScroll(pendingScrollAnchor.Value);
            pendingScrollAnchor = null;
        }

    #endregion

    }
}
