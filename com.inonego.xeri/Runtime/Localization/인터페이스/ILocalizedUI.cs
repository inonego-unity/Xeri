/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : ILocalizedUI.cs
수정일 : 2026-05-09

# 설명
다국어 지원 UI 의 갱신 계약 + 일괄 reload 정적 메서드.
LangCode 변경 시 모든 씬의 MonoBehaviour 를 순회해 ILocalizedUI 구현체에 ReloadLocalizedUI() 호출.

# 특이사항
씬 순회 비용은 O(scene) 이지만 LangCode 변경은 사용자 트리거 1회 발생이라 수용 가능.
복잡 커스텀 UI 의 사용자 보일러플레이트 최소화 (이벤트 구독/해제 4줄 vs ReloadLocalizedUI 1메서드) 가 큰 이점.
UI Toolkit 측은 이 인터페이스 대신 LocalizationBinding 의 OnLangCodeChange 구독 모델 사용.
========================================================================= BLOCK_HEADER_END */

using UnityEngine;
using UnityEngine.SceneManagement;

namespace inonego.Xeri.Localization
{
    // ============================================================
    /// <summary>
    /// 다국어 지원 UI 의 갱신 계약.
    /// </summary>
    // ============================================================
    public interface ILocalizedUI
    {
        // ------------------------------------------------------------
        /// <summary>
        /// 현재 언어로 UI 텍스트를 갱신한다.
        /// </summary>
        // ------------------------------------------------------------
        void ReloadLocalizedUI();

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> 모든 씬의 MonoBehaviour 를 순회하여 ILocalizedUI 구현체에 ReloadLocalizedUI 를 호출한다.
        /// <br/> root 가 null 이면 모든 활성 씬의 RootGameObjects 를 대상으로 한다.
        /// </summary>
        // ----------------------------------------------------------------------
        public static void ReloadLocalizedUIAll(GameObject root = null, bool includeInactive = true)
        {
            if (root == null)
            {
                int sceneCount = SceneManager.sceneCount;

                for (int i = 0; i < sceneCount; i++)
                {
                    var scene = SceneManager.GetSceneAt(i);
                    if (!scene.IsValid() || !scene.isLoaded) continue;

                    var rootGameObjects = scene.GetRootGameObjects();

                    foreach (var rootGameObject in rootGameObjects)
                    {
                        if (rootGameObject == null) continue;

                        ReloadLocalizedUIAll(rootGameObject, includeInactive);
                    }
                }

                return;
            }

            // root 의 자식 MonoBehaviour 중 ILocalizedUI 구현체에 reload 호출.
            var monoBehaviours = root.GetComponentsInChildren<MonoBehaviour>(includeInactive);

            foreach (var monoBehaviour in monoBehaviours)
            {
                if (monoBehaviour is ILocalizedUI localizedUI)
                {
                    localizedUI.ReloadLocalizedUI();
                }
            }
        }
    }
}
