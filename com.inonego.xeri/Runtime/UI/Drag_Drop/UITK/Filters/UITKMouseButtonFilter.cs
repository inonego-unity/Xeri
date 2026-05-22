/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : UITKMouseButtonFilter.cs
수정일 : 2026-05-22

# 설명
UI Toolkit PointerDownEvent 버튼 값으로 드래그 시작 가능 여부를 판단한다.
========================================================================= BLOCK_HEADER_END */

using UnityEngine.UIElements;

namespace inonego.Xeri.UI.DragDrop
{
    // ============================================================
    /// <summary>
    /// UI Toolkit 마우스 버튼 드래그 필터.
    /// </summary>
    // ============================================================
    public sealed class UITKMouseButtonFilter : IDragInputFilter<PointerDownEvent>
    {

    #region 필드

        // ------------------------------------------------------------
        /// <summary>
        /// 드래그를 허용할 버튼.
        /// </summary>
        // ------------------------------------------------------------
        public int Button
        {
            get => button;
            set => button = value;
        }

        private int button;

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// UI Toolkit 마우스 버튼 드래그 필터를 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        public UITKMouseButtonFilter(int button = 0) : base()
        {
            this.button = button;
        }

    #endregion

    #region 메서드

        // ------------------------------------------------------------
        /// <summary>
        /// 입력 버튼이 드래그 허용 버튼과 일치하는지 확인한다.
        /// </summary>
        // ------------------------------------------------------------
        public bool CanDrag(PointerDownEvent input)
        {
            return input != null && input.button == button;
        }

    #endregion

    }
}
