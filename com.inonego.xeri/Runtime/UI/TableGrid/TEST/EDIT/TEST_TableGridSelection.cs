/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TEST_TableGridSelection.cs
수정일 : 2026-11-22

# 설명
TableGrid 셀·행·열 선택 스냅샷의 독립성과 안정적인 주소를 검증한다.
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
    /// 셀과 행 선택이 분리되고 입력 식별자가 보존되는지 확인한다.
    /// </summary>
    // ============================================================
    [TestFixture]
    public sealed class TEST_TableGridSelection
    {

    #region 셀 선택

        // ------------------------------------------------------------
        /// <summary>
        /// 셀 범위는 anchor와 focus의 주소를 독립적으로 보존한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void CellRange_시작과_종료_주소를_보존한다()
        {
            var start = new TableGridCellAddress("row-one", "first");
            var end = new TableGridCellAddress("row-three", "second");

            TableGridSelection range = TableGridSelection.CellRange(start, end);

            Assert.AreEqual(TableGridSelectionKind.Cell, range.Kind);
            Assert.AreEqual(start, range.Anchor);
            Assert.AreEqual(end, range.Focus);
            Assert.IsEmpty(range.RowIDs);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 행과 열 ID가 같을 때 셀 주소도 동일한 것으로 비교한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void CellAddress_논리_ID를_기준으로_동일성을_판정한다()
        {
            var a = new TableGridCellAddress("row-10", "value");
            var b = new TableGridCellAddress("row-10", "value");
            var c = new TableGridCellAddress("row-11", "value");

            Assert.IsTrue(a == b);
            Assert.IsTrue(a != c);
            Assert.AreEqual(a.GetHashCode(), b.GetHashCode());
        }

    #endregion

    #region 행 선택

        // ------------------------------------------------------------
        /// <summary>
        /// 원본 행 ID 목록을 바꾸더라도 선택 snapshot을 보존한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void Rows_입력_컬렉션을_복사한다()
        {
            var ids = new List<string>
            {
                "row-a",
                "row-b"
            };
            TableGridSelection selection = TableGridSelection.Rows(ids);

            ids[0] = "changed";

            Assert.AreEqual(TableGridSelectionKind.Row, selection.Kind);
            Assert.AreEqual("row-a", selection.RowIDs[0]);
            Assert.AreEqual(2, selection.RowIDs.Count);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 한 행의 중복 ID는 선택 계약 위반으로 거부한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void Rows_중복_ID를_거부한다()
        {
            var ids = new List<string>
            {
                "same",
                "same"
            };

            Assert.Throws<ArgumentException>(() => TableGridSelection.Rows(ids));
        }

    #endregion

    #region 열 선택

        // ------------------------------------------------------------
        /// <summary>
        /// 원본 열 ID 목록을 바꾸더라도 열 선택 스냅샷을 보존한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void Columns_입력_컬렉션을_복사한다()
        {
            var ids = new List<string>
            {
                "name",
                "price"
            };
            TableGridSelection selection = TableGridSelection.Columns(ids);

            ids[0] = "changed";

            Assert.AreEqual(TableGridSelectionKind.Column, selection.Kind);
            Assert.AreEqual("name", selection.ColumnIDs[0]);
            Assert.AreEqual(2, selection.ColumnIDs.Count);
            Assert.IsEmpty(selection.RowIDs);
            Assert.AreEqual(default(TableGridCellAddress), selection.Anchor);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 중복된 열 ID는 선택 계약 위반으로 거부한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void Columns_중복_ID를_거부한다()
        {
            var ids = new List<string>
            {
                "same",
                "same"
            };

            Assert.Throws<ArgumentException>(() => TableGridSelection.Columns(ids));
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 종류마다 쓰는 값만 담을 수 있다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void Selection_종류에_맞지_않는_값을_거부한다()
        {
            var ids = new[]
            {
                "id",
            };
            var cell = new TableGridCellAddress("row", "id");

            Assert.Throws<ArgumentException>(() => new TableGridSelection(TableGridSelectionKind.Column, default, default, ids, ids));
            Assert.Throws<ArgumentException>(() => new TableGridSelection(TableGridSelectionKind.Column, cell, cell, null, ids));
            Assert.Throws<ArgumentException>(() => new TableGridSelection(TableGridSelectionKind.Row, default, default, ids, ids));
            Assert.Throws<ArgumentException>(() => new TableGridSelection(TableGridSelectionKind.Row, cell, cell, ids));
            Assert.Throws<ArgumentNullException>(() => TableGridSelection.Columns(null));
        }

    #endregion

    }
}

