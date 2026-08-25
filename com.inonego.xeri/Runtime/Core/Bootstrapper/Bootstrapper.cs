/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : Bootstrapper.cs
수정일 : 2026-08-25

# 설명
플레이 시작 시 BootStrapper 설정에 따라 부트 씬을 로드하고 모듈을 초기화한 뒤 시작 씬으로 이동한다.
Editor에서는 BootStrapper 씬과 충돌하는 Play Mode 시작 씬 오버라이드를 해제한다.
모듈은 BootstrapperModuleAsset 에셋 목록을 순서대로 실행한다.
========================================================================= BLOCK_HEADER_END */

using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace inonego.Xeri.Bootstrapper
{
    // ============================================================
    /// <summary>
    /// BootStrapper 런타임 진입점.
    /// </summary>
    // ============================================================
    public class Bootstrapper : MonoBehaviour
    {

    #region 필드

    #if UNITY_EDITOR

        private static int initSceneIndex = -1;
        private static string initScenePath = null;

    #endif

    #endregion

    #region 에디터 플레이 진입

    #if UNITY_EDITOR

        // ----------------------------------------------------------------------
        /// <summary>
        /// Editor 로드 시 Play Mode 진입 상태 감시를 등록한다.
        /// </summary>
        // ----------------------------------------------------------------------
        [InitializeOnLoadMethod]
        private static void InitializeEditorPlayMode()
        {
            // 이미 남아 있는 충돌 상태를 즉시 정리하고 중복 구독 없이 감시를 유지한다.
            ClearBootstrapperPlayModeStartScene();
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// Play Mode 진입 직전에 BootStrapper와 충돌하는 시작 씬 오버라이드를 정리한다.
        /// </summary>
        // ----------------------------------------------------------------------
        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.ExitingEditMode) return;

            // Unity가 Play 시작 씬을 교체하기 전에 충돌하는 Editor 오버라이드를 제거한다.
            ClearBootstrapperPlayModeStartScene();
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// 설정된 BootStrapper 씬과 동일한 Play Mode 시작 씬 오버라이드를 해제한다.
        /// </summary>
        // ----------------------------------------------------------------------
        private static void ClearBootstrapperPlayModeStartScene()
        {
            var playModeStartScene = EditorSceneManager.playModeStartScene;
            if (playModeStartScene == null) return;

            var settings = BootstrapperSettings.Instance;
            if (settings == null || !settings.BootstrapperSceneIndex.HasValue) return;

            var scenes = EditorBuildSettings.scenes;
            int bootstrapperSceneIndex = settings.BootstrapperSceneIndex.Value;
            if (bootstrapperSceneIndex < 0 || bootstrapperSceneIndex >= scenes.Length) return;

            string playModeStartScenePath = AssetDatabase.GetAssetPath(playModeStartScene);
            string bootstrapperScenePath = scenes[bootstrapperSceneIndex].path;
            if (playModeStartScenePath != bootstrapperScenePath) return;

            EditorSceneManager.playModeStartScene = null;
        }

    #endif

    #endregion

    #region 부트스트랩

        // ----------------------------------------------------------------------
        /// <summary>
        /// 첫 씬 로드 전에 BootStrapper 씬으로 전환하고 실행 객체를 생성한다.
        /// </summary>
        // ----------------------------------------------------------------------
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void InitBootstrapper()
        {
            var settings = BootstrapperSettings.Instance;
            if (settings == null) return;
            if (!settings.BootstrapperSceneIndex.HasValue) return;

            var activeScene = SceneManager.GetActiveScene();

        #if UNITY_EDITOR
            initSceneIndex = activeScene.buildIndex;
            initScenePath = activeScene.path;
        #endif

            int bootstrapperSceneIndex = settings.BootstrapperSceneIndex.Value;

            if (activeScene.buildIndex != bootstrapperSceneIndex)
            {
                SceneManager.LoadScene(bootstrapperSceneIndex);
            }

            var go = new GameObject(nameof(Bootstrapper));
            go.AddComponent<Bootstrapper>();
        }

    #endregion

    #region 유니티 이벤트

        // ------------------------------------------------------------
        /// <summary>
        /// 모듈 초기화를 실행한 뒤 시작 씬으로 이동한다.
        /// </summary>
        // ------------------------------------------------------------
        private async void Awake()
        {
            var settings = BootstrapperSettings.Instance;

            if (settings != null)
            {
                await BootstrapperRunner.Init(settings.Modules);
            }

            await Awaitable.NextFrameAsync();

            LoadInitScene();
        }

    #endregion

    #region 메서드

        // ------------------------------------------------------------
        /// <summary>
        /// BootStrapper 초기화가 끝난 뒤 시작 씬을 로드한다.
        /// </summary>
        // ------------------------------------------------------------
        private void LoadInitScene()
        {
            var activeScene = SceneManager.GetActiveScene();

        #if UNITY_EDITOR

            if (!string.IsNullOrEmpty(initScenePath))
            {
                if (activeScene.buildIndex != initSceneIndex)
                {
                    var param = new LoadSceneParameters(LoadSceneMode.Single);
                    EditorSceneManager.LoadSceneInPlayMode(initScenePath, param);
                }

                initSceneIndex = -1;
                initScenePath = null;

                return;
            }

        #endif

            var settings = BootstrapperSettings.Instance;
            if (settings == null || !settings.SceneIndexToLoad.HasValue) return;

            int targetSceneIndex = settings.SceneIndexToLoad.Value;

            if (activeScene.buildIndex != targetSceneIndex)
            {
                SceneManager.LoadScene(targetSceneIndex);
            }
        }

    #endregion

    }
}
