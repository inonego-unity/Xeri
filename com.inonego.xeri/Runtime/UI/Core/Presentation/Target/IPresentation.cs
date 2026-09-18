/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : IPresentation.cs
수정일 : 2026-09-17
# 설명
기존 UI 표현 대상을 Xeri Presentation State에 연결하는 최소 계약을 정의한다.
Presentation은 View 소유권이나 UI hierarchy를 강제하지 않고 지원하는 State만 선택적으로 노출한다.
========================================================================= BLOCK_HEADER_END */

namespace inonego.Xeri.UI
{
    // ================================================================================
    /// <summary>
    /// <br/> Alpha·Visibility 같은 표현 상태 작업에 참여하는 논리적 UI 대상.
    /// <br/> 지원하지 않는 State는 null로 노출해 backend capability를 강제하지 않는다.
    /// </summary>
    // ================================================================================
    public interface IPresentation
    {
        // ------------------------------------------------------------
        /// <summary>
        /// 합성 가능한 Alpha State. 지원하지 않으면 null이다.
        /// </summary>
        // ------------------------------------------------------------
        PresentationAlpha Alpha { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 합성 가능한 Visibility State. 지원하지 않으면 null이다.
        /// </summary>
        // ------------------------------------------------------------
        PresentationVisibility Visibility { get; }
    }
}
