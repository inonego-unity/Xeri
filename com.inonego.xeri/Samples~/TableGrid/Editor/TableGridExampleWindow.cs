/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TableGridExampleWindow.cs
수정일 : 2026-11-24

# 설명
TableGrid의 화면과 조작을 확인하는 예제 창. 1만 행 가짜 데이터, 고정 열, 선택형 칩, 열 메뉴, 쿼리 칩을 보인다.
위쪽 한 줄에 검색창과 쿼리 칩이 나란히 있다. 검색은 이 창이 직접 거르고 거른 결과만 표에 넘긴다.
스크롤/페이지 전환은 창 메뉴(탭의 ⋮)의 "페이지 모드"로 한다.
행 항목은 셀 글자 배열(string[])이고, 열은 TableGridColumn<string[], string>으로 배열에서 값을 꺼낸다.
수량·가격은 숫자 글자(NumberText), 분류·등급·일차는 선택형(Choice) 값 규칙을 쓴다.

# 특이사항, 제약사항
이 창이 호스트 역할을 한다. 표가 QueryRequested로 정렬·필터·그룹을 요청하면 데이터를 직접 거르고 정렬하고
그룹 머리 행을 넣은 뒤 SetSource·QueryState로 반영한다.
검색창(TableGridSearchField)은 SearchesGrid를 끄고 SearchTextChanged를 받아 같은 방식으로 거른다.
거른 동안은 IsSourceNarrowed로 알려, 결과가 없을 때 빈 표 문구 대신 "검색 결과 없음" 문구가 보이게 한다.
EditRequested는 편집한 값을 기억하고 행 항목 배열에도 써 둔 뒤 ApplyEditResults로 다시 읽게 하고,
DeleteRowsRequested는 행을 지운 뒤 다시 조회한다.
MoveRowsRequested(끌기·Alt+방향키·메뉴·잘라낸 행 삽입)는 행 순서를 바꾼다. 정렬 중이면 정렬을 풀어 옮긴 순서가 보이게 한다.
AddRowRequested(위에·아래에 행 삽입, 복사한 행 삽입)는 생성 행 뒤에 새 행 번호를 붙여 그 자리에 넣고 요청한 행 값을 쓴다.
편집·행 삭제·행 이동·행 삽입은 창의 직렬화 상태를 Unity Undo에 한 단계씩 기록한다(Ctrl+Z·Ctrl+Y). 되돌리면 그 단계를 요청한 때의 선택도 다시 고른다.
ID·일차 열은 읽기 전용이라 붙여넣기에서 건너뛰는 모습을 볼 수 있다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;

using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

using inonego;
using inonego.Xeri;
using inonego.Xeri.UI;
using inonego.Xeri.UI.TableGrid;
using inonego.Xeri.UI.TableGrid.Editor;

namespace inonego.Xeri.UI.TableGrid.Samples
{
    // ============================================================
    /// <summary>
    /// TableGrid 예제 창.
    /// </summary>
    // ============================================================
    public sealed class TableGridExampleWindow : EditorWindow, IHasCustomMenu
    {

    #region 예제 데이터

        private const int GENERATED_ROW_COUNT = 10000;

        private static readonly string[] categories =
        {
            "Weapon", "Armor", "Consumable", "Material", "Quest",
        };

        private static readonly string[] rarities =
        {
            "Common", "Rare", "Epic", "Legendary",
        };

        private static readonly string[] days =
        {
            "1일차", "2일차", "3일차", "4일차", "5일차",
        };

        private static readonly string[] names =
        {
            "Crossbow", "Ancient Key", "Steel Helm", "Amber Charm", "Mana Flask",
            "Oak Bow", "Crystal Lens", "Bandage", "Fire Bomb", "Herbal Tonic",
        };

        // 열 ID와 값 getter는 같은 순서다(행 항목 배열의 자리도 이 순서다).
        private static readonly string[] columnIDs =
        {
            "id", "name", "category", "rarity", "day", "quantity", "price",
        };

        private static readonly Func<int, string>[] cellGetters =
        {
            RowID,
            index => $"{names[index % names.Length]} {index + 1:00000}",
            index => categories[(index * 7 + 3) % categories.Length],
            index => rarities[(index * 5 + index / 3) % rarities.Length],
            index => days[(index * 3 + 1) % days.Length],
            index => ((index * 37) % 500).ToString(CultureInfo.InvariantCulture),
            index => ((index * 113) % 9000 + 100).ToString(CultureInfo.InvariantCulture),
        };

        // 편집 결과는 창에 직렬화해 Unity Undo가 기록하고 되돌린다. 아래 사전과 집합은 이 목록으로 다시 만드는 조회용이다.
        [SerializeField] private List<EditedCell> editedCells = new();
        [SerializeField] private List<int> deletedRowList = new();
        [SerializeField] private List<int> rowOrder = new();
        [SerializeField] private int addedRowCount;
        [SerializeField] private int revision;

        // 검색창 글자. 거르기는 이 창이 하므로 표가 아니라 창이 들고 있는다.
        private string searchText = string.Empty;
        private readonly Dictionary<int, string[]> rowItems = new();
        private readonly Dictionary<(int Row, int Column), string> editedValues = new();
        private readonly HashSet<int> deletedRows = new();

        // 단계(revision)마다 그 조작을 요청한 때의 선택. 실행 취소·다시 실행 뒤 그 자리를 다시 보인다.
        private readonly Dictionary<int, TableGridSelection> stepSelections = new();
        private int seenRevision;
        private TableGrid grid;

        // 생성한 행과 넣은 행을 합한 행 수. 넣은 행은 생성 행 뒤 번호를 쓴다.
        private int RowCount => GENERATED_ROW_COUNT + addedRowCount;

        // ============================================================
        /// <summary>
        /// 편집한 셀 하나(직렬화용).
        /// </summary>
        // ============================================================
        [Serializable]
        private struct EditedCell
        {
            // ------------------------------------------------------------
            /// <summary>
            /// 행 인덱스.
            /// </summary>
            // ------------------------------------------------------------
            public int Row;

            // ------------------------------------------------------------
            /// <summary>
            /// 열 인덱스.
            /// </summary>
            // ------------------------------------------------------------
            public int Column;

            // ------------------------------------------------------------
            /// <summary>
            /// 셀 값.
            /// </summary>
            // ------------------------------------------------------------
            public string Value;
        }

    #endregion

    #region 창

        // ------------------------------------------------------------
        /// <summary>
        /// 예제 창을 연다.
        /// </summary>
        // ------------------------------------------------------------
        [MenuItem("Xeri/Validation/Table Grid")]
        public static void Open()
        {
            var window = GetWindow<TableGridExampleWindow>();
            window.titleContent = new GUIContent("TableGrid 예제");
            window.minSize = new Vector2(640f, 360f);
        }

        private void OnEnable()
        {
            RebuildCaches();
            seenRevision = revision;
            Undo.undoRedoPerformed += HandleUndoRedo;
        }

        private void OnDisable()
        {
            Undo.undoRedoPerformed -= HandleUndoRedo;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> 창 메뉴(탭의 ⋮)에 스크롤/페이지 전환을 넣는다. 같은 본문을 스크롤과 페이지 사이에서 바꿔 본다.
        /// <br/> 표를 만들기 전에는 선택할 수 없다.
        /// </summary>
        // ------------------------------------------------------------
        public void AddItemsToMenu(GenericMenu menu)
        {
            var label = new GUIContent("페이지 모드");
            if (grid == null)
            {
                menu.AddDisabledItem(label);
                return;
            }

            menu.AddItem(label, grid.Options.Navigation == TableGridNavigation.Paged, ToggleNavigation);
        }

        private void ToggleNavigation()
        {
            // 같은 본문을 스크롤과 페이지(보이는 높이만큼 한 페이지) 사이에서 바꾼다.
            grid.Options.Navigation = grid.Options.Navigation == TableGridNavigation.Scroll
                ? TableGridNavigation.Paged
                : TableGridNavigation.Scroll;
            grid.Refresh();
        }

        private void CreateGUI()
        {
            rootVisualElement.AddToClassList("table-grid-sample");
            rootVisualElement.style.flexGrow = 1f;
            rootVisualElement.styleSheets.Add(Resources.Load<StyleSheet>("Xeri/UI/TableGridSample/TableGridSample"));

            var options = TableGridOptions.Workspace(TableGridAccess.Editable);
            options.RowHeight = 32f;
            grid = new TableGrid(options);
            grid.SetColumns(CreateColumns());
            grid.FrozenColumnCount = 2;
            grid.QueryRequested += (_, request) => ApplyQuery(request.Requested);
            grid.EditRequested += (_, request) => ApplyEdits(request);
            grid.DeleteRowsRequested += (_, request) => DeleteRows(request);
            grid.MoveRowsRequested += (_, request) => MoveRows(request);
            grid.AddRowRequested += (_, request) => AddRows(request);

            // 검색창과 쿼리 칩은 호스트가 원하는 곳에 둔다. 예제에서는 표 위 한 줄에 검색창과 칩을 나란히 둔다.
            // 이 창이 직접 거르므로 입력을 표(SearchText)로 보내지 않고 SearchTextChanged에서 처리한다.
            var search = new TableGridSearchField(grid)
            {
                Placeholder = "이름·분류 검색",
                ShowResultCount = true,
                SearchesGrid = false,
            };
            search.SearchTextChanged += (_, _) =>
            {
                searchText = search.Text;
                ApplyQuery(grid.QueryState);
            };
            var queryRow = new VisualElement();
            queryRow.AddToClassList("table-grid-sample__query");
            queryRow.Add(search);

            var chips = new TableGridQueryChipBar(grid);
            chips.AddToClassList("table-grid-sample__chips");
            queryRow.Add(chips);

            rootVisualElement.Add(queryRow);
            rootVisualElement.Add(grid);

            var categoryValues = new[]
            {
                "Weapon", "Armor",
            };
            var filters = new[]
            {
                TableGridFilterRule.ChoiceSet("filter-category", "category", categoryValues),
            };
            ApplyQuery(new TableGridQueryState(filters, "rarity", TableGridSortDirection.Descending));

            // 열 표시·순서·폭·고정 수와 쿼리를 EditorPrefs에 저장하고, 다시 열면 복원한다.
            TableGridViewStatePrefs.Bind(grid, "Samples.TableGridExampleWindow");
        }

    #endregion

    #region 열

        private static IReadOnlyList<TableGridColumn> CreateColumns()
        {
            // 행 항목은 셀 글자 배열이다. 열은 자기 자리의 글자를 꺼내고, 편집 열은 기본 편집기(입력칸·선택 메뉴)를 쓴다.
            var edit = TableGridCellEdit.Editor();
            return new TableGridColumn[]
            {
                new TableGridColumn<string[], string>
                (
                    "id", "ID", Cell(0), TableGridFormats.Text, TableGridColumnLayout.Fixed(96f), groupable: false
                ),
                new TableGridColumn<string[], string>
                (
                    "name", "이름", Cell(1), TableGridFormats.Text, TableGridColumnLayout.Flexible(200f, 140f, 0f, 2f),
                    edit: edit, groupable: false
                ),
                new TableGridColumn<string[], string>
                (
                    "category", "분류", Cell(2), TableGridFormats.Choice(categories),
                    TableGridColumnLayout.Flexible(130f, 110f, 0f, 1f), edit: edit
                ),
                new TableGridColumn<string[], string>
                (
                    "rarity", "등급", Cell(3), TableGridFormats.Choice(rarities),
                    TableGridColumnLayout.Flexible(130f, 110f, 0f, 1f), edit: edit
                ),
                new TableGridColumn<string[], string>
                (
                    "day", "일차", Cell(4), TableGridFormats.Choice(days), TableGridColumnLayout.Fixed(90f)
                ),
                new TableGridColumn<string[], string>
                (
                    "quantity", "수량", Cell(5), TableGridFormats.NumberText, TableGridColumnLayout.Fixed(90f),
                    TableGridAlignment.Right, edit, groupable: false
                ),
                new TableGridColumn<string[], string>
                (
                    "price", "가격", Cell(6), TableGridFormats.NumberText, TableGridColumnLayout.Fixed(100f),
                    TableGridAlignment.Right, edit, groupable: false
                ),
            };
        }

        private static Func<string[], string> Cell(int column)
        {
            return cells => column < cells.Length ? cells[column] : string.Empty;
        }

        private static string RowID(int index)
        {
            return $"ITM-{index + 1:00000}";
        }

    #endregion

    #region 조회 (호스트 역할)

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> 쿼리대로 행을 거르고 정렬·그룹한 소스를 표에 넣는다.
        /// <br/> keepView면 스크롤 위치와 선택을 유지한다(행 삭제·되살리기).
        /// </summary>
        // ------------------------------------------------------------
        private void ApplyQuery(TableGridQueryState query, bool keepView = false)
        {
            // 행 이동으로 바뀐 순서를 기본 순서로 쓰고, 정렬 값이 같으면 그 순서를 지킨다.
            var baseOrder = GetRowOrder();
            var position = new int[RowCount];
            for (var at = 0; at < baseOrder.Count; at++)
            {
                position[baseOrder[at]] = at;
            }

            var indices = new List<int>();
            foreach (var index in baseOrder)
            {
                if (Matches(index, query))
                {
                    indices.Add(index);
                }
            }

            if (query.SortDirection.HasValue)
            {
                var getter = GetterOf(query.SortColumnID);
                var sign = query.SortDirection == TableGridSortDirection.Descending ? -1 : 1;
                int CompareRows(int left, int right)
                {
                    var order = Compare(getter(left), getter(right));
                    return order != 0 ? order * sign : position[left].CompareTo(position[right]);
                }

                indices.Sort(CompareRows);
            }

            rowItems.Clear();

            // 검색으로 거른 동안은 결과가 없을 때 "검색 결과 없음"이 보이도록 표에 알린다.
            grid.IsSourceNarrowed = !string.IsNullOrWhiteSpace(searchText);

            // 예제 데이터는 행 순서를 바꿀 수 있다(행 이동 요청을 연다).
            grid.SetSource(new TableGridRowListSource(BuildRows(indices, query.GroupColumnID), canReorder: true), keepView);
            grid.QueryState = query;
        }

        private List<TableGridRow> BuildRows(List<int> indices, string groupColumnID)
        {
            var rows = new List<TableGridRow>(indices.Count);
            if (string.IsNullOrEmpty(groupColumnID))
            {
                foreach (var index in indices)
                {
                    rows.Add(CreateRow(index));
                }

                return rows;
            }

            // 그룹 값이 처음 나온 순서로 묶고, 묶음마다 머리 행을 넣는다.
            var getter = GetterOf(groupColumnID);
            var groups = new Dictionary<string, List<int>>(StringComparer.Ordinal);
            var order = new List<string>();
            foreach (var index in indices)
            {
                var key = getter(index);
                if (!groups.TryGetValue(key, out var members))
                {
                    members = new List<int>();
                    groups.Add(key, members);
                    order.Add(key);
                }

                members.Add(index);
            }

            foreach (var key in order)
            {
                // 그룹 머리 행은 제목 글자를 항목에 담는다.
                var title = $"{key} · {groups[key].Count:N0}행";
                rows.Add(new TableGridRow($"group-{key}", title, TableGridRowKind.GroupHeader));
                foreach (var index in groups[key])
                {
                    rows.Add(CreateRow(index));
                }
            }

            return rows;
        }

        private TableGridRow CreateRow(int index)
        {
            var cells = new string[cellGetters.Length];
            for (var column = 0; column < cells.Length; column++)
            {
                cells[column] = ValueOf(index, column);
            }

            // 편집을 반영할 때 같은 배열에 쓸 수 있게 기억한다(표는 ApplyEditResults 때 항목을 다시 읽는다).
            rowItems[index] = cells;
            return new TableGridRow(RowID(index), cells);
        }

        private bool Matches(int index, TableGridQueryState query)
        {
            if (deletedRows.Contains(index) || !MatchesSearch(index))
            {
                return false;
            }

            foreach (var filter in query.Filters)
            {
                var value = GetterOf(filter.ColumnID)(index);
                switch (filter.ValueKind)
                {
                    case TableGridColumnValueKind.Choice:
                        if (!Contains(filter.Choices, value))
                        {
                            return false;
                        }

                        break;
                    case TableGridColumnValueKind.Number:
                        if (!InRange(value, filter.Minimum, filter.Maximum))
                        {
                            return false;
                        }

                        break;
                    default:
                        if (!MatchesText(value, filter))
                        {
                            return false;
                        }

                        break;
                }
            }

            return true;
        }

        private bool MatchesSearch(int index)
        {
            // 검색 글자가 어느 열의 값에든 들어 있으면 통과한다(대소문자 무시). 공백뿐이면 거르지 않는다.
            var text = searchText.Trim();
            if (text.Length == 0)
            {
                return true;
            }

            for (var column = 0; column < columnIDs.Length; column++)
            {
                if (ValueOf(index, column).IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool InRange(string value, string minimum, string maximum)
        {
            // 숫자로 읽히지 않는 값(편집으로 비운 값 등)은 범위 필터에 걸리지 않는다.
            var style = NumberStyles.Float | NumberStyles.AllowThousands;
            if (!double.TryParse(value, style, CultureInfo.InvariantCulture, out var number))
            {
                return false;
            }

            var belowMinimum = minimum.Length > 0 && number < double.Parse(minimum, CultureInfo.InvariantCulture);
            var aboveMaximum = maximum.Length > 0 && number > double.Parse(maximum, CultureInfo.InvariantCulture);
            return !belowMinimum && !aboveMaximum;
        }

        private static bool MatchesText(string value, TableGridFilterRule filter)
        {
            var comparison = StringComparison.OrdinalIgnoreCase;
            return filter.Operator switch
            {
                TableGridFilterOperator.Equals => string.Equals(value, filter.Value, comparison),
                TableGridFilterOperator.StartsWith => value.StartsWith(filter.Value, comparison),
                _ => value.IndexOf(filter.Value, comparison) >= 0,
            };
        }

        private static bool Contains(IReadOnlyList<string> values, string value)
        {
            foreach (var candidate in values)
            {
                if (string.Equals(candidate, value, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static int Compare(string left, string right)
        {
            // 숫자로 읽히면 숫자로, 아니면 문자열로 비교한다.
            var leftNumber = double.TryParse(left, NumberStyles.Float, CultureInfo.InvariantCulture, out var a);
            var rightNumber = double.TryParse(right, NumberStyles.Float, CultureInfo.InvariantCulture, out var b);
            return leftNumber && rightNumber ? a.CompareTo(b) : string.CompareOrdinal(left, right);
        }

        private Func<int, string> GetterOf(string columnID)
        {
            var column = Array.IndexOf(columnIDs, columnID);
            if (column < 0)
            {
                throw new ArgumentException($"알 수 없는 열 ID입니다: {columnID}", nameof(columnID));
            }

            return index => ValueOf(index, column);
        }

        private string ValueOf(int index, int column)
        {
            // 편집한 값이 있으면 그 값을, 없으면 생성 규칙의 값을 쓴다. 넣은 행은 ID만 있고 나머지는 빈 글자다.
            if (editedValues.TryGetValue((index, column), out var edited))
            {
                return edited;
            }

            return index < GENERATED_ROW_COUNT || column == 0 ? cellGetters[column](index) : string.Empty;
        }

    #endregion

    #region 편집 (호스트 역할)

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> 셀 변경을 Unity Undo 한 단계로 기록해 데이터와 행 항목 배열에 반영하고,
        /// <br/> 다시 조회하지 않고 표가 같은 행을 다시 읽게 한다.
        /// <br/> 이 예제의 열 값은 모두 글자라 요청 값을 string으로 읽는다.
        /// </summary>
        // ----------------------------------------------------------------------
        private void ApplyEdits(TableGridEditRequest request)
        {
            var accepted = new List<TableGridEditIntent>();
            var cells = new List<(int Row, int Column)>();
            foreach (var edit in request.Edits)
            {
                var column = Array.IndexOf(columnIDs, edit.ColumnID);
                if (column >= 0 && TryParseRowIndex(edit.RowID, out var index))
                {
                    cells.Add((index, column));
                    accepted.Add(edit);
                }
            }

            if (accepted.Count == 0)
            {
                return;
            }

            BeginUndoStep("표 셀 편집", request.Selection);
            for (var position = 0; position < accepted.Count; position++)
            {
                var (row, column) = cells[position];
                var value = accepted[position].Value as string ?? string.Empty;
                editedValues[(row, column)] = value;
                if (rowItems.TryGetValue(row, out var item))
                {
                    item[column] = value;
                }
            }

            SaveEditedCells();
            grid.ApplyEditResults(accepted);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 행 삭제를 Unity Undo 한 단계로 기록하고 지금 쿼리로 다시 조회한다.
        /// </summary>
        // ------------------------------------------------------------
        private void DeleteRows(TableGridDeleteRowsRequest request)
        {
            var indices = new List<int>();
            foreach (var rowID in request.RowIDs)
            {
                if (TryParseRowIndex(rowID, out var index))
                {
                    indices.Add(index);
                }
            }

            if (indices.Count == 0)
            {
                return;
            }

            BeginUndoStep("표 행 삭제", request.Selection);
            deletedRows.UnionWith(indices);
            deletedRowList.Clear();
            deletedRowList.AddRange(deletedRows);

            // 보던 자리를 지키고, 지운 행을 가리키던 선택은 비운다.
            ApplyQuery(grid.QueryState, true);
            grid.Selection = TableGridSelection.None;
        }

        // --------------------------------------------------------------------------------
        /// <summary>
        /// <br/> 행 이동 요청을 Unity Undo 한 단계로 기록한다.
        /// <br/> 대상 행의 앞(Before)이나 뒤(After)에 넣는다.
        /// <br/> 정렬 적용(ApplySort)이면 지금 정렬 순서를 기본 순서로 굳히고 정렬을 푼다.
        /// </summary>
        // --------------------------------------------------------------------------------
        private void MoveRows(TableGridMoveRowsRequest request)
        {
            if (request.Source == TableGridMoveSource.ApplySort)
            {
                ApplySortOrder(request);
                return;
            }

            var moving = new List<int>();
            foreach (var rowID in request.RowIDs)
            {
                if (TryParseRowIndex(rowID, out var index))
                {
                    moving.Add(index);
                }
            }

            if (moving.Count == 0 || !TryParseRowIndex(request.TargetRowID, out var target) || moving.Contains(target))
            {
                return;
            }

            BeginUndoStep("표 행 이동", request.Selection);
            var order = new List<int>(GetRowOrder());
            var movingSet = new HashSet<int>(moving);
            order.RemoveAll(movingSet.Contains);
            var at = order.IndexOf(target) + (request.Placement == TableGridMovePlacement.After ? 1 : 0);
            order.InsertRange(at, moving);
            rowOrder.Clear();
            rowOrder.AddRange(order);

            var query = grid.QueryState;
            if (query.SortDirection.HasValue)
            {
                query = new TableGridQueryState(query.Filters, groupColumnID: query.GroupColumnID);
            }

            ApplyQuery(query, true);
            grid.Selection = TableGridSelection.Rows(request.RowIDs);
            grid.ScrollToSelection();
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> 행 삽입 요청을 Unity Undo 한 단계로 기록한다.
        /// <br/> 새 행 번호를 붙여 기준 행의 앞(Before)·뒤(After)나 끝(End)에 넣고,
        /// <br/> 요청한 행 값(복사한 행 삽입)이 있으면 그 값을 쓴다. 넣은 행을 고른다.
        /// </summary>
        // ----------------------------------------------------------------------
        private void AddRows(TableGridAddRowRequest request)
        {
            var order = new List<int>(GetRowOrder());
            var at = order.Count;
            if (request.Position != TableGridAddPosition.End)
            {
                if (!TryParseRowIndex(request.RowID, out var target))
                {
                    return;
                }

                at = order.IndexOf(target) + (request.Position == TableGridAddPosition.After ? 1 : 0);
            }

            BeginUndoStep("표 행 삽입", request.Selection);
            var count = Math.Max(1, request.RowValues.Count);
            var added = new List<int>(count);
            var rowIDs = new List<string>(count);
            for (var row = 0; row < count; row++)
            {
                var index = RowCount;
                addedRowCount++;
                added.Add(index);
                rowIDs.Add(RowID(index));
                if (row >= request.RowValues.Count)
                {
                    continue;
                }

                foreach (var pair in request.RowValues[row])
                {
                    var column = Array.IndexOf(columnIDs, pair.Key);
                    if (column >= 0)
                    {
                        editedValues[(index, column)] = pair.Value as string ?? string.Empty;
                    }
                }
            }

            order.InsertRange(at, added);
            rowOrder.Clear();
            rowOrder.AddRange(order);
            SaveEditedCells();

            var query = grid.QueryState;
            if (query.SortDirection.HasValue)
            {
                query = new TableGridQueryState(query.Filters, groupColumnID: query.GroupColumnID);
            }

            ApplyQuery(query, true);
            grid.Selection = TableGridSelection.Rows(rowIDs);
            grid.ScrollToSelection();
        }

        private void ApplySortOrder(TableGridMoveRowsRequest request)
        {
            var query = grid.QueryState;
            if (!query.SortDirection.HasValue)
            {
                return;
            }

            // 필터와 상관없이 전체 행을 지금 정렬 기준으로 다시 늘어놓아 기본 순서로 삼는다(같은 값은 지금 순서 유지).
            BeginUndoStep("표 정렬 순서 적용", request.Selection);
            var order = new List<int>(GetRowOrder());
            var position = new int[RowCount];
            for (var at = 0; at < order.Count; at++)
            {
                position[order[at]] = at;
            }

            var getter = GetterOf(query.SortColumnID);
            var sign = query.SortDirection == TableGridSortDirection.Descending ? -1 : 1;
            order.Sort
            (
                (left, right) =>
                {
                    var compared = Compare(getter(left), getter(right));
                    return compared != 0 ? compared * sign : position[left].CompareTo(position[right]);
                }
            );

            rowOrder.Clear();
            rowOrder.AddRange(order);
            ApplyQuery(new TableGridQueryState(query.Filters, groupColumnID: query.GroupColumnID), true);
        }

        private IReadOnlyList<int> GetRowOrder()
        {
            // 행을 옮기거나 넣은 적이 없으면 원래 순서다.
            if (rowOrder.Count == RowCount)
            {
                return rowOrder;
            }

            var natural = new List<int>(RowCount);
            for (var index = 0; index < RowCount; index++)
            {
                natural.Add(index);
            }

            return natural;
        }

        private bool TryParseRowIndex(string rowID, out int index)
        {
            // 행 ID는 "ITM-00001"처럼 1부터 센 번호다.
            index = -1;
            if (rowID == null || !rowID.StartsWith("ITM-", StringComparison.Ordinal)
                || !int.TryParse(rowID.Substring(4), out var number))
            {
                return false;
            }

            index = number - 1;
            return index >= 0 && index < RowCount;
        }

    #endregion

    #region 실행 취소 (호스트 역할)

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> 바꾸기 전 상태를 Unity Undo에 기록하고 단계 번호를 올린다.
        /// <br/> 그 단계의 선택은 번호로 기억해 두었다가 되돌릴 때 다시 보인다.
        /// </summary>
        // ------------------------------------------------------------
        private void BeginUndoStep(string name, TableGridSelection selection)
        {
            Undo.RecordObject(this, name);
            revision++;
            seenRevision = revision;
            stepSelections[revision] = selection;
        }

        private void SaveEditedCells()
        {
            editedCells.Clear();
            foreach (var pair in editedValues)
            {
                var cell = new EditedCell
                {
                    Row = pair.Key.Row,
                    Column = pair.Key.Column,
                    Value = pair.Value,
                };
                editedCells.Add(cell);
            }
        }

        private void RebuildCaches()
        {
            editedValues.Clear();
            foreach (var cell in editedCells)
            {
                editedValues[(cell.Row, cell.Column)] = cell.Value;
            }

            deletedRows.Clear();
            deletedRows.UnionWith(deletedRowList);
        }

        // --------------------------------------------------------------------------------
        /// <summary>
        /// <br/> Unity Undo가 직렬화 상태(값·지운 행·행 순서)를 되돌린 뒤 조회용 사전을 다시 만들고,
        /// <br/> 보던 자리를 지킨 채 다시 조회한다.
        /// <br/> 되돌린(또는 다시 실행한) 단계의 선택을 다시 고르고 그 자리로 스크롤한다.
        /// </summary>
        // --------------------------------------------------------------------------------
        private void HandleUndoRedo()
        {
            if (grid == null || revision == seenRevision)
            {
                return;
            }

            // 실행 취소는 r+1 → r, 다시 실행은 r → r+1이므로 큰 쪽이 움직인 단계다.
            var step = Math.Max(revision, seenRevision);
            seenRevision = revision;

            RebuildCaches();
            ApplyQuery(grid.QueryState, true);

            if (stepSelections.TryGetValue(step, out var selection))
            {
                grid.Selection = selection;
                grid.ScrollToSelection();
            }
        }

    #endregion

    }
}
