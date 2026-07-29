/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : UGUIModalDriver.cs
수정일 : 2026-07-29

# 설명
Modal Stack 상단 여부를 UGUI CanvasGroup 상호작용과 선택적 Dim Root에 적용한다.
========================================================================= BLOCK_HEADER_END */

using System;

using UnityEngine;

namespace inonego.Xeri.UI.Game
{
    // ============================================================
    /// <summary>
    /// UGUI Modal 상단 상태 backend.
    /// </summary>
    // ============================================================
    public sealed class UGUIModalDriver : MonoBehaviour, IModalDriver
    {
    #region 필드

        [SerializeField]
        private CanvasGroup canvasGroup = null;

        [SerializeField]
        private GameObject dimRoot = null;

    #endregion

    #region IModalDriver

        // ------------------------------------------------------------
        /// <summary>
        /// Stack 상단 Modal만 상호작용과 raycast를 받도록 적용한다.
        /// </summary>
        // ------------------------------------------------------------
        public void SetTop(bool isTop)
        {
            if (canvasGroup == null)
            {
                throw new InvalidOperationException("UGUI Modal CanvasGroup이 연결되지 않았습니다.");
            }

            canvasGroup.interactable = isTop;
            canvasGroup.blocksRaycasts = isTop;

            if (dimRoot != null)
            {
                dimRoot.SetActive(isTop);
            }
        }

    #endregion

    }
}
