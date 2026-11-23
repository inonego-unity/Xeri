/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TEST_TableGridCore.cs
수정일 : 2026-11-23

# 설명
TableGrid 동기 소스 조회·안정적 행 식별·행 항목·검색·결과 변경 때 선택 규칙·설정 검증 계약을 확인한다.
열 선택 규칙(숨긴 열 제거·ID 유지·활성 행), 열 이동 순서(고정 경계 포함), 열 선택 클립보드 계획도 확인한다.
잘라내기 표시, 같은 표 안 옮기기 계획(값이 들어간 원본만 비움, 행 잘라내기도 덮어쓰기), 열 이름 포함 복사의 되붙이기,
삽입 붙여넣기의 행 값 계획과 행 추가 요청의 행 값 보관, 그룹 머리 행을 세지 않는 데이터 행 번호도 확인한다.
그룹 머리 행이 낀 결과에서 이동·행 모으기·클립보드·열 선택 활성 행, 열 최소 폭·고정 열 판정·결과 요약도 확인한다.
Paged 행 선택 표의 좌/우 페이지 넘기기와 넘긴 페이지의 행 고르기도 확인한다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using NUnit;
using NUnit.Framework;

using inonego;
using inonego.Xeri;
using inonego.Xeri.UI;
using inonego.Xeri.UI.TableGrid;

namespace inonego.Xeri.TEST.UI._TableGrid
{
    // ============================================================
    /// <summary>
    /// TableGrid 코어의 데이터 소유권, 동기 조회, 선택 규칙을 검증한다.
    /// </summary>
    // ============================================================
    [TestFixture]
    public sealed class TEST_TableGridCore
    {

    #region 내부 데이터

        private sealed class SampleRow
        {
            // ------------------------------------------------------------
            /// <summary>
            /// 행 식별자를 반환한다.
            /// </summary>
            // ------------------------------------------------------------
            public string ID { get; }

            // ------------------------------------------------------------
            /// <summary>
            /// 행 이름을 반환한다.
            /// </summary>
            // ------------------------------------------------------------
            public string Name { get; }

            // ------------------------------------------------------------
            /// <summary>
            /// 식별자와 이름으로 샘플 행을 만든다.
            /// </summary>
            // ------------------------------------------------------------
            public SampleRow(string id, string name)
            {
                ID = id;
                Name = name;
            }
        }

        private static TableGridColumn NameColumn()
        {
            return new TableGridColumn<SampleRow, string>("name", "이름", row => row.Name, TableGridFormats.Text);
        }

        private static TableGridColumn IDColumn()
        {
            return new TableGridColumn<SampleRow, string>("id", "ID", row => row.ID, TableGridFormats.Text);
        }

        private static List<SampleRow> CreateRows(params string[] ids)
        {
            var rows = new List<SampleRow>(ids.Length);
            foreach (var id in ids)
            {
                rows.Add(new SampleRow(id, id.ToUpperInvariant()));
            }

            return rows;
        }

        private static TableGridListSource<SampleRow> CreateSource(List<SampleRow> rows)
        {
            return new TableGridListSource<SampleRow>(rows, row => row.ID);
        }

        private static TableGridModel CreateModel(params string[] ids)
        {
            var model = new TableGridModel();
            var columns = new[]
            {
                IDColumn(),
                NameColumn(),
            };
            model.Columns.SetColumns(columns);
            model.Rows.SetSource(CreateSource(CreateRows(ids)));

            return model;
        }

        private static List<string> CollectIDs(ITableGridSource source)
        {
            var ids = new List<string>(source.Count);
            for (var index = 0; index < source.Count; index++)
            {
                ids.Add(source.GetRow(index).ID);
            }

            return ids;
        }

    #endregion

    #region 동기 조회

        // ------------------------------------------------------------
        /// <summary>
        /// 목록 source는 위치의 행과 ID의 위치를 바로 답한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void ListSource_위치와_ID를_바로_조회한다()
        {
            var data = CreateRows("row-a", "row-b", "row-c");
            ITableGridSource source = CreateSource(data);

            Assert.AreEqual(3, source.Count);
            Assert.AreEqual("row-b", source.GetRow(1).ID);
            Assert.AreSame(data[1], source.GetRow(1).Item);
            Assert.AreSame(source.GetRow(1), source.GetRow(1));
            Assert.AreEqual("ROW-B", NameColumn().GetText(source.GetRow(1).Item));
            Assert.AreEqual(2, source.IndexOf("row-c"));
            Assert.AreEqual(-1, source.IndexOf("missing"));
            Assert.AreEqual(-1, source.IndexOf(null));
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 검색·정렬이 바뀌면 ID의 위치도 결과 순서를 따른다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void ListSource_ID위치는_검색과_정렬을_따른다()
        {
            var source = CreateSource(CreateRows("row-a", "row-b", "row-c"));
            ITableGridSource grid = source;
            var columns = new[]
            {
                IDColumn(),
            };

            source.SetSort(IDColumn(), TableGridSortDirection.Descending);
            Assert.AreEqual(0, grid.IndexOf("row-c"));
            Assert.AreEqual(2, grid.IndexOf("row-a"));

            source.SetSearch("row-b", columns);
            Assert.AreEqual(1, grid.Count);
            Assert.AreEqual(0, grid.IndexOf("row-b"));
            Assert.AreEqual(-1, grid.IndexOf("row-a"));
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 항목 목록을 바꾼 뒤 Invalidate하면 새 항목의 ID를 찾는다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void ListSource_Invalidate하면_새_항목_ID를_찾는다()
        {
            var data = CreateRows("row-a", "row-b");
            var source = CreateSource(data);
            ITableGridSource grid = source;
            Assert.AreEqual(1, grid.IndexOf("row-b"));

            data[1] = new SampleRow("row-z", "Z");
            source.Invalidate();

            Assert.AreEqual(-1, grid.IndexOf("row-b"));
            Assert.AreEqual(1, grid.IndexOf("row-z"));
            Assert.AreSame(data[1], grid.GetRow(1).Item);
        }

    #endregion

    #region 검색

        // ------------------------------------------------------------
        /// <summary>
        /// 목록 source는 열 글자에 검색 글자가 든 항목만 남긴다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void ListSource_검색은_열_글자로_거른다()
        {
            var data = new List<SampleRow>
            {
                new("row-a", "Alpha"),
                new("row-b", "Beta"),
                new("row-c", "Alphabet")
            };
            var source = CreateSource(data);
            var columns = new[]
            {
                NameColumn(),
            };

            source.SetSearch("alpha", columns);

            var expected = new[]
            {
                "row-a", "row-c",
            };
            CollectionAssert.AreEqual(expected, CollectIDs(source));

            source.SetSearch(string.Empty, columns);
            Assert.AreEqual(3, source.Count);
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// 목록 source는 검색 → 필터 → 정렬 순으로 적용하고 같은 값은 원본 순서를 지킨다.
        /// </summary>
        // ----------------------------------------------------------------------
        [Test]
        public void ListSource_검색_필터_정렬_순서로_적용하고_안정정렬한다()
        {
            var data = new List<SampleRow>
            {
                new("row-a", "Beta"),
                new("row-b", "Alpha"),
                new("row-c", "Beta"),
                new("row-d", "Gamma"),
                new("row-e", "Alpha"),
            };
            var source = CreateSource(data);
            var column = NameColumn();
            var columns = new[]
            {
                column,
            };

            // 공백뿐인 검색 글자는 거르지 않는다.
            source.SetSearch("   ", columns);
            Assert.AreEqual(5, source.Count);

            source.SetSearch("a", columns);
            source.SetFilter(row => row.ID != "row-d");
            source.SetSort(column, TableGridSortDirection.Ascending);

            var expected = new[]
            {
                "row-b", "row-e", "row-a", "row-c",
            };
            CollectionAssert.AreEqual(expected, CollectIDs(source));
            Assert.AreEqual(4, source.Count);
            Assert.AreEqual(2, source.IndexOf(data[0]));
            Assert.AreEqual(-1, source.IndexOf(data[3]));

            source.SetSort(column, TableGridSortDirection.Descending);
            Assert.AreSame(data[0], source[0]);
            Assert.AreSame(data[2], source[1]);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 행 목록 source 검색은 남는 데이터 행이 있는 그룹 머리만 남긴다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void RowListSource_검색은_남는_행이_있는_그룹머리만_남긴다()
        {
            var rows = new[]
            {
                new TableGridRow("group-1", "첫 묶음", TableGridRowKind.GroupHeader),
                new TableGridRow("row-a", new SampleRow("row-a", "Alpha")),
                new TableGridRow("group-2", "둘째 묶음", TableGridRowKind.GroupHeader),
                new TableGridRow("row-b", new SampleRow("row-b", "Beta")),
            };
            var source = new TableGridRowListSource(rows);
            var columns = new[]
            {
                NameColumn(),
            };

            source.SetSearch("beta", columns);

            Assert.AreEqual(2, source.Count);
            Assert.AreEqual("group-2", source.GetRow(0).ID);
            Assert.AreEqual("둘째 묶음", source.GetRow(0).Title);
            Assert.AreEqual("row-b", source.GetRow(1).ID);
            Assert.AreEqual(1, source.IndexOf("row-b"));
            Assert.AreEqual(-1, source.IndexOf("row-a"));

            source.SetSearch(string.Empty, columns);
            Assert.AreEqual(3, source.IndexOf("row-b"));
        }

    #endregion

    #region 선택 규칙

        // ----------------------------------------------------------------------
        /// <summary>
        /// 행이 많아도 표는 모든 위치의 행과 ID 위치를 바로 찾는다(먼 Shift 범위의 전제).
        /// </summary>
        // ----------------------------------------------------------------------
        [Test]
        public void Model_멀리_떨어진_행도_위치와_ID를_찾는다()
        {
            var ids = new string[5000];
            for (var index = 0; index < ids.Length; index++)
            {
                ids[index] = $"row-{index}";
            }

            var model = CreateModel(ids);

            Assert.AreEqual(0, model.Rows.IndexOf("row-0"));
            Assert.AreEqual(4999, model.Rows.IndexOf("row-4999"));
            Assert.IsTrue(model.TryGetDataRowID(2500, out var rowID));
            Assert.AreEqual("row-2500", rowID);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 결과에서 빠진 행은 행 선택에서 빠지고 변경을 알린다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void Model_결과에서_빠진_행은_행선택에서_뺀다()
        {
            var model = CreateModel("row-a", "row-b", "row-c");
            var selected = new[]
            {
                "row-a", "row-b", "row-c",
            };
            model.SetSelection(TableGridSelection.Rows(selected), out _);
            TableGridSelection adjustedFrom = null;
            model.SelectionAdjusted += previous => adjustedFrom = previous;

            model.Rows.SetSource(CreateSource(CreateRows("row-a", "row-c")));

            var expected = new[]
            {
                "row-a", "row-c",
            };
            CollectionAssert.AreEqual(expected, model.Selection.Current.RowIDs);
            Assert.IsNotNull(adjustedFrom);
            Assert.AreEqual(3, adjustedFrom.RowIDs.Count);

            model.Rows.SetSource(CreateSource(CreateRows("row-b")));
            Assert.AreEqual(TableGridSelectionKind.None, model.Selection.Current.Kind);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 정렬로 순서가 바뀌어도 행 선택은 고른 행을 유지한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void Model_순서가_바뀌어도_행선택은_고른_행을_유지한다()
        {
            var model = CreateModel("row-a", "row-b", "row-c");
            var selected = new[]
            {
                "row-b",
            };
            var selection = TableGridSelection.Rows(selected);
            model.SetSelection(selection, out _);

            model.Rows.SetSource(CreateSource(CreateRows("row-c", "row-b", "row-a")));

            Assert.AreSame(selection, model.Selection.Current);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 순서가 바뀌면 셀 선택은 같은 자리의 새 행을 가리킨다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void Model_순서가_바뀌면_셀선택은_같은_자리를_유지한다()
        {
            var model = CreateModel("row-a", "row-b", "row-c", "row-d", "row-e");
            var anchor = new TableGridCellAddress("row-b", "id");
            var focus = new TableGridCellAddress("row-d", "name");
            model.SetSelection(TableGridSelection.CellRange(anchor, focus), out _);

            model.Rows.SetSource(CreateSource(CreateRows("row-e", "row-d", "row-c", "row-b", "row-a")));

            var current = model.Selection.Current;
            Assert.AreEqual(new TableGridCellAddress("row-d", "id"), current.Anchor);
            Assert.AreEqual(new TableGridCellAddress("row-b", "name"), current.Focus);
            Assert.AreEqual(1, model.Selection.RowMin);
            Assert.AreEqual(3, model.Selection.RowMax);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 행 수가 줄면 셀 선택은 남은 행 안으로 잘리고, 행이 없으면 해제된다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void Model_행이_줄면_셀선택은_남은_안으로_자른다()
        {
            var model = CreateModel("row-a", "row-b", "row-c", "row-d", "row-e");
            var anchor = new TableGridCellAddress("row-b", "id");
            var focus = new TableGridCellAddress("row-d", "id");
            model.SetSelection(TableGridSelection.CellRange(anchor, focus), out _);

            model.Rows.SetSource(CreateSource(CreateRows("row-a", "row-b", "row-c")));
            Assert.AreEqual(new TableGridCellAddress("row-b", "id"), model.Selection.Current.Anchor);
            Assert.AreEqual(new TableGridCellAddress("row-c", "id"), model.Selection.Current.Focus);

            // 시작 위치가 끝을 넘으면 마지막 행으로 옮긴다.
            model.Rows.SetSource(CreateSource(CreateRows("row-x")));
            Assert.AreEqual(new TableGridCellAddress("row-x", "id"), model.Selection.Current.Anchor);
            Assert.AreEqual(new TableGridCellAddress("row-x", "id"), model.Selection.Current.Focus);

            model.Rows.SetSource(CreateSource(CreateRows()));
            Assert.AreEqual(TableGridSelectionKind.None, model.Selection.Current.Kind);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 행 이동을 요청한 뒤의 한 번은 셀 선택이 옮긴 행을 따라간다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void Model_행이동_뒤에는_셀선택이_옮긴_행을_따라간다()
        {
            var model = CreateModel("row-a", "row-b", "row-c");
            var cell = new TableGridCellAddress("row-c", "id");
            var selection = TableGridSelection.SingleCell(cell);
            model.SetSelection(selection, out _);

            model.FollowSelectionOnce();
            model.Rows.SetSource(CreateSource(CreateRows("row-c", "row-a", "row-b")));
            Assert.AreSame(selection, model.Selection.Current);
            Assert.AreEqual(0, model.Selection.ActiveRow);

            // 한 번 쓴 뒤에는 다시 같은 자리 규칙이다.
            model.Rows.SetSource(CreateSource(CreateRows("row-a", "row-b", "row-c")));
            Assert.AreEqual(new TableGridCellAddress("row-a", "id"), model.Selection.Current.Focus);
        }

    #endregion

    #region 열 선택

        private static TableGridColumn ValueColumn(string id)
        {
            // 셀 글자는 "행 ID.열 ID"로, 복사 순서와 대상을 바로 읽을 수 있게 한다.
            return new TableGridColumn<SampleRow, string>
            (
                id, id, row => row.ID + "." + id, TableGridFormats.Text, edit: TableGridCellEdit.Editor()
            );
        }

        private static TableGridModel CreateColumnModel(params string[] rowIDs)
        {
            var model = new TableGridModel();
            var columns = new[]
            {
                ValueColumn("a"), ValueColumn("b"), ValueColumn("c"), ValueColumn("d"),
            };
            model.Columns.SetColumns(columns);
            model.Rows.SetSource(CreateSource(CreateRows(rowIDs)));

            return model;
        }

        private static List<string> CollectVisibleIDs(TableGridModel model)
        {
            var ids = new List<string>();
            foreach (var column in model.Columns.VisibleColumns)
            {
                ids.Add(column.ID);
            }

            return ids;
        }

        private static HashSet<string> SetOf(params string[] ids)
        {
            return new HashSet<string>(ids, StringComparer.Ordinal);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 숨긴 열은 열 선택에서 빠지고, 남은 열이 없으면 해제된다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void Model_숨긴_열은_열선택에서_빼고_모두_빠지면_해제한다()
        {
            var model = CreateColumnModel("row-a", "row-b");
            var selected = new[]
            {
                "b", "c",
            };
            model.SetSelection(TableGridSelection.Columns(selected), out _);
            TableGridSelection adjustedFrom = null;
            model.SelectionAdjusted += previous => adjustedFrom = previous;

            var visible = new[]
            {
                "a", "c", "d",
            };
            model.Columns.SetVisibleColumns(visible);

            var expected = new[]
            {
                "c",
            };
            CollectionAssert.AreEqual(expected, model.Selection.Current.ColumnIDs);
            Assert.IsNotNull(adjustedFrom);
            Assert.AreEqual(2, adjustedFrom.ColumnIDs.Count);

            var remaining = new[]
            {
                "a", "d",
            };
            model.Columns.SetVisibleColumns(remaining);
            Assert.AreEqual(TableGridSelectionKind.None, model.Selection.Current.Kind);
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// 열을 옮겨도 열 선택은 고른 열을 따라가고 그 열의 모든 셀이 범위 안이다.
        /// </summary>
        // ----------------------------------------------------------------------
        [Test]
        public void Model_열을_옮겨도_열선택은_고른_열을_따라간다()
        {
            var model = CreateColumnModel("row-a", "row-b");
            var selected = new[]
            {
                "c",
            };
            var selection = TableGridSelection.Columns(selected);
            model.SetColumnAnchor("c");
            model.SetSelection(selection, out _);
            Assert.AreEqual(2, model.Selection.ActiveColumn);
            Assert.AreEqual(0, model.Selection.ActiveRow);

            var moved = new[]
            {
                "c", "a", "b", "d",
            };
            model.Columns.SetVisibleColumns(moved);

            var positions = new[]
            {
                0,
            };
            Assert.AreSame(selection, model.Selection.Current);
            CollectionAssert.AreEqual(positions, model.Selection.ColumnPositions);
            Assert.AreEqual(0, model.Selection.ActiveColumn);
            Assert.IsFalse(model.Selection.HasCellRange);
            Assert.IsTrue((model.Selection.GetCellMarks(1, 0) & TableGridCellMarks.InRange) != 0);
            Assert.AreEqual(TableGridCellMarks.None, model.Selection.GetCellMarks(1, 1));
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// 열 선택은 활성 행을 잇고, 그 행이 결과에서 빠지면 첫 데이터 행으로 둔다.
        /// </summary>
        // ----------------------------------------------------------------------
        [Test]
        public void Model_열선택의_활성_행이_빠지면_첫_데이터_행으로_둔다()
        {
            var model = CreateColumnModel("row-a", "row-b", "row-c");
            model.SetSelection(TableGridSelection.SingleCell(new TableGridCellAddress("row-b", "a")), out _);

            var selected = new[]
            {
                "a",
            };
            model.SetColumnAnchor("a");
            model.SetSelection(TableGridSelection.Columns(selected), out _);
            Assert.AreEqual(1, model.Selection.ActiveRow);

            model.Rows.SetSource(CreateSource(CreateRows("row-c", "row-a")));
            Assert.AreEqual("row-c", model.Selection.ColumnActiveRowID);
            Assert.AreEqual(0, model.Selection.ActiveRow);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 한 칸 이동은 고른 열 덩어리를 옆 열과 바꾸고, 끝이면 순서가 그대로다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void ColumnSet_한_칸_이동은_고른_열을_옆_열과_바꾼다()
        {
            var order = new[]
            {
                "a", "b", "c", "d", "e",
            };
            var block = SetOf("c", "d");
            var spread = SetOf("b", "d");
            var first = SetOf("a");

            var left = TableGridColumnSet.MoveColumns(order, block, TableGridColumnSet.GetStepIndex(order, block, -1));
            var right = TableGridColumnSet.MoveColumns(order, block, TableGridColumnSet.GetStepIndex(order, block, 1));
            var gathered = TableGridColumnSet.MoveColumns(order, spread, TableGridColumnSet.GetStepIndex(order, spread, -1));
            var edge = TableGridColumnSet.MoveColumns(order, first, TableGridColumnSet.GetStepIndex(order, first, -1));

            var expectedLeft = new[]
            {
                "a", "c", "d", "b", "e",
            };
            var expectedRight = new[]
            {
                "a", "b", "e", "c", "d",
            };
            var expectedGathered = new[]
            {
                "b", "d", "a", "c", "e",
            };
            CollectionAssert.AreEqual(expectedLeft, left);
            CollectionAssert.AreEqual(expectedRight, right);
            CollectionAssert.AreEqual(expectedGathered, gathered);
            CollectionAssert.AreEqual(order, edge);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> 고정 열 수는 그대로이고, 놓은 구역에 들어가며
        /// <br/> 밀려난 열이 반대 구역으로 넘어간다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void ColumnSet_고정_경계를_넘어_놓으면_밀려난_열이_반대_구역으로_간다()
        {
            var order = new[]
            {
                "a", "b", "c", "d",
            };
            const int frozenCount = 2;

            // 스크롤 열 c를 고정 구역 끝 경계에 놓으면 고정 구역에 들고 b가 스크롤 구역으로 밀린다.
            var intoFrozen = SetOf("c");
            var frozenIndex = TableGridColumnSet.GetDropIndex(order, intoFrozen, 2, frozenCount, true);
            var expectedFrozen = new[]
            {
                "a", "c", "b", "d",
            };
            CollectionAssert.AreEqual(expectedFrozen, TableGridColumnSet.MoveColumns(order, intoFrozen, frozenIndex));

            // 고정 열 a를 같은 경계의 스크롤 구역 쪽에 놓으면 스크롤 구역 첫 열이 되고 c가 고정 구역으로 넘어온다.
            var intoScroll = SetOf("a");
            var scrollIndex = TableGridColumnSet.GetDropIndex(order, intoScroll, 2, frozenCount, false);
            var expectedScroll = new[]
            {
                "b", "c", "a", "d",
            };
            CollectionAssert.AreEqual(expectedScroll, TableGridColumnSet.MoveColumns(order, intoScroll, scrollIndex));

            // 같은 경계라도 고정 구역 쪽에 놓으면 고정 구역 안에 남는다.
            var stayIndex = TableGridColumnSet.GetDropIndex(order, intoScroll, 2, frozenCount, true);
            var expectedStay = new[]
            {
                "b", "a", "c", "d",
            };
            CollectionAssert.AreEqual(expectedStay, TableGridColumnSet.MoveColumns(order, intoScroll, stayIndex));

            // 적용해도 고정 열 수는 그대로다.
            var model = CreateColumnModel("row-a");
            model.Columns.FrozenCount = frozenCount;
            model.Columns.SetVisibleColumns(TableGridColumnSet.MoveColumns(order, intoFrozen, frozenIndex));
            Assert.AreEqual(frozenCount, model.Columns.FrozenCount);
            CollectionAssert.AreEqual(expectedFrozen, CollectVisibleIDs(model));
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 열 선택 복사는 모든 데이터 행의 고른 열을 보기 순서로 잇는다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void Clipboard_열선택_복사는_모든_행의_고른_열을_보기순서로_잇는다()
        {
            var model = CreateColumnModel("row-a", "row-b");
            var selected = new[]
            {
                "c", "a",
            };
            model.SetSelection(TableGridSelection.Columns(selected), out _);

            var bundle = TableGridClipboard.BuildSelection(model, false);
            var withHeaders = TableGridClipboard.BuildSelection(model, true);

            Assert.AreEqual("row-a.a\trow-a.c\nrow-b.a\trow-b.c", bundle.Text);
            Assert.AreEqual(2, bundle.Rows.Count);
            Assert.AreEqual("a\tc\nrow-a.a\trow-a.c\nrow-b.a\trow-b.c", withHeaders.Text);
            Assert.IsNull(TableGridClipboard.CheckCopy(model));
        }

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> 열 선택 지우기는 그 열 전체를 비우고,
        /// <br/> 떨어진 열은 잘라낼 수 없다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void Clipboard_열선택_지우기는_열_전체를_비우고_떨어진_열은_잘라내지_않는다()
        {
            var model = CreateColumnModel("row-a", "row-b", "row-c");
            var spread = new[]
            {
                "a", "c",
            };
            model.SetSelection(TableGridSelection.Columns(spread), out _);

            var plan = TableGridClipboard.BuildClear(model);
            Assert.IsNull(plan.Error);
            Assert.AreEqual(6, plan.Edits.Count);
            foreach (var edit in plan.Edits)
            {
                Assert.IsTrue(edit.ColumnID == "a" || edit.ColumnID == "c");
                Assert.AreEqual(string.Empty, edit.Value);
            }

            Assert.IsNotNull(TableGridClipboard.CheckCut(model));

            var adjacent = new[]
            {
                "b", "a",
            };
            model.SetSelection(TableGridSelection.Columns(adjacent), out _);
            Assert.IsNull(TableGridClipboard.CheckCut(model));
        }

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> 열 선택 붙여넣기는 첫 데이터 행부터 고른 열에 채우고,
        /// <br/> 값이 하나면 고른 열 전체를 채운다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void Clipboard_열선택_붙여넣기는_첫_행부터_채우고_값_하나면_열_전체를_채운다()
        {
            var model = CreateColumnModel("row-a", "row-b", "row-c");
            var selected = new[]
            {
                "b",
            };
            model.SetSelection(TableGridSelection.Columns(selected), out _);

            var fill = TableGridClipboard.BuildPaste(model, TableGridClipboardBundle.FromText("x"));
            Assert.AreEqual(3, fill.Edits.Count);
            foreach (var edit in fill.Edits)
            {
                Assert.AreEqual("b", edit.ColumnID);
                Assert.AreEqual("x", edit.Value);
            }

            // 원본이 고른 열보다 넓으면 마지막 고른 열 뒤로 잇는다.
            var block = TableGridClipboard.BuildPaste(model, TableGridClipboardBundle.FromText("1\t2\n3\t4"));
            Assert.AreEqual(4, block.Edits.Count);
            Assert.AreEqual("row-a", block.Edits[0].RowID);
            Assert.AreEqual("b", block.Edits[0].ColumnID);
            Assert.AreEqual("c", block.Edits[1].ColumnID);
            Assert.AreEqual("row-b", block.Edits[3].RowID);
            Assert.AreEqual("4", block.Edits[3].Value);
        }

    #endregion

    #region 잘라내기와 옮기기

        private static void SelectCells(TableGridModel model, string anchorRow, string anchorColumn, string focusRow, string focusColumn)
        {
            var anchor = new TableGridCellAddress(anchorRow, anchorColumn);
            var focus = new TableGridCellAddress(focusRow, focusColumn);
            model.SetSelection(TableGridSelection.CellRange(anchor, focus), out _);
        }

        private static HashSet<string> CollectCleared(TableGridEditPlan plan)
        {
            var cleared = new HashSet<string>(StringComparer.Ordinal);
            foreach (var edit in plan.Edits)
            {
                if (string.Empty.Equals(edit.Value))
                {
                    cleared.Add(edit.RowID + "." + edit.ColumnID);
                }
            }

            return cleared;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> 잘라내기는 복사한 자리만 표시하고,
        /// <br/> 셀 잘라내기는 셀로, 행 잘라내기는 행으로 표시한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void Cut_복사한_자리를_종류대로_표시한다()
        {
            var model = CreateColumnModel("row-a", "row-b", "row-c");
            SelectCells(model, "row-a", "a", "row-b", "b");
            model.Edit.SetCopied(TableGridClipboard.BuildSelection(model, false), true);

            Assert.IsTrue(model.Edit.IsCut);
            Assert.IsTrue(model.Edit.IsCutCell("row-b", "b"));
            Assert.IsFalse(model.Edit.IsCutCell("row-c", "a"));
            Assert.IsFalse(model.Edit.IsCutCell("row-a", "c"));
            Assert.IsFalse(model.Edit.IsCutRow("row-a"));

            var rowIDs = new[]
            {
                "row-c",
            };
            model.SetSelection(TableGridSelection.Rows(rowIDs), out _);
            model.Edit.SetCopied(TableGridClipboard.BuildSelection(model, false), true);
            Assert.IsTrue(model.Edit.IsCutRow("row-c"));
            Assert.IsFalse(model.Edit.IsCutCell("row-c", "a"));

            // 복사하면 앞의 잘라내기 표시는 지우고 마지막 복사는 남긴다.
            model.Edit.SetCopied(TableGridClipboard.BuildSelection(model, false), false);
            Assert.IsFalse(model.Edit.IsCut);
            Assert.IsFalse(model.Edit.IsCutRow("row-c"));
            Assert.IsNotNull(model.Edit.LastCopied);
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> 같은 표 안 옮기기는 붙여넣은 뒤 원본을 비우되,
        /// <br/> 새 값으로 덮인 칸과 값이 들어가지 못한(표 밖) 원본은 비우지 않는다.
        /// </summary>
        // ----------------------------------------------------------------------
        [Test]
        public void Move_값이_들어간_원본만_비우고_덮인_칸은_둔다()
        {
            var model = CreateColumnModel("row-a", "row-b", "row-c");
            SelectCells(model, "row-a", "a", "row-b", "b");
            var bundle = TableGridClipboard.BuildSelection(model, false);

            // 한 칸 오른쪽 아래로 옮기면 겹친 (row-b, b)는 새 값으로 덮이므로 비우지 않는다.
            SelectCells(model, "row-b", "b", "row-b", "b");
            var overlap = TableGridClipboard.BuildMove(model, bundle);
            Assert.IsNull(overlap.Error);
            var overlapCleared = new[]
            {
                "row-a.a", "row-a.b", "row-b.a",
            };
            Assert.AreEqual(7, overlap.Edits.Count);
            CollectionAssert.AreEquivalent(overlapCleared, CollectCleared(overlap));

            // 마지막 행으로 옮기면 표 밖으로 나간 둘째 행 값은 들어가지 못하므로 그 원본은 남긴다.
            SelectCells(model, "row-c", "c", "row-c", "c");
            var clipped = TableGridClipboard.BuildMove(model, bundle);
            var clippedCleared = new[]
            {
                "row-a.a", "row-a.b",
            };
            Assert.AreEqual(2, clipped.Outside);
            CollectionAssert.AreEquivalent(clippedCleared, CollectCleared(clipped));
        }

        private static void SelectRows(TableGridModel model, params string[] rowIDs)
        {
            model.SetSelection(TableGridSelection.Rows(rowIDs), out _);
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> 행 잘라내기를 같은 표에 붙여넣으면 덮어쓰고 원본 행 값을 비운다.
        /// <br/> 행은 옮기지 않는다(행 이동은 잘라낸 행 삽입).
        /// </summary>
        // ----------------------------------------------------------------------
        [Test]
        public void Move_행_잘라내기도_덮어쓰고_원본_행을_비운다()
        {
            var model = CreateColumnModel("row-a", "row-b", "row-c");
            SelectRows(model, "row-a");
            Assert.IsNull(TableGridClipboard.CheckCut(model));
            var bundle = TableGridClipboard.BuildSelection(model, false);

            SelectRows(model, "row-c");
            var plan = TableGridClipboard.BuildMove(model, bundle);
            var cleared = new[]
            {
                "row-a.a", "row-a.b", "row-a.c", "row-a.d",
            };
            Assert.IsNull(plan.Error);
            Assert.AreEqual(8, plan.Edits.Count);
            Assert.AreEqual("row-c", plan.Edits[0].RowID);
            Assert.AreEqual("row-a.a", plan.Edits[0].Value);
            CollectionAssert.AreEquivalent(cleared, CollectCleared(plan));
        }

        // --------------------------------------------------------------------------------
        /// <summary>
        /// <br/> 삽입 붙여넣기는 클립보드 행마다 첫 표시 열부터 새 행 값을 만든다.
        /// <br/> 표 안 행 복사는 값 묶음으로, 밖에서 온 글자는 열마다 읽고, 표시 열보다 넓으면 센다.
        /// </summary>
        // --------------------------------------------------------------------------------
        [Test]
        public void Insert_클립보드_행마다_첫_열부터_새_행_값을_만든다()
        {
            var model = CreateColumnModel("row-a", "row-b");
            SelectRows(model, "row-a", "row-b");
            var copied = TableGridClipboard.BuildInsert(model, TableGridClipboard.BuildSelection(model, false));
            Assert.IsNull(copied.Error);
            Assert.AreEqual(2, copied.Rows.Count);
            Assert.AreEqual("row-a.a", copied.Rows[0]["a"]);
            Assert.AreEqual("row-b.d", copied.Rows[1]["d"]);

            var outside = TableGridClipboard.BuildInsert(model, TableGridClipboardBundle.FromText("1\t2\t3\t4\t5"));
            Assert.AreEqual(1, outside.Rows.Count);
            Assert.AreEqual(4, outside.Rows[0].Count);
            Assert.AreEqual("2", outside.Rows[0]["b"]);
            Assert.AreEqual(1, outside.Outside);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> 행 추가 요청은 행 값을 복사해 보관하고,
        /// <br/> 행 값이 없으면 빈 목록(빈 행 하나)이다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void AddRowRequest_행_값을_복사해_보관한다()
        {
            var values = new Dictionary<string, object>
            {
                ["a"] = "x",
            };
            var rowValues = new List<IReadOnlyDictionary<string, object>>
            {
                values,
            };
            var request = new TableGridAddRowRequest(TableGridAddPosition.Before, "row-a", null, rowValues);
            values["a"] = "changed";

            Assert.AreEqual(1, request.RowValues.Count);
            Assert.AreEqual("x", request.RowValues[0]["a"]);
            Assert.AreEqual(0, new TableGridAddRowRequest(TableGridAddPosition.End, null).RowValues.Count);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 열 이름 포함 복사를 같은 표에 되붙이면 열 이름 줄은 빠진다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void Clipboard_열이름_포함_복사를_되붙이면_열이름_줄은_빠진다()
        {
            var model = CreateColumnModel("row-a", "row-b");
            SelectCells(model, "row-a", "a", "row-b", "b");
            var bundle = TableGridClipboard.BuildSelection(model, true);
            Assert.IsTrue(bundle.Matches(bundle.Text));

            SelectCells(model, "row-a", "c", "row-a", "c");
            var plan = TableGridClipboard.BuildPaste(model, bundle);
            Assert.AreEqual(4, plan.Edits.Count);
            Assert.AreEqual("row-a", plan.Edits[0].RowID);
            Assert.AreEqual("row-a.a", plan.Edits[0].Value);
        }

    #endregion

    #region 그룹 머리 행

        // 결과: [그룹1, row-a, row-b, 그룹2, row-c]. 열은 a~d(글자 "행 ID.열 ID").
        private static TableGridModel CreateGroupedModel()
        {
            var model = new TableGridModel();
            var columns = new[]
            {
                ValueColumn("a"), ValueColumn("b"), ValueColumn("c"), ValueColumn("d"),
            };
            var rows = new List<TableGridRow>
            {
                new("group-1", "그룹 1", TableGridRowKind.GroupHeader),
                new("row-a", new SampleRow("row-a", "A")),
                new("row-b", new SampleRow("row-b", "B")),
                new("group-2", "그룹 2", TableGridRowKind.GroupHeader),
                new("row-c", new SampleRow("row-c", "C")),
            };
            model.Columns.SetColumns(columns);
            model.Rows.SetSource(new TableGridRowListSource(rows));

            return model;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 세로 이동·처음·끝은 그룹 머리 행을 건너뛴다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void Navigator_세로_이동은_그룹_머리_행을_건너뛴다()
        {
            var rows = CreateGroupedModel().Rows;

            Assert.IsTrue(TableGridNavigator.TryMove(rows, 4, 2, 0, TableGridMove.Down, 10, out var down, out _));
            Assert.AreEqual(4, down);
            Assert.IsTrue(TableGridNavigator.TryMove(rows, 4, 4, 0, TableGridMove.Up, 10, out var up, out _));
            Assert.AreEqual(2, up);
            Assert.IsTrue(TableGridNavigator.TryMove(rows, 4, 4, 0, TableGridMove.First, 10, out var first, out _));
            Assert.AreEqual(1, first);
            Assert.IsTrue(TableGridNavigator.TryMove(rows, 4, 1, 0, TableGridMove.Last, 10, out var last, out _));
            Assert.AreEqual(4, last);
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> 데이터 행 조회와 행 모으기는 그룹 머리 행을 빼고 화면 순서로 모은다.
        /// <br/> 데이터 행 수 경계는 결과 밖 위치를 잘라 낸다.
        /// </summary>
        // ----------------------------------------------------------------------
        [Test]
        public void Model_데이터_행_조회와_모으기는_머리_행을_뺀다()
        {
            var model = CreateGroupedModel();
            var all = new[]
            {
                1, 2, 4,
            };
            var firstTwo = new[]
            {
                1, 2,
            };
            var selectedPositions = new[]
            {
                1, 4,
            };
            var selectedIDs = new[]
            {
                "row-a", "row-c",
            };

            Assert.IsFalse(model.Rows.TryGetDataRow(0, out _));
            Assert.IsTrue(model.Rows.TryGetDataRow(1, out _));
            CollectionAssert.AreEqual(all, model.CollectDataRowPositions(0, 4));
            CollectionAssert.AreEqual(firstTwo, model.CollectDataRowPositions(4, 0, 2));

            SelectRows(model, "row-c", "row-a");
            CollectionAssert.AreEqual(selectedPositions, model.CollectSelectedRowPositions());
            CollectionAssert.AreEqual(selectedIDs, model.ToRowIDs(model.CollectSelectedRowPositions()));

            // 선택에 머리 행 ID가 섞여 있으면 그 행은 뺀다.
            var withHeader = new[]
            {
                4,
            };
            SelectRows(model, "group-2", "row-c");
            CollectionAssert.AreEqual(withHeader, model.CollectSelectedRowPositions());

            Assert.AreEqual(0, model.Rows.CountDataRowsBefore(-1));
            Assert.AreEqual(3, model.Rows.CountDataRowsBefore(99));
        }

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> 머리 행을 낀 범위는 데이터 행만 복사·지우고,
        /// <br/> 붙여넣기는 머리 행을 건너뛰며 남는 원본 행은 범위 밖으로 센다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void Clipboard_머리_행을_낀_범위는_데이터_행만_다룬다()
        {
            var model = CreateGroupedModel();
            SelectCells(model, "row-a", "a", "row-c", "a");
            var bundle = TableGridClipboard.BuildSelection(model, false);
            Assert.AreEqual("row-a.a\nrow-b.a\nrow-c.a", bundle.Text);
            Assert.AreEqual(3, TableGridClipboard.BuildClear(model).Edits.Count);

            var column = new[]
            {
                "b",
            };
            model.SetSelection(TableGridSelection.Columns(column), out _);
            Assert.AreEqual("row-a.b\nrow-b.b\nrow-c.b", TableGridClipboard.BuildSelection(model, false).Text);

            SelectCells(model, "row-b", "a", "row-b", "a");
            var paste = TableGridClipboard.BuildPaste(model, bundle);
            Assert.AreEqual(2, paste.Edits.Count);
            Assert.AreEqual("row-b", paste.Edits[0].RowID);
            Assert.AreEqual("row-c", paste.Edits[1].RowID);
            Assert.AreEqual(1, paste.Outside);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 열 선택의 활성 행은 맨 위 머리 행이 아니라 첫 데이터 행이다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void Model_열선택_활성_행은_첫_데이터_행이다()
        {
            var model = CreateGroupedModel();
            var selected = new[]
            {
                "a",
            };
            model.SetColumnAnchor("a");
            model.SetSelection(TableGridSelection.Columns(selected), out _);

            Assert.AreEqual("row-a", model.Selection.ColumnActiveRowID);
            Assert.AreEqual(1, model.Selection.ActiveRow);
        }

    #endregion

    #region 열 폭·고정 영역·결과 요약

        // --------------------------------------------------------------------------------
        /// <summary>
        /// <br/> 사용자 최소 폭은 고정 열은 32px, 가변 열은 정책 최소와 32px 중 큰 값이다.
        /// <br/> 사용자 폭은 고정 열만 받는다.
        /// </summary>
        // --------------------------------------------------------------------------------
        [Test]
        public void ColumnSet_사용자_최소_폭과_고정_열_전용_폭()
        {
            Assert.AreEqual(32f, TableGridColumnSet.GetMinUserWidth(TableGridColumnLayout.Fixed(100f)));
            Assert.AreEqual(50f, TableGridColumnSet.GetMinUserWidth(TableGridColumnLayout.Flexible(200f, 50f, 0f, 1f)));
            Assert.AreEqual(32f, TableGridColumnSet.GetMinUserWidth(TableGridColumnLayout.Flexible(200f, 10f, 0f, 1f)));

            var set = new TableGridColumnSet();
            var columns = new TableGridColumn[]
            {
                new TableGridColumn<SampleRow, string>("fixed", "F", row => row.ID, TableGridFormats.Text, TableGridColumnLayout.Fixed(80f)),
                new TableGridColumn<SampleRow, string>("flex", "X", row => row.ID, TableGridFormats.Text, TableGridColumnLayout.Flexible(200f, 50f, 0f, 1f)),
            };
            set.SetColumns(columns);

            set.SetUserWidth("fixed", 10f);
            Assert.IsTrue(set.TryGetUserWidth("fixed", out var width));
            Assert.AreEqual(32f, width);
            Assert.Throws<ArgumentException>(() => set.SetUserWidth("flex", 120f));
        }

        // --------------------------------------------------------------------------------
        /// <summary>
        /// <br/> 고정 열이 없으면 고정 영역(손잡이·번호 칸)이 있어도 고정 영역 열을 찾지 않고,
        /// <br/> 모두 고정 열이면 스크롤 영역에서는 마지막 고정 열이 가장 가깝다.
        /// </summary>
        // --------------------------------------------------------------------------------
        [Test]
        public void Geometry_고정_열_유무로_열_찾기_영역을_정한다()
        {
            var layouts = new[]
            {
                TableGridColumnLayout.Fixed(100f), TableGridColumnLayout.Fixed(100f),
            };
            var geometry = new TableGridGeometry();
            geometry.LayoutColumns(layouts, 0, 20f, 30f, 500f);

            Assert.IsFalse(geometry.HasFrozenColumns);
            Assert.Greater(geometry.FrozenWidth, 0f);
            Assert.AreEqual(-1, geometry.FindColumnAt(10f, true));
            Assert.AreEqual(0, geometry.FindColumnAt(-5f, false));

            geometry.LayoutColumns(layouts, 2, 20f, 30f, 500f);
            Assert.IsTrue(geometry.HasFrozenColumns);
            Assert.AreEqual(1, geometry.FindColumnAt(10f, false));
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 붙여넣기 결과 요약은 건너뛴 셀 수만 사유별로 담는다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void ClipPlan_결과_요약은_건너뛴_셀_수만_담는다()
        {
            var plan = new TableGridEditPlan
            {
                ReadOnly = 1,
                Invalid = 2,
                Outside = 3,
            };
            var feedback = plan.ToFeedback();

            Assert.AreEqual(1, feedback.ReadOnly);
            Assert.AreEqual(2, feedback.Invalid);
            Assert.AreEqual(3, feedback.Outside);
            Assert.AreEqual(6, feedback.Skipped);
            Assert.AreEqual("요청 · 맞지 않는 값 2셀 · 범위 밖 3셀", plan.Describe("요청"));
        }

    #endregion

    #region 계약 검증

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> 데이터 행 번호·개수는 그룹 머리 행을 세지 않고,
        /// <br/> 번호로 찾으면 머리 행을 건너뛴 위치를 준다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void RowSet_데이터_행_번호는_그룹_머리_행을_세지_않는다()
        {
            var rows = new List<TableGridRow>
            {
                new("group-1", "그룹 1", TableGridRowKind.GroupHeader),
                new("row-a", new SampleRow("row-a", "A")),
                new("row-b", new SampleRow("row-b", "B")),
                new("group-2", "그룹 2", TableGridRowKind.GroupHeader),
                new("row-c", new SampleRow("row-c", "C")),
            };
            var rowSet = new TableGridRowSet();
            rowSet.SetSource(new TableGridRowListSource(rows));

            Assert.AreEqual(5, rowSet.RowCount);
            Assert.AreEqual(3, rowSet.DataRowCount);
            Assert.AreEqual(0, rowSet.GetDataRowNumber(0, rowSet.GetRow(0)));
            Assert.AreEqual(2, rowSet.GetDataRowNumber(2, rowSet.GetRow(2)));
            Assert.AreEqual(3, rowSet.GetDataRowNumber(4, rowSet.GetRow(4)));
            Assert.AreEqual(2, rowSet.CountDataRowsBefore(3));
            Assert.AreEqual(1, rowSet.FindDataRowByNumber(1));
            Assert.AreEqual(4, rowSet.FindDataRowByNumber(3));
            Assert.AreEqual(-1, rowSet.FindDataRowByNumber(4));

            // 소스를 다시 지정하면 머리 행 위치를 다시 센다.
            var dataOnly = new List<TableGridRow>
            {
                rows[1], rows[2],
            };
            rowSet.SetSource(new TableGridRowListSource(dataOnly));
            Assert.AreEqual(2, rowSet.DataRowCount);
            Assert.AreEqual(1, rowSet.GetDataRowNumber(0, rowSet.GetRow(0)));
            Assert.AreEqual(1, rowSet.FindDataRowByNumber(2));
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 행은 항목 참조를 그대로 보관하고 글자 항목을 제목으로 낸다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TableGridRow_항목을_그대로_보관한다()
        {
            var item = new SampleRow("stable-id", "Original");
            var row = new TableGridRow("stable-id", item);
            var header = new TableGridRow("header", "제목", TableGridRowKind.GroupHeader);

            Assert.AreSame(item, row.Item);
            Assert.AreEqual(TableGridRowKind.Data, row.Kind);
            Assert.AreEqual(string.Empty, row.Title);
            Assert.AreEqual("제목", header.Title);
            Assert.Throws<ArgumentException>(() => new TableGridRow(" ", item));
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 잘못된 가상화 설정은 즉시 거부한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TableGrid_잘못된_옵션을_거부한다()
        {
            var options = new TableGridOptions
            {
                MaxRenderedRows = 0
            };
            Assert.Throws<ArgumentOutOfRangeException>(() => options.Validate());
        }

    #endregion

    #region Paged 페이지 넘기기

        private static TableGrid CreatePagedGrid(TableGridOptions options)
        {
            options.Navigation = TableGridNavigation.Paged;
            options.PageSize = 2;

            var grid = new TableGrid(options);
            var columns = new List<TableGridColumn>
            {
                new TableGridColumn<string, string>
                (
                    "name", "이름", item => item, TableGridFormats.Text, TableGridColumnLayout.Fixed(80f)
                ),
            };
            grid.SetColumns(columns);

            var items = new[]
            {
                "A", "B", "C",
            };
            grid.SetSource(new TableGridListSource<string>(items, item => item));
            return grid;
        }

        // --------------------------------------------------------------------------------
        /// <summary>
        /// <br/> Paged 행 선택 표에서 →는 다음 페이지 처음 행을, ←는 이전 페이지 마지막 행을 고른다.
        /// <br/> 셀 선택 표에서는 좌/우가 페이지를 넘기지 않는다.
        /// </summary>
        // --------------------------------------------------------------------------------
        [Test]
        public void TableGrid_Paged_행선택표는_좌우키로_페이지를_넘기고_행을_고른다()
        {
            var grid = CreatePagedGrid(TableGridOptions.Picker());

            Assert.IsTrue(grid.HandleForwardedKey(KeyCode.RightArrow, false));
            Assert.AreEqual(1, grid.PageIndex);
            Assert.AreEqual("C", grid.Selection.RowIDs[0]);

            Assert.IsTrue(grid.HandleForwardedKey(KeyCode.LeftArrow, false));
            Assert.AreEqual(0, grid.PageIndex);
            Assert.AreEqual("B", grid.Selection.RowIDs[0]);

            var cellGrid = CreatePagedGrid(TableGridOptions.Workspace(TableGridAccess.ReadOnly));
            cellGrid.HandleForwardedKey(KeyCode.RightArrow, false);
            Assert.AreEqual(0, cellGrid.PageIndex);
        }

    #endregion

    }
}
