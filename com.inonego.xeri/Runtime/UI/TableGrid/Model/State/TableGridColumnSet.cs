/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TableGridColumnSet.cs
수정일 : 2026-11-22

# 설명
원본 열 목록과 화면 표시 상태(순서·숨김·사용자 폭·고정 열 수)를 함께 보관한다.
셀 값은 원본 열 순서로 들어오므로 표시 열에서 원본 인덱스를 찾는 일도 여기서 맡는다.
열 이동(헤더 끌기·Ctrl+Alt+←/→)의 새 표시 순서 계산도 여기서 맡는다. 적용은 SetVisibleColumns로 한다.

# 특이사항, 제약사항
열 ID는 중복될 수 없다. 표시 상태를 바꾸는 입력은 적용 전에 모두 검증해 일부만 바뀌지 않게 한다.
열을 다시 지정하면 남아 있는 ID의 순서·폭은 유지하고 사라진 ID의 상태는 버린다.
열을 옮겨도 고정 열 수는 그대로다. 놓은 자리의 구역에 들어가고 밀려난 열이 반대 구역으로 넘어간다.
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
    /// 원본 열과 표시 열 상태.
    /// </summary>
    // ============================================================
    internal sealed class TableGridColumnSet
    {

    #region 필드

        private const float MIN_USER_WIDTH = 32f;

        private readonly Dictionary<string, int> sourceIndexByID = new(StringComparer.Ordinal);
        private readonly Dictionary<string, int> visibleIndexByID = new(StringComparer.Ordinal);
        private readonly Dictionary<string, float> userWidths = new(StringComparer.Ordinal);
        private readonly Dictionary<string, float> userWeights = new(StringComparer.Ordinal);
        private int requestedFrozenCount;

        // ------------------------------------------------------------
        /// <summary>
        /// 원본 순서의 열 목록.
        /// </summary>
        // ------------------------------------------------------------
        public IReadOnlyList<TableGridColumn> SourceColumns => sourceColumns;
        private readonly List<TableGridColumn> sourceColumns = new();

        // ------------------------------------------------------------
        /// <summary>
        /// 화면 순서의 표시 열 목록.
        /// </summary>
        // ------------------------------------------------------------
        public IReadOnlyList<TableGridColumn> VisibleColumns => visibleColumns;
        private readonly List<TableGridColumn> visibleColumns = new();

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> 왼쪽에 고정할 표시 열 수. 읽을 때는 표시 열 수를 넘지 않는다.
        /// <br/> 지정은 0 이상이어야 하고, 바뀌면 배치만 다시 하도록 알린다.
        /// </summary>
        // ------------------------------------------------------------
        public int FrozenCount
        {
            get => Math.Min(requestedFrozenCount, visibleColumns.Count);
            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), "고정 열 수는 0 이상이어야 합니다.");
                }

                if (requestedFrozenCount == value)
                {
                    return;
                }

                requestedFrozenCount = value;
                LayoutChanged?.Invoke();
            }
        }

    #endregion

    #region 이벤트

        // ----------------------------------------------------------------------
        /// <summary>
        /// 원본 열이나 표시 열·순서가 바뀌었을 때 알린다. 셀을 다시 만들어야 한다.
        /// </summary>
        // ----------------------------------------------------------------------
        public event Action Changed;

        // ------------------------------------------------------------
        /// <summary>
        /// 사용자 폭이나 고정 열 수가 바뀌었을 때 알린다. 배치만 다시 하면 된다.
        /// </summary>
        // ------------------------------------------------------------
        public event Action LayoutChanged;

    #endregion

    #region 열 구성

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> 원본 열을 교체한다. 남아 있는 ID의 표시 순서와 사용자 폭은 유지한다.
        /// <br/> 새 열은 뒤에 붙고, 이전 표시 상태가 없으면 원본 순서 그대로 모두 보인다.
        /// </summary>
        // ----------------------------------------------------------------------
        public void SetColumns(IReadOnlyList<TableGridColumn> columns)
        {
            if (columns == null)
            {
                throw new ArgumentNullException(nameof(columns));
            }

            var indexByID = new Dictionary<string, int>(StringComparer.Ordinal);
            for (var index = 0; index < columns.Count; index++)
            {
                var column = columns[index] ?? throw new ArgumentException("열 목록에 null이 있습니다.", nameof(columns));
                if (!indexByID.TryAdd(column.ID, index))
                {
                    throw new ArgumentException($"열 ID가 중복됩니다: {column.ID}", nameof(columns));
                }
            }

            // 남은 표시 열은 순서를 유지하고, 처음 보는 열은 뒤에 붙인다. 숨겼던 열은 숨긴 채로 둔다.
            var previousOrder = new List<string>();
            foreach (var column in visibleColumns)
            {
                if (indexByID.ContainsKey(column.ID))
                {
                    previousOrder.Add(column.ID);
                }
            }

            foreach (var column in columns)
            {
                if (!sourceIndexByID.ContainsKey(column.ID) && previousOrder.Count > 0)
                {
                    previousOrder.Add(column.ID);
                }
            }

            sourceColumns.Clear();
            sourceColumns.AddRange(columns);
            sourceIndexByID.Clear();
            foreach (var pair in indexByID)
            {
                sourceIndexByID.Add(pair.Key, pair.Value);
            }

            RemoveMissing(userWidths, indexByID);
            RemoveMissing(userWeights, indexByID);

            // 처음 지정하는 경우와 이전 표시 열이 모두 사라진 경우에는 원본 순서로 모두 보인다.
            if (previousOrder.Count == 0)
            {
                foreach (var column in columns)
                {
                    previousOrder.Add(column.ID);
                }
            }

            ApplyVisibleOrder(previousOrder);
            Changed?.Invoke();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 원본 순서에서 열의 위치를 찾는다. 없으면 -1이다.
        /// </summary>
        // ------------------------------------------------------------
        public int GetSourceIndex(string columnID)
        {
            return columnID != null && sourceIndexByID.TryGetValue(columnID, out var index) ? index : -1;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 화면 순서에서 열의 위치를 찾는다. 숨긴 열이면 -1이다.
        /// </summary>
        // ------------------------------------------------------------
        public int GetVisibleIndex(string columnID)
        {
            return columnID != null && visibleIndexByID.TryGetValue(columnID, out var index) ? index : -1;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// ID로 원본 열을 찾는다.
        /// </summary>
        // ------------------------------------------------------------
        public TableGridColumn Find(string columnID)
        {
            var index = GetSourceIndex(columnID);
            return index < 0 ? null : sourceColumns[index];
        }

    #endregion

    #region 표시 상태

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> 표시할 열과 순서를 지정한다. 한 열 이상이어야 하며 중복될 수 없다.
        /// <br/> 숨긴 열의 사용자 폭은 다시 보일 때를 위해 유지한다.
        /// </summary>
        // ----------------------------------------------------------------------
        public void SetVisibleColumns(IReadOnlyList<string> columnIDs)
        {
            if (columnIDs == null)
            {
                throw new ArgumentNullException(nameof(columnIDs));
            }

            if (columnIDs.Count == 0)
            {
                throw new ArgumentException("표시 열은 한 개 이상이어야 합니다.", nameof(columnIDs));
            }

            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var id in columnIDs)
            {
                if (!sourceIndexByID.ContainsKey(id ?? string.Empty))
                {
                    throw new ArgumentException($"알 수 없는 열 ID입니다: {id}", nameof(columnIDs));
                }

                if (!seen.Add(id))
                {
                    throw new ArgumentException($"열 ID가 중복됩니다: {id}", nameof(columnIDs));
                }
            }

            ApplyVisibleOrder(columnIDs);
            Changed?.Invoke();
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> 고정 열의 사용자 폭(px)을 지정한다.
        /// <br/> 최소 MIN_USER_WIDTH이며, 고정 열 정책의 폭에 묶이지 않는다.
        /// <br/> null이면 열의 폭 정책으로 되돌린다.
        /// <br/> 가변 열은 SetUserWeight로 비율을 바꾼다(여기서는 받지 않는다).
        /// <br/> 바뀌었으면 true다.
        /// </summary>
        // ----------------------------------------------------------------------
        public bool SetUserWidth(string columnID, float? width)
        {
            var column = Find(columnID) ?? throw new ArgumentException($"알 수 없는 열 ID입니다: {columnID}", nameof(columnID));
            if (!width.HasValue)
            {
                return ClearUserSize(columnID);
            }

            var value = width.Value;
            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                throw new ArgumentOutOfRangeException(nameof(width), "열 폭은 유한한 값이어야 합니다.");
            }

            if (column.Layout.Mode != TableGridColumnLayoutMode.Fixed)
            {
                throw new ArgumentException($"가변 열은 SetUserWeight로 비율을 바꿉니다: {columnID}", nameof(columnID));
            }

            value = Math.Max(GetMinUserWidth(column.Layout), value);

            if (userWidths.TryGetValue(columnID, out var current) && current == value)
            {
                return false;
            }

            userWidths[columnID] = value;
            LayoutChanged?.Invoke();
            return true;
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> 사용자가 줄일 수 있는 열의 최소 폭(열 폭 최소값을 정하는 유일한 곳).
        /// <br/> 고정 열은 정책 폭에 묶이지 않아 MIN_USER_WIDTH이고(설계 4.6절),
        /// <br/> 가변 열은 정책 최소 폭과 MIN_USER_WIDTH 중 큰 값이다.
        /// </summary>
        // ----------------------------------------------------------------------
        public static float GetMinUserWidth(TableGridColumnLayout layout)
        {
            return layout.Mode == TableGridColumnLayoutMode.Fixed ? MIN_USER_WIDTH : Math.Max(layout.MinWidth, MIN_USER_WIDTH);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 사용자가 정한 열 폭을 찾는다.
        /// </summary>
        // ------------------------------------------------------------
        public bool TryGetUserWidth(string columnID, out float width)
        {
            return userWidths.TryGetValue(columnID, out width);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> 가변 열의 사용자 비율(가중치)을 지정한다.
        /// <br/> null이면 열의 폭 정책으로 되돌린다. 바뀌었으면 true다.
        /// </summary>
        // ------------------------------------------------------------
        public bool SetUserWeight(string columnID, float? weight)
        {
            if (Find(columnID) == null)
            {
                throw new ArgumentException($"알 수 없는 열 ID입니다: {columnID}", nameof(columnID));
            }

            if (!weight.HasValue)
            {
                return ClearUserSize(columnID);
            }

            var value = weight.Value;
            if (float.IsNaN(value) || float.IsInfinity(value) || value <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(weight), "가중치는 양의 유한한 값이어야 합니다.");
            }

            if (userWeights.TryGetValue(columnID, out var current) && current == value)
            {
                return false;
            }

            userWeights[columnID] = value;
            LayoutChanged?.Invoke();
            return true;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 사용자가 정한 가변 열 비율(가중치)을 찾는다.
        /// </summary>
        // ------------------------------------------------------------
        public bool TryGetUserWeight(string columnID, out float weight)
        {
            return userWeights.TryGetValue(columnID, out weight);
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// 열의 사용자 폭·비율을 지워 열의 폭 정책으로 되돌린다. 지운 것이 있으면 true다.
        /// </summary>
        // ----------------------------------------------------------------------
        public bool ClearUserSize(string columnID)
        {
            var removedWidth = userWidths.Remove(columnID);
            var removedWeight = userWeights.Remove(columnID);
            if (!removedWidth && !removedWeight)
            {
                return false;
            }

            LayoutChanged?.Invoke();
            return true;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 모든 열의 사용자 폭·비율을 지운다.
        /// </summary>
        // ------------------------------------------------------------
        public void ClearUserSizes()
        {
            if (userWidths.Count == 0 && userWeights.Count == 0)
            {
                return;
            }

            userWidths.Clear();
            userWeights.Clear();
            LayoutChanged?.Invoke();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> 표시 열마다 실제 폭 계산에 쓸 정책을 채운다.
        /// <br/> 사용자가 조절한 고정 열은 그 폭으로,
        /// <br/> 가변 열은 그 비율(가중치)로 다루며 열 종류는 바꾸지 않는다.
        /// </summary>
        // ------------------------------------------------------------
        public void FillEffectiveLayouts(List<TableGridColumnLayout> layouts)
        {
            layouts.Clear();
            foreach (var column in visibleColumns)
            {
                layouts.Add(GetEffectiveLayout(column));
            }
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// 열 하나의 실제 폭 정책을 돌려준다. 사용자 폭·비율이 없으면 열의 정책이다.
        /// </summary>
        // ----------------------------------------------------------------------
        public TableGridColumnLayout GetEffectiveLayout(TableGridColumn column)
        {
            var layout = column.Layout;
            if (layout.Mode == TableGridColumnLayoutMode.Fixed)
            {
                return userWidths.TryGetValue(column.ID, out var width) ? TableGridColumnLayout.Fixed(width) : layout;
            }

            return userWeights.TryGetValue(column.ID, out var weight)
                ? TableGridColumnLayout.Flexible(layout.Width, layout.MinWidth, layout.MaxWidth, weight)
                : layout;
        }

        private static void RemoveMissing(Dictionary<string, float> sizes, Dictionary<string, int> indexByID)
        {
            var removed = new List<string>();
            foreach (var id in sizes.Keys)
            {
                if (!indexByID.ContainsKey(id))
                {
                    removed.Add(id);
                }
            }

            foreach (var id in removed)
            {
                sizes.Remove(id);
            }
        }

        private void ApplyVisibleOrder(IReadOnlyList<string> columnIDs)
        {
            visibleColumns.Clear();
            visibleIndexByID.Clear();
            foreach (var id in columnIDs)
            {
                visibleIndexByID.Add(id, visibleColumns.Count);
                visibleColumns.Add(sourceColumns[sourceIndexByID[id]]);
            }
        }

    #endregion

    #region 열 이동 순서

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> 옮길 열을 빼고, 남은 열 사이 insertIndex 자리에
        /// <br/> 옮길 열을 보기 순서대로 끼운 새 순서를 만든다.
        /// <br/> insertIndex는 남은 열 기준 자리이며 0 ~ 남은 열 수로 맞춘다.
        /// <br/> order에 없는 ID는 무시한다.
        /// </summary>
        // ----------------------------------------------------------------------
        public static List<string> MoveColumns(IReadOnlyList<string> order, ISet<string> movingIDs, int insertIndex)
        {
            var remaining = new List<string>(order.Count);
            var block = new List<string>();
            foreach (var id in order)
            {
                if (movingIDs.Contains(id))
                {
                    block.Add(id);
                }
                else
                {
                    remaining.Add(id);
                }
            }

            remaining.InsertRange(Math.Clamp(insertIndex, 0, remaining.Count), block);
            return remaining;
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> 끌어 놓은 열 경계를 남은 열 사이 자리로 바꾼다.
        /// <br/> 경계 gap은 지금 보기 순서에서 열 gap 바로 왼쪽이다.
        /// <br/> 고정 열 수는 그대로이므로 고정 구역에 놓으면 옮길 열이 고정 구역 안에,
        /// <br/> 스크롤 구역에 놓으면 고정 구역 뒤에 들도록 자리를 맞춘다.
        /// <br/> 그래서 밀려난 열은 반대 구역으로 넘어간다.
        /// </summary>
        // ----------------------------------------------------------------------
        public static int GetDropIndex(IReadOnlyList<string> order, ISet<string> movingIDs, int gap, int frozenCount, bool frozenZone)
        {
            var index = 0;
            var movingCount = 0;
            for (var position = 0; position < order.Count; position++)
            {
                if (movingIDs.Contains(order[position]))
                {
                    movingCount++;
                }
                else if (position < gap)
                {
                    index++;
                }
            }

            if (frozenZone)
            {
                return Math.Min(index, Math.Max(0, frozenCount - movingCount));
            }

            return Math.Max(index, Math.Min(frozenCount, order.Count - movingCount));
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> 옮길 열을 왼쪽(direction 음수)·오른쪽으로 한 칸 옮길
        /// <br/> 남은 열 사이 자리를 구한다. 끝이면 그 자리 그대로다.
        /// <br/> 떨어진 열은 첫 열(왼쪽)이나 마지막 열(오른쪽) 기준으로 한 덩어리가 된다.
        /// </summary>
        // ----------------------------------------------------------------------
        public static int GetStepIndex(IReadOnlyList<string> order, ISet<string> movingIDs, int direction)
        {
            var remainingCount = 0;
            var firstIndex = -1;
            var lastIndex = -1;
            foreach (var id in order)
            {
                if (!movingIDs.Contains(id))
                {
                    remainingCount++;
                    continue;
                }

                // 옮길 열 앞에 남는 열 수가 그 열의 남은 열 기준 자리다.
                if (firstIndex < 0)
                {
                    firstIndex = remainingCount;
                }

                lastIndex = remainingCount;
            }

            var index = direction < 0 ? firstIndex - 1 : lastIndex + 1;
            return Math.Clamp(index, 0, remainingCount);
        }

    #endregion

    }
}
