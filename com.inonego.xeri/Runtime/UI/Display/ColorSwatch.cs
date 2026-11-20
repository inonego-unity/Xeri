/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : ColorSwatch.cs
수정일 : 2026-11-19

# 설명
색 하나를 작은 견본으로 보이는 UI Toolkit 요소. Runtime과 Editor UI에서 함께 쓴다.
크기·테두리·모서리는 USS 변수(--xeri-color-swatch-*)로, 색은 Color 프로퍼티나 UXML 속성으로 정한다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UIElements;

namespace inonego.Xeri.UI
{
    // ============================================================
    /// <summary>
    /// 색 하나를 보이는 작은 견본.
    /// </summary>
    // ============================================================
    [UxmlElement]
    public partial class ColorSwatch : VisualElement
    {

    #region 필드

        // ------------------------------------------------------------
        /// <summary>
        /// 모든 색 견본에 붙는 USS 클래스.
        /// </summary>
        // ------------------------------------------------------------
        public static readonly string UssClassName = "xeri-color-swatch";

        private const string styleSheetPath = "Xeri/UI/Display/ColorSwatch";
        private static StyleSheet styleSheet;

        // ------------------------------------------------------------
        /// <summary>
        /// 보이는 색을 반환하거나 바꾼다.
        /// </summary>
        // ------------------------------------------------------------
        [UxmlAttribute]
        public Color Color
        {
            get => color;
            set
            {
                color = value;
                style.backgroundColor = value;
            }
        }
        private Color color = Color.clear;

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// UXML에서 쓰는 투명 견본을 만든다.
        /// </summary>
        // ------------------------------------------------------------
        public ColorSwatch() : this(Color.clear)
        {
            // NONE
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 지정한 색의 견본을 만든다.
        /// </summary>
        // ------------------------------------------------------------
        public ColorSwatch(Color color)
        {
            AddToClassList(UssClassName);
            styleSheets.Add(LoadStyle());
            Color = color;
        }

    #endregion

    #region 스타일

        private static StyleSheet LoadStyle()
        {
            if (styleSheet == null)
            {
                styleSheet = Resources.Load<StyleSheet>(styleSheetPath)
                    ?? throw new InvalidOperationException($"ColorSwatch 스타일을 찾을 수 없습니다: Resources/{styleSheetPath}.uss");
            }

            return styleSheet;
        }

    #endregion

    }
}
