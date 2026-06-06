/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : PickerFilterSpec.cs
수정일 : 2026-06-06

# 설명
Picker filter chip과 실제 필터 조건을 함께 보관하는 모델.
========================================================================= BLOCK_HEADER_END */

using System;

using inonego.Xeri;

namespace inonego.Xeri.UI.Picker
{
   // ============================================================
   /// <summary>
   /// Picker filter chip과 실제 필터 조건을 함께 보관하는 모델.
   /// </summary>
   // ============================================================
   [Serializable]
   public sealed class PickerFilterSpec<TEntry, TValue>
   {

   #region 필드

      // ------------------------------------------------------------
      /// <summary>
      /// filter 식별자.
      /// </summary>
      // ------------------------------------------------------------
      public readonly string ID;

      // ------------------------------------------------------------
      /// <summary>
      /// filter 표시 라벨.
      /// </summary>
      // ------------------------------------------------------------
      public readonly string Label;

      // ------------------------------------------------------------
      /// <summary>
      /// 기본 활성 상태.
      /// </summary>
      // ------------------------------------------------------------
      public readonly bool DefaultEnabled;

      private readonly IFilter<PickerEntry<TEntry, TValue>> filter;

   #endregion

   #region 생성자

      // ------------------------------------------------------------
      /// <summary>
      /// filter 정의를 생성한다.
      /// </summary>
      // ------------------------------------------------------------
      public PickerFilterSpec
      (
         string id,
         string label,
         bool defaultEnabled,
         IFilter<PickerEntry<TEntry, TValue>> filter
      ) : base()
      {
         ID             = id ?? string.Empty;
         Label          = label ?? string.Empty;
         DefaultEnabled = defaultEnabled;
         this.filter    = filter ?? new PredicateFilter<PickerEntry<TEntry, TValue>>(_ => true);
      }

   #endregion

   #region 메서드

      // ------------------------------------------------------------
      /// <summary>
      /// entry가 filter 조건에 맞는지 확인한다.
      /// </summary>
      // ------------------------------------------------------------
      public bool IsMatch(PickerEntry<TEntry, TValue> entry)
      {
         return filter.IsMatch(entry);
      }

   #endregion

   }

   // ============================================================
   /// <summary>
   /// lambda predicate를 IFilter 계약으로 감싸는 내부 어댑터.
   /// </summary>
   // ============================================================
   internal sealed class PredicateFilter<T> : IFilter<T>
   {

   #region 필드

      private readonly Func<T, bool> predicate;

   #endregion

   #region 생성자

      // ------------------------------------------------------------
      /// <summary>
      /// predicate filter를 생성한다.
      /// </summary>
      // ------------------------------------------------------------
      public PredicateFilter(Func<T, bool> predicate) : base()
      {
         this.predicate = predicate ?? (_ => true);
      }

   #endregion

   #region 메서드

      // ------------------------------------------------------------
      /// <summary>
      /// 지정한 값이 조건에 맞는지 확인한다.
      /// </summary>
      // ------------------------------------------------------------
      public bool IsMatch(T value)
      {
         return predicate(value);
      }

   #endregion

   }
}
