/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : PickerWindow.cs
수정일 : 2026-10-28

# 설명
PickerView를 Unity EditorWindow 안에 호스팅하고 창 수명주기를 관리한다.

# 특이사항, 제약사항
모달 표시 전용이며 앵커에 붙는 팝업은 PickerPopup이 공통 팝업 호스트로 연다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEditor;
using UnityEngine;

using inonego;
using inonego.Xeri;
using inonego.Xeri.UI;
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

        private static readonly Vector2 minimumModalWindowSize = new(640f, 560f);
        private static readonly Vector2 initialModalWindowSize = new(680f, 620f);
        private IPickerWindowBridge bridge = null;

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
            window.minSize = minimumModalWindowSize;
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
            if (bridge == null)
            {
                rootVisualElement.Add
                (
                    new UnityEngine.UIElements.HelpBox
                    (
                        "에디터 상태가 초기화되었습니다. Picker를 다시 여세요.",
                        UnityEngine.UIElements.HelpBoxMessageType.Info
                    )
                );
                return;
            }

            rootVisualElement.Add(bridge.CreateView(Close));
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
