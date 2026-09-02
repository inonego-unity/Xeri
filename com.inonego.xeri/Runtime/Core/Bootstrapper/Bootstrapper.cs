/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : Bootstrapper.cs
수정일 : 2026-09-02

# 설명
플레이 시작 시 Bootstrapper 설정에 따라 Initial Scene 전후 Module phase를 순차 실행한다.
최초 Application Scene의 load 완료와 active 상태를 검증한 뒤 AfterInitialScene Module 실행을 허용한다.
Editor에서는 Bootstrapper Scene과 충돌하는 Play Mode 시작 Scene 오버라이드를 해제한다.
========================================================================= BLOCK_HEADER_END */

using System;

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
    /// Bootstrapper 런타임 진입점.
    /// </summary>
    // ============================================================
    public class Bootstrapper : MonoBehaviour
    {

    #region 필드

    #if UNITY_EDITOR

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
        /// Play Mode 진입 직전에 Bootstrapper와 충돌하는 시작 씬 오버라이드를 정리한다.
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
        /// 설정된 Bootstrapper 씬과 동일한 Play Mode 시작 씬 오버라이드를 해제한다.
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
        /// 첫 씬 로드 전에 Bootstrapper 씬으로 전환하고 실행 객체를 생성한다.
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
            initScenePath = activeScene.path;
        #endif

            int bootstrapperSceneIndex = settings.BootstrapperSceneIndex.Value;

            if (activeScene.buildIndex != bootstrapperSceneIndex)
            {
                SceneManager.LoadScene(bootstrapperSceneIndex);
            }

            var go = new GameObject(nameof(Bootstrapper));

            // Component Awake가 시작되기 전에 AfterInitialScene phase까지 유지할 persistent ownership을 확보한다.
            DontDestroyOnLoad(go);
            go.AddComponent<Bootstrapper>();
        }

    #endregion

    #region 유니티 이벤트

        // ----------------------------------------------------------------------
        /// <summary>
        /// Initial Scene 전후 Module phase를 Scene load 경계에 맞춰 순차 실행한다.
        /// </summary>
        // ----------------------------------------------------------------------
        private async void Awake()
        {
            try
            {
                var settings = BootstrapperSettings.Instance ??
                    throw new InvalidOperationException("Bootstrapper Settings가 없습니다.");

                // Scene 비의존 서비스가 먼저 자신의 application 수명을 준비한다.
                await BootstrapperRunner.RunPhase
                (
                    settings.Modules,
                    BootstrapperModulePhase.BeforeInitialScene
                );

                // Pre-Scene Module이 생성한 Host의 Awake/Start 준비를 보장한 뒤 최초 Scene을 교체한다.
                await Awaitable.NextFrameAsync();
                await LoadInitialSceneAsync();

                // 최초 active Scene 확정 이후에 실행해야 하는 post-scene Module을 시작한다.
                await BootstrapperRunner.RunPhase
                (
                    settings.Modules,
                    BootstrapperModulePhase.AfterInitialScene
                );
            }
            finally
            {
                // Bootstrap 실행 객체는 startup phase가 끝난 뒤 persistent 수명에서 제거한다.
                Destroy(gameObject);
            }
        }

    #endregion

    #region 최초 Scene 로드

        // ----------------------------------------------------------------------
        /// <summary>
        /// Bootstrapper 초기화 뒤 최초 Application Scene을 load하고 active 상태를 검증한다.
        /// </summary>
        // ----------------------------------------------------------------------
        private static async Awaitable LoadInitialSceneAsync()
        {
            var activeScene = SceneManager.GetActiveScene();

        #if UNITY_EDITOR

            if (!string.IsNullOrEmpty(initScenePath))
            {
                var targetPath = initScenePath;

                if (activeScene.path != targetPath)
                {
                    var parameters = new LoadSceneParameters(LoadSceneMode.Single);
                    var operation = EditorSceneManager.LoadSceneAsyncInPlayMode(targetPath, parameters);
                    await WaitForSceneLoadAsync(operation);
                }

                var initialScene = SceneManager.GetSceneByPath(targetPath);
                initScenePath = null;
                RequireLoadedActiveScene(initialScene);
                return;
            }

        #endif

            var settings = BootstrapperSettings.Instance ??
                throw new InvalidOperationException("Bootstrapper Settings가 없습니다.");
            if (!settings.SceneIndexToLoad.HasValue)
            {
                throw new InvalidOperationException("최초 Application Scene index가 설정되지 않았습니다.");
            }

            var targetSceneIndex = settings.SceneIndexToLoad.Value;
            if (activeScene.buildIndex != targetSceneIndex)
            {
                var operation = SceneManager.LoadSceneAsync(targetSceneIndex, LoadSceneMode.Single);
                await WaitForSceneLoadAsync(operation);
            }

            RequireLoadedActiveScene(SceneManager.GetSceneByBuildIndex(targetSceneIndex));
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 지정 Scene load operation이 완료될 때까지 frame 경계에서 대기한다.
        /// </summary>
        // ------------------------------------------------------------
        private static async Awaitable WaitForSceneLoadAsync(AsyncOperation operation)
        {
            if (operation == null)
            {
                throw new InvalidOperationException("최초 Application Scene load를 시작하지 못했습니다.");
            }

            while (!operation.isDone)
            {
                await Awaitable.NextFrameAsync();
            }
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// 지정 Scene이 load 완료된 현재 active Scene인지 검증한다.
        /// </summary>
        // ----------------------------------------------------------------------
        private static void RequireLoadedActiveScene(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded || SceneManager.GetActiveScene() != scene)
            {
                throw new InvalidOperationException("최초 Application Scene이 load 완료된 active 상태가 아닙니다.");
            }
        }

    #endregion

    }
}
