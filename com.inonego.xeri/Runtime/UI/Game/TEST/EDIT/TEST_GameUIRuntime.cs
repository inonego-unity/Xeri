/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TEST_GameUIRuntime.cs
수정일 : 2026-07-30

# 설명
GameUIRuntime의 Profile 롤백, Scene 중복 구성과 초기화·종료 실패 정리를 검증한다.

# 테스트 구성
 P: Profile 획득 실패 롤백
 I: OnInitialized 실패 롤백
 R: OnReleasing 실패와 Terminal 정리
 S: Screen 정리 실패와 Terminal Shutdown
 C: Scene 구성 중복 검증
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
                get => parent;
                set
                {
                    if (FailWhenClearingParent && value == null)
                    {
                        throw new InvalidOperationException("injected provider parent restore failure");
                    }

                    parent = value;
                }
            }

            private Transform parent = null;

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
            /// null Parent 복원에서 예외를 발생시키는지 여부.
            /// </summary>
            // ------------------------------------------------------------
            public bool FailWhenClearingParent { get; set; }

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
        /// private setter를 포함한 Runtime 프로퍼티를 설정한다.
        /// </summary>
        // ------------------------------------------------------------
        private static void SetProperty
        (
            object target,
            string name,
            object value
        )
        {
            var property = target.GetType().GetProperty
            (
                name,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
            );

            Assert.IsNotNull(property, $"{target.GetType().Name}.{name}");
            property.SetValue(target, value);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// private instance 필드 값을 조회한다.
        /// </summary>
        // ------------------------------------------------------------
        private static T GetField<T>
        (
            object target,
            string name
        )
        {
            var field = target.GetType().GetField
            (
                name,
                BindingFlags.Instance | BindingFlags.NonPublic
            );

            Assert.IsNotNull(field, $"{target.GetType().Name}.{name}");
            return (T)field.GetValue(target);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// private 메서드를 호출하고 내부 예외를 원형으로 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        private static object Invoke
        (
            object target,
            string name,
            params object[] arguments
        )
        {
            var method = target.GetType().GetMethod
            (
                name,
                BindingFlags.Instance | BindingFlags.NonPublic
            );

            Assert.IsNotNull(method, $"{target.GetType().Name}.{name}");

            try
            {
                return method.Invoke(target, arguments);
            }
            catch (TargetInvocationException exception)
            {
                throw exception.InnerException;
            }
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
            SetField(asset, "mode", PresentationLayerMode.Shared);
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
                typeof(UGUILayerCanvas)
            );
            gameObject.transform.SetParent(parent, false);
            var driver = gameObject.GetComponent<UGUILayerCanvas>();
            SetField(driver, "root", gameObject.GetComponent<RectTransform>());
            SetField(driver, "canvas", null);
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
            var layout = host.AddComponent<UGUILayoutController>();
            var focus = host.AddComponent<UGUIFocusDriver>();
            var input = host.AddComponent<InputSystemScreenInputDriver>();
            var runtime = host.AddComponent<GameUIRuntime>();

            var layerRootObject = new GameObject("Layer Root", typeof(RectTransform));
            layerRootObject.transform.SetParent(host.transform, false);
            var safeAreaObject = new GameObject("Safe Area", typeof(RectTransform));
            safeAreaObject.transform.SetParent(host.transform, false);
            SetField(layout, "safeAreaRoot", safeAreaObject.GetComponent<RectTransform>());
            SetField(focus, "eventSystem", eventSystem);

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
            SetField(settings, "sceneFadeViewProvider", fadeProvider);
            SetField(settings, "uiActionMap", "UI");
            SetField(settings, "gameplayActionMap", "Player");
            SetField(settings, "releaseActionNames", new[] { "Cancel", "Submit", "Pause" });
            ownedObjects.Add(settings);

            SetField(runtime, "layerRoot", layerRootObject.transform);
            SetField(runtime, "eventSystem", eventSystem);
            SetField(runtime, "inputModule", inputModule);
            SetField(runtime, "layoutController", layout);
            SetField(runtime, "focusDriver", focus);
            SetField(runtime, "inputDriver", input);
            host.SetActive(true);

            return new RuntimeFixture
            {
                Runtime = runtime,
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
            var host = new GameObject("Profile Runtime");
            var layerRoot = new GameObject("Layer Root", typeof(RectTransform));
            layerRoot.transform.SetParent(host.transform, false);
            ownedObjects.Add(host);
            var runtime = host.AddComponent<GameUIRuntime>();
            var registry = new PresentationLayerRegistry();
            SetField(runtime, "layerRoot", layerRoot.transform);
            SetProperty(runtime, "LayerRegistry", registry);
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
                (CreateLayerAsset("First", 0), firstProvider),
                (CreateLayerAsset("Second", 1), secondProvider)
            );

            Assert.Throws<InvalidOperationException>
            (
                () => Invoke(runtime, "AcquireProfileInternal", profile)
            );

            Assert.AreEqual(1, firstProvider.AcquireCount);
            Assert.AreEqual(1, firstProvider.ReleaseCount);
            Assert.IsFalse(firstProvider.LastAcquireWorldPositionStays);
            Assert.IsFalse(firstProvider.LastReleaseWorldPositionStays);
            Assert.AreEqual(1, secondProvider.AcquireCount);
            Assert.IsFalse(registry.Contains("First"));
            var handles = (ICollection)typeof(GameUIRuntime).GetField
            (
                "profileHandles",
                BindingFlags.Instance | BindingFlags.NonPublic
            ).GetValue(runtime);
            Assert.AreEqual(0, handles.Count);
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// Provider Parent 복원 실패도 이미 획득한 Layer 인스턴스를 반환하는지 검증한다.
        /// </summary>
        // ----------------------------------------------------------------------
        [Test]
        public void TEST_GameUIRuntime_ProfileParent복원실패_획득Instance롤백()
        {
            var host = new GameObject("Profile Runtime");
            var layerRoot = new GameObject("Layer Root", typeof(RectTransform));
            layerRoot.transform.SetParent(host.transform, false);
            ownedObjects.Add(host);
            var runtime = host.AddComponent<GameUIRuntime>();
            SetField(runtime, "layerRoot", layerRoot.transform);
            SetProperty(runtime, "LayerRegistry", new PresentationLayerRegistry());
            var provider = new TestProvider
            (
                parent => CreateLayerRoot(parent, "Restored Layer")
            )
            {
                FailWhenClearingParent = true,
            };
            var profile = CreateProfile
            (
                (CreateLayerAsset("Restored", 0), provider)
            );

            Assert.Throws<InvalidOperationException>
            (
                () => Invoke(runtime, "AcquireProfileInternal", profile)
            );

            Assert.AreEqual(1, provider.AcquireCount);
            Assert.AreEqual(1, provider.ReleaseCount);
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
            SetField(fixture.Settings, "sceneFadeViewProvider", invalidFadeProvider);

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
        /// Terminal Runtime의 후속 Shutdown이 소유권이 남은 Provider 반환만 다시 시도하는지 검증한다.
        /// </summary>
        // ----------------------------------------------------------------------
        [Test]
        public void TEST_GameUIRuntime_Provider반환실패_후속Shutdown에서물리반환재시도()
        {
            var fixture = CreateRuntimeFixture();
            fixture.Runtime.Initialize(fixture.Settings);
            fixture.LayerProvider.ReleaseFailuresRemaining = 1;

            Assert.Throws<AggregateException>(fixture.Runtime.Shutdown);

            Assert.IsTrue(fixture.Runtime.IsReleased);
            Assert.AreEqual(1, fixture.LayerProvider.ReleaseCount);
            Assert.AreEqual(1, fixture.FadeProvider.ReleaseCount);

            Assert.DoesNotThrow(fixture.Runtime.Shutdown);

            Assert.AreEqual(2, fixture.LayerProvider.ReleaseCount);
            Assert.AreEqual(1, fixture.FadeProvider.ReleaseCount);
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// 종료 콜백의 재귀 Shutdown이 진행 중 종료를 조기 완료로 바꾸지 않는지 검증한다.
        /// </summary>
        // ----------------------------------------------------------------------
        [Test]
        public void TEST_GameUIRuntime_OnReleasing재귀Shutdown_외부종료만Terminal확정()
        {
            var fixture = CreateRuntimeFixture();
            var callbackCount = 0;
            fixture.Runtime.OnReleasing += runtime =>
            {
                callbackCount++;
                Assert.IsTrue(runtime.IsReleasing);
                Assert.IsFalse(runtime.IsReleased);
                Assert.DoesNotThrow(runtime.Shutdown);
                Assert.IsTrue(runtime.IsReleasing);
                Assert.IsFalse(runtime.IsReleased);
            };
            fixture.Runtime.Initialize(fixture.Settings);

            fixture.Runtime.Shutdown();

            Assert.AreEqual(1, callbackCount);
            Assert.IsFalse(fixture.Runtime.IsReleasing);
            Assert.IsTrue(fixture.Runtime.IsReleased);
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

            Assert.IsFalse(GetField<bool>(fixture.Runtime, "sceneLoadedSubscribed"));
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
