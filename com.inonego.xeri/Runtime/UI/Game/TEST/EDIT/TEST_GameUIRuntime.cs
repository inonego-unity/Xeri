/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TEST_GameUIRuntime.cs
수정일 : 2026-07-31

# 설명
GameUIRuntime의 혼합 Layer Profile, 롤백, Scene 중복 구성과 초기화·종료 실패 정리를 검증한다.

# 테스트 구성
 P: Profile 획득 실패 롤백
 I: OnInitialized 실패 롤백
 R: Runtime 종료 실패와 Terminal 정리
 S: Screen 정리 실패와 Terminal Shutdown
 C: Host·Scene 구성 검증
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

using NUnit.Framework;

namespace inonego.Xeri.TEST.UI._Game
{
    using inonego.Xeri.UI.Game;

    // ============================================================
    /// <summary>
    /// Game UI Composition Root의 실패 원자성과 역순 정리 테스트.
    /// </summary>
    // ============================================================
    public sealed class TEST_GameUIRuntime
    {
    #region 헬퍼 타입

        // ============================================================
        /// <summary>
        /// 획득·반환 호출과 local-space 인자를 기록하는 Provider.
        /// </summary>
        // ============================================================
        private sealed class TestProvider : IGameObjectProvider
        {
            // ------------------------------------------------------------
            /// <summary>
            /// Provider 기본 부모.
            /// </summary>
            // ------------------------------------------------------------
            public Transform Parent
            {
                get;
                set;
            }

            // ------------------------------------------------------------
            /// <summary>
            /// 누적 획득 호출 수.
            /// </summary>
            // ------------------------------------------------------------
            public int AcquireCount { get; private set; }

            // ------------------------------------------------------------
            /// <summary>
            /// 누적 반환 호출 수.
            /// </summary>
            // ------------------------------------------------------------
            public int ReleaseCount { get; private set; }

            // ------------------------------------------------------------
            /// <summary>
            /// 앞으로 실패시킬 반환 호출 수.
            /// </summary>
            // ------------------------------------------------------------
            public int ReleaseFailuresRemaining { get; set; }

            // ------------------------------------------------------------
            /// <summary>
            /// 마지막 획득의 worldPositionStays 인자.
            /// </summary>
            // ------------------------------------------------------------
            public bool LastAcquireWorldPositionStays { get; private set; }

            // ------------------------------------------------------------
            /// <summary>
            /// 마지막 반환의 worldPositionStays 인자.
            /// </summary>
            // ------------------------------------------------------------
            public bool LastReleaseWorldPositionStays { get; private set; }

            private readonly Func<Transform, GameObject> acquire = null;

            // ------------------------------------------------------------
            /// <summary>
            /// GameObject 생성 함수를 사용하는 테스트 Provider를 생성한다.
            /// </summary>
            // ------------------------------------------------------------
            public TestProvider(Func<Transform, GameObject> acquire) : base()
            {
                this.acquire = acquire ?? throw new ArgumentNullException(nameof(acquire));
            }

            // ------------------------------------------------------------
            /// <summary>
            /// 현재 Parent에 테스트 GameObject를 획득한다.
            /// </summary>
            // ------------------------------------------------------------
            public GameObject Acquire(bool worldPositionStays = true)
            {
                AcquireCount++;
                LastAcquireWorldPositionStays = worldPositionStays;
                return acquire(Parent);
            }

            // ------------------------------------------------------------
            /// <summary>
            /// 이 테스트에서는 비동기 획득을 지원하지 않는다.
            /// </summary>
            // ------------------------------------------------------------
            public Awaitable<GameObject> AcquireAsync(bool worldPositionStays = true)
            {
                throw new NotSupportedException();
            }

            // ------------------------------------------------------------
            /// <summary>
            /// 반환 호출과 좌표계 인자를 기록한다.
            /// </summary>
            // ------------------------------------------------------------
            public void Release
            (
                GameObject gameObject,
                bool worldPositionStays = true
            )
            {
                ReleaseCount++;
                LastReleaseWorldPositionStays = worldPositionStays;

                if (ReleaseFailuresRemaining > 0)
                {
                    ReleaseFailuresRemaining--;
                    throw new InvalidOperationException("injected provider release failure");
                }
            }
        }

        // ============================================================
        /// <summary>
        /// 초기화 실패 롤백에서 Screen Source 반환을 기록한다.
        /// </summary>
        // ============================================================
        private sealed class TestScreenSource : IScreenSource
        {
            // ------------------------------------------------------------
            /// <summary>
            /// 누적 획득 호출 수.
            /// </summary>
            // ------------------------------------------------------------
            public int AcquireCount { get; private set; }

            // ------------------------------------------------------------
            /// <summary>
            /// 누적 반환 호출 수.
            /// </summary>
            // ------------------------------------------------------------
            public int ReleaseCount { get; private set; }

            // ------------------------------------------------------------
            /// <summary>
            /// 단순 Screen backend를 반환한다.
            /// </summary>
            // ------------------------------------------------------------
            public ScreenInstance Acquire(ScreenViewScope scope)
            {
                AcquireCount++;
                return new ScreenInstance(new TestScreenDriver());
            }

            // ------------------------------------------------------------
            /// <summary>
            /// Screen backend 반환을 기록한다.
            /// </summary>
            // ------------------------------------------------------------
            public void Release(ScreenInstance instance)
            {
                ReleaseCount++;
            }
        }

        // ============================================================
        /// <summary>
        /// 즉시 Transition에 사용할 단순 Screen backend.
        /// </summary>
        // ============================================================
        private sealed class TestScreenDriver : IScreenDriver
        {
            // ------------------------------------------------------------
            /// <summary>
            /// 테스트 backend는 항상 유효하다.
            /// </summary>
            // ------------------------------------------------------------
            public bool IsValid => true;

            // ------------------------------------------------------------
            /// <summary>
            /// 현재 표시 진행 값.
            /// </summary>
            // ------------------------------------------------------------
            public float Visibility { get; private set; }

            // ------------------------------------------------------------
            /// <summary>
            /// 기본 Focus는 사용하지 않는다.
            /// </summary>
            // ------------------------------------------------------------
            public object DefaultFocus => null;

            // ------------------------------------------------------------
            /// <summary>
            /// 표시 상태는 이 계약 테스트에서 별도로 기록하지 않는다.
            /// </summary>
            // ------------------------------------------------------------
            public void SetVisible(bool visible)
            {
            }

            // ------------------------------------------------------------
            /// <summary>
            /// 상호작용 상태는 이 계약 테스트에서 별도로 기록하지 않는다.
            /// </summary>
            // ------------------------------------------------------------
            public void SetInteractable(bool interactable)
            {
            }

            // ------------------------------------------------------------
            /// <summary>
            /// Transition 진행 값을 기록한다.
            /// </summary>
            // ------------------------------------------------------------
            public void Apply(float value)
            {
                Visibility = value;
            }
        }

        // ============================================================
        /// <summary>
        /// Runtime 표시 서비스가 종료 구독자 실패 뒤에도 복원되는지 기록한다.
        /// </summary>
        // ============================================================
        private sealed class TestVisibilityTarget : IVisibilityTarget
        {
            // ------------------------------------------------------------
            /// <summary>
            /// 현재 표시 상태.
            /// </summary>
            // ------------------------------------------------------------
            public bool IsVisible { get; private set; } = true;

            // ------------------------------------------------------------
            /// <summary>
            /// 표시 상태를 적용한다.
            /// </summary>
            // ------------------------------------------------------------
            public void SetVisible(bool visible)
            {
                IsVisible = visible;
            }
        }

        // ============================================================
        /// <summary>
        /// 조립된 Runtime과 핵심 Provider를 묶는다.
        /// </summary>
        // ============================================================
        private sealed class RuntimeFixture
        {
            public GameUIRuntime Runtime { get; set; }
            public UGUISceneFadeSource FadeSource { get; set; }
            public GameUISettingsAsset Settings { get; set; }
            public TestProvider LayerProvider { get; set; }
            public TestProvider FadeProvider { get; set; }
        }

        // ============================================================
        /// <summary>
        /// Dispose 호출을 기록한 뒤 예외를 던지는 하위 Handle.
        /// </summary>
        // ============================================================
        private sealed class ThrowingHandle : IDisposable
        {
            public int DisposeCount { get; private set; }

            public void Dispose()
            {
                DisposeCount++;
                throw new InvalidOperationException("injected runtime screen child failure");
            }
        }

        private readonly List<UnityEngine.Object> ownedObjects = new List<UnityEngine.Object>();

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

        // ------------------------------------------------------------
        /// <summary>
        /// Presentation Layer Asset을 생성한다.
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
        /// private LayerEntry 목록을 채운 Profile Asset을 생성한다.
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
        /// Runtime Profile에 사용할 공유 UGUI Layer Root를 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        private GameObject CreateLayerRoot
        (
            Transform parent,
            string name
        )
        {
            var gameObject = new GameObject
            (
                name,
                typeof(RectTransform),
                typeof(Canvas),
                typeof(UGUILayerCanvas)
            );
            gameObject.transform.SetParent(parent, false);
            gameObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var driver = gameObject.GetComponent<UGUILayerCanvas>();
            SetField(driver, "root", gameObject.GetComponent<RectTransform>());
            SetField(driver, "canvas", gameObject.GetComponent<Canvas>());
            return gameObject;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 유효한 UGUI Scene Fade View를 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        private GameObject CreateFadeView(Transform parent)
        {
            var gameObject = new GameObject
            (
                "Fade View",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(CanvasGroup),
                typeof(UGUISceneFadeDriver)
            );
            gameObject.transform.SetParent(parent, false);
            var driver = gameObject.GetComponent<UGUISceneFadeDriver>();
            SetField(driver, "image", gameObject.GetComponent<Image>());
            SetField(driver, "canvasGroup", gameObject.GetComponent<CanvasGroup>());
            return gameObject;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Scene Fade Driver가 없는 잘못된 View를 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        private GameObject CreateInvalidFadeView(Transform parent)
        {
            var gameObject = new GameObject("Invalid Fade View", typeof(RectTransform));
            gameObject.transform.SetParent(parent, false);
            return gameObject;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 실제 Runtime Initialize에 필요한 최소 Host와 Settings를 조립한다.
        /// </summary>
        // ------------------------------------------------------------
        private RuntimeFixture CreateRuntimeFixture()
        {
            var host = new GameObject("Game UI Host");
            host.SetActive(false);
            ownedObjects.Add(host);

            var eventSystem = host.AddComponent<EventSystem>();
            var inputModule = host.AddComponent<InputSystemUIInputModule>();
            var uguiFocus = host.AddComponent<UGUIFocusDriver>();
            var uitkFocus = host.AddComponent<UITKFocusDriver>();
            var focus = host.AddComponent<GameUIFocusDriver>();
            var input = host.AddComponent<InputSystemScreenInputDriver>();
            var fadeSource = host.AddComponent<UGUISceneFadeSource>();
            var runtime = host.AddComponent<GameUIRuntime>();

            var layerRootObject = new GameObject("Layer Root", typeof(RectTransform));
            layerRootObject.transform.SetParent(host.transform, false);
            SetField(uguiFocus, "eventSystem", eventSystem);
            SetField(focus, "uguiFocusDriver", uguiFocus);
            SetField(focus, "uitkFocusDriver", uitkFocus);

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

            var layerProvider = new TestProvider
            (
                parent => CreateLayerRoot(parent, "Fade Layer")
            );
            var fadeProvider = new TestProvider(CreateFadeView);
            var profile = CreateProfile((CreateLayerAsset("Fade"), layerProvider));
            var settings = ScriptableObject.CreateInstance<GameUISettingsAsset>();
            SetField(settings, "defaultProfile", profile);
            SetField(settings, "sceneFadeLayerID", "Fade");
            SetField(settings, "uiActionMap", "UI");
            SetField(settings, "gameplayActionMap", "Player");
            SetField(settings, "releaseActionNames", new[] { "Cancel", "Submit", "Pause" });
            ownedObjects.Add(settings);

            SetField(fadeSource, "viewProvider", fadeProvider);
            SetField(runtime, "layerRoot", layerRootObject.transform);
            SetField(runtime, "focusDriver", focus);
            SetField(runtime, "sceneFadeSource", fadeSource);
            SetField(runtime, "eventSystem", eventSystem);
            SetField(runtime, "inputModule", inputModule);
            SetField(runtime, "inputDriver", input);
            host.SetActive(true);

            return new RuntimeFixture
            {
                Runtime = runtime,
                FadeSource = fadeSource,
                Settings = settings,
                LayerProvider = layerProvider,
                FadeProvider = fadeProvider,
            };
        }

    #endregion

    #region 픽스처

        // ------------------------------------------------------------
        /// <summary>
        /// 테스트에서 만든 Unity Object를 역순 제거한다.
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

    #region P-1: Profile 획득 실패

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> Profile 중간 Provider 실패가 앞서 획득한 Layer 인스턴스와 Registry를 롤백하고,
        /// <br/> Runtime 추적 Handle을 남기지 않는지 검증한다.
        /// </summary>
        // ----------------------------------------------------------------------
        [Test]
        public void TEST_GameUIRuntime_Profile획득실패_Layer와Provider전체롤백()
        {
            var fixture = CreateRuntimeFixture();
            fixture.Runtime.Initialize(fixture.Settings);
            var firstProvider = new TestProvider
            (
                parent => CreateLayerRoot(parent, "First Layer")
            );
            var secondProvider = new TestProvider
            (
                _ => throw new InvalidOperationException("injected provider acquire failure")
            );
            var profile = CreateProfile
            (
                (CreateLayerAsset("First", 1), firstProvider),
                (CreateLayerAsset("Second", 2), secondProvider)
            );

            Assert.Throws<InvalidOperationException>
            (
                () => fixture.Runtime.AcquireProfile(profile)
            );

            Assert.AreEqual(1, firstProvider.AcquireCount);
            Assert.AreEqual(1, firstProvider.ReleaseCount);
            Assert.IsFalse(firstProvider.LastAcquireWorldPositionStays);
            Assert.IsFalse(firstProvider.LastReleaseWorldPositionStays);
            Assert.AreEqual(1, secondProvider.AcquireCount);
            Assert.IsFalse(fixture.Runtime.LayerRegistry.Contains("First"));
            Assert.IsTrue(fixture.Runtime.IsInitialized);
            Assert.DoesNotThrow(fixture.Runtime.Shutdown);
        }

    #endregion

    #region I-1: 초기화 구독자 실패

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> OnInitialized 구독자 실패가 열린 Screen을 먼저 반환한 뒤 OnReleasing을 호출하고,
        /// <br/> Profile Provider와 Runtime Core를 모두 정리하는지 검증한다.
        /// </summary>
        // ----------------------------------------------------------------------
        [Test]
        public void TEST_GameUIRuntime_OnInitialized실패_Screen후OnReleasing과Core롤백()
        {
            var fixture = CreateRuntimeFixture();
            var source = new TestScreenSource();
            var releasingCount = 0;
            var sourceReleasedBeforeReleasing = false;

            fixture.Runtime.OnInitialized += runtime =>
            {
                runtime.ScreenRegistry.Register
                (
                    new ScreenOptions
                    (
                        "Boot",
                        "Fade",
                        openDuration: 0.0f,
                        closeDuration: 0.0f
                    ),
                    source
                );
                var response = runtime.Screens.Open("Boot");
                Assert.IsTrue(response.Accepted);
            };
            fixture.Runtime.OnInitialized += _ =>
            {
                throw new InvalidOperationException("injected initialized subscriber failure");
            };
            fixture.Runtime.OnReleasing += _ =>
            {
                releasingCount++;
                sourceReleasedBeforeReleasing = source.ReleaseCount == 1;
            };

            Assert.Throws<InvalidOperationException>
            (
                () => fixture.Runtime.Initialize(fixture.Settings)
            );

            Assert.AreEqual(1, source.AcquireCount);
            Assert.AreEqual(1, source.ReleaseCount);
            Assert.AreEqual(1, releasingCount);
            Assert.IsTrue(sourceReleasedBeforeReleasing);
            Assert.AreEqual(1, fixture.LayerProvider.ReleaseCount);
            Assert.AreEqual(1, fixture.FadeProvider.ReleaseCount);
            Assert.IsTrue(fixture.Runtime.IsReleased);
            Assert.IsFalse(fixture.Runtime.IsInitialized);
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> OnInitialized 중 명시적 Shutdown이 초기화 성공으로 반환되지 않고,
        /// <br/> 이미 끝난 소유 리소스를 후속 종료에서 다시 정리하지 않는지 검증한다.
        /// </summary>
        // ----------------------------------------------------------------------
        [Test]
        public void TEST_GameUIRuntime_OnInitialized중Shutdown_초기화실패와Terminal종료()
        {
            var fixture = CreateRuntimeFixture();
            fixture.Runtime.OnInitialized += runtime => runtime.Shutdown();

            Assert.Throws<InvalidOperationException>
            (
                () => fixture.Runtime.Initialize(fixture.Settings)
            );

            Assert.IsTrue(fixture.Runtime.IsReleased);
            Assert.IsFalse(fixture.Runtime.IsInitialized);
            Assert.AreEqual(1, fixture.LayerProvider.ReleaseCount);
            Assert.AreEqual(1, fixture.FadeProvider.ReleaseCount);

            Assert.DoesNotThrow(fixture.Runtime.Shutdown);
            Assert.AreEqual(1, fixture.LayerProvider.ReleaseCount);
            Assert.AreEqual(1, fixture.FadeProvider.ReleaseCount);
        }

    #endregion

    #region I-2: 필수 Fade 구성 실패

        // ------------------------------------------------------------
        /// <summary>
        /// Fade Driver 누락이 초기화 시점에 관찰되고 기본 Profile까지 롤백되는지 검증한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_GameUIRuntime_FadeDriver누락_초기화실패와기본Profile롤백()
        {
            var fixture = CreateRuntimeFixture();
            var invalidFadeProvider = new TestProvider(CreateInvalidFadeView);
            SetField(fixture.FadeSource, "viewProvider", invalidFadeProvider);

            Assert.Throws<InvalidOperationException>
            (
                () => fixture.Runtime.Initialize(fixture.Settings)
            );

            Assert.AreEqual(1, invalidFadeProvider.AcquireCount);
            Assert.AreEqual(1, invalidFadeProvider.ReleaseCount);
            Assert.IsFalse(invalidFadeProvider.LastAcquireWorldPositionStays);
            Assert.IsFalse(invalidFadeProvider.LastReleaseWorldPositionStays);
            Assert.AreEqual(1, fixture.LayerProvider.ReleaseCount);
            Assert.AreEqual(0, fixture.FadeProvider.AcquireCount);
            Assert.IsTrue(fixture.Runtime.IsReleased);
            Assert.IsNull(fixture.Runtime.LayerRegistry);
        }

    #endregion

    #region R-1: 종료 구독자 실패

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> OnReleasing 실패가 표시·Profile 정리를 막지 않고,
        /// <br/> 반복 Shutdown에서 구독자와 Provider를 다시 호출하지 않는지 검증한다.
        /// </summary>
        // ----------------------------------------------------------------------
        [Test]
        public void TEST_GameUIRuntime_OnReleasing실패_Core정리완료()
        {
            var fixture = CreateRuntimeFixture();
            var releasingCount = 0;
            var servicesAvailableToSubscriber = false;
            fixture.Runtime.OnReleasing += runtime =>
            {
                releasingCount++;
                servicesAvailableToSubscriber =
                    runtime.Visibility != null &&
                    runtime.Modals != null &&
                    runtime.LayerRegistry != null;
            };
            fixture.Runtime.OnReleasing += _ =>
            {
                throw new InvalidOperationException("injected releasing subscriber failure");
            };
            fixture.Runtime.Initialize(fixture.Settings);
            var target = new TestVisibilityTarget();
            fixture.Runtime.Visibility.Set(target, false);

            Assert.IsFalse(target.IsVisible);
            Assert.Throws<AggregateException>(fixture.Runtime.Shutdown);

            Assert.AreEqual(1, releasingCount);
            Assert.IsTrue(servicesAvailableToSubscriber);
            Assert.IsTrue(target.IsVisible);
            Assert.AreEqual(1, fixture.LayerProvider.ReleaseCount);
            Assert.AreEqual(1, fixture.FadeProvider.ReleaseCount);
            Assert.IsTrue(fixture.Runtime.IsReleased);

            Assert.DoesNotThrow(fixture.Runtime.Shutdown);
            Assert.AreEqual(1, releasingCount);
            Assert.AreEqual(1, fixture.LayerProvider.ReleaseCount);
            Assert.AreEqual(1, fixture.FadeProvider.ReleaseCount);
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// Provider 반환 실패 뒤 Terminal Runtime이 같은 반환을 다시 시도하지 않는지 검증한다.
        /// </summary>
        // ----------------------------------------------------------------------
        [Test]
        public void TEST_GameUIRuntime_Provider반환실패_후속Shutdown에서반복하지않음()
        {
            var fixture = CreateRuntimeFixture();
            fixture.Runtime.Initialize(fixture.Settings);
            fixture.LayerProvider.ReleaseFailuresRemaining = 1;

            Assert.Throws<AggregateException>(fixture.Runtime.Shutdown);

            Assert.IsTrue(fixture.Runtime.IsReleased);
            Assert.AreEqual(1, fixture.LayerProvider.ReleaseCount);
            Assert.AreEqual(1, fixture.FadeProvider.ReleaseCount);

            Assert.DoesNotThrow(fixture.Runtime.Shutdown);

            Assert.AreEqual(1, fixture.LayerProvider.ReleaseCount);
            Assert.AreEqual(1, fixture.FadeProvider.ReleaseCount);
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> 활성 Layer 소비자로 Profile 종료가 상태 변경 전에 거부돼도 소유권을 보존하고,
        /// <br/> 소비자 반환 뒤 후속 Shutdown이 Profile Provider를 반환하는지 검증한다.
        /// </summary>
        // ----------------------------------------------------------------------
        [Test]
        public void TEST_GameUIRuntime_활성Layer소비자_후속Shutdown에서Profile반환()
        {
            var fixture = CreateRuntimeFixture();
            fixture.Runtime.Initialize(fixture.Settings);
            var layerRegistry = fixture.Runtime.LayerRegistry;
            Assert.IsTrue
            (
                layerRegistry.TryAcquireUsage
                (
                    "Fade",
                    out _,
                    out var usage
                )
            );

            Assert.Throws<AggregateException>(fixture.Runtime.Shutdown);

            Assert.IsTrue(fixture.Runtime.IsReleased);
            Assert.AreEqual(0, fixture.LayerProvider.ReleaseCount);
            Assert.AreEqual(1, fixture.FadeProvider.ReleaseCount);

            usage.Dispose();

            Assert.DoesNotThrow(fixture.Runtime.Shutdown);
            Assert.AreEqual(1, fixture.LayerProvider.ReleaseCount);
            Assert.AreEqual(1, fixture.FadeProvider.ReleaseCount);
            Assert.Throws<ObjectDisposedException>
            (
                () => layerRegistry.TryGet("Fade", out _)
            );
        }

    #endregion

    #region S-1: Screen 정리 실패와 Terminal Shutdown

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> Screen 자식 정리가 실패해도 Scene 구독과 나머지 소유권을 한 번씩 정리하고,
        /// <br/> Runtime을 Terminal 상태로 확정하여 다음 Shutdown이 no-op인지 검증한다.
        /// </summary>
        // ----------------------------------------------------------------------
        [Test]
        public void TEST_GameUIRuntime_Screen정리실패_TerminalShutdown()
        {
            var fixture = CreateRuntimeFixture();
            fixture.Runtime.Initialize(fixture.Settings);
            var source = new TestScreenSource();
            fixture.Runtime.ScreenRegistry.Register
            (
                new ScreenOptions
                (
                    "Cleanup",
                    "Fade",
                    openDuration: 0.0f,
                    closeDuration: 0.0f
                ),
                source
            );
            var response = fixture.Runtime.Screens.Open("Cleanup");
            var child = new ThrowingHandle();
            response.Session.RegisterChild(child);

            Assert.Throws<AggregateException>(fixture.Runtime.Shutdown);

            Assert.IsFalse(fixture.Runtime.IsReleasing);
            Assert.IsTrue(fixture.Runtime.IsReleased);
            Assert.AreEqual(ScreenState.Closed, response.Session.State);
            Assert.AreEqual(1, source.ReleaseCount);
            Assert.AreEqual(1, child.DisposeCount);
            Assert.AreEqual(1, fixture.LayerProvider.ReleaseCount);
            Assert.AreEqual(1, fixture.FadeProvider.ReleaseCount);

            Assert.DoesNotThrow(fixture.Runtime.Shutdown);

            Assert.IsTrue(fixture.Runtime.IsReleased);
            Assert.AreEqual(ScreenState.Closed, response.Session.State);
            Assert.AreEqual(1, source.ReleaseCount);
            Assert.AreEqual(1, child.DisposeCount);
            Assert.AreEqual(1, fixture.LayerProvider.ReleaseCount);
            Assert.AreEqual(1, fixture.FadeProvider.ReleaseCount);
        }

    #endregion

    #region C-1: Scene 구성 중복

        // ------------------------------------------------------------
        /// <summary>
        /// 한 Runtime Host에 Scene Fade Source가 둘이면 초기화 전에 명시적으로 거부한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_GameUIRuntime_SceneFadeSource중복_초기화전거부()
        {
            var fixture = CreateRuntimeFixture();
            fixture.Runtime.gameObject.AddComponent<UGUISceneFadeSource>();

            var exception = Assert.Throws<InvalidOperationException>
            (
                () => fixture.Runtime.Initialize(fixture.Settings)
            );

            StringAssert.Contains("Scene Fade Source가 정확히 하나", exception.Message);
            Assert.IsFalse(fixture.Runtime.IsInitialized);
            Assert.IsTrue(fixture.Runtime.IsReleased);
            Assert.AreEqual(0, fixture.LayerProvider.AcquireCount);
            Assert.AreEqual(0, fixture.FadeProvider.AcquireCount);
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> Runtime 초기화 뒤 추가된 EventSystem을 명시적으로 거부하고,
        /// <br/> 중복 제거 뒤 기존 Runtime 구성이 그대로 유효한지 검증한다.
        /// </summary>
        // ----------------------------------------------------------------------
        [Test]
        public void TEST_GameUIRuntime_후속SceneEventSystem중복_기존Runtime유지하고명시적거부()
        {
            var fixture = CreateRuntimeFixture();
            fixture.Runtime.Initialize(fixture.Settings);
            var duplicateRoot = new GameObject("Duplicate EventSystem");
            ownedObjects.Add(duplicateRoot);
            duplicateRoot.AddComponent<EventSystem>();

            var exception = Assert.Throws<InvalidOperationException>
            (
                fixture.Runtime.ValidateSceneComposition
            );

            StringAssert.Contains("다른 EventSystem", exception.Message);
            Assert.IsTrue(fixture.Runtime.IsInitialized);

            UnityEngine.Object.DestroyImmediate(duplicateRoot);

            Assert.DoesNotThrow(fixture.Runtime.ValidateSceneComposition);
            Assert.DoesNotThrow(fixture.Runtime.Shutdown);
            Assert.IsTrue(fixture.Runtime.IsReleased);
        }

    #endregion

    }
}
