/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : GameUIRuntime.cs
수정일 : 2026-07-30

# 설명
App 단위 Game UI 서비스, Scene 구성 검증, 기본·추가 Profile과 Host backend의 생성·역순 해제를 소유한다.
Shutdown은 논리 소유권을 한 번씩 정리하고, 소유권 유지가 명시된 Provider 반환 실패만 후속 Shutdown까지 보존한다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;

namespace inonego.Xeri.UI.Game
{
    // ============================================================
    /// <summary>
    /// 게임 UI Runtime의 명시적 Composition Root.
    /// </summary>
    // ============================================================
    public sealed class GameUIRuntime : MonoBehaviour
    {
    #region 필드

        // ------------------------------------------------------------
        /// <summary>
        /// Runtime 초기화가 완료됐는지 여부.
        /// </summary>
        // ------------------------------------------------------------
        public bool IsInitialized { get; private set; }

        // ------------------------------------------------------------
        /// <summary>
        /// Runtime이 해제 중인지 여부.
        /// </summary>
        // ------------------------------------------------------------
        public bool IsReleasing { get; private set; }

        // ------------------------------------------------------------
        /// <summary>
        /// Runtime Core 해제가 완료됐는지 여부.
        /// </summary>
        // ------------------------------------------------------------
        public bool IsReleased { get; private set; }

        // ------------------------------------------------------------
        /// <summary>
        /// Runtime이 사용하는 Settings.
        /// </summary>
        // ------------------------------------------------------------
        public GameUISettingsAsset Settings { get; private set; }

        // ------------------------------------------------------------
        /// <summary>
        /// Presentation Layer 등록과 소비자 수명 Registry.
        /// </summary>
        // ------------------------------------------------------------
        public PresentationLayerRegistry LayerRegistry { get; private set; }

        // ------------------------------------------------------------
        /// <summary>
        /// 동적 Screen 등록 Registry.
        /// </summary>
        // ------------------------------------------------------------
        public ScreenRegistry ScreenRegistry { get; private set; }

        // ------------------------------------------------------------
        /// <summary>
        /// Screen 명령과 Stack 수명 Controller.
        /// </summary>
        // ------------------------------------------------------------
        public ScreenController Screens { get; private set; }

        // ------------------------------------------------------------
        /// <summary>
        /// Screen Focus 기록과 복원 Controller.
        /// </summary>
        // ------------------------------------------------------------
        public FocusController Focus { get; private set; }

        // ------------------------------------------------------------
        /// <summary>
        /// App 기본 Profile Layer를 사용하는 Scene Fade 서비스.
        /// </summary>
        // ------------------------------------------------------------
        public SceneFader SceneFader { get; private set; }

        // ------------------------------------------------------------
        /// <summary>
        /// Visibility Override Controller.
        /// </summary>
        // ------------------------------------------------------------
        public VisibilityController Visibility { get; private set; }

        // ------------------------------------------------------------
        /// <summary>
        /// Modal Stack Controller.
        /// </summary>
        // ------------------------------------------------------------
        public ModalController Modals { get; private set; }

        // ------------------------------------------------------------
        /// <summary>
        /// 여러 실제 UI 대상을 표시하는 Focus Highlight Controller.
        /// </summary>
        // ------------------------------------------------------------
        public FocusHighlightController FocusHighlights { get; private set; }

        // ------------------------------------------------------------
        /// <summary>
        /// World UI Projection 계산 Controller.
        /// </summary>
        // ------------------------------------------------------------
        public ProjectionController Projection { get; private set; }

        // ------------------------------------------------------------
        /// <summary>
        /// Safe Area 기준 Placement 계산 Controller.
        /// </summary>
        // ------------------------------------------------------------
        public PlacementController Placement { get; private set; }

        // ------------------------------------------------------------
        /// <summary>
        /// Drag Visual 재배치 Controller.
        /// </summary>
        // ------------------------------------------------------------
        public DragVisualController DragVisuals { get; private set; }

        // ------------------------------------------------------------
        /// <summary>
        /// Host Safe Area 갱신 Controller.
        /// </summary>
        // ------------------------------------------------------------
        public UGUILayoutController Layout => layoutController;

        // ------------------------------------------------------------
        /// <summary>
        /// 마지막으로 UI 입력을 수행한 장치.
        /// </summary>
        // ------------------------------------------------------------
        public InputDevice LastInputDevice
        {
            get
            {
                ThrowIfUnavailable();
                return inputDriver.LastInputDevice;
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Settings의 기본 Scene Fade 실행 인자.
        /// </summary>
        // ------------------------------------------------------------
        public SceneFadeParams DefaultSceneFadeParams
        {
            get
            {
                ThrowIfUnavailable();
                return new SceneFadeParams(Settings.DefaultFadeColor, Settings.DefaultFadeDuration);
            }
        }

        [SerializeField]
        private GameUISettingsAsset directSettings = null;

        [SerializeField]
        private Transform layerRoot = null;

        [SerializeField]
        private EventSystem eventSystem = null;

        [SerializeField]
        private InputSystemUIInputModule inputModule = null;

        [SerializeField]
        private UGUILayoutController layoutController = null;

        [SerializeField]
        private UGUIFocusDriver focusDriver = null;

        [SerializeField]
        private InputSystemScreenInputDriver inputDriver = null;

        private readonly List<GameUIProfileHandle> profileHandles =
            new List<GameUIProfileHandle>();

        private DOTweenPresentationTransitioner transitioner = null;
        private GameObjectProviderOverlaySource<ISceneFadeDriver> sceneFadeSource = null;
        private GameUIProfileHandle defaultProfile = null;
        private bool sceneLoadedSubscribed = false;

    #endregion

    #region 이벤트

        // ------------------------------------------------------------
        /// <summary>
        /// Core와 기본 Profile 조립이 완료된 뒤 발생한다.
        /// </summary>
        // ------------------------------------------------------------
        public event Action<GameUIRuntime> OnInitialized = null;

        // ------------------------------------------------------------
        /// <summary>
        /// 모든 Screen Source 반환 뒤 프로젝트 소유 리소스 해제 직전에 발생한다.
        /// </summary>
        // ------------------------------------------------------------
        public event Action<GameUIRuntime> OnReleasing = null;

        // ------------------------------------------------------------
        /// <summary>
        /// 마지막 UI 입력 장치가 바뀌었을 때 발생한다.
        /// </summary>
        // ------------------------------------------------------------
        public event Action<InputDevice> OnLastInputDeviceChanged
        {
            add
            {
                ThrowIfUnavailable();
                inputDriver.OnLastInputDeviceChanged += value;
            }
            remove
            {
                if (inputDriver != null)
                {
                    inputDriver.OnLastInputDeviceChanged -= value;
                }
            }
        }

    #endregion

    #region Unity 이벤트

        // ------------------------------------------------------------
        /// <summary>
        /// Bootstrapper를 사용하지 않는 직접 배치 Host를 한 번 초기화한다.
        /// </summary>
        // ------------------------------------------------------------
        private void Start()
        {
            if (directSettings == null || IsInitialized || IsReleasing || IsReleased) return;

            try
            {
                Initialize(directSettings);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);

                // 실패한 직접 배치 Host가 다음 정상 Runtime과 EventSystem 구성을 막지 않게 제거한다.
                gameObject.SetActive(false);
                Destroy(gameObject);
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Application 종료 fallback 여부를 기록하고 Runtime을 정리한다.
        /// </summary>
        // ------------------------------------------------------------
        private void OnApplicationQuit()
        {
            ShutdownFallback();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Host 비활성화 시 backend Component 파괴 순서보다 먼저 Runtime 소유권을 정리한다.
        /// </summary>
        // ------------------------------------------------------------
        private void OnDisable()
        {
            if (!Application.isPlaying || IsReleased) return;

            ShutdownFallback();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 명시적 Shutdown이 누락된 Host 파괴에서 같은 종료 경로를 실행한다.
        /// </summary>
        // ------------------------------------------------------------
        private void OnDestroy()
        {
            if (!IsReleased)
            {
                ShutdownFallback();
            }
        }

    #endregion

    #region 초기화

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> Host 참조와 Settings를 검증하고 Core 서비스와 기본 Profile을 조립한다.
        /// <br/> 구독자 실패를 포함한 초기화 실패는 생성된 소유 리소스를 역순 롤백한다.
        /// </summary>
        // ----------------------------------------------------------------------
        public void Initialize(GameUISettingsAsset settings)
        {
            if (IsInitialized)
            {
                throw new InvalidOperationException("Game UI Runtime이 이미 초기화됐습니다.");
            }

            if (IsReleasing || IsReleased)
            {
                throw new InvalidOperationException("해제 중이거나 해제된 Game UI Runtime은 초기화할 수 없습니다.");
            }

            if (settings != null && directSettings != null && directSettings != settings)
            {
                throw new InvalidOperationException
                (
                    "Host의 직접 배치 Settings와 Initialize에 전달된 Settings가 서로 다릅니다."
                );
            }

            var coreReady = false;

            try
            {
                ValidateHost(settings);
                Settings = settings;

                LayerRegistry = new PresentationLayerRegistry();
                transitioner = new DOTweenPresentationTransitioner();
                inputDriver.Initialize(inputModule, settings);
                Focus = new FocusController(focusDriver);
                ScreenRegistry = new ScreenRegistry(LayerRegistry);
                Screens = new ScreenController
                (
                    ScreenRegistry,
                    LayerRegistry,
                    transitioner,
                    Focus,
                    inputDriver
                );

                Visibility = new VisibilityController();
                Modals = new ModalController();
                FocusHighlights = new FocusHighlightController();
                Projection = new ProjectionController();
                Placement = new PlacementController();
                DragVisuals = new DragVisualController(LayerRegistry);

                defaultProfile = AcquireProfileInternal(settings.DefaultProfile);

                if (!LayerRegistry.Contains(settings.SceneFadeLayerID))
                {
                    throw new InvalidOperationException
                    (
                        $"기본 Profile에 Scene Fade Layer '{settings.SceneFadeLayerID}'가 없습니다."
                    );
                }

                if (!LayerRegistry.TryGet(settings.SceneFadeLayerID, out var fadeLayer))
                {
                    throw new InvalidOperationException
                    (
                        $"Scene Fade Layer '{settings.SceneFadeLayerID}' backend를 조회할 수 없습니다."
                    );
                }

                sceneFadeSource =
                    new GameObjectProviderOverlaySource<ISceneFadeDriver>(settings.SceneFadeViewProvider);
                ValidateSceneFadeSource(sceneFadeSource, fadeLayer.Root);

                SceneFader = new SceneFader
                (
                    LayerRegistry,
                    settings.SceneFadeLayerID,
                    sceneFadeSource,
                    transitioner,
                    PresentationTimeSource.Unscaled
                );

                layoutController.Refresh();
                Screens.Activate();
                IsInitialized = true;
                coreReady = true;
                SubscribeSceneValidation();

                OnInitialized?.Invoke(this);

                // 모든 조립과 외부 구독이 성공한 실행 Host만 App 수명으로 커밋한다.
                if (Application.isPlaying)
                {
                    DontDestroyOnLoad(gameObject);
                }
            }
            catch (Exception exception)
            {
                IsInitialized = false;
                var errors = Release(coreReady);

                if (errors.Count == 0)
                {
                    throw;
                }

                errors.Insert(0, exception);
                throw new AggregateException("Game UI Runtime 초기화와 롤백이 실패했습니다.", errors);
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// App·Scene·게임 모드 Layer Profile을 명시적으로 획득한다.
        /// </summary>
        // ------------------------------------------------------------
        public GameUIProfileHandle AcquireProfile(GameUIProfileAsset profile)
        {
            ThrowIfUnavailable();
            return AcquireProfileInternal(profile);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Provider Layer Root를 모두 준비한 뒤 Registry에 등록한다.
        /// </summary>
        // ------------------------------------------------------------
        private GameUIProfileHandle AcquireProfileInternal(GameUIProfileAsset profile)
        {
            if (profile == null)
            {
                throw new ArgumentNullException(nameof(profile));
            }

            profile.Validate();

            var handle = new GameUIProfileHandle
            (
                profile,
                HandleProfileReleaseCompleted
            );
            profileHandles.Add(handle);
            var prepared = new List<(GameUIProfileHandle.OwnedLayer Layer, PresentationLayerAsset Asset, UGUILayerCanvas Driver)>();

            try
            {
                for (var i = 0; i < profile.Count; i++)
                {
                    var asset = profile.GetLayerAsset(i);
                    var provider = profile.GetProvider(i);
                    var previousParent = provider.Parent;
                    GameObject instance = null;
                    GameUIProfileHandle.OwnedLayer ownedLayer = null;

                    try
                    {
                        provider.Parent = layerRoot;
                        instance = provider.Acquire(false);

                        if (instance != null)
                        {
                            // Parent 복원도 실패할 수 있으므로 획득 직후 물리 소유권부터 기록한다.
                            ownedLayer = handle.AddLayer(provider, instance);
                        }
                    }
                    finally
                    {
                        provider.Parent = previousParent;
                    }

                    if (instance == null)
                    {
                        throw new InvalidOperationException
                        (
                            $"Profile Layer '{asset.ID}' Provider가 null 인스턴스를 반환했습니다."
                        );
                    }

                    instance.SetActive(false);

                    var driver = instance.GetComponent<UGUILayerCanvas>();

                    if (driver == null)
                    {
                        throw new InvalidOperationException
                        (
                            $"Profile Layer '{asset.ID}' Root에 UGUILayerCanvas가 없습니다."
                        );
                    }

                    prepared.Add((ownedLayer, asset, driver));
                }

                for (var i = 0; i < prepared.Count; i++)
                {
                    var item = prepared[i];

                    if (!item.Driver.Validate(item.Asset, out var error))
                    {
                        throw new InvalidOperationException
                        (
                            $"Profile Layer '{item.Asset.ID}' 구성이 유효하지 않습니다. {error}"
                        );
                    }
                }

                for (var i = 0; i < prepared.Count; i++)
                {
                    var item = prepared[i];
                    var layerHandle = LayerRegistry.Register(item.Asset, item.Driver);
                    handle.AttachLayerHandle(item.Layer, layerHandle);
                }

                return handle;
            }
            catch (Exception exception)
            {
                try
                {
                    handle.Dispose();
                }
                catch (Exception cleanupException)
                {
                    throw new AggregateException
                    (
                        $"Game UI Profile '{profile.name}' 획득과 롤백이 실패했습니다.",
                        exception,
                        cleanupException
                    );
                }

                throw;
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 완전히 반환된 Profile Handle을 Runtime 추적에서 제거한다.
        /// </summary>
        // ------------------------------------------------------------
        private void HandleProfileReleaseCompleted(GameUIProfileHandle handle)
        {
            profileHandles.Remove(handle);

            if (ReferenceEquals(defaultProfile, handle))
            {
                defaultProfile = null;
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Fade View Provider와 Driver를 초기화 시점에 한 번 획득·반환해 검증한다.
        /// </summary>
        // ------------------------------------------------------------
        private static void ValidateSceneFadeSource
        (
            GameObjectProviderOverlaySource<ISceneFadeDriver> source,
            Transform parent
        )
        {
            ISceneFadeDriver view = null;
            Exception validationError = null;

            try
            {
                view = source.Acquire(parent);

                if (!view.IsValid)
                {
                    validationError = new InvalidOperationException
                    (
                        "Scene Fade View Driver의 UGUI 참조가 유효하지 않습니다."
                    );
                }
                else
                {
                    view.Apply(0.0f);
                }
            }
            catch (Exception exception)
            {
                validationError = exception;
            }

            if (view != null)
            {
                try
                {
                    source.Release(view);
                }
                catch (Exception releaseException)
                {
                    validationError = validationError == null
                        ? releaseException
                        : new AggregateException(validationError, releaseException);
                }
            }

            if (validationError != null)
            {
                throw new InvalidOperationException
                (
                    "Scene Fade View Provider 또는 Driver 검증이 실패했습니다.",
                    validationError
                );
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Host, EventSystem과 직렬화 backend 참조를 명시적으로 검증한다.
        /// </summary>
        // ------------------------------------------------------------
        private void ValidateHost(GameUISettingsAsset settings)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            settings.Validate();

            if (!gameObject.activeInHierarchy)
            {
                throw new InvalidOperationException("Game UI Runtime Host Root가 활성 상태가 아닙니다.");
            }

            if (transform != transform.root)
            {
                throw new InvalidOperationException("GameUIRuntime은 Game UI Host Root에 있어야 합니다.");
            }

            if (!enabled)
            {
                throw new InvalidOperationException("Game UI Runtime Component가 비활성 상태입니다.");
            }

            if (layerRoot == null)
            {
                throw new InvalidOperationException("Game UI Layer 부모 Root가 연결되지 않았습니다.");
            }

            if (eventSystem == null || !eventSystem.enabled)
            {
                throw new InvalidOperationException("활성 EventSystem이 연결되지 않았습니다.");
            }

            if (inputModule == null || !inputModule.enabled)
            {
                throw new InvalidOperationException("활성 InputSystemUIInputModule이 연결되지 않았습니다.");
            }

            if (inputModule.GetComponent<EventSystem>() != eventSystem)
            {
                throw new InvalidOperationException("Input Module과 EventSystem이 같은 Host에 연결되지 않았습니다.");
            }

            if (layoutController == null || !layoutController.enabled ||
                layoutController.SafeAreaRoot == null)
            {
                throw new InvalidOperationException("활성 UGUI Layout Controller와 Safe Area Root가 필요합니다.");
            }

            if (focusDriver == null || !focusDriver.enabled ||
                focusDriver.EventSystem != eventSystem)
            {
                throw new InvalidOperationException("UGUI Focus Driver가 Host EventSystem에 연결되지 않았습니다.");
            }

            if (inputDriver == null || !inputDriver.enabled)
            {
                throw new InvalidOperationException("활성 Input System Screen Input Driver가 연결되지 않았습니다.");
            }

            ValidateSceneComposition();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 로드된 Scene 전체에서 Runtime Host와 EventSystem의 단일 구성을 검증한다.
        /// </summary>
        // ------------------------------------------------------------
        internal void ValidateSceneComposition()
        {
            var runtimes = FindObjectsByType<GameUIRuntime>
            (
                FindObjectsInactive.Include
            );

            for (var i = 0; i < runtimes.Length; i++)
            {
                if (runtimes[i] != this)
                {
                    throw new InvalidOperationException
                    (
                        $"다른 Game UI Runtime Host '{runtimes[i].name}'가 이미 로드되어 있습니다."
                    );
                }
            }

            var eventSystems = FindObjectsByType<EventSystem>
            (
                FindObjectsInactive.Include
            );

            for (var i = 0; i < eventSystems.Length; i++)
            {
                if (eventSystems[i] != eventSystem)
                {
                    throw new InvalidOperationException
                    (
                        $"다른 EventSystem '{eventSystems[i].name}'가 이미 로드되어 있습니다."
                    );
                }
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Runtime 초기화 뒤 Scene 로드 경계 검증을 등록한다.
        /// </summary>
        // ------------------------------------------------------------
        private void SubscribeSceneValidation()
        {
            if (sceneLoadedSubscribed) return;

            SceneManager.sceneLoaded += HandleSceneLoaded;
            sceneLoadedSubscribed = true;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Runtime 종료 전에 Scene 로드 경계 검증을 해제한다.
        /// </summary>
        // ------------------------------------------------------------
        private void UnsubscribeSceneValidation()
        {
            if (!sceneLoadedSubscribed) return;

            SceneManager.sceneLoaded -= HandleSceneLoaded;
            sceneLoadedSubscribed = false;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 새 Scene이 추가한 중복 Runtime 또는 EventSystem을 명시적 오류로 보고한다.
        /// </summary>
        // ------------------------------------------------------------
        private void HandleSceneLoaded
        (
            Scene scene,
            LoadSceneMode mode
        )
        {
            if (!IsInitialized || IsReleasing || IsReleased) return;

            try
            {
                ValidateSceneComposition();
            }
            catch (Exception exception)
            {
                Debug.LogException
                (
                    new InvalidOperationException
                    (
                        $"Scene '{scene.name}'의 Game UI 구성이 유효하지 않습니다.",
                        exception
                    ),
                    this
                );
            }
        }

    #endregion

    #region 종료

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> Screen, 프로젝트 Composition, 표시 서비스, Profile과 backend를 역순 해제한다.
        /// <br/> 논리 소유권은 한 번만 정리하고 Runtime은 오류와 관계없이 Terminal 상태로 끝난다.
        /// <br/> 후속 Shutdown은 소유권 유지가 명시된 Provider 반환 실패만 다시 시도한다.
        /// </summary>
        // ----------------------------------------------------------------------
        public void Shutdown()
        {
            var errors = Release(invokeReleasingEvent: true);

            if (errors.Count > 0)
            {
                throw new AggregateException("Game UI Runtime 종료 중 하나 이상의 정리가 실패했습니다.", errors);
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Unity 종료 callback에서 같은 종료 오류를 기록만 한다.
        /// </summary>
        // ------------------------------------------------------------
        private void ShutdownFallback()
        {
            var errors = Release(invokeReleasingEvent: true);

            for (var i = 0; i < errors.Count; i++)
            {
                if (errors[i] != null)
                {
                    Debug.LogException(errors[i], this);
                }
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 현재까지 생성된 Runtime 소유 리소스를 해제하고 오류를 수집한다.
        /// </summary>
        // ------------------------------------------------------------
        private List<Exception> Release(bool invokeReleasingEvent)
        {
            var errors = new List<Exception>();

            if (IsReleasing) return errors;

            if (IsReleased)
            {
                // Terminal Runtime에서는 상태 변경 전 거부되었거나 Provider 반환이 남은
                // 명시적 소유권만 다시 정리한다.
                if (sceneFadeSource != null)
                {
                    try
                    {
                        sceneFadeSource.Dispose();
                        sceneFadeSource = null;
                    }
                    catch (Exception exception)
                    {
                        errors.Add(exception);
                    }
                }

                for (var i = profileHandles.Count - 1; i >= 0; i--)
                {
                    DisposeOwned(profileHandles[i], errors);
                }

                return errors;
            }

            IsInitialized = false;
            IsReleasing = true;
            var screens = Screens;
            var sceneFader = SceneFader;
            var fadeSource = sceneFadeSource;
            var modals = Modals;
            var focusHighlights = FocusHighlights;
            var dragVisuals = DragVisuals;
            var visibility = Visibility;
            var screenRegistry = ScreenRegistry;
            var layout = layoutController;
            var input = inputDriver;
            var currentTransitioner = transitioner;
            var layerRegistry = LayerRegistry;
            var releasingSubscribers =
                invokeReleasingEvent
                    ? OnReleasing
                    : null;

            UnsubscribeSceneValidation();

            if (screens != null)
            {
                try
                {
                    errors.AddRange(screens.Shutdown());
                }
                catch (Exception exception)
                {
                    errors.Add(exception);
                }
            }

            if (releasingSubscribers != null)
            {
                try
                {
                    releasingSubscribers.Invoke(this);
                }
                catch (Exception exception)
                {
                    errors.Add(exception);
                }
            }

            // 종료 구독자가 살아 있는 Runtime 서비스를 확인한 뒤 public 접근 경계를 닫는다.
            Screens = null;
            SceneFader = null;
            Modals = null;
            FocusHighlights = null;
            DragVisuals = null;
            Visibility = null;
            ScreenRegistry = null;
            defaultProfile = null;
            layoutController = null;
            inputDriver = null;
            transitioner = null;
            LayerRegistry = null;
            Focus = null;
            Projection = null;
            Placement = null;
            Settings = null;
            OnInitialized = null;
            OnReleasing = null;

            DisposeOwned(input, errors);

            DisposeOwned(sceneFader, errors);

            if (fadeSource != null)
            {
                try
                {
                    fadeSource.Dispose();
                    sceneFadeSource = null;
                }
                catch (Exception exception)
                {
                    // Provider 계약상 반환 실패한 물리 소유권은 Source가 다음 Shutdown까지 보존한다.
                    errors.Add(exception);
                }
            }

            DisposeOwned(modals, errors);
            DisposeOwned(focusHighlights, errors);
            DisposeOwned(dragVisuals, errors);
            DisposeOwned(visibility, errors);
            DisposeOwned(screenRegistry, errors);

            // 성공한 Handle은 완료 Callback이 제거한다. 상태 변경 전 거부와 Provider 반환 실패는
            // Runtime 소유 목록에 남겨 후속 Shutdown까지 소유권을 보존한다.
            for (var i = profileHandles.Count - 1; i >= 0; i--)
            {
                DisposeOwned(profileHandles[i], errors);
            }

            DisposeOwned(layout, errors);
            DisposeOwned(currentTransitioner, errors);
            DisposeOwned(layerRegistry, errors);

            IsReleasing = false;
            IsReleased = true;

            return errors;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Terminal 상태로 분리한 IDisposable 서비스를 한 번 해제하고 오류를 수집한다.
        /// </summary>
        // ------------------------------------------------------------
        private static void DisposeOwned
        (
            IDisposable owned,
            List<Exception> errors
        )
        {
            if (owned == null) return;

            try
            {
                owned.Dispose();
            }
            catch (Exception exception)
            {
                errors.Add(exception);
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 초기화 완료 상태에서만 public Runtime 서비스를 사용하게 한다.
        /// </summary>
        // ------------------------------------------------------------
        private void ThrowIfUnavailable()
        {
            if (!IsInitialized || IsReleasing || IsReleased)
            {
                throw new InvalidOperationException("Game UI Runtime이 사용 가능한 상태가 아닙니다.");
            }
        }

    #endregion

    }
}
