/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : PickerWindow.cs
수정일 : 2026-08-04

# 설명
PickerView를 Unity EditorWindow 안에 호스팅하고 창 수명주기를 관리한다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections.Generic;

using UnityEditor;
using UnityEngine;

using inonego.Xeri.UI.Picker;

namespace inonego.Xeri.Editor.Picker
{
    // ============================================================
    /// <summary>
    /// PickerView를 Unity EditorWindow 안에 호스팅한다.
    /// </summary>
    // ============================================================
    public sealed class PickerWindow : EditorWindow
    {
    #region 필드

        private static readonly Vector2 minimumModalWindowSize = new Vector2(640f, 560f);
        private static readonly Vector2 initialModalWindowSize = new Vector2(680f, 620f);
        private static readonly Vector2 minimumDropdownWindowSize = new Vector2(250f, 180f);
        private static readonly Vector2 initialDropdownWindowSize = new Vector2(290f, 280f);
        private IPickerWindowBridge bridge = null;
        private bool isDropdown = false;

    #endregion

    #region 생성

        // ------------------------------------------------------------
        /// <summary>
        /// Picker를 모달 EditorWindow로 연다.
        /// </summary>
        // ------------------------------------------------------------
        internal static PickerWindow OpenModal<TEntry, TValue>
        (
            PickerSpec<TEntry, TValue> spec,
            IReadOnlyList<TEntry> entries,
            TValue currentValue,
            Action<TValue> onSelected,
            Action onCanceled,
            int pageSize
        )
        {
            var window = Create
            (
                spec,
                entries,
                currentValue,
                onSelected,
                onCanceled,
                pageSize,
                minimumModalWindowSize,
                isDropdown: false
            );
            window.position = new Rect
            (
                100f,
                100f,
                initialModalWindowSize.x,
                initialModalWindowSize.y
            );
            window.ShowModalUtility();

            return window;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Picker를 지정한 rect에 연결된 dropdown EditorWindow로 연다.
        /// </summary>
        // ------------------------------------------------------------
        internal static PickerWindow OpenDropdown<TEntry, TValue>
        (
            PickerSpec<TEntry, TValue> spec,
            IReadOnlyList<TEntry> entries,
            TValue currentValue,
            Action<TValue> onSelected,
            Action onCanceled,
            int pageSize,
            Rect rect
        )
        {
            var window = Create
            (
                spec,
                entries,
                currentValue,
                onSelected,
                onCanceled,
                pageSize,
                minimumDropdownWindowSize,
                isDropdown: true
            );
            window.ShowAsDropDown(rect, initialDropdownWindowSize);

            return window;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 표시 방식과 무관한 PickerView 및 취소 수명주기를 초기화한다.
        /// </summary>
        // ------------------------------------------------------------
        private static PickerWindow Create<TEntry, TValue>
        (
            PickerSpec<TEntry, TValue> spec,
            IReadOnlyList<TEntry> entries,
            TValue currentValue,
            Action<TValue> onSelected,
            Action onCanceled,
            int pageSize,
            Vector2 minimumWindowSize,
            bool isDropdown
        )
        {
            var window = CreateInstance<PickerWindow>();
            window.bridge = new PickerWindowBridge<TEntry, TValue>
            (
                spec,
                entries,
                currentValue,
                onSelected,
                onCanceled,
                pageSize
            );
            window.titleContent = new GUIContent(window.bridge.Title);
            window.minSize = minimumWindowSize;
            window.isDropdown = isDropdown;

            return window;
        }

    #endregion

    #region Unity 이벤트

        // ------------------------------------------------------------
        /// <summary>
        /// EditorWindow UI를 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        private void CreateGUI()
        {
            rootVisualElement.Clear();
            var view = bridge.CreateView(Close);
            if (isDropdown)
            {
                view.AddToClassList("xeri-picker-host--dropdown");
            }

            rootVisualElement.Add(view);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 확정 선택 없이 닫힌 Picker를 취소로 처리한다.
        /// </summary>
        // ------------------------------------------------------------
        private void OnDestroy()
        {
            bridge?.CancelIfNotCompleted();
            bridge = null;
        }

    #endregion
    }
}
