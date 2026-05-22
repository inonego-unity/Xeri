# Xeri Drag Drop

`Drag_Drop`은 UGUI와 UI Toolkit에서 공통으로 사용할 수 있는 드래그/드롭 시스템입니다.

Core는 UI 프레임워크에 의존하지 않고, UGUI와 UITK 구현은 Core 객체에 입력과 좌표계를 연결하는 어댑터 역할을 합니다.

## 구성

```text
Runtime/UI/Drag_Drop
├─ Core
│  ├─ Input
│  │  ├─ InputPoint.cs
│  │  └─ IDragInputFilter.cs
│  ├─ Drag
│  │  ├─ Draggable.cs
│  │  ├─ DragEventArgs.cs
│  │  └─ IDragCoordinateProvider.cs
│  ├─ Drop
│  │  ├─ DropZone.cs
│  │  ├─ DropEventArgs.cs
│  │  ├─ IDropResolver.cs
│  │  ├─ IDropRule.cs
│  │  └─ DropRuleAsset.cs
│  └─ DragDropCoordinator.cs
├─ UGUI
│  ├─ DraggableUI.cs
│  ├─ DropZoneUI.cs
│  ├─ UGUIDragCoordinateProvider.cs
│  ├─ UGUIDropResolver.cs
│  ├─ Filters
│  └─ Policies
├─ UITK
│  ├─ UITKDraggableManipulator.cs
│  ├─ UITKDropZoneManipulator.cs
│  ├─ UITKDragCoordinateProvider.cs
│  ├─ UITKDropResolver.cs
│  └─ Filters
└─ Editor
   └─ EditorAssetDropManipulator.cs
```

## Core 개념

### `Draggable`

드래그 대상의 상태 객체입니다.

- `CanMove`: 드래그 중 대상 위치를 움직일지 여부
- `CanDrop`: 드롭 영역에 들어갈 수 있는지 여부
- `IsDragging`: 현재 드래그 중인지 여부
- `OnDragBegin`: 드래그 시작 이벤트
- `OnDrag`: 드래그 진행 이벤트
- `OnDragEnd`: 드래그 종료 이벤트

### `DropZone`

드롭 영역의 상태 객체입니다.

- `CanDrop`: 드롭 허용 여부
- `IsDropping`: 현재 드래그 대상이 들어와 있는지 여부
- `Draggable`: 현재 들어온 드래그 대상
- `AddDropRule()`: 드롭 가능 여부를 판단하는 규칙 추가
- `OnDropEnter`: 드롭 영역 진입 이벤트
- `OnDropExit`: 드롭 영역 이탈 이벤트
- `OnDropDone`: 드롭 완료 이벤트

### `DragDropCoordinator`

드래그 대상과 드롭 영역의 관계를 조율합니다.

- `Register()`: 드롭 영역 등록
- `Unregister()`: 드롭 영역 등록 해제
- `HandleDragBegin()`: 활성 드래그 목록에 추가
- `HandleDrag()`: 현재 입력 위치 기준으로 드롭 영역 진입/이탈 처리
- `HandleDragEnd()`: 드롭 완료 처리
- `HandleDragCancel()`: 드롭 없이 드래그 취소 처리

기본 인스턴스는 `DragDropCoordinator.Default`입니다.

### `IDropResolver`

입력 위치와 드래그 대상을 기준으로 어떤 `DropZone`에 들어갔는지 찾는 객체입니다.

UGUI는 `UGUIDropResolver`, UITK는 `UITKDropResolver`를 제공합니다.

## UGUI 사용법

### 드래그 대상 만들기

1. 드래그할 GameObject에 `RectTransform`과 `CanvasGroup`을 둡니다.
2. `DraggableUI`를 추가합니다.
3. 필요하면 Inspector에서 값을 조정합니다.

주요 옵션:

- `CanMove`: 드래그 중 RectTransform 이동 여부
- `CanDrop`: 드롭 가능 여부
- `DragButton`: 드래그 시작 버튼
- `DisableRaycastDuringDrag`: 드래그 중 raycast 차단 여부

### 드롭 영역 만들기

1. 드롭 영역 GameObject에 `RectTransform`을 둡니다.
2. `DropZoneUI`를 추가합니다.
3. 필요하면 `DropRuleAsset`을 등록합니다.

### UGUI 예시

```csharp
using UnityEngine;

using inonego.Xeri.UI.DragDrop;

public sealed class UGUIDragDropExample : MonoBehaviour
{
    [SerializeField]
    private DraggableUI draggableUI = null;

    [SerializeField]
    private DropZoneUI dropZoneUI = null;

    private void Awake()
    {
        draggableUI.OnDragBegin += OnDragBegin;
        draggableUI.OnDragEnd   += OnDragEnd;
        dropZoneUI.OnDropDone   += OnDropDone;
    }

    private void OnDragBegin(Draggable sender, DragEventArgs e)
    {
        Debug.Log($"Drag Begin: {e.Pos}");
    }

    private void OnDragEnd(Draggable sender, DragEventArgs e)
    {
        Debug.Log($"Drag End: {e.Pos}");
    }

    private void OnDropDone(DropZone sender, DropEventArgs e)
    {
        Debug.Log("Drop Done");
    }
}
```

## UITK 사용법

UITK는 `Manipulator`를 VisualElement에 붙여 사용합니다.

### 기본 흐름

1. root VisualElement를 기준으로 `UITKDropResolver`를 만듭니다.
2. 드래그할 VisualElement에 `UITKDraggableManipulator`를 붙입니다.
3. 드롭 영역 VisualElement에 `UITKDropZoneManipulator`를 붙입니다.
4. 같은 `DragDropCoordinator`와 `UITKDropResolver`를 공유합니다.

### UITK 예시

```csharp
using UnityEngine;
using UnityEngine.UIElements;

using inonego.Xeri.UI.DragDrop;

public sealed class UITKDragDropExample : MonoBehaviour
{
    [SerializeField]
    private UIDocument document = null;

    private void Awake()
    {
        var root = document.rootVisualElement;

        var draggableElement = root.Q<VisualElement>("draggable");
        var dropZoneElement  = root.Q<VisualElement>("drop-zone");

        var coordinator  = new DragDropCoordinator();
        var dropResolver = new UITKDropResolver(root);

        coordinator.DropResolver = dropResolver;

        var draggable = new UITKDraggableManipulator(coordinator)
        {
            CanMove               = true,
            CanDrop               = true,
            DragButton            = 0,
            DragThreshold         = 5f,
            ForceAbsolutePosition = true,
        };

        var dropZone = new UITKDropZoneManipulator(coordinator, dropResolver)
        {
            CanDrop = true,
        };

        draggable.OnDragBegin += OnDragBegin;
        draggable.OnDragEnd   += OnDragEnd;
        dropZone.OnDropDone   += OnDropDone;

        draggableElement.AddManipulator(draggable);
        dropZoneElement.AddManipulator(dropZone);
    }

    private void OnDragBegin(Draggable sender, DragEventArgs e)
    {
        Debug.Log($"Drag Begin: {e.Pos}");
    }

    private void OnDragEnd(Draggable sender, DragEventArgs e)
    {
        Debug.Log($"Drag End: {e.Pos}");
    }

    private void OnDropDone(DropZone sender, DropEventArgs e)
    {
        Debug.Log("Drop Done");
    }
}
```

## 드롭 규칙

`IDropRule`을 구현하면 특정 조건에서만 드롭을 허용할 수 있습니다.

```csharp
using inonego.Xeri.UI.DragDrop;

public sealed class OwnerTypeDropRule : IDropRule
{
    public bool CanDrop(Draggable draggable, DropZone dropZone)
    {
        return draggable.Owner != dropZone.Owner;
    }
}
```

UGUI에서는 `DropZoneUI`의 `DropRuleAsset` 목록을 사용할 수 있고, Core/UITK에서는 `AddDropRule()`로 직접 추가할 수 있습니다.

## 이벤트 순서

정상 드래그/드롭 흐름은 다음 순서로 처리됩니다.

1. 드래그 시작 입력 준비
2. `OnDragBegin`
3. `DragDropCoordinator.HandleDragBegin`
4. 드래그 이동마다 `OnDrag`
5. 드래그 위치에 따라 `OnDropEnter` 또는 `OnDropExit`
6. 드래그 종료 시 `OnDropDone`
7. `OnDropExit`
8. `OnDragEnd`

드래그가 취소되면 `OnDropDone` 없이 `OnDropExit`와 `OnDragEnd`만 처리됩니다.

## 구현 시 주의점

- 같은 드래그/드롭 그룹은 같은 `DragDropCoordinator`를 공유해야 합니다.
- 드롭 판정이 필요하면 `Coordinator.DropResolver`를 설정해야 합니다.
- 드래그 중 위치 이동을 막고 이벤트만 받고 싶으면 `CanMove = false`로 설정합니다.
- 드롭 대상이 될 수 없는 드래그 항목은 `CanDrop = false`로 설정합니다.
- UGUI에서 드래그 중 드롭 영역 raycast가 필요하면 `DisableRaycastDuringDrag`를 켜는 것이 일반적입니다.
- UITK에서 직접 위치를 이동하려면 대상 VisualElement가 absolute position을 사용할 수 있어야 합니다. `UITKDraggableManipulator.ForceAbsolutePosition`이 기본적으로 이를 보정합니다.
