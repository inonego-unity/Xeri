/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TEST_UITKDragDrop.cs
수정일 : 2026-07-30

# 설명
Runtime UI Toolkit DragDrop Manipulator 연결과 예외 종료 상태 테스트.

# 테스트 구성
 L: UITK lifecycle
 R: UITK resolver 등록
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;

using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.TestTools;

using NUnit;
using NUnit.Framework;

using inonego.Xeri.UI.DragDrop;

namespace inonego.Xeri.TEST.UI._Drag_Drop
{
    // ============================================================
    /// <summary>
    /// UI Toolkit DragDrop 연결 테스트 클래스.
    /// </summary>
    // ============================================================
    public class TEST_UITKDragDrop
    {

    #region 헬퍼

        // ------------------------------------------------------------
        /// <summary>
        /// 실제 pointer capture를 사용할 Runtime UI Toolkit 문서를 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        private static UIDocument CreateDocument
        (
            out GameObject documentObject,
            out PanelSettings panelSettings
        )
        {
            panelSettings = ScriptableObject.CreateInstance<PanelSettings>();
            documentObject = new GameObject("TEST_UITKDragDrop");

            var document = documentObject.AddComponent<UIDocument>();
            document.panelSettings = panelSettings;

            return document;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// PointerDownEvent를 대상 element에 전달한다.
        /// </summary>
        // ------------------------------------------------------------
        private static void SendPointerDown(VisualElement target, Vector2 pos)
        {
            var systemEvent = new Event
            {
                type          = EventType.MouseDown,
                button        = 0,
                mousePosition = pos,
            };

            using var pointerEvent = PointerDownEvent.GetPooled(systemEvent);

            pointerEvent.target = target;
            target.SendEvent(pointerEvent);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// PointerMoveEvent를 대상 element에 전달한다.
        /// </summary>
        // ------------------------------------------------------------
        private static void SendPointerMove(VisualElement target, Vector2 pos)
        {
            var systemEvent = new Event
            {
                type          = EventType.MouseMove,
                button        = 0,
                mousePosition = pos,
            };

            using var pointerEvent = PointerMoveEvent.GetPooled(systemEvent);

            pointerEvent.target = target;
            target.SendEvent(pointerEvent);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// PointerUpEvent를 대상 element에 전달한다.
        /// </summary>
        // ------------------------------------------------------------
        private static void SendPointerUp(VisualElement target, Vector2 pos)
        {
            var systemEvent = new Event
            {
                type          = EventType.MouseUp,
                button        = 0,
                mousePosition = pos,
            };

            using var pointerEvent = PointerUpEvent.GetPooled(systemEvent);

            pointerEvent.target = target;
            target.SendEvent(pointerEvent);
        }

    #endregion

    #region L-1: Draggable Manipulator

        // ------------------------------------------------------------
        /// <summary>
        /// VisualElement에 UITKDraggableManipulator를 붙이면 Core Draggable이 생성된다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_UITKDragDrop_AddManipulator_Draggable_생성()
        {
            var element = new VisualElement();
            var manipulator = new UITKDraggableManipulator();

            element.AddManipulator(manipulator);

            Assert.IsNotNull(manipulator.Draggable);
            Assert.IsFalse(manipulator.IsDragging);
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// 드래그 시작 알림 실패 뒤 Core Drag와 pointer capture가 남지 않는지 검증한다.
        /// </summary>
        // ----------------------------------------------------------------------
        [UnityTest]
        public IEnumerator TEST_UITKDragDrop_DragBegin실패_Drag와Pointer정리()
        {
            var document = CreateDocument(out var documentObject, out var panelSettings);
            var element = new VisualElement();
            var coordinator = new DragDropCoordinator();
            var manipulator = new UITKDraggableManipulator(coordinator)
            {
                CanMove       = false,
                DragThreshold = 0f,
            };

            document.rootVisualElement.Add(element);
            element.AddManipulator(manipulator);

            try
            {
                // Runtime panel과 pointer capture가 연결된 뒤 공개 입력을 전달한다.
                yield return null;
                yield return null;

                var callbackInvoked = false;
                manipulator.OnDragBegin += (_, _) =>
                {
                    callbackInvoked = true;
                    throw new InvalidOperationException();
                };
                SendPointerDown(element, new Vector2(10f, 10f));

                Assert.Throws<InvalidOperationException>
                (
                    () => SendPointerMove(element, new Vector2(20f, 10f))
                );

                Assert.IsTrue(callbackInvoked);
                Assert.IsFalse(manipulator.IsDragging);
                Assert.IsFalse(element.HasPointerCapture(PointerId.mousePointerId));
                CollectionAssert.DoesNotContain
                (
                    coordinator.ActiveDraggables,
                    manipulator.Draggable
                );
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(documentObject);
                UnityEngine.Object.DestroyImmediate(panelSettings);
            }
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// 드래그 종료 알림 실패 뒤 Core Drag와 pointer capture가 남지 않는지 검증한다.
        /// </summary>
        // ----------------------------------------------------------------------
        [UnityTest]
        public IEnumerator TEST_UITKDragDrop_DragEnd실패_Drag와Pointer정리()
        {
            var document = CreateDocument(out var documentObject, out var panelSettings);
            var element = new VisualElement();
            var coordinator = new DragDropCoordinator();
            var manipulator = new UITKDraggableManipulator(coordinator)
            {
                CanMove       = false,
                DragThreshold = 0f,
            };

            document.rootVisualElement.Add(element);
            element.AddManipulator(manipulator);

            try
            {
                // 실제 pointer 입력으로 시작한 Drag가 종료 예외 뒤에도 닫히는지 확인한다.
                yield return null;
                yield return null;

                SendPointerDown(element, new Vector2(10f, 10f));
                SendPointerMove(element, new Vector2(20f, 10f));
                Assert.IsTrue(manipulator.IsDragging);

                var callbackInvoked = false;
                manipulator.OnDragEnd += (_, _) =>
                {
                    callbackInvoked = true;
                    throw new InvalidOperationException();
                };

                Assert.Throws<InvalidOperationException>
                (
                    () => SendPointerUp(element, new Vector2(20f, 10f))
                );

                Assert.IsTrue(callbackInvoked);
                Assert.IsFalse(manipulator.IsDragging);
                Assert.IsFalse(element.HasPointerCapture(PointerId.mousePointerId));
                CollectionAssert.DoesNotContain
                (
                    coordinator.ActiveDraggables,
                    manipulator.Draggable
                );
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(documentObject);
                UnityEngine.Object.DestroyImmediate(panelSettings);
            }
        }

    #endregion

    #region R-1: DropZone Manipulator

        // ------------------------------------------------------------
        /// <summary>
        /// DropZone Manipulator는 Coordinator와 Resolver에 DropZone을 등록한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_UITKDragDrop_DropZoneManipulator_Register()
        {
            var root = new VisualElement();
            var zone = new VisualElement();
            var coordinator = new DragDropCoordinator();
            var resolver = new UITKDropResolver(root);
            var manipulator = new UITKDropZoneManipulator(coordinator, resolver);

            root.Add(zone);
            zone.AddManipulator(manipulator);

            CollectionAssert.Contains(coordinator.DropZones, manipulator.DropZone);
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// DropZone 이탈 알림 실패 뒤 Resolver 등록이 남지 않는지 검증한다.
        /// </summary>
        // ----------------------------------------------------------------------
        [UnityTest]
        public IEnumerator TEST_UITKDragDrop_DropZone해제실패_Resolver등록해제()
        {
            var document = CreateDocument(out var documentObject, out var panelSettings);
            var root = document.rootVisualElement;
            var zone = new VisualElement();
            var coordinator = new DragDropCoordinator();
            var resolver = new UITKDropResolver(root);
            var manipulator = new UITKDropZoneManipulator(coordinator, resolver);

            zone.style.width  = 100f;
            zone.style.height = 100f;
            root.Add(zone);
            zone.AddManipulator(manipulator);

            try
            {
                // worldBound를 확정한 뒤 Resolver의 공개 조회 경로로 등록 상태를 확인한다.
                yield return null;
                yield return null;

                var input = new InputPoint(1, zone.worldBound.center);
                var draggable = new Draggable
                (
                    this,
                    new UITKDragCoordinateProvider(zone)
                )
                {
                    CanMove = false,
                };
                draggable.PrepareDrag(input);
                draggable.InvokeDragBegin(input);

                var registeredDropZone = manipulator.DropZone;
                registeredDropZone.TryAccept(draggable);
                Assert.AreSame(registeredDropZone, resolver.Resolve(input, draggable));

                DropEventHandler throwOnExit = (_, _) => throw new InvalidOperationException();
                manipulator.OnDropExit += throwOnExit;

                Assert.Throws<InvalidOperationException>
                (
                    () => zone.RemoveManipulator(manipulator)
                );

                Assert.IsNull(resolver.Resolve(input, draggable));
                CollectionAssert.DoesNotContain(coordinator.DropZones, registeredDropZone);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(documentObject);
                UnityEngine.Object.DestroyImmediate(panelSettings);
            }
        }

    #endregion

    }
}
