/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TableGridFormats.cs
수정일 : 2026-11-22

# 설명
TableGrid 기본 값 규칙(글자, 숫자, 정수, 숫자 글자, 선택형, 기본)을 만드는 진입점과 그 구현을 정의한다.

# 특이사항, 제약사항
숫자는 현재 문화권으로 쓰고, 읽을 때는 현재 문화권과 고정 문화권(InvariantCulture)을 모두 받는다.
숫자 글자(NumberText)는 숫자를 담은 string 값이다. 빈 글자는 받고(비워도 되는지는 호스트가 정한다), 비교는 숫자로 한다.
선택형은 후보 중 하나나 빈 글자만 받고, 후보 순서로 비교한다.
기본(Default)은 ToString으로만 나타내며 글자에서 읽지 않는다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;

namespace inonego.Xeri.UI.TableGrid
{
    // ============================================================
    /// <summary>
    /// TableGrid 기본 값 규칙을 만든다.
    /// </summary>
    // ============================================================
    public static class TableGridFormats
    {

    #region 기본 값 규칙

        // ------------------------------------------------------------
        /// <summary>
        /// 글자 값 규칙. 어떤 글자든 받고 지우면 빈 글자다.
        /// </summary>
        // ------------------------------------------------------------
        public static TableGridValueFormat<string> Text { get; } = new TableGridTextFormat();

        // ------------------------------------------------------------
        /// <summary>
        /// 숫자를 담은 글자 값 규칙. 빈 글자나 유한한 숫자만 받는다.
        /// </summary>
        // ------------------------------------------------------------
        public static TableGridValueFormat<string> NumberText { get; } = new TableGridNumberTextFormat();

        // ----------------------------------------------------------------------
        /// <summary>
        /// 실수 값 규칙. format은 표시 서식(예: "N0")이고 null이면 기본 서식이다.
        /// </summary>
        // ----------------------------------------------------------------------
        public static TableGridValueFormat<double> Number(string format = null)
        {
            return new TableGridNumberFormat(format);
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// 정수 값 규칙. format은 표시 서식(예: "N0")이고 null이면 기본 서식이다.
        /// </summary>
        // ----------------------------------------------------------------------
        public static TableGridValueFormat<int> Integer(string format = null)
        {
            return new TableGridIntegerFormat(format);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 선택형 값 규칙. 후보 중 하나나 빈 글자만 받는다.
        /// </summary>
        // ------------------------------------------------------------
        public static TableGridValueFormat<string> Choice(IReadOnlyList<string> choices)
        {
            return new TableGridChoiceFormat(choices);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// ToString으로만 나타내고 글자에서 읽지 않는 기본 값 규칙.
        /// </summary>
        // ------------------------------------------------------------
        public static TableGridValueFormat<T> Default<T>()
        {
            return TableGridDefaultFormat<T>.Shared;
        }

    #endregion

    }

    #region 구현

    // ============================================================
    /// <summary>
    /// 글자 값 규칙.
    /// </summary>
    // ============================================================
    internal sealed class TableGridTextFormat : TableGridValueFormat<string>
    {
        // ------------------------------------------------------------
        /// <summary>
        /// 지우기를 받는다.
        /// </summary>
        // ------------------------------------------------------------
        public override bool CanClear => true;

        // ------------------------------------------------------------
        /// <summary>
        /// 지울 때 값은 빈 글자다.
        /// </summary>
        // ------------------------------------------------------------
        public override string ClearValue => string.Empty;

        // ------------------------------------------------------------
        /// <summary>
        /// 글자를 그대로 보인다.
        /// </summary>
        // ------------------------------------------------------------
        public override string ToText(string value)
        {
            return value ?? string.Empty;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 글자에서 값을 읽을 수 있다.
        /// </summary>
        // ------------------------------------------------------------
        public override bool CanParse => true;

        // ------------------------------------------------------------
        /// <summary>
        /// 글자를 그대로 읽는다.
        /// </summary>
        // ------------------------------------------------------------
        public override bool TryParse(string text, out string value, out string error)
        {
            value = text ?? string.Empty;
            error = null;
            return true;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 문화권 대소문자 무시로 비교한다.
        /// </summary>
        // ------------------------------------------------------------
        public override int Compare(string left, string right)
        {
            return string.Compare(left, right, StringComparison.CurrentCultureIgnoreCase);
        }
    }

    // ============================================================
    /// <summary>
    /// 숫자 읽기 공통 규칙.
    /// </summary>
    // ============================================================
    internal static class TableGridNumberParsing
    {
        // ------------------------------------------------------------
        /// <summary>
        /// 숫자를 입력하라는 안내 문구.
        /// </summary>
        // ------------------------------------------------------------
        public const string NUMBER_REQUIRED = "숫자를 입력하세요";

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 문화권, 그다음 고정 문화권으로 유한한 실수를 읽는다.
        /// </summary>
        // ------------------------------------------------------------
        public static bool TryParseDouble(string text, out double value)
        {
            var style = NumberStyles.Float | NumberStyles.AllowThousands;
            var parsed = double.TryParse(text, style, CultureInfo.CurrentCulture, out value)
                || double.TryParse(text, style, CultureInfo.InvariantCulture, out value);
            return parsed && IsFinite(value);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 문화권, 그다음 고정 문화권으로 정수를 읽는다.
        /// </summary>
        // ------------------------------------------------------------
        public static bool TryParseInt(string text, out int value)
        {
            var style = NumberStyles.Integer | NumberStyles.AllowThousands;
            return int.TryParse(text, style, CultureInfo.CurrentCulture, out value)
                || int.TryParse(text, style, CultureInfo.InvariantCulture, out value);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// NaN·무한대가 아닌지 확인한다.
        /// </summary>
        // ------------------------------------------------------------
        public static bool IsFinite(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }
    }

    // ============================================================
    /// <summary>
    /// 실수 값 규칙.
    /// </summary>
    // ============================================================
    internal sealed class TableGridNumberFormat : TableGridValueFormat<double>
    {

    #region 필드

        private readonly string format;

    #endregion

    #region 값 규칙

        // ------------------------------------------------------------
        /// <summary>
        /// 표시 서식으로 값 규칙을 만든다.
        /// </summary>
        // ------------------------------------------------------------
        public TableGridNumberFormat(string format)
        {
            this.format = format;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 숫자 입력 종류.
        /// </summary>
        // ------------------------------------------------------------
        public override TableGridColumnValueKind Kind => TableGridColumnValueKind.Number;

        // ------------------------------------------------------------
        /// <summary>
        /// 표시 서식과 현재 문화권으로 나타낸다.
        /// </summary>
        // ------------------------------------------------------------
        public override string ToText(double value)
        {
            return value.ToString(format, CultureInfo.CurrentCulture);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 글자에서 값을 읽을 수 있다.
        /// </summary>
        // ------------------------------------------------------------
        public override bool CanParse => true;

        // ------------------------------------------------------------
        /// <summary>
        /// 유한한 실수를 읽는다.
        /// </summary>
        // ------------------------------------------------------------
        public override bool TryParse(string text, out double value, out string error)
        {
            error = TableGridNumberParsing.TryParseDouble(text?.Trim(), out value) ? null : TableGridNumberParsing.NUMBER_REQUIRED;
            return error == null;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// NaN·무한대를 거부한다.
        /// </summary>
        // ------------------------------------------------------------
        public override bool Validate(double value, out string error)
        {
            error = TableGridNumberParsing.IsFinite(value) ? null : TableGridNumberParsing.NUMBER_REQUIRED;
            return error == null;
        }

    #endregion

    }

    // ============================================================
    /// <summary>
    /// 정수 값 규칙.
    /// </summary>
    // ============================================================
    internal sealed class TableGridIntegerFormat : TableGridValueFormat<int>
    {

    #region 필드

        private readonly string format;

    #endregion

    #region 값 규칙

        // ------------------------------------------------------------
        /// <summary>
        /// 표시 서식으로 값 규칙을 만든다.
        /// </summary>
        // ------------------------------------------------------------
        public TableGridIntegerFormat(string format)
        {
            this.format = format;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 숫자 입력 종류.
        /// </summary>
        // ------------------------------------------------------------
        public override TableGridColumnValueKind Kind => TableGridColumnValueKind.Number;

        // ------------------------------------------------------------
        /// <summary>
        /// 표시 서식과 현재 문화권으로 나타낸다.
        /// </summary>
        // ------------------------------------------------------------
        public override string ToText(int value)
        {
            return value.ToString(format, CultureInfo.CurrentCulture);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 글자에서 값을 읽을 수 있다.
        /// </summary>
        // ------------------------------------------------------------
        public override bool CanParse => true;

        // ------------------------------------------------------------
        /// <summary>
        /// 정수를 읽는다.
        /// </summary>
        // ------------------------------------------------------------
        public override bool TryParse(string text, out int value, out string error)
        {
            error = TableGridNumberParsing.TryParseInt(text?.Trim(), out value) ? null : "정수를 입력하세요";
            return error == null;
        }

    #endregion

    }

    // ============================================================
    /// <summary>
    /// 숫자를 담은 글자 값 규칙.
    /// </summary>
    // ============================================================
    internal sealed class TableGridNumberTextFormat : TableGridValueFormat<string>
    {
        // ------------------------------------------------------------
        /// <summary>
        /// 숫자 입력 종류.
        /// </summary>
        // ------------------------------------------------------------
        public override TableGridColumnValueKind Kind => TableGridColumnValueKind.Number;

        // ------------------------------------------------------------
        /// <summary>
        /// 지우기를 받는다.
        /// </summary>
        // ------------------------------------------------------------
        public override bool CanClear => true;

        // ------------------------------------------------------------
        /// <summary>
        /// 지울 때 값은 빈 글자다.
        /// </summary>
        // ------------------------------------------------------------
        public override string ClearValue => string.Empty;

        // ------------------------------------------------------------
        /// <summary>
        /// 글자를 그대로 보인다.
        /// </summary>
        // ------------------------------------------------------------
        public override string ToText(string value)
        {
            return value ?? string.Empty;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 글자에서 값을 읽을 수 있다.
        /// </summary>
        // ------------------------------------------------------------
        public override bool CanParse => true;

        // ------------------------------------------------------------
        /// <summary>
        /// 빈 글자나 유한한 숫자 글자를 그대로 읽는다.
        /// </summary>
        // ------------------------------------------------------------
        public override bool TryParse(string text, out string value, out string error)
        {
            value = text ?? string.Empty;
            return Validate(value, out error);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 빈 글자나 유한한 숫자 글자만 받는다.
        /// </summary>
        // ------------------------------------------------------------
        public override bool Validate(string value, out string error)
        {
            var valid = string.IsNullOrWhiteSpace(value) || TableGridNumberParsing.TryParseDouble(value.Trim(), out _);
            error = valid ? null : TableGridNumberParsing.NUMBER_REQUIRED;
            return valid;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 둘 다 숫자로 읽히면 숫자로, 아니면 서수 글자로 비교한다.
        /// </summary>
        // ------------------------------------------------------------
        public override int Compare(string left, string right)
        {
            var leftNumber = TableGridNumberParsing.TryParseDouble(left, out var a);
            var rightNumber = TableGridNumberParsing.TryParseDouble(right, out var b);
            return leftNumber && rightNumber ? a.CompareTo(b) : string.CompareOrdinal(left, right);
        }
    }

    // ============================================================
    /// <summary>
    /// 선택형 값 규칙.
    /// </summary>
    // ============================================================
    internal sealed class TableGridChoiceFormat : TableGridValueFormat<string>
    {

    #region 값 규칙

        // ------------------------------------------------------------
        /// <summary>
        /// 빈 글자를 뺀 후보 목록을 복사해 값 규칙을 만든다.
        /// </summary>
        // ------------------------------------------------------------
        public TableGridChoiceFormat(IReadOnlyList<string> choices)
        {
            var copy = new List<string>();
            if (choices != null)
            {
                foreach (var choice in choices)
                {
                    if (!string.IsNullOrEmpty(choice))
                    {
                        copy.Add(choice);
                    }
                }
            }

            this.choices = new ReadOnlyCollection<string>(copy);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 선택형 입력 종류.
        /// </summary>
        // ------------------------------------------------------------
        public override TableGridColumnValueKind Kind => TableGridColumnValueKind.Choice;

        // ------------------------------------------------------------
        /// <summary>
        /// 값 후보. 칩 색은 이 순서로 정한다.
        /// </summary>
        // ------------------------------------------------------------
        public override IReadOnlyList<string> Choices => choices;
        private readonly ReadOnlyCollection<string> choices;

        // ------------------------------------------------------------
        /// <summary>
        /// 지우기를 받는다.
        /// </summary>
        // ------------------------------------------------------------
        public override bool CanClear => true;

        // ------------------------------------------------------------
        /// <summary>
        /// 지울 때 값은 빈 글자다.
        /// </summary>
        // ------------------------------------------------------------
        public override string ClearValue => string.Empty;

        // ------------------------------------------------------------
        /// <summary>
        /// 글자를 그대로 보인다.
        /// </summary>
        // ------------------------------------------------------------
        public override string ToText(string value)
        {
            return value ?? string.Empty;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 글자에서 값을 읽을 수 있다.
        /// </summary>
        // ------------------------------------------------------------
        public override bool CanParse => true;

        // ------------------------------------------------------------
        /// <summary>
        /// 후보 중 하나나 빈 글자를 읽는다.
        /// </summary>
        // ------------------------------------------------------------
        public override bool TryParse(string text, out string value, out string error)
        {
            value = text ?? string.Empty;
            return Validate(value, out error);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 후보 중 하나나 빈 글자만 받는다. 후보가 없으면 모두 받는다.
        /// </summary>
        // ------------------------------------------------------------
        public override bool Validate(string value, out string error)
        {
            var valid = string.IsNullOrWhiteSpace(value) || choices.Count == 0 || IndexOf(value) >= 0;
            error = valid ? null : "후보에 없는 값입니다";
            return valid;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 후보 순서로 비교한다. 후보에 없는 값은 뒤에 두고 서수 글자로 비교한다.
        /// </summary>
        // ------------------------------------------------------------
        public override int Compare(string left, string right)
        {
            var leftIndex = IndexOf(left);
            var rightIndex = IndexOf(right);
            if (leftIndex >= 0 && rightIndex >= 0)
            {
                return leftIndex.CompareTo(rightIndex);
            }

            if (leftIndex >= 0 || rightIndex >= 0)
            {
                return leftIndex >= 0 ? -1 : 1;
            }

            return string.CompareOrdinal(left, right);
        }

        private int IndexOf(string value)
        {
            for (var index = 0; index < choices.Count; index++)
            {
                if (string.Equals(choices[index], value, StringComparison.Ordinal))
                {
                    return index;
                }
            }

            return -1;
        }

    #endregion

    }

    // ============================================================
    /// <summary>
    /// ToString으로만 나타내는 기본 값 규칙.
    /// </summary>
    // ============================================================
    internal sealed class TableGridDefaultFormat<T> : TableGridValueFormat<T>
    {
        // ------------------------------------------------------------
        /// <summary>
        /// 형식마다 하나인 공유 인스턴스.
        /// </summary>
        // ------------------------------------------------------------
        public static TableGridDefaultFormat<T> Shared { get; } = new();

        // ------------------------------------------------------------
        /// <summary>
        /// ToString으로 나타낸다. null이면 빈 글자다.
        /// </summary>
        // ------------------------------------------------------------
        public override string ToText(T value)
        {
            return value == null ? string.Empty : value.ToString() ?? string.Empty;
        }
    }

    #endregion

}
