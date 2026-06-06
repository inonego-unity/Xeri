/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TEST_DraggableUI.cs
수정일 : 2026-05-22

# 설명
DraggableUI Play Mode 통합 테스트.
실제 GameObject + Canvas + EventSystem 을 셋업하고 PointerEventData 를 직접 주입한다.

# 테스트 구성
 L: UGUI 드래그 lifecycle
 F: input filter
 P: UGUI policy
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
    /// DraggableUI 핵심 동작 테스트.
    /// </summary>
    // ============================================================
    public class TEST_DraggableUI
    {

    #region 헬퍼

        // ------------------------------------------------------------
        /// <summary>
        /// PointerEventData 를 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        private PointerEventData CreateEventData
        (
            PointerEventData.InputButton button = PointerEventData.InputButton.Left
        )
        {
            return new PointerEventData(EventSystem.current)
            {
                button   = button,
                position = new Vector2(100f, 100f),
            };
        }

    #endregion

    #region 픽스처

        private GameObject eventSystemGO = null;
        private GameObject canvasGO = null;
        private GameObject dragGO = null;
        private DraggableUI draggable = null;

        // ----------------------------------------------------------------------
        /// <summary>
        /// EventSystem · Canvas · DraggableUI 가 부착된 GameObject 를 준비한다.
        /// </summary>
        // ----------------------------------------------------------------------
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
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 셋업한 GameObject 를 모두 정리한다.
        /// </summary>
        // ------------------------------------------------------------
        [TearDown]
        public void TearDown()
        {
            if (dragGO        != null) UnityEngine.Object.DestroyImmediate(dragGO);
            if (canvasGO      != null) UnityEngine.Object.DestroyImmediate(canvasGO);
            if (eventSystemGO != null) UnityEngine.Object.DestroyImmediate(eventSystemGO);
        }

    #endregion

    #region L-1: OnBeginDrag

        // ----------------------------------------------------------------------
        /// <summary>
        /// OnBeginDrag 호출 시 IsDragging=true · OnDragBegin 이벤트 발화 확인.
        /// </summary>
        // ----------------------------------------------------------------------
        [UnityTest]
        public IEnumerator TEST_DraggableUI_OnBeginDrag_OnDragBegin_발화()
        {
            var fired = false;
            draggable.OnDragBegin += (_, _) => fired = true;

            var eventData = CreateEventData();

            ExecuteEvents.Execute(dragGO, eventData, ExecuteEvents.pointerDownHandler);
            ExecuteEvents.Execute(dragGO, eventData, ExecuteEvents.beginDragHandler);

            Assert.IsTrue(fired);
            Assert.IsTrue(draggable.IsDragging);

            yield return null;
        }

    #endregion

    #region L-2: OnDrag

        // ------------------------------------------------------------
        /// <summary>
        /// OnDrag 호출 시 GoalPos가 RectTransform에 적용된다.
        /// </summary>
        // ------------------------------------------------------------
        [UnityTest]
        public IEnumerator TEST_DraggableUI_OnDrag_GoalPos_적용()
        {
            var rectTransform = dragGO.GetComponent<RectTransform>();
            rectTransform.anchoredPosition = new Vector2(10f, 20f);

            var eventData = CreateEventData();

            ExecuteEvents.Execute(dragGO, eventData, ExecuteEvents.pointerDownHandler);
            ExecuteEvents.Execute(dragGO, eventData, ExecuteEvents.beginDragHandler);

            eventData.position = new Vector2(130f, 140f);
            ExecuteEvents.Execute(dragGO, eventData, ExecuteEvents.dragHandler);

            Assert.AreNotEqual(new Vector2(10f, 20f), rectTransform.anchoredPosition);

            yield return null;
        }

    #endregion

    #region L-3: OnEndDrag

        // ----------------------------------------------------------------------
        /// <summary>
        /// OnEndDrag 호출 시 IsDragging=false · OnDragEnd 이벤트 발화 확인.
        /// </summary>
        // ----------------------------------------------------------------------
        [UnityTest]
        public IEnumerator TEST_DraggableUI_OnEndDrag_OnDragEnd_발화()
        {
            var eventData = CreateEventData();

            ExecuteEvents.Execute(dragGO, eventData, ExecuteEvents.pointerDownHandler);
            ExecuteEvents.Execute(dragGO, eventData, ExecuteEvents.beginDragHandler);

            var fired = false;
            draggable.OnDragEnd += (_, _) => fired = true;

            ExecuteEvents.Execute(dragGO, eventData, ExecuteEvents.endDragHandler);

            Assert.IsTrue(fired);
            Assert.IsFalse(draggable.IsDragging);

            yield return null;
        }

    #endregion

    #region F-1: Input Filter

        // ----------------------------------------------------------------------
        /// <summary>
        /// 허용되지 않은 버튼은 OnInitializePotentialDrag 에서 pointerDrag 가 null 로 리셋된다.
        /// </summary>
        // ----------------------------------------------------------------------
        [UnityTest]
        public IEnumerator TEST_DraggableUI_InputFilter_false_드래그_시작_거부()
        {
            draggable.DragButton = PointerEventData.InputButton.Left;

            var rightButtonEvent = CreateEventData(PointerEventData.InputButton.Right);
            rightButtonEvent.pointerDrag = dragGO;

            ExecuteEvents.Execute(dragGO, rightButtonEvent, ExecuteEvents.initializePotentialDrag);
            ExecuteEvents.Execute(dragGO, rightButtonEvent, ExecuteEvents.pointerDownHandler);
            ExecuteEvents.Execute(dragGO, rightButtonEvent, ExecuteEvents.beginDragHandler);

            Assert.IsNull(rightButtonEvent.pointerDrag);
            Assert.IsFalse(draggable.IsDragging);

            yield return null;
        }

    #endregion

    #region P-1: Raycast Policy

        // ----------------------------------------------------------------------
        /// <summary>
        /// DisableRaycastDuringDrag=true 면 드래그 중 CanvasGroup.blocksRaycasts 가 false 로 전환된다.
        /// </summary>
        // ----------------------------------------------------------------------
        [UnityTest]
        public IEnumerator TEST_DraggableUI_UGUIRaycastPolicy_blocksRaycasts_전환_및_복원()
        {
            var canvasGroup = dragGO.GetComponent<CanvasGroup>();
            canvasGroup.blocksRaycasts = true;
            draggable.DisableRaycastDuringDrag = true;

            var eventData = CreateEventData();

            ExecuteEvents.Execute(dragGO, eventData, ExecuteEvents.pointerDownHandler);
            ExecuteEvents.Execute(dragGO, eventData, ExecuteEvents.beginDragHandler);

            Assert.IsFalse(canvasGroup.blocksRaycasts);

            ExecuteEvents.Execute(dragGO, eventData, ExecuteEvents.endDragHandler);

            Assert.IsTrue(canvasGroup.blocksRaycasts);

            yield return null;
        }

    #endregion

    }
}
