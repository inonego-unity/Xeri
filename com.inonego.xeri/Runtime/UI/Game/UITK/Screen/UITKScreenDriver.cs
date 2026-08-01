/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : UITKScreenDriver.cs
수정일 : 2026-07-31

# 설명
UI Toolkit Screen Root의 표시, 상호작용, Focus와 Transition 값을 Core Screen 계약에 연결한다.
========================================================================= BLOCK_HEADER_END */

using System;

using UnityEngine;
using UnityEngine.UIElements;

namespace inonego.Xeri.UI.Game
{
    // ============================================================
    /// <summary>
    /// UI Toolkit Screen 표시 backend.
    /// </summary>
    // ============================================================
    public sealed class UITKScreenDriver : IScreenDriver, IVisibilityTarget
    {
    #region 필드

        // ------------------------------------------------------------
        /// <summary>
        /// Screen Root가 현재 Visual Tree에 연결돼 있는지 여부.
        /// </summary>
        // ------------------------------------------------------------
        public bool IsValid => root != null && root.parent != null;

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 Screen 표시 진행 값.
        /// </summary>
        // ------------------------------------------------------------
        public float Visibility => root.resolvedStyle.opacity;

        // ------------------------------------------------------------
        /// <summary>
        /// Screen의 기본 Focus Element.
        /// </summary>
        // ------------------------------------------------------------
        public object DefaultFocus => defaultFocus;

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 Screen Root 표시 상태.
        /// </summary>
        // ------------------------------------------------------------
        public bool IsVisible => root.resolvedStyle.display != DisplayStyle.None;

        private readonly VisualElement root = null;
        private readonly VisualElement defaultFocus = null;

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// Visual Tree에 추가된 Screen Root와 선택적 기본 Focus를 연결한다.
        /// </summary>
        // ------------------------------------------------------------
        public UITKScreenDriver
        (
            VisualElement root,
            VisualElement defaultFocus = null
        ) : base()
        {
            this.root = root ?? throw new ArgumentNullException(nameof(root));
            this.defaultFocus = defaultFocus;
        }

    #endregion

    #region IScreenDriver

        // ------------------------------------------------------------
        /// <summary>
        /// Screen Root의 layout 표시 상태를 적용한다.
        /// </summary>
        // ------------------------------------------------------------
        public void SetVisible(bool visible)
        {
            root.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Screen Root의 enabled 상태와 picking 정책을 적용한다.
        /// </summary>
        // ------------------------------------------------------------
        public void SetInteractable(bool interactable)
        {
            root.SetEnabled(interactable);
            root.pickingMode = interactable ? PickingMode.Position : PickingMode.Ignore;
        }

    #endregion

    #region IPresentationTransitionTarget

        // ------------------------------------------------------------
        /// <summary>
        /// Transition 값을 Screen Root opacity로 적용한다.
        /// </summary>
        // ------------------------------------------------------------
        public void Apply(float value)
        {
            root.style.opacity = Mathf.Clamp01(value);
        }

    #endregion

    }
}
