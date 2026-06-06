/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : PickerWindow.cs
수정일 : 2026-06-03

# 설명
PickerView 목업을 Unity EditorWindow 모달 창에서 확인하기 위한 최소 shell.
실제 picker 선택 로직은 PickerView의 더블클릭 이벤트를 로그로 연결하는 수준만 구현한다.
========================================================================= BLOCK_HEADER_END */

using UnityEditor;

using UnityEngine;

namespace inonego.Xeri.Editor.Picker
{
    // ============================================================
    /// <summary>
    /// Picker mockup editor window.
    /// </summary>
    // ============================================================
    public sealed class PickerMockupWindow : EditorWindow
    {
        #region 메뉴

        // ------------------------------------------------------------
        /// <summary>
        /// Opens picker mockup window.
        /// </summary>
        // ------------------------------------------------------------
        [MenuItem("Tools/Unixeri/선택 목업")]
        public static void Open()
        {
            var window = CreateInstance<PickerMockupWindow>();
            window.titleContent = new GUIContent("선택 목업");
            window.minSize = new Vector2(640f, 580f);
            window.position = new Rect(100f, 100f, 640f, 620f);
            window.ShowModalUtility();
        }

        #endregion

        #region Unity 이벤트

        private void CreateGUI()
        {
            rootVisualElement.Clear();

            var view = new PickerMockupView();
            view.OnEntryConfirmed += entryID =>
            {
                Debug.Log($"[선택 목업] 선택됨: {entryID}");
            };

            rootVisualElement.Add(view);
        }

        #endregion
    }
}
