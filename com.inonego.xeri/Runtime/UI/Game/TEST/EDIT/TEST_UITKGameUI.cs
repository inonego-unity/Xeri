/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TEST_UITKGameUI.cs
수정일 : 2026-07-31

# 설명
UITK Layer, Screen, Modal, Fade와 혼합 Profile의 공개 Runtime 경로를 검증한다.

# 테스트 구성
 L: UIDocument Layer와 Order
 D: Screen·Modal·Fade Driver
 C: Screen Close와 Visual Tree 반환
 R: Runtime 혼합 Profile과 종료
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UIElements;

using NUnit.Framework;

namespace inonego.Xeri.TEST.UI._Game
{
    using inonego.Xeri.UI.Game;

    // ============================================================
    /// <summary>
    /// UI Toolkit Game UI의 실제 VisualElement 경계 테스트.
    /// </summary>
    // ============================================================
    public sealed class TEST_UITKGameUI
    {
    #region 헬퍼

        // ============================================================
        /// <summary>
        /// VisualElement Root를 제공하는 테스트 Layer Driver.
        /// </summary>
        // ============================================================
        private sealed class TestLayerDriver : IPresentationLayerDriver<VisualElement>
        {
            public VisualElement Root { get; }

            public TestLayerDriver(VisualElement root) : base()
            {
                Root = root;
            }

            public bool Validate(PresentationLayerAsset asset, out string error)
            {
                error = asset == null || Root == null ? "invalid" : "";
                return string.IsNullOrEmpty(error);
            }

            public void SetOrder(int order)
            {
            }

            public void SetActive(bool active)
            {
                Root.style.display = active ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        // ============================================================
        /// <summary>
        /// Visual Tree Screen을 생성하고 제거하는 테스트 Source.
        /// </summary>
        // ============================================================
        private sealed class TestScreenSource : IScreenSource
        {
            public VisualElement ScreenRoot { get; private set; }
            public int ReleaseCount { get; private set; }

            public ScreenInstance Acquire(ScreenViewScope scope)
            {
                if (!(scope.Layer is IPresentationLayerDriver<VisualElement> layer))
                {
                    throw new InvalidOperationException("VisualElement Layer가 필요합니다.");
                }

                ScreenRoot = new VisualElement { name = "RuntimeScreen" };
                layer.Root.Add(ScreenRoot);
                return new ScreenInstance(new UITKScreenDriver(ScreenRoot));
            }

            public void Release(ScreenInstance instance)
            {
                ReleaseCount++;
                ScreenRoot.RemoveFromHierarchy();
            }
        }

        // ============================================================
        /// <summary>
        /// 즉시 완료되는 Presentation Transitioner.
        /// </summary>
        // ============================================================
        private sealed class ImmediateTransitioner : IPresentationTransitioner
        {
            public PresentationTransitionHandle Play
            (
                PresentationTransitionParams parameters,
                Action onCompleted,
                Action<Exception> onFailed
            )
            {
                parameters.Target.Apply(parameters.EndValue);
                var handle = new PresentationTransitionHandle(null);
                handle.Complete();
                onCompleted?.Invoke();
                return handle;
            }

            public void Dispose()
            {
            }
        }

        // ============================================================
        /// <summary>
        /// Screen Focus 계약에 필요한 최소 메모리 Driver.
        /// </summary>
        // ============================================================
        private sealed class TestFocusDriver : IFocusDriver
        {
            public object Current { get; private set; }

            public bool IsValid(object target)
            {
                return target != null;
            }

            public void Select(object target)
            {
                Current = target;
            }

            public object FindFallback()
            {
                return null;
            }
        }

        // ============================================================
        /// <summary>
        /// 즉시 반환되는 Screen Input Session Driver.
        /// </summary>
        // ============================================================
        private sealed class TestInputDriver : IScreenInputDriver
        {
            private readonly List<ScreenInputSession> sessions =
                new List<ScreenInputSession>();

            public ScreenInputSession Acquire(ScreenOptions options)
            {
                var session = new ScreenInputSession(options, Release);
                sessions.Add(session);
                return session;
            }

            public void BeginBatch()
            {
            }

            public void EndBatch()
            {
            }

            public void ForceReleaseAll()
            {
                for (var i = sessions.Count - 1; i >= 0; i--)
                {
                    sessions[i].MarkReleased();
                }

                sessions.Clear();
            }

            public void Dispose()
            {
                ForceReleaseAll();
            }

            private void Release
            (
                ScreenInputSession session,
                bool waitForInputRelease,
                bool retainCursorWhileAwaitingRelease
            )
            {
                sessions.Remove(session);
                session.MarkReleased();
            }
        }

        // ============================================================
        /// <summary>
        /// Runtime Profile Layer의 획득·반환을 기록하는 Provider.
        /// </summary>
        // ============================================================
        private sealed class TestProvider : IGameObjectProvider
        {
            public Transform Parent { get; set; }
            public int AcquireCount { get; private set; }
            public int ReleaseCount { get; private set; }
            public bool LastReleasedActiveSelf { get; private set; }

            private readonly Func<Transform, GameObject> acquire = null;

            public TestProvider(Func<Transform, GameObject> acquire) : base()
            {
                this.acquire = acquire ?? throw new ArgumentNullException(nameof(acquire));
            }

            public GameObject Acquire(bool worldPositionStays = true)
            {
                AcquireCount++;
                return acquire(Parent);
            }

            public Awaitable<GameObject> AcquireAsync(bool worldPositionStays = true)
            {
                throw new NotSupportedException();
            }

            public void Release
            (
                GameObject gameObject,
                bool worldPositionStays = true
            )
            {
                ReleaseCount++;
                LastReleasedActiveSelf = gameObject.activeSelf;
            }
        }

        private readonly List<UnityEngine.Object> ownedObjects =
            new List<UnityEngine.Object>();

        // ------------------------------------------------------------
        /// <summary>
        /// 테스트 UXML Asset을 Resources에서 읽는다.
        /// </summary>
        // ------------------------------------------------------------
        private static VisualTreeAsset LoadViewAsset()
        {
            var asset = Resources.Load<VisualTreeAsset>
            (
                "Xeri/Game/TEST_UITKGameUI"
            );
            Assert.IsNotNull(asset);
            return asset;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 테스트 Layer Asset을 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        private PresentationLayerAsset CreateLayerAsset
        (
            string id,
            int order = 0
        )
        {
            var asset = ScriptableObject.CreateInstance<PresentationLayerAsset>();
            SetField(asset, "id", id);
            SetField(asset, "order", order);
            ownedObjects.Add(asset);
            return asset;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 테스트 Layer Entry를 가진 Profile을 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        private GameUIProfileAsset CreateProfile
        (
            params (PresentationLayerAsset Asset, IGameObjectProvider Provider)[] entries
        )
        {
            var profile = ScriptableObject.CreateInstance<GameUIProfileAsset>();
            var entryType = typeof(GameUIProfileAsset).GetNestedType
            (
                "LayerEntry",
                BindingFlags.NonPublic
            );
            Assert.IsNotNull(entryType);
            var listType = typeof(List<>).MakeGenericType(entryType);
            var list = (IList)Activator.CreateInstance(listType);

            for (var i = 0; i < entries.Length; i++)
            {
                var entry = Activator.CreateInstance(entryType, true);
                SetField(entry, "asset", entries[i].Asset);
                SetField(entry, "provider", entries[i].Provider);
                list.Add(entry);
            }

            SetField(profile, "layers", list);
            ownedObjects.Add(profile);
            return profile;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Profile Provider가 반환할 실제 UITK Layer GameObject를 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        private GameObject CreateLayerObject(Transform parent)
        {
            var gameObject = new GameObject("UITK Runtime Layer");
            gameObject.SetActive(false);
            gameObject.transform.SetParent(parent, false);
            ownedObjects.Add(gameObject);
            var document = gameObject.AddComponent<UIDocument>();
            var panelSettings = ScriptableObject.CreateInstance<PanelSettings>();
            ownedObjects.Add(panelSettings);
            document.panelSettings = panelSettings;
            document.visualTreeAsset = LoadViewAsset();
            var driver = gameObject.AddComponent<UITKLayerPanel>();
            SetField(driver, "document", document);
            SetField(driver, "rootName", "LayerRoot");
            return gameObject;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Profile Provider가 반환할 실제 UGUI Layer GameObject를 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        private GameObject CreateUGUILayerObject(Transform parent)
        {
            var gameObject = new GameObject
            (
                "UGUI Runtime Layer",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(UGUILayerCanvas)
            );
            gameObject.SetActive(false);
            gameObject.transform.SetParent(parent, false);
            ownedObjects.Add(gameObject);
            gameObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var driver = gameObject.GetComponent<UGUILayerCanvas>();
            SetField(driver, "root", gameObject.GetComponent<RectTransform>());
            SetField(driver, "canvas", gameObject.GetComponent<Canvas>());
            return gameObject;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// private 직렬화 필드를 설정한다.
        /// </summary>
        // ------------------------------------------------------------
        private static void SetField
        (
            object target,
            string name,
            object value
        )
        {
            var field = target.GetType().GetField
            (
                name,
                BindingFlags.Instance | BindingFlags.NonPublic
            );

            Assert.IsNotNull(field, $"{target.GetType().Name}.{name}");
            field.SetValue(target, value);
        }

    #endregion

    #region 픽스처

        // ------------------------------------------------------------
        /// <summary>
        /// 테스트에서 생성한 Unity Object를 역순 제거한다.
        /// </summary>
        // ------------------------------------------------------------
        [TearDown]
        public void TearDown()
        {
            for (var i = ownedObjects.Count - 1; i >= 0; i--)
            {
                if (ownedObjects[i] != null)
                {
                    UnityEngine.Object.DestroyImmediate(ownedObjects[i]);
                }
            }

            ownedObjects.Clear();
        }

    #endregion

    #region L-1: UIDocument Layer

        // ------------------------------------------------------------
        /// <summary>
        /// Layer 등록이 원본 Asset을 보존한 독립 Panel Order와 typed Root를 제공한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_UITKLayerPanel_Register_Order와TypedRoot적용()
        {
            var gameObject = new GameObject("UITK Layer");
            gameObject.SetActive(false);
            ownedObjects.Add(gameObject);
            var document = gameObject.AddComponent<UIDocument>();
            var panelSettings = ScriptableObject.CreateInstance<PanelSettings>();
            ownedObjects.Add(panelSettings);
            document.panelSettings = panelSettings;
            document.visualTreeAsset = LoadViewAsset();
            var driver = gameObject.AddComponent<UITKLayerPanel>();
            SetField(driver, "document", document);
            SetField(driver, "rootName", "LayerRoot");
            var registry = new PresentationLayerRegistry();

            var handle = registry.Register
            (
                CreateLayerAsset("Screen", 23),
                driver
            );

            Assert.AreNotSame(panelSettings, document.panelSettings);
            Assert.AreEqual(0, panelSettings.sortingOrder);
            Assert.AreEqual(23, document.panelSettings.sortingOrder);
            Assert.AreEqual(23, document.sortingOrder);
            Assert.IsNotNull(driver.Root);
            Assert.AreEqual("LayerRoot", driver.Root.name);

            handle.Dispose();
            registry.Dispose();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 공통 Screen Overlay Order와 비교할 수 없는 Target Texture Panel 등록을 거부한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_UITKLayerPanel_Register_TargetTexturePanel거부()
        {
            var gameObject = new GameObject("UITK Render Texture Layer");
            ownedObjects.Add(gameObject);
            var document = gameObject.AddComponent<UIDocument>();
            var panelSettings = ScriptableObject.CreateInstance<PanelSettings>();
            ownedObjects.Add(panelSettings);
            var targetTexture = new RenderTexture(16, 16, 0);
            ownedObjects.Add(targetTexture);
            panelSettings.targetTexture = targetTexture;
            document.panelSettings = panelSettings;
            document.visualTreeAsset = LoadViewAsset();
            var driver = gameObject.AddComponent<UITKLayerPanel>();
            SetField(driver, "document", document);
            SetField(driver, "rootName", "LayerRoot");
            var registry = new PresentationLayerRegistry();

            var exception = Assert.Throws<InvalidOperationException>
            (
                () => registry.Register
                (
                    CreateLayerAsset("UITK Render Texture", 23),
                    driver
                )
            );

            StringAssert.Contains("Target Texture", exception.Message);
            registry.Dispose();
        }

    #endregion

    #region D-1: Screen·Modal·Fade Driver

        // ------------------------------------------------------------
        /// <summary>
        /// Screen과 Modal Driver가 UITK 표시·상호작용 값을 직접 적용한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_UITKDrivers_표시상호작용Opacity와ModalTop적용()
        {
            var parent = new VisualElement();
            var screen = new VisualElement();
            var dim = new VisualElement();
            parent.Add(screen);
            screen.Add(dim);
            var screenDriver = new UITKScreenDriver(screen);
            var modalDriver = new UITKModalDriver(screen, dim);

            screenDriver.SetVisible(false);
            screenDriver.SetInteractable(false);
            screenDriver.Apply(0.25f);
            modalDriver.SetTop(true);

            Assert.AreEqual(DisplayStyle.None, screen.style.display.value);
            Assert.AreEqual(0.25f, screen.style.opacity.value);
            Assert.AreEqual(PickingMode.Position, screen.pickingMode);
            Assert.AreEqual(DisplayStyle.Flex, dim.style.display.value);
            Assert.AreEqual(PickingMode.Position, dim.pickingMode);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Fade Source 반환과 종료가 생성한 Visual Tree를 남기지 않는다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_UITKSceneFadeSource_AcquireRelease와Dispose_Tree제거()
        {
            var layerRoot = new VisualElement();
            var layer = new TestLayerDriver(layerRoot);
            var host = new GameObject("UITK Scene Fade Source");
            ownedObjects.Add(host);
            var source = host.AddComponent<UITKSceneFadeSource>();
            SetField(source, "viewAsset", LoadViewAsset());
            SetField(source, "rootName", "SceneFade");
            source.Initialize();
            var baselineCount = layerRoot.childCount;

            var first = source.Acquire(layer);
            var second = source.Acquire(layer);

            Assert.AreEqual(baselineCount + 2, layerRoot.childCount);
            Assert.IsTrue(first.IsValid);
            Assert.IsTrue(second.IsValid);

            source.Release(first);
            Assert.AreEqual(baselineCount + 1, layerRoot.childCount);
            Assert.IsFalse(first.IsValid);

            source.Dispose();
            Assert.AreEqual(baselineCount, layerRoot.childCount);
            Assert.IsFalse(second.IsValid);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// UITK Fade Source가 UGUI Layer에서 View를 만들기 전에 명시적으로 실패한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_UITKSceneFadeSource_UGUILayer_View생성전거부()
        {
            var sourceObject = new GameObject("UITK Scene Fade Source");
            ownedObjects.Add(sourceObject);
            var source = sourceObject.AddComponent<UITKSceneFadeSource>();
            SetField(source, "viewAsset", LoadViewAsset());
            SetField(source, "rootName", "SceneFade");
            source.Initialize();
            var layerObject = CreateUGUILayerObject(null);
            var layer = layerObject.GetComponent<UGUILayerCanvas>();

            Assert.Throws<InvalidOperationException>
            (
                () => source.Acquire(layer)
            );

            Assert.AreEqual(0, layer.Root.childCount);
            source.Dispose();
        }

    #endregion

    #region C-1: Screen Close

        // ------------------------------------------------------------
        /// <summary>
        /// UITK Screen Close가 기존 Controller 경로를 거쳐 View와 Layer Usage를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_UITKScreen_Close_Controller경로로Tree와Usage반환()
        {
            var layerRoot = new VisualElement();
            var layerRegistry = new PresentationLayerRegistry();
            var layerHandle = layerRegistry.Register
            (
                CreateLayerAsset("Screen"),
                new TestLayerDriver(layerRoot)
            );
            var screenRegistry = new ScreenRegistry(layerRegistry);
            var source = new TestScreenSource();
            var registration = screenRegistry.Register
            (
                new ScreenOptions("Menu", "Screen"),
                source
            );
            var transitioner = new ImmediateTransitioner();
            var input = new TestInputDriver();
            var controller = new ScreenController
            (
                screenRegistry,
                layerRegistry,
                transitioner,
                new FocusController(new TestFocusDriver()),
                input
            );
            controller.Activate();

            var response = controller.Open("Menu");
            Assert.IsTrue(response.Accepted);
            Assert.IsTrue(layerHandle.HasConsumers);
            Assert.AreSame(layerRoot, source.ScreenRoot.parent);

            Assert.IsTrue(response.Session.Close());

            Assert.AreEqual(1, source.ReleaseCount);
            Assert.IsNull(source.ScreenRoot.parent);
            Assert.IsFalse(layerHandle.HasConsumers);

            controller.Clear();
            input.Dispose();
            transitioner.Dispose();
            registration.Dispose();
            screenRegistry.Dispose();
            layerHandle.Dispose();
            layerRegistry.Dispose();
        }

    #endregion

    #region R-1: Runtime 혼합 Profile

        // ------------------------------------------------------------
        /// <summary>
        /// 공통 Runtime이 UGUI·UITK Layer가 섞인 Profile과 UITK Fade Source를 조립하고 종료한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_GameUIRuntime_혼합Profile_두Layer등록과Shutdown전체반환()
        {
            var host = new GameObject("Mixed Runtime Host");
            host.SetActive(false);
            ownedObjects.Add(host);
            var eventSystem = host.AddComponent<EventSystem>();
            var inputModule = host.AddComponent<InputSystemUIInputModule>();
            var inputDriver = host.AddComponent<InputSystemScreenInputDriver>();
            var uguiFocus = host.AddComponent<UGUIFocusDriver>();
            var uitkFocus = host.AddComponent<UITKFocusDriver>();
            var focus = host.AddComponent<GameUIFocusDriver>();
            var fadeSource = host.AddComponent<UITKSceneFadeSource>();
            var runtime = host.AddComponent<GameUIRuntime>();
            var layerRoot = new GameObject("Layer Root");
            layerRoot.transform.SetParent(host.transform, false);
            ownedObjects.Add(layerRoot);
            SetField(uguiFocus, "eventSystem", eventSystem);
            SetField(focus, "uguiFocusDriver", uguiFocus);
            SetField(focus, "uitkFocusDriver", uitkFocus);
            SetField(fadeSource, "viewAsset", LoadViewAsset());
            SetField(fadeSource, "rootName", "SceneFade");

            var actions = ScriptableObject.CreateInstance<InputActionAsset>();
            var ui = new InputActionMap("UI");
            ui.AddAction("Cancel", InputActionType.Button);
            ui.AddAction("Submit", InputActionType.Button);
            ui.AddAction("Pause", InputActionType.Button);
            var gameplay = new InputActionMap("Player");
            gameplay.AddAction("Move", InputActionType.Value);
            actions.AddActionMap(ui);
            actions.AddActionMap(gameplay);
            inputModule.actionsAsset = actions;
            ownedObjects.Add(actions);

            var uitkProvider = new TestProvider(CreateLayerObject);
            var uguiProvider = new TestProvider(CreateUGUILayerObject);
            var profile = CreateProfile
            (
                (CreateLayerAsset("Fade"), uitkProvider),
                (CreateLayerAsset("UGUI", 10), uguiProvider)
            );
            var settings = ScriptableObject.CreateInstance<GameUISettingsAsset>();
            SetField(settings, "defaultProfile", profile);
            SetField(settings, "sceneFadeLayerID", "Fade");
            SetField(settings, "uiActionMap", "UI");
            SetField(settings, "gameplayActionMap", "Player");
            SetField
            (
                settings,
                "releaseActionNames",
                new[] { "Cancel", "Submit", "Pause" }
            );
            ownedObjects.Add(settings);

            SetField(runtime, "layerRoot", layerRoot.transform);
            SetField(runtime, "focusDriver", focus);
            SetField(runtime, "sceneFadeSource", fadeSource);
            SetField(runtime, "eventSystem", eventSystem);
            SetField(runtime, "inputModule", inputModule);
            SetField(runtime, "inputDriver", inputDriver);
            host.SetActive(true);

            runtime.Initialize(settings);

            Assert.IsTrue(runtime.IsInitialized);
            Assert.IsTrue(runtime.LayerRegistry.Contains("Fade"));
            Assert.IsTrue(runtime.LayerRegistry.Contains("UGUI"));
            Assert.AreEqual(1, uitkProvider.AcquireCount);
            Assert.AreEqual(1, uguiProvider.AcquireCount);

            runtime.Shutdown();

            Assert.IsTrue(runtime.IsReleased);
            Assert.AreEqual(1, uitkProvider.ReleaseCount);
            Assert.AreEqual(1, uguiProvider.ReleaseCount);
            Assert.IsFalse(uitkProvider.LastReleasedActiveSelf);
            Assert.IsFalse(uguiProvider.LastReleasedActiveSelf);
        }

    #endregion

    }
}
