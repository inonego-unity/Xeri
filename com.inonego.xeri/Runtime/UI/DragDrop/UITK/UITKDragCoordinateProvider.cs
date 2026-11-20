/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : UITKDragCoordinateProvider.cs
수정일 : 2026-05-22

# 설명
UI Toolkit VisualElement 좌표를 Core 드래그 좌표 Provider 로 연결한다.
========================================================================= BLOCK_HEADER_END */

using UnityEngine;
using UnityEngine.UIElements;

namespace inonego.Xeri.UI.DragDrop
{
    // ============================================================
    /// <summary>
    /// UI Toolkit 드래그 좌표 Provider.
    /// </summary>
    // ============================================================
    public sealed class UITKDragCoordinateProvider : IDragCoordinateProvider
    {

    #region 필드

        // ------------------------------------------------------------
        /// <summary>
        /// 좌표를 읽고 쓸 VisualElement.
        /// </summary>
        // ------------------------------------------------------------
        private readonly VisualElement target;

        // ------------------------------------------------------------
        /// <summary>
        /// 위치 적용을 위해 absolute position 을 강제할지 여부.
        /// </summary>
        // ------------------------------------------------------------
        public bool ForceAbsolutePosition
        {
            get => forceAbsolutePosition;
            set
            {
                forceAbsolutePosition = value;
                ApplyPositionMode();
            }
        }

        private bool forceAbsolutePosition;

        // ------------------------------------------------------------
        /// <summary>
        /// VisualElement 의 드래그 기준 위치.
        /// </summary>
        // ------------------------------------------------------------
        public Vector2 Pos
        {
            get
            {
                if (target == null) return Vector2.zero;

                var x = float.IsNaN(target.resolvedStyle.left)
                    ? target.layout.x
                    : target.resolvedStyle.left;
                var y = float.IsNaN(target.resolvedStyle.top)
                    ? target.layout.y
                    : target.resolvedStyle.top;

                return new Vector2(x, y);
            }
            set
            {
                if (target == null) return;

                target.style.left = value.x;
                target.style.top  = value.y;
            }
        }

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// UI Toolkit 좌표 Provider 를 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        public UITKDragCoordinateProvider
        (
            VisualElement target,
            bool forceAbsolutePosition = true
        ) : base()
        {
            this.target           = target;
            this.forceAbsolutePosition = forceAbsolutePosition;

            ApplyPositionMode();
        }

    #endregion

    #region 메서드

        // ------------------------------------------------------------
        /// <summary>
        /// Panel 좌표 입력을 부모 VisualElement 기준 좌표로 변환한다.
        /// </summary>
        // ------------------------------------------------------------
        public Vector2 ToLocalPos(Vector2 inputPos)
        {
            if (target == null) return Vector2.zero;
            if (target.parent == null) return inputPos;

            return target.parent.WorldToLocal(inputPos);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 위치 적용 방식이 absolute position 이 되도록 설정한다.
        /// </summary>
        // ------------------------------------------------------------
        private void ApplyPositionMode()
        {
            if (target == null) return;
            if (!forceAbsolutePosition) return;

            target.style.position = Position.Absolute;
        }

    #endregion

    }
}
