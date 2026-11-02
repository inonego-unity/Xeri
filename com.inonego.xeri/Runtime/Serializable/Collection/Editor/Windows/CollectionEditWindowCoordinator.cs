/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : CollectionEditWindowCoordinator.cs
수정일 : 2026-10-29

# 설명
Collection Key/Order와 Value 편집창을 대상과 무관하게 한 개씩 재사용한다.
편집 모드를 전환하면 다른 종류의 창을 닫아 동시에 하나의 편집 팝업만 남긴다.
========================================================================= BLOCK_HEADER_END */

using UnityEditor;
using UnityEngine;

namespace inonego.Xeri.Serializable.Editor
{
    // ============================================================
    /// <summary>
    /// Collection 상세 편집 팝업의 재사용과 모드 간 전환을 조정한다.
    /// </summary>
    // ============================================================
    internal static class CollectionEditWindowCoordinator
    {

    #region 메서드

        // ------------------------------------------------------------
        /// <summary>
        /// 열려 있는 같은 종류의 편집창을 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public static T Find<T>() where T : EditorWindow
        {
            T found = null;
            foreach (var window in Resources.FindObjectsOfTypeAll<T>())
            {
                // 닫힌 EditorWindow의 managed 객체는 아직 Resources에 남을 수 있다.
                // 실제 패널에 연결된 창만 재사용 대상으로 취급한다.
                if (window.rootVisualElement.panel == null)
                {
                    window.Close();
                    continue;
                }

                if (found == null)
                {
                    found = window;
                }
                else
                {
                    window.Close();
                }
            }

            return found;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 새 팝업을 쌓지 않고 같은 종류의 창을 가져온다.
        /// </summary>
        // ------------------------------------------------------------
        public static T Acquire<T>() where T : EditorWindow
        {
            return Find<T>() ?? ScriptableObject.CreateInstance<T>();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Key/Order와 Value 중 현재 모드의 창 하나만 표시한다.
        /// </summary>
        // ------------------------------------------------------------
        public static void Activate<T>() where T : EditorWindow
        {
            if (typeof(T) != typeof(CollectionStructuralEditorWindow))
            {
                Find<CollectionStructuralEditorWindow>()?.Close();
            }

            if (typeof(T) != typeof(CollectionValueEditorWindow))
            {
                Find<CollectionValueEditorWindow>()?.Close();
            }
        }

    #endregion

    }
}
