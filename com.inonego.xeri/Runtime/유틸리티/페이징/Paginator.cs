/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : Paginator.cs
수정일 : 2026-06-06

# 설명
아이템 목록의 페이징 상태를 관리하는 유틸리티 클래스.
컬렉션 자체는 모르고 전체 개수, 페이지 크기, 현재 페이지 index만 계산한다.
========================================================================= BLOCK_HEADER_END */

using System;

using UnityEngine;

namespace inonego.Xeri.Utility
{
   // ============================================================
   /// <summary>
   /// 아이템 목록의 페이징 상태를 관리하는 유틸리티 클래스.
   /// </summary>
   // ============================================================
   [Serializable]
   public class Paginator : IReadOnlyPaginator
   {

   #region 필드

      // ------------------------------------------------------------
      /// <summary>
      /// 전체 아이템 개수.
      /// </summary>
      // ------------------------------------------------------------
      public int TotalCount
      {
         get => totalCount;
         set => ApplyState(value, PerPage, PageIndex);
      }
      [SerializeField]
      private int totalCount = 0;

      // ------------------------------------------------------------
      /// <summary>
      /// 한 페이지에 표시할 아이템 개수.
      /// </summary>
      // ------------------------------------------------------------
      public int PerPage
      {
         get => perPage;
         set
         {
            // 입력 검증: 이후 보정 계산은 유효한 per page를 전제로 한다.
            ValidatePerPage(value);

            // 기존 첫 표시 위치를 기준으로 새 page index를 계산해 사용자의 위치 맥락을 유지한다.
            var currentBeginIndex = Range.BeginIndex;
            var nextPageIndex     = IsEmpty ? 0 : currentBeginIndex / value;

            ApplyState(TotalCount, value, nextPageIndex);
         }
      }
      [SerializeField]
      private int perPage = 20;

      // ------------------------------------------------------------
      /// <summary>
      /// 현재 페이지 index. 0부터 시작한다.
      /// </summary>
      // ------------------------------------------------------------
      public int PageIndex
      {
         get => pageIndex;
         set => ApplyState(TotalCount, PerPage, value);
      }
      [SerializeField]
      private int pageIndex = 0;

      // ------------------------------------------------------------
      /// <summary>
      /// 전체 페이지 개수.
      /// </summary>
      // ------------------------------------------------------------
      public int PageCount
      {
         get
         {
            if (TotalCount == 0)
            {
               return 0;
            }

            return (TotalCount + PerPage - 1) / PerPage;
         }
      }

      // ------------------------------------------------------------
      /// <summary>
      /// UI 표시용 현재 페이지 번호. 1부터 시작하고 비어 있으면 0이다.
      /// </summary>
      // ------------------------------------------------------------
      public int PageNumber => IsEmpty ? 0 : PageIndex + 1;

      // ------------------------------------------------------------
      /// <summary>
      /// 표시할 아이템이 없는지 여부.
      /// </summary>
      // ------------------------------------------------------------
      public bool IsEmpty => TotalCount == 0;

      // ------------------------------------------------------------
      /// <summary>
      /// 첫 페이지로 이동할 수 있는지 여부.
      /// </summary>
      // ------------------------------------------------------------
      public bool CanMoveFirst => !IsEmpty && PageIndex > 0;

      // ------------------------------------------------------------
      /// <summary>
      /// 이전 페이지로 이동할 수 있는지 여부.
      /// </summary>
      // ------------------------------------------------------------
      public bool CanMovePrev => !IsEmpty && PageIndex > 0;

      // ------------------------------------------------------------
      /// <summary>
      /// 다음 페이지로 이동할 수 있는지 여부.
      /// </summary>
      // ------------------------------------------------------------
      public bool CanMoveNext => !IsEmpty && PageIndex < PageCount - 1;

      // ------------------------------------------------------------
      /// <summary>
      /// 마지막 페이지로 이동할 수 있는지 여부.
      /// </summary>
      // ------------------------------------------------------------
      public bool CanMoveLast => !IsEmpty && PageIndex < PageCount - 1;

      // ------------------------------------------------------------
      /// <summary>
      /// 현재 페이지의 0-based 범위를 반환한다.
      /// </summary>
      // ------------------------------------------------------------
      public PageRange Range
      {
         get
         {
            // 빈 목록은 begin과 end가 같은 빈 범위로 표현한다.
            var (beginIndex, endIndex) = IsEmpty
               ? (0, 0)
               : (PageIndex * PerPage, Math.Min((PageIndex + 1) * PerPage, TotalCount));

            return new PageRange(beginIndex, endIndex);
         }
      }

   #endregion

   #region 이벤트

      // ------------------------------------------------------------
      /// <summary>
      /// 관찰 가능한 페이징 상태가 변경될 때 발생한다.
      /// </summary>
      // ------------------------------------------------------------
      public event EventHandler OnChange = null;

      // ------------------------------------------------------------
      /// <summary>
      /// 전체 아이템 개수가 변경될 때 발생한다.
      /// </summary>
      // ------------------------------------------------------------
      public event ValueChangeEventHandler<int> OnTotalCountChange = null;

      // ------------------------------------------------------------
      /// <summary>
      /// 한 페이지에 표시할 아이템 개수가 변경될 때 발생한다.
      /// </summary>
      // ------------------------------------------------------------
      public event ValueChangeEventHandler<int> OnPerPageChange = null;

      // ------------------------------------------------------------
      /// <summary>
      /// 현재 페이지 index가 변경될 때 발생한다.
      /// </summary>
      // ------------------------------------------------------------
      public event ValueChangeEventHandler<int> OnPageIndexChange = null;

   #endregion

   #region 생성자

      // ------------------------------------------------------------
      /// <summary>
      /// 기본 페이지 크기로 Paginator를 생성한다.
      /// </summary>
      // ------------------------------------------------------------
      public Paginator() : this(20) {}

      // ------------------------------------------------------------
      /// <summary>
      /// 지정한 페이지 크기로 Paginator를 생성한다.
      /// </summary>
      // ------------------------------------------------------------
      public Paginator(int perPage) : base()
      {
         ValidatePerPage(perPage);

         this.perPage = perPage;
      }

   #endregion

   #region 메서드

      // ------------------------------------------------------------
      /// <summary>
      /// 지정한 page index로 이동한다.
      /// </summary>
      // ------------------------------------------------------------
      public void MoveTo(int pageIndex)
      {
         PageIndex = pageIndex;
      }

      // ------------------------------------------------------------
      /// <summary>
      /// 첫 페이지로 이동한다.
      /// </summary>
      // ------------------------------------------------------------
      public void MoveFirst()
      {
         MoveTo(0);
      }

      // ------------------------------------------------------------
      /// <summary>
      /// 이전 페이지로 이동한다.
      /// </summary>
      // ------------------------------------------------------------
      public void MovePrev()
      {
         MoveTo(PageIndex - 1);
      }

      // ------------------------------------------------------------
      /// <summary>
      /// 다음 페이지로 이동한다.
      /// </summary>
      // ------------------------------------------------------------
      public void MoveNext()
      {
         MoveTo(PageIndex + 1);
      }

      // ------------------------------------------------------------
      /// <summary>
      /// 마지막 페이지로 이동한다.
      /// </summary>
      // ------------------------------------------------------------
      public void MoveLast()
      {
         MoveTo(PageCount - 1);
      }

      // ------------------------------------------------------------
      /// <summary>
      /// 상태 변경을 한 지점에 모아 검증, 보정, 이벤트 발화를 일관되게 처리한다.
      /// </summary>
      // ------------------------------------------------------------
      private void ApplyState(int nextTotalCount, int nextPerPage, int nextPageIndex)
      {
         // 입력 검증: 상태가 일부만 변경되는 상황을 막기 위해 대입 전에 검증한다.
         if (nextTotalCount < 0)
         {
            throw new ArgumentOutOfRangeException(nameof(nextTotalCount), "전체 개수는 0 이상이어야 합니다.");
         }

         ValidatePerPage(nextPerPage);

         // 변경 감지 기준을 고정해 보정 후 실제 변경된 값만 이벤트로 알린다.
         var (prevTotalCount, prevPerPage, prevPageIndex) = (TotalCount, PerPage, PageIndex);

         var nextClampedPageIndex = GetClampedPageIndex(nextTotalCount, nextPerPage, nextPageIndex);

         // 상태 적용: page index는 새 total/per page 기준으로 clamp한다.
         (totalCount, perPage, pageIndex) = (nextTotalCount, nextPerPage, nextClampedPageIndex);

         var isChanged = prevTotalCount != TotalCount
                      || prevPerPage    != PerPage
                      || prevPageIndex  != PageIndex;

         if (prevTotalCount != TotalCount)
         {
            OnTotalCountChange?.Invoke(this, new(prevTotalCount, TotalCount));
         }

         if (prevPerPage != PerPage)
         {
            OnPerPageChange?.Invoke(this, new(prevPerPage, PerPage));
         }

         if (prevPageIndex != PageIndex)
         {
            OnPageIndexChange?.Invoke(this, new(prevPageIndex, PageIndex));
         }

         // 통합 변경 알림은 세부 이벤트 이후에 발화해 구독자가 최신 상태를 읽게 한다.
         if (isChanged)
         {
            OnChange?.Invoke(this, EventArgs.Empty);
         }
      }

      // ------------------------------------------------------------
      /// <summary>
      /// page index 입력 값을 현재 total/per page 기준의 유효 범위로 보정한다.
      /// </summary>
      // ------------------------------------------------------------
      private static int GetClampedPageIndex(int totalCount, int perPage, int candidate)
      {
         if (totalCount == 0)
         {
            return 0;
         }

         var pageCount = (totalCount + perPage - 1) / perPage;

         return Mathf.Clamp(candidate, 0, pageCount - 1);
      }

      // ------------------------------------------------------------
      /// <summary>
      /// per page 입력 값을 검증한다.
      /// </summary>
      // ------------------------------------------------------------
      private static void ValidatePerPage(int perPage)
      {
         if (perPage <= 0)
         {
            throw new ArgumentOutOfRangeException(nameof(perPage), "페이지당 표시 개수는 1 이상이어야 합니다.");
         }
      }

   #endregion

   }
}
