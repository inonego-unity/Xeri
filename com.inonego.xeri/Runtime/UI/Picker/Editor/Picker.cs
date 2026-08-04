/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : Picker.cs
수정일 : 2026-08-04

# 설명
Picker 선택 UI를 모달 또는 dropdown으로 여는 Editor 전용 공개 진입점.

# 특이사항
PickerWindow는 Unity EditorWindow 호스트이고, 이 타입이 소비자가 선택할 표시 방식을 결정한다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections.Generic;

using UnityEngine;

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

        private const int DefaultPageSize = 8;

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
            int pageSize = DefaultPageSize
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
            int pageSize = DefaultPageSize
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
        /// rect 기준으로 Picker dropdown window를 표시한다.
        /// </summary>
        // ------------------------------------------------------------
        public static PickerWindow Show<TEntry, TValue>
        (
            PickerSpec<TEntry, TValue> spec,
            IReadOnlyList<TEntry> entries,
            TValue currentValue,
            Action<TValue> onSelected,
            Rect rect,
            int pageSize = DefaultPageSize
        )
        {
            return Show(spec, entries, currentValue, onSelected, rect, null, pageSize);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 취소 callback을 포함해 rect 기준으로 Picker dropdown window를 표시한다.
        /// </summary>
        // ------------------------------------------------------------
        public static PickerWindow Show<TEntry, TValue>
        (
            PickerSpec<TEntry, TValue> spec,
            IReadOnlyList<TEntry> entries,
            TValue currentValue,
            Action<TValue> onSelected,
            Rect rect,
            Action onCanceled,
            int pageSize = DefaultPageSize
        )
        {
            return PickerWindow.OpenDropdown
            (
                spec,
                entries,
                currentValue,
                onSelected,
                onCanceled,
                pageSize,
                rect
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
            int pageSize = DefaultPageSize
        )
        {
            return Show(spec, entries, default, onSelected, pageSize);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 선택값 없이 rect 기준으로 Picker dropdown window를 표시한다.
        /// </summary>
        // ------------------------------------------------------------
        public static PickerWindow Show<TEntry, TValue>
        (
            PickerSpec<TEntry, TValue> spec,
            IReadOnlyList<TEntry> entries,
            Action<TValue> onSelected,
            Rect rect,
            int pageSize = DefaultPageSize
        )
        {
            return Show(spec, entries, default, onSelected, rect, pageSize);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 선택값 없이 취소 callback을 포함해 Picker modal window를 표시한다.
        /// </summary>
        // ------------------------------------------------------------
        public static PickerWindow Show<TEntry, TValue>
        (
            PickerSpec<TEntry, TValue> spec,
            IReadOnlyList<TEntry> entries,
            Action<TValue> onSelected,
            Action onCanceled,
            int pageSize = DefaultPageSize
        )
        {
            return Show(spec, entries, default, onSelected, onCanceled, pageSize);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 선택값 없이 취소 callback을 포함해 rect 기준으로 Picker dropdown window를 표시한다.
        /// </summary>
        // ------------------------------------------------------------
        public static PickerWindow Show<TEntry, TValue>
        (
            PickerSpec<TEntry, TValue> spec,
            IReadOnlyList<TEntry> entries,
            Action<TValue> onSelected,
            Rect rect,
            Action onCanceled,
            int pageSize = DefaultPageSize
        )
        {
            return Show(spec, entries, default, onSelected, rect, onCanceled, pageSize);
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
            int pageSize = DefaultPageSize
        )
        {
            return ShowList(title, entries, currentValue, onSelected, null, pageSize);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 취소 callback을 포함해 list entry 자체를 선택값으로 반환하는 modal window를 표시한다.
        /// </summary>
        // ------------------------------------------------------------
        public static PickerWindow ShowList<TEntry>
        (
            string title,
            IReadOnlyList<TEntry> entries,
            TEntry currentValue,
            Action<TEntry> onSelected,
            Action onCanceled,
            int pageSize = DefaultPageSize
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
            int pageSize = DefaultPageSize
        )
        {
            return ShowList(title, entries, default, onSelected, pageSize);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 선택값 없이 취소 callback을 포함해 list picker modal window를 표시한다.
        /// </summary>
        // ------------------------------------------------------------
        public static PickerWindow ShowList<TEntry>
        (
            string title,
            IReadOnlyList<TEntry> entries,
            Action<TEntry> onSelected,
            Action onCanceled,
            int pageSize = DefaultPageSize
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
            int pageSize = DefaultPageSize
        )
        {
            return ShowList(title, entries, valueGetter, currentValue, onSelected, null, pageSize);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 취소 callback을 포함해 list entry에서 선택값을 추출하는 modal window를 표시한다.
        /// </summary>
        // ------------------------------------------------------------
        public static PickerWindow ShowList<TEntry, TValue>
        (
            string title,
            IReadOnlyList<TEntry> entries,
            Func<TEntry, TValue> valueGetter,
            TValue currentValue,
            Action<TValue> onSelected,
            Action onCanceled,
            int pageSize = DefaultPageSize
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
            int pageSize = DefaultPageSize
        )
        {
            return ShowDictionary(title, dictionary, currentKey, onSelected, null, pageSize);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 취소 callback을 포함해 dictionary picker modal window를 표시한다.
        /// </summary>
        // ------------------------------------------------------------
        public static PickerWindow ShowDictionary<TKey, TValue>
        (
            string title,
            IReadOnlyDictionary<TKey, TValue> dictionary,
            TKey currentKey,
            Action<TKey> onSelected,
            Action onCanceled,
            int pageSize = DefaultPageSize
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
            int pageSize = DefaultPageSize
        )
        {
            return ShowDictionary(title, dictionary, default, onSelected, pageSize);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 선택값 없이 취소 callback을 포함해 dictionary picker modal window를 표시한다.
        /// </summary>
        // ------------------------------------------------------------
        public static PickerWindow ShowDictionary<TKey, TValue>
        (
            string title,
            IReadOnlyDictionary<TKey, TValue> dictionary,
            Action<TKey> onSelected,
            Action onCanceled,
            int pageSize = DefaultPageSize
        )
        {
            return ShowDictionary(title, dictionary, default, onSelected, onCanceled, pageSize);
        }

    #endregion
    }
}
