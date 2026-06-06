/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : PageRange.cs
수정일 : 2026-06-06

# 설명
현재 페이지가 참조할 0-based slice 범위 값.
컬렉션을 직접 자르지 않고 범위 계산 결과만 표현한다.
========================================================================= BLOCK_HEADER_END */

using System;

namespace inonego.Xeri.Utility
{
   // ============================================================
   /// <summary>
   /// 현재 페이지가 참조할 0-based slice 범위 값.
   /// </summary>
   // ============================================================
   [Serializable]
   public readonly struct PageRange
   {

   #region 필드

      // ------------------------------------------------------------
      /// <summary>
      /// begin index. 0부터 시작한다.
      /// </summary>
      // ------------------------------------------------------------
      public int BeginIndex { get; }

      // ------------------------------------------------------------
      /// <summary>
      /// 범위 끝 index. 이 값은 포함하지 않는다.
      /// </summary>
      // ------------------------------------------------------------
      public int EndIndex { get; }

      // ------------------------------------------------------------
      /// <summary>
      /// 현재 범위에 포함되는 아이템 개수.
      /// </summary>
      // ------------------------------------------------------------
      public int Count { get; }

      // ------------------------------------------------------------
      /// <summary>
      /// 범위가 비어 있는지 여부.
      /// </summary>
      // ------------------------------------------------------------
      public bool IsEmpty => Count == 0;

   #endregion

   #region 생성자

      // ------------------------------------------------------------
      /// <summary>
      /// 범위 값을 생성한다. end index는 범위에 포함하지 않는다.
      /// </summary>
      // ------------------------------------------------------------
      public PageRange(int beginIndex, int endIndex) : this()
      {
         if (beginIndex < 0)
         {
            throw new ArgumentOutOfRangeException(nameof(beginIndex), "begin index는 0 이상이어야 합니다.");
         }

         if (endIndex < beginIndex)
         {
            throw new ArgumentOutOfRangeException(nameof(endIndex), "end index는 begin index 이상이어야 합니다.");
         }

         (BeginIndex, EndIndex, Count) = (beginIndex, endIndex, endIndex - beginIndex);
      }

   #endregion

   }
}
