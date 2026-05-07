/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TEST_DropZoneUI.cs
수정일 : 2026-05-08

# 설명
DropZoneUI Play Mode 통합 테스트. DraggableUI 와의 페어링 동작을 검증한다.
========================================================================= BLOCK_HEADER_END */

using System.Collections;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;

using NUnit.Framework;

using inonego.Xeri.UI;

// ============================================================
/// <summary>
/// DropZoneUI 핵심 동작 테스트.
/// </summary>
// ============================================================
public class TEST_DropZoneUI
{

#region 셋업 / 정리

    private GameObject  eventSystemGO;
    private GameObject  canvasGO;
    private GameObject  dragGO;
    private GameObject  zoneGO;
    private DraggableUI draggable;
    private DropZoneUI  dropZone;

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

    [TearDown]
    public void TearDown()
    {
        if (zoneGO        != null) Object.DestroyImmediate(zoneGO);
        if (dragGO        != null) Object.DestroyImmediate(dragGO);
        if (canvasGO      != null) Object.DestroyImmediate(canvasGO);
        if (eventSystemGO != null) Object.DestroyImmediate(eventSystemGO);
    }

#endregion

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

#region 드롭 lifecycle

    // ----------------------------------------------------------------------
    /// <summary>
    /// 드래그 중 드롭존 진입 시 IsDropping=true · OnDropEnter 발화.
    /// </summary>
    // ----------------------------------------------------------------------
    [UnityTest]
    public IEnumerator DropZoneUI_01_OnPointerEnter_드롭존_진입_테스트()
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

    // ----------------------------------------------------------------------
    /// <summary>
    /// 드롭존에서 이탈 시 IsDropping=false · OnDropExit 발화.
    /// </summary>
    // ----------------------------------------------------------------------
    [UnityTest]
    public IEnumerator DropZoneUI_02_OnPointerExit_드롭존_이탈_테스트()
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

    // ----------------------------------------------------------------------
    /// <summary>
    /// 드롭 완료 시 OnDropDone 발화 + 이후 IsDropping=false.
    /// </summary>
    // ----------------------------------------------------------------------
    [UnityTest]
    public IEnumerator DropZoneUI_03_OnDrop_완료_테스트()
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

#region 매칭

    // ----------------------------------------------------------------------
    /// <summary>
    /// SpecificDropZone 이 다른 드롭존이면 진입 시 OnDropEnter 가 발화하지 않는다.
    /// </summary>
    // ----------------------------------------------------------------------
    [UnityTest]
    public IEnumerator DropZoneUI_04_SpecificDropZone_매칭_거부_테스트()
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

    // ----------------------------------------------------------------------
    /// <summary>
    /// CanDrop=false 인 드롭존은 드래그 진입을 거부한다.
    /// </summary>
    // ----------------------------------------------------------------------
    [UnityTest]
    public IEnumerator DropZoneUI_05_CanDrop_false_거부_테스트()
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
