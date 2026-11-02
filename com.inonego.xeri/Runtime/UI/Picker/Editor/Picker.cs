/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : Picker.cs
수정일 : 2026-10-28
# 설명
Picker 선택 UI를 독립 모달 창 또는 앵커에 붙는 팝업으로 여는 Editor 전용 공개 진입점.

# 특이사항
Show 계열은 PickerWindow 모달 창을, ShowPopup은 공통 팝업 호스트의 팝업을 열며 소비자가 표시 방식을 고른다.
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

namespace inonego.Xeri.Editor.Picker
{
    // ============================================================
    /// <summary>
    /// Picker 선택 UI의 Editor 전용 공개 진입점.
    /// </summary>
    // ============================================================
    public static class Picker
    {

    #region 상수

        private const int defaultPageSize = 8;

    #endregion

    #region Show

        // ------------------------------------------------------------
        /// <summary>
        /// Picker modal window를 표시한다.
        /// </summary>
        // ------------------------------------------------------------
        public static PickerWindow Show<TEntry, TValue>
        (
            PickerSpec<TEntry, TValue> spec,
            IReadOnlyList<TEntry> entries,
            TValue currentValue,
            Action<TValue> onSelected,
            int pageSize = defaultPageSize
        )
        {
            return Show(spec, entries, currentValue, onSelected, null, pageSize);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 취소 callback을 포함해 Picker modal window를 표시한다.
        /// </summary>
        // ------------------------------------------------------------
        public static PickerWindow Show<TEntry, TValue>
        (
            PickerSpec<TEntry, TValue> spec,
            IReadOnlyList<TEntry> entries,
            TValue currentValue,
            Action<TValue> onSelected,
            Action onCanceled,
            int pageSize = defaultPageSize
        )
        {
            return PickerWindow.OpenModal
            (
                spec,
                entries,
                currentValue,
                onSelected,
                onCanceled,
                pageSize
            );
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 앵커 요소에 붙는 Picker 팝업을 표시한다.
        /// </summary>
        // ------------------------------------------------------------
        public static void ShowPopup<TEntry, TValue>
        (
            PickerSpec<TEntry, TValue> spec,
            IReadOnlyList<TEntry> entries,
            TValue currentValue,
            Action<TValue> onSelected,
            VisualElement anchor,
            int pageSize = defaultPageSize
        )
        {
            ShowPopup(spec, entries, currentValue, onSelected, anchor, null, pageSize);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 취소 callback을 포함해 앵커 요소에 붙는 Picker 팝업을 표시한다.
        /// </summary>
        // ------------------------------------------------------------
        public static void ShowPopup<TEntry, TValue>
        (
            PickerSpec<TEntry, TValue> spec,
            IReadOnlyList<TEntry> entries,
            TValue currentValue,
            Action<TValue> onSelected,
            VisualElement anchor,
            Action onCanceled,
            int pageSize = defaultPageSize
        )
        {
            PickerPopup.Open
            (
                spec,
                entries,
                currentValue,
                onSelected,
                onCanceled,
                pageSize,
                anchor
            );
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 선택값 없이 Picker modal window를 표시한다.
        /// </summary>
        // ------------------------------------------------------------
        public static PickerWindow Show<TEntry, TValue>
        (
            PickerSpec<TEntry, TValue> spec,
            IReadOnlyList<TEntry> entries,
            Action<TValue> onSelected,
            int pageSize = defaultPageSize
        )
        {
            return Show(spec, entries, default, onSelected, pageSize);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 선택값 없이 앵커 요소에 붙는 Picker 팝업을 표시한다.
        /// </summary>
        // ------------------------------------------------------------
        public static void ShowPopup<TEntry, TValue>
        (
            PickerSpec<TEntry, TValue> spec,
            IReadOnlyList<TEntry> entries,
            Action<TValue> onSelected,
            VisualElement anchor,
            int pageSize = defaultPageSize
        )
        {
            ShowPopup(spec, entries, default, onSelected, anchor, pageSize);
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// 현재 선택값 없이 취소 callback을 포함해 Picker modal window를 표시한다.
        /// </summary>
        // ----------------------------------------------------------------------
        public static PickerWindow Show<TEntry, TValue>
        (
            PickerSpec<TEntry, TValue> spec,
            IReadOnlyList<TEntry> entries,
            Action<TValue> onSelected,
            Action onCanceled,
            int pageSize = defaultPageSize
        )
        {
            return Show(spec, entries, default, onSelected, onCanceled, pageSize);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> 현재 선택값 없이 취소 callback을 포함해
        /// <br/> 앵커 요소에 붙는 Picker 팝업을 표시한다.
        /// </summary>
        // ------------------------------------------------------------
        public static void ShowPopup<TEntry, TValue>
        (
            PickerSpec<TEntry, TValue> spec,
            IReadOnlyList<TEntry> entries,
            Action<TValue> onSelected,
            VisualElement anchor,
            Action onCanceled,
            int pageSize = defaultPageSize
        )
        {
            ShowPopup(spec, entries, default, onSelected, anchor, onCanceled, pageSize);
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
            int pageSize = defaultPageSize
        )
        {
            return ShowList(title, entries, currentValue, onSelected, null, pageSize);
        }

        // --------------------------------------------------------------------------------
        /// <summary>
        /// 취소 callback을 포함해 list entry 자체를 선택값으로 반환하는 modal window를 표시한다.
        /// </summary>
        // --------------------------------------------------------------------------------
        public static PickerWindow ShowList<TEntry>
        (
            string title,
            IReadOnlyList<TEntry> entries,
            TEntry currentValue,
            Action<TEntry> onSelected,
            Action onCanceled,
            int pageSize = defaultPageSize
        )
        {
            var spec = ListPicker.Spec<TEntry>(title).Build();

            return Show(spec, entries, currentValue, onSelected, onCanceled, pageSize);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 선택값 없이 list picker modal window를 표시한다.
        /// </summary>
        // ------------------------------------------------------------
        public static PickerWindow ShowList<TEntry>
        (
            string title,
            IReadOnlyList<TEntry> entries,
            Action<TEntry> onSelected,
            int pageSize = defaultPageSize
        )
        {
            return ShowList(title, entries, default, onSelected, pageSize);
        }

        // --------------------------------------------------------------------------------
        /// <summary>
        /// 현재 선택값 없이 취소 callback을 포함해 list picker modal window를 표시한다.
        /// </summary>
        // --------------------------------------------------------------------------------
        public static PickerWindow ShowList<TEntry>
        (
            string title,
            IReadOnlyList<TEntry> entries,
            Action<TEntry> onSelected,
            Action onCanceled,
            int pageSize = defaultPageSize
        )
        {
            return ShowList(title, entries, default, onSelected, onCanceled, pageSize);
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
            int pageSize = defaultPageSize
        )
        {
            return ShowList(title, entries, valueGetter, currentValue, onSelected, null, pageSize);
        }

        // --------------------------------------------------------------------------------
        /// <summary>
        /// 취소 callback을 포함해 list entry에서 선택값을 추출하는 modal window를 표시한다.
        /// </summary>
        // --------------------------------------------------------------------------------
        public static PickerWindow ShowList<TEntry, TValue>
        (
            string title,
            IReadOnlyList<TEntry> entries,
            Func<TEntry, TValue> valueGetter,
            TValue currentValue,
            Action<TValue> onSelected,
            Action onCanceled,
            int pageSize = defaultPageSize
        )
        {
            var spec = ListPicker.Spec(title, valueGetter).Build();

            return Show(spec, entries, currentValue, onSelected, onCanceled, pageSize);
        }

    #endregion

    #region ShowDictionary

        // ------------------------------------------------------------
        /// <summary>
        /// dictionary picker modal window를 표시한다.
        /// </summary>
        // ------------------------------------------------------------
        public static PickerWindow ShowDictionary<TKey, TValue>
        (
            string title,
            IReadOnlyDictionary<TKey, TValue> dictionary,
            TKey currentKey,
            Action<TKey> onSelected,
            int pageSize = defaultPageSize
        )
        {
            return ShowDictionary(title, dictionary, currentKey, onSelected, null, pageSize);
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// 취소 callback을 포함해 dictionary picker modal window를 표시한다.
        /// </summary>
        // ----------------------------------------------------------------------
        public static PickerWindow ShowDictionary<TKey, TValue>
        (
            string title,
            IReadOnlyDictionary<TKey, TValue> dictionary,
            TKey currentKey,
            Action<TKey> onSelected,
            Action onCanceled,
            int pageSize = defaultPageSize
        )
        {
            var spec = DictionaryPicker.Spec<TKey, TValue>(title).Build();
            var entries = DictionaryPicker.Entries(dictionary);

            return Show(spec, entries, currentKey, onSelected, onCanceled, pageSize);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 선택값 없이 dictionary picker modal window를 표시한다.
        /// </summary>
        // ------------------------------------------------------------
        public static PickerWindow ShowDictionary<TKey, TValue>
        (
            string title,
            IReadOnlyDictionary<TKey, TValue> dictionary,
            Action<TKey> onSelected,
            int pageSize = defaultPageSize
        )
        {
            return ShowDictionary(title, dictionary, default, onSelected, pageSize);
        }

        // --------------------------------------------------------------------------------
        /// <summary>
        /// 현재 선택값 없이 취소 callback을 포함해 dictionary picker modal window를 표시한다.
        /// </summary>
        // --------------------------------------------------------------------------------
        public static PickerWindow ShowDictionary<TKey, TValue>
        (
            string title,
            IReadOnlyDictionary<TKey, TValue> dictionary,
            Action<TKey> onSelected,
            Action onCanceled,
            int pageSize = defaultPageSize
        )
        {
            return ShowDictionary(title, dictionary, default, onSelected, onCanceled, pageSize);
        }

    #endregion

    }
}
