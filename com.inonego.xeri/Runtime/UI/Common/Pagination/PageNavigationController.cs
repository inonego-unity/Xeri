/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : PageNavigationController.cs
수정일 : 2026-10-28

# 설명
공통 검색·페이지 목록에 이동 명령을 적용하고 선택할 페이지 내부 행과 스크롤 방향을 결정한다.
목록 선택 상태와 실제 UI Toolkit 스크롤은 호출자가 반영한다.
========================================================================= BLOCK_HEADER_END */

using System;

namespace inonego.Xeri.UI
{
    // ============================================================
    /// <summary>
    /// 페이지 이동 명령의 종류를 나타낸다.
    /// </summary>
    // ============================================================
    public enum PageNavigationKind
    {
        First,
        Previous,
        Next,
        Last,
        Direct,
    }

    // ============================================================
    /// <summary>
    /// 페이지 전환 후 목록이 보여줄 경계를 나타낸다.
    /// </summary>
    // ============================================================
    public enum PageScrollAnchor
    {
        Start,
        End,
    }

    // ============================================================
    /// <summary>
    /// 페이지 이동 종류와 직접 입력한 페이지 인덱스를 전달한다.
    /// </summary>
    // ============================================================
    public readonly struct PageNavigationRequest
    {

    #region 프로퍼티

        // ------------------------------------------------------------
        /// <summary>
        /// 실행할 페이지 이동 명령.
        /// </summary>
        // ------------------------------------------------------------
        public PageNavigationKind Kind { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 직접 페이지 이동에 사용할 0 기반 페이지 인덱스.
        /// </summary>
        // ------------------------------------------------------------
        public int PageIndex { get; }

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// 페이지 이동 명령을 구성한다.
        /// </summary>
        // ------------------------------------------------------------
        public PageNavigationRequest(PageNavigationKind kind, int pageIndex = 0)
        {
            Kind = kind;
            PageIndex = pageIndex;
        }

    #endregion

    }

    // ============================================================
    /// <summary>
    /// 페이지 이동 후 선택 대상과 스크롤 경계를 전달한다.
    /// </summary>
    // ============================================================
    public readonly struct PageNavigationResult
    {

    #region 프로퍼티

        // ------------------------------------------------------------
        /// <summary>
        /// 이동 결과를 선택과 스크롤에 반영할지 여부.
        /// </summary>
        // ------------------------------------------------------------
        public bool ShouldApply { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 이동한 페이지 내부의 선택 인덱스. 빈 페이지에서는 -1.
        /// </summary>
        // ------------------------------------------------------------
        public int SelectedIndex { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 이동한 페이지에서 표시할 목록 경계.
        /// </summary>
        // ------------------------------------------------------------
        public PageScrollAnchor ScrollAnchor { get; }

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// 선택·스크롤 적용 여부와 표시할 페이지 경계를 저장한다.
        /// </summary>
        // ------------------------------------------------------------
        public PageNavigationResult(bool shouldApply, int selectedIndex, PageScrollAnchor scrollAnchor)
        {
            ShouldApply = shouldApply;
            SelectedIndex = selectedIndex;
            ScrollAnchor = scrollAnchor;
        }

    #endregion

    }

    // ============================================================
    /// <summary>
    /// 공통 페이지 목록의 이동과 이동 후 표시 정책을 관리한다.
    /// </summary>
    // ============================================================
    internal sealed class PageNavigationController<T>
    {

    #region 필드

        private readonly SearchPagedList<T> pages;

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// 페이지 이동에 사용할 공통 검색·페이지 목록을 연결한다.
        /// </summary>
        // ------------------------------------------------------------
        public PageNavigationController(SearchPagedList<T> pages)
        {
            this.pages = pages ?? throw new ArgumentNullException(nameof(pages));
        }

    #endregion

    #region 메서드

        // ------------------------------------------------------------
        /// <summary>
        /// 페이지를 이동하고 선택할 행과 스크롤 경계를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public PageNavigationResult Navigate(PageNavigationRequest request)
        {
            var changed = false;
            switch (request.Kind)
            {
                case PageNavigationKind.First:
                    changed = pages.MoveFirst();
                    break;

                case PageNavigationKind.Previous:
                    changed = pages.MovePrev();
                    break;

                case PageNavigationKind.Next:
                    changed = pages.MoveNext();
                    break;

                case PageNavigationKind.Last:
                    changed = pages.MoveLast();
                    break;

                case PageNavigationKind.Direct:
                    changed = pages.MoveTo(request.PageIndex);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(request));
            }

            var scrollToEnd = request.Kind == PageNavigationKind.Previous
                || request.Kind == PageNavigationKind.Last;
            var selectedIndex = pages.PageItems.Count == 0
                ? -1
                : scrollToEnd ? pages.PageItems.Count - 1 : 0;

            return new PageNavigationResult
            (
                changed || request.Kind == PageNavigationKind.Direct,
                selectedIndex,
                scrollToEnd ? PageScrollAnchor.End : PageScrollAnchor.Start
            );
        }

    #endregion

    }
}
