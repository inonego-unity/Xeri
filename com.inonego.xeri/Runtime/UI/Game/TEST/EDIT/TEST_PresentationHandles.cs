/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TEST_PresentationHandles.cs
수정일 : 2026-07-31

# 설명
Modal·Drag Visual·Visibility·Overlay의 해제 및 Terminal 실패 계약을 검증한다.

# 테스트 구성
 M: Modal top 복원과 Terminal 정리
 D: Drag Visual 외부 파괴
 V: UGUI 표시 구성 검증
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
    /// Presentation Handle의 정상 해제와 Terminal 실패 처리 테스트.
    /// </summary>
    // ============================================================
    public sealed class TEST_PresentationHandles
    {
    #region 헬퍼

        // ============================================================
        /// <summary>
        /// Modal top 상태를 기록하는 backend.
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
            /// top 상태를 적용한다.
            /// </summary>
            // ------------------------------------------------------------
            public void SetTop(bool isTop)
            {
                IsTop = isTop;
            }
        }

        // ============================================================
        /// <summary>
        /// Dispose 호출을 기록한 뒤 예외를 던지는 Handle.
        /// </summary>
        // ============================================================
        private sealed class ThrowingHandle : IDisposable
        {
            // ------------------------------------------------------------
            /// <summary>
            /// Dispose 호출 횟수.
            /// </summary>
            // ------------------------------------------------------------
            public int DisposeCount { get; private set; }

            // ------------------------------------------------------------
            /// <summary>
            /// Dispose 부작용 뒤 실패를 주입한다.
            /// </summary>
            // ------------------------------------------------------------
            public void Dispose()
            {
                DisposeCount++;
                throw new InvalidOperationException("injected owned handle failure");
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
            /// 표시 상태 적용 호출 수.
            /// </summary>
            // ------------------------------------------------------------
            public int SetCount { get; private set; }

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
                SetCount++;
            }
        }

        // ============================================================
        /// <summary>
        /// Overlay Layer 등록에 사용할 테스트 backend.
        /// </summary>
        // ============================================================
        private sealed class TestLayerDriver : IPresentationLayerDriver<RectTransform>
        {
            // ------------------------------------------------------------
            /// <summary>
            /// 테스트 Layer Root.
            /// </summary>
            // ------------------------------------------------------------
            public RectTransform Root { get; }

            // ------------------------------------------------------------
            /// <summary>
            /// 지정 Root를 사용하는 backend를 생성한다.
            /// </summary>
            // ------------------------------------------------------------
            public TestLayerDriver(RectTransform root) : base()
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
            /// 테스트 Layer 순서는 별도로 기록하지 않는다.
            /// </summary>
            // ------------------------------------------------------------
            public void SetOrder(int order)
            {
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
            public object Acquire(IPresentationLayerDriver layer)
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

        // ============================================================
        /// <summary>
        /// 필수 View가 없는 GameObject를 반환하고 반환 정리도 실패시키는 Provider.
        /// </summary>
        // ============================================================
        private sealed class FailingReleaseProvider : IGameObjectProvider
        {
            // ------------------------------------------------------------
            /// <summary>
            /// 획득 인스턴스에 적용할 기본 부모.
            /// </summary>
            // ------------------------------------------------------------
            public Transform Parent
            {
                get;
                set;
            }

            // ------------------------------------------------------------
            /// <summary>
            /// 획득할 테스트 인스턴스.
            /// </summary>
            // ------------------------------------------------------------
            public GameObject Instance { get; set; }

            // ------------------------------------------------------------
            /// <summary>
            /// 누적 반환 시도 수.
            /// </summary>
            // ------------------------------------------------------------
            public int ReleaseCount { get; private set; }

            // ------------------------------------------------------------
            /// <summary>
            /// 반환 호출에서 예외를 발생시킬지 여부.
            /// </summary>
            // ------------------------------------------------------------
            public bool FailOnRelease { get; set; } = true;

            // ------------------------------------------------------------
            /// <summary>
            /// 지정된 테스트 인스턴스를 반환한다.
            /// </summary>
            // ------------------------------------------------------------
            public GameObject Acquire(bool worldPositionStays = true)
            {
                return Instance;
            }

            // ------------------------------------------------------------
            /// <summary>
            /// 이 Fixture에서 지원하지 않는 비동기 획득을 거부한다.
            /// </summary>
            // ------------------------------------------------------------
            public Awaitable<GameObject> AcquireAsync(bool worldPositionStays = true)
            {
                throw new NotSupportedException();
            }

            // ------------------------------------------------------------
            /// <summary>
            /// 반환 시도를 기록하고 설정된 실패를 발생시킨다.
            /// </summary>
            // ------------------------------------------------------------
            public void Release
            (
                GameObject gameObject,
                bool worldPositionStays = true
            )
            {
                ReleaseCount++;

                if (FailOnRelease)
                {
                    throw new InvalidOperationException("injected overlay provider release failure");
                }
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
        /// 소유 Handle 정리가 실패해도 이전 top을 복원하고 현재 ModalHandle을 Terminal화하는지 검증한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_ModalController_소유Handle정리실패_이전Top복원과HandleTerminal()
        {
            var controller = new ModalController();
            var previousDriver = new TestModalDriver();
            var currentDriver = new TestModalDriver();
            var child = new ThrowingHandle();
            var previous = controller.Open(previousDriver);
            var current = controller.Open(currentDriver, child);

            Assert.Throws<AggregateException>(current.Dispose);
            Assert.AreEqual(1, controller.Count);
            Assert.IsTrue(previousDriver.IsTop);
            Assert.IsFalse(currentDriver.IsTop);
            Assert.IsTrue(current.IsDisposed);
            Assert.AreEqual(1, child.DisposeCount);

            Assert.DoesNotThrow(current.Dispose);

            Assert.IsTrue(current.IsDisposed);
            Assert.AreEqual(1, child.DisposeCount);
            Assert.AreEqual(1, controller.Count);

            previous.Dispose();
            controller.Dispose();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Controller 종료가 남은 ModalHandle과 자식 Lease를 함께 Terminal로 종료하는지 검증한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_ModalController_Controller종료_남은HandleTerminal()
        {
            var controller = new ModalController();
            var driver = new TestModalDriver();
            var childDisposeCount = 0;
            var child = new Lease(() => childDisposeCount++);
            var handle = controller.Open(driver, child);

            Assert.DoesNotThrow(controller.Dispose);
            Assert.AreEqual(0, controller.Count);
            Assert.IsTrue(handle.IsDisposed);
            Assert.IsFalse(driver.IsTop);
            Assert.AreEqual(1, childDisposeCount);

            Assert.DoesNotThrow(handle.Dispose);
            Assert.AreEqual(1, childDisposeCount);
        }

    #endregion

    #region C-1: 중첩 Core 요청

        // ------------------------------------------------------------
        /// <summary>
        /// 중간 Visibility 요청을 해제해도 최신 요청과 기준 복원이 유지되는지 검증한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_VisibilityController_중첩요청중간해제_최신값과기준복원()
        {
            var visibility = new VisibilityController();
            var target = new TestVisibilityTarget(false);
            var firstVisibility = visibility.Set(target, false);
            var secondVisibility = visibility.Set(target, true);

            Assert.AreEqual(2, target.SetCount);

            firstVisibility.Dispose();

            Assert.IsTrue(target.IsVisible);
            Assert.AreEqual(2, target.SetCount);

            secondVisibility.Dispose();

            Assert.IsFalse(target.IsVisible);
            Assert.AreEqual(3, target.SetCount);
            visibility.Dispose();
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
            var root = new GameObject("Overlay Layer", typeof(RectTransform));
            root.transform.SetParent(parent.transform, false);
            ownedObjects.Add(parent);
            ownedObjects.Add(root);
            var registry = new PresentationLayerRegistry();
            var layerHandle = registry.Register
            (
                CreateLayerAsset(),
                new TestLayerDriver(root.GetComponent<RectTransform>())
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

        // ------------------------------------------------------------
        /// <summary>
        /// View 반환 실패 뒤 Source 종료가 같은 Provider 반환을 반복하지 않는지 검증한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_GameObjectProviderOverlaySource_View반환실패_Source종료에서반복하지않음()
        {
            var parent = new GameObject("Overlay Parent", typeof(RectTransform));
            var instance = new GameObject("Overlay View");
            instance.AddComponent<UGUIScreenDriver>();
            ownedObjects.Add(parent);
            ownedObjects.Add(instance);
            var provider = new FailingReleaseProvider { Instance = instance };
            var source = new GameObjectProviderOverlaySource<IVisibilityTarget>(provider);
            var view = source.Acquire
            (
                new TestLayerDriver(parent.GetComponent<RectTransform>())
            );

            Assert.Throws<InvalidOperationException>(() => source.Release(view));
            Assert.AreEqual(1, provider.ReleaseCount);

            provider.FailOnRelease = false;
            Assert.DoesNotThrow(source.Dispose);
            Assert.AreEqual(1, provider.ReleaseCount);
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// Source가 먼저 물리 소유권을 종료해도 남은 Overlay Handle은 오류 없이 Layer 사용을 끝낸다.
        /// </summary>
        // ----------------------------------------------------------------------
        [Test]
        public void TEST_GameObjectProviderOverlaySource_Source선종료_남은HandleTerminal()
        {
            var parent = new GameObject("Layer Parent");
            var root = new GameObject("Overlay Layer", typeof(RectTransform));
            var instance = new GameObject("Overlay View");
            root.transform.SetParent(parent.transform, false);
            instance.AddComponent<UGUIScreenDriver>();
            ownedObjects.Add(parent);
            ownedObjects.Add(root);
            ownedObjects.Add(instance);
            var provider = new FailingReleaseProvider
            {
                Instance = instance,
                FailOnRelease = false,
            };
            var source = new GameObjectProviderOverlaySource<UGUIScreenDriver>(provider);
            var registry = new PresentationLayerRegistry();
            var layerHandle = registry.Register
            (
                CreateLayerAsset(),
                new TestLayerDriver(root.GetComponent<RectTransform>())
            );
            var handle = OverlayHandle<UGUIScreenDriver>.Acquire
            (
                registry,
                "Overlay",
                source
            );

            source.Dispose();

            Assert.AreEqual(1, provider.ReleaseCount);
            Assert.DoesNotThrow(handle.Dispose);
            Assert.IsTrue(handle.IsDisposed);
            Assert.IsFalse(layerHandle.HasConsumers);
            Assert.AreEqual(1, provider.ReleaseCount);

            layerHandle.Dispose();
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

    #region D-1: Drag Visual 직접 Root

        // ------------------------------------------------------------
        /// <summary>
        /// 직접 Root Begin이 종료 시 원래 부모와 sibling을 복원하는지 검증한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_DragVisualHandle_직접Root_원래Hierarchy복원()
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
