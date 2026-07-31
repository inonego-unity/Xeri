/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : UITKSceneFadeDriver.cs
수정일 : 2026-07-31

# 설명
UI Toolkit VisualElement에 Scene Fade 색상, opacity와 입력 차단을 적용한다.
========================================================================= BLOCK_HEADER_END */

using System;

using UnityEngine;
using UnityEngine.UIElements;

namespace inonego.Xeri.UI.Game
{
    // ============================================================
    /// <summary>
    /// UI Toolkit Scene Fade 표시 backend.
    /// </summary>
    // ============================================================
    public sealed class UITKSceneFadeDriver : ISceneFadeDriver
    {
    #region 필드

        // ------------------------------------------------------------
        /// <summary>
        /// Fade Root가 현재 Visual Tree에 연결돼 있는지 여부.
        /// </summary>
        // ------------------------------------------------------------
        public bool IsValid => root != null && viewRoot != null && viewRoot.parent != null;

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 Fade opacity.
        /// </summary>
        // ------------------------------------------------------------
        public float Alpha => alpha;

        private readonly VisualElement root = null;
        private readonly VisualElement viewRoot = null;
        private float alpha = 0.0f;

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// Visual Tree에 추가된 Fade Root를 연결한다.
        /// </summary>
        // ------------------------------------------------------------
        public UITKSceneFadeDriver(VisualElement root) : this(root, root)
        {
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Fade 표시 Root와 Source가 Visual Tree에 추가한 View Root를 연결한다.
        /// </summary>
        // ------------------------------------------------------------
        internal UITKSceneFadeDriver
        (
            VisualElement root,
            VisualElement viewRoot
        ) : base()
        {
            this.root = root ?? throw new ArgumentNullException(nameof(root));
            this.viewRoot = viewRoot ?? throw new ArgumentNullException(nameof(viewRoot));
            this.root.pickingMode = PickingMode.Position;
        }

    #endregion

    #region ISceneFadeDriver

        // ------------------------------------------------------------
        /// <summary>
        /// Fade Root의 배경 색상을 적용한다.
        /// </summary>
        // ------------------------------------------------------------
        public void SetColor(Color color)
        {
            root.style.backgroundColor = color;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Transition 값을 Fade Root opacity로 적용한다.
        /// </summary>
        // ------------------------------------------------------------
        public void Apply(float value)
        {
            alpha = Mathf.Clamp01(value);
            root.style.opacity = alpha;
        }

    #endregion

    }
}
