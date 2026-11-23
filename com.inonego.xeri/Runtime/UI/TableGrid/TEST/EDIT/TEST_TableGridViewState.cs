/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TEST_TableGridViewState.cs
수정일 : 2026-11-21

# 설명
TableGrid 보기 상태의 JSON 왕복, 읽기 실패, 열 ID 맞춤(없어진 열·새 열), 고정·가변 열 사용자 폭, 버전 차이를 검증한다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

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
    /// 보기 상태를 꺼내고 적용하는 규칙을 확인한다.
    /// </summary>
    // ============================================================
    [TestFixture]
    public sealed class TEST_TableGridViewState
    {

    #region 준비

        private static TableGrid CreateGrid(params string[] extraColumnIDs)
        {
            var columns = new List<TableGridColumn>
            {
                CreateColumn("id", "ID", TableGridColumnLayout.Fixed(80f)),
                CreateColumn("name", "이름", TableGridColumnLayout.Flexible(200f, 80f, 0f, 2f)),
                CreateColumn("note", "메모", TableGridColumnLayout.Flexible(160f, 80f, 0f, 1f)),
            };

            foreach (var id in extraColumnIDs)
            {
                columns.Add(CreateColumn(id, id, TableGridColumnLayout.Fixed(60f)));
            }

            var grid = new TableGrid(TableGridOptions.Workspace(TableGridAccess.ReadOnly));
            grid.SetColumns(columns);
            return grid;
        }

        private static TableGridColumn CreateColumn(string id, string title, TableGridColumnLayout layout)
        {
            return new TableGridColumn<string, string>(id, title, item => item, TableGridFormats.Text, layout);
        }

        private static List<string> VisibleIDs(TableGrid grid)
        {
            var ids = new List<string>();
            foreach (var column in grid.VisibleColumns)
            {
                ids.Add(column.ID);
            }

            return ids;
        }

    #endregion

    #region 직렬화

        // ------------------------------------------------------------
        /// <summary>
        /// JSON으로 바꿨다가 읽으면 열 상태와 쿼리(필터 원래 값 포함)가 같다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void Json_왕복하면_열과_쿼리가_같다()
        {
            var choices = new[]
            {
                "Weapon",
                "Armor",
            };
            var filters = new[]
            {
                TableGridFilterRule.ChoiceSet("f1", "category", choices),
            };
            var query = new TableGridQueryState
            (
                filters,
                "name", TableGridSortDirection.Descending
            );

            var state = new TableGridViewState
            {
                FrozenColumnCount = 1,
                HasQuery = true,
                Query = TableGridQueryViewState.From(query),
            };
            var idColumn = new TableGridColumnViewState
            {
                ID = "id",
                Visible = true,
                Order = 0,
                HasWidth = true,
                Width = 120f,
            };
            state.Columns.Add(idColumn);

            Assert.IsTrue(TableGridViewState.TryFromJson(state.ToJson(), out var loaded));
            Assert.AreEqual(1, loaded.FrozenColumnCount);
            Assert.AreEqual("id", loaded.Columns[0].ID);
            Assert.AreEqual(120f, loaded.Columns[0].Width);

            var restored = loaded.Query.ToQuery();
            Assert.AreEqual("name", restored.SortColumnID);
            Assert.AreEqual(TableGridSortDirection.Descending, restored.SortDirection);
            Assert.AreEqual(1, restored.Filters.Count);
            CollectionAssert.AreEqual(choices, restored.Filters[0].Choices);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 비었거나 읽을 수 없는 글자는 false다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void Json_읽기_실패는_false다()
        {
            Assert.IsFalse(TableGridViewState.TryFromJson(null, out _));
            Assert.IsFalse(TableGridViewState.TryFromJson("   ", out _));
            Assert.IsFalse(TableGridViewState.TryFromJson("{ 깨진", out _));
        }

    #endregion

    #region 적용

        // ----------------------------------------------------------------------
        /// <summary>
        /// 열은 ID로 맞춘다. 없어진 열은 무시하고 상태에 없는 새 열은 뒤에 보인다.
        /// </summary>
        // ----------------------------------------------------------------------
        [Test]
        public void Apply_열을_ID로_맞추고_새_열은_뒤에_보인다()
        {
            var grid = CreateGrid("extra");
            var state = new TableGridViewState();
            var noteColumn = new TableGridColumnViewState
            {
                ID = "note",
                Visible = true,
                Order = 0,
            };
            state.Columns.Add(noteColumn);
            var idColumn = new TableGridColumnViewState
            {
                ID = "id",
                Visible = true,
                Order = 1,
            };
            state.Columns.Add(idColumn);
            var nameColumn = new TableGridColumnViewState
            {
                ID = "name",
                Visible = false,
            };
            state.Columns.Add(nameColumn);
            var removedColumn = new TableGridColumnViewState
            {
                ID = "removed",
                Visible = true,
                Order = 2,
            };
            state.Columns.Add(removedColumn);

            grid.ApplyViewState(state);

            var expectedIDs = new[]
            {
                "note",
                "id",
                "extra",
            };

            CollectionAssert.AreEqual(expectedIDs, VisibleIDs(grid));
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 고정 열은 Width, 가변 열은 Weight만 받고 꺼내면 같은 값이 나온다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void Apply_고정_열은_폭_가변_열은_비율을_받는다()
        {
            var grid = CreateGrid();
            var state = new TableGridViewState();
            var idColumn = new TableGridColumnViewState
            {
                ID = "id",
                Visible = true,
                Order = 0,
                HasWidth = true,
                Width = 140f,
            };
            state.Columns.Add(idColumn);
            var nameColumn = new TableGridColumnViewState
            {
                ID = "name",
                Visible = true,
                Order = 1,
                HasWeight = true,
                Weight = 3f,
            };
            state.Columns.Add(nameColumn);
            var noteColumn = new TableGridColumnViewState
            {
                ID = "note",
                Visible = true,
                Order = 2,
                HasWidth = true,
                Width = 300f,
            };
            state.Columns.Add(noteColumn);

            grid.ApplyViewState(state);
            var captured = grid.CaptureViewState(includeQuery: false);

            var byID = new Dictionary<string, TableGridColumnViewState>();
            foreach (var column in captured.Columns)
            {
                byID[column.ID] = column;
            }

            Assert.IsTrue(byID["id"].HasWidth);
            Assert.AreEqual(140f, byID["id"].Width);
            Assert.IsTrue(byID["name"].HasWeight);
            Assert.AreEqual(3f, byID["name"].Weight);

            // 가변 열에 준 px 폭은 받지 않는다.
            Assert.IsFalse(byID["note"].HasWidth);
            Assert.IsFalse(byID["note"].HasWeight);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 버전이 다르면 열 부분만 적용하고 쿼리는 요청하지 않는다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void Apply_버전이_다르면_열만_적용한다()
        {
            var grid = CreateGrid();
            var requested = 0;
            grid.QueryRequested += (_, _) => requested++;

            var state = new TableGridViewState
            {
                Version = TableGridViewState.CURRENT_VERSION + 1,
                HasQuery = true,
            };
            var nameColumn = new TableGridColumnViewState
            {
                ID = "name",
                Visible = true,
                Order = 0,
            };
            state.Columns.Add(nameColumn);

            grid.ApplyViewState(state);

            Assert.AreEqual(0, requested);
            Assert.AreEqual("name", VisibleIDs(grid)[0]);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 적용하는 동안에는 ViewStateChanged를 알리지 않는다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void Apply_중에는_변경을_알리지_않는다()
        {
            var grid = CreateGrid();
            var changed = 0;
            grid.ViewStateChanged += (_, _) => changed++;

            var state = new TableGridViewState
            {
                FrozenColumnCount = 1,
            };
            var idColumn = new TableGridColumnViewState
            {
                ID = "id",
                Visible = true,
                Order = 0,
            };
            state.Columns.Add(idColumn);
            grid.ApplyViewState(state);
            Assert.AreEqual(0, changed);

            grid.FrozenColumnCount = 0;
            Assert.AreEqual(1, changed);
        }

    #endregion

    }
}
