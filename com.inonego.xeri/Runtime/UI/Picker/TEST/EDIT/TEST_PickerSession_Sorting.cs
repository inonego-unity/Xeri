/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TEST_PickerSession_Sorting.cs
수정일 : 2026-10-24

# 설명
PickerSession의 숫자·문자열 열 정렬과 정렬 불가능한 열의 동작을 검증한다.

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

            var session = new PickerSession<Entry, string>(spec, entries, null, _ => { }, 10);
            session.SetSort("점수", true);

            Assert.AreEqual("B", session.PageEntries[0].Label);
            Assert.AreEqual("A", session.PageEntries[1].Label);
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

            var session = new PickerSession<Entry, string>(spec, entries, null, _ => { }, 10);
            session.SetSort("가중치", true);

            Assert.AreEqual("B", session.PageEntries[0].Label);
            Assert.AreEqual("A", session.PageEntries[1].Label);
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

            var session = new PickerSession<Entry, string>(spec, entries, null, _ => { }, 10);
            session.SetSort("코드", true);

            Assert.AreEqual("A", session.PageEntries[0].Label);
            Assert.AreEqual("B", session.PageEntries[1].Label);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 정렬 불가 column의 정렬 요청은 표시 순서를 바꾸지 않는다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_PickerSession_ColumnSorting_NonSortableColumn_SetSort_무시()
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

            var session = new PickerSession<Entry, string>(spec, entries, null, _ => { }, 10);
            session.SetSort("썸네일", true);

            Assert.AreEqual("B", session.PageEntries[0].Label);
            Assert.AreEqual("A", session.PageEntries[1].Label);
        }

    #endregion

    }
}
