/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TEST_DraggableUI.cs
수정일 : 2026-05-08

# 설명
DraggableUI Play Mode 통합 테스트.
실제 GameObject + Canvas + EventSystem 을 셋업하고 PointerEventData 를 직접 주입한다.

# 테스트 구성
 L: 드래그 lifecycle (BeginDrag/EndDrag)
 A: ActiveCollection 추적
 B: 버튼 매칭 (허용/거부)
 F: 강제 종료 (ForceDragEnd)
 R: RaycastTarget / blocksRaycasts
========================================================================= BLOCK_HEADER_END */

using System.Collections;
using System.Linq;

using NUnit.Framework;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace inonego.Xeri.TEST.UI._Drag_Drop
{

    using inonego.Xeri.UI;

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
    /// 좌클릭 PointerEventData 를 생성한다.
    /// </summary>
    // ------------------------------------------------------------
    private PointerEventData CreateEventData(PointerEventData.InputButton button = PointerEventData.InputButton.Left)
    {
        return new PointerEventData(EventSystem.current)
        {
            button   = button,
            position = new Vector2(100, 100),
        };
    }

#endregion

#region 픽스처

    private GameObject  eventSystemGO;
    private GameObject  canvasGO;
    private GameObject  dragGO;
    private DraggableUI draggable;

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

        dragGO = new GameObject("Draggable", typeof(RectTransform));
        dragGO.transform.SetParent(canvasGO.transform, worldPositionStays: false);
        draggable = dragGO.AddComponent<DraggableUI>();
    }

    // ----------------------------------------------------------------------
    /// <summary>
    /// 셋업한 GameObject 를 모두 정리한다.
    /// </summary>
    // ----------------------------------------------------------------------
    [TearDown]
    public void TearDown()
    {
        if (dragGO        != null) Object.DestroyImmediate(dragGO);
        if (canvasGO      != null) Object.DestroyImmediate(canvasGO);
        if (eventSystemGO != null) Object.DestroyImmediate(eventSystemGO);
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
        bool fired = false;
        draggable.OnDragBegin += (_, _) => fired = true;

        var eventData = CreateEventData();

        ExecuteEvents.Execute(dragGO, eventData, ExecuteEvents.pointerDownHandler);
        ExecuteEvents.Execute(dragGO, eventData, ExecuteEvents.beginDragHandler);

        Assert.IsTrue(fired);
        Assert.IsTrue(draggable.IsDragging);

        yield return null;
    }

#endregion

#region L-2: OnEndDrag

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

        bool fired = false;
        draggable.OnDragEnd += (_, _) => fired = true;

        ExecuteEvents.Execute(dragGO, eventData, ExecuteEvents.endDragHandler);

        Assert.IsTrue(fired);
        Assert.IsFalse(draggable.IsDragging);

        yield return null;
    }

#endregion

#region A-1: ActiveCollection 추적

    // ----------------------------------------------------------------------
    /// <summary>
    /// 드래그 중에는 ActiveCollection 에 추가, 종료 시 제거된다.
    /// </summary>
    // ----------------------------------------------------------------------
    [UnityTest]
    public IEnumerator TEST_DraggableUI_ActiveCollection_드래그_중_추가_종료_제거()
    {
        var eventData = CreateEventData();

        Assert.IsFalse(DraggableUI.ActiveCollection.Contains(draggable));

        ExecuteEvents.Execute(dragGO, eventData, ExecuteEvents.pointerDownHandler);
        ExecuteEvents.Execute(dragGO, eventData, ExecuteEvents.beginDragHandler);

        Assert.IsTrue(DraggableUI.ActiveCollection.Contains(draggable));

        ExecuteEvents.Execute(dragGO, eventData, ExecuteEvents.endDragHandler);

        Assert.IsFalse(DraggableUI.ActiveCollection.Contains(draggable));

        yield return null;
    }

#endregion

#region B-1: 허용되지 않은 버튼

    // ----------------------------------------------------------------------
    /// <summary>
    /// 허용되지 않은 버튼은 OnInitializePotentialDrag 에서 pointerDrag 가 null 로 리셋된다.
    /// </summary>
    // ----------------------------------------------------------------------
    [UnityTest]
    public IEnumerator TEST_DraggableUI_허용되지_않은_버튼_pointerDrag_null()
    {
        draggable.Button = MouseButton.Left;

        var rightButtonEvent = CreateEventData(PointerEventData.InputButton.Right);
        rightButtonEvent.pointerDrag = dragGO;

        ExecuteEvents.Execute(dragGO, rightButtonEvent, ExecuteEvents.initializePotentialDrag);

        Assert.IsNull(rightButtonEvent.pointerDrag);

        yield return null;
    }

#endregion

#region F-1: ForceDragEnd

    // ----------------------------------------------------------------------
    /// <summary>
    /// ForceDragEnd 호출 시 IsDragging=false 로 전환되고 OnDragEnd 가 발화한다.
    /// </summary>
    // ----------------------------------------------------------------------
    [UnityTest]
    public IEnumerator TEST_DraggableUI_ForceDragEnd_종료_및_이벤트_발화()
    {
        var eventData = CreateEventData();

        ExecuteEvents.Execute(dragGO, eventData, ExecuteEvents.pointerDownHandler);
        ExecuteEvents.Execute(dragGO, eventData, ExecuteEvents.beginDragHandler);

        Assert.IsTrue(draggable.IsDragging);

        bool fired = false;
        draggable.OnDragEnd += (_, _) => fired = true;

        draggable.ForceDragEnd();

        Assert.IsFalse(draggable.IsDragging);
        Assert.IsTrue(fired);

        yield return null;
    }

#endregion

#region R-1: RaycastTarget / blocksRaycasts

    // ----------------------------------------------------------------------
    /// <summary>
    /// RaycastTarget=false 면 드래그 시작 시 CanvasGroup.blocksRaycasts 가 false 로 변경된다.
    /// </summary>
    // ----------------------------------------------------------------------
    [UnityTest]
    public IEnumerator TEST_DraggableUI_RaycastTarget_false_blocksRaycasts_전환()
    {
        var canvasGroup = dragGO.GetComponent<CanvasGroup>();
        canvasGroup.blocksRaycasts = true;

        draggable.RaycastTarget = false;

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
