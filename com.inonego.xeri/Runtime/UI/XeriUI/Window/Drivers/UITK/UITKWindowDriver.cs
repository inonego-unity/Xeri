/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : UITKWindowDriver.cs
수정일 : 2026-05-23

# 설명
Xeri 커스텀 윈도우 상태를 UITK VisualElement style에 반영하는 driver.
========================================================================= BLOCK_HEADER_END */

using UnityEngine;
using UnityEngine.UIElements;

namespace inonego.Xeri.UI.Window
{
    // ============================================================
    /// <summary>
    /// UITK VisualElement 기반 Xeri 윈도우 driver.
    /// </summary>
    // ============================================================
    public sealed class UITKWindowDriver : IXeriWindowDriver
    {

    #region 필드

        private readonly VisualElement target = null;

        private Vector2 pos = Vector2.zero;
        private Vector2 size = new Vector2(200f, 120f);
        private Vector2 normalPos = Vector2.zero;
        private Vector2 normalSize = new Vector2(200f, 120f);
        private XeriWindowState state = XeriWindowState.Normal;

    #endregion

    #region 프로퍼티

        // ------------------------------------------------------------
        /// <summary>
        /// 윈도우 위치.
        /// </summary>
        // ------------------------------------------------------------
        public Vector2 Pos
        {
            get => pos;
            set
            {
                pos = value;
                ApplyBounds();
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 윈도우 크기.
        /// </summary>
        // ------------------------------------------------------------
        public Vector2 Size
        {
            get => size;
            set
            {
                size = value;
                ApplyBounds();
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 윈도우 표시 상태.
        /// </summary>
        // ------------------------------------------------------------
        public XeriWindowState State
        {
            get => state;
            set
            {
                if (state == value) return;

                var previous = state;

                if (value == XeriWindowState.Maximized)
                {
                    normalPos  = pos;
                    normalSize = size;
                }

                if (previous == XeriWindowState.Maximized && value == XeriWindowState.Normal)
                {
                    pos  = normalPos;
                    size = normalSize;
                }

                state = value;
                ApplyState(previous, state);
                ApplyBounds();
            }
        }

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// UITK window driver를 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        public UITKWindowDriver(VisualElement target) : base()
        {
            this.target = target;

            ApplyBounds();
            ApplyState(state, state);
        }

    #endregion

    #region 내부 메서드

        // ------------------------------------------------------------
        /// <summary>
        /// 위치와 크기를 대상 style에 반영한다.
        /// </summary>
        // ------------------------------------------------------------
        private void ApplyBounds()
        {
            if (target == null) return;

            if (state == XeriWindowState.Maximized)
            {
                ApplyMaximizedBounds();
                return;
            }

            target.style.left = pos.x;
            target.style.top = pos.y;
            target.style.right = StyleKeyword.Auto;
            target.style.bottom = StyleKeyword.Auto;
            target.style.width = size.x;
            target.style.height = size.y;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 대상이 부모 영역을 채우도록 최대화 영역을 반영한다.
        /// </summary>
        // ------------------------------------------------------------
        private void ApplyMaximizedBounds()
        {
            target.style.left = 0f;
            target.style.top = 0f;
            target.style.right = 0f;
            target.style.bottom = 0f;
            target.style.width = StyleKeyword.Auto;
            target.style.height = StyleKeyword.Auto;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 상태 클래스와 표시 여부를 대상에 반영한다.
        /// </summary>
        // ------------------------------------------------------------
        private void ApplyState(XeriWindowState previous, XeriWindowState next)
        {
            if (target == null) return;

            target.RemoveFromClassList(GetStateClass(previous));
            target.AddToClassList(GetStateClass(next));

            target.style.display = next == XeriWindowState.Minimized ||
                                   next == XeriWindowState.Closed
                ? DisplayStyle.None
                : DisplayStyle.Flex;

            if (target is XeriWindowPanel panel)
            {
                panel.ApplyState(next);
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 상태에 대응하는 USS class 이름을 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        private static string GetStateClass(XeriWindowState state)
        {
            return state switch
            {
                XeriWindowState.Minimized => "xeri-window--minimized",
                XeriWindowState.Maximized => "xeri-window--maximized",
                XeriWindowState.Closed    => "xeri-window--closed",
                _                         => "xeri-window--normal",
            };
        }

    #endregion

    }
}
