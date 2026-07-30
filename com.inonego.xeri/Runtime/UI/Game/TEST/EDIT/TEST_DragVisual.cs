/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TEST_DragVisual.cs
수정일 : 2026-07-30

# 설명
기존 DraggableUI와 UGUI Drag Visual의 좌표·Layer Usage·종료 수명 연동을 검증한다.

# 테스트 구성
 L: Presentation Layer Usage와 pose 복원
 B: DraggableUI Begin·End·Cancel 연결
 R: Runtime 종료 안전 경계
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections.Generic;
using System.Reflection;

using UnityEngine;
using UnityEngine.EventSystems;

using NUnit.Framework;

using inonego.Xeri.UI.DragDrop;
using inonego.Xeri.UI.Game;

namespace inonego.Xeri.TEST.UI._Game
{
    // ============================================================
    /// <summary>
    /// UGUI Drag Visual과 기존 Drag_Drop 연동 계약 테스트.
    /// </summary>
    // ============================================================
    public sealed class TEST_DragVisual
    {
    #region 헬퍼

        // ============================================================
        /// <summary>
        /// 테스트 RectTransform을 Presentation Layer backend로 제공한다.
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
            /// 지정 Root로 테스트 Driver를 생성한다.
            /// </summary>
            // ------------------------------------------------------------
            public TestLayerDriver(Transform root) : base()
            {
                Root = root;
            }

            // ------------------------------------------------------------
            /// <summary>
            /// 테스트 Layer 구성을 유효한 것으로 반환한다.
            /// </summary>
            // ------------------------------------------------------------
            public bool Validate(PresentationLayerAsset asset, out string error)
            {
                error = "";
                return true;
            }

            // ------------------------------------------------------------
            /// <summary>
            /// 테스트 Layer Root의 활성 상태를 적용한다.
            /// </summary>
            // ------------------------------------------------------------
            public void SetActive(bool active)
            {
                if (Root != null)
                {
                    Root.gameObject.SetActive(active);
                }
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 직렬화 private 필드에 테스트 값을 지정한다.
        /// </summary>
        // ------------------------------------------------------------
        private static void SetField
        (
            object target,
            string fieldName,
            object value
        )
        {
            var field = target.GetType().GetField
            (
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic
            );

            Assert.IsNotNull(field, $"필드 '{fieldName}'를 찾지 못했습니다.");
            field.SetValue(target, value);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// EventSystem Pointer 입력 데이터를 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        private static PointerEventData CreateEventData(Vector2 position)
        {
            return new PointerEventData(EventSystem.current)
            {
                button = PointerEventData.InputButton.Left,
                pointerId = 1,
                position = position,
            };
        }

    #endregion

    #region 픽스처

        private readonly List<UnityEngine.Object> ownedObjects =
            new List<UnityEngine.Object>();

        private PresentationLayerRegistry registry = null;
        private PresentationLayerHandle layerHandle = null;
        private DragVisualController controller = null;
        private IDisposable binding = null;
        private RectTransform originalParent = null;
        private RectTransform layerRoot = null;
        private RectTransform target = null;
        private DraggableUI draggable = null;

        // ----------------------------------------------------------------------
        /// <summary>
        /// 서로 다른 좌표계의 원래 부모와 Drag Layer, DraggableUI를 준비한다.
        /// </summary>
        // ----------------------------------------------------------------------
        [SetUp]
        public void SetUp()
        {
            var eventSystemObject = new GameObject("EventSystem");
            eventSystemObject.AddComponent<EventSystem>();
            ownedObjects.Add(eventSystemObject);

            var canvasRoot = new GameObject("Canvas Root", typeof(RectTransform));
            ownedObjects.Add(canvasRoot);

            var originalObject = new GameObject("Original Parent", typeof(RectTransform));
            ownedObjects.Add(originalObject);
            originalParent = originalObject.GetComponent<RectTransform>();
            originalParent.SetParent(canvasRoot.transform, false);
            originalParent.anchoredPosition = new Vector2(-120.0f, 40.0f);
            originalParent.localScale = new Vector3(1.5f, 0.75f, 1.0f);

            var layerObject = new GameObject("Drag Layer", typeof(RectTransform));
            ownedObjects.Add(layerObject);
            layerRoot = layerObject.GetComponent<RectTransform>();
            layerRoot.SetParent(canvasRoot.transform, false);
            layerRoot.anchoredPosition = new Vector2(80.0f, -60.0f);
            layerRoot.localScale = new Vector3(0.5f, 2.0f, 1.0f);

            var targetObject = new GameObject
            (
                "Draggable",
                typeof(RectTransform),
                typeof(CanvasGroup),
                typeof(DraggableUI)
            );
            ownedObjects.Add(targetObject);
            target = targetObject.GetComponent<RectTransform>();
            target.SetParent(originalParent, false);
            target.anchorMin = new Vector2(0.2f, 0.3f);
            target.anchorMax = new Vector2(0.7f, 0.8f);
            target.pivot = new Vector2(0.4f, 0.6f);
            target.anchoredPosition = new Vector2(25.0f, 35.0f);
            target.sizeDelta = new Vector2(70.0f, 90.0f);
            target.localRotation = Quaternion.Euler(0.0f, 0.0f, 12.0f);
            target.localScale = new Vector3(1.1f, 0.9f, 1.0f);
            draggable = targetObject.GetComponent<DraggableUI>();

            var asset = ScriptableObject.CreateInstance<PresentationLayerAsset>();
            ownedObjects.Add(asset);
            SetField(asset, "id", "Drag");

            registry = new PresentationLayerRegistry();
            layerHandle = registry.Register(asset, new TestLayerDriver(layerRoot));
            controller = new DragVisualController(registry);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Drag 연결과 Layer 소유권을 역순으로 정리한다.
        /// </summary>
        // ------------------------------------------------------------
        [TearDown]
        public void TearDown()
        {
            binding?.Dispose();
            controller?.Dispose();
            layerHandle?.Dispose();
            registry?.Dispose();

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

    #region L-1: Layer Usage와 pose 복원

        // ----------------------------------------------------------------------
        /// <summary>
        /// Layer 기반 Begin과 Dispose가 Usage와 전체 RectTransform pose를 대칭으로 복원한다.
        /// </summary>
        // ----------------------------------------------------------------------
        [Test]
        public void TEST_DragVisualController_BeginDispose_LayerUsage와Pose복원()
        {
            var originalSibling = target.GetSiblingIndex();
            var parameters = new DragVisualParams(target, "Drag");

            var handle = controller.Begin(parameters);

            Assert.AreSame(layerRoot, target.parent);
            Assert.IsTrue(layerHandle.HasConsumers);

            handle.Dispose();

            Assert.IsTrue(handle.IsDisposed);
            Assert.IsFalse(layerHandle.HasConsumers);
            Assert.AreSame(originalParent, target.parent);
            Assert.AreEqual(originalSibling, target.GetSiblingIndex());
            Assert.AreEqual(new Vector2(0.2f, 0.3f), target.anchorMin);
            Assert.AreEqual(new Vector2(0.7f, 0.8f), target.anchorMax);
            Assert.AreEqual(new Vector2(0.4f, 0.6f), target.pivot);
            Assert.AreEqual(new Vector2(25.0f, 35.0f), target.anchoredPosition);
            Assert.AreEqual(new Vector2(70.0f, 90.0f), target.sizeDelta);
            Assert.AreEqual(Quaternion.Euler(0.0f, 0.0f, 12.0f), target.localRotation);
            Assert.AreEqual(new Vector3(1.1f, 0.9f, 1.0f), target.localScale);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 외부에서 Drag 대상을 파괴해도 Handle이 Layer Usage를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_DragVisualHandle_대상외부파괴_LayerUsage반환()
        {
            var handle = controller.Begin(new DragVisualParams(target, "Drag"));

            UnityEngine.Object.DestroyImmediate(target.gameObject);

            Assert.DoesNotThrow(handle.Dispose);
            Assert.IsTrue(handle.IsDisposed);
            Assert.IsFalse(layerHandle.HasConsumers);
        }

    #endregion

    #region B-1: Draggable Begin과 End

        // ----------------------------------------------------------------------
        /// <summary>
        /// 계층 승격 직후 같은 Pointer 위치의 첫 Drag가 현재 화면 위치를 유지한다.
        /// </summary>
        // ----------------------------------------------------------------------
        [Test]
        public void TEST_DragVisualBinding_부모변경후첫Drag_Pointer위치유지()
        {
            binding = controller.Bind
            (
                draggable,
                new DragVisualParams(target, "Drag")
            );
            var eventData = CreateEventData(new Vector2(100.0f, 120.0f));

            ExecuteEvents.Execute
            (
                target.gameObject,
                eventData,
                ExecuteEvents.pointerDownHandler
            );
            ExecuteEvents.Execute
            (
                target.gameObject,
                eventData,
                ExecuteEvents.beginDragHandler
            );
            var positionAfterBegin = target.position;

            ExecuteEvents.Execute
            (
                target.gameObject,
                eventData,
                ExecuteEvents.dragHandler
            );

            Assert.AreSame(layerRoot, target.parent);
            Assert.IsTrue(draggable.IsDragging);
            Assert.IsTrue(layerHandle.HasConsumers);
            Assert.Less
            (
                Vector3.Distance(positionAfterBegin, target.position),
                0.001f
            );

            ExecuteEvents.Execute
            (
                target.gameObject,
                eventData,
                ExecuteEvents.endDragHandler
            );

            Assert.IsFalse(draggable.IsDragging);
            Assert.AreSame(originalParent, target.parent);
            Assert.IsFalse(layerHandle.HasConsumers);
            Assert.IsTrue(target.GetComponent<CanvasGroup>().blocksRaycasts);
        }

    #endregion

    #region B-2: 연결 선종료

        // ------------------------------------------------------------
        /// <summary>
        /// 활성 Drag 중 Binding을 닫으면 Cancel, Raycast와 Visual 수명이 함께 종료된다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_DragVisualBinding_활성Drag중Dispose_Cancel과Visual종료()
        {
            binding = controller.Bind
            (
                draggable,
                new DragVisualParams(target, "Drag")
            );
            var eventData = CreateEventData(new Vector2(100.0f, 120.0f));

            ExecuteEvents.Execute
            (
                target.gameObject,
                eventData,
                ExecuteEvents.pointerDownHandler
            );
            ExecuteEvents.Execute
            (
                target.gameObject,
                eventData,
                ExecuteEvents.beginDragHandler
            );

            binding.Dispose();
            binding = null;

            Assert.IsFalse(draggable.IsDragging);
            Assert.AreSame(originalParent, target.parent);
            Assert.IsFalse(layerHandle.HasConsumers);
            Assert.IsTrue(target.GetComponent<CanvasGroup>().blocksRaycasts);
        }

    #endregion

    #region R-1: Controller 종료

        // ------------------------------------------------------------
        /// <summary>
        /// Runtime 안전 경계인 Controller 종료가 활성 Binding과 Layer Usage를 함께 정리한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_DragVisualController_활성Binding중Dispose_Drag과LayerUsage종료()
        {
            binding = controller.Bind
            (
                draggable,
                new DragVisualParams(target, "Drag")
            );
            var eventData = CreateEventData(new Vector2(100.0f, 120.0f));

            ExecuteEvents.Execute
            (
                target.gameObject,
                eventData,
                ExecuteEvents.pointerDownHandler
            );
            ExecuteEvents.Execute
            (
                target.gameObject,
                eventData,
                ExecuteEvents.beginDragHandler
            );

            controller.Dispose();
            controller = null;
            binding = null;

            Assert.IsFalse(draggable.IsDragging);
            Assert.AreSame(originalParent, target.parent);
            Assert.IsFalse(layerHandle.HasConsumers);
            Assert.IsTrue(target.GetComponent<CanvasGroup>().blocksRaycasts);
        }

    #endregion

    }
}
