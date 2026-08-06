# Xeri Game UI 가이드

Xeri Game UI는 UGUI와 UI Toolkit으로 만든 게임 화면을 같은 Runtime에서 운용하기 위한
라이브러리다. Layer 순서, Screen Stack, Modal, Focus, Input, Transition과 표시 객체의 수명을
공통 계약으로 관리한다.

이 문서는 개발자와 AI가 내부 구현을 먼저 해석하지 않고도 화면을 추가할 수 있도록 작성한
사용 가이드다. 프로젝트 도메인, 화면 디자인과 데이터 모델은 다루지 않으며 Xeri가 공개한
계약과 제공 에셋만 설명한다.

- Core Namespace: `inonego.Xeri.UI.Game`
- Xeri UI Toolkit Namespace: `inonego.Xeri.UI`
- 지원 Backend: UGUI, UI Toolkit, 두 Backend의 혼합 구성
- 기준 Unity: Unity 6 이상

## 필수 환경

Xeri Package의 `package.json`이 UGUI, Input System과 Addressables를 설치한다. DOTween은
Package dependency에 포함되지 않으므로 사용하는 프로젝트가 별도로 설치해야 한다.

`inonego.Xeri` Assembly는 `DOTween.Modules`를 직접 참조하고 `GameUIRuntime`은 항상
`DOTweenPresentationTransitioner`를 사용한다. 따라서 DOTween은 선택형 Transition Backend가
아니라 현재 Xeri Runtime의 필수 의존성이다. DOTween과 `DOTween.Modules` Assembly가 준비되지
않은 프로젝트에서는 Xeri Runtime이 컴파일되지 않는다.

## 먼저 실행할 샘플

Package Manager에서 `Xeri > Samples > Game UI Validation`을 Import한다.

```text
Assets/Samples/Xeri/<version>/Game UI Validation/GameUIValidation.unity
```

이 Scene은 실제 공개 API로 Screen Stack, Modal, Overlay, Focus, Transition, Spotlight와
Gamma Compositor를 조립한다. 화면에 표시되는 Runtime 배지는 실행 환경을 뜻한다.

| 표시 | 의미 | 검증 범위 |
|---|---|---|
| `STANDALONE / FULL VALIDATION` | Sample이 Runtime을 직접 생성 | Context 기능과 Scene Fade·Input Settings |
| `SHARED RUNTIME / CONTEXT VALIDATION` | 기존 Runtime 아래에 Sample Child Context 생성 | Screen·Modal·Overlay·Focus·Spotlight와 Context 수명 |

`Web~/index.html`은 기능 없는 1920×1080 HTML/CSS 시각 기준본이다. Unity 화면과 레이아웃,
색상, 폰트와 Gradient 수치를 비교할 때 사용한다.

## 전체 구조

일반적인 사용 흐름은 다음과 같다.

```text
GameUISettingsAsset
    ↓
GameUIBootstrapperModuleAsset + GameUIHost.prefab
    ↓
GameUIRuntime.Current
    ↓
GameUIRuntime.Main
    ↓
GameUIProfileAsset 획득
    ↓
ScreenOptions + IScreenSource 등록
    ↓
ScreenController.Open / Replace / Close / Clear
    ↓
Session → Registration → Source → Profile 순서로 종료
```

Runtime과 Context의 책임은 다음과 같이 나뉜다.

```text
GameUIRuntime
├── Main
│   ├── LayerRegistry
│   ├── ScreenRegistry
│   ├── Screens
│   ├── Modals
│   └── 선택적 Child Context
├── SceneFader
├── Visibility
└── Settings
```

| 구성 요소 | 책임 |
|---|---|
| `GameUIRuntime` | App 범위 UI Runtime과 전역 Scene Fade |
| `GameUIContext` | 독립 Layer 공간, Screen Registry, Stack, Modal Stack과 Focus 기록 |
| `PresentationLayerRegistry` | UGUI·UITK 공통 Layer ID, Order와 사용 수명 |
| `ScreenRegistry` | Screen ID와 `ScreenOptions`, `IScreenSource`의 대응 관계 |
| `ScreenController` | Context 안의 Screen Stack과 상태 전환 |
| `ModalController` | Modal 순서와 top 상호작용 상태 |
| `FocusController` | Context별 기본·마지막·대체 Focus 기록 |

대부분의 UI는 `runtime.Main`만 사용한다. 별도 Stack과 Focus 기록이 필요한 내장 창 같은
독립 표시 범위만 Child Context로 만든다.

## Runtime 설정

### 제공 에셋

아래 경로는 이 README가 있는 `Runtime/UI/Game`을 기준으로 한다.

| 에셋 | 경로 | 용도 |
|---|---|---|
| Runtime Host | `Assets/Host/GameUIHost.prefab` | Runtime, EventSystem, Input, Focus와 Fade Source |
| UGUI Layer | `Assets/Layer/GameUIUGUILayer.prefab` | Screen Space Overlay UGUI Layer |
| UI Toolkit Layer | `Assets/Layer/GameUIUITKLayer.prefab` | UIDocument와 선택형 Gamma 합성 |
| Fade Layer | `Assets/Layer/GameUIFadeLayer.asset` | 기본 Fade Layer ID와 Order |
| Fade View | `Assets/Fade/GameUIFadeView.prefab` | 기본 UGUI Fade View |
| Default Profile | `Assets/Profile/GameUIDefaultProfile.asset` | 기본 Fade Layer Profile |
| Panel Settings | `Assets/UITK/GameUIPanelSettings.asset` | Screen Space Overlay Panel 설정 |
| Runtime Theme | `Assets/UITK/GameUIRuntimeTheme.tss` | UI Toolkit Runtime Theme |
| UI Input Actions | `InputSystem/GameUIInputActions.inputactions` | Point, Move, Submit, Cancel과 Click |
| Gradient Materials | `../XeriUI/Resources/XeriUI/Materials` | Linear, Radial과 Conic Gradient |

기본 Profile은 `SystemFade` Layer만 제공한다. Screen, Modal과 Overlay Layer는 사용하는
표시 범위에 맞춰 별도 Profile로 만든다.

### Settings 만들기

`Assets > Create > Xeri > UI > Game > Settings`에서 `GameUISettingsAsset`을 만든다.

| 필드 | 계약 |
|---|---|
| `DefaultProfile` | Runtime과 함께 유지할 기본 Profile |
| `SceneFadeLayerID` | Default Profile에 포함된 Fade Layer ID |
| `DefaultFadeColor` | 기본 Fade 색상 |
| `DefaultFadeDuration` | 0 이상의 기본 Fade 시간 |
| `UIActionMap` | UI 전용 Action Map 이름 |
| `GameplayActionsAsset` | Gameplay 입력을 소유한 Input Action Asset |
| `GameplayActionMap` | Gameplay Action Map 이름 |
| `ReleaseActionNames` | Screen 종료 뒤 입력 해제를 기다릴 UI Action 이름 |

`UIActionMap`과 `GameplayActionMap`은 서로 달라야 한다. Xeri가 제공하는 Input Actions는
UI 입력만 포함하며 프로젝트 Gameplay Actions를 대신하지 않는다.

### Bootstrapper 연결

`Assets > Create > Xeri > Bootstrapper > Game UI Module`에서
`GameUIBootstrapperModuleAsset`을 만든다.

1. `Host Prefab`에 `Assets/Host/GameUIHost.prefab`을 지정한다.
2. `Settings`에 `GameUISettingsAsset`을 지정한다.
3. Module을 Xeri `BootstrapperSettings`에 등록한다.

Bootstrapper는 Host를 한 번 생성하고 Runtime을 초기화한다. 이 경로를 사용하면 Scene마다
`GameUIRuntime`이나 `EventSystem`을 배치하지 않는다. 수동 초기화와 Bootstrapper 초기화를
동시에 사용하지 않는다.

초기화가 끝난 뒤 일반 코드는 다음 진입점을 사용한다.

```csharp
GameUIRuntime runtime = GameUIRuntime.Current;
GameUIContext context = runtime.Main;
```

Runtime 존재 여부가 실행 환경에 따라 달라지는 Sample이나 도구는 `TryCurrent`를 사용한다.

```csharp
if (!GameUIRuntime.TryCurrent(out GameUIRuntime runtime))
{
    return;
}
```

## Layer와 Profile

### Layer 정의

`Assets > Create > Xeri > UI > Game > Presentation Layer`에서
`PresentationLayerAsset`을 만든다.

| 필드 | 의미 |
|---|---|
| `ID` | Screen과 표시 기능이 조회하는 stable string ID |
| `Order` | UGUI와 UI Toolkit이 공유하는 Screen Space Overlay 순서 |

동시에 활성화되는 Layer의 ID와 Order는 각각 고유해야 한다. UGUI와 UI Toolkit은 같은 Order
공간을 사용하므로 Backend가 다르다는 이유로 같은 Order를 중복하지 않는다.

Layer Prefab Root에는 `IPresentationLayerDriver` 구현이 정확히 하나 있어야 한다.

| Driver | 공개 Root | 필수 조건 |
|---|---|---|
| `UGUILayerCanvas` | `RectTransform` | Screen Space Overlay, 기본 Display와 Sorting Layer |
| `UITKLayerPanel` | `VisualElement` | UIDocument, 기본 Display, Target Texture 미지정 |

Camera Canvas, World Space Canvas, 다른 Display와 호출자 소유 RenderTexture Panel은 이 공통
Screen Space Overlay Order 계약 밖에서 호출자가 관리한다.

`UITKLayerPanel`은 활성화될 때 다음을 자동 처리한다.

- Layer별 `PanelSettings` Runtime 복제
- `PanelSettings.sortingOrder`와 `UIDocument.sortingOrder`
- Layer Root의 `xeri-game-ui` Class
- `GameUIRuntimeBaseline.uss`
- 옵션이 켜진 경우 Gamma Compositor 생성과 해제

`Root Name`이 비어 있으면 `UIDocument.rootVisualElement` 전체가 Layer Root다. 이름을 지정하면
그 이름의 하위 `VisualElement`만 공개 Root가 된다.

### Profile 획득

`Assets > Create > Xeri > UI > Game > Profile`에서 `GameUIProfileAsset`을 만든다. Entry 하나는
`PresentationLayerAsset`과 Layer Prefab용 `IGameObjectProvider`를 묶는다.

```csharp
GameUIProfileHandle profileHandle = runtime.AcquireProfile(profile);
```

Scene이나 게임 모드에서 필요한 Profile은 해당 범위의 조립 객체가 소유한다. 포함된 Layer를
사용하는 Screen, Overlay, Modal과 Drag Visual이 모두 종료된 뒤 Handle을 해제한다.

```csharp
profileHandle.Dispose();
profileHandle = null;
```

활성 Layer 소비자가 남아 있으면 종료는 상태 변경 전에 거부되고 소유권이 유지된다. 소비자를
종료한 뒤 같은 Handle로 다시 요청할 수 있다. 실제 종료가 시작된 뒤의 일반 실패는 재시도하지
않는다.

## Screen 구현

Screen 하나는 세 계약으로 구성한다.

| 계약 | 책임 |
|---|---|
| `ScreenOptions` | ID, Layer, 중복, Focus, Input과 Transition 정책 |
| `IScreenSource` | View 획득, Binding과 대칭 반환 |
| `ScreenInstance` | `IScreenDriver`와 선택적 `IScreenStateHandler` |

### Options

```csharp
var options = new ScreenOptions
(
    id: "example.screen",
    layerID: "Screen",
    duplicatePolicy: ScreenDuplicatePolicy.Reject,
    blocksGameplayInput: true,
    showsCursor: true,
    cursorLockMode: CursorLockMode.None,
    inputPriority: 100,
    openDuration: 0.2f,
    closeDuration: 0.2f,
    usesUnscaledTime: true
);
```

Focus 후보는 다음 순서로 선택된다.

1. 유효한 `ScreenOptions.DefaultFocus`
2. 유효한 `IScreenDriver.DefaultFocus`
3. Focus Driver fallback

동적으로 생성되는 View의 기본 Focus는 Source가 생성한 Driver에 전달한다.

### Source

`IScreenSource.Acquire`는 한 호출 안에서 다음 작업을 끝낸다.

1. `scope.Layer`를 필요한 Root 타입으로 확인한다.
2. View를 생성하거나 Provider에서 획득한다.
3. `scope.OpenParams.Payload`를 필요한 타입으로 확인한다.
4. Button Callback과 데이터 Binding을 연결한다.
5. `ScreenInstance`를 반환한다.

중간에 실패하면 이번 호출에서 만든 View와 Binding을 즉시 정리하고 예외를 전달한다. 실패한
View를 Source 소유 목록에 남기지 않는다.

`Release`는 Callback과 Binding을 해제하고, Source 소유 매핑에서 제거한 뒤 View를 원래
공급 경로로 반환한다.

```csharp
public sealed class ExampleScreenSource : IScreenSource
{
    public ScreenInstance Acquire(ScreenViewScope scope)
    {
        // Layer 확인 → View 획득 → Payload 검증 → Binding 연결
        return new ScreenInstance(driver, stateHandler);
    }

    public void Release(ScreenInstance instance)
    {
        // Binding 해제 → View 반환
    }
}
```

UGUI Source는 `IPresentationLayerDriver<RectTransform>`을 요구하고 View Prefab의
`UGUIScreenDriver`를 반환한다. UI Toolkit Source는
`IPresentationLayerDriver<VisualElement>`을 요구하고 `VisualTreeAsset`을 Clone한다.

```csharp
var screenRoot = viewAsset.Instantiate();
layer.Root.Add(screenRoot);

var defaultFocus = screenRoot.Q<VisualElement>("DefaultFocus");
var driver = new UITKScreenDriver(screenRoot, defaultFocus);
return new ScreenInstance(driver, stateHandler);
```

UI Toolkit Source는 `Release`에서 Callback과 Binding을 먼저 해제한 뒤 생성한 Root에
`RemoveFromHierarchy()`를 호출한다.

### 등록과 Open

Screen Layer를 포함한 Profile을 먼저 획득한 뒤 등록한다.

```csharp
ScreenRegistrationHandle registration = context.ScreenRegistry.Register
(
    options,
    source
);
```

```csharp
ScreenOpenResponse response = context.Screens.Open
(
    "example.screen",
    new ScreenOpenParams(payload)
);

if (!response.Accepted)
{
    Debug.LogError($"{response.Kind}: {response.Error}");
}
```

`Payload`는 호출자가 소유한다. Source는 타입을 확인해 읽지만 Payload의 수명을 종료하지 않는다.

| `ScreenOpenKind` | 의미 |
|---|---|
| `Accepted` | Session 생성과 Open 시작 수락 |
| `Rejected` | 등록, 중복 정책 또는 현재 상태에서 거부 |
| `Cancelled` | `OnOpening`에서 취소 |
| `SourceFailed` | View 획득 또는 Binding 실패 |
| `TransitionFailed` | Open Transition 시작 실패 |

### Stack 명령

```csharp
context.Screens.Open("example.first");
context.Screens.Replace("example.second");
context.Screens.Close();
context.Screens.Clear();
```

| 명령 | 동작 |
|---|---|
| `Open` | 현재 top을 Covered로 만들고 새 Screen 추가 |
| `Replace` | 새 Open이 수락되면 기존 top 대체 |
| `Close` | 현재 top을 정상 Transition으로 닫음 |
| `Clear` | 모든 Screen을 최신 항목부터 Transition 없이 종료 |

View의 닫기 Button은 전역 Stack을 다시 조회하지 않고 자신이 받은 Session을 닫는다.

```csharp
scope.Session.Close();
```

### 상태 훅과 자식 수명

선택적 `IScreenStateHandler`는 동기 훅을 제공한다.
MonoBehaviour 기반 Screen은 `ScreenStateBehaviour`를 상속해 필요한 훅만 재정의할 수 있다.

| 훅 | 시점 |
|---|---|
| `OnOpening` | Open Transition 전, 취소 가능 |
| `OnOpened` | Open Transition 완료 후 |
| `OnClosing` | 정상 Close Transition 전, 취소 가능 |
| `OnClosed` | 자식 Handle과 Close Transition 정리 후 |

훅 안에서 다른 `Open`, `Replace`, `Close` 또는 `Clear`를 재진입 호출하지 않는다. 후속 명령은
훅이 반환된 다음 실행한다.

Screen과 함께 닫을 Handle은 Session에 넘긴다.

```csharp
response.Session.RegisterChild(ownedHandle);
```

등록된 자식은 Session 종료 시 등록 역순으로 한 번 해제된다.

## Main과 Child Context

Main Context는 일반적인 App UI의 기본 표시 범위다. Child Context는 다음 조건을 모두 만족할
때만 만든다.

- Parent와 독립된 Screen Stack 또는 Modal Stack이 필요하다.
- Parent와 별도의 마지막 Focus 기록이 필요하다.
- Child 전체를 하나의 수명으로 종료할 소유자가 있다.

```csharp
GameUIContext windowContext = runtime.Main.CreateChild(windowLayerRegistry);
windowContext.Focus();

// Window 범위가 끝날 때 Screen, Modal과 하위 Context를 함께 종료한다.
windowContext.Dispose();
```

Layer Registry를 생략하면 Parent와 같은 표시 공간을 공유한다. 특정 Window Root 안에 UI를
배치하려면 해당 Root의 Layer Driver를 별도 `PresentationLayerRegistry`에 등록해 전달한다.
Context는 전달받은 Registry를 소유하거나 해제하지 않는다.

`Focus()`와 `Unfocus()`는 실제 Focus 적용 Context만 전환한다. Visibility, Raycast, Input Map,
Layer 순서나 Screen 상태는 변경하지 않는다.

`GameUIFocusDriver`는 같은 Host의 `FocusDriverBehaviour` Component를 수집해 하나의
Focus 경로로 합친다. UGUI EventSystem과 UI Toolkit Panel 처리는 각 Driver에 남고,
공통 Driver는 구체 backend이나 대상 타입을 알지 않는다. 현재 Focus가 유효한 대상 없이
끝나면 Focus 권한을 가진 Context의 마지막·화면 기본·Driver 기본·대체 Focus 순서가
다시 적용된다.

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
Drop 계약은 [Drag_Drop README](../Drag_Drop/README.md)를 따른다.

## Focus와 Input

Screen Stack이 바뀌면 Runtime은 다음 상태를 합성한다.

- Options 또는 Driver의 기본 Focus
- Screen별 마지막 Focus와 fallback
- Gameplay Action별 기존 활성 상태
- UI Action Map 활성 상태
- Cursor 표시와 Lock Mode
- `InputPriority`

Screen Source가 별도 전역 Focus Manager나 Input Map 전환 코드를 만들지 않는다.

Host의 `InputSystemUIInputModule`에는 Point, Move, Submit, Cancel과 Click Action Reference가
실제로 연결되어 있어야 한다. Xeri의 UI Input Actions와 프로젝트 Gameplay Input Asset은
별도 에셋으로 유지한다.

마지막 입력 장치 변경은 Runtime에서 구독할 수 있다.

```csharp
runtime.OnLastInputDeviceChanged += HandleInputDeviceChanged;
```

구독한 소유자는 Runtime 종료 전에 같은 Callback을 해제한다.

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

## 종료와 소유권

| 획득 결과 | 소유자가 종료할 항목 | 종료 전 조건 |
|---|---|---|
| `GameUIProfileHandle` | Profile Layer | 해당 Layer 소비자 종료 |
| `ScreenRegistrationHandle` | Screen 등록 | 해당 Source의 열린 Session 종료 |
| `ScreenSession` | View, Layer Usage, Input과 자식 Handle | 정상 Close는 현재 top |
| `OverlayHandle<TView>` | Overlay View와 Layer Usage | 없음 |
| `ModalHandle` | Modal Stack 항목과 전달된 Handle | 없음 |
| `Lease` | Visibility, Spotlight 또는 Input Block 요청 | 없음 |
| `DragVisualHandle` | Drag 부모와 Transform | 없음 |
| Child `GameUIContext` | Screen, Modal, Focus와 하위 Context | 소유 범위 종료 |

일반적인 종료 순서는 다음과 같다.

1. 열린 Screen과 Session 자식을 닫는다.
2. `ScreenRegistrationHandle`을 해제한다.
3. Source, Binding과 선택 기능 객체를 해제한다.
4. 추가 `GameUIProfileHandle`을 해제한다.
5. App 종료에서만 `runtime.Shutdown()`을 호출한다.

일반 `Dispose`, `Release`와 Callback 정리는 attempt-once다. 정리가 시작된 객체를 재시도
Registry나 복구 상태 머신에 넣지 않는다. 상태 변경 전 사전 조건 거부로 소유권이 유지되는
경우만 일반 종료 실패와 구분한다.

## 새 UI를 추가하는 체크리스트

AI와 개발자는 새 화면을 만들기 전에 다음을 확정한다.

- 일반 UI이면 `runtime.Main`, 실제 독립 Stack이 필요할 때만 Child Context를 사용한다.
- 화면이 사용할 Layer ID와 해당 Layer를 제공하는 Profile 소유자를 정한다.
- UGUI `RectTransform`과 UITK `VisualElement` 중 View Root Backend를 정한다.
- View 획득, Binding과 반환을 하나의 `IScreenSource`가 대칭 소유하게 한다.
- Screen은 표시·입력 정책만 담고 프로젝트 데이터와 도메인 명령은 Source 밖의 Presenter가 맡는다.
- Session, 등록 Handle, Profile Handle과 선택 기능 Lease의 저장 위치를 정한다.
- 정상 종료 순서를 코드 작성 전에 확정한다.
- 실제 호출 경로가 없는 Manager, Backend enum, Dispose 재시도 관리자나 복구 Registry를 만들지 않는다.

기존 공개 확장점은 다음과 같다.

| 필요한 역할 | 사용할 계약 |
|---|---|
| 새 Layer Root | `IPresentationLayerDriver<TRoot>` |
| 새 Screen 표시 Backend | `IScreenDriver` |
| Screen View 획득과 반환 | `IScreenSource` |
| Screen 상태 관찰 | `IScreenStateHandler` |
| Focus Backend | `IFocusDriver` |
| Input Backend | `IScreenInputDriver` |
| Transition Backend | `IPresentationTransitioner` |
| Overlay View | `IOverlaySource<TView>` |
| Modal 표시 | `IModalDriver` |
| Visibility 대상 | `IVisibilityTarget` |
| Spotlight 표시 Backend | `ISpotlightDriver<TParams>` |
| 중첩 Pointer 차단 | `IInteractionBlocker` |

`IPresentationTransitioner`는 Core Controller를 직접 조립할 때 사용할 수 있는 계약이다.
기본 `GameUIRuntime`은 구현을 주입받지 않고 `DOTweenPresentationTransitioner`를 사용한다.

## 문제 확인

| 증상 | 확인 순서 |
|---|---|
| Runtime 초기화 실패 | Host 필수 Component → Settings → Default Profile → Fade Layer |
| UI 입력 없음 | Input Module Action Reference → UI Map → Gameplay Asset와 Map |
| Layer 등록 실패 | Profile 활성 → ID 중복 → Order 중복 → Driver Validate |
| Screen Open 거부 | Screen 등록 → Layer ID → 중복 정책 → Stack 명령 상태 |
| Focus 없음 | Options Focus → Driver Focus → Focus Driver fallback |
| UITK Layer 미표시 | UIDocument → PanelSettings → Root Name → Target Texture |
| World UI 위치 반전 | Projector와 Bounds Root → UGUI `YUp` / UITK `YDown` |
| Spotlight 구멍 위치 오류 | Driver와 Target Panel/Canvas → Target 표시 상태 → Padding |
| Spotlight가 전체 Pointer 입력 차단 | 유효 Target → Target 표시 상태 → Lease 반환 |
| Gradient가 어두움 | `background-color: white` → Color Space → Gamma 옵션 |
| 둥근 Clip에서 자식이 사라짐 | Gamma 옵션 → RT Depth/Stencil → Shader Console 오류 |
| Gamma Layer 메모리 증가 | 활성 Gamma Layer 수 → 화면 해상도 |
| Modal이나 Overlay 잔존 | Handle 소유자 → Session 자식 등록 |
| Profile 종료 거부 | Screen, Overlay, Modal 또는 Drag Layer Usage |
| Loop Animation 정지 | `xeri-loop` → `--xeri-next` → Duration → Trigger Property |

## 소스 위치

README의 공개 사용법으로 부족할 때만 내부 소스를 확인한다.

| 관심사 | 경로 |
|---|---|
| Runtime 조립 | `Runtime/GameUIRuntime.cs` |
| Context와 수명 | `Runtime/Context/GameUIContext.cs` |
| Settings | `GameUISettingsAsset.cs` |
| Profile | `GameUIProfileAsset.cs`, `Runtime/Profile` |
| Layer Core | `Core/Presentation/Layer` |
| Screen Core | `Core/Screen` |
| Modal, Overlay와 Visibility | `Core/Presentation` |
| Placement와 Projection | `Core/Presentation/Placement` |
| 범용 Tracking | `Runtime/Tracking` |
| Spotlight Lease 정책 | `Core/Presentation/Spotlight` |
| Focus와 Input | `Core/Interaction`, `InputSystem` |
| UGUI Backend | `UGUI` |
| UI Toolkit Backend | `UITK` |
| 필수 DOTween Transition 구현 | `DOTween` |
| Gamma Compositor | `../XeriUI/Presentation/Gamma` |
| Gradient Material과 Shader | `../XeriUI/Resources/XeriUI` |
| Loop Animator | `../XeriUI/Animation/Loop` |
