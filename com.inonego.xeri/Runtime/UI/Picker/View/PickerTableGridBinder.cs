/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : PickerTableGridBinder.cs
수정일 : 2026-11-23

# 설명
Picker 목록을 TableGrid(TableGridOptions.Picker())로 표시하고 표 입력을 PickerSession에 연결한다.
표에는 전체 entry를 담은 TableGridListSource 하나를 연결하고, 검색·정렬·스크롤·키보드 선택은 표가 맡는다.
filter chip은 그 소스의 필터로, 헤더 정렬 요청은 그 소스의 정렬로, 행 선택은 session 현재 entry로,
행 더블클릭은 session 확정으로 잇는다(Enter는 PickerView가 먼저 받아 현재 entry를 확정한다).

# 특이사항, 제약사항
column 값은 PickerColumnValue로 넘겨 정렬은 원래 값(같은 형식의 비교 가능한 값), 표시는 DisplayText를 쓴다.
검색 글자는 entry.SearchText(label·desc·value·tag·검색 가능 column)이고 소스가 항목별로 캐시한다.
표는 filter chip을 모르므로 chip이 켜져 있으면 TableGrid.IsSourceNarrowed로 알려, 결과가 없을 때 "검색 결과 없음"이 보이게 한다.
session을 표에 반영하는 동안 표가 보내는 선택 이벤트는 다시 session으로 보내지 않는다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UIElements;

using inonego;
using inonego.Xeri;
using inonego.Xeri.UI;
using inonego.Xeri.UI.TableGrid;

using TableGridView = inonego.Xeri.UI.TableGrid.TableGrid;

namespace inonego.Xeri.UI.Picker
{
    // ============================================================
    /// <summary>
    /// Picker 목록 표를 session에 연결한다.
    /// </summary>
    // ============================================================
    internal sealed class PickerTableGridBinder<TEntry, TValue>
    {

    #region 필드

        private const string IMAGE_COLUMN_ID = "__xeri_picker_image";
        private const string COLUMN_ID_PREFIX = "__xeri_picker_column_";
        private const string ROW_ID_PREFIX = "entry-";
        private const float IMAGE_COLUMN_WIDTH = 42f;
        private const float ROW_HEIGHT = 30f;
        private const string EMPTY_TEXT = "비어있음";
        private const string NO_RESULT_TEXT = "검색 결과 없음";

        private readonly PickerSession<TEntry, TValue> session;
        private readonly TableGridListSource<PickerEntry<TEntry, TValue>> source;
        private readonly Dictionary<PickerEntry<TEntry, TValue>, string> rowIDs = new();
        private readonly Dictionary<string, PickerEntry<TEntry, TValue>> entriesByRowID = new();

        private bool syncing;

        // ------------------------------------------------------------
        /// <summary>
        /// Picker 목록 표.
        /// </summary>
        // ------------------------------------------------------------
        public TableGridView Grid { get; }

    #endregion

    #region 생성자

        // ----------------------------------------------------------------------
        /// <summary>
        /// Picker 프리셋 표를 만들어 host에 붙이고 전체 entry 소스와 표 입력을 연결한다.
        /// </summary>
        // ----------------------------------------------------------------------
        public PickerTableGridBinder(PickerSession<TEntry, TValue> session, VisualElement host)
        {
            this.session = session ?? throw new ArgumentNullException(nameof(session));
            if (host == null)
            {
                throw new ArgumentNullException(nameof(host));
            }

            var options = TableGridOptions.Picker();
            options.RowHeight = ROW_HEIGHT;
            options.EmptyText = EMPTY_TEXT;
            options.NoResultText = NO_RESULT_TEXT;

            Grid = new TableGridView(options)
            {
                name = "entry-grid",
            };
            Grid.AddToClassList("xeri-picker__grid");
            Grid.SetColumns(CreateColumns());

            // 행 ID는 원본 순서로 한 번 정한다(검색·정렬·필터와 상관없이 같은 entry는 같은 행이다).
            for (var index = 0; index < session.Entries.Count; index++)
            {
                var rowID = ROW_ID_PREFIX + index;
                rowIDs[session.Entries[index]] = rowID;
                entriesByRowID[rowID] = session.Entries[index];
            }

            source = new TableGridListSource<PickerEntry<TEntry, TValue>>
            (
                session.Entries, entry => rowIDs[entry], searchText: entry => entry.SearchText
            );
            source.SetFilter(session.MatchesFilters);
            Grid.SetSource(source);

            Grid.SelectionChanged += HandleSelectionChanged;
            Grid.RowActivated += HandleRowActivated;
            Grid.QueryRequested += HandleQueryRequested;
            session.FiltersChanged += HandleFiltersChanged;

            host.Add(Grid);
        }

    #endregion

    #region 열 구성

        // ------------------------------------------------------------
        /// <summary>
        /// 이미지 열과 표시 column spec을 표 열로 만든다.
        /// </summary>
        // ------------------------------------------------------------
        private IReadOnlyList<TableGridColumn> CreateColumns()
        {
            var columns = new List<TableGridColumn>(session.Columns.Count + 1)
            {
                new TableGridColumn<PickerEntry<TEntry, TValue>, string>
                (
                    IMAGE_COLUMN_ID, string.Empty, _ => string.Empty, TableGridFormats.Text,
                    TableGridColumnLayout.Fixed(IMAGE_COLUMN_WIDTH), TableGridAlignment.Center,
                    filterable: false, sortable: false, groupable: false, searchable: false,
                    view: new PickerImageCellView<TEntry, TValue>()
                ),
            };

            // spec ID는 비거나 겹칠 수 있으므로 표 열 ID는 순서로 만든다.
            for (var index = 0; index < session.Columns.Count; index++)
            {
                var spec = session.Columns[index];
                if (!spec.Options.Visible) continue;

                var specIndex = index;
                columns.Add
                (
                    new TableGridColumn<PickerEntry<TEntry, TValue>, PickerColumnValue>
                    (
                        COLUMN_ID_PREFIX + index, spec.Header, entry => entry.Columns[specIndex], PickerColumnValueFormat.Instance,
                        spec.Options.Layout, spec.Options.Alignment,
                        filterable: false, sortable: spec.Options.Sortable, groupable: false, searchable: false,
                        view: PickerTextCellView<TEntry, TValue>.Get(spec.Options.Overflow)
                    )
                );
            }

            return columns;
        }

    #endregion

    #region 표 동기화

        // ----------------------------------------------------------------------
        /// <summary>
        /// session 현재 entry를 표 선택으로 반영한다(Escape로 비운 경우 등).
        /// </summary>
        // ----------------------------------------------------------------------
        public void Refresh()
        {
            var entry = session.CurrentEntry;
            var rowID = entry != null && rowIDs.TryGetValue(entry, out var id) ? id : null;
            if (IsGridSelection(rowID))
            {
                return;
            }

            syncing = true;
            try
            {
                var selectedRowIDs = new[]
                {
                    rowID,
                };
                Grid.Selection = rowID == null ? TableGridSelection.None : TableGridSelection.Rows(selectedRowIDs);
                Grid.ScrollToSelection();
            }
            finally
            {
                syncing = false;
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 검색으로 결과가 바뀐 뒤 부른다(표는 검색하며 이미 다시 읽었다).
        /// </summary>
        // ------------------------------------------------------------
        public void HandleResultsChanged()
        {
            ApplyResults(false);
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> 결과가 바뀐 뒤 걸러진 상태를 표에 알리고, reread이면 표를 한 번 다시 읽는다.
        /// <br/> 현재 entry가 결과에서 빠졌으면 비우고, 고른 행이나 맨 위를 보인다.
        /// </summary>
        // ----------------------------------------------------------------------
        private void ApplyResults(bool reread)
        {
            Grid.IsSourceNarrowed = session.HasActiveFilter;

            if (reread)
            {
                Grid.Refresh();
            }

            var current = session.CurrentEntry;
            if (current != null && source.IndexOf(current) < 0)
            {
                session.ClearCurrentEntry();
            }

            Grid.RevealSelectionOrFirstPage();
        }

        private bool IsGridSelection(string rowID)
        {
            var selection = Grid.Selection;
            if (rowID == null)
            {
                return selection.Kind == TableGridSelectionKind.None;
            }

            return selection.Kind == TableGridSelectionKind.Row
                && selection.RowIDs.Count == 1
                && selection.RowIDs[0] == rowID;
        }

        private PickerEntry<TEntry, TValue> FindEntry(string rowID)
        {
            return rowID != null && entriesByRowID.TryGetValue(rowID, out var entry) ? entry : null;
        }

    #endregion

    #region 이벤트 핸들러

        // ------------------------------------------------------------
        /// <summary>
        /// 표에서 고른 행을 session 현재 entry로 반영한다.
        /// </summary>
        // ------------------------------------------------------------
        private void HandleSelectionChanged(object sender, TableGridSelectionChanged e)
        {
            if (syncing) return;

            var current = e.Current;
            var entry = current.Kind == TableGridSelectionKind.Row && current.RowIDs.Count > 0
                ? FindEntry(current.RowIDs[0])
                : null;

            session.SetCurrentEntry(entry);
        }

        // --------------------------------------------------------------------------------
        /// <summary>
        /// 행 더블클릭을 session 확정 선택으로 보낸다(Enter는 PickerView가 먼저 처리한다).
        /// </summary>
        // --------------------------------------------------------------------------------
        private void HandleRowActivated(object sender, TableGridRowActivated e)
        {
            session.Confirm(FindEntry(e.RowID));
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 헤더 정렬 요청을 소스 정렬과 헤더 정렬 표시로 반영한다.
        /// </summary>
        // ------------------------------------------------------------
        private void HandleQueryRequested(object sender, TableGridQueryRequest e)
        {
            var requested = e.Requested;
            var column = requested.SortDirection.HasValue ? FindColumn(requested.SortColumnID) : null;
            if (column != null)
            {
                source.SetSort(column, requested.SortDirection.Value);
                Grid.QueryState = new TableGridQueryState(null, column.ID, requested.SortDirection);
            }
            else
            {
                source.SetSort((Comparison<PickerEntry<TEntry, TValue>>)null);
                Grid.QueryState = new TableGridQueryState(null, null, null);
            }

            ApplyResults(true);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// filter chip이 바뀌면 소스를 다시 걸러 표에 반영한다.
        /// </summary>
        // ------------------------------------------------------------
        private void HandleFiltersChanged()
        {
            source.Refresh();
            ApplyResults(true);
        }

        private TableGridColumn FindColumn(string columnID)
        {
            foreach (var column in Grid.Columns)
            {
                if (column.ID == columnID && column.Sortable)
                {
                    return column;
                }
            }

            return null;
        }

    #endregion

    }

    // ================================================================================
    /// <summary>
    /// <br/> Picker column 값 규칙. 글자는 DisplayText다.
    /// <br/> 비교는 값이 없으면 앞, 형식이 다르면 형식 이름 순, 같은 비교 가능한 형식이면 원래 값,
    /// <br/> 그 밖은 DisplayText 순서다(한 열에 형식이 섞여도 순서가 한 가지로 정해진다).
    /// </summary>
    // ================================================================================
    internal sealed class PickerColumnValueFormat : TableGridValueFormat<PickerColumnValue>
    {

    #region 필드

        // ------------------------------------------------------------
        /// <summary>
        /// 공유 인스턴스(상태 없음).
        /// </summary>
        // ------------------------------------------------------------
        public static readonly PickerColumnValueFormat Instance = new();

    #endregion

    #region 값 규칙

        // ------------------------------------------------------------
        /// <summary>
        /// 표시 글자.
        /// </summary>
        // ------------------------------------------------------------
        public override string ToText(PickerColumnValue value)
        {
            return value.DisplayText ?? string.Empty;
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> 값이 없으면 앞, 형식이 다르면 형식 이름 순으로 먼저 가른다.
        /// <br/> 같은 형식이면 비교 가능한 값은 원래 값으로, 아니면 표시 글자로 비교한다.
        /// </summary>
        // ----------------------------------------------------------------------
        public override int Compare(PickerColumnValue left, PickerColumnValue right)
        {
            var leftValue = left.Value;
            var rightValue = right.Value;
            if (ReferenceEquals(leftValue, rightValue)) return 0;
            if (leftValue == null) return -1;
            if (rightValue == null) return 1;

            // 형식이 섞인 열(예: object 값)에서도 비교가 한 순서로 이어지도록 형식부터 가른다.
            var leftType = leftValue.GetType();
            var rightType = rightValue.GetType();
            if (leftType != rightType)
            {
                return StringComparer.Ordinal.Compare(leftType.AssemblyQualifiedName, rightType.AssemblyQualifiedName);
            }

            if (leftValue is IComparable comparable)
            {
                return comparable.CompareTo(rightValue);
            }

            return StringComparer.Ordinal.Compare(left.DisplayText ?? string.Empty, right.DisplayText ?? string.Empty);
        }

    #endregion

    }

    // ================================================================================
    /// <summary>
    /// <br/> Picker column 글자 셀 뷰. overflow 정책을 USS 클래스로 반영한다.
    /// <br/> 선택할 수 없는 entry의 셀은 흐리게 보인다(xeri-picker__cell--disabled).
    /// </summary>
    // ================================================================================
    internal sealed class PickerTextCellView<TEntry, TValue> : TableGridCellView<PickerColumnValue>
    {

    #region 필드

        private static readonly PickerTextCellView<TEntry, TValue> ellipsis = new(PickerColumnOverflow.Ellipsis);
        private static readonly PickerTextCellView<TEntry, TValue> clip = new(PickerColumnOverflow.Clip);
        private static readonly PickerTextCellView<TEntry, TValue> wrap = new(PickerColumnOverflow.Wrap);

        private readonly PickerColumnOverflow overflow;

    #endregion

    #region 생성

        private PickerTextCellView(PickerColumnOverflow overflow)
        {
            this.overflow = overflow;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// overflow 정책에 맞는 공유 셀 뷰를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public static PickerTextCellView<TEntry, TValue> Get(PickerColumnOverflow overflow)
        {
            return overflow switch
            {
                PickerColumnOverflow.Clip => clip,
                PickerColumnOverflow.Wrap => wrap,
                _ => ellipsis,
            };
        }

    #endregion

    #region 셀 뷰

        // ------------------------------------------------------------
        /// <summary>
        /// 표 기본 글자 모양에 overflow 클래스를 더한 Label을 만든다.
        /// </summary>
        // ------------------------------------------------------------
        public override VisualElement MakeCell()
        {
            var label = new Label();
            label.AddToClassList("xeri-table__cell-text");
            label.AddToClassList("xeri-picker__cell-text");
            label.EnableInClassList("xeri-picker__cell-text--clip", overflow == PickerColumnOverflow.Clip);
            label.EnableInClassList("xeri-picker__cell-text--wrap", overflow == PickerColumnOverflow.Wrap);

            return label;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// column 표시 글자를 연결한다.
        /// </summary>
        // ------------------------------------------------------------
        public override void Bind(VisualElement element, PickerColumnValue value, in TableGridCellContext context)
        {
            if (element is Label label)
            {
                label.text = value.DisplayText ?? string.Empty;
                label.EnableInClassList("xeri-picker__cell--disabled", IsDisabled(context.Item));
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 행 항목이 선택할 수 없는 entry인지 확인한다.
        /// </summary>
        // ------------------------------------------------------------
        internal static bool IsDisabled(object item)
        {
            return item is PickerEntry<TEntry, TValue> entry && !entry.IsEnabled;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 재사용 전 글자를 비운다.
        /// </summary>
        // ------------------------------------------------------------
        public override void Unbind(VisualElement element)
        {
            if (element is Label label)
            {
                label.text = string.Empty;
            }
        }

    #endregion

    }

    // ============================================================
    /// <summary>
    /// Picker entry 이미지 셀 뷰. 이미지가 없으면 회색 체커를 보인다.
    /// </summary>
    // ============================================================
    internal sealed class PickerImageCellView<TEntry, TValue> : TableGridItemCellView
    {

    #region 셀 뷰

        // ------------------------------------------------------------
        /// <summary>
        /// 가운데 정렬 틀 안에 이미지 요소를 만든다.
        /// </summary>
        // ------------------------------------------------------------
        public override VisualElement MakeCell()
        {
            var frame = new VisualElement();
            frame.AddToClassList("xeri-picker__cell-image-frame");

            var image = new Image
            {
                scaleMode = ScaleMode.ScaleAndCrop,
            };
            image.AddToClassList("xeri-picker__cell-image");
            frame.Add(image);

            return frame;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 행 entry 이미지를 연결한다.
        /// </summary>
        // ------------------------------------------------------------
        public override void Bind(VisualElement element, in TableGridCellContext context)
        {
            var image = element.Q<Image>();
            if (image == null) return;

            var entry = context.Item as PickerEntry<TEntry, TValue>;
            image.image = entry?.Image != null ? entry.Image : PickerDefaultTextures.GrayChecker;
            element.EnableInClassList("xeri-picker__cell--disabled", PickerTextCellView<TEntry, TValue>.IsDisabled(entry));
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 재사용 전 이미지를 비운다.
        /// </summary>
        // ------------------------------------------------------------
        public override void Unbind(VisualElement element)
        {
            var image = element.Q<Image>();
            if (image != null)
            {
                image.image = null;
            }
        }

    #endregion

    }
}
