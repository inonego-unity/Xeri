/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : UGUISceneFadeDriver.cs
수정일 : 2026-07-29

# 설명
UGUI Image와 CanvasGroup으로 Scene Fade 색상과 Alpha를 적용한다.
========================================================================= BLOCK_HEADER_END */

using UnityEngine;
using UnityEngine.UI;

namespace inonego.Xeri.UI.Game
{
    // ============================================================
    /// <summary>
    /// UGUI Scene Fade 표시 backend.
    /// </summary>
    // ============================================================
    public sealed class UGUISceneFadeDriver : MonoBehaviour, ISceneFadeDriver
    {
    #region 필드

        // ------------------------------------------------------------
        /// <summary>
        /// Fade backend 참조가 유효한지 여부.
        /// </summary>
        // ------------------------------------------------------------
        public bool IsValid => image != null && canvasGroup != null;

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 Fade Alpha.
        /// </summary>
        // ------------------------------------------------------------
        public float Alpha => canvasGroup != null ? canvasGroup.alpha : 0.0f;

        [SerializeField]
        private Image image = null;

        [SerializeField]
        private CanvasGroup canvasGroup = null;

    #endregion

    #region ISceneFadeDriver

        // ------------------------------------------------------------
        /// <summary>
        /// Fade Image 색상을 적용한다.
        /// </summary>
        // ------------------------------------------------------------
        public void SetColor(Color color)
        {
            if (image != null)
            {
                image.color = color;
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Transition 값을 Fade CanvasGroup alpha로 적용한다.
        /// </summary>
        // ------------------------------------------------------------
        public void Apply(float value)
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = Mathf.Clamp01(value);
            }
        }

    #endregion

    }
}
