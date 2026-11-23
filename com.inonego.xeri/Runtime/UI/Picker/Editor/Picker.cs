/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : Picker.cs
수정일 : 2026-11-22
# 설명
Picker 선택 UI를 독립 모달 창 또는 앵커에 붙는 팝업으로 여는 Editor 전용 공개 진입점.

# 특이사항
Show 계열은 PickerWindow 모달 창을, ShowPopup은 공통 팝업 호스트의 팝업을 열며 소비자가 표시 방식을 고른다.
입구는 표시 방식·데이터 모양마다 하나이고, 현재 선택값이 없으면 default를, 취소 처리가 필요하면 onCanceled를 넘긴다.
목록은 늘 한 목록에서 스크롤한다(TableGridNavigation.Scroll).
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UIElements;

using inonego;
using inonego.Xeri;
using inonego.Xeri.UI;
using inonego.Xeri.UI.Picker;

namespace inonego.Xeri.UI.Picker.Editor
{
    // ============================================================
    /// <summary>
    /// Picker 선택 UI의 Editor 전용 공개 진입점.
    /// </summary>
    // ============================================================
    public static class Picker
    {

    #region Show

        // --------------------------------------------------------------------------------
        /// <summary>
        /// <br/> Picker modal window를 표시한다.
        /// <br/> 현재 선택값이 없으면 default를 넘긴다. onCanceled는 확정 없이 닫힐 때 부른다.
        /// </summary>
        // --------------------------------------------------------------------------------
        public static PickerWindow Show<TEntry, TValue>
        (
            PickerSpec<TEntry, TValue> spec,
            IReadOnlyList<TEntry> entries,
            TValue currentValue,
            Action<TValue> onSelected,
            Action onCanceled = null
        )
        {
            return PickerWindow.OpenModal(spec, entries, currentValue, onSelected, onCanceled);
        }

        // --------------------------------------------------------------------------------
        /// <summary>
        /// <br/> 앵커 요소에 붙는 Picker 팝업을 표시한다.
        /// <br/> 현재 선택값이 없으면 default를 넘긴다. onCanceled는 확정 없이 닫힐 때 부른다.
        /// </summary>
        // --------------------------------------------------------------------------------
        public static void ShowPopup<TEntry, TValue>
        (
            PickerSpec<TEntry, TValue> spec,
            IReadOnlyList<TEntry> entries,
            TValue currentValue,
            Action<TValue> onSelected,
            VisualElement anchor,
            Action onCanceled = null
        )
        {
            PickerPopup.Open(spec, entries, currentValue, onSelected, onCanceled, anchor);
        }

    #endregion

    #region ShowList

        // ------------------------------------------------------------
        /// <summary>
        /// list entry 자체를 선택값으로 반환하는 modal window를 표시한다.
        /// </summary>
        // ------------------------------------------------------------
        public static PickerWindow ShowList<TEntry>
        (
            string title,
            IReadOnlyList<TEntry> entries,
            TEntry currentValue,
            Action<TEntry> onSelected,
            Action onCanceled = null
        )
        {
            var spec = ListPicker.Spec<TEntry>(title).Build();

            return Show(spec, entries, currentValue, onSelected, onCanceled);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// list entry에서 선택값을 추출하는 modal window를 표시한다.
        /// </summary>
        // ------------------------------------------------------------
        public static PickerWindow ShowList<TEntry, TValue>
        (
            string title,
            IReadOnlyList<TEntry> entries,
            Func<TEntry, TValue> valueGetter,
            TValue currentValue,
            Action<TValue> onSelected,
            Action onCanceled = null
        )
        {
            var spec = ListPicker.Spec(title, valueGetter).Build();

            return Show(spec, entries, currentValue, onSelected, onCanceled);
        }

    #endregion

    #region ShowDictionary

        // ------------------------------------------------------------
        /// <summary>
        /// dictionary의 키를 고르는 modal window를 표시한다.
        /// </summary>
        // ------------------------------------------------------------
        public static PickerWindow ShowDictionary<TKey, TValue>
        (
            string title,
            IReadOnlyDictionary<TKey, TValue> dictionary,
            TKey currentKey,
            Action<TKey> onSelected,
            Action onCanceled = null
        )
        {
            var spec = DictionaryPicker.Spec<TKey, TValue>(title).Build();
            var entries = DictionaryPicker.Entries(dictionary);

            return Show(spec, entries, currentKey, onSelected, onCanceled);
        }

    #endregion

    }
}
