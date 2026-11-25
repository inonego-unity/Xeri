/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TableGridStatusBar.cs
수정일 : 2026-11-22

# 설명
표의 상태 표시. 본문 가운데의 빈 상태 문구와 하단 줄의 보이는 범위·조작 알림·선택 개수를 맡는다.

# 특이사항, 제약사항
문구만 바꾸고 모양은 USS가 정한다. 빈 상태와 선택 개수는 빈 문구일 때 숨기고, 범위 문구는 자리를 유지한다.
로딩·오류 상태는 두지 않는다. 외부 데이터를 가져오는 동안의 표시는 호스트가 맡는다.
Paged의 페이지 이동 컨트롤(PageNavigator)은 이 줄 가운데 슬롯(page-slot)에 표가 담는다. 범위·개수 글자는 이 줄 하나만 보인다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UIElements;

namespace inonego.Xeri.UI.TableGrid
{
    // ============================================================
    /// <summary>
    /// 표의 상태 표시.
    /// </summary>
    // ============================================================
    internal sealed class TableGridStatusBar
    {

    #region 필드

        private const long MESSAGE_DURATION_MS = 4000;

        private readonly Label stateLabel;
        private readonly Label rangeLabel;
        private readonly Label messageLabel;
        private readonly Label selectionLabel;
        private Revision messageGeneration;

    #endregion

    #region 생성

        // ------------------------------------------------------------
        /// <summary>
        /// UXML이 만든 상태 문구 요소에 연결한다.
        /// </summary>
        // ------------------------------------------------------------
        public TableGridStatusBar(Label stateLabel, Label rangeLabel, Label messageLabel, Label selectionLabel)
        {
            this.stateLabel = stateLabel ?? throw new ArgumentNullException(nameof(stateLabel));
            this.rangeLabel = rangeLabel ?? throw new ArgumentNullException(nameof(rangeLabel));
            this.messageLabel = messageLabel ?? throw new ArgumentNullException(nameof(messageLabel));
            this.selectionLabel = selectionLabel ?? throw new ArgumentNullException(nameof(selectionLabel));
            SetText(messageLabel, string.Empty);
        }

    #endregion

    #region 알림

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> 조작 결과(붙여넣기 요약, 거부 이유 등)를 잠깐 보인다.
        /// <br/> 새 알림이 오면 이전 알림을 바로 바꾼다.
        /// </summary>
        // ------------------------------------------------------------
        public void ShowMessage(string text, bool isError)
        {
            messageGeneration = messageGeneration.Inc();

            var generation = messageGeneration;
            SetText(messageLabel, text);
            messageLabel.EnableInClassList("xeri-table__status-message--error", isError);

            void Expire()
            {
                if (generation == messageGeneration)
                {
                    SetText(messageLabel, string.Empty);
                }
            }

            messageLabel.schedule.Execute(Expire).StartingIn(MESSAGE_DURATION_MS);
        }

    #endregion

    #region 표시

        // --------------------------------------------------------------------------------
        /// <summary>
        /// <br/> 빈 상태 문구와 보이는 데이터 행 범위를 표시한다(그룹 머리 행은 세지 않는다).
        /// <br/> 보이는 위치 구간 [firstPosition, endPosition)은
        /// <br/> 표가 페이지 구간 안으로 잘라 준다.
        /// </summary>
        // --------------------------------------------------------------------------------
        public void ShowRows(TableGridRowSet rows, int firstPosition, int endPosition, string emptyText)
        {
            SetText(stateLabel, rows.IsEmpty ? emptyText ?? string.Empty : string.Empty);

            var count = rows.DataRowCount;
            if (count == 0)
            {
                rangeLabel.text = string.Empty;
                return;
            }

            // 보이는 위치 구간을 그 안의 데이터 행 번호로 바꾼다. 데이터 행이 안 보이면 0–0이다.
            var first = rows.CountDataRowsBefore(firstPosition) + 1;
            var last = rows.CountDataRowsBefore(endPosition);
            rangeLabel.text = last < first ? $"0–0 · 전체 {count:N0}행" : $"{first:N0}–{last:N0} · 전체 {count:N0}행";
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 선택한 행·열 수나 셀 범위 크기를 표시한다. 셀 하나면 표시하지 않는다.
        /// </summary>
        // ------------------------------------------------------------
        public void ShowSelection(TableGridSelectionState selection)
        {
            var text = string.Empty;
            var current = selection.Current;
            if (current.Kind == TableGridSelectionKind.Row && current.RowIDs.Count > 0)
            {
                text = $"{current.RowIDs.Count:N0}개 행 선택";
            }
            else if (current.Kind == TableGridSelectionKind.Column && current.ColumnIDs.Count > 0)
            {
                text = $"{current.ColumnIDs.Count:N0}개 열 선택";
            }
            else
            {
                var rowCount = selection.RangeRowCount;
                var columnCount = selection.RangeColumnCount;
                if (rowCount * columnCount > 1)
                {
                    text = $"{rowCount:N0}×{columnCount:N0} · {rowCount * columnCount:N0}셀 선택";
                }
            }

            SetText(selectionLabel, text);
        }

        private static void SetText(Label label, string text)
        {
            label.text = text;
            label.style.display = string.IsNullOrEmpty(text) ? DisplayStyle.None : DisplayStyle.Flex;
        }

    #endregion

    }
}
