/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TEST_PickerSession.cs
수정일 : 2026-06-07

# 설명
Picker session의 검색, 필터, 정렬, 페이징, 현재 선택, 확정 선택 상태 전이 테스트.

# 테스트 구성
 S: Session
 C: Column Sorting
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

using NUnit;
using NUnit.Framework;

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
   public class TEST_PickerSession
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

   #region S-1: Session

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
            .Column("이름", entry => entry.Name)
            .Column("점수", entry => entry.Score)
            .Filter("active", "활성", false, entry => entry.Desc == "활성")
            .Build();

         var entries = new List<Entry>
         {
            new Entry { ID = "1", Name = "BETA",  Score = 20, Status = "활성" },
            new Entry { ID = "2", Name = "ALPHA", Score = 18, Status = "잠김" },
            new Entry { ID = "3", Name = "GAMMA", Score = 22, Status = "활성" },
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
            new Entry { ID = "1", Name = "ALPHA" },
            new Entry { ID = "2", Name = "BETA" },
            new Entry { ID = "3", Name = "GAMMA" },
         };

         var session = new PickerSession<Entry, string>(spec, entries, "3", _ => { }, 2);

         Assert.AreEqual(1, session.PageIndex);
         Assert.AreEqual("GAMMA", session.CurrentEntry.Label);
         Assert.AreEqual("GAMMA", session.PageEntries[0].Label);
      }

      // ------------------------------------------------------------
      /// <summary>
      /// 다음 페이지 이동은 이동한 페이지의 첫 entry를 현재 entry로 설정한다.
      /// </summary>
      // ------------------------------------------------------------
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
            new Entry { ID = "1", Name = "ALPHA" },
            new Entry { ID = "2", Name = "BETA" },
            new Entry { ID = "3", Name = "GAMMA" },
            new Entry { ID = "4", Name = "DELTA" },
         };

         var session = new PickerSession<Entry, string>(spec, entries, "1", _ => { }, 2);

         session.MoveNext();

         Assert.AreEqual(1, session.PageIndex);
         Assert.AreEqual("GAMMA", session.CurrentEntry.Label);
      }

      // ------------------------------------------------------------
      /// <summary>
      /// 이전 페이지 이동은 이동한 페이지의 마지막 entry를 현재 entry로 설정한다.
      /// </summary>
      // ------------------------------------------------------------
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
            new Entry { ID = "1", Name = "ALPHA" },
            new Entry { ID = "2", Name = "BETA" },
            new Entry { ID = "3", Name = "GAMMA" },
            new Entry { ID = "4", Name = "DELTA" },
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
            new Entry { ID = "1", Name = "ALPHA" },
            new Entry { ID = "2", Name = "BETA" },
            new Entry { ID = "3", Name = "GAMMA" },
            new Entry { ID = "4", Name = "DELTA" },
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
            new Entry { ID = "1", Name = "ALPHA" },
            new Entry { ID = "2", Name = "BETA" },
            new Entry { ID = "3", Name = "GAMMA" },
            new Entry { ID = "4", Name = "DELTA" },
         };

         var session = new PickerSession<Entry, string>(spec, entries, "3", _ => { }, 2);

         session.MoveSelectionPrev();

         Assert.AreEqual(0, session.PageIndex);
         Assert.AreEqual("BETA", session.CurrentEntry.Label);
      }

      // ------------------------------------------------------------
      /// <summary>
      /// 현재 entry 확정은 callback과 Confirmed 이벤트를 한 번씩 발생시킨다.
      /// </summary>
      // ------------------------------------------------------------
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
            new Entry { ID = "1", Name = "ALPHA" },
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

   #region C-1: Column Sorting

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
            .Column("점수", entry => entry.Score)
            .Build();

         var entries = new[]
         {
            new Entry { Name = "A", Score = 10 },
            new Entry { Name = "B", Score = 2 },
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
            .Column("가중치", entry => entry.Weight)
            .Build();

         var entries = new[]
         {
            new Entry { Name = "A", Weight = 10.5f },
            new Entry { Name = "B", Weight = 2.25f },
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
            .Column("코드", entry => entry.Code)
            .Build();

         var entries = new[]
         {
            new Entry { Name = "B", Code = "B-001" },
            new Entry { Name = "A", Code = "A-001" },
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
            .Column("썸네일", entry => entry.Thumbnail, 48f, sortable: false)
            .Build();

         var entries = new[]
         {
            new Entry { Name = "B", Thumbnail = new object() },
            new Entry { Name = "A", Thumbnail = new object() },
         };

         var session = new PickerSession<Entry, string>(spec, entries, null, _ => { }, 10);
         session.SetSort("썸네일", true);

         Assert.AreEqual("B", session.PageEntries[0].Label);
         Assert.AreEqual("A", session.PageEntries[1].Label);
      }

   #endregion

   }
}
