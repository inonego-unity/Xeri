/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : PickerFooterBinder.cs
수정일 : 2026-06-06

# 설명
Picker footer count와 page control을 session paginator에 바인딩한다.
========================================================================= BLOCK_HEADER_END */

using UnityEngine;
using UnityEngine.UIElements;

namespace inonego.Xeri.UI.Picker
{
   // ============================================================
   /// <summary>
   /// Picker footer count와 page control을 session paginator에 바인딩한다.
   /// </summary>
   // ============================================================
   internal sealed class PickerFooterBinder<TEntry, TValue>
   {

   #region 필드

      private readonly PickerSession<TEntry, TValue> session;
      private readonly Label countLabel;
      private readonly Label pageLabel;
      private readonly Button firstButton;
      private readonly Button prevButton;
      private readonly Button nextButton;
      private readonly Button lastButton;

   #endregion

   #region 생성자

      // ------------------------------------------------------------
      /// <summary>
      /// footer binder를 생성한다.
      /// </summary>
      // ------------------------------------------------------------
      public PickerFooterBinder(PickerSession<TEntry, TValue> session, VisualElement root) : base()
      {
         this.session = session;
         countLabel   = root.Q<Label>("count-label");
         pageLabel    = root.Q<Label>("page-label");
         firstButton  = root.Q<Button>("first-page-button");
         prevButton   = root.Q<Button>("prev-page-button");
         nextButton   = root.Q<Button>("next-page-button");
         lastButton   = root.Q<Button>("last-page-button");

         firstButton.clicked += session.MoveFirst;
         prevButton.clicked  += session.MovePrev;
         nextButton.clicked  += session.MoveNext;
         lastButton.clicked  += session.MoveLast;
      }

   #endregion

   #region 메서드

      // ------------------------------------------------------------
      /// <summary>
      /// footer 표시 정보를 갱신한다.
      /// </summary>
      // ------------------------------------------------------------
      public void Refresh()
      {
         var range = session.Paginator.Range;
         var begin = range.IsEmpty ? 0 : range.BeginIndex + 1;
         var end = range.EndIndex;

         countLabel.text = $"{begin}-{end} / 전체 {session.FilteredCount}개";
         pageLabel.text = $"{session.PageNumber}/{session.PageCount}";

         firstButton.SetEnabled(session.Paginator.CanMoveFirst);
         prevButton.SetEnabled(session.Paginator.CanMovePrev);
         nextButton.SetEnabled(session.Paginator.CanMoveNext);
         lastButton.SetEnabled(session.Paginator.CanMoveLast);
      }

   #endregion

   }
}
