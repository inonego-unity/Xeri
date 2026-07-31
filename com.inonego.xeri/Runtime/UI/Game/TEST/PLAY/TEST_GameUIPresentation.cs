/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TEST_GameUIPresentation.cs
수정일 : 2026-07-31

# 설명
실제 Runtime Panel과 Canvas에서 UGUI·UITK 표시와 mixed Focus의 대표 경로를 검증한다.

# 테스트 구성
 O: UGUI·UITK 공통 Layer Order
 I: Input System 입력 해제 장벽
 P: DOTween Presentation Transition
 U: UGUI Layer·Screen·Modal·Fade
 T: UITK Panel·Focus·Screen·Modal·Fade
 M: UGUI·UITK mixed Focus
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.UI;
using UnityEngine.UIElements;
using UnityEngine.TestTools;

using NUnit.Framework;

using UGUIButton = UnityEngine.UI.Button;
using UGUIImage = UnityEngine.UI.Image;
using UnityCursor = UnityEngine.Cursor;

namespace inonego.Xeri.TEST.UI._Game
{
    using inonego.Xeri.UI.Game;

    // ============================================================
    /// <summary>
    /// Game UI native 표시와 mixed Focus의 대표 Runtime 통합 테스트.
    /// </summary>
    // ============================================================
    public sealed class TEST_GameUIPresentation
    {
    #region 헬퍼

        // ============================================================
        /// <summary>
        /// VisualElement Root를 공통 Layer Registry에 제공하는 테스트 Driver.
        /// </summary>
        // ============================================================
        private sealed class TestUITKLayerDriver : IPresentationLayerDriver<VisualElement>
        {
            // ------------------------------------------------------------
            /// <summary>
            /// View를 배치할 테스트 VisualElement Root.
            /// </summary>
            // ------------------------------------------------------------
            public VisualElement Root { get; }

            // ------------------------------------------------------------
            /// <summary>
            /// 지정 VisualElement를 Root로 사용하는 Driver를 생성한다.
            /// </summary>
            // ------------------------------------------------------------
            public TestUITKLayerDriver(VisualElement root) : base()
            {
                Root = root;
            }

            // ------------------------------------------------------------
            /// <summary>
            /// Layer Asset과 Root가 존재하는지 검증한다.
            /// </summary>
            // ------------------------------------------------------------
            public bool Validate(PresentationLayerAsset asset, out string error)
            {
                error = asset == null || Root == null ? "invalid" : "";
                return string.IsNullOrEmpty(error);
            }

            // ------------------------------------------------------------
            /// <summary>
            /// 이 테스트 Driver에서는 native Order 적용을 생략한다.
            /// </summary>
            // ------------------------------------------------------------
            public void SetOrder(int order)
            {
            }

            // ------------------------------------------------------------
            /// <summary>
            /// Root의 표시 상태를 적용한다.
            /// </summary>
            // ------------------------------------------------------------
            public void SetActive(bool active)
            {
                Root.style.display = active ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        // ============================================================
        /// <summary>
        /// UGUI Screen View를 생성하고 반환하는 테스트 Source.
        /// </summary>
        // ============================================================
        private sealed class TestUGUIScreenSource : IScreenSource
        {
            // ------------------------------------------------------------
            /// <summary>
            /// 마지막으로 획득한 Screen의 기본 Focus.
            /// </summary>
            // ------------------------------------------------------------
            public GameObject DefaultFocus { get; private set; }

            // ------------------------------------------------------------
            /// <summary>
            /// 누적 Screen 반환 호출 수.
            /// </summary>
            // ------------------------------------------------------------
            public int ReleaseCount { get; private set; }

            private GameObject root = null;

            // ------------------------------------------------------------
            /// <summary>
            /// RectTransform Layer에 UGUI Screen과 기본 Focus를 생성한다.
            /// </summary>
            // ------------------------------------------------------------
            public ScreenInstance Acquire(ScreenViewScope scope)
            {
                if (!(scope.Layer is IPresentationLayerDriver<RectTransform> layer))
                {
                    throw new InvalidOperationException("RectTransform Layer가 필요합니다.");
                }

                root = new GameObject
                (
                    "UGUI Screen",
                    typeof(RectTransform),
                    typeof(CanvasGroup),
                    typeof(UGUIScreenDriver)
                );
                root.transform.SetParent(layer.Root, false);
                DefaultFocus = new GameObject
                (
                    "UGUI Default Focus",
                    typeof(RectTransform),
                    typeof(UGUIButton)
                );
                DefaultFocus.transform.SetParent(root.transform, false);
                var driver = root.GetComponent<UGUIScreenDriver>();
                SetField(driver, "root", root);
                SetField(driver, "canvasGroup", root.GetComponent<CanvasGroup>());
                SetField(driver, "defaultFocus", DefaultFocus);
                return new ScreenInstance(driver);
            }

            // ------------------------------------------------------------
            /// <summary>
            /// Source가 생성한 UGUI Screen을 반환한다.
            /// </summary>
            // ------------------------------------------------------------
            public void Release(ScreenInstance instance)
            {
                ReleaseCount++;
                UnityEngine.Object.Destroy(root);
                root = null;
                DefaultFocus = null;
            }
        }

        // ============================================================
        /// <summary>
        /// UITK Screen View를 생성하고 Visual Tree에서 제거하는 테스트 Source.
        /// </summary>
        // ============================================================
        private sealed class TestUITKScreenSource : IScreenSource
        {
            // ------------------------------------------------------------
            /// <summary>
            /// 마지막으로 획득한 Screen의 기본 Focus.
            /// </summary>
            // ------------------------------------------------------------
            public VisualElement DefaultFocus { get; private set; }

            // ------------------------------------------------------------
            /// <summary>
            /// 누적 Screen 반환 호출 수.
            /// </summary>
            // ------------------------------------------------------------
            public int ReleaseCount { get; private set; }

            private VisualElement root = null;

            // ------------------------------------------------------------
            /// <summary>
            /// VisualElement Layer에 UITK Screen과 기본 Focus를 생성한다.
            /// </summary>
            // ------------------------------------------------------------
            public ScreenInstance Acquire(ScreenViewScope scope)
            {
                if (!(scope.Layer is IPresentationLayerDriver<VisualElement> layer))
                {
                    throw new InvalidOperationException("VisualElement Layer가 필요합니다.");
                }

                root = new VisualElement { name = "UITK Screen" };
                DefaultFocus = new Button { name = "UITK Default Focus" };
                root.Add(DefaultFocus);
                layer.Root.Add(root);
                return new ScreenInstance(new UITKScreenDriver(root, DefaultFocus));
            }

            // ------------------------------------------------------------
            /// <summary>
            /// Source가 생성한 UITK Screen을 Visual Tree에서 제거한다.
            /// </summary>
            // ------------------------------------------------------------
            public void Release(ScreenInstance instance)
            {
                ReleaseCount++;
                root.RemoveFromHierarchy();
                root = null;
                DefaultFocus = null;
            }
        }

        // ============================================================
        /// <summary>
        /// DOTween이 적용한 Presentation 값을 기록하는 테스트 Target.
        /// </summary>
        // ============================================================
        private sealed class TestTransitionTarget : IPresentationTransitionTarget
        {
            // ------------------------------------------------------------
            /// <summary>
            /// 테스트 수명 동안 Target은 유효하다.
            /// </summary>
            // ------------------------------------------------------------
            public bool IsValid => true;

            // ------------------------------------------------------------
            /// <summary>
            /// 마지막으로 적용된 진행 값.
            /// </summary>
            // ------------------------------------------------------------
            public float Value { get; private set; }

            // ------------------------------------------------------------
            /// <summary>
            /// Transition 진행 값을 기록한다.
            /// </summary>
            // ------------------------------------------------------------
            public void Apply(float value)
            {
                Value = value;
            }
        }

        // ============================================================
        /// <summary>
        /// 요청 값을 즉시 적용하고 Transition을 완료하는 테스트 구현.
        /// </summary>
        // ============================================================
        private sealed class ImmediateTransitioner : IPresentationTransitioner
        {
            // ------------------------------------------------------------
            /// <summary>
            /// 요청한 마지막 값을 적용하고 즉시 완료한다.
            /// </summary>
            // ------------------------------------------------------------
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

            // ------------------------------------------------------------
            /// <summary>
            /// 이 테스트 구현에는 별도 소유 자원이 없다.
            /// </summary>
            // ------------------------------------------------------------
            public void Dispose()
            {
            }
        }

        // ============================================================
        /// <summary>
        /// Screen별 Input Session 획득과 반환만 수행하는 테스트 구현.
        /// </summary>
        // ============================================================
        private sealed class TestInputDriver : IScreenInputDriver
        {
            private readonly List<ScreenInputSession> sessions =
                new List<ScreenInputSession>();

            // ------------------------------------------------------------
            /// <summary>
            /// Screen 입력 정책 Session을 획득한다.
            /// </summary>
            // ------------------------------------------------------------
            public ScreenInputSession Acquire(ScreenOptions options)
            {
                var session = new ScreenInputSession(options, Release);
                sessions.Add(session);
                return session;
            }

            // ------------------------------------------------------------
            /// <summary>
            /// 이 테스트 구현에서는 Batch 중간 적용이 없다.
            /// </summary>
            // ------------------------------------------------------------
            public void BeginBatch()
            {
            }

            // ------------------------------------------------------------
            /// <summary>
            /// 이 테스트 구현에서는 Batch 종료 적용이 없다.
            /// </summary>
            // ------------------------------------------------------------
            public void EndBatch()
            {
            }

            // ------------------------------------------------------------
            /// <summary>
            /// 남은 Input Session을 모두 종결한다.
            /// </summary>
            // ------------------------------------------------------------
            public void ForceReleaseAll()
            {
                for (var i = sessions.Count - 1; i >= 0; i--)
                {
                    sessions[i].MarkReleased();
                }

                sessions.Clear();
            }

            // ------------------------------------------------------------
            /// <summary>
            /// 남은 Input Session을 모두 반환한다.
            /// </summary>
            // ------------------------------------------------------------
            public void Dispose()
            {
                ForceReleaseAll();
            }

            // ------------------------------------------------------------
            /// <summary>
            /// 지정 Input Session을 소유 목록에서 제거하고 종결한다.
            /// </summary>
            // ------------------------------------------------------------
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
        /// 테스트 UIDocument을 활성화하고 Layer Root를 직접 구성한다.
        /// </summary>
        // ------------------------------------------------------------
        private static void PrepareDocumentLayer(UIDocument document)
        {
            // 비활성화 시 비워지는 Runtime Visual Tree를 등록 직전에 다시 준비한다.
            document.gameObject.SetActive(true);
            var documentLayerRoot = new VisualElement { name = "LayerRoot" };
            document.rootVisualElement.Add(documentLayerRoot);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 테스트 Layer Asset을 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        private static PresentationLayerAsset CreateLayerAsset
        (
            string id,
            int order
        )
        {
            var asset = ScriptableObject.CreateInstance<PresentationLayerAsset>();
            SetField(asset, "id", id);
            SetField(asset, "order", order);
            return asset;
        }

    #endregion

    #region O-1: mixed Layer Order

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> 같은 Screen Overlay에서 더 높은 Layer Order의 UGUI 또는 UITK가
        /// <br/> 기술과 관계없이 실제 최종 픽셀의 전면에 렌더링되는지 검증한다.
        /// </summary>
        // ----------------------------------------------------------------------
        [UnityTest]
        public IEnumerator TEST_PresentationLayerRegistry_UGUI와UITK교차Order_높은Layer가전면렌더링()
        {
            var uguiObject = new GameObject
            (
                "UGUI Order Layer",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(UGUILayerCanvas)
            );
            uguiObject.SetActive(false);
            var imageObject = new GameObject
            (
                "UGUI Fill",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(UGUIImage)
            );
            imageObject.transform.SetParent(uguiObject.transform, false);
            var imageRect = imageObject.GetComponent<RectTransform>();
            imageRect.anchorMin = Vector2.zero;
            imageRect.anchorMax = Vector2.one;
            imageRect.offsetMin = Vector2.zero;
            imageRect.offsetMax = Vector2.zero;
            imageObject.GetComponent<UGUIImage>().color = Color.red;

            var ugui = uguiObject.GetComponent<UGUILayerCanvas>();
            uguiObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            SetField(ugui, "root", uguiObject.GetComponent<RectTransform>());
            SetField(ugui, "canvas", uguiObject.GetComponent<Canvas>());

            var uitkObject = new GameObject("UITK Order Layer");
            var panelSettings = ScriptableObject.CreateInstance<PanelSettings>();
            var document = uitkObject.AddComponent<UIDocument>();
            document.panelSettings = panelSettings;
            PrepareDocumentLayer(document);
            var uitk = uitkObject.AddComponent<UITKLayerPanel>();
            SetField(uitk, "document", document);
            SetField(uitk, "rootName", "LayerRoot");

            var uguiAsset = CreateLayerAsset("UGUI Order", 10);
            var uitkAsset = CreateLayerAsset("UITK Order", 20);
            var registry = new PresentationLayerRegistry();
            PresentationLayerHandle uguiHandle = null;
            PresentationLayerHandle uitkHandle = null;

            try
            {
                uguiHandle = registry.Register(uguiAsset, ugui);
                uitkHandle = registry.Register(uitkAsset, uitk);

                var documentRoot = document.rootVisualElement;
                documentRoot.style.position = Position.Absolute;
                documentRoot.style.left = 0.0f;
                documentRoot.style.top = 0.0f;
                documentRoot.style.right = 0.0f;
                documentRoot.style.bottom = 0.0f;

                var layerRoot = uitk.Root;
                layerRoot.style.position = Position.Absolute;
                layerRoot.style.left = 0.0f;
                layerRoot.style.top = 0.0f;
                layerRoot.style.right = 0.0f;
                layerRoot.style.bottom = 0.0f;
                layerRoot.style.backgroundColor = Color.blue;

                yield return null;
                yield return new WaitForEndOfFrame();

                var screenshot = ScreenCapture.CaptureScreenshotAsTexture();

                try
                {
                    var color = screenshot.GetPixel
                    (
                        screenshot.width / 2,
                        screenshot.height / 2
                    );
                    Assert.Greater(color.b, color.r);
                }
                finally
                {
                    UnityEngine.Object.Destroy(screenshot);
                }

                uitkHandle.Dispose();
                uitkHandle = null;
                uguiHandle.Dispose();
                uguiHandle = null;
                SetField(uguiAsset, "order", 30);
                SetField(uitkAsset, "order", 20);
                PrepareDocumentLayer(document);
                uguiHandle = registry.Register(uguiAsset, ugui);
                uitkHandle = registry.Register(uitkAsset, uitk);

                documentRoot = document.rootVisualElement;
                documentRoot.style.position = Position.Absolute;
                documentRoot.style.left = 0.0f;
                documentRoot.style.top = 0.0f;
                documentRoot.style.right = 0.0f;
                documentRoot.style.bottom = 0.0f;

                layerRoot = uitk.Root;
                layerRoot.style.position = Position.Absolute;
                layerRoot.style.left = 0.0f;
                layerRoot.style.top = 0.0f;
                layerRoot.style.right = 0.0f;
                layerRoot.style.bottom = 0.0f;
                layerRoot.style.backgroundColor = Color.blue;

                yield return null;
                yield return new WaitForEndOfFrame();

                screenshot = ScreenCapture.CaptureScreenshotAsTexture();

                try
                {
                    var color = screenshot.GetPixel
                    (
                        screenshot.width / 2,
                        screenshot.height / 2
                    );
                    Assert.Greater(color.r, color.b);
                }
                finally
                {
                    UnityEngine.Object.Destroy(screenshot);
                }
            }
            finally
            {
                uitkHandle?.Dispose();
                uguiHandle?.Dispose();
                registry.Dispose();
                UnityEngine.Object.Destroy(uitkObject);
                UnityEngine.Object.Destroy(uguiObject);
                UnityEngine.Object.Destroy(panelSettings);
                UnityEngine.Object.Destroy(uitkAsset);
                UnityEngine.Object.Destroy(uguiAsset);
            }
        }

    #endregion

    #region I-1: Input System 입력 해제 장벽

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> 실제 해제 입력이 끝난 뒤 여러 Session을 같은 갱신에서 제거하고,
        /// <br/> 각 완료 callback이 최종 기준 Cursor 상태를 관찰하는지 검증한다.
        /// </summary>
        // ----------------------------------------------------------------------
        [UnityTest]
        public IEnumerator TEST_InputSystemScreenInputDriver_다중해제완료_Callback이최종Cursor만관찰()
        {
            var baselineCursorVisible = UnityCursor.visible;
            var baselineCursorLockMode = UnityCursor.lockState;
            var host = new GameObject("Input Host");
            var inputModule = host.AddComponent<InputSystemUIInputModule>();
            var driver = host.AddComponent<InputSystemScreenInputDriver>();
            var actions = ScriptableObject.CreateInstance<InputActionAsset>();
            var settings = ScriptableObject.CreateInstance<GameUISettingsAsset>();
            Keyboard keyboard = null;

            try
            {
                keyboard = InputSystem.AddDevice<Keyboard>();
                var ui = new InputActionMap("UI");
                ui.AddAction("Cancel", InputActionType.Button)
                    .AddBinding("<Keyboard>/escape");
                var gameplay = new InputActionMap("Player");
                gameplay.AddAction("Cancel", InputActionType.Button)
                    .AddBinding("<Keyboard>/escape");
                actions.AddActionMap(ui);
                actions.AddActionMap(gameplay);
                gameplay.Enable();
                inputModule.actionsAsset = actions;
                SetField(settings, "uiActionMap", "UI");
                SetField(settings, "gameplayActionMap", "Player");
                SetField(settings, "releaseActionNames", new[] { "Cancel" });

                UnityCursor.visible = false;
                driver.Initialize(inputModule, settings);
                var lower = driver.Acquire
                (
                    new ScreenOptions
                    (
                        "Lower",
                        "Screen",
                        showsCursor: true
                    )
                );
                var upper = driver.Acquire
                (
                    new ScreenOptions
                    (
                        "Upper",
                        "Screen",
                        showsCursor: false
                    )
                );
                bool? lowerObservedCursorVisible = null;
                bool? upperObservedCursorVisible = null;

                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Escape));
                InputSystem.Update();
                lower.Release
                (
                    waitForInputRelease: true,
                    retainCursorWhileAwaitingRelease: true,
                    onReleaseCompleted: () => lowerObservedCursorVisible = UnityCursor.visible
                );
                upper.Release
                (
                    waitForInputRelease: true,
                    retainCursorWhileAwaitingRelease: true,
                    onReleaseCompleted: () => upperObservedCursorVisible = UnityCursor.visible
                );

                Assert.IsTrue(lower.IsAwaitingRelease);
                Assert.IsTrue(upper.IsAwaitingRelease);

                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                InputSystem.Update();
                yield return null;
                yield return null;
                yield return null;

                Assert.IsTrue(lower.IsReleased);
                Assert.IsTrue(upper.IsReleased);
                Assert.AreEqual(false, lowerObservedCursorVisible);
                Assert.AreEqual(false, upperObservedCursorVisible);
                Assert.IsFalse(UnityCursor.visible);
            }
            finally
            {
                driver.Dispose();

                if (keyboard != null)
                {
                    InputSystem.RemoveDevice(keyboard);
                }

                UnityCursor.visible = baselineCursorVisible;
                UnityCursor.lockState = baselineCursorLockMode;
                UnityEngine.Object.Destroy(host);
                UnityEngine.Object.Destroy(actions);
                UnityEngine.Object.Destroy(settings);
            }
        }

    #endregion

    #region P-1: DOTween Presentation Transition

        // ------------------------------------------------------------
        /// <summary>
        /// 실제 DOTween backend가 0이 아닌 시간의 Transition을 완료 값까지 재생하는지 검증한다.
        /// </summary>
        // ------------------------------------------------------------
        [UnityTest]
        public IEnumerator TEST_DOTweenPresentationTransitioner_실제Tween_완료값적용()
        {
            var transitioner = new DOTweenPresentationTransitioner();
            var target = new TestTransitionTarget();
            PresentationTransitionHandle handle = null;
            var completed = false;
            Exception failure = null;

            try
            {
                handle = transitioner.Play
                (
                    new PresentationTransitionParams
                    (
                        target,
                        0.0f,
                        1.0f,
                        0.05f,
                        PresentationTimeSource.Unscaled
                    ),
                    () => completed = true,
                    exception => failure = exception
                );

                for (var i = 0; i < 120 && !completed && failure == null; i++)
                {
                    yield return null;
                }

                Assert.IsNull(failure);
                Assert.IsTrue(completed);
                Assert.IsTrue(handle.IsCompleted);
                Assert.AreEqual(1.0f, target.Value, 0.001f);
            }
            finally
            {
                handle?.Cancel();
                transitioner.Dispose();
            }
        }

    #endregion

    #region U-1: UGUI Runtime

        // ------------------------------------------------------------
        /// <summary>
        /// 실제 Canvas에서 Layer Order와 Screen·Modal·Fade 표시 값이 적용된다.
        /// </summary>
        // ------------------------------------------------------------
        [UnityTest]
        public IEnumerator TEST_UGUIPresentation_RuntimeCanvas_대표표시경로()
        {
            var canvasRoot = new GameObject
            (
                "UGUI Canvas Root",
                typeof(RectTransform),
                typeof(Canvas)
            );
            var layerObject = new GameObject
            (
                "UGUI Layer",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(UGUILayerCanvas)
            );
            layerObject.transform.SetParent(canvasRoot.transform, false);
            var screenObject = new GameObject
            (
                "UGUI Screen",
                typeof(RectTransform),
                typeof(CanvasGroup),
                typeof(UGUIScreenDriver),
                typeof(UGUIModalDriver)
            );
            screenObject.transform.SetParent(layerObject.transform, false);
            var dimObject = new GameObject
            (
                "Dim",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(UGUIImage)
            );
            dimObject.transform.SetParent(screenObject.transform, false);
            var fadeObject = new GameObject
            (
                "Fade",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(UGUIImage),
                typeof(CanvasGroup),
                typeof(UGUISceneFadeDriver)
            );
            fadeObject.transform.SetParent(layerObject.transform, false);
            var asset = CreateLayerAsset("UGUI", 17);
            var registry = new PresentationLayerRegistry();

            try
            {
                var layer = layerObject.GetComponent<UGUILayerCanvas>();
                var canvas = layerObject.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                SetField(layer, "root", layerObject.GetComponent<RectTransform>());
                SetField(layer, "canvas", canvas);
                var layerHandle = registry.Register(asset, layer);
                var screen = screenObject.GetComponent<UGUIScreenDriver>();
                SetField(screen, "root", screenObject);
                SetField(screen, "canvasGroup", screenObject.GetComponent<CanvasGroup>());
                var modal = screenObject.GetComponent<UGUIModalDriver>();
                SetField(modal, "canvasGroup", screenObject.GetComponent<CanvasGroup>());
                SetField(modal, "dimRoot", dimObject);
                var fade = fadeObject.GetComponent<UGUISceneFadeDriver>();
                SetField(fade, "image", fadeObject.GetComponent<UGUIImage>());
                SetField(fade, "canvasGroup", fadeObject.GetComponent<CanvasGroup>());

                screen.SetVisible(true);
                screen.SetInteractable(true);
                screen.Apply(0.5f);
                modal.SetTop(true);
                fade.SetColor(Color.black);
                fade.Apply(1.0f);
                yield return null;

                Assert.IsTrue(canvas.overrideSorting);
                Assert.AreEqual(17, canvas.sortingOrder);
                Assert.AreEqual(0.5f, screenObject.GetComponent<CanvasGroup>().alpha);
                Assert.IsTrue(dimObject.activeSelf);
                Assert.AreEqual(1.0f, fadeObject.GetComponent<CanvasGroup>().alpha);

                layerHandle.Dispose();
                Assert.IsFalse(layerObject.activeSelf);
            }
            finally
            {
                registry.Dispose();
                UnityEngine.Object.Destroy(canvasRoot);
                UnityEngine.Object.Destroy(asset);
            }
        }

    #endregion

    #region T-1: UITK Runtime

        // ------------------------------------------------------------
        /// <summary>
        /// 실제 Panel에서 Focus와 Screen·Modal·Fade VisualElement 상태가 적용된다.
        /// </summary>
        // ------------------------------------------------------------
        [UnityTest]
        public IEnumerator TEST_UITKPresentation_RuntimePanel_대표표시경로()
        {
            var host = new GameObject("UITK Panel");
            var panelSettings = ScriptableObject.CreateInstance<PanelSettings>();
            var document = host.AddComponent<UIDocument>();
            document.panelSettings = panelSettings;
            var focus = host.AddComponent<UITKFocusDriver>();
            var screenRoot = new VisualElement { name = "Screen" };
            var button = new Button { name = "DefaultFocus" };
            var dim = new VisualElement { name = "Dim" };
            var fadeRoot = new VisualElement { name = "Fade" };
            document.rootVisualElement.Add(screenRoot);
            screenRoot.Add(button);
            screenRoot.Add(dim);
            document.rootVisualElement.Add(fadeRoot);

            try
            {
                yield return null;
                yield return null;

                var screen = new UITKScreenDriver(screenRoot, button);
                var modal = new UITKModalDriver(screenRoot, dim);
                var fade = new UITKSceneFadeDriver(fadeRoot);
                screen.SetVisible(true);
                screen.SetInteractable(true);
                screen.Apply(0.6f);
                modal.SetTop(true);
                fade.SetColor(Color.black);
                fade.Apply(1.0f);
                focus.Select(button);
                yield return null;

                Assert.AreSame(button, focus.Current);
                Assert.AreEqual(0.6f, screenRoot.style.opacity.value);
                Assert.AreEqual(PickingMode.Position, screenRoot.pickingMode);
                Assert.AreEqual(DisplayStyle.Flex, dim.style.display.value);
                Assert.AreEqual(1.0f, fadeRoot.style.opacity.value);
                Assert.AreEqual(PickingMode.Position, fadeRoot.pickingMode);

                focus.Select(null);
                Assert.IsNull(focus.Current);
            }
            finally
            {
                UnityEngine.Object.Destroy(host);
                UnityEngine.Object.Destroy(panelSettings);
            }
        }

    #endregion

    #region M-1: mixed Focus

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> UGUI와 UITK 대상을 교대로 선택할 때 이전 native Focus를 비우고,
        /// <br/> 공통 Driver가 현재 기술의 실제 Focus를 반환하는지 검증한다.
        /// </summary>
        // ----------------------------------------------------------------------
        [UnityTest]
        public IEnumerator TEST_GameUIFocusDriver_UGUI와UITK교차선택_단일현재Focus유지()
        {
            var host = new GameObject("Mixed Focus Host");
            var eventSystem = host.AddComponent<EventSystem>();
            var document = host.AddComponent<UIDocument>();
            var panelSettings = ScriptableObject.CreateInstance<PanelSettings>();
            document.panelSettings = panelSettings;
            var uguiFocus = host.AddComponent<UGUIFocusDriver>();
            var uitkFocus = host.AddComponent<UITKFocusDriver>();
            var focus = host.AddComponent<GameUIFocusDriver>();
            SetField(uguiFocus, "eventSystem", eventSystem);
            SetField(focus, "uguiFocusDriver", uguiFocus);
            SetField(focus, "uitkFocusDriver", uitkFocus);

            var uguiTarget = new GameObject
            (
                "UGUI Focus",
                typeof(RectTransform),
                typeof(UnityEngine.UI.Button)
            );
            uguiTarget.transform.SetParent(host.transform, false);
            var uitkTarget = new Button { name = "UITK Focus" };
            document.rootVisualElement.Add(uitkTarget);

            try
            {
                yield return null;
                yield return null;
                focus.Initialize(eventSystem);

                focus.Select(uguiTarget);

                Assert.AreSame(uguiTarget, eventSystem.currentSelectedGameObject);
                Assert.AreSame(uguiTarget, focus.Current);

                focus.Select(uitkTarget);
                yield return null;

                Assert.AreNotSame(uguiTarget, eventSystem.currentSelectedGameObject);
                Assert.AreSame(uitkTarget, focus.Current);

                focus.Select(uguiTarget);

                Assert.AreSame(uguiTarget, eventSystem.currentSelectedGameObject);
                Assert.AreSame(uguiTarget, focus.Current);
                Assert.AreNotSame(uitkTarget, uitkFocus.Current);
            }
            finally
            {
                UnityEngine.Object.Destroy(host);
                UnityEngine.Object.Destroy(panelSettings);
            }
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> 한 Screen Stack에서 UGUI Screen 위에 UITK Screen을 열고 닫을 때,
        /// <br/> Source와 Layer Usage를 반환한 뒤 이전 UGUI Focus를 복원하는지 검증한다.
        /// </summary>
        // ----------------------------------------------------------------------
        [UnityTest]
        public IEnumerator TEST_ScreenController_UGUI에서UITK교차OpenClose_이전Focus와소유권복원()
        {
            var host = new GameObject("Mixed Screen Host");
            var eventSystem = host.AddComponent<EventSystem>();
            var document = host.AddComponent<UIDocument>();
            var panelSettings = ScriptableObject.CreateInstance<PanelSettings>();
            document.panelSettings = panelSettings;
            var uguiFocus = host.AddComponent<UGUIFocusDriver>();
            var uitkFocus = host.AddComponent<UITKFocusDriver>();
            var focus = host.AddComponent<GameUIFocusDriver>();
            SetField(uguiFocus, "eventSystem", eventSystem);
            SetField(focus, "uguiFocusDriver", uguiFocus);
            SetField(focus, "uitkFocusDriver", uitkFocus);

            var uguiLayerObject = new GameObject
            (
                "UGUI Layer",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(UGUILayerCanvas)
            );
            uguiLayerObject.transform.SetParent(host.transform, false);
            uguiLayerObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var uguiLayer = uguiLayerObject.GetComponent<UGUILayerCanvas>();
            SetField(uguiLayer, "root", uguiLayerObject.GetComponent<RectTransform>());
            SetField(uguiLayer, "canvas", uguiLayerObject.GetComponent<Canvas>());

            var uguiAsset = CreateLayerAsset("UGUI", 0);
            var uitkAsset = CreateLayerAsset("UITK", 1);
            var layerRegistry = new PresentationLayerRegistry();
            var screenRegistry = new ScreenRegistry(layerRegistry);
            var transitioner = new ImmediateTransitioner();
            var input = new TestInputDriver();
            ScreenRegistrationHandle uguiRegistration = null;
            ScreenRegistrationHandle uitkRegistration = null;
            PresentationLayerHandle uguiLayerHandle = null;
            PresentationLayerHandle uitkLayerHandle = null;
            ScreenController controller = null;

            try
            {
                yield return null;
                yield return null;
                focus.Initialize(eventSystem);

                uguiLayerHandle = layerRegistry.Register(uguiAsset, uguiLayer);
                uitkLayerHandle = layerRegistry.Register
                (
                    uitkAsset,
                    new TestUITKLayerDriver(document.rootVisualElement)
                );
                var uguiSource = new TestUGUIScreenSource();
                var uitkSource = new TestUITKScreenSource();
                uguiRegistration = screenRegistry.Register
                (
                    new ScreenOptions
                    (
                        "UGUI",
                        "UGUI",
                        openDuration: 0.0f,
                        closeDuration: 0.0f
                    ),
                    uguiSource
                );
                uitkRegistration = screenRegistry.Register
                (
                    new ScreenOptions
                    (
                        "UITK",
                        "UITK",
                        openDuration: 0.0f,
                        closeDuration: 0.0f
                    ),
                    uitkSource
                );
                controller = new ScreenController
                (
                    screenRegistry,
                    layerRegistry,
                    transitioner,
                    new FocusController(focus),
                    input
                );
                controller.Activate();

                var uguiResponse = controller.Open("UGUI");

                Assert.IsTrue(uguiResponse.Accepted);
                Assert.AreSame(uguiSource.DefaultFocus, focus.Current);
                Assert.IsTrue(uguiLayerHandle.HasConsumers);

                var uitkResponse = controller.Open("UITK");
                yield return null;

                Assert.IsTrue(uitkResponse.Accepted);
                Assert.AreSame(uitkSource.DefaultFocus, focus.Current);
                Assert.IsTrue(uitkLayerHandle.HasConsumers);
                Assert.AreEqual(0, uguiSource.ReleaseCount);

                Assert.IsTrue(uitkResponse.Session.Close());
                yield return null;

                Assert.AreEqual(1, uitkSource.ReleaseCount);
                Assert.IsFalse(uitkLayerHandle.HasConsumers);
                Assert.AreSame(uguiSource.DefaultFocus, focus.Current);

                controller.Clear();

                Assert.AreEqual(1, uguiSource.ReleaseCount);
                Assert.IsFalse(uguiLayerHandle.HasConsumers);
            }
            finally
            {
                controller?.Clear();
                uitkRegistration?.Dispose();
                uguiRegistration?.Dispose();
                input.Dispose();
                transitioner.Dispose();
                screenRegistry.Dispose();
                uitkLayerHandle?.Dispose();
                uguiLayerHandle?.Dispose();
                layerRegistry.Dispose();
                UnityEngine.Object.Destroy(host);
                UnityEngine.Object.Destroy(panelSettings);
                UnityEngine.Object.Destroy(uitkAsset);
                UnityEngine.Object.Destroy(uguiAsset);
            }
        }

    #endregion

    }
}
