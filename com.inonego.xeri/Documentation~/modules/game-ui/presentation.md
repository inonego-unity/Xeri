# Game UI 표시와 배치

## 목적

Screen Stack 바깥에서 사용하는 Scene Fade, Overlay, Modal, Visibility, Tracking, Projection, Spotlight와 UI Toolkit 표현 기능을 어떤 소유권으로 조립해야 하는지 설명합니다.

## 언제 읽는가

- Screen과 독립된 Overlay/Modal을 표시할 때
- 월드 위치를 UGUI/UITK 화면 좌표로 추적할 때
- Safe Area, Spotlight, Pointer 차단을 Screen 수명에 연결할 때
- UI Toolkit Gradient/Gamma 표현을 구성할 때

## 표시 기능

### Scene Fade

`SceneFader`는 Runtime 전체 화면 전환을 담당한다. Context별 UI Fade나 Window Fade가 아니다.

```csharp
var fade = new SceneFadeParams(Color.black, 0.25f);

runtime.SceneFader.Cover(fade, HandleCovered, HandleFadeFailure);
runtime.SceneFader.Reveal(fade, HandleRevealed, HandleFadeFailure);
```

`Cover` 완료 뒤에는 불투명 View를 유지하고 `Reveal` 완료 뒤 반환한다. 새 요청은 기존 Fade를
취소하며 실패는 요청 Callback과 `LastFailure`로 확인한다.

### Overlay

Overlay는 Screen Stack과 독립적으로 Layer를 점유하는 임시 View다.

```csharp
OverlayHandle<ExampleView> overlay = OverlayHandle<ExampleView>.Acquire
(
    context.LayerRegistry,
    "Overlay",
    source
);
```

`IOverlaySource<TView>`가 View의 획득과 반환을 소유한다. Screen에 종속되면 Session의 자식으로
등록하고, 독립 수명이면 획득한 객체가 직접 해제한다.

### Modal

`ModalController`는 View를 생성하지 않는다. Modal Stack과 top 상호작용 상태만 관리한다.

- UGUI: `UGUIModalDriver`
- UI Toolkit: `UITKModalDriver`

Overlay로 Modal View를 획득했다면 Handle 소유권을 Modal에 넘길 수 있다.

```csharp
ModalHandle modal = context.Modals.Open(driver, overlayHandle);
```

Modal Handle을 해제하면 현재 항목을 닫고 이전 Modal을 top으로 복원한 뒤 전달받은 Handle을
해제한다.

### Visibility

`VisibilityController`는 같은 Target에 대한 중첩 요청을 최신 요청 우선으로 합성한다.

```csharp
Lease hidden = runtime.Visibility.Set(target, visible: false);
```

마지막 Lease를 해제하면 Target의 최초 상태로 복원한다.

### Drag Visual

`DragVisualController`는 Xeri `Drag_Drop`의 `DraggableUI`와 연결한다.

```csharp
var dragVisuals = new DragVisualController(context.LayerRegistry);
IDisposable binding = dragVisuals.Bind
(
    draggable,
    new DragVisualParams(target, "Drag")
);
```

Drag 중 Target을 지정 Layer로 옮기고 종료 시 부모, sibling과 Transform을 복원한다. Drag 판정과
Drop 계약은 [Drag_Drop README](../../../Runtime/UI/Drag_Drop/README.md)를 따른다.

## 배치와 입력 보조 기능

| 역할 | UGUI | UI Toolkit | 결과 수명 |
|---|---|---|---|
| World 위치를 Root 좌표로 변환 | `UGUIWorldProjector` | `UITKWorldProjector` | 값 |
| Root 안에서 요소 배치 | `PlacementSolver` + `YUp` | `PlacementSolver` + `YDown` | 값 |
| Safe Area 적용 | `UGUISafeAreaLayout` | `UITKSafeAreaLayout` | Component |
| dim과 입력 통과 구멍 | `UGUISpotlight` | `UITKSpotlight` | `Lease` |
| 명시적 Pointer 입력 차단 | `UGUIInteractionBlocker` | `UITKInteractionBlocker` | `Lease` |

이 기능은 Runtime이 자동 생성하지 않는다. 사용하는 Screen, Presenter 또는 조립 객체가 만들고
반환된 Lease와 Component를 자신의 수명 안에서 종료한다.

### World Projection과 Placement

Projector 결과와 `PlacementSolver`의 Bounds는 같은 UI Root 로컬 좌표여야 한다. UGUI는
`RectTransform`의 Y-up, UI Toolkit은 `VisualElement`의 Y-down 좌표를 사용한다.

```csharp
var projector = new UITKWorldProjector();
ProjectionResult projected = projector.Project(worldPosition, camera, layerRoot);

if (projected.Succeeded && !projected.IsBehindCamera)
{
    var solver = new PlacementSolver();
    PlacementResult placed = solver.Place
    (
        layerRoot.contentRect,
        projected.LocalPosition,
        element.layout.size,
        Vector2.zero,
        new PlacementOptions
        (
            PlacementAlignment.Bottom,
            new Vector2(0.0f, 12.0f),
            new Vector2(16.0f, 16.0f),
            coordinateSystem: PlacementCoordinateSystem.YDown
        )
    );

    element.style.left = placed.LocalPosition.x;
    element.style.top = placed.LocalPosition.y;
}
```

`left`와 `top`을 적용할 때는 Pivot으로 `Vector2.zero`를 사용한다. UGUI는 Projector가 반환한
Root 로컬 좌표를 `RectTransform.localPosition`에 적용하고, 요소
크기 계산에는 실제 `RectTransform.pivot`과 `YUp`을 사용한다. `anchoredPosition`을 사용하려면
Marker의 고정 Anchor가 부모 Pivot과 일치해야 한다. Solver 호출만 감싸는 Backend별 Popup
타입은 제공하지 않는다.

### 연속 Tracking

매 Frame 같은 관계를 갱신해야 할 때는 `TrackingRunner`에 `TrackingBinding<T>`를 등록한다.
Binding은 다음 세 단계를 하나로 묶는다.

1. `resolve`: 현재 원하는 값을 조회한다. 대상이 없으면 `Available`을 `false`로 반환한다.
2. `transition`: 이전 실제 적용값에서 새 값으로 이동한다. 즉시 반영이면 생략한다.
3. `commit`: 값을 실제 대상에 적용하고, 적용된 최종값을 반환한다.

등록 결과는 별도 Tracking Handle이 아니라 공통 `Lease`다. Marker, Screen 또는 Presenter를
소유한 범위가 이 Lease를 보관하고 종료한다. `TrackingRunner`는 필요한 Scene 범위에만 명시적으로
배치하며 `GameUIRuntime`이 자동 생성하거나 소유하지 않는다.

UGUI World Marker는 Projection, Safe Rect 배치와 보간을 다음처럼 조립한다. 아래 예시는
Marker가 현재 `screenSession`에 속하며 해당 Screen이 Tracking Lease를 소유한다고 가정한다.

```csharp
var projector = new UGUIWorldProjector();
var solver = new PlacementSolver();
var velocity = Vector2.zero;

var binding = new TrackingBinding<Vector2>
(
    resolve: () =>
    {
        if (target == null) return (false, default);

        ProjectionResult projected = projector.Project
        (
            target.position,
            camera,
            layerRoot
        );

        return
        (
            projected.Succeeded && !projected.IsBehindCamera,
            projected.LocalPosition
        );
    },
    transition: (current, desired, deltaTime) => Vector2.SmoothDamp
    (
        current,
        desired,
        ref velocity,
        0.12f,
        Mathf.Infinity,
        deltaTime
    ),
    commit: position =>
    {
        PlacementResult placed = solver.Place
        (
            layerRoot.rect,
            position,
            marker.rect.size,
            marker.pivot,
            new PlacementOptions
            (
                PlacementAlignment.Bottom,
                new Vector2(0.0f, 12.0f),
                new Vector2(16.0f, 16.0f)
            )
        );

        // Clamp 이후에도 외부 SmoothDamp 속도가 화면 밖 방향으로 누적되지 않게 한다.
        if (placed.WasClamped)
        {
            velocity = Vector2.zero;
        }

        marker.gameObject.SetActive(true);
        marker.localPosition = new Vector3
        (
            placed.LocalPosition.x,
            placed.LocalPosition.y,
            marker.localPosition.z
        );
        return placed.LocalPosition;
    },
    clear: () => marker.gameObject.SetActive(false)
);

Lease trackingLease = trackingRunner.Track(binding);
screenSession.RegisterChild(trackingLease);
```

`commit`이 clamp된 실제 위치를 반환하므로 다음 Frame의 보간도 화면에 표시된 위치에서 이어진다.
화면 뒤 대상을 숨길지, 가장자리 Indicator로 바꿀지는 `IsBehindCamera`를 해석하는 사용처 정책이다.

UI Toolkit에서도 같은 Binding을 사용하고 Backend 경계만 바꾼다.

```csharp
var projector = new UITKWorldProjector();
var solver = new PlacementSolver();

var binding = new TrackingBinding<Vector2>
(
    resolve: () =>
    {
        if (target == null) return (false, default);

        ProjectionResult projected = projector.Project
        (
            target.position,
            camera,
            layerRoot
        );

        return
        (
            projected.Succeeded && !projected.IsBehindCamera,
            projected.LocalPosition
        );
    },
    commit: position =>
    {
        PlacementResult placed = solver.Place
        (
            layerRoot.contentRect,
            position,
            element.layout.size,
            Vector2.zero,
            new PlacementOptions
            (
                PlacementAlignment.Bottom,
                new Vector2(0.0f, 12.0f),
                new Vector2(16.0f, 16.0f),
                coordinateSystem: PlacementCoordinateSystem.YDown
            )
        );

        element.style.display = DisplayStyle.Flex;
        element.style.left = placed.LocalPosition.x;
        element.style.top = placed.LocalPosition.y;
        return placed.LocalPosition;
    },
    clear: () => element.style.display = DisplayStyle.None
);

Lease trackingLease = trackingRunner.Track(binding);
screenSession.RegisterChild(trackingLease);
```

일시 정지와 무관하게 UI 전이를 계속하려면 `trackingRunner.UsesUnscaledTime = true`로 설정한다.
Runner 비활성화는 갱신만 멈추고, Lease 해제 또는 Runner 파괴가 Binding과 마지막 표시 상태를 정리한다.
`RegisterChild`에 전달한 Lease는 Screen이 정상 종료를 소유한다. Screen보다 먼저 Tracking을
끝내야 하면 같은 Lease를 직접 `Dispose()`해도 이후 Screen 종료에서는 다시 처리되지 않는다.
Screen에 속하지 않는 Tracking은 해당 Marker 또는 Presenter가 Lease를 필드로 보관하고 자신의
종료 시점에 `Dispose()`한다.

### Safe Area

`UGUISafeAreaLayout`은 연결한 `RectTransform` 하나에 적용한다. `UITKSafeAreaLayout`은 같은
`UIDocument`의 이름 있는 `VisualElement` 하나에 적용한다. 활성화 시 첫 Layout 전에 반영하고
화면 또는 Panel 크기가 바뀌면 다시 계산한다.

### Spotlight

Spotlight는 Focus 선택 기능이 아니다. 튜토리얼처럼 화면을 dim 처리하고 지정 Target 영역의
입력만 통과시키는 표시 기능이다.

```csharp
var spotlight = new UITKSpotlight();
var spotlightElement = new UITKSpotlightElement();
layerRoot.Add(spotlightElement);

Lease spotlightLease = spotlight.Show
(
    spotlightElement,
    new UITKSpotlightParams
    (
        new[]
        {
            new UITKSpotlightTarget
            (
                target,
                new Vector4(8.0f, 8.0f, 8.0f, 8.0f)
            )
        }
    )
);

screenSession.RegisterChild(spotlightLease);
```

`UITKSpotlightElement`와 Target은 같은 Panel에 연결한다. 유효한 Target이 모두 숨겨지거나
분리되면 dim과 Picking을 함께 비워 전체 입력 잠금을 만들지 않는다. UGUI에서는
`UGUISpotlightDriver`와 `UGUISpotlightParams`를 사용한다.

### Interaction Blocker

`IInteractionBlocker.Acquire()`는 중첩 가능한 Pointer 차단 Lease를 반환한다. 마지막 Lease가
해제될 때만 Blocker가 숨겨진다. UGUI는 연결한 Root와 `CanvasGroup`, UI Toolkit은 생성자에
전달한 전용 `VisualElement`만 변경한다. Keyboard·Gamepad Focus, Gameplay Input, Spotlight의
입력 구멍이나 Modal Stack을 대신하지 않는다.

## UI Toolkit 표현

### Runtime Baseline

`UITKLayerPanel`은 Layer Root에 `GameUIRuntimeBaseline.uss`를 자동 연결한다. Label, Button,
BaseField와 ProgressBar의 기본 외부 간격을 정규화하되 TextField 입력부, Slider Tracker,
Popup Arrow와 Scroller처럼 동작에 필요한 Unity 내부 구조는 유지한다.

### Gradient Material

UI Toolkit USS는 CSS의 `linear-gradient()`, `radial-gradient()`와 `conic-gradient()`를 배경
함수로 제공하지 않는다. Xeri Material을 Gradient가 필요한 요소에만 지정한다.

| 웹 CSS | Xeri Material |
|---|---|
| `linear-gradient()` | `XeriUI/Materials/LinearGrad` |
| `radial-gradient()` | `XeriUI/Materials/RadialGrad` |
| `conic-gradient()` | `XeriUI/Materials/ConicGrad` |

```css
.example-gradient {
    background-color: white;
    -unity-material: resource("XeriUI/Materials/LinearGrad")
        prop("_Color0" #7c3aed)
        prop("_Color1" #06b6d4)
        prop("_ColorCount" 2)
        prop("_Stop0" 0 0)
        prop("_Stop1" 1 1)
        prop("_Angle" 90)
        prop("_Tiling" 1);
}
```

| Property | 값 |
|---|---|
| `_Color0` ~ `_Color7` | 최대 8개 색상 |
| `_ColorCount` | 사용할 색상 수, 2~8 |
| `_Stop0` ~ `_Stop7` | 각 색상의 시작·종료 위치, 0~1 |
| `_Angle` | Linear 방향 또는 Conic 시작 각도 |
| `_Center` | Radial·Conic 중심점 |
| `_Radius` | Radial X·Y 반경 |
| `_Tiling` | Linear·Radial 반복 횟수 |

Material 출력에는 요소의 `background-color`가 곱해진다. Material 색상을 그대로 표시하려면
`background-color: white`를 사용한다.

한 `VisualElement`에는 Material 하나만 지정할 수 있다. CSS 다중 배경처럼 여러 Gradient가
필요하면 Gradient별 자식 요소를 같은 영역에 겹친다.

```xml
<ui:VisualElement class="gradient-card">
    <ui:VisualElement class="gradient-card__linear" picking-mode="Ignore" />
    <ui:VisualElement class="gradient-card__radial" picking-mode="Ignore" />
    <ui:VisualElement class="gradient-card__content" />
</ui:VisualElement>
```

```css
.gradient-card {
    position: relative;
    overflow: hidden;
    border-radius: 26px;
}

.gradient-card__linear,
.gradient-card__radial {
    position: absolute;
    background-color: white;
}

.gradient-card__linear {
    left: 0;
    right: 0;
    top: 0;
    bottom: 0;
    -unity-material: resource("XeriUI/Materials/LinearGrad")
        prop("_Color0" #10294c)
        prop("_Color1" #29235f)
        prop("_ColorCount" 2)
        prop("_Stop0" 0 0)
        prop("_Stop1" 1 1)
        prop("_Angle" 116)
        prop("_Tiling" 1);
}

.gradient-card__radial {
    left: -120px;
    top: -180px;
    width: 520px;
    height: 520px;
    border-radius: 50%;
    opacity: 0.56;
    -unity-material: resource("XeriUI/Materials/RadialGrad")
        prop("_Color0" rgba(77, 235, 255, 0.72))
        prop("_Color1" rgba(77, 235, 255, 0))
        prop("_ColorCount" 2)
        prop("_Stop0" 0 0)
        prop("_Stop1" 0.75 0.75)
        prop("_Center" 0.5 0.5)
        prop("_Radius" 0.5 0.5)
        prop("_Tiling" 1);
}
```

부모의 `overflow: hidden`과 `border-radius`가 자식 Gradient를 같은 영역으로 자른다. Gradient
요소는 `picking-mode="Ignore"`로 두고 Button과 입력 요소는 Content Layer에 둔다.

### Gamma Compositor

Gradient Material은 Gamma Compositor 없이도 표시된다. Gamma Compositor는 Linear Color
Space에서 USS 색상을 웹의 sRGB 결과에 가깝게 합성하는 Layer 단위 경로다.

기본 `GameUIUITKLayer.prefab`은 `Use Gamma Compositing`이 켜져 있다. Linear Color Space에서
Layer가 활성화되면 다음 경로를 `UITKLayerPanel`이 자동 구성한다.

```text
원본 UIDocument
    → Layer 전용 Linear UNORM RenderTexture
    → 화면용 합성 UIDocument
    → gamma-to-linear 합성
```

호출자는 RenderTexture나 합성 UIDocument를 만들지 않는다. Layer가 해제되면 Runtime이
`PanelSettings`의 Target Texture, Depth/Stencil, Clear와 Gamma 상태를 원래 값으로 복원한다.
Gamma Color Space에서는 원본 UIDocument를 직접 표시한다.

활성 Gamma Layer마다 화면 크기의 RenderTexture 하나를 사용한다. 웹 색상 일치가 필요하지
않은 Layer만 Prefab Variant에서 옵션을 끈다.

### XeriLoopAnimator

`XeriLoopAnimator`는 USS 값을 계산하지 않는다. Transition 완료 Event를 받아 다음 Class를
적용하는 방식으로 선언된 단계를 반복한다.

```xml
<ui:VisualElement class="example-pulse xeri-loop" />
```

```css
.example-pulse {
    opacity: 0.5;
    transition: opacity 0.8s ease-in-out;
    --xeri-next: "example-pulse-on";
    --xeri-loop-trigger: "opacity";
}

.example-pulse.example-pulse-on {
    opacity: 1;
    --xeri-next: "example-pulse-off";
}

.example-pulse.example-pulse-off {
    opacity: 0.2;
    --xeri-next: "example-pulse-on";
}
```

반복할 `UIDocument`와 같은 GameObject에 Component를 추가한다. Gamma Compositor와 Loop
Animator는 독립 기능이며 반복이 필요한 문서에만 Animator를 둔다.
