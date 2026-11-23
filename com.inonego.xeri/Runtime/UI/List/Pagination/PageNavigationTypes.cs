/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : PageNavigationTypes.cs
수정일 : 2026-11-22

# 설명
페이지 이동 요청의 공용 형식을 정의한다.
PageNavigator(TableGrid Paged 페이지 이동 컨트롤)가 만들고 TableGrid가 처리한다.
========================================================================= BLOCK_HEADER_END */

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
}
