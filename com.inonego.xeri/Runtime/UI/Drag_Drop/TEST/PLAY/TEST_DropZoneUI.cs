/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TEST_DropZoneUI.cs
수정일 : 2026-05-22

# 설명
DropZoneUI Play Mode 통합 테스트.
새 Core DropZone과 DragDropCoordinator 기반 라우팅을 검증한다.

# 테스트 구성
 L: UGUI DropZone 등록 lifecycle
 R: UGUI Drop 라우팅
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;

using NUnit;
using NUnit.Framework;

using inonego.Xeri.UI.DragDrop;

namespace inonego.Xeri.TEST.UI._Drag_Drop
{
    // ============================================================
    /// <summary>
    /// DropZoneUI 핵심 동작 테스트.
    /// </summary>
    // ============================================================
    public class TEST_DropZoneUI
    {

    #region 헬퍼

        // ============================================================
        /// <summary>
        /// 테스트용 드롭 Resolver.
        /// </summary>
        // ============================================================
        private sealed class Resolver : IDropResolver
        {
            // ------------------------------------------------------------
            /// <summary>
            /// 반환할 DropZone.
            /// </summary>
            // ------------------------------------------------------------
            public DropZone DropZone
            {
                get => dropZone;
                set => dropZone = value;
            }

            private DropZone dropZone = null;

            // ------------------------------------------------------------
            /// <summary>
            /// 현재 설정된 DropZone을 반환한다.
            /// </summary>
            // ------------------------------------------------------------
            public DropZone Resolve(InputPoint input, Draggable draggable)
            {
                return dropZone;
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 좌클릭 PointerEventData 를 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        private PointerEventData CreateEventData()
        {
            return new PointerEventData(EventSystem.current)
            {
                button   = PointerEventData.InputButton.Left,
                position = new Vector2(100f, 100f),
            };
        }

        // ------------------------------------------------------------
        /// <summary>
        /// DraggableUI 를 드래그 중 상태로 만든다.
        /// </summary>
        // ------------------------------------------------------------
        private PointerEventData StartDrag()
        {
            var eventData = CreateEventData();

            ExecuteEvents.Execute(dragGO, eventData, ExecuteEvents.pointerDownHandler);
            ExecuteEvents.Execute(dragGO, eventData, ExecuteEvents.beginDragHandler);

            return eventData;
        }

    #endregion

    #region 픽스처

        private GameObject eventSystemGO = null;
        private GameObject canvasGO = null;
        private GameObject dragGO = null;
        private GameObject zoneGO = null;
        private DraggableUI draggable = null;
        private DropZoneUI dropZone = null;
        private Resolver resolver = null;
        private DragDropCoordinator coordinator = null;

        // ------------------------------------------------------------
        /// <summary>
        /// EventSystem · Canvas · DraggableUI · DropZoneUI 를 준비한다.
        /// </summary>
        // ------------------------------------------------------------
        [SetUp]
        public void SetUp()
        {
            eventSystemGO = new GameObject("EventSystem");
            eventSystemGO.AddComponent<EventSystem>();

            canvasGO = new GameObject("Canvas", typeof(RectTransform));
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGO.AddComponent<GraphicRaycaster>();

            dragGO = new GameObject("Draggable", typeof(RectTransform));
            dragGO.transform.SetParent(canvasGO.transform, worldPositionStays: false);
            draggable = dragGO.AddComponent<DraggableUI>();

            zoneGO = new GameObject("DropZone", typeof(RectTransform));
            zoneGO.transform.SetParent(canvasGO.transform, worldPositionStays: false);
            dropZone = zoneGO.AddComponent<DropZoneUI>();

            resolver = new Resolver();
            coordinator = new DragDropCoordinator
            {
                DropResolver = resolver,
            };

            draggable.Coordinator = coordinator;
            dropZone.Coordinator  = coordinator;
            coordinator.Register(dropZone.DropZone);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 셋업한 GameObject 를 모두 정리한다.
        /// </summary>
        // ------------------------------------------------------------
        [TearDown]
        public void TearDown()
        {
            if (zoneGO        != null) UnityEngine.Object.DestroyImmediate(zoneGO);
            if (dragGO        != null) UnityEngine.Object.DestroyImmediate(dragGO);
            if (canvasGO      != null) UnityEngine.Object.DestroyImmediate(canvasGO);
            if (eventSystemGO != null) UnityEngine.Object.DestroyImmediate(eventSystemGO);
        }

    #endregion

    #region L-1: Register

        // ------------------------------------------------------------
        /// <summary>
        /// OnEnable 이후 DropZone이 Coordinator에 등록된다.
        /// </summary>
        // ------------------------------------------------------------
        [UnityTest]
        public IEnumerator TEST_DropZoneUI_OnEnable_Coordinator_Register()
        {
            CollectionAssert.Contains(coordinator.DropZones, dropZone.DropZone);

            yield return null;
        }

    #endregion

    #region R-1: Enter

        // ------------------------------------------------------------
        /// <summary>
        /// 드래그 중 Resolver가 DropZone을 반환하면 OnDropEnter가 발화한다.
        /// </summary>
        // ------------------------------------------------------------
        [UnityTest]
        public IEnumerator TEST_DropZoneUI_Coordinator_Route_OnDropEnter_발화()
        {
            var eventData = StartDrag();
            resolver.DropZone = dropZone.DropZone;

            var fired = false;
            dropZone.OnDropEnter += (_, _) => fired = true;

            ExecuteEvents.Execute(dragGO, eventData, ExecuteEvents.dragHandler);

            Assert.IsTrue(fired);
            Assert.IsTrue(dropZone.IsDropping);
            Assert.AreSame(draggable.Draggable, dropZone.Draggable);

            yield return null;
        }

    #endregion

    #region R-2: Drop

        // ------------------------------------------------------------
        /// <summary>
        /// 드래그 종료 시 현재 DropZone에 OnDropDone이 발화한다.
        /// </summary>
        // ------------------------------------------------------------
        [UnityTest]
        public IEnumerator TEST_DropZoneUI_Release_DropDone_발화()
        {
            var eventData = StartDrag();
            resolver.DropZone = dropZone.DropZone;

            ExecuteEvents.Execute(dragGO, eventData, ExecuteEvents.dragHandler);

            var fired = false;
            dropZone.OnDropDone += (_, _) => fired = true;

            ExecuteEvents.Execute(dragGO, eventData, ExecuteEvents.endDragHandler);

            Assert.IsTrue(fired);
            Assert.IsFalse(dropZone.IsDropping);

            yield return null;
        }

    #endregion

    #region R-3: CanDrop

        // ------------------------------------------------------------
        /// <summary>
        /// CanDrop=false 인 DropZoneUI는 드래그 진입을 거부한다.
        /// </summary>
        // ------------------------------------------------------------
        [UnityTest]
        public IEnumerator TEST_DropZoneUI_CanDrop_false_진입_거부()
        {
            dropZone.CanDrop = false;

            var eventData = StartDrag();
            resolver.DropZone = dropZone.DropZone;

            var fired = false;
            dropZone.OnDropEnter += (_, _) => fired = true;

            ExecuteEvents.Execute(dragGO, eventData, ExecuteEvents.dragHandler);

            Assert.IsFalse(fired);
            Assert.IsFalse(dropZone.IsDropping);

            yield return null;
        }

    #endregion

    }
}
