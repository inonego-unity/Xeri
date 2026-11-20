/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : Bootstrapper.cs
수정일 : 2026-11-03

# 설명
Unity Play 시작 시 설정된 시스템을 한 번 자동 초기화하는 진입점이다.
ApplicationRuntime의 시작 단계에서 Initial Scene 전후 Module phase를 순차 실행한다.
최초 Application Scene의 load 완료와 active 상태를 검증한 뒤 AfterInitialScene Module 실행을 허용한다.
Editor에서는 Bootstrapper Scene과 충돌하는 Play Mode 시작 Scene 오버라이드를 해제한다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Threading;
using System.Threading.Tasks;

using UnityEngine;
using UnityEngine.SceneManagement;

using inonego;
using inonego.Xeri;
using inonego.Xeri.Application;

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

    #region 내부 데이터

        // ======================================================================
        /// <summary>
        /// Bootstrapper의 Scene 전후 phase를 Application 시작 계약으로 실행한다.
        /// </summary>
        // ======================================================================
        private sealed class BootstrapStartupModule : IApplicationModule
        {
            private readonly BootstrapperSettings settings;

            public BootstrapStartupModule(BootstrapperSettings settings)
            {
                this.settings = settings;
            }

            // ------------------------------------------------------------
            /// <summary>
            /// Module phase와 최초 Scene 전환을 하나의 시작 단계로 실행한다.
            /// </summary>
            // ------------------------------------------------------------
            public async Task StartAsync(ApplicationContext context, CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await BootstrapperRunner.RunPhase
                (
                    settings.Modules,
                    BootstrapperModulePhase.BeforeInitialScene,
                    context,
                    cancellationToken
                );

                cancellationToken.ThrowIfCancellationRequested();
                await Awaitable.NextFrameAsync(cancellationToken);
                await LoadInitialSceneAsync(settings, cancellationToken);

                cancellationToken.ThrowIfCancellationRequested();
                await BootstrapperRunner.RunPhase
                (
                    settings.Modules,
                    BootstrapperModulePhase.AfterInitialScene,
                    context,
                    cancellationToken
                );
                cancellationToken.ThrowIfCancellationRequested();
            }
        }

    #endregion

    #if UNITY_EDITOR

        private static string initScenePath = null;

    #endif

    #region 부트스트랩

        // ------------------------------------------------------------
        /// <summary>
        /// 첫 씬 로드 전에 Bootstrapper 씬으로 전환하고 실행 객체를 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void InitBootstrapper()
        {
            try
            {
                InitializeBootstrapper();
            }
            catch (Exception exception)
            {
                // 자동 진입 전 구성 오류도 시작 완료 계약에 실패로 반영한다.
                ApplicationHost.FailStartup(exception);
                Debug.LogException(exception);

                // 필수 Bootstrap 구성 오류 후 게임 Scene을 정상 실행하지 않는다.
            #if UNITY_EDITOR
                EditorApplication.ExitPlaymode();
            #else
                UnityEngine.Application.Quit(1);
            #endif
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 설정을 검증하고 Bootstrapper Scene 및 실행 객체를 준비한다.
        /// </summary>
        // ------------------------------------------------------------
        private static void InitializeBootstrapper()
        {
            var settings = BootstrapperSettings.Instance;
            if (settings == null ||
                !settings.BootstrapperSceneIndex.HasValue ||
                !settings.SceneIndexToLoad.HasValue)
            {
                throw new InvalidOperationException("Bootstrapper와 최초 Application Scene 설정이 필요합니다.");
            }

            int bootstrapperSceneIndex = settings.BootstrapperSceneIndex.Value;
            int initialSceneIndex = settings.SceneIndexToLoad.Value;
            int sceneCount = SceneManager.sceneCountInBuildSettings;
            if (bootstrapperSceneIndex < 0 || bootstrapperSceneIndex >= sceneCount ||
                initialSceneIndex < 0 || initialSceneIndex >= sceneCount)
            {
                throw new InvalidOperationException("Bootstrapper Scene 인덱스가 Build Settings 범위를 벗어났습니다.");
            }

            if (bootstrapperSceneIndex == initialSceneIndex)
            {
                throw new InvalidOperationException("Bootstrapper Scene과 최초 Application Scene은 달라야 합니다.");
            }

            var activeScene = SceneManager.GetActiveScene();

        #if UNITY_EDITOR
            // Bootstrapper Scene에서 직접 Play하면 설정된 최초 Application Scene을 사용한다.
            initScenePath = activeScene.buildIndex == bootstrapperSceneIndex ?
                null : activeScene.path;
        #endif

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

                // 이전 실행 세대가 정리된 뒤 Application 시작 경계를 통과한다.
                var modules = new IApplicationModule[1];
                modules[0] = new BootstrapStartupModule(settings);
                await ApplicationHost.StartAsync(modules);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);

                // 초기화가 실패한 Scene을 서비스 없는 상태로 계속 실행하지 않는다.
                if (UnityEngine.Application.isPlaying &&
                    ApplicationHost.Current?.State == ApplicationState.Failed)
                {
                    try
                    {
                        await RestoreBootstrapperSceneAsync();
                    }
                    catch (Exception restoreException)
                    {
                        Debug.LogException
                        (
                            new AggregateException
                            (
                                "Application 시작 실패 후 Bootstrapper Scene 복귀도 실패했습니다.",
                                exception, restoreException
                            ),
                            this
                        );
                    }
                }
            }
            finally
            {
                // Bootstrap 실행 객체는 startup phase가 끝난 뒤 persistent 수명에서 제거한다.
                Destroy(gameObject);
            }
        }

    #endregion

    #region 최초 Scene 로드

        // --------------------------------------------------------------------------------
        /// <summary>
        /// Bootstrapper 초기화 뒤 최초 Application Scene을 load하고 active 상태를 검증한다.
        /// </summary>
        // --------------------------------------------------------------------------------
        private static async Awaitable LoadInitialSceneAsync
        (
            BootstrapperSettings settings,
            CancellationToken cancellationToken
        )
        {
            cancellationToken.ThrowIfCancellationRequested();

            var activeScene = SceneManager.GetActiveScene();

        #if UNITY_EDITOR

            if (!string.IsNullOrEmpty(initScenePath))
            {
                var targetPath = initScenePath;

                if (activeScene.path != targetPath)
                {
                    var parameters = new LoadSceneParameters(LoadSceneMode.Single);
                    var operation = EditorSceneManager.LoadSceneAsyncInPlayMode(targetPath, parameters);
                    await WaitForSceneLoadAsync(operation, cancellationToken);
                }

                var initialScene = SceneManager.GetSceneByPath(targetPath);
                initScenePath = null;
                RequireLoadedActiveScene(initialScene);
                return;
            }

        #endif

            if (!settings.SceneIndexToLoad.HasValue)
            {
                throw new InvalidOperationException("최초 Application Scene index가 설정되지 않았습니다.");
            }

            var targetSceneIndex = settings.SceneIndexToLoad.Value;
            if (activeScene.buildIndex != targetSceneIndex)
            {
                var operation = SceneManager.LoadSceneAsync(targetSceneIndex, LoadSceneMode.Single);
                await WaitForSceneLoadAsync(operation, cancellationToken);
            }

            RequireLoadedActiveScene(SceneManager.GetSceneByBuildIndex(targetSceneIndex));
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// 초기화 실패 후 게임 Scene을 종료하고 Bootstrapper Scene으로 복귀한다.
        /// </summary>
        // ----------------------------------------------------------------------
        private static async Awaitable RestoreBootstrapperSceneAsync()
        {
            var settings = BootstrapperSettings.Instance ??
                throw new InvalidOperationException("Bootstrapper Settings가 없습니다.");

            if (!settings.BootstrapperSceneIndex.HasValue)
            {
                throw new InvalidOperationException("Bootstrapper Scene index가 설정되지 않았습니다.");
            }

            int index = settings.BootstrapperSceneIndex.Value;
            if (SceneManager.GetActiveScene().buildIndex != index)
            {
                var operation = SceneManager.LoadSceneAsync(index, LoadSceneMode.Single);
                await WaitForSceneLoadAsync(operation, CancellationToken.None);
            }

            RequireLoadedActiveScene(SceneManager.GetSceneByBuildIndex(index));
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// 지정 Scene load operation이 완료될 때까지 frame 경계에서 대기한다.
        /// </summary>
        // ----------------------------------------------------------------------
        private static async Awaitable WaitForSceneLoadAsync
        (
            AsyncOperation operation,
            CancellationToken cancellationToken
        )
        {
            if (operation == null)
            {
                throw new InvalidOperationException("최초 Application Scene load를 시작하지 못했습니다.");
            }

            // 이미 시작한 Scene 로드는 중단할 수 없으므로 완료를 관찰한 뒤 취소를 확정한다.
            while (!operation.isDone)
            {
                await Awaitable.NextFrameAsync();
            }

            cancellationToken.ThrowIfCancellationRequested();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 지정 Scene이 load 완료된 현재 active Scene인지 검증한다.
        /// </summary>
        // ------------------------------------------------------------
        private static void RequireLoadedActiveScene(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded || SceneManager.GetActiveScene() != scene)
            {
                throw new InvalidOperationException("최초 Application Scene이 load 완료된 active 상태가 아닙니다.");
            }
        }

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

            // Build Index는 비활성 Scene을 제외한 순서이므로 Editor 목록도 같은 기준으로 맞춘다.
            var scenes = Array.FindAll(EditorBuildSettings.scenes, scene => scene.enabled);
            int bootstrapperSceneIndex = settings.BootstrapperSceneIndex.Value;
            if (bootstrapperSceneIndex < 0 || bootstrapperSceneIndex >= scenes.Length) return;

            string playModeStartScenePath = AssetDatabase.GetAssetPath(playModeStartScene);
            string bootstrapperScenePath = scenes[bootstrapperSceneIndex].path;
            if (playModeStartScenePath != bootstrapperScenePath) return;

            EditorSceneManager.playModeStartScene = null;
        }

    #endif

    #endregion

    }
}
