/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TableGridGeometry.cs
수정일 : 2026-11-22

# 설명
TableGrid의 기하를 한곳에서 계산한다. 헤더·행·선택 표시가 모두 이 결과를 쓴다.
열 폭은 Xeri Core의 TableGridColumnLayout.Distribute로 나누고, 고정 영역과 스크롤 영역으로 가른다.
세로 방향은 고정 행 높이로 보이는 행 범위와 전체 콘텐츠 높이를 계산한다.

# 특이사항, 제약사항
UI 요소에 의존하지 않는다. 좌표 단위는 UI Toolkit 픽셀이다.
고정 영역은 앞쪽 장식 칸(끌기 손잡이, 행 선택 칸)과 고정 열을, 스크롤 영역은 나머지 열을 담는다.
각 영역의 x는 영역 왼쪽 기준이다. 장식 칸은 손잡이 → 행 선택 칸 순서로 고정 영역 맨 앞에 놓인다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

using inonego;
using inonego.Xeri;
using inonego.Xeri.UI;

namespace inonego.Xeri.UI.TableGrid
{
    // ============================================================
    /// <summary>
    /// 열 배치와 세로 가상화 범위를 계산한다.
    /// </summary>
    // ============================================================
    internal sealed class TableGridGeometry
    {

    #region 필드

        private float[] widths = Array.Empty<float>();
        private float[] offsets = Array.Empty<float>();

        // ------------------------------------------------------------
        /// <summary>
        /// 표시 열 수.
        /// </summary>
        // ------------------------------------------------------------
        public int ColumnCount { get; private set; }

        // ------------------------------------------------------------
        /// <summary>
        /// 왼쪽 고정 열 수.
        /// </summary>
        // ------------------------------------------------------------
        public int FrozenCount { get; private set; }

        // ------------------------------------------------------------
        /// <summary>
        /// 행 끌기 손잡이 칸의 폭. 손잡이를 숨기면 0이다.
        /// </summary>
        // ------------------------------------------------------------
        public float HandleWidth { get; private set; }

        // ------------------------------------------------------------
        /// <summary>
        /// 행 선택 칸의 폭. 선택 칸을 숨기면 0이다.
        /// </summary>
        // ------------------------------------------------------------
        public float SelectorWidth { get; private set; }

        // ------------------------------------------------------------
        /// <summary>
        /// 고정 영역 폭(끌기 손잡이 + 행 선택 칸 + 고정 열).
        /// </summary>
        // ------------------------------------------------------------
        public float FrozenWidth { get; private set; }

        // ------------------------------------------------------------
        /// <summary>
        /// 스크롤 영역 콘텐츠 폭(고정하지 않은 열의 합).
        /// </summary>
        // ------------------------------------------------------------
        public float ScrollContentWidth { get; private set; }

        // ------------------------------------------------------------
        /// <summary>
        /// 행 높이.
        /// </summary>
        // ------------------------------------------------------------
        public float RowHeight { get; private set; } = 32f;

        // ------------------------------------------------------------
        /// <summary>
        /// 전체 행 수.
        /// </summary>
        // ------------------------------------------------------------
        public int RowCount { get; private set; }

        // ------------------------------------------------------------
        /// <summary>
        /// 전체 콘텐츠 높이.
        /// </summary>
        // ------------------------------------------------------------
        public float ContentHeight => WindowCount * RowHeight;

        // ------------------------------------------------------------
        /// <summary>
        /// 본문이 보이는 행 구간의 첫 행 위치. Scroll 모드는 0이다.
        /// </summary>
        // ------------------------------------------------------------
        public int WindowStart { get; private set; }

        // ------------------------------------------------------------
        /// <summary>
        /// 본문이 보이는 행 구간의 행 수. Scroll 모드는 RowCount다.
        /// </summary>
        // ------------------------------------------------------------
        public int WindowCount { get; private set; }

    #endregion

    #region 열 배치

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> 표시 열의 폭과 위치를 계산한다.
        /// <br/> 사용 가능한 폭은 끌기 손잡이·행 선택 칸을 뺀 나머지를 열 정책대로 나눈다.
        /// </summary>
        // ----------------------------------------------------------------------
        public void LayoutColumns
        (
            IReadOnlyList<TableGridColumnLayout> layouts,
            int frozenCount,
            float handleWidth,
            float selectorWidth,
            float availableWidth
        )
        {
            if (layouts == null)
            {
                throw new ArgumentNullException(nameof(layouts));
            }

            ColumnCount = layouts.Count;
            FrozenCount = Math.Clamp(frozenCount, 0, ColumnCount);
            HandleWidth = Math.Max(0f, handleWidth);
            SelectorWidth = Math.Max(0f, selectorWidth);

            if (widths.Length != ColumnCount)
            {
                widths = new float[ColumnCount];
                offsets = new float[ColumnCount];
            }

            var leadingWidth = HandleWidth + SelectorWidth;
            var distributable = Math.Max(0f, availableWidth - leadingWidth);
            TableGridColumnLayout.Distribute(layouts, distributable, widths);

            var frozenX = leadingWidth;
            var scrollX = 0f;
            for (var index = 0; index < ColumnCount; index++)
            {
                if (index < FrozenCount)
                {
                    offsets[index] = frozenX;
                    frozenX += widths[index];
                }
                else
                {
                    offsets[index] = scrollX;
                    scrollX += widths[index];
                }
            }

            FrozenWidth = frozenX;
            ScrollContentWidth = scrollX;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 표시 열의 폭.
        /// </summary>
        // ------------------------------------------------------------
        public float GetWidth(int columnIndex)
        {
            return widths[columnIndex];
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 표시 열이 속한 영역 안에서의 x 위치.
        /// </summary>
        // ------------------------------------------------------------
        public float GetOffset(int columnIndex)
        {
            return offsets[columnIndex];
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 표시 열이 고정 영역에 있는지 여부.
        /// </summary>
        // ------------------------------------------------------------
        public bool IsFrozen(int columnIndex)
        {
            return columnIndex < FrozenCount;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> 고정 열이 있는지 여부.
        /// <br/> 열 찾기·열 경계는 고정 열이 있을 때만 고정 영역을 묻는다.
        /// <br/> 고정 영역(FrozenWidth)은 끌기 손잡이·행 선택 칸 때문에
        /// <br/> 고정 열이 없어도 있을 수 있다.
        /// </summary>
        // ------------------------------------------------------------
        public bool HasFrozenColumns => FrozenCount > 0;

        // --------------------------------------------------------------------------------
        /// <summary>
        /// <br/> 영역 안의 x 위치에 있는 표시 열을 찾는다. 영역 밖이면 가장 가까운 열이다.
        /// <br/> 스크롤 영역에 열이 없으면(모두 고정) 마지막 고정 열을 준다.
        /// <br/> 표시 열이 없거나 고정 열 없이 고정 영역을 물으면 -1이다(HasFrozenColumns).
        /// </summary>
        // --------------------------------------------------------------------------------
        public int FindColumnAt(float x, bool frozen)
        {
            if (ColumnCount == 0)
            {
                return -1;
            }

            var first = frozen ? 0 : FrozenCount;
            var end = frozen ? FrozenCount : ColumnCount;
            if (first >= end)
            {
                return frozen ? -1 : FrozenCount - 1;
            }

            for (var index = first; index < end; index++)
            {
                if (x < offsets[index] + widths[index])
                {
                    return index;
                }
            }

            return end - 1;
        }

        // --------------------------------------------------------------------------------
        /// <summary>
        /// <br/> 영역 안의 x 위치에 가장 가까운 열 경계(열 이동의 삽입 자리)를 찾는다.
        /// <br/> 경계 k는 표시 열 k 바로 왼쪽이고, 영역의 끝 경계는 영역 마지막 열 다음이다.
        /// <br/> 고정 영역은 0 ~ FrozenCount, 스크롤 영역은 FrozenCount ~ ColumnCount다.
        /// <br/> 표시 열이 없으면 -1이다.
        /// </summary>
        // --------------------------------------------------------------------------------
        public int FindColumnGapAt(float x, bool frozen)
        {
            if (ColumnCount == 0)
            {
                return -1;
            }

            var first = frozen ? 0 : FrozenCount;
            var end = frozen ? FrozenCount : ColumnCount;
            for (var index = first; index < end; index++)
            {
                if (x < offsets[index] + widths[index] * 0.5f)
                {
                    return index;
                }
            }

            return end;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 영역의 열 경계 x 위치(영역 왼쪽 기준).
        /// </summary>
        // ------------------------------------------------------------
        public float GetGapOffset(int gap, bool frozen)
        {
            var end = frozen ? FrozenCount : ColumnCount;
            if (gap < end)
            {
                return offsets[gap];
            }

            return frozen ? FrozenWidth : ScrollContentWidth;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> 열이 스크롤 영역에 보이도록 할 가로 스크롤 위치를 계산한다.
        /// <br/> 이미 보이거나 고정 열이면 지금 위치를 그대로 준다.
        /// </summary>
        // ------------------------------------------------------------
        public float GetScrollXToShow(int columnIndex, float scrollX, float viewportWidth)
        {
            if (columnIndex < 0 || columnIndex >= ColumnCount || IsFrozen(columnIndex) || viewportWidth <= 0f)
            {
                return scrollX;
            }

            var left = offsets[columnIndex];
            var width = widths[columnIndex];
            if (left < scrollX)
            {
                return left;
            }

            if (left + width > scrollX + viewportWidth)
            {
                return width >= viewportWidth ? left : left + width - viewportWidth;
            }

            return scrollX;
        }

    #endregion

    #region 세로 범위

        // --------------------------------------------------------------------------------
        /// <summary>
        /// 행 높이·행 수와 본문이 보일 행 구간을 지정한다. windowCount가 음수면 전체 행이다.
        /// </summary>
        // --------------------------------------------------------------------------------
        public void SetRows(float rowHeight, int rowCount, int windowStart = 0, int windowCount = -1)
        {
            if (float.IsNaN(rowHeight) || float.IsInfinity(rowHeight) || rowHeight <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(rowHeight), "행 높이는 양의 유한한 값이어야 합니다.");
            }

            RowHeight = rowHeight;
            RowCount = Math.Max(0, rowCount);
            WindowStart = Math.Clamp(windowStart, 0, RowCount);
            WindowCount = windowCount < 0 ? RowCount - WindowStart : Math.Min(windowCount, RowCount - WindowStart);
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// 행이 본문 콘텐츠에서 놓이는 세로 위치. 구간 밖이면 구간 기준으로 계산한 값이다.
        /// </summary>
        // ----------------------------------------------------------------------
        public float GetRowTop(int rowIndex)
        {
            return (rowIndex - WindowStart) * RowHeight;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 행이 본문이 보이는 구간 안에 있는지 여부.
        /// </summary>
        // ------------------------------------------------------------
        public bool IsInWindow(int rowIndex)
        {
            return rowIndex >= WindowStart && rowIndex < WindowStart + WindowCount;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> 세로 스크롤 위치와 보이는 높이로 그릴 행 범위를 계산한다.
        /// <br/> 위아래로 overscan 행을 더하되 전체 행 범위를 넘지 않는다.
        /// </summary>
        // ------------------------------------------------------------
        public (int First, int LastExclusive) GetVisibleRows(float scrollY, float viewportHeight, int overscan)
        {
            if (WindowCount == 0 || viewportHeight <= 0f)
            {
                return (WindowStart, WindowStart);
            }

            var first = (int)Math.Floor(Math.Max(0f, scrollY) / RowHeight) - overscan;
            var last = (int)Math.Ceiling((Math.Max(0f, scrollY) + viewportHeight) / RowHeight) + overscan;

            return (WindowStart + Math.Max(0, first), WindowStart + Math.Min(WindowCount, Math.Max(0, last)));
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 보이는 높이에 들어가는 최대 행 수(overscan 제외).
        /// </summary>
        // ------------------------------------------------------------
        public int GetViewportCapacity(float viewportHeight)
        {
            return viewportHeight <= 0f ? 0 : (int)Math.Ceiling(viewportHeight / RowHeight) + 1;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 콘텐츠 y 위치의 행을 찾는다. 범위 밖이면 가장 가까운 행이다.
        /// </summary>
        // ------------------------------------------------------------
        public int FindRowAt(float contentY)
        {
            if (WindowCount == 0)
            {
                return -1;
            }

            return WindowStart + Math.Clamp((int)Math.Floor(contentY / RowHeight), 0, WindowCount - 1);
        }

        // --------------------------------------------------------------------------------
        /// <summary>
        /// <br/> 콘텐츠 y 위치에 가장 가까운 행 경계를 찾는다. 행 끌기의 삽입 자리다.
        /// <br/> 경계 k는 행 k 바로 위이며 구간 안(WindowStart ~ 끝 행 다음)으로 맞춘다.
        /// <br/> 구간이 비었으면 -1이다.
        /// </summary>
        // --------------------------------------------------------------------------------
        public int FindGapAt(float contentY)
        {
            if (WindowCount == 0)
            {
                return -1;
            }

            return WindowStart + Math.Clamp((int)Math.Floor(contentY / RowHeight + 0.5f), 0, WindowCount);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 행이 보이도록 할 세로 스크롤 위치를 계산한다.
        /// </summary>
        // ------------------------------------------------------------
        public float GetScrollYToShow(int rowIndex, float scrollY, float viewportHeight)
        {
            if (rowIndex < 0 || viewportHeight <= 0f)
            {
                return scrollY;
            }

            var top = GetRowTop(rowIndex);
            if (top < scrollY)
            {
                return top;
            }

            if (top + RowHeight > scrollY + viewportHeight)
            {
                return top + RowHeight - viewportHeight;
            }

            return scrollY;
        }

    #endregion

    }
}
