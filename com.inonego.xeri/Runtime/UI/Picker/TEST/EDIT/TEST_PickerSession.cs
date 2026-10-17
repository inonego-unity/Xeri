/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TEST_PickerSession.cs
수정일 : 2026-10-24

# 설명
PickerSession의 후보 캐시, 검색, 페이지 이동과 현재 선택 확정을 검증한다.

# 테스트 구성
 S: 세션 초기화와 선택·페이지 이동
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

    #region 헬퍼

        // ============================================================
        /// <summary>
        /// 테스트용 원본 entry.
        /// </summary>
        // ============================================================
        private sealed class Entry
        {
            public string ID;
            public string Name;
            public int Score;
            public string Status;
            public float Weight;
            public string Code;
            public object Thumbnail;
        }

    #endregion

    #region S-1: 후보 캐시와 초기 페이지

        // ------------------------------------------------------------
        /// <summary>
        /// 불변 후보는 Entry를 공유하고 선택 상태는 분리한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_PickerSession_불변후보표시캐시_재사용()
        {
            var builds = 0;
            Func<Entry, string> getName = entry =>
            {
                builds++;
                return entry.Name;
            };
            var entries = new[]
            {
                new Entry
                {
                    ID = "1",
                    Name = "ALPHA",
                },
                new Entry
                {
                    ID = "2",
                    Name = "BETA",
                },
            };
            var spec = PickerSpec<Entry, string>
                .Create("타입 선택")
                .Value(entry => entry.ID)
                .Label(getName)
                .CacheImmutableEntries()
                .Build();

            var first = new PickerSession<Entry, string>(
                spec, entries, null, _ => { }, 5
            );
            var second = new PickerSession<Entry, string>(
                spec, entries, null, _ => { }, 5
            );

            Assert.AreEqual(entries.Length, builds);
            Assert.AreSame(first.PageEntries[0], second.PageEntries[0]);

            first.SetSearchText("BETA");
            Assert.AreEqual(1, first.FilteredCount);
            Assert.AreEqual(2, second.FilteredCount);

            var changedSource = new[]
            {
                new Entry
                {
                    ID = "3",
                    Name = "GAMMA",
                },
            };
            var third = new PickerSession<Entry, string>(
                spec, changedSource, null, _ => { }, 5
            );
            Assert.AreEqual("GAMMA", third.PageEntries[0].Label);
            Assert.AreEqual(3, builds);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 일반 Picker는 기존처럼 바뀐 원본 데이터를 매번 평가한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_PickerSession_기본모드_가변데이터재평가()
        {
            var entries = new[]
            {
                new Entry
                {
                    ID = "1",
                    Name = "BEFORE",
                },
            };
            var spec = PickerSpec<Entry, string>
                .Create("가변 데이터")
                .Value(entry => entry.ID)
                .Label(entry => entry.Name)
                .Build();

            var first = new PickerSession<Entry, string>(
                spec, entries, null, _ => { }, 5
            );
            entries[0].Name = "AFTER";
            var second = new PickerSession<Entry, string>(
                spec, entries, null, _ => { }, 5
            );

            Assert.AreEqual("BEFORE", first.PageEntries[0].Label);
            Assert.AreEqual("AFTER", second.PageEntries[0].Label);
            Assert.AreNotSame(first.PageEntries[0], second.PageEntries[0]);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 검색, 필터, 정렬, 페이징이 session 표시 결과에 적용된다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_PickerSession_Search_Filter_Sort_Page_적용()
        {
            var spec = PickerSpec<Entry, string>
                .Create("항목 선택")
                .Value(entry => entry.ID)
                .Label(entry => entry.Name)
                .Desc(entry => entry.Status)
                .Column("이름", entry => entry.Name, PickerColumnOptions.Flexible())
                .Column("점수", entry => entry.Score, PickerColumnOptions.Fixed(70f))
                .Filter("active", "활성", false, entry => entry.Desc == "활성")
                .Build();

            var entries = new List<Entry>
            {
                new Entry
                {
                    ID = "1",
                    Name = "BETA",
                    Score = 20,
                    Status = "활성",
                },
                new Entry
                {
                    ID = "2",
                    Name = "ALPHA",
                    Score = 18,
                    Status = "잠김",
                },
                new Entry
                {
                    ID = "3",
                    Name = "GAMMA",
                    Score = 22,
                    Status = "활성",
                },
            };

            var session = new PickerSession<Entry, string>(spec, entries, null, _ => { }, 2);

            session.SetSearchText(string.Empty);
            session.SetFilterEnabled("active", true);
            session.SetSort("점수", true);

            Assert.AreEqual(2, session.FilteredCount);
            Assert.AreEqual("BETA", session.PageEntries[0].Label);
            Assert.AreEqual("GAMMA", session.PageEntries[1].Label);
            Assert.AreEqual(1, session.PageCount);
            Assert.AreEqual(1, session.Paginator.PageNumber);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 기존 선택값이 있으면 해당 entry가 있는 페이지로 이동한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_PickerSession_CurrentValue_해당_페이지로_이동()
        {
            var spec = PickerSpec<Entry, string>
                .Create("항목 선택")
                .Value(entry => entry.ID)
                .Label(entry => entry.Name)
                .Build();

            var entries = new List<Entry>
            {
                new Entry
                {
                    ID = "1",
                    Name = "ALPHA",
                },
                new Entry
                {
                    ID = "2",
                    Name = "BETA",
                },
                new Entry
                {
                    ID = "3",
                    Name = "GAMMA",
                },
            };

            var session = new PickerSession<Entry, string>(spec, entries, "3", _ => { }, 2);

            Assert.AreEqual(1, session.PageIndex);
            Assert.AreEqual("GAMMA", session.CurrentEntry.Label);
            Assert.AreEqual("GAMMA", session.PageEntries[0].Label);
        }


    #endregion

#region S-2: 페이지 경계와 선택 이동
        [Test]
        public void TEST_PickerSession_MoveNext_다음페이지_첫항목_선택()
        {
            var spec = PickerSpec<Entry, string>
                .Create("항목 선택")
                .Value(entry => entry.ID)
                .Label(entry => entry.Name)
                .Build();

            var entries = new[]
            {
                new Entry
                {
                    ID = "1",
                    Name = "ALPHA",
                },
                new Entry
                {
                    ID = "2",
                    Name = "BETA",
                },
                new Entry
                {
                    ID = "3",
                    Name = "GAMMA",
                },
                new Entry
                {
                    ID = "4",
                    Name = "DELTA",
                },
            };

            var session = new PickerSession<Entry, string>(spec, entries, "1", _ => { }, 2);

            session.MoveNext();

            Assert.AreEqual(1, session.PageIndex);
            Assert.AreEqual("GAMMA", session.CurrentEntry.Label);
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// 이전 페이지 이동은 이동한 페이지의 마지막 entry를 현재 entry로 설정한다.
        /// </summary>
        // ----------------------------------------------------------------------
        [Test]
        public void TEST_PickerSession_MovePrev_이전페이지_마지막항목_선택()
        {
            var spec = PickerSpec<Entry, string>
                .Create("항목 선택")
                .Value(entry => entry.ID)
                .Label(entry => entry.Name)
                .Build();

            var entries = new[]
            {
                new Entry
                {
                    ID = "1",
                    Name = "ALPHA",
                },
                new Entry
                {
                    ID = "2",
                    Name = "BETA",
                },
                new Entry
                {
                    ID = "3",
                    Name = "GAMMA",
                },
                new Entry
                {
                    ID = "4",
                    Name = "DELTA",
                },
            };

            var session = new PickerSession<Entry, string>(spec, entries, "4", _ => { }, 2);

            session.MovePrev();

            Assert.AreEqual(0, session.PageIndex);
            Assert.AreEqual("BETA", session.CurrentEntry.Label);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 마지막 항목에서 다음 선택 이동은 다음 페이지 첫 entry를 선택한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_PickerSession_MoveSelectionNext_페이지경계_다음페이지_첫항목_선택()
        {
            var spec = PickerSpec<Entry, string>
                .Create("항목 선택")
                .Value(entry => entry.ID)
                .Label(entry => entry.Name)
                .Build();

            var entries = new[]
            {
                new Entry
                {
                    ID = "1",
                    Name = "ALPHA",
                },
                new Entry
                {
                    ID = "2",
                    Name = "BETA",
                },
                new Entry
                {
                    ID = "3",
                    Name = "GAMMA",
                },
                new Entry
                {
                    ID = "4",
                    Name = "DELTA",
                },
            };

            var session = new PickerSession<Entry, string>(spec, entries, "2", _ => { }, 2);

            session.MoveSelectionNext();

            Assert.AreEqual(1, session.PageIndex);
            Assert.AreEqual("GAMMA", session.CurrentEntry.Label);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 첫 항목에서 이전 선택 이동은 이전 페이지 마지막 entry를 선택한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_PickerSession_MoveSelectionPrev_페이지경계_이전페이지_마지막항목_선택()
        {
            var spec = PickerSpec<Entry, string>
                .Create("항목 선택")
                .Value(entry => entry.ID)
                .Label(entry => entry.Name)
                .Build();

            var entries = new[]
            {
                new Entry
                {
                    ID = "1",
                    Name = "ALPHA",
                },
                new Entry
                {
                    ID = "2",
                    Name = "BETA",
                },
                new Entry
                {
                    ID = "3",
                    Name = "GAMMA",
                },
                new Entry
                {
                    ID = "4",
                    Name = "DELTA",
                },
            };

            var session = new PickerSession<Entry, string>(spec, entries, "3", _ => { }, 2);

            session.MoveSelectionPrev();

            Assert.AreEqual(0, session.PageIndex);
            Assert.AreEqual("BETA", session.CurrentEntry.Label);
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// 현재 entry 확정은 callback과 Confirmed 이벤트를 한 번씩 발생시킨다.
        /// </summary>
        // ----------------------------------------------------------------------
        [Test]
        public void TEST_PickerSession_ConfirmCurrent_Callback_Event_발생()
        {
            var selectedValue = string.Empty;
            var eventCount = 0;
            var spec = PickerSpec<Entry, string>
                .Create("항목 선택")
                .Value(entry => entry.ID)
                .Label(entry => entry.Name)
                .Build();

            var entries = new[]
            {
                new Entry
                {
                    ID = "1",
                    Name = "ALPHA",
                },
            };

            var session = new PickerSession<Entry, string>(spec, entries, "1", value => selectedValue = value, 8);
            session.Confirmed += (_, e) =>
            {
                eventCount++;
                Assert.AreEqual("1", e.Value);
            };

            session.ConfirmCurrent();

            Assert.AreEqual("1", selectedValue);
            Assert.AreEqual(1, eventCount);
        }

    #endregion

    }
}
