/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TEST_PickerSession_Sorting.cs
수정일 : 2026-11-22

# 설명
Picker 목록 표의 숫자·문자열 열 정렬(원래 값 비교)과 정렬 불가능한 열의 동작을 검증한다.
헤더 클릭처럼 정렬을 요청하고, 아래 키로 첫 행을 골라 순서를 확인한다.

# 테스트 구성
 C: 열 정렬 계약
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

using NUnit;
using NUnit.Framework;

using inonego;
using inonego.Xeri;
using inonego.Xeri.UI;
using inonego.Xeri.UI.Picker;
using inonego.Xeri.UI.TableGrid;

namespace inonego.Xeri.TEST.UI._Picker
{
    // ============================================================
    /// <summary>
    /// Picker session 테스트 클래스.
    /// </summary>
    // ============================================================
    public partial class TEST_PickerSession
    {

    #region C-1: 열 정렬

        // ------------------------------------------------------------
        /// <summary>
        /// int column은 숫자 값 기준으로 정렬된다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_PickerSession_ColumnSorting_IntColumn_DefaultSorter_숫자순_정렬()
        {
            var spec = PickerSpec<Entry, string>
                .Create("행 선택")
                .Value(entry => entry.Name)
                .Label(entry => entry.Name)
                .Column("점수", entry => entry.Score, PickerColumnOptions.Fixed(70f))
                .Build();

            var entries = new[]
            {
                new Entry
                {
                    Name = "A",
                    Score = 10,
                },
                new Entry
                {
                    Name = "B",
                    Score = 2,
                },
            };

            var session = new PickerSession<Entry, string>(spec, entries, null, _ => { });
            var grid = CreateGrid(session);
            RequestSort(grid, "점수", TableGridSortDirection.Ascending);

            Assert.AreEqual("B", SelectFirstRow(grid, session));
            grid.HandleForwardedKey(UnityEngine.KeyCode.DownArrow, false);
            Assert.AreEqual("A", session.CurrentEntry.Label);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// float column은 숫자 값 기준으로 정렬된다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_PickerSession_ColumnSorting_FloatColumn_DefaultSorter_숫자순_정렬()
        {
            var spec = PickerSpec<Entry, string>
                .Create("행 선택")
                .Value(entry => entry.Name)
                .Label(entry => entry.Name)
                .Column("가중치", entry => entry.Weight, PickerColumnOptions.Fixed(80f))
                .Build();

            var entries = new[]
            {
                new Entry
                {
                    Name = "A",
                    Weight = 10.5f,
                },
                new Entry
                {
                    Name = "B",
                    Weight = 2.25f,
                },
            };

            var session = new PickerSession<Entry, string>(spec, entries, null, _ => { });
            var grid = CreateGrid(session);
            RequestSort(grid, "가중치", TableGridSortDirection.Ascending);

            Assert.AreEqual("B", SelectFirstRow(grid, session));
            grid.HandleForwardedKey(UnityEngine.KeyCode.DownArrow, false);
            Assert.AreEqual("A", session.CurrentEntry.Label);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// string column은 문자열 값 기준으로 정렬된다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_PickerSession_ColumnSorting_StringColumn_DefaultSorter_문자열순_정렬()
        {
            var spec = PickerSpec<Entry, string>
                .Create("행 선택")
                .Value(entry => entry.Name)
                .Label(entry => entry.Name)
                .Column("코드", entry => entry.Code, PickerColumnOptions.Flexible())
                .Build();

            var entries = new[]
            {
                new Entry
                {
                    Name = "B",
                    Code = "B-001",
                },
                new Entry
                {
                    Name = "A",
                    Code = "A-001",
                },
            };

            var session = new PickerSession<Entry, string>(spec, entries, null, _ => { });
            var grid = CreateGrid(session);
            RequestSort(grid, "코드", TableGridSortDirection.Ascending);

            Assert.AreEqual("A", SelectFirstRow(grid, session));
            grid.HandleForwardedKey(UnityEngine.KeyCode.DownArrow, false);
            Assert.AreEqual("B", session.CurrentEntry.Label);
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// 정렬 불가 column의 정렬 요청은 표시 순서와 헤더 정렬 표시를 바꾸지 않는다.
        /// </summary>
        // ----------------------------------------------------------------------
        [Test]
        public void TEST_PickerSession_ColumnSorting_NonSortableColumn_정렬요청_무시()
        {
            var spec = PickerSpec<Entry, string>
                .Create("행 선택")
                .Value(entry => entry.Name)
                .Label(entry => entry.Name)
                .Column("썸네일", entry => entry.Thumbnail, PickerColumnOptions.Fixed(48f, sortable: false, searchable: false))
                .Build();

            var entries = new[]
            {
                new Entry
                {
                    Name = "B",
                    Thumbnail = new object(),
                },
                new Entry
                {
                    Name = "A",
                    Thumbnail = new object(),
                },
            };

            var session = new PickerSession<Entry, string>(spec, entries, null, _ => { });
            var grid = CreateGrid(session);
            RequestSort(grid, "썸네일", TableGridSortDirection.Ascending);
            Assert.IsNull(grid.QueryState.SortColumnID);

            Assert.AreEqual("B", SelectFirstRow(grid, session));
            grid.HandleForwardedKey(UnityEngine.KeyCode.DownArrow, false);
            Assert.AreEqual("A", session.CurrentEntry.Label);
        }

    #endregion

    }
}
