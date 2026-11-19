/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : CollectionIconButton.cs
수정일 : 2026-11-19

# 설명
컬렉션 표의 셀·행에 놓는 20px 아이콘 버튼을 만든다(상세 편집, 작업 메뉴, 행 삭제 등).
크기·여백·호버는 CollectionTableStyle.uss의 .xeri-collection__icon-button이 정한다.
========================================================================= BLOCK_HEADER_END */

using System;

using UnityEditor;
using UnityEditor.UIElements;

using inonego;
using inonego.Xeri;

namespace inonego.Xeri.Serializable.Editor
{
    // ============================================================
    /// <summary>
    /// 컬렉션 표의 아이콘 버튼 생성.
    /// </summary>
    // ============================================================
    internal static class CollectionIconButton
    {

    #region 생성

        // --------------------------------------------------------------------------------
        /// <summary>
        /// 아이콘 버튼을 만든다. 아이콘을 행마다 바꾸는 버튼은 icon 없이 만든 뒤 바인딩 때 지정한다.
        /// </summary>
        // --------------------------------------------------------------------------------
        public static ToolbarButton Create(Action clicked, string icon = null, string tooltip = null)
        {
            var button = new ToolbarButton(clicked);
            button.AddToClassList("xeri-collection__icon-button");
            if (icon != null)
            {
                button.iconImage = XeriIcon.LoadBackground(icon);
            }

            button.tooltip = tooltip;
            return button;
        }

    #endregion

    }
}
