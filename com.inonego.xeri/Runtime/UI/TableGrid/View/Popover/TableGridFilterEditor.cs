/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TableGridFilterEditor.cs
수정일 : 2026-11-23

# 설명
열 하나의 필터를 고치는 화면 요소. 열의 값 종류에 따라 편집기가 다르다.
선택형은 전체 + 값 다중 체크, 숫자는 최소값–최대값(비우면 제한 없음), 문자열은 포함·정확히 일치·시작 문자 + 값이다.
아래에 적용과(규칙이 있으면) 초기화를 둔다.

# 특이사항, 제약사항
결과는 Applied로만 알린다. null은 그 열의 필터를 푼다는 뜻이다.
선택형에서 모든 값을 고르면 필터가 없는 것과 같으므로 null을 보낸다. 하나도 고르지 않으면 적용할 수 없다.
숫자 입력은 InvariantCulture로 읽고, 읽을 수 없으면 적용할 수 없다. 비교 자체는 호스트가 한다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;

using UnityEngine;
using UnityEngine.UIElements;

using inonego;
using inonego.Xeri;
using inonego.Xeri.UI;

namespace inonego.Xeri.UI.TableGrid
{
    // ============================================================
    /// <summary>
    /// 열 필터 편집 화면 요소.
    /// </summary>
    // ============================================================
    internal sealed class TableGridFilterEditor : TableGridPopoverPanel
    {

    #region 필드

        private static readonly TableGridFilterOperator[] textOperators =
        {
            TableGridFilterOperator.Contains,
            TableGridFilterOperator.Equals,
            TableGridFilterOperator.StartsWith,
        };

        private static readonly string[] textOperatorNames =
        {
            "포함", "정확히 일치", "시작 문자",
        };

        private readonly HashSet<string> selectedChoices = new(StringComparer.Ordinal);
        private readonly List<(VisualElement Row, string Value)> choiceRows = new();
        private readonly List<VisualElement> operatorButtons = new();

        private TableGridColumn column;
        private TableGridFilterRule current;
        private VisualElement allRow;
        private VisualElement applyButton;
        private TextField minimumField;
        private TextField maximumField;
        private TextField textField;
        private int textOperatorIndex;

    #endregion

    #region 이벤트

        // ------------------------------------------------------------
        /// <summary>
        /// 적용·초기화했을 때 결과 규칙을 알린다. null이면 필터를 푼다.
        /// </summary>
        // ------------------------------------------------------------
        public event Action<TableGridFilterRule> Applied;

    #endregion

    #region 생성과 구성

        // ------------------------------------------------------------
        /// <summary>
        /// 빈 필터 편집기를 만든다. 내용은 Build로 채운다.
        /// </summary>
        // ------------------------------------------------------------
        public TableGridFilterEditor()
        {
            // NONE
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 열의 값 종류와 지금 걸린 규칙으로 편집기를 만든다.
        /// </summary>
        // ------------------------------------------------------------
        public void Build(TableGridColumn column, TableGridFilterRule current)
        {
            this.column = column ?? throw new ArgumentNullException(nameof(column));
            this.current = current;
            ResetContent();
            selectedChoices.Clear();
            choiceRows.Clear();
            operatorButtons.Clear();
            allRow = null;
            applyButton = null;
            minimumField = maximumField = textField = null;

            AddHeader(column.Title, current != null ? "필터 수정" : "필터 추가");
            switch (column.ValueKind)
            {
                case TableGridColumnValueKind.Choice:
                    BuildChoice();
                    break;
                case TableGridColumnValueKind.Number:
                    BuildNumber();
                    break;
                default:
                    BuildText();
                    break;
            }

            BuildActions();
            UpdateApplyState();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 첫 입력칸, 없으면 첫 행에 포커스를 준다.
        /// </summary>
        // ------------------------------------------------------------
        public override void FocusFirst()
        {
            var field = textField ?? minimumField;
            if (field != null)
            {
                field.Focus();
                return;
            }

            base.FocusFirst();
        }

    #endregion

    #region 선택형

        private void BuildChoice()
        {
            var choices = column.Choices;
            if (current != null && current.ValueKind == TableGridColumnValueKind.Choice)
            {
                foreach (var choice in current.Choices)
                {
                    selectedChoices.Add(choice);
                }
            }
            else
            {
                // 규칙이 없으면 모든 값이 보이는 상태와 같다.
                foreach (var choice in choices)
                {
                    selectedChoices.Add(choice);
                }
            }

            var list = new ScrollView(ScrollViewMode.Vertical);
            list.AddToClassList("xeri-table-filter__choices");
            var rows = Math.Min(choices.Count + 1, TableGridPopoverMetrics.MAX_VISIBLE_CHOICES);
            AddSized(list, rows * TableGridPopoverMetrics.CHOICE_ROW_HEIGHT);

            allRow = CreateChoiceRow("전체", ToggleAll);
            list.Add(allRow);
            foreach (var choice in choices)
            {
                var value = choice;
                // 값은 글자 대신 표 셀과 같은 톤의 칩으로 보인다(행에 글자 라벨을 두지 않는다).
                var row = CreateChoiceRow(null, () => ToggleChoice(value));
                var chip = new Badge(value);
                chip.AddToClassList("xeri-table__chip");
                TableGridDefaultCellViews.ApplyTone(chip, TableGridDefaultCellViews.GetToneName(column, value));
                row.Add(chip);

                choiceRows.Add((row, value));
                list.Add(row);
            }

            RefreshChoiceRows();
        }

        private VisualElement CreateChoiceRow(string text, Action toggle)
        {
            var row = CreateAction(text, "check-box", false, toggle);
            row.AddToClassList("xeri-table-filter__choice");
            row.style.height = TableGridPopoverMetrics.CHOICE_ROW_HEIGHT;
            row.style.flexShrink = 0f;
            return row;
        }

        private void ToggleAll()
        {
            var all = selectedChoices.Count == column.Choices.Count;
            selectedChoices.Clear();
            if (!all)
            {
                foreach (var choice in column.Choices)
                {
                    selectedChoices.Add(choice);
                }
            }

            RefreshChoiceRows();
        }

        private void ToggleChoice(string value)
        {
            if (!selectedChoices.Remove(value))
            {
                selectedChoices.Add(value);
            }

            RefreshChoiceRows();
        }

        private void RefreshChoiceRows()
        {
            var all = selectedChoices.Count == column.Choices.Count;
            allRow.EnableInClassList("xeri-table-popover__action--checked", all);
            allRow.EnableInClassList("xeri-table-popover__action--mixed", !all && selectedChoices.Count > 0);
            foreach (var (row, value) in choiceRows)
            {
                row.EnableInClassList("xeri-table-popover__action--checked", selectedChoices.Contains(value));
            }

            UpdateApplyState();
        }

    #endregion

    #region 숫자

        private void BuildNumber()
        {
            var isRange = current != null && current.ValueKind == TableGridColumnValueKind.Number;

            var row = new VisualElement();
            row.AddToClassList("xeri-table-popover__range");
            minimumField = CreateField(isRange ? current.Minimum : string.Empty, "최소값");
            maximumField = CreateField(isRange ? current.Maximum : string.Empty, "최대값");
            var dash = new Label("–");
            dash.AddToClassList("xeri-table-filter__dash");
            row.Add(minimumField);
            row.Add(dash);
            row.Add(maximumField);
            AddSized(row, TableGridPopoverMetrics.FIELD_ROW_HEIGHT);

            AddHint("비우면 제한 없음");
        }

        private static bool TryReadNumber(TextField field, out bool empty)
        {
            var text = field.value?.Trim() ?? string.Empty;
            empty = text.Length == 0;
            // 규칙 값은 보기 상태에 저장되므로 문화권과 무관하게 읽고, 셀 입력과 같이 NaN·무한대는 받지 않는다.
            return empty
                || (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var number)
                    && TableGridNumberParsing.IsFinite(number));
        }

    #endregion

    #region 문자열

        private void BuildText()
        {
            var isText = current != null && current.ValueKind == TableGridColumnValueKind.Text;
            // 편집기가 만들지 않는 연산자의 기존 규칙은 -1(아무 연산자도 고르지 않음)로 두어 조용히 바뀌지 않게 한다.
            textOperatorIndex = isText ? Array.IndexOf(textOperators, current.Operator) : 0;

            var segments = new VisualElement();
            segments.AddToClassList("xeri-table-filter__segments");
            for (var index = 0; index < textOperators.Length; index++)
            {
                var operatorIndex = index;
                var button = CreateAction(textOperatorNames[index], null, false, () => SelectOperator(operatorIndex));
                button.AddToClassList("xeri-table-filter__segment");
                operatorButtons.Add(button);
                segments.Add(button);
            }

            AddSized(segments, TableGridPopoverMetrics.FIELD_ROW_HEIGHT);
            SelectOperator(textOperatorIndex);

            var fieldRow = new VisualElement();
            fieldRow.AddToClassList("xeri-table-popover__range");
            textField = CreateField(isText ? current.Value : string.Empty, "값");
            fieldRow.Add(textField);
            AddSized(fieldRow, TableGridPopoverMetrics.FIELD_ROW_HEIGHT);
        }

        private void SelectOperator(int index)
        {
            textOperatorIndex = index;
            for (var button = 0; button < operatorButtons.Count; button++)
            {
                operatorButtons[button].EnableInClassList("xeri-table-popover__action--active", button == index);
            }
        }

    #endregion

    #region 입력칸과 적용

        // 입력칸에서 Enter는 바로 적용하고, 글자가 바뀌면 적용 버튼 상태를 다시 본다.
        private TextField CreateField(string value, string placeholder)
        {
            var field = CreateTextField(value, placeholder, Apply);
            field.RegisterValueChangedCallback(_ => UpdateApplyState());
            return field;
        }

        private void AddHint(string text)
        {
            var hint = new Label(text);
            hint.AddToClassList("xeri-table-popover__hint");
            AddSized(hint, TableGridPopoverMetrics.HINT_HEIGHT);
        }

        private void BuildActions()
        {
            var actions = new VisualElement();
            actions.AddToClassList("xeri-table-popover__actions");

            if (current != null)
            {
                var reset = CreateAction("초기화", null, false, () => Applied?.Invoke(null));
                reset.AddToClassList("xeri-table-popover__button");
                actions.Add(reset);
            }

            var spacer = new VisualElement();
            spacer.AddToClassList("xeri-table-popover__spacer");
            actions.Add(spacer);

            applyButton = CreateAction("적용", null, false, Apply);
            applyButton.AddToClassList("xeri-table-popover__button");
            applyButton.AddToClassList("xeri-table-popover__button--primary");
            actions.Add(applyButton);

            AddSized(actions, TableGridPopoverMetrics.ACTIONS_HEIGHT);
        }

        private void UpdateApplyState()
        {
            ShowFieldValidity();
            if (applyButton != null)
            {
                applyButton.SetEnabled(TryCreateRule(out _));
            }
        }

        // 입력이 틀린 칸을 표시한다. 규칙 만들기(TryCreateRule)는 화면을 바꾸지 않는다.
        private void ShowFieldValidity()
        {
            if (column.ValueKind != TableGridColumnValueKind.Number)
            {
                return;
            }

            minimumField.EnableInClassList("xeri-table-popover__field--invalid", !TryReadNumber(minimumField, out _));
            maximumField.EnableInClassList("xeri-table-popover__field--invalid", !TryReadNumber(maximumField, out _));
        }

        private void Apply()
        {
            if (TryCreateRule(out var rule))
            {
                Applied?.Invoke(rule);
            }
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> 입력으로 규칙을 만든다. 입력이 틀렸으면 false다. 화면은 바꾸지 않는다.
        /// <br/> 조건이 비어 필터가 필요 없으면 true와 null 규칙이다.
        /// </summary>
        // ----------------------------------------------------------------------
        private bool TryCreateRule(out TableGridFilterRule rule)
        {
            rule = null;
            var id = current?.ID ?? $"filter-{column.ID}";

            switch (column.ValueKind)
            {
                case TableGridColumnValueKind.Choice:
                    if (selectedChoices.Count == 0)
                    {
                        return false;
                    }

                    if (selectedChoices.Count < column.Choices.Count)
                    {
                        rule = TableGridFilterRule.ChoiceSet(id, column.ID, OrderedSelection());
                    }

                    return true;

                case TableGridColumnValueKind.Number:
                    var minimumValid = TryReadNumber(minimumField, out var minimumEmpty);
                    var maximumValid = TryReadNumber(maximumField, out var maximumEmpty);
                    if (!minimumValid || !maximumValid)
                    {
                        return false;
                    }

                    if (!minimumEmpty || !maximumEmpty)
                    {
                        rule = TableGridFilterRule.NumberRange
                        (
                            id, column.ID, minimumField.value.Trim(), maximumField.value.Trim()
                        );
                    }

                    return true;

                default:
                    var text = textField.value ?? string.Empty;
                    if (text.Length > 0)
                    {
                        // 편집기가 만들지 않는 연산자의 규칙은 사용자가 다른 연산자를 고르지 않는 한 그대로 둔다.
                        var filterOperator = textOperatorIndex >= 0 ? textOperators[textOperatorIndex] : current.Operator;
                        rule = new TableGridFilterRule(id, column.ID, filterOperator, text);
                    }

                    return true;
            }
        }

        private List<string> OrderedSelection()
        {
            // 열의 Choices 순서를 유지해 칩 표시가 매번 같게 한다.
            var ordered = new List<string>();
            foreach (var choice in column.Choices)
            {
                if (selectedChoices.Contains(choice))
                {
                    ordered.Add(choice);
                }
            }

            return ordered;
        }

    #endregion

    }
}
