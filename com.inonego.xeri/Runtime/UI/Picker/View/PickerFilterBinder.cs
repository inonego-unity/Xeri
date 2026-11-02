/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : PickerFilterBinder.cs
수정일 : 2026-10-18

# 설명
Picker filter chip을 session에 연결한다.
========================================================================= BLOCK_HEADER_END */

using UnityEngine;
using UnityEngine.UIElements;

using inonego;
using inonego.Xeri;
using inonego.Xeri.UI;

namespace inonego.Xeri.UI.Picker
{
    // ============================================================
    /// <summary>
    /// Picker filter chip을 session에 연결한다.
    /// </summary>
    // ============================================================
    internal sealed class PickerFilterBinder<TEntry, TValue>
    {

    #region 필드

        private readonly PickerSession<TEntry, TValue> session;
        private readonly VisualElement filterContainer;

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// Filter 정의에 대응하는 칩 UI를 구성한다.
        /// </summary>
        // ------------------------------------------------------------
        public PickerFilterBinder(PickerSession<TEntry, TValue> session, VisualElement root) : base()
        {
            this.session    = session;
            filterContainer = root.Q<VisualElement>("filter-chip-container");

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
                var button = new Button
                (
                    () =>
                    {
                        var next = !session.GetFilterEnabled(filter.ID);
                        session.SetFilterEnabled(filter.ID, next);
                    }
                )
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
