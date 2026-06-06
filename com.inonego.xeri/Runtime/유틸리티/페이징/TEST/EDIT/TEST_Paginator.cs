/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TEST_Paginator.cs
수정일 : 2026-06-06

# 설명
Paginator 핵심 기능 유닛 테스트.
Unity Test Runner (Edit Mode) 에서 실행한다.

# 테스트 구성
 E: 기본 기능
 R: 범위 계산
 M: 페이지 이동
 C: 상태 보정
 V: 이벤트
 X: 예외 처리
========================================================================= BLOCK_HEADER_END */

using System;

using NUnit;
using NUnit.Framework;

namespace inonego.Xeri.TEST.Utility._Paging
{

   using inonego.Xeri.Utility;

   // ============================================================
   /// <summary>
   /// Paginator 핵심 기능 테스트.
   /// </summary>
   // ============================================================
   public class TEST_Paginator
   {

   #region E-1: 기본 생성

      // ------------------------------------------------------------
      /// <summary>
      /// 기본 생성 시 초기 페이징 상태를 검증한다.
      /// </summary>
      // ------------------------------------------------------------
      [Test]
      public void TEST_Paginator_기본_생성_초기값()
      {
         var paginator = new Paginator();

         Assert.AreEqual(0,  paginator.TotalCount);
         Assert.AreEqual(20, paginator.PerPage);
         Assert.AreEqual(0,  paginator.PageIndex);
         Assert.AreEqual(0,  paginator.PageNumber);
         Assert.AreEqual(0,  paginator.PageCount);
         Assert.IsTrue(paginator.IsEmpty);
         Assert.IsFalse(paginator.CanMoveFirst);
         Assert.IsFalse(paginator.CanMovePrev);
         Assert.IsFalse(paginator.CanMoveNext);
         Assert.IsFalse(paginator.CanMoveLast);
      }

   #endregion

   #region R-1: 범위 계산

      // ------------------------------------------------------------
      /// <summary>
      /// 전체 개수에 따른 페이지 개수와 범위 계산을 검증한다.
      /// </summary>
      // ------------------------------------------------------------
      [Test]
      public void TEST_Paginator_TotalCount_범위와_페이지_개수_계산()
      {
         var paginator = new Paginator(8);

         paginator.TotalCount = 24;

         Assert.AreEqual(3, paginator.PageCount);

         var first = paginator.Range;
         Assert.AreEqual(0, first.BeginIndex);
         Assert.AreEqual(8, first.EndIndex);
         Assert.AreEqual(8, first.Count);
         Assert.IsFalse(first.IsEmpty);

         paginator.PageIndex = 2;

         var last = paginator.Range;
         Assert.AreEqual(16, last.BeginIndex);
         Assert.AreEqual(24, last.EndIndex);
         Assert.AreEqual(8,  last.Count);
      }

      // ------------------------------------------------------------
      /// <summary>
      /// 마지막 페이지의 잔여 아이템 범위를 검증한다.
      /// </summary>
      // ------------------------------------------------------------
      [Test]
      public void TEST_Paginator_TotalCount_마지막_페이지_잔여_개수_계산()
      {
         var paginator = new Paginator(8);

         paginator.TotalCount = 22;
         paginator.MoveLast();

         var range = paginator.Range;

         Assert.AreEqual(16, range.BeginIndex);
         Assert.AreEqual(22, range.EndIndex);
         Assert.AreEqual(6,  range.Count);
      }

      // ------------------------------------------------------------
      /// <summary>
      /// 빈 목록의 범위와 표시 정보를 검증한다.
      /// </summary>
      // ------------------------------------------------------------
      [Test]
      public void TEST_Paginator_빈목록_범위와_표시정보()
      {
         var paginator = new Paginator(8);

         paginator.TotalCount = 0;

         var range = paginator.Range;

         Assert.AreEqual(0, range.BeginIndex);
         Assert.AreEqual(0, range.EndIndex);
         Assert.AreEqual(0, range.Count);
         Assert.IsTrue(range.IsEmpty);

         Assert.AreEqual(0, paginator.PageNumber);
         Assert.AreEqual(0, paginator.PageCount);
         Assert.IsTrue(paginator.IsEmpty);
      }

   #endregion

   #region M-1: 페이지 이동

      // ------------------------------------------------------------
      /// <summary>
      /// 직접 이동과 처음, 이전, 다음, 마지막 이동을 검증한다.
      /// </summary>
      // ------------------------------------------------------------
      [Test]
      public void TEST_Paginator_페이지_이동_직접_처음_이전_다음_마지막()
      {
         var paginator = new Paginator(8);
         paginator.TotalCount = 24;

         paginator.MoveTo(1);
         Assert.AreEqual(1, paginator.PageIndex);

         paginator.MoveNext();
         Assert.AreEqual(2, paginator.PageIndex);
         Assert.IsFalse(paginator.CanMoveNext);
         Assert.IsFalse(paginator.CanMoveLast);

         paginator.MovePrev();
         Assert.AreEqual(1, paginator.PageIndex);

         paginator.MoveFirst();
         Assert.AreEqual(0, paginator.PageIndex);
         Assert.IsFalse(paginator.CanMoveFirst);
         Assert.IsFalse(paginator.CanMovePrev);

         paginator.MoveLast();
         Assert.AreEqual(2, paginator.PageIndex);
      }

      // ------------------------------------------------------------
      /// <summary>
      /// 범위를 벗어난 이동 요청이 가까운 유효 페이지로 보정되는지 검증한다.
      /// </summary>
      // ------------------------------------------------------------
      [Test]
      public void TEST_Paginator_페이지_이동_범위초과_가까운_페이지로_보정()
      {
         var paginator = new Paginator(8);
         paginator.TotalCount = 24;

         paginator.MoveTo(-10);
         Assert.AreEqual(0, paginator.PageIndex);

         paginator.MoveTo(10);
         Assert.AreEqual(2, paginator.PageIndex);
      }

   #endregion

   #region C-1: 상태 보정

      // ------------------------------------------------------------
      /// <summary>
      /// 전체 개수 감소 시 현재 페이지가 마지막 유효 페이지로 보정되는지 검증한다.
      /// </summary>
      // ------------------------------------------------------------
      [Test]
      public void TEST_Paginator_TotalCount_감소_PageIndex_마지막_페이지로_보정()
      {
         var paginator = new Paginator(8);
         paginator.TotalCount = 24;
         paginator.MoveLast();

         paginator.TotalCount = 10;

         Assert.AreEqual(1, paginator.PageIndex);
         Assert.AreEqual(2, paginator.PageCount);

         var range = paginator.Range;
         Assert.AreEqual(8,  range.BeginIndex);
         Assert.AreEqual(10, range.EndIndex);
         Assert.AreEqual(2,  range.Count);
      }

      // ------------------------------------------------------------
      /// <summary>
      /// 페이지당 개수 변경 시 기존 첫 표시 아이템을 유지하도록 보정되는지 검증한다.
      /// </summary>
      // ------------------------------------------------------------
      [Test]
      public void TEST_Paginator_PerPage_변경_기존_첫_표시_아이템을_포함하도록_보정()
      {
         var paginator = new Paginator(8);
         paginator.TotalCount = 30;
         paginator.PageIndex = 2;

         paginator.PerPage = 10;

         Assert.AreEqual(1, paginator.PageIndex);

         var range = paginator.Range;
         Assert.AreEqual(10, range.BeginIndex);
         Assert.AreEqual(20, range.EndIndex);
         Assert.AreEqual(10, range.Count);
      }

   #endregion

   #region V-1: 이벤트

      // ------------------------------------------------------------
      /// <summary>
      /// 상태 변경 이벤트가 실제 변경 시에만 발생하는지 검증한다.
      /// </summary>
      // ------------------------------------------------------------
      [Test]
      public void TEST_Paginator_상태_변경_이벤트_실제_변경시에만_발생()
      {
         var paginator = new Paginator(8);
         var changeCount = 0;
         var totalCountChangeCount = 0;
         var perPageChangeCount = 0;
         var pageIndexChangeCount = 0;

         paginator.OnChange += (_, _) => changeCount++;
         paginator.OnTotalCountChange += (_, e) =>
         {
            totalCountChangeCount++;
            Assert.AreEqual(0, e.Previous);
            Assert.AreEqual(24, e.Current);
         };
         paginator.OnPerPageChange += (_, e) =>
         {
            perPageChangeCount++;
            Assert.AreEqual(8,  e.Previous);
            Assert.AreEqual(12, e.Current);
         };
         paginator.OnPageIndexChange += (_, e) =>
         {
            pageIndexChangeCount++;
            Assert.AreEqual(0, e.Previous);
            Assert.AreEqual(1, e.Current);
         };

         paginator.TotalCount = 24;
         paginator.TotalCount = 24;
         paginator.PerPage = 12;
         paginator.PerPage = 12;
         paginator.MoveTo(1);
         paginator.MoveTo(1);

         Assert.AreEqual(3, changeCount);
         Assert.AreEqual(1, totalCountChangeCount);
         Assert.AreEqual(1, pageIndexChangeCount);
         Assert.AreEqual(1, perPageChangeCount);
      }

   #endregion

   #region X-1: 잘못된 입력

      // ------------------------------------------------------------
      /// <summary>
      /// 잘못된 페이지당 개수 입력 예외를 검증한다.
      /// </summary>
      // ------------------------------------------------------------
      [Test]
      public void TEST_Paginator_잘못된_PerPage_예외()
      {
         Assert.Throws<ArgumentOutOfRangeException>(() => new Paginator(0));

         var paginator = new Paginator();

         Assert.Throws<ArgumentOutOfRangeException>(() => paginator.PerPage = 0);
         Assert.Throws<ArgumentOutOfRangeException>(() => paginator.PerPage = -1);
      }

      // ------------------------------------------------------------
      /// <summary>
      /// 잘못된 전체 개수 입력 예외를 검증한다.
      /// </summary>
      // ------------------------------------------------------------
      [Test]
      public void TEST_Paginator_잘못된_TotalCount_예외()
      {
         var paginator = new Paginator();

         Assert.Throws<ArgumentOutOfRangeException>(() => paginator.TotalCount = -1);
      }

      // ------------------------------------------------------------
      /// <summary>
      /// 잘못된 페이지 범위 입력 예외를 검증한다.
      /// </summary>
      // ------------------------------------------------------------
      [Test]
      public void TEST_PageRange_잘못된_범위_예외()
      {
         Assert.Throws<ArgumentOutOfRangeException>(() => new PageRange(-1, 0));
         Assert.Throws<ArgumentOutOfRangeException>(() => new PageRange(2, 1));
      }

   #endregion

   }

}
