/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : UGUIScreenStateHandler.cs
수정일 : 2026-07-29

# 설명
MonoBehaviour 기반 Screen이 선택적으로 상속할 수 있는 동기 상태 훅 편의 구현을 제공한다.
========================================================================= BLOCK_HEADER_END */

using UnityEngine;

namespace inonego.Xeri.UI.Game
{
    // ============================================================
    /// <summary>
    /// MonoBehaviour 기반 선택형 Screen 상태 Handler.
    /// </summary>
    // ============================================================
    public abstract class UGUIScreenStateHandler : MonoBehaviour, IScreenStateHandler
    {
    #region IScreenStateHandler

        // ------------------------------------------------------------
        /// <summary>
        /// 열기 Transition 시작 전에 호출된다.
        /// </summary>
        // ------------------------------------------------------------
        public virtual void OnOpening(ScreenStateContext context)
        {
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 열기 Transition 완료 뒤 호출된다.
        /// </summary>
        // ------------------------------------------------------------
        public virtual void OnOpened(ScreenStateContext context)
        {
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 닫기 Transition 시작 전에 호출된다.
        /// </summary>
        // ------------------------------------------------------------
        public virtual void OnClosing(ScreenStateContext context)
        {
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 닫기 Transition과 하위 표시 정리 뒤 호출된다.
        /// </summary>
        // ------------------------------------------------------------
        public virtual void OnClosed(ScreenStateContext context)
        {
        }

    #endregion

    }
}
