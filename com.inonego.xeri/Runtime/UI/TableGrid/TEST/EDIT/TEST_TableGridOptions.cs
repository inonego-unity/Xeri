/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TEST_TableGridOptions.cs
수정일 : 2026-11-21

# 설명
TableGrid 옵션 프리셋과 두 축(Access·Tools)·선택 방식에서 파생하는 값을 검증한다.
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
    /// 프리셋이 사용처별 구성을 만들고 파생 값이 축에서 정해지는지 확인한다.
    /// </summary>
    // ============================================================
    [TestFixture]
    public sealed class TEST_TableGridOptions
    {

    #region 프리셋

        // ----------------------------------------------------------------------
        /// <summary>
        /// Picker 프리셋은 읽기 전용 목록 수준이며 작업공간 도구와 행 선택 칸이 없다.
        /// </summary>
        // ----------------------------------------------------------------------
        [Test]
        public void Picker_읽기_전용_목록_수준이다()
        {
            var options = TableGridOptions.Picker();

            Assert.AreEqual(TableGridAccess.ReadOnly, options.Access);
            Assert.AreEqual(TableGridTools.List, options.Tools);
            Assert.AreEqual(TableGridSelectionMode.Row, options.Selection);
            Assert.AreEqual(TableGridTheme.Editor, options.Theme);
            Assert.IsFalse(options.IsEditable);
            Assert.IsFalse(options.IsWorkspace);
            Assert.IsFalse(options.AllowsCellRange);
            Assert.IsFalse(options.ShowsRowSelector);
            Assert.IsFalse(options.ShowsStatusBar);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Inspector 프리셋은 편집 가능한 목록 수준이다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void Inspector_편집_가능한_목록_수준이다()
        {
            var options = TableGridOptions.Inspector();

            Assert.IsTrue(options.IsEditable);
            Assert.IsFalse(options.IsWorkspace);
            Assert.AreEqual(TableGridSelectionMode.Row, options.Selection);
            Assert.AreEqual(TableGridTheme.Editor, options.Theme);
        }

        // --------------------------------------------------------------------------------
        /// <summary>
        /// Workspace 프리셋은 셀 범위 선택, 행 선택 칸, 상태 줄을 켜고 권한은 인수를 따른다.
        /// </summary>
        // --------------------------------------------------------------------------------
        [Test]
        public void Workspace_작업공간_도구와_셀_범위를_켠다()
        {
            var readOnly = TableGridOptions.Workspace(TableGridAccess.ReadOnly);
            var editable = TableGridOptions.Workspace(TableGridAccess.Editable);

            Assert.IsTrue(readOnly.IsWorkspace);
            Assert.IsFalse(readOnly.IsEditable);
            Assert.IsTrue(editable.IsEditable);
            Assert.IsTrue(editable.AllowsCellRange);
            Assert.IsTrue(editable.AllowsRowSelection);
            Assert.IsTrue(editable.ShowsRowSelector);
            Assert.IsTrue(editable.ShowsStatusBar);
            Assert.AreEqual(TableGridTheme.Default, editable.Theme);
        }

    #endregion

    #region 파생 값

        // ------------------------------------------------------------
        /// <summary>
        /// 행 선택 칸·상태 줄은 지정하면 파생 값보다 지정 값을 따른다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void ShowValues_지정한_표시_값이_파생_값보다_우선한다()
        {
            var options = TableGridOptions.Workspace(TableGridAccess.Editable);
            options.ShowRowSelector = false;
            options.ShowStatusBar = false;

            Assert.IsFalse(options.ShowsRowSelector);
            Assert.IsFalse(options.ShowsStatusBar);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 정의되지 않은 열거형 값은 검증에서 거부한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void Validate_정의되지_않은_열거형_값을_거부한다()
        {
            var options = new TableGridOptions
            {
                Tools = (TableGridTools)99
            };

            Assert.Throws<ArgumentOutOfRangeException>(() => options.Validate());
        }

    #endregion

    }
}
