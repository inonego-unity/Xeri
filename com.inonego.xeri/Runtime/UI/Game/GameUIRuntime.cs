/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : GameUIRuntime.cs
수정일 : 2026-07-29

# 설명
App 단위 Game UI 서비스, Scene 구성 검증, 기본·추가 Profile과 Host backend의 생성·역순 해제를 소유한다.
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
        private bool releasingNotified = false;
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

            var errors = new List<Exception>();
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
                DragVisuals = new DragVisualController();

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

                errors.AddRange(InvokeSubscribers(OnInitialized));

                if (errors.Count > 0)
                {
                    throw new AggregateException
                    (
                        "Game UI Runtime 초기화 구독자 실행이 실패했습니다.",
                        errors
                    );
                }

                // 모든 조립과 외부 구독이 성공한 실행 Host만 App 수명으로 커밋한다.
                if (Application.isPlaying)
                {
                    DontDestroyOnLoad(gameObject);
                }
            }
            catch (Exception exception)
            {
                if (errors.Count == 0)
                {
                    errors.Add(exception);
                }

                IsInitialized = false;
                errors.AddRange(ShutdownInternal(coreReady));

                if (errors.Count == 1)
                {
                    throw;
                }

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

            var handle = new GameUIProfileHandle(profile, HandleProfileDisposed);
            profileHandles.Add(handle);
            var prepared = new List<(GameUIProfileHandle.Entry Entry, PresentationLayerAsset Asset, UGUILayerCanvas Driver)>();

            try
            {
                for (var i = 0; i < profile.Count; i++)
                {
                    var asset = profile.GetLayerAsset(i);
                    var provider = profile.GetProvider(i);
                    var previousParent = provider.Parent;
                    GameObject instance = null;
                    GameUIProfileHandle.Entry entry = null;

                    try
                    {
                        provider.Parent = layerRoot;
                        instance = provider.Acquire(false);

                        if (instance != null)
                        {
                            entry = handle.Add(provider, instance);
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

                    prepared.Add((entry, asset, driver));
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
                    handle.SetLayerHandle(item.Entry, layerHandle);
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
        private void HandleProfileDisposed(GameUIProfileHandle handle)
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
        /// <br/> 정리가 끝난 뒤 수집한 예외를 호출자에게 전파한다.
        /// </summary>
        // ----------------------------------------------------------------------
        public void Shutdown()
        {
            if (IsReleased) return;

            var errors = ShutdownInternal(true);

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
            var errors = ShutdownInternal(true);

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
        private List<Exception> ShutdownInternal(bool notifyReleasing)
        {
            var errors = new List<Exception>();

            if (IsReleased) return errors;

            IsInitialized = false;
            IsReleasing = true;
            UnsubscribeSceneValidation();

            if (Screens != null)
            {
                errors.AddRange(Screens.Shutdown());

                if (!Screens.IsShutdownComplete)
                {
                    return errors;
                }
            }

            if (notifyReleasing && !releasingNotified)
            {
                releasingNotified = true;
                errors.AddRange(InvokeSubscribers(OnReleasing));
                OnInitialized = null;
                OnReleasing = null;
            }

            if (inputDriver != null)
            {
                try
                {
                    inputDriver.ForceReleaseAll();
                }
                catch (Exception exception)
                {
                    errors.Add(exception);
                }
            }

            DisposeOwned(SceneFader, errors, () => SceneFader = null);

            if (SceneFader == null)
            {
                DisposeOwned(sceneFadeSource, errors, () => sceneFadeSource = null);
            }

            DisposeOwned(Modals, errors, () => Modals = null);
            DisposeOwned(FocusHighlights, errors, () => FocusHighlights = null);
            DisposeOwned(DragVisuals, errors, () => DragVisuals = null);
            DisposeOwned(Visibility, errors, () => Visibility = null);

            if (ScreenRegistry != null)
            {
                try
                {
                    ScreenRegistry.Dispose();
                    ScreenRegistry = null;
                }
                catch (Exception exception)
                {
                    errors.Add(exception);
                }
            }

            var profiles = profileHandles.ToArray();

            for (var i = profiles.Length - 1; i >= 0; i--)
            {
                try
                {
                    profiles[i].Dispose();
                }
                catch (Exception exception)
                {
                    errors.Add(exception);
                }
            }

            DisposeOwned(layoutController, errors, () => layoutController = null);

            if (profileHandles.Count == 0)
            {
                if (inputDriver != null)
                {
                    try
                    {
                        inputDriver.Dispose();
                        inputDriver = null;
                    }
                    catch (Exception exception)
                    {
                        errors.Add(exception);
                    }
                }

                DisposeOwned(transitioner, errors, () => transitioner = null);
                DisposeOwned(LayerRegistry, errors, () => LayerRegistry = null);
            }

            if (profileHandles.Count == 0 &&
                SceneFader == null &&
                sceneFadeSource == null &&
                Modals == null &&
                FocusHighlights == null &&
                DragVisuals == null &&
                Visibility == null &&
                ScreenRegistry == null &&
                layoutController == null &&
                inputDriver == null &&
                transitioner == null &&
                LayerRegistry == null &&
                !sceneLoadedSubscribed)
            {
                Screens = null;
                Focus = null;
                Projection = null;
                Placement = null;
                Settings = null;
                OnInitialized = null;
                OnReleasing = null;
                IsReleasing = false;
                IsReleased = true;
            }

            return errors;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// IDisposable 서비스 해제를 시도하고 성공한 참조만 제거한다.
        /// </summary>
        // ------------------------------------------------------------
        private static void DisposeOwned
        (
            IDisposable owned,
            List<Exception> errors,
            Action clear
        )
        {
            if (owned == null) return;

            try
            {
                owned.Dispose();
                clear();
            }
            catch (Exception exception)
            {
                errors.Add(exception);
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// event 구독자를 각각 호출해 한 구독자 실패가 나머지를 막지 않게 한다.
        /// </summary>
        // ------------------------------------------------------------
        private List<Exception> InvokeSubscribers(Action<GameUIRuntime> subscribers)
        {
            var errors = new List<Exception>();

            if (subscribers == null) return errors;

            var invocationList = subscribers.GetInvocationList();

            for (var i = 0; i < invocationList.Length; i++)
            {
                try
                {
                    ((Action<GameUIRuntime>)invocationList[i]).Invoke(this);
                }
                catch (Exception exception)
                {
                    errors.Add(exception);
                }
            }

            return errors;
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
