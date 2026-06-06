/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : PickerToolbarBinder.cs
수정일 : 2026-06-07

# 설명
Picker search field와 filter chip 입력을 session에 연결한다.
========================================================================= BLOCK_HEADER_END */

using UnityEngine;
using UnityEngine.UIElements;

namespace inonego.Xeri.UI.Picker
{
   // ============================================================
   /// <summary>
   /// Picker search field와 filter chip 입력을 session에 연결한다.
   /// </summary>
   // ============================================================
   internal sealed class PickerToolbarBinder<TEntry, TValue>
   {

   #region 필드

      private readonly PickerSession<TEntry, TValue> session;
      private readonly VisualElement searchField;
      private readonly VisualElement filterContainer;

   #endregion

   #region 생성자

      // ------------------------------------------------------------
      /// <summary>
      /// toolbar binder를 생성한다.
      /// </summary>
      // ------------------------------------------------------------
      public PickerToolbarBinder(PickerSession<TEntry, TValue> session, VisualElement root) : base()
      {
         this.session    = session;
         searchField     = root.Q<VisualElement>("search-field");
         filterContainer = root.Q<VisualElement>("filter-chip-container");

         searchField.RegisterCallback<ChangeEvent<string>>(evt => session.SetSearchText(evt.newValue));
         BuildFilters();
      }

   #endregion

   #region 메서드

      // ------------------------------------------------------------
      /// <summary>
      /// filter chip 표시 상태를 갱신한다.
      /// </summary>
      // ------------------------------------------------------------
      public void Refresh()
      {
         foreach (var element in filterContainer.Children())
         {
            var button = element as Button;
            if (button == null) continue;

            var filterID = button.name;
            button.EnableInClassList("xeri-picker__filter-chip--active", session.GetFilterEnabled(filterID));
         }
      }

      // ------------------------------------------------------------
      /// <summary>
      /// spec filter 목록으로 chip 버튼을 만든다.
      /// </summary>
      // ------------------------------------------------------------
      private void BuildFilters()
      {
         filterContainer.Clear();

         var index = 0;
         foreach (var filter in session.Filters)
         {
            var button = new Button(() =>
            {
               var next = !session.GetFilterEnabled(filter.ID);
               session.SetFilterEnabled(filter.ID, next);
            })
            {
               name = filter.ID,
               text = filter.Label,
            };

            button.AddToClassList("xeri-picker__filter-chip");
            button.AddToClassList($"xeri-picker__filter-chip--tone-{index % 4}");
            filterContainer.Add(button);

            index++;
         }
      }

   #endregion

   }
}
