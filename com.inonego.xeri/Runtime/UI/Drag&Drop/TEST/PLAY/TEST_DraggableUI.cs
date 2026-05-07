/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TEST_DraggableUI.cs
수정일 : 2026-05-08

# 설명
DraggableUI Play Mode 통합 테스트.
실제 GameObject + Canvas + EventSystem 을 셋업하고 PointerEventData 를 직접 주입한다.
========================================================================= BLOCK_HEADER_END */

using System.Collections;
using System.Linq;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;

using NUnit.Framework;

using inonego.Xeri.UI;

// ============================================================
/// <summary>
/// DraggableUI 핵심 동작 테스트.
/// </summary>
// ============================================================
public class TEST_DraggableUI
{

#region 셋업 / 정리

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

    [TearDown]
    public void TearDown()
    {
        if (dragGO        != null) Object.DestroyImmediate(dragGO);
        if (canvasGO      != null) Object.DestroyImmediate(canvasGO);
        if (eventSystemGO != null) Object.DestroyImmediate(eventSystemGO);
    }

#endregion

#region 헬퍼

    private PointerEventData CreateEventData(PointerEventData.InputButton button = PointerEventData.InputButton.Left)
    {
        return new PointerEventData(EventSystem.current)
        {
            button   = button,
            position = new Vector2(100, 100),
        };
    }

#endregion

#region 드래그 lifecycle

    // ----------------------------------------------------------------------
    /// <summary>
    /// OnBeginDrag 호출 시 IsDragging=true · OnDragBegin 이벤트 발화 확인.
    /// </summary>
    // ----------------------------------------------------------------------
    [UnityTest]
    public IEnumerator DraggableUI_01_OnBeginDrag_이벤트_발화_테스트()
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

    // ----------------------------------------------------------------------
    /// <summary>
    /// OnEndDrag 호출 시 IsDragging=false · OnDragEnd 이벤트 발화 확인.
    /// </summary>
    // ----------------------------------------------------------------------
    [UnityTest]
    public IEnumerator DraggableUI_02_OnEndDrag_이벤트_발화_테스트()
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

#region ActiveCollection

    // ----------------------------------------------------------------------
    /// <summary>
    /// 드래그 중에는 ActiveCollection 에 추가, 종료 시 제거된다.
    /// </summary>
    // ----------------------------------------------------------------------
    [UnityTest]
    public IEnumerator DraggableUI_03_ActiveCollection_추적_테스트()
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

#region 버튼 매칭

    // ----------------------------------------------------------------------
    /// <summary>
    /// 허용되지 않은 버튼은 OnInitializePotentialDrag 에서 pointerDrag 가 null 로 리셋된다.
    /// </summary>
    // ----------------------------------------------------------------------
    [UnityTest]
    public IEnumerator DraggableUI_04_허용되지_않은_버튼_무시_테스트()
    {
        draggable.Button = MouseButton.Left;

        var rightButtonEvent = CreateEventData(PointerEventData.InputButton.Right);
        rightButtonEvent.pointerDrag = dragGO;

        ExecuteEvents.Execute(dragGO, rightButtonEvent, ExecuteEvents.initializePotentialDrag);

        Assert.IsNull(rightButtonEvent.pointerDrag);

        yield return null;
    }

#endregion

#region 강제 종료

    // ----------------------------------------------------------------------
    /// <summary>
    /// ForceDragEnd 호출 시 IsDragging=false 로 전환되고 OnDragEnd 가 발화한다.
    /// </summary>
    // ----------------------------------------------------------------------
    [UnityTest]
    public IEnumerator DraggableUI_05_ForceDragEnd_종료_테스트()
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

#region RaycastTarget

    // ----------------------------------------------------------------------
    /// <summary>
    /// RaycastTarget=false 면 드래그 시작 시 CanvasGroup.blocksRaycasts 가 false 로 변경된다.
    /// </summary>
    // ----------------------------------------------------------------------
    [UnityTest]
    public IEnumerator DraggableUI_06_RaycastTarget_blocksRaycasts_테스트()
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
