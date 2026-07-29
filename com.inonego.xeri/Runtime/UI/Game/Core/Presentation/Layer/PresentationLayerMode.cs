/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : PresentationLayerMode.cs
수정일 : 2026-07-29

# 설명
게임 UI Layer가 상위 Canvas를 공유하는지 독립 정렬 Canvas를 사용하는지 정의한다.
========================================================================= BLOCK_HEADER_END */

namespace inonego.Xeri.UI.Game
{
    // ============================================================
    /// <summary>
    /// 게임 UI Layer의 Canvas 구성 방식.
    /// </summary>
    // ============================================================
    public enum PresentationLayerMode
    {
        Shared = 0,
        Independent = 1,
    }
}
