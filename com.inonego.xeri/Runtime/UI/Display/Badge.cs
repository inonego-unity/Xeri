/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : Badge.cs
수정일 : 2026-11-19

# 설명
짧은 상태·분류 표식을 그리는 UI Toolkit 요소. Runtime과 Editor UI에서 함께 쓴다.
폭은 글자에 맞추거나(Fit) 고정할 수 있고(Fixed), 색은 코드 프로퍼티 또는 USS로 바꾼다.

# 특이사항, 제약사항
기본 스타일(Badge.uss)은 요소 자신에 붙으므로 같은 우선순위의 바깥 USS보다 먼저 적용된다.
바깥에서 바꿀 때는 상위 요소에 USS 변수(--xeri-badge-*)를 지정하거나 더 구체적인 선택자를 쓴다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UIElements;

namespace inonego.Xeri
{
    // ============================================================
    /// <summary>
    /// 배지 폭을 정하는 방식.
    /// </summary>
    // ============================================================
    public enum BadgeSizing
    {
        // ------------------------------------------------------------
        /// <summary>
        /// 글자 길이에 맞춰 폭이 바뀐다.
        /// </summary>
        // ------------------------------------------------------------
        Fit,

        // --------------------------------------------------------------------------------
        /// <summary>
        /// --xeri-badge-width 또는 style.width로 정한 폭을 유지하고 넘치는 글자는 줄인다.
        /// </summary>
        // --------------------------------------------------------------------------------
        Fixed,
    }

    // ============================================================
    /// <summary>
    /// 배지의 기본 색 조합.
    /// </summary>
    // ============================================================
    public enum BadgeTone
    {
        // ------------------------------------------------------------
        /// <summary>
        /// 주변 글자색과 옅은 회색 배경.
        /// </summary>
        // ------------------------------------------------------------
        Neutral,

        // ------------------------------------------------------------
        /// <summary>
        /// 강조색 글자와 강조색 배경.
        /// </summary>
        // ------------------------------------------------------------
        Accent,
    }

    // ============================================================
    /// <summary>
    /// 짧은 상태·분류 표식.
    /// </summary>
    // ============================================================
    [UxmlElement]
    public partial class Badge : Label
    {

    #region 필드

        // ------------------------------------------------------------
        /// <summary>
        /// 모든 배지에 붙는 USS 클래스.
        /// </summary>
        // ------------------------------------------------------------
        public static readonly string UssClassName = "xeri-badge";

        // ------------------------------------------------------------
        /// <summary>
        /// 고정 폭 배지에 붙는 USS 클래스.
        /// </summary>
        // ------------------------------------------------------------
        public static readonly string FixedUssClassName = UssClassName + "--fixed";

        // ------------------------------------------------------------
        /// <summary>
        /// 강조 배지에 붙는 USS 클래스.
        /// </summary>
        // ------------------------------------------------------------
        public static readonly string AccentUssClassName = UssClassName + "--accent";

        private const string styleSheetPath = "Xeri/Display/Badge";
        private static StyleSheet styleSheet;

        // ------------------------------------------------------------
        /// <summary>
        /// 폭을 정하는 방식을 반환하거나 바꾼다.
        /// </summary>
        // ------------------------------------------------------------
        [UxmlAttribute]
        public BadgeSizing Sizing
        {
            get => sizing;
            set
            {
                sizing = value;
                EnableInClassList(FixedUssClassName, value == BadgeSizing.Fixed);
            }
        }
        private BadgeSizing sizing;

        // ------------------------------------------------------------
        /// <summary>
        /// 기본 색 조합을 반환하거나 바꾼다.
        /// </summary>
        // ------------------------------------------------------------
        [UxmlAttribute]
        public BadgeTone Tone
        {
            get => tone;
            set
            {
                tone = value;
                EnableInClassList(AccentUssClassName, value == BadgeTone.Accent);
            }
        }
        private BadgeTone tone;

        // ------------------------------------------------------------
        /// <summary>
        /// 코드에서 지정한 글자색을 반환하거나 바꾸며 null이면 USS를 따른다.
        /// </summary>
        // ------------------------------------------------------------
        public Color? TextColor
        {
            get => textColor;
            set
            {
                textColor = value;
                style.color = value.HasValue ? new StyleColor(value.Value) : new StyleColor(StyleKeyword.Null);
            }
        }
        private Color? textColor;

        // ------------------------------------------------------------
        /// <summary>
        /// 코드에서 지정한 배경색을 반환하거나 바꾸며 null이면 USS를 따른다.
        /// </summary>
        // ------------------------------------------------------------
        public Color? BackgroundColor
        {
            get => backgroundColor;
            set
            {
                backgroundColor = value;
                style.backgroundColor = value.HasValue ? new StyleColor(value.Value) : new StyleColor(StyleKeyword.Null);
            }
        }
        private Color? backgroundColor;

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// UXML에서 쓰는 빈 배지를 만든다.
        /// </summary>
        // ------------------------------------------------------------
        public Badge() : this(string.Empty)
        {
            // NONE
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 글자를 가진 배지를 만든다.
        /// </summary>
        // ------------------------------------------------------------
        public Badge(string text) : base(text)
        {
            AddToClassList(UssClassName);
            styleSheets.Add(LoadStyle());
        }

    #endregion

    #region 스타일

        private static StyleSheet LoadStyle()
        {
            if (styleSheet == null)
            {
                styleSheet = Resources.Load<StyleSheet>(styleSheetPath)
                    ?? throw new InvalidOperationException($"Badge 스타일을 찾을 수 없습니다: Resources/{styleSheetPath}.uss");
            }

            return styleSheet;
        }

    #endregion

    }
}
