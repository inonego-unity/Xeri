/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TableGridDefaultCellViews.cs
수정일 : 2026-11-21

# 설명
셀 뷰를 지정하지 않은 열의 기본 셀 뷰(값 규칙 글자 Label, 선택형 칩)와 선택형 칩 톤 규칙을 정의한다.
셀 틀(폭·여백·구분선·선택 표시)은 TableGrid가 그리고, 기본 셀 뷰는 틀 안의 내용만 맡는다.

# 특이사항, 제약사항
기본 셀 뷰는 상태를 두지 않는다. 칩 요소의 지금 톤만 요소의 userData에 둔다.
선택형 칩은 Xeri Core의 Badge를 쓰고, 색은 열의 Choices 순서로 6가지 톤을 돌려 쓴다(USS 클래스).
열 메뉴 필터 편집의 값 목록도 같은 톤 규칙을 쓴다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UIElements;

using inonego;
using inonego.Xeri;
using inonego.Xeri.UI;

namespace inonego.Xeri.UI.TableGrid
{
    // ============================================================
    /// <summary>
    /// 기본 셀 뷰(글자, 선택형 칩)와 칩 톤 규칙.
    /// </summary>
    // ============================================================
    internal static class TableGridDefaultCellViews
    {

    #region 톤

        private const string TONE_CLASS_PREFIX = "xeri-table__chip--tone-";

        private static readonly string[] toneNames =
        {
            "blue", "violet", "teal", "green", "amber", "rose",
        };

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> 값의 톤 이름. 열의 Choices 순서로 정하며 후보에 없으면 null(중립)이다.
        /// <br/> 열 메뉴의 값 목록도 같은 톤을 쓴다.
        /// </summary>
        // ----------------------------------------------------------------------
        public static string GetToneName(TableGridColumn column, string value)
        {
            if (column == null || string.IsNullOrEmpty(value))
            {
                return null;
            }

            var choices = column.Choices;
            for (var index = 0; index < choices.Count; index++)
            {
                if (string.Equals(choices[index], value, StringComparison.Ordinal))
                {
                    return toneNames[index % toneNames.Length];
                }
            }

            return null;
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// 칩 요소의 톤 클래스를 바꾼다. 이전 톤과 같으면 클래스를 건드리지 않는다.
        /// </summary>
        // ----------------------------------------------------------------------
        public static void ApplyTone(VisualElement chip, string toneName)
        {
            var previous = chip.userData as string;
            if (string.Equals(previous, toneName, StringComparison.Ordinal))
            {
                return;
            }

            if (previous != null)
            {
                chip.RemoveFromClassList(TONE_CLASS_PREFIX + previous);
            }

            if (toneName != null)
            {
                chip.AddToClassList(TONE_CLASS_PREFIX + toneName);
            }

            chip.userData = toneName;
        }

    #endregion

    #region 기본 셀 뷰

        // ------------------------------------------------------------
        /// <summary>
        /// 한 줄 글자 요소를 만든다. 넘치면 말줄임한다(USS).
        /// </summary>
        // ------------------------------------------------------------
        public static VisualElement MakeText()
        {
            var label = new Label();
            label.AddToClassList("xeri-table__cell-text");
            return label;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 선택형 칩 요소를 만든다.
        /// </summary>
        // ------------------------------------------------------------
        public static VisualElement MakeChoice()
        {
            var badge = new Badge();
            badge.AddToClassList("xeri-table__chip");
            return badge;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 값 규칙 글자를 연결한다. 칩이면 톤을 바꾸고 빈 값이면 칩을 숨긴다.
        /// </summary>
        // ------------------------------------------------------------
        public static void Bind(VisualElement element, TableGridColumn column, string text)
        {
            text ??= string.Empty;
            if (element is Badge badge)
            {
                var empty = text.Length == 0;
                badge.text = text;
                badge.style.display = empty ? DisplayStyle.None : DisplayStyle.Flex;
                ApplyTone(badge, empty ? null : GetToneName(column, text));
                return;
            }

            if (element is Label label)
            {
                label.text = text;
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 글자를 비운다. 칩 톤 클래스는 다음 연결에서 바꾼다.
        /// </summary>
        // ------------------------------------------------------------
        public static void Unbind(VisualElement element)
        {
            if (element is Label label)
            {
                label.text = string.Empty;
            }
        }

    #endregion

    }
}
