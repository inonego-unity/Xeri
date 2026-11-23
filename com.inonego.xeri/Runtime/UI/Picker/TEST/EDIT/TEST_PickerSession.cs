/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TEST_PickerSession.cs
수정일 : 2026-11-22

# 설명
PickerSession의 후보 캐시, filter chip, 현재 값, 확정을 검증하고
Picker 화면(PickerView)의 목록 표가 검색·filter·정렬·선택을 session과 맞추는지 확인한다.
화면 테스트는 패널 없이 표 모델만 쓴다(배치·스크롤은 확인하지 않는다).

# 테스트 구성
 S: 세션 초기화·filter·확정
 V: 목록 표 연결(검색·filter·선택)
 C: 열 정렬(TEST_PickerSession_Sorting.cs)
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UIElements;

using NUnit;
using NUnit.Framework;

using inonego;
using inonego.Xeri;
using inonego.Xeri.UI;
using inonego.Xeri.UI.Picker;
using inonego.Xeri.UI.TableGrid;

using TableGridView = inonego.Xeri.UI.TableGrid.TableGrid;

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
            // ------------------------------------------------------------
            /// <summary>
            /// 항목 식별자.
            /// </summary>
            // ------------------------------------------------------------
            public string ID;

            // ------------------------------------------------------------
            /// <summary>
            /// 항목 이름.
            /// </summary>
            // ------------------------------------------------------------
            public string Name;

            // ------------------------------------------------------------
            /// <summary>
            /// 정렬 확인용 점수.
            /// </summary>
            // ------------------------------------------------------------
            public int Score;

            // ------------------------------------------------------------
            /// <summary>
            /// 상태 글자.
            /// </summary>
            // ------------------------------------------------------------
            public string Status;

            // ------------------------------------------------------------
            /// <summary>
            /// 실수 정렬 확인용 무게.
            /// </summary>
            // ------------------------------------------------------------
            public float Weight;

            // ------------------------------------------------------------
            /// <summary>
            /// 코드 글자.
            /// </summary>
            // ------------------------------------------------------------
            public string Code;

            // ------------------------------------------------------------
            /// <summary>
            /// 미리보기 이미지.
            /// </summary>
            // ------------------------------------------------------------
            public object Thumbnail;
        }

    #endregion

    #region 헬퍼 (화면)

        // ------------------------------------------------------------
        /// <summary>
        /// 패널 없이 Picker 화면을 만들고 목록 표를 꺼낸다.
        /// </summary>
        // ------------------------------------------------------------
        private static TableGridView CreateGrid(PickerSession<Entry, string> session)
        {
            var view = new PickerView<Entry, string>(session);
            return view.Q<TableGridView>("entry-grid");
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 선택을 비우고 아래 키로 첫 행을 골라 그 entry 이름을 돌려준다.
        /// </summary>
        // ------------------------------------------------------------
        private static string SelectFirstRow(TableGridView grid, PickerSession<Entry, string> session)
        {
            grid.Selection = TableGridSelection.None;
            grid.HandleForwardedKey(KeyCode.DownArrow, false);
            return session.CurrentEntry?.Label;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 제목이 header인 열의 정렬을 헤더 클릭처럼 요청한다.
        /// </summary>
        // ------------------------------------------------------------
        private static void RequestSort(TableGridView grid, string header, TableGridSortDirection direction)
        {
            foreach (var column in grid.Columns)
            {
                if (column.Title == header)
                {
                    grid.RequestQuery(grid.QueryState.WithSort(column.ID, direction));
                    return;
                }
            }

            Assert.Fail($"열을 찾을 수 없습니다: {header}");
        }

        private static List<Entry> CreateThree()
        {
            return new List<Entry>
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
        }

        private static PickerSpec<Entry, string> CreateSpec()
        {
            return PickerSpec<Entry, string>
                .Create("항목 선택")
                .Value(entry => entry.ID)
                .Label(entry => entry.Name)
                .Desc(entry => entry.Status)
                .Column("이름", entry => entry.Name, PickerColumnOptions.Flexible())
                .Column("점수", entry => entry.Score, PickerColumnOptions.Fixed(70f))
                .FilterByEntry("active", "활성", false, entry => entry.Status == "활성")
                .Build();
        }

    #endregion

    #region S-1: 후보 캐시·filter·확정

        // ------------------------------------------------------------
        /// <summary>
        /// 불변 후보는 Entry를 공유하고 원본 목록이 바뀌면 다시 만든다.
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

            var first = new PickerSession<Entry, string>(spec, entries, null, _ => { });
            var second = new PickerSession<Entry, string>(spec, entries, "2", _ => { });

            Assert.AreEqual(entries.Length, builds);
            Assert.AreSame(first.Entries[0], second.Entries[0]);
            Assert.IsNull(first.CurrentEntry);
            Assert.AreEqual("BETA", second.CurrentEntry.Label);

            var changedSource = new[]
            {
                new Entry
                {
                    ID = "3",
                    Name = "GAMMA",
                },
            };
            var third = new PickerSession<Entry, string>(spec, changedSource, null, _ => { });
            Assert.AreEqual("GAMMA", third.Entries[0].Label);
            Assert.AreEqual(3, builds);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 일반 Picker는 바뀐 원본 데이터를 매번 평가한다.
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

            var first = new PickerSession<Entry, string>(spec, entries, null, _ => { });
            entries[0].Name = "AFTER";
            var second = new PickerSession<Entry, string>(spec, entries, null, _ => { });

            Assert.AreEqual("BEFORE", first.Entries[0].Label);
            Assert.AreEqual("AFTER", second.Entries[0].Label);
            Assert.AreNotSame(first.Entries[0], second.Entries[0]);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// filter chip을 켜면 켜진 filter만 통과하고 변경을 알린다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_PickerSession_Filter_켜진것만_통과하고_알린다()
        {
            var session = new PickerSession<Entry, string>(CreateSpec(), CreateThree(), null, _ => { });
            var changed = 0;
            session.Changed += (_, _) => changed++;

            Assert.IsFalse(session.HasActiveFilter);
            Assert.IsTrue(session.MatchesFilters(session.Entries[1]));

            session.SetFilterEnabled("active", true);

            Assert.AreEqual(1, changed);
            Assert.IsTrue(session.HasActiveFilter);
            Assert.IsTrue(session.GetFilterEnabled("active"));
            Assert.IsTrue(session.MatchesFilters(session.Entries[0]));
            Assert.IsFalse(session.MatchesFilters(session.Entries[1]));
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
            var session = new PickerSession<Entry, string>(CreateSpec(), CreateThree(), "1", value => selectedValue = value);
            session.Confirmed += (_, e) =>
            {
                eventCount++;
                Assert.AreEqual("1", e.Value);
            };

            session.ConfirmCurrent();

            Assert.AreEqual("1", selectedValue);
            Assert.AreEqual(1, eventCount);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 선택할 수 없는 entry는 확정하지 않고 사유를 보관한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_PickerSession_비활성_항목은_확정하지_않는다()
        {
            var selected = 0;
            var spec = PickerSpec<Entry, string>
                .Create("항목 선택")
                .Value(entry => entry.ID)
                .Label(entry => entry.Name)
                .DisabledWhen(entry => entry.Status == "잠김", entry => "잠긴 항목")
                .Build();
            var session = new PickerSession<Entry, string>(spec, CreateThree(), "2", _ => selected++);

            session.ConfirmCurrent();

            Assert.AreEqual(0, selected);
            Assert.IsFalse(session.CurrentEntry.IsEnabled);
            Assert.AreEqual("잠긴 항목", session.CurrentEntry.DisabledReason);
        }

    #endregion

    #region V-1: 목록 표 연결

        // ------------------------------------------------------------
        /// <summary>
        /// 표는 전체 entry를 받고 검색은 entry 검색 글자(설명 포함)로 거른다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_PickerView_검색은_표가_entry_검색글자로_거른다()
        {
            var session = new PickerSession<Entry, string>(CreateSpec(), CreateThree(), null, _ => { });
            var grid = CreateGrid(session);

            Assert.AreEqual(3, grid.DataRowCount);

            grid.SearchText = "잠김";

            Assert.AreEqual(1, grid.DataRowCount);
            Assert.AreEqual("ALPHA", SelectFirstRow(grid, session));
        }

        // ------------------------------------------------------------
        /// <summary>
        /// filter chip을 켜면 표가 다시 거르고 빠진 현재 entry를 비운다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_PickerView_Filter_표를_거르고_빠진_현재항목을_비운다()
        {
            var session = new PickerSession<Entry, string>(CreateSpec(), CreateThree(), "2", _ => { });
            var grid = CreateGrid(session);

            session.SetFilterEnabled("active", true);

            Assert.AreEqual(2, grid.DataRowCount);
            Assert.IsNull(session.CurrentEntry);
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// 처음 열면 현재 값의 행이 골라져 있고, 표 선택은 session 현재 entry가 된다.
        /// </summary>
        // ----------------------------------------------------------------------
        [Test]
        public void TEST_PickerView_현재값_행선택과_session이_같다()
        {
            var session = new PickerSession<Entry, string>(CreateSpec(), CreateThree(), "3", _ => { });
            var grid = CreateGrid(session);

            Assert.AreEqual(TableGridSelectionKind.Row, grid.Selection.Kind);
            Assert.AreEqual("entry-2", grid.Selection.RowIDs[0]);

            var rowIDs = new[]
            {
                "entry-1",
            };
            grid.Selection = TableGridSelection.Rows(rowIDs);

            Assert.AreEqual("ALPHA", session.CurrentEntry.Label);
        }

    #endregion

    }
}
