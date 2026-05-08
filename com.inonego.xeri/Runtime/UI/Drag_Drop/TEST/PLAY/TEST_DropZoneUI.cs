/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TEST_DropZoneUI.cs
수정일 : 2026-05-08

# 설명
DropZoneUI Play Mode 통합 테스트. DraggableUI 와의 페어링 동작을 검증한다.

# 테스트 구성
 L: 드롭 lifecycle (Enter/Exit/Drop)
 M: 매칭 (SpecificDropZone/CanDrop)
========================================================================= BLOCK_HEADER_END */

using System.Collections;

using NUnit.Framework;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;

namespace inonego.Xeri.TEST.UI._Drag_Drop
{

    using inonego.Xeri.UI;

// ============================================================
/// <summary>
/// DropZoneUI 핵심 동작 테스트.
/// </summary>
// ============================================================
public class TEST_DropZoneUI
{

#region 헬퍼

    // ------------------------------------------------------------
    /// <summary>
    /// DraggableUI 가 드래그 중인 상태로 셋업한 PointerEventData 를 반환.
    /// </summary>
    // ------------------------------------------------------------
    private PointerEventData StartDragAndCreateEvent()
    {
        var eventData = new PointerEventData(EventSystem.current)
        {
            button   = PointerEventData.InputButton.Left,
            position = new Vector2(100, 100),
        };

        ExecuteEvents.Execute(dragGO, eventData, ExecuteEvents.pointerDownHandler);
        ExecuteEvents.Execute(dragGO, eventData, ExecuteEvents.beginDragHandler);

        return eventData;
    }

#endregion

#region 픽스처

    private GameObject  eventSystemGO;
    private GameObject  canvasGO;
    private GameObject  dragGO;
    private GameObject  zoneGO;
    private DraggableUI draggable;
    private DropZoneUI  dropZone;

    // ------------------------------------------------------------
    /// <summary>
    /// EventSystem · Canvas · DraggableUI · DropZoneUI 셋업.
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

        dragGO = new GameObject("Draggable", typeof(RectTransform));
        dragGO.transform.SetParent(canvasGO.transform, worldPositionStays: false);
        draggable = dragGO.AddComponent<DraggableUI>();

        zoneGO = new GameObject("DropZone", typeof(RectTransform));
        zoneGO.transform.SetParent(canvasGO.transform, worldPositionStays: false);
        dropZone = zoneGO.AddComponent<DropZoneUI>();
    }

    // ------------------------------------------------------------
    /// <summary>
    /// 셋업한 GameObject 를 모두 정리한다.
    /// </summary>
    // ------------------------------------------------------------
    [TearDown]
    public void TearDown()
    {
        if (zoneGO        != null) Object.DestroyImmediate(zoneGO);
        if (dragGO        != null) Object.DestroyImmediate(dragGO);
        if (canvasGO      != null) Object.DestroyImmediate(canvasGO);
        if (eventSystemGO != null) Object.DestroyImmediate(eventSystemGO);
    }

#endregion

#region L-1: OnPointerEnter — 진입

    // ----------------------------------------------------------------------
    /// <summary>
    /// 드래그 중 드롭존 진입 시 IsDropping=true · OnDropEnter 발화.
    /// </summary>
    // ----------------------------------------------------------------------
    [UnityTest]
    public IEnumerator TEST_DropZoneUI_OnPointerEnter_드롭존_진입()
    {
        var eventData = StartDragAndCreateEvent();

        bool fired = false;
        dropZone.OnDropEnter += (_, _) => fired = true;

        ExecuteEvents.Execute(zoneGO, eventData, ExecuteEvents.pointerEnterHandler);

        Assert.IsTrue(fired);
        Assert.IsTrue(dropZone.IsDropping);
        Assert.AreSame(draggable, dropZone.Draggable);

        yield return null;
    }

#endregion

#region L-2: OnPointerExit — 이탈

    // ----------------------------------------------------------------------
    /// <summary>
    /// 드롭존에서 이탈 시 IsDropping=false · OnDropExit 발화.
    /// </summary>
    // ----------------------------------------------------------------------
    [UnityTest]
    public IEnumerator TEST_DropZoneUI_OnPointerExit_드롭존_이탈()
    {
        var eventData = StartDragAndCreateEvent();

        ExecuteEvents.Execute(zoneGO, eventData, ExecuteEvents.pointerEnterHandler);

        bool fired = false;
        dropZone.OnDropExit += (_, _) => fired = true;

        ExecuteEvents.Execute(zoneGO, eventData, ExecuteEvents.pointerExitHandler);

        Assert.IsTrue(fired);
        Assert.IsFalse(dropZone.IsDropping);

        yield return null;
    }

#endregion

#region L-3: OnDrop — 완료

    // ----------------------------------------------------------------------
    /// <summary>
    /// 드롭 완료 시 OnDropDone 발화 + 이후 IsDropping=false.
    /// </summary>
    // ----------------------------------------------------------------------
    [UnityTest]
    public IEnumerator TEST_DropZoneUI_OnDrop_완료_OnDropDone_발화()
    {
        var eventData = StartDragAndCreateEvent();

        ExecuteEvents.Execute(zoneGO, eventData, ExecuteEvents.pointerEnterHandler);

        bool fired = false;
        dropZone.OnDropDone += (_, _) => fired = true;

        ExecuteEvents.Execute(zoneGO, eventData, ExecuteEvents.dropHandler);

        Assert.IsTrue(fired);
        Assert.IsFalse(dropZone.IsDropping);

        yield return null;
    }

#endregion

#region M-1: SpecificDropZone 매칭 거부

    // ----------------------------------------------------------------------
    /// <summary>
    /// SpecificDropZone 이 다른 드롭존이면 진입 시 OnDropEnter 가 발화하지 않는다.
    /// </summary>
    // ----------------------------------------------------------------------
    [UnityTest]
    public IEnumerator TEST_DropZoneUI_SpecificDropZone_불일치_진입_거부()
    {
        // 다른 DropZone 생성
        var otherZoneGO = new GameObject("OtherZone", typeof(RectTransform));
        otherZoneGO.transform.SetParent(canvasGO.transform, worldPositionStays: false);
        var otherZone = otherZoneGO.AddComponent<DropZoneUI>();

        draggable.SpecificDropZone = otherZone;

        var eventData = StartDragAndCreateEvent();

        bool fired = false;
        dropZone.OnDropEnter += (_, _) => fired = true;

        ExecuteEvents.Execute(zoneGO, eventData, ExecuteEvents.pointerEnterHandler);

        Assert.IsFalse(fired);
        Assert.IsFalse(dropZone.IsDropping);

        Object.DestroyImmediate(otherZoneGO);

        yield return null;
    }

#endregion

#region M-2: CanDrop 거부

    // ----------------------------------------------------------------------
    /// <summary>
    /// CanDrop=false 인 드롭존은 드래그 진입을 거부한다.
    /// </summary>
    // ----------------------------------------------------------------------
    [UnityTest]
    public IEnumerator TEST_DropZoneUI_CanDrop_false_진입_거부()
    {
        dropZone.CanDrop = false;

        var eventData = StartDragAndCreateEvent();

        bool fired = false;
        dropZone.OnDropEnter += (_, _) => fired = true;

        ExecuteEvents.Execute(zoneGO, eventData, ExecuteEvents.pointerEnterHandler);

        Assert.IsFalse(fired);
        Assert.IsFalse(dropZone.IsDropping);

        yield return null;
    }

#endregion

}

}
