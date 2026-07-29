/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TEST_PresentationHandles.cs
수정일 : 2026-07-29

# 설명
Modal top 복원 재시도와 파괴된 Drag Visual의 소유 연결 해제를 검증한다.

# 테스트 구성
 M: Modal top 복원과 정리 재시도
 D: Drag Visual 외부 파괴
 V: UGUI 표시 backend 구성 검증
 C: 중첩 Core 요청과 Overlay 롤백
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections.Generic;
using System.Reflection;

using UnityEngine;

using NUnit.Framework;

namespace inonego.Xeri.TEST.UI._Game
{
    using inonego.Xeri.UI.Game;

    // ============================================================
    /// <summary>
    /// Presentation Handle의 실패 재시도와 소유권 종결 테스트.
    /// </summary>
    // ============================================================
    public sealed class TEST_PresentationHandles
    {
    #region 헬퍼

        // ============================================================
        /// <summary>
        /// top 복원 실패를 한 번 주입하는 Modal backend.
        /// </summary>
        // ============================================================
        private sealed class TestModalDriver : IModalDriver
        {
            // ------------------------------------------------------------
            /// <summary>
            /// 현재 top 적용 상태.
            /// </summary>
            // ------------------------------------------------------------
            public bool IsTop { get; private set; }

            // ------------------------------------------------------------
            /// <summary>
            /// 다음 top 활성화를 실패시킬지 여부.
            /// </summary>
            // ------------------------------------------------------------
            public bool FailNextActivation { get; set; }

            // ------------------------------------------------------------
            /// <summary>
            /// 다음 top 비활성화를 실패시킬지 여부.
            /// </summary>
            // ------------------------------------------------------------
            public bool FailNextDeactivation { get; set; }

            // ------------------------------------------------------------
            /// <summary>
            /// top 상태를 적용하고 요청된 실패를 한 번 발생시킨다.
            /// </summary>
            // ------------------------------------------------------------
            public void SetTop(bool isTop)
            {
                if (isTop && FailNextActivation)
                {
                    FailNextActivation = false;
                    throw new InvalidOperationException("injected modal restore failure");
                }

                if (!isTop && FailNextDeactivation)
                {
                    FailNextDeactivation = false;
                    throw new InvalidOperationException("injected modal deactivation failure");
                }

                IsTop = isTop;
            }
        }

        // ============================================================
        /// <summary>
        /// 첫 Dispose만 실패하고 다음 호출에서 정상 종료되는 Handle.
        /// </summary>
        // ============================================================
        private sealed class FailOnceHandle : IDisposable
        {
            // ------------------------------------------------------------
            /// <summary>
            /// Dispose 호출 횟수.
            /// </summary>
            // ------------------------------------------------------------
            public int DisposeCount { get; private set; }

            // ------------------------------------------------------------
            /// <summary>
            /// 첫 호출에 실패를 주입하고 이후 호출은 성공한다.
            /// </summary>
            // ------------------------------------------------------------
            public void Dispose()
            {
                DisposeCount++;

                if (DisposeCount == 1)
                {
                    throw new InvalidOperationException("injected owned handle failure");
                }
            }
        }

        // ============================================================
        /// <summary>
        /// Visibility 상태를 기록하는 테스트 Target.
        /// </summary>
        // ============================================================
        private sealed class TestVisibilityTarget : IVisibilityTarget
        {
            // ------------------------------------------------------------
            /// <summary>
            /// 현재 표시 상태.
            /// </summary>
            // ------------------------------------------------------------
            public bool IsVisible { get; private set; }

            // ------------------------------------------------------------
            /// <summary>
            /// 기준 표시 상태를 지정한다.
            /// </summary>
            // ------------------------------------------------------------
            public TestVisibilityTarget(bool visible) : base()
            {
                IsVisible = visible;
            }

            // ------------------------------------------------------------
            /// <summary>
            /// 표시 상태를 기록한다.
            /// </summary>
            // ------------------------------------------------------------
            public void SetVisible(bool visible)
            {
                IsVisible = visible;
            }
        }

        // ============================================================
        /// <summary>
        /// Overlay Layer 등록에 사용할 테스트 backend.
        /// </summary>
        // ============================================================
        private sealed class TestLayerDriver : IPresentationLayerDriver
        {
            // ------------------------------------------------------------
            /// <summary>
            /// 테스트 Layer Root.
            /// </summary>
            // ------------------------------------------------------------
            public Transform Root { get; }

            // ------------------------------------------------------------
            /// <summary>
            /// 지정 Root를 사용하는 backend를 생성한다.
            /// </summary>
            // ------------------------------------------------------------
            public TestLayerDriver(Transform root) : base()
            {
                Root = root;
            }

            // ------------------------------------------------------------
            /// <summary>
            /// Asset과 Root 존재 여부를 검증한다.
            /// </summary>
            // ------------------------------------------------------------
            public bool Validate
            (
                PresentationLayerAsset asset,
                out string error
            )
            {
                error = asset == null || Root == null ? "invalid" : "";
                return string.IsNullOrEmpty(error);
            }

            // ------------------------------------------------------------
            /// <summary>
            /// 테스트에서는 별도 활성 상태를 기록하지 않는다.
            /// </summary>
            // ------------------------------------------------------------
            public void SetActive(bool active)
            {
            }
        }

        // ============================================================
        /// <summary>
        /// 획득 단계에서 실패하는 Overlay Source.
        /// </summary>
        // ============================================================
        private sealed class FailingOverlaySource : IOverlaySource<object>
        {
            // ------------------------------------------------------------
            /// <summary>
            /// Overlay 획득 실패를 주입한다.
            /// </summary>
            // ------------------------------------------------------------
            public object Acquire(Transform parent)
            {
                throw new InvalidOperationException("injected overlay acquire failure");
            }

            // ------------------------------------------------------------
            /// <summary>
            /// 획득되지 않은 View는 반환되지 않는다.
            /// </summary>
            // ------------------------------------------------------------
            public void Release(object view)
            {
                throw new InvalidOperationException("unreachable");
            }
        }

        private readonly List<UnityEngine.Object> ownedObjects =
            new List<UnityEngine.Object>();

        // ------------------------------------------------------------
        /// <summary>
        /// 테스트 Presentation Layer Asset을 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        private PresentationLayerAsset CreateLayerAsset()
        {
            var asset = ScriptableObject.CreateInstance<PresentationLayerAsset>();
            SetField(asset, "id", "Overlay");
            SetField(asset, "mode", PresentationLayerMode.Shared);
            SetField(asset, "order", 0);
            ownedObjects.Add(asset);
            return asset;
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
        /// 테스트에서 만든 GameObject를 역순 제거한다.
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

    #region M-1: Modal top 복원

        // ------------------------------------------------------------
        /// <summary>
        /// 기존 top 비활성화 실패가 새 Modal을 공개하지 않고 이전 top을 복원하는지 검증한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_ModalController_기존Top비활성화실패_이전Stack복원()
        {
            var controller = new ModalController();
            var previousDriver = new TestModalDriver();
            var currentDriver = new TestModalDriver();
            var previous = controller.Open(previousDriver);
            previousDriver.FailNextDeactivation = true;

            Assert.Throws<AggregateException>(() => controller.Open(currentDriver));
            Assert.AreEqual(1, controller.Count);
            Assert.IsTrue(previousDriver.IsTop);
            Assert.IsFalse(currentDriver.IsTop);

            previous.Dispose();
            controller.Dispose();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 이전 Modal top 복원 실패가 Stack 제거 전에 남아 같은 Handle로 재시도되는지 검증한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_ModalController_이전Top복원실패_Handle재시도()
        {
            var controller = new ModalController();
            var previousDriver = new TestModalDriver();
            var currentDriver = new TestModalDriver();
            var previous = controller.Open(previousDriver);
            var current = controller.Open(currentDriver);
            previousDriver.FailNextActivation = true;

            Assert.Throws<InvalidOperationException>(current.Dispose);
            Assert.AreEqual(2, controller.Count);
            Assert.IsFalse(current.IsDisposed);
            Assert.IsTrue(currentDriver.IsTop);
            Assert.IsFalse(previousDriver.IsTop);

            current.Dispose();

            Assert.AreEqual(1, controller.Count);
            Assert.IsTrue(current.IsDisposed);
            Assert.IsTrue(previousDriver.IsTop);

            previous.Dispose();
            controller.Dispose();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Stack 제거 뒤 실패한 소유 Handle만 같은 ModalHandle에서 재시도되는지 검증한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_ModalController_소유Handle정리실패_공개Stack제거후정리만재시도()
        {
            var controller = new ModalController();
            var previousDriver = new TestModalDriver();
            var currentDriver = new TestModalDriver();
            var child = new FailOnceHandle();
            var previous = controller.Open(previousDriver);
            var current = controller.Open(currentDriver, child);

            Assert.Throws<AggregateException>(current.Dispose);
            Assert.AreEqual(1, controller.Count);
            Assert.IsTrue(previousDriver.IsTop);
            Assert.IsFalse(currentDriver.IsTop);
            Assert.IsFalse(current.IsDisposed);
            Assert.AreEqual(1, child.DisposeCount);

            current.Dispose();

            Assert.IsTrue(current.IsDisposed);
            Assert.AreEqual(2, child.DisposeCount);
            Assert.AreEqual(1, controller.Count);

            previous.Dispose();
            controller.Dispose();
        }

    #endregion

    #region C-1: 중첩 Core 요청

        // ------------------------------------------------------------
        /// <summary>
        /// 중간 Visibility·Override 요청을 해제해도 최신 요청과 기준 복원이 유지되는지 검증한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_PresentationCore_중첩요청중간해제_최신값과기준복원()
        {
            var visibility = new VisibilityController();
            var target = new TestVisibilityTarget(false);
            var firstVisibility = visibility.Set(target, true);
            var secondVisibility = visibility.Set(target, false);
            var value = 1;
            var presentationOverride = new PresentationOverrideController<int>
            (
                () => value,
                next => value = next
            );
            var firstOverride = presentationOverride.Set(2);
            var secondOverride = presentationOverride.Set(3);

            firstVisibility.Dispose();
            firstOverride.Dispose();

            Assert.IsFalse(target.IsVisible);
            Assert.AreEqual(3, value);

            secondVisibility.Dispose();
            secondOverride.Dispose();

            Assert.IsFalse(target.IsVisible);
            Assert.AreEqual(1, value);
            visibility.Dispose();
            presentationOverride.Dispose();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Overlay 획득 실패가 Layer 소비 수명을 남기지 않는지 검증한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_OverlayHandle_획득실패_Layer소비수명롤백()
        {
            var parent = new GameObject("Layer Parent");
            var root = new GameObject("Overlay Layer");
            root.transform.SetParent(parent.transform, false);
            ownedObjects.Add(parent);
            ownedObjects.Add(root);
            var registry = new PresentationLayerRegistry();
            var layerHandle = registry.Register
            (
                CreateLayerAsset(),
                new TestLayerDriver(root.transform)
            );

            Assert.Throws<InvalidOperationException>
            (
                () => OverlayHandle<object>.Acquire
                (
                    registry,
                    "Overlay",
                    new FailingOverlaySource()
                )
            );

            Assert.IsFalse(layerHandle.HasConsumers);
            Assert.DoesNotThrow(layerHandle.Dispose);
            registry.Dispose();
        }

    #endregion

    #region M-2: 정상 Modal Stack

        // ------------------------------------------------------------
        /// <summary>
        /// Modal Stack이 마지막 Modal만 top으로 두고 해제 시 이전 top을 복원하는지 검증한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_ModalController_정상Stack_마지막Top과이전Top복원()
        {
            var controller = new ModalController();
            var firstDriver = new TestModalDriver();
            var secondDriver = new TestModalDriver();
            var first = controller.Open(firstDriver);
            var second = controller.Open(secondDriver);

            Assert.IsFalse(firstDriver.IsTop);
            Assert.IsTrue(secondDriver.IsTop);

            second.Dispose();

            Assert.IsTrue(firstDriver.IsTop);
            Assert.IsFalse(secondDriver.IsTop);

            first.Dispose();
            Assert.AreEqual(0, controller.Count);
            controller.Dispose();
        }

    #endregion

    #region D-1: Drag Visual 외부 파괴

        // ------------------------------------------------------------
        /// <summary>
        /// 대상 RectTransform이 파괴돼도 Handle과 Controller 연결이 종결되는지 검증한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_DragVisualHandle_대상외부파괴_Controller소유연결종결()
        {
            var originalParent = new GameObject("Original", typeof(RectTransform));
            var dragRoot = new GameObject("Drag Root", typeof(RectTransform));
            var target = new GameObject("Target", typeof(RectTransform));
            ownedObjects.Add(originalParent);
            ownedObjects.Add(dragRoot);
            ownedObjects.Add(target);
            target.transform.SetParent(originalParent.transform, false);

            var controller = new DragVisualController();
            var handle = controller.Begin
            (
                target.GetComponent<RectTransform>(),
                dragRoot.GetComponent<RectTransform>()
            );

            UnityEngine.Object.DestroyImmediate(target);

            Assert.IsFalse(handle.IsDisposed);

            handle.Dispose();

            Assert.IsTrue(handle.IsDisposed);
            Assert.DoesNotThrow(controller.Dispose);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Drag Visual 종료가 원래 부모, sibling과 RectTransform pose를 복원하는지 검증한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_DragVisualHandle_정상종료_원래Hierarchy와Pose복원()
        {
            var originalParent = new GameObject("Original", typeof(RectTransform));
            var sibling = new GameObject("Sibling", typeof(RectTransform));
            var dragRoot = new GameObject("Drag Root", typeof(RectTransform));
            var target = new GameObject("Target", typeof(RectTransform));
            ownedObjects.Add(originalParent);
            ownedObjects.Add(sibling);
            ownedObjects.Add(dragRoot);
            ownedObjects.Add(target);
            sibling.transform.SetParent(originalParent.transform, false);
            target.transform.SetParent(originalParent.transform, false);
            var rect = target.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.1f, 0.2f);
            rect.anchorMax = new Vector2(0.7f, 0.8f);
            rect.pivot = new Vector2(0.3f, 0.4f);
            rect.anchoredPosition = new Vector2(12.0f, 34.0f);
            rect.sizeDelta = new Vector2(56.0f, 78.0f);
            rect.localRotation = Quaternion.Euler(0.0f, 0.0f, 15.0f);
            rect.localScale = new Vector3(1.2f, 0.8f, 1.0f);
            var originalSibling = rect.GetSiblingIndex();
            var controller = new DragVisualController();
            var handle = controller.Begin
            (
                rect,
                dragRoot.GetComponent<RectTransform>()
            );

            handle.Dispose();

            Assert.AreSame(originalParent.transform, rect.parent);
            Assert.AreEqual(originalSibling, rect.GetSiblingIndex());
            Assert.AreEqual(new Vector2(0.1f, 0.2f), rect.anchorMin);
            Assert.AreEqual(new Vector2(0.7f, 0.8f), rect.anchorMax);
            Assert.AreEqual(new Vector2(0.3f, 0.4f), rect.pivot);
            Assert.AreEqual(new Vector2(12.0f, 34.0f), rect.anchoredPosition);
            Assert.AreEqual(new Vector2(56.0f, 78.0f), rect.sizeDelta);
            Assert.AreEqual(Quaternion.Euler(0.0f, 0.0f, 15.0f), rect.localRotation);
            Assert.AreEqual(new Vector3(1.2f, 0.8f, 1.0f), rect.localScale);
            controller.Dispose();
        }

    #endregion

    #region V-1: UGUI backend 구성

        // ------------------------------------------------------------
        /// <summary>
        /// CanvasGroup 없는 UGUI Modal이 입력 상태 적용을 성공으로 숨기지 않는지 검증한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_UGUIModalDriver_CanvasGroup누락_명시적실패()
        {
            var gameObject = new GameObject("Modal Driver");
            ownedObjects.Add(gameObject);
            var driver = gameObject.AddComponent<UGUIModalDriver>();

            Assert.Throws<InvalidOperationException>(() => driver.SetTop(true));
        }

        // ------------------------------------------------------------
        /// <summary>
        /// UGUI Modal top 상태가 상호작용, raycast와 Dim Root에 함께 적용되는지 검증한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_UGUIModalDriver_정상Top_Interaction과Dim동기화()
        {
            var gameObject = new GameObject("Modal Driver", typeof(CanvasGroup));
            var dim = new GameObject("Dim");
            dim.transform.SetParent(gameObject.transform, false);
            ownedObjects.Add(gameObject);
            ownedObjects.Add(dim);
            var driver = gameObject.AddComponent<UGUIModalDriver>();
            var canvasGroup = gameObject.GetComponent<CanvasGroup>();
            SetField(driver, "canvasGroup", canvasGroup);
            SetField(driver, "dimRoot", dim);

            driver.SetTop(true);

            Assert.IsTrue(canvasGroup.interactable);
            Assert.IsTrue(canvasGroup.blocksRaycasts);
            Assert.IsTrue(dim.activeSelf);

            driver.SetTop(false);

            Assert.IsFalse(canvasGroup.interactable);
            Assert.IsFalse(canvasGroup.blocksRaycasts);
            Assert.IsFalse(dim.activeSelf);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 표시 참조가 모두 없는 UGUI Blocker가 점유 Handle을 반환하지 않는지 검증한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_UGUIInteractionBlocker_표시참조누락_명시적실패()
        {
            var gameObject = new GameObject("Interaction Blocker");
            ownedObjects.Add(gameObject);
            var blocker = gameObject.AddComponent<UGUIInteractionBlocker>();

            Assert.Throws<InvalidOperationException>(() => blocker.Acquire());
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 중첩 Blocker 하나를 해제해도 남은 점유가 유지되고 마지막 해제만 숨기는지 검증한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_UGUIInteractionBlocker_중첩점유_마지막해제만비활성()
        {
            var gameObject = new GameObject("Interaction Blocker");
            var root = new GameObject("Blocker Root");
            root.transform.SetParent(gameObject.transform, false);
            ownedObjects.Add(gameObject);
            ownedObjects.Add(root);
            var blocker = gameObject.AddComponent<UGUIInteractionBlocker>();
            SetField(blocker, "root", root);
            root.SetActive(false);

            var first = blocker.Acquire();
            var second = blocker.Acquire();
            first.Dispose();

            Assert.IsTrue(root.activeSelf);

            second.Dispose();

            Assert.IsFalse(root.activeSelf);
        }

    #endregion

    }
}
