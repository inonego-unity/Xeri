/* BLOCK_HEADER_BEGIN =======================================================================
파일명: BuildProfileWindowExtender.cs
수정일: 2026-05-20

# 설명
Unity Build Profile 창의 Scene List 영역에 BootStrapper 설정 버튼과 상태 표시를 추가한다.
BootStrapper 씬 생성, Build Settings 등록, 부트 씬/시작 씬 인덱스 저장을 지원한다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;

using UnityEngine;
using UnityEngine.UIElements;

using UnityEditor;
using UnityEditor.UIElements;
using UnityEditor.SceneManagement;

namespace inonego.Xeri.Bootstrapper.Editor
{
    using Serializable;

    // ============================================================
    /// <summary>
    /// Build Profile 창의 Scene List에 BootStrapper 제어 UI를 추가한다.
    /// </summary>
    // ============================================================
    [InitializeOnLoad]
    public static class BuildProfileWindowExtender
    {

    #region 상수

        private const string WINDOW_TYPE_NAME = "UnityEditor.Build.Profile.BuildProfileWindow";
        private const string EXTENSION_NAME = "bootstrapper-header-ext";
        private const string STATUS_NAME = "status";
        private const string UXML_FILE_NAME = "BuildProfileWindowExtender.uxml";
        private const string BOOTSTRAPPER_SCENE_PATH = "Assets/Bootstrapper.unity";
        private const string SETTINGS_MENU_PATH = "Project/Bootstrapper";

    #endregion

    #region 스타일 상수

        private static readonly Color HEADER_BACKGROUND_COLOR = new(51f / 255f, 51f / 255f, 51f / 255f, 0.8f);
        private static readonly Color HEADER_BORDER_COLOR = new(128f / 255f, 128f / 255f, 128f / 255f, 1f);
        private static readonly Color STATUS_COLOR = Color.yellow;

        private const float HEADER_PADDING_VERTICAL = 6f;
        private const float HEADER_PADDING_HORIZONTAL = 10f;
        private const float HEADER_MARGIN_BOTTOM = 12f;
        private const float HEADER_BORDER_BOTTOM_WIDTH = 1f;
        private const float STATUS_FONT_SIZE = 10f;
        private const float BUTTON_HEIGHT = 18f;
        private const float BUTTON_FONT_SIZE = 10f;
        private const float SCENE_LIST_HORIZONTAL_MARGIN = 6f;

    #endregion

    #region 필드

        private static bool isWatching;

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// Build Profile 창 감시와 플레이 모드 상태 변경 콜백을 등록한다.
        /// </summary>
        // ------------------------------------------------------------
        static BuildProfileWindowExtender()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;

            if (!EditorApplication.isPlayingOrWillChangePlaymode)
            {
                StartWatching();
            }
        }

    #endregion

    #region 감시

        // ------------------------------------------------------------
        /// <summary>
        /// Build Profile 창 감시를 시작한다.
        /// </summary>
        // ------------------------------------------------------------
        private static void StartWatching()
        {
            if (isWatching) return;

            EditorApplication.update += OnUpdate;
            isWatching = true;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Build Profile 창 감시를 중지한다.
        /// </summary>
        // ------------------------------------------------------------
        private static void StopWatching()
        {
            if (!isWatching) return;

            EditorApplication.update -= OnUpdate;
            isWatching = false;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 플레이 모드 진입 중에는 Build Profile 창 감시를 중지한다.
        /// </summary>
        // ------------------------------------------------------------
        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode)
            {
                StopWatching();
                return;
            }

            if (state == PlayModeStateChange.EnteredEditMode)
            {
                StartWatching();
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 주입된 UI가 패널에서 제거되면 Build Profile 창 감시를 다시 시작한다.
        /// </summary>
        // ------------------------------------------------------------
        private static void OnExtensionDetached(DetachFromPanelEvent evt)
        {
            if (!EditorApplication.isPlayingOrWillChangePlaymode)
            {
                StartWatching();
            }
        }

    #endregion

    #region 갱신

        // ------------------------------------------------------------
        /// <summary>
        /// 열린 Build Profile 창을 찾아 BootStrapper UI를 주입한다.
        /// </summary>
        // ------------------------------------------------------------
        private static void OnUpdate()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                StopWatching();
                return;
            }

            bool IsProfileWindow(EditorWindow window)
            {
                return window.GetType().FullName == WINDOW_TYPE_NAME;
            }

            var windows = Resources.FindObjectsOfTypeAll<EditorWindow>();
            var profileWindow = windows.FirstOrDefault(IsProfileWindow);

            if (profileWindow != null)
            {
                InjectToSceneList(profileWindow);
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Build Profile 창의 Scene List 영역에 확장 UI를 삽입한다.
        /// </summary>
        // ------------------------------------------------------------
        private static void InjectToSceneList(EditorWindow window)
        {
            var root = window.rootVisualElement;

            var container = root.Q("scene-list-foldout-root") ?? root.Q("scene-list-foldout")?.Q("unity-content");
            if (container == null) return;

            ApplySceneListMargin(container);

            var existing = container.Q(EXTENSION_NAME);
            if (existing != null)
            {
                existing.UnregisterCallback<DetachFromPanelEvent>(OnExtensionDetached);
                existing.RegisterCallback<DetachFromPanelEvent>(OnExtensionDetached);

                ApplyExtensionStyle(existing);
                UpdateStatus(existing.Q<Label>(STATUS_NAME));
                StopWatching();
                return;
            }

            var extension = CreateExtensionElement(window);
            if (extension == null) return;

            extension.RegisterCallback<DetachFromPanelEvent>(OnExtensionDetached);

            container.Insert(0, extension);
            UpdateStatus(extension.Q<Label>(STATUS_NAME));
            StopWatching();
        }

    #endregion

    #region UI 생성

        // ------------------------------------------------------------
        /// <summary>
        /// Build Profile 창에 삽입할 BootStrapper 확장 UI를 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        private static VisualElement CreateExtensionElement(EditorWindow window)
        {
            var visualTree = LoadVisualTree();
            if (visualTree == null) return null;

            var root = visualTree.CloneTree().Q(EXTENSION_NAME);
            if (root == null) return null;

            var statusLabel = root.Q<Label>(STATUS_NAME);

            ApplyExtensionStyle(root);

            root.Q<Button>("btn-create").clicked      += () => CreateAndRegisterBootstrapper(window);
            root.Q<Button>("btn-select-boot").clicked += () => ShowSceneSelectionMenu(statusLabel, true);
            root.Q<Button>("btn-select-init").clicked += () => ShowSceneSelectionMenu(statusLabel, false);
            root.Q<Button>("btn-config").clicked      += () => SettingsService.OpenProjectSettings(SETTINGS_MENU_PATH);

            return root;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// BootStrapper 헤더 스타일을 적용한다.
        /// </summary>
        // ------------------------------------------------------------
        private static void ApplyExtensionStyle(VisualElement root)
        {
            root.style.backgroundColor      = HEADER_BACKGROUND_COLOR;
            root.style.paddingTop           = HEADER_PADDING_VERTICAL;
            root.style.paddingBottom        = HEADER_PADDING_VERTICAL;
            root.style.paddingLeft          = HEADER_PADDING_HORIZONTAL;
            root.style.paddingRight         = HEADER_PADDING_HORIZONTAL;
            root.style.marginBottom         = HEADER_MARGIN_BOTTOM;
            root.style.borderBottomWidth    = HEADER_BORDER_BOTTOM_WIDTH;
            root.style.borderBottomColor    = HEADER_BORDER_COLOR;
            root.style.flexDirection        = FlexDirection.Row;
            root.style.alignItems           = Align.Center;
            root.style.justifyContent       = Justify.SpaceBetween;

            var status = root.Q<Label>(STATUS_NAME);
            if (status != null)
            {
                ApplyStatusStyle(status);
            }

            root.Query<Button>().ForEach(ApplyButtonStyle);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// BootStrapper 상태 라벨 스타일을 적용한다.
        /// </summary>
        // ------------------------------------------------------------
        private static void ApplyStatusStyle(Label label)
        {
            label.style.flexGrow    = 1f;
            label.style.flexShrink  = 1f;
            label.style.color       = STATUS_COLOR;
            label.style.fontSize    = STATUS_FONT_SIZE;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// BootStrapper 버튼 스타일을 적용한다.
        /// </summary>
        // ------------------------------------------------------------
        private static void ApplyButtonStyle(Button button)
        {
            button.style.height     = BUTTON_HEIGHT;
            button.style.fontSize   = BUTTON_FONT_SIZE;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Build Profile 기본 씬 목록 영역에 좌우 여백을 적용한다.
        /// </summary>
        // ------------------------------------------------------------
        private static void ApplySceneListMargin(VisualElement container)
        {
            var sceneList = container.Children().OfType<IMGUIContainer>().FirstOrDefault();
            if (sceneList == null) return;

            sceneList.style.marginLeft = SCENE_LIST_HORIZONTAL_MARGIN;
            sceneList.style.marginRight = SCENE_LIST_HORIZONTAL_MARGIN;
        }

    #endregion

    #region 리소스

        // ------------------------------------------------------------
        /// <summary>
        /// Build Profile 확장 UXML을 로드한다.
        /// </summary>
        // ------------------------------------------------------------
        private static VisualTreeAsset LoadVisualTree()
        {
            var type = typeof(BuildProfileWindowExtender);
            var dir = EditorAssetHelper.GetScriptDirectory(type);
            if (string.IsNullOrEmpty(dir)) return null;

            return AssetDatabase.LoadAssetAtPath<VisualTreeAsset>($"{dir}/{UXML_FILE_NAME}");
        }

    #endregion

    #region 씬 설정

        // ------------------------------------------------------------
        /// <summary>
        /// BootStrapper 씬을 생성하고 Build Settings 첫 번째 항목으로 등록한다.
        /// </summary>
        // ------------------------------------------------------------
        private static void CreateAndRegisterBootstrapper(EditorWindow window)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("[Bootstrapper] Bootstrapper scene cannot be created or registered during play mode.");
                return;
            }

            var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(BOOTSTRAPPER_SCENE_PATH);
            if (sceneAsset == null)
            {
                var newScene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Additive);

                EditorSceneManager.SaveScene(newScene, BOOTSTRAPPER_SCENE_PATH);
                EditorSceneManager.CloseScene(newScene, true);

                AssetDatabase.ImportAsset(BOOTSTRAPPER_SCENE_PATH);
            }

            var scenes = new List<EditorBuildSettingsScene>
            {
                new(BOOTSTRAPPER_SCENE_PATH, true)
            };

            foreach (var scene in EditorBuildSettings.scenes)
            {
                if (scene.path == BOOTSTRAPPER_SCENE_PATH) continue;

                scenes.Add(scene);
            }

            EditorBuildSettings.scenes = scenes.ToArray();

            SaveSceneIndex(0, true);

            Debug.Log("[Bootstrapper] Registered Bootstrapper scene at index 0.");

            window.Repaint();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Build Settings 씬 목록을 메뉴로 표시하고 선택한 씬 인덱스를 저장한다.
        /// </summary>
        // ------------------------------------------------------------
        private static void ShowSceneSelectionMenu(Label statusLabel, bool isBootstrapper)
        {
            var scenes = EditorBuildSettings.scenes;
            var menu = new GenericMenu();

            string clearLabel = isBootstrapper ? "Clear Bootstrapper Scene" : "Clear Init Scene";

            menu.AddItem(new GUIContent(clearLabel), false, () =>
            {
                ClearSceneIndex(isBootstrapper);
                UpdateStatus(statusLabel);
            });

            if (scenes == null || scenes.Length == 0)
            {
                menu.ShowAsContext();
                return;
            }

            menu.AddSeparator("");

            for (int i = 0; i < scenes.Length; i++)
            {
                int index = i;
                string sceneName = Path.GetFileNameWithoutExtension(scenes[index].path);

                void OnClick()
                {
                    SaveSceneIndex(index, isBootstrapper);
                    UpdateStatus(statusLabel);
                }

                menu.AddItem(new GUIContent($"[{index}] {sceneName}"), false, OnClick);
            }

            menu.ShowAsContext();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 선택한 BootStrapper 또는 Init 씬 인덱스를 제거한다.
        /// </summary>
        // ------------------------------------------------------------
        private static void ClearSceneIndex(bool isBootstrapper)
        {
            var settings = BootstrapperSettings.CreateAsset();

            if (isBootstrapper)
            {
                settings.BootstrapperSceneIndex = null;
            }
            else
            {
                settings.SceneIndexToLoad = null;
            }

            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 선택한 씬 인덱스를 BootStrapper 설정 에셋에 저장한다.
        /// </summary>
        // ------------------------------------------------------------
        private static void SaveSceneIndex(int sceneIndex, bool isBootstrapper)
        {
            var settings = BootstrapperSettings.CreateAsset();

            if (isBootstrapper)
            {
                settings.BootstrapperSceneIndex = new XNullable<int>(sceneIndex);
            }
            else
            {
                settings.SceneIndexToLoad = new XNullable<int>(sceneIndex);
            }

            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
        }

    #endregion

    #region 상태

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 BootStrapper 씬 설정 상태를 라벨에 표시한다.
        /// </summary>
        // ------------------------------------------------------------
        private static void UpdateStatus(Label label)
        {
            if (label == null) return;

            var settings = BootstrapperSettings.Instance;

            if (settings == null)
            {
                label.text = "N/A";
                return;
            }

            var scenes = EditorBuildSettings.scenes;

            var bootIndex = settings.BootstrapperSceneIndex;
            var initIndex = settings.SceneIndexToLoad;

            string bootText = GetSceneNameWithIndex(scenes, bootIndex);
            string initText = GetSceneNameWithIndex(scenes, initIndex);

            if (!bootIndex.HasValue && !initIndex.HasValue)
            {
                label.text = "No Scene Selected";
                return;
            }

            label.text = bootIndex == initIndex
                ? $"Error: Same Index ({bootText} / {initText})"
                : $"Boot: {bootText} -> Init: {initText}";
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 씬 인덱스를 Build Settings 씬 이름과 함께 표시한다.
        /// </summary>
        // ------------------------------------------------------------
        private static string GetSceneNameWithIndex(EditorBuildSettingsScene[] scenes, XNullable<int> nullableIndex)
        {
            if (!nullableIndex.HasValue) return "N/A";

            int index = nullableIndex.Value;
            if (scenes.CheckInRange(index))
            {
                return $"[{index}] {Path.GetFileNameWithoutExtension(scenes[index].path)}";
            }

            return "Invalid Index";
        }

    #endregion

    }
}
