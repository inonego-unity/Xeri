/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TableGridColumnPanel.cs
수정일 : 2026-11-23

# 설명
열 표시·순서를 바꾸는 화면 요소. 열마다 표시 체크, 열 이름, 고정 열 표시(Xeri Core Badge), 위·아래 이동 버튼을 둔다.
바꾸는 즉시 표시 열 순서를 알려 표에 바로 반영되게 하고, 아래의 기본값으로 원본 순서·모두 표시로 되돌린다.

# 특이사항, 제약사항
열 표시·순서는 표 자신의 표시 상태라 호스트 요청 없이 바로 바뀐다.
숨긴 열의 자리는 이 요소가 기억해 다시 보일 때 그 자리로 돌아온다.
표시 열은 하나 이상이어야 하므로 마지막 남은 열은 숨길 수 없다.
팝업 창 크기가 고정이므로 열이 많으면 목록 안에서 스크롤한다.
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
    /// 열 표시·순서 화면 요소.
    /// </summary>
    // ============================================================
    internal sealed class TableGridColumnPanel : TableGridPopoverPanel
    {

    #region 필드

        private readonly List<TableGridColumn> order = new();
        private readonly HashSet<string> shownIDs = new(StringComparer.Ordinal);
        private readonly List<TableGridColumn> sourceColumns = new();

        private ScrollView list;
        private int frozenCount;

    #endregion

    #region 이벤트

        // ------------------------------------------------------------
        /// <summary>
        /// 표시 열과 순서가 바뀌었을 때 표시할 열 ID를 순서대로 알린다.
        /// </summary>
        // ------------------------------------------------------------
        public event Action<IReadOnlyList<string>> VisibleColumnsChanged;

    #endregion

    #region 생성과 구성

        // ------------------------------------------------------------
        /// <summary>
        /// 빈 열 표시·순서 요소를 만든다. 내용은 Build로 채운다.
        /// </summary>
        // ------------------------------------------------------------
        public TableGridColumnPanel()
        {
            // NONE
        }

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> 원본 열과 지금 표시 열·고정 열 수로 목록을 만든다.
        /// <br/> 숨긴 열은 원본 순서대로 표시 열 뒤에 놓는다.
        /// </summary>
        // ------------------------------------------------------------
        public void Build
        (
            IReadOnlyList<TableGridColumn> source, IReadOnlyList<TableGridColumn> shown, int frozen
        )
        {
            sourceColumns.Clear();
            sourceColumns.AddRange(source);
            order.Clear();
            shownIDs.Clear();
            frozenCount = frozen;

            foreach (var column in shown)
            {
                order.Add(column);
                shownIDs.Add(column.ID);
            }

            foreach (var column in source)
            {
                if (!shownIDs.Contains(column.ID))
                {
                    order.Add(column);
                }
            }

            ResetContent();
            AddHeader("열 표시 · 순서", null);

            list = new ScrollView(ScrollViewMode.Vertical);
            list.AddToClassList("xeri-table-columns__list");
            var rows = Math.Min(order.Count, TableGridPopoverMetrics.MAX_VISIBLE_COLUMNS);
            AddSized(list, rows * TableGridPopoverMetrics.CHOICE_ROW_HEIGHT);

            BuildActions();
            RefreshRows();
        }

    #endregion

    #region 목록

        private void RefreshRows()
        {
            ClearActionsIn(list);
            var visibleCount = shownIDs.Count;
            var shownIndex = 0;

            for (var index = 0; index < order.Count; index++)
            {
                var column = order[index];
                var isVisible = shownIDs.Contains(column.ID);
                var position = index;

                // 마지막 남은 표시 열은 숨길 수 없으므로 체크를 흐리게 보이고 토글도 막는다(ToggleVisible).
                var row = CreateAction(column.Title, "check-box", false, () => ToggleVisible(position));
                row.AddToClassList("xeri-table-columns__row");
                row.EnableInClassList("xeri-table-popover__action--checked", isVisible);
                row.EnableInClassList("xeri-table-columns__row--locked", isVisible && visibleCount == 1);
                row.style.height = TableGridPopoverMetrics.CHOICE_ROW_HEIGHT;
                row.style.flexShrink = 0f;

                if (isVisible && shownIndex++ < frozenCount)
                {
                    var frozenBadge = new Badge("고정");
                    frozenBadge.AddToClassList("xeri-table-columns__badge");
                    row.Add(frozenBadge);
                }

                // 순서는 보이는 열끼리 바꾼다. 숨긴 열은 표에 순서가 없으므로 옮기지 않는다.
                var canMoveUp = isVisible && FindShownNeighbor(index, -1) >= 0;
                var canMoveDown = isVisible && FindShownNeighbor(index, 1) >= 0;
                row.Add(CreateMoveButton("chevron-up", canMoveUp, () => Move(position, -1)));
                row.Add(CreateMoveButton("chevron-down", canMoveDown, () => Move(position, 1)));
                list.Add(row);
            }
        }

        private VisualElement CreateMoveButton(string icon, bool enabled, Action move)
        {
            var button = new VisualElement();
            button.AddToClassList("xeri-table-columns__move");
            button.AddToClassList($"xeri-table-popover__icon--{icon}");
            button.SetEnabled(enabled);

            // 이동 버튼은 행의 표시 토글로 이어지지 않게 누름을 여기서 끝낸다.
            void HandlePointerDown(PointerDownEvent evt)
            {
                evt.StopPropagation();
            }

            button.RegisterCallback<PointerDownEvent>(HandlePointerDown);
            button.AddManipulator(new Clickable(move));
            return button;
        }

        private void ToggleVisible(int index)
        {
            var id = order[index].ID;
            if (!shownIDs.Remove(id))
            {
                shownIDs.Add(id);
            }
            else if (shownIDs.Count == 0)
            {
                shownIDs.Add(id);
                return;
            }

            Publish(index);
        }

        private void Move(int index, int step)
        {
            var target = FindShownNeighbor(index, step);
            if (target < 0)
            {
                return;
            }

            (order[index], order[target]) = (order[target], order[index]);
            Publish(target);
        }

        private int FindShownNeighbor(int index, int step)
        {
            // step 쪽으로 가장 가까운 보이는 열. 없으면 -1이다.
            for (var target = index + step; target >= 0 && target < order.Count; target += step)
            {
                if (shownIDs.Contains(order[target].ID))
                {
                    return target;
                }
            }

            return -1;
        }

        private void ResetOrder()
        {
            order.Clear();
            order.AddRange(sourceColumns);
            shownIDs.Clear();
            foreach (var column in sourceColumns)
            {
                shownIDs.Add(column.ID);
            }

            Publish(-1);
        }

        // 행을 다시 만들면 포커스를 가진 행이 사라지므로, focusIndex가 있으면 같은 위치 행에 포커스를 돌려준다.
        private void Publish(int focusIndex)
        {
            var ids = new List<string>();
            foreach (var column in order)
            {
                if (shownIDs.Contains(column.ID))
                {
                    ids.Add(column.ID);
                }
            }

            RefreshRows();
            VisibleColumnsChanged?.Invoke(ids);

            // 알림을 받은 표가 포커스를 가져갈 수 있으므로 알린 뒤에 돌려준다.
            if (focusIndex >= 0 && focusIndex < list.contentContainer.childCount)
            {
                list.contentContainer[focusIndex].Focus();
            }
        }

    #endregion

    #region 아래 버튼

        private void BuildActions()
        {
            var actions = new VisualElement();
            actions.AddToClassList("xeri-table-popover__actions");

            var spacer = new VisualElement();
            spacer.AddToClassList("xeri-table-popover__spacer");
            actions.Add(spacer);

            var reset = CreateAction("기본값", null, false, ResetOrder);
            reset.AddToClassList("xeri-table-popover__button");
            actions.Add(reset);

            AddSized(actions, TableGridPopoverMetrics.ACTIONS_HEIGHT);
        }

    #endregion

    }
}
