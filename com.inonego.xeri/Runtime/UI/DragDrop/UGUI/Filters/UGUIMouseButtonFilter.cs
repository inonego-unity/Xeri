/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : UGUIMouseButtonFilter.cs
수정일 : 2026-05-22

# 설명
UGUI PointerEventData 버튼 값으로 드래그 시작 가능 여부를 판단한다.
========================================================================= BLOCK_HEADER_END */

using UnityEngine;
using UnityEngine.EventSystems;

namespace inonego.Xeri.UI.DragDrop
{
    // ============================================================
    /// <summary>
    /// UGUI 마우스 버튼 드래그 필터.
    /// </summary>
    // ============================================================
    public sealed class UGUIMouseButtonFilter : IDragInputFilter<PointerEventData>
    {

    #region 필드

        // ------------------------------------------------------------
        /// <summary>
        /// 드래그를 허용할 버튼.
        /// </summary>
        // ------------------------------------------------------------
        public PointerEventData.InputButton Button
        {
            get => button;
            set => button = value;
        }

        private PointerEventData.InputButton button;

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// UGUI 마우스 버튼 드래그 필터를 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        public UGUIMouseButtonFilter(PointerEventData.InputButton button) : base()
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
        public bool CanDrag(PointerEventData input)
        {
            return input != null && input.button == button;
        }

    #endregion

    }
}
