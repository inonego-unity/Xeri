# Xeri Game UI 사용 가이드

Xeri Game UI는 UGUI와 UI Toolkit을 같은 Runtime에서 사용하는 게임 UI 기반 라이브러리다.
Layer 순서, Screen Stack, Focus, Input, Transition과 표시 객체의 수명을 공통 계약으로
관리한다.

이 문서는 처음 사용하는 개발자와 AI가 소스 코드를 먼저 해석하지 않고도 UI를 구성할 수
있도록 작성한 사용 가이드다. 설명과 예제는 Xeri 라이브러리의 공개 API만 다룬다.

- Core Namespace: `inonego.Xeri.UI.Game`
- Xeri UI Toolkit Namespace: `inonego.Xeri.UI`
- 지원 UI: UGUI, UI Toolkit, 두 기술을 함께 사용하는 구성
- 기준 Unity: Unity 6 이상
- Gradient Shader: Unity UI Toolkit 공통 Shader 계약 사용

## 목차

1. [전체 사용 흐름](#전체-사용-흐름)
2. [핵심 구성 요소](#핵심-구성-요소)
3. [최초 설정](#최초-설정)
4. [Layer와 Profile 구성](#layer와-profile-구성)
5. [Screen 하나 만들기](#screen-하나-만들기)
6. [Screen Stack 사용](#screen-stack-사용)
7. [상태 훅과 자식 수명](#상태-훅과-자식-수명)
8. [Fade, Modal과 Overlay](#fade-modal과-overlay)
9. [Focus와 Input](#focus와-input)
10. [UGUI 보조 기능](#ugui-보조-기능)
11. [UI Toolkit 웹 스타일 표현](#ui-toolkit-웹-스타일-표현)
12. [종료와 소유권](#종료와-소유권)
13. [AI 작업 절차](#ai-작업-절차)
14. [문제 확인](#문제-확인)
15. [소스 탐색표](#소스-탐색표)

## 전체 사용 흐름

Xeri Game UI를 사용하는 순서는 다음과 같다.

```text
GameUISettingsAsset 작성
    ↓
GameUIBootstrapperModuleAsset에 Host와 Settings 연결
    ↓
GameUIRuntime 초기화
    ↓
필요한 GameUIProfileAsset 획득
    ↓
IScreenSource 작성 및 ScreenRegistry 등록
    ↓
ScreenController.Open / Replace / Close / Clear
    ↓
Screen → 등록 → Profile 순서로 해제
    ↓
GameUIRuntime.Shutdown
```

가장 중요한 기준은 다음 세 가지다.

1. `GameUIRuntime`은 Singleton이 아니다. Runtime을 만든 객체가 참조를 보관하고 전달한다.
2. `Register`, `Acquire`, `Open`이 반환한 Handle과 Session은 명확한 소유자가 종료한다.
3. View 생성과 Binding은 `IScreenSource.Acquire`, 반환은 같은 Source의 `Release`가 담당한다.

## 핵심 구성 요소

```text
GameUIRuntime
├── LayerRegistry       Layer 등록과 사용 수명
├── ScreenRegistry      ScreenOptions와 IScreenSource 등록
├── Screens             Screen Stack
├── Focus               기본·마지막·대체 Focus
├── SceneFader          전체 화면 Cover / Reveal
├── Modals              Modal Stack
├── Visibility          중첩 표시 요청
└── Settings            현재 Runtime 설정
```

| 구성 요소 | 의미 |
|---|---|
| `GameUISettingsAsset` | Runtime의 기본 Profile, Fade와 Input 설정 |
| `GameUIProfileAsset` | 함께 활성화할 Presentation Layer 묶음 |
| `PresentationLayerAsset` | Layer의 stable ID와 공통 Order |
| `IPresentationLayerDriver` | UGUI 또는 UI Toolkit Layer backend |
| `ScreenOptions` | Screen의 Layer, 중복, Focus, Input, Transition 정책 |
| `IScreenSource` | Screen View 생성·Binding·반환 책임 |
| `ScreenSession` | 열린 Screen 하나의 상태와 수명 |

Xeri는 화면 내용, 데이터 Binding 방식 또는 View 생성 방식을 강제하지 않는다. Prefab,
Addressables, Pool과 `VisualTreeAsset` 중 무엇을 사용할지는 `IScreenSource`가 결정한다.

## 최초 설정

### 의존성

Game UI Runtime은 다음 Unity Package를 사용한다.

- Addressables
- Input System
- UGUI
- UI Toolkit
- DOTween Modules

Gradient Material은 특정 Scriptable Render Pipeline의 Shader Library에 의존하지 않는다.
Unity UI Toolkit의 내부 `UnityUIE` Shader 계약을 사용하므로 패키지 기준 Unity 버전에서
사용한다.

### 제공 에셋

아래 경로는 이 README가 있는 `Runtime/UI/Game`을 기준으로 한다.

| 에셋 | 경로 | 용도 |
|---|---|---|
| Runtime Host | `Assets/Host/GameUIHost.prefab` | Runtime, EventSystem, Input, Focus, Fade Source |
| UGUI Layer | `Assets/Layer/GameUIUGUILayer.prefab` | Screen Space Overlay UGUI Layer |
| UI Toolkit Layer | `Assets/Layer/GameUIUITKLayer.prefab` | UIDocument와 선택형 Gamma 합성 |
| Fade Layer | `Assets/Layer/GameUIFadeLayer.asset` | 기본 Fade Layer ID와 Order |
| Fade View | `Assets/Fade/GameUIFadeView.prefab` | 기본 UGUI Fade View |
| Default Profile | `Assets/Profile/GameUIDefaultProfile.asset` | 기본 Fade Layer Profile |
| Panel Settings | `Assets/UITK/GameUIPanelSettings.asset` | Screen Space Overlay Panel 설정 |
| Runtime Theme | `Assets/UITK/GameUIRuntimeTheme.tss` | UI Toolkit 기본 Theme |
| UI Input Actions | `InputSystem/GameUIInputActions.inputactions` | UI 입력 Actions |
| Gradient Materials | `../XeriUI/Resources/XeriUI/Materials` | Linear, Radial, Conic Material |

Default Profile에는 `SystemFade` Layer만 들어 있다. Screen, Modal, Overlay 등 다른 Layer는
사용 목적에 맞는 Profile을 별도로 만든다.

### GameUISettingsAsset 만들기

`Assets > Create > Xeri > UI > Game > Settings`에서 생성한다.

| 필드 | 설정 방법 |
|---|---|
| `DefaultProfile` | Runtime과 함께 유지할 기본 Profile |
| `SceneFadeLayerID` | Default Profile에 들어 있는 Fade Layer ID |
| `DefaultFadeColor` | 기본 Fade 색상 |
| `DefaultFadeDuration` | 0 이상의 기본 Fade 시간 |
| `UIActionMap` | UI 전용 Action Map 이름 |
| `GameplayActionsAsset` | Gameplay 입력을 소유한 Input Action Asset |
| `GameplayActionMap` | Gameplay Action Map 이름 |
| `ReleaseActionNames` | Screen 종료 뒤 입력 해제를 기다릴 UI Action 이름 |

`UIActionMap`과 `GameplayActionMap`은 서로 달라야 한다. Xeri가 제공하는
`GameUIInputActions`는 UI 입력만 포함하며 Gameplay Actions를 대신하지 않는다.

### Bootstrapper 연결

`Assets > Create > Xeri > Bootstrapper > Game UI Module`에서
`GameUIBootstrapperModuleAsset`을 생성한다.

1. `Host Prefab`에 `Assets/Host/GameUIHost.prefab`을 지정한다.
2. `Settings`에 앞에서 만든 `GameUISettingsAsset`을 지정한다.
3. Module을 Xeri `BootstrapperSettings`에 등록한다.

Bootstrapper는 Host를 한 번 만들고 `GameUIRuntime.Initialize(settings)`를 호출한다.
이 경로를 사용하면 Scene에 별도 `GameUIRuntime`이나 `EventSystem`을 배치하지 않는다.

Bootstrapper를 사용하지 않는 경우 Host Prefab을 한 번 생성하고 Runtime의 `Initialize`를
직접 호출할 수 있다. Bootstrapper 초기화와 수동 초기화를 함께 사용하지 않는다.

### Runtime 참조 전달

`GameUIRuntime.Instance`는 없다. Runtime을 생성한 수명 소유자가 참조를 보관하고 Screen
등록자나 UI 조립 객체에 생성자, 직렬화 참조 또는 명시적 초기화 메서드로 전달한다.

Bootstrapper Host에 UI 조립 Component를 붙일 때는 패키지 Prefab을 직접 수정하지 않고
Prefab Variant를 사용한다. 하나의 조립 Component가 다음 이벤트를 받아 자신이 만든
Profile, Source와 등록 Handle을 관리하는 구성이 가장 단순하다.

- `OnInitialized`: 추가 Profile 획득과 Screen 등록
- `OnReleasing`: 자신이 소유한 항목을 역순으로 해제

여러 `OnReleasing` 구독자를 독립 정리 파이프라인처럼 사용하지 않는다. 종료 순서가 필요한
항목은 하나의 소유자가 명시적으로 관리한다.

## Layer와 Profile 구성

### PresentationLayerAsset

`Assets > Create > Xeri > UI > Game > Presentation Layer`에서 생성한다.

| 필드 | 의미 |
|---|---|
| `ID` | Screen과 표시 기능이 조회하는 stable string ID |
| `Order` | UGUI와 UI Toolkit이 함께 사용하는 Screen Space Overlay 순서 |

동시에 활성화되는 모든 Profile에서 Layer ID와 Order는 각각 고유해야 한다.

### Layer Prefab

Layer Prefab Root에는 `IPresentationLayerDriver` 구현이 정확히 하나 있어야 한다.

| Driver | Root | 필수 조건 |
|---|---|---|
| `UGUILayerCanvas` | `RectTransform` | Screen Space Overlay, 기본 Display와 Sorting Layer |
| `UITKLayerPanel` | `VisualElement` | UIDocument, 기본 Display, Target Texture 미지정 |

UGUI와 UI Toolkit은 같은 Registry와 Order 공간을 사용한다. 한 Scene이나 Profile에서 두
기술을 함께 사용할 수 있지만, 각 기술은 별도 Layer Prefab과 ID로 구성한다.

Camera Canvas, World Space Canvas, 다른 Display와 호출자 소유 RenderTexture Panel은 이
공통 Screen Space Overlay Order 계약 밖에 둔다.

`UITKLayerPanel`은 Layer마다 `PanelSettings` Runtime 복제본을 만들고 다음을 자동 처리한다.

- `PanelSettings.sortingOrder`와 `UIDocument.sortingOrder`
- Root에 `xeri-game-ui` Class 추가
- `GameUIRuntimeBaseline.uss` 연결
- 옵션이 켜진 경우 Gamma Compositor 생성과 해제

`Root Name`이 비어 있으면 `UIDocument.rootVisualElement`가 Layer Root다. 값을 지정하면
그 이름의 하위 `VisualElement`를 Root로 사용한다.

### GameUIProfileAsset

`Assets > Create > Xeri > UI > Game > Profile`에서 생성한다. Entry 하나는 다음 두 항목을
묶는다.

- `PresentationLayerAsset`
- Layer Prefab을 획득·반환하는 `IGameObjectProvider`

Runtime 기본 Layer는 Default Profile에 넣고, Scene이나 특정 모드에서만 필요한 Layer는
별도 Profile로 구성한다.

```csharp
GameUIProfileHandle profileHandle = runtime.AcquireProfile(profile);
```

Profile Handle은 포함된 Layer를 사용하는 Screen, Overlay, Modal과 Drag Visual이 모두
종료된 뒤 해제한다.

```csharp
profileHandle.Dispose();
profileHandle = null;
```

활성 Layer 소비자가 남아 있으면 `Dispose`는 상태 변경 전에 거부된다. 소비자를 종료한 뒤
같은 Handle로 다시 호출할 수 있다. 실제 종료가 시작된 이후의 일반 실패는 재시도하지 않는다.

## Screen 하나 만들기

Screen 하나는 세 부분으로 구성한다.

| 계약 | 책임 |
|---|---|
| `ScreenOptions` | ID, Layer, 중복, Focus, Input과 Transition 정책 |
| `IScreenSource` | View 획득, Binding, 대칭 반환 |
| `ScreenInstance` | `IScreenDriver`와 선택적 `IScreenStateHandler` |

### ScreenOptions 정의

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

`DefaultFocus`를 지정할 수도 있다. Focus 선택 순서는 다음과 같다.

1. 유효한 `ScreenOptions.DefaultFocus`
2. 유효한 `IScreenDriver.DefaultFocus`
3. Focus Driver fallback

동적으로 생성되는 View의 기본 Focus는 Source가 만드는 Driver에 전달하는 편이 자연스럽다.

### IScreenSource 구현 규칙

`Acquire`는 한 번의 호출에서 다음 작업을 완료한다.

1. `scope.Layer`를 필요한 Root 타입으로 확인한다.
2. View를 생성하거나 Provider에서 획득한다.
3. `scope.OpenParams.Payload` 타입을 확인한다.
4. Button Callback과 데이터 Binding을 연결한다.
5. `ScreenInstance`를 반환한다.

중간에 실패하면 이번 호출에서 만든 View와 Binding을 Source가 즉시 정리하고 예외를
전달한다. 실패한 View를 Source 소유 목록에 남기지 않는다.

`Release`는 반대 순서로 동작한다.

1. Callback과 Binding을 해제한다.
2. Source 소유 매핑에서 제거한다.
3. View를 Provider 또는 Visual Tree에 반환한다.

```csharp
public sealed class ExampleScreenSource : IScreenSource
{
    public ScreenInstance Acquire(ScreenViewScope scope)
    {
        // Layer Root 확인 → View 획득 → Payload 검증 → Binding 연결
        return new ScreenInstance(driver, stateHandler);
    }

    public void Release(ScreenInstance instance)
    {
        // Binding 해제 → View 반환
    }
}
```

위 코드는 책임 경계를 보여주는 골격이다. `driver`, `stateHandler`, View 공급 방식과
Binding 코드는 해당 UI의 Source가 소유한다.

### UGUI Screen Source

UGUI Source는 Layer를 `RectTransform` Root로 확인한다.

```csharp
if (!(scope.Layer is IPresentationLayerDriver<RectTransform> layer))
{
    throw new InvalidOperationException("RectTransform Layer가 필요합니다.");
}
```

View Prefab을 `layer.Root` 아래에 획득하고 View의 `UGUIScreenDriver`를
`ScreenInstance`에 전달한다. `UGUIScreenDriver`에는 표시 Root, `CanvasGroup`과 선택적
기본 Focus를 연결한다.

MonoBehaviour 상태 훅이 필요하면 `UGUIScreenStateHandler`를 상속한 Component를 같은
`ScreenInstance`에 전달한다.

### UI Toolkit Screen Source

UI Toolkit Source는 Layer를 `VisualElement` Root로 확인한다.

```csharp
if (!(scope.Layer is IPresentationLayerDriver<VisualElement> layer))
{
    throw new InvalidOperationException("VisualElement Layer가 필요합니다.");
}
```

`VisualTreeAsset`을 Clone하고 `layer.Root`에 추가한 뒤 Callback과 Binding을 연결한다.

```csharp
var screenRoot = viewAsset.Instantiate();
layer.Root.Add(screenRoot);

var defaultFocus = screenRoot.Q<VisualElement>("DefaultFocus");
var driver = new UITKScreenDriver(screenRoot, defaultFocus);
return new ScreenInstance(driver, stateHandler);
```

`Release`에서는 Callback과 Binding을 먼저 해제하고 `screenRoot.RemoveFromHierarchy()`를
호출한다. Source는 `ScreenInstance`와 생성한 Root의 대응 관계를 보관해야 한다.

### Screen 등록

Screen이 사용할 Layer Profile을 먼저 획득한다.

```csharp
ScreenRegistrationHandle registration = runtime.ScreenRegistry.Register
(
    options,
    source
);
```

등록 Handle을 해제하면 이후 Open 조회에서 제거된다. 이미 열린 Session은 Source를 계속
사용하므로 해당 Session을 모두 닫은 뒤 Source와 등록 Handle을 해제한다.

## Screen Stack 사용

### Open

```csharp
ScreenOpenResponse response = runtime.Screens.Open
(
    "example.screen",
    new ScreenOpenParams(payload)
);

if (!response.Accepted)
{
    Debug.LogError($"{response.Kind}: {response.Error}");
}
```

`ScreenOpenParams.Payload`는 호출자가 소유한다. Source는 필요한 타입으로 확인해 읽지만
Payload의 수명을 종료하지 않는다.

| `ScreenOpenKind` | 의미 |
|---|---|
| `Accepted` | Session 생성과 Open 시작 수락 |
| `Rejected` | 등록, 중복 정책 또는 현재 상태에서 거부 |
| `Cancelled` | `OnOpening`에서 취소 |
| `SourceFailed` | View 획득 또는 Binding 실패 |
| `TransitionFailed` | Open Transition 시작 실패 |

### Stack 명령

```csharp
runtime.Screens.Open("example.first");
runtime.Screens.Replace("example.second");
runtime.Screens.Close();
runtime.Screens.Clear();
```

| 명령 | 동작 |
|---|---|
| `Open` | 현재 top을 Covered로 만들고 새 Screen을 top에 추가 |
| `Replace` | 새 Open이 수락되면 기존 top을 대체 |
| `Close` | 현재 top을 정상 Transition 경로로 닫음 |
| `Clear` | 모든 생존 Screen을 최신 항목부터 Transition 없이 종료 |

View의 닫기 Button은 Stack을 다시 조회하지 않고 자신이 받은 Session을 닫는다.

```csharp
scope.Session.Close();
```

`ScreenSession.Close()`는 해당 Session이 현재 top일 때만 성공한다.

## 상태 훅과 자식 수명

선택적 `IScreenStateHandler`는 다음 동기 훅을 제공한다.

| 훅 | 시점 |
|---|---|
| `OnOpening` | Open Transition 전, 취소 가능 |
| `OnOpened` | Open Transition 완료 후 |
| `OnClosing` | 정상 Close Transition 전, 취소 가능 |
| `OnClosed` | 자식 Handle과 Close Transition 정리 후 |

```csharp
public void OnClosing(ScreenStateContext context)
{
    if (context.CanCancel && HasUnsavedChanges())
    {
        context.Cancel();
    }
}
```

모든 훅은 동기식이다. 훅 안에서 `Open`, `Replace`, `Close` 또는 `Clear`를 재진입 호출하지
않는다. 후속 Screen 명령은 훅이 반환된 뒤 실행한다.

Screen과 함께 닫힐 Handle은 Session에 등록한다.

```csharp
OverlayHandle<ExampleView> overlay = CreateOverlay();
response.Session.RegisterChild(overlay);
```

자식 Handle은 Session 종료 시 등록 역순으로 한 번 해제된다.

## Fade, Modal과 Overlay

### Scene Fade

`SceneFader`는 Settings의 Fade Layer와 Fade Source를 사용한다.

```csharp
var parameters = new SceneFadeParams(Color.black, 0.25f);

runtime.SceneFader.Cover
(
    parameters,
    onCompleted: HandleCovered,
    onFailed: HandleFadeFailure
);

runtime.SceneFader.Reveal
(
    parameters,
    onCompleted: HandleRevealed,
    onFailed: HandleFadeFailure
);
```

`Cover`가 완료되면 불투명 Overlay를 유지하고, `Reveal`이 완료되면 Overlay를 반환한다.
새 요청은 기존 Fade Transition을 취소한다. 실패는 요청 Callback과 `LastFailure`로 확인한다.

### Overlay

Overlay는 Screen Stack과 독립적으로 Layer를 잠시 점유하는 View다.

```csharp
OverlayHandle<ExampleView> overlay = OverlayHandle<ExampleView>.Acquire
(
    runtime.LayerRegistry,
    "Overlay",
    source
);
```

`IOverlaySource<TView>`가 View의 획득과 반환을 소유한다. Handle은 View와 Layer Usage를 함께
보관한다. Screen에 종속되면 `ScreenSession.RegisterChild`로 넘기고, 독립 수명이면 획득한
소유자가 `Dispose`한다.

UGUI Prefab에는 `GameObjectProviderOverlaySource<TView>`를 사용할 수 있다. UI Toolkit
Overlay는 `IOverlaySource<TView>`에서 Visual Tree 추가와 제거를 대칭 구현한다.

### Modal

`ModalController`는 View를 만들지 않는다. Modal Stack과 top 상호작용 상태만 관리한다.

- UGUI: `UGUIModalDriver`
- UI Toolkit: `UITKModalDriver`

Overlay로 Modal View를 획득했다면 해당 Handle의 소유권을 Modal에 넘긴다.

```csharp
ModalHandle modal = runtime.Modals.Open(driver, overlayHandle);
```

Modal Handle을 해제하면 현재 Modal을 닫고, 이전 Modal을 top으로 복원한 뒤 전달받은 Handle을
역순으로 해제한다.

### Visibility

`VisibilityController.Set`은 같은 Target에 대한 중첩 표시 요청을 합성한다.

```csharp
Lease hidden = runtime.Visibility.Set(target, visible: false);
```

가장 최근 요청이 실제 상태를 결정한다. 마지막 Lease를 해제하면 최초 상태로 복원한다.

## Focus와 Input

Screen Stack이 바뀌면 Runtime이 다음 상태를 자동 합성한다.

- Options 또는 Driver의 기본 Focus
- Screen별 마지막 Focus와 fallback
- Gameplay Action별 기존 활성 상태
- UI Action Map 활성 상태
- Cursor 표시와 Lock Mode
- `InputPriority`

Screen Source가 별도 전역 Focus Manager나 Input Map 전환 코드를 만들 필요는 없다.

Host의 `InputSystemUIInputModule`에는 Point, Move, Submit, Cancel과 Click Action Reference가
실제로 연결되어 있어야 한다. Xeri의 `GameUIInputActions.inputactions`를 UI Actions로
사용하고, Settings에는 별도의 Gameplay Action Asset과 Map을 지정한다.

마지막 입력 장치 변경은 Runtime에서 구독할 수 있다.

```csharp
runtime.OnLastInputDeviceChanged += HandleInputDeviceChanged;
```

구독한 소유자는 Runtime 종료 전 같은 Callback을 해제한다.

## UGUI 보조 기능

### Drag Visual

`DragVisualController`는 Xeri `Drag_Drop`의 `DraggableUI`와 연결한다.

```csharp
var controller = new DragVisualController(runtime.LayerRegistry);
IDisposable binding = controller.Bind
(
    draggable,
    new DragVisualParams(target, "Drag")
);
```

Drag 중 Target을 지정 Layer로 옮기고 종료 시 부모, sibling과 Transform을 복원한다.
Binding과 활성 Drag를 먼저 닫고 Controller를 해제한 뒤 Profile을 해제한다. Drag 판정과
Drop 계약은 [Drag_Drop README](../Drag_Drop/README.md)를 따른다.

### 기타 기능

| 기능 | 타입 | 결과 수명 |
|---|---|---|
| Safe Area | `UGUILayoutController` | 소유자가 `Dispose` |
| Popup Placement | `PlacementController` | 계산 값 반환 |
| World Projection | `ProjectionController` | 계산 값 반환 |
| Focus Highlight | `FocusHighlightController` | `Lease` |
| Input Block | `UGUIInteractionBlocker` | `Lease` |

이 기능들은 `GameUIRuntime`이 자동 생성하지 않는다. 필요한 View 또는 UI 조립 객체가
생성하고 반환된 Handle을 자신의 수명 안에서 해제한다.

## UI Toolkit 웹 스타일 표현

### Runtime Baseline

`UITKLayerPanel`은 Layer Root에 `GameUIRuntimeBaseline.uss`를 자동 연결한다. Baseline은
Label, Button, BaseField 계열과 ProgressBar의 기본 외부 간격을 정규화한다.

TextField 입력부, Slider Tracker, Popup Arrow와 Scroller처럼 기능에 필요한 내부 구조는
Unity Runtime Theme을 유지한다. 모든 표준 Control을 직접 초기화하는 별도 Reset USS는
필요하지 않다.

### Gradient Material

UI Toolkit USS는 VisualElement 배경에서 CSS의 `linear-gradient()`, `radial-gradient()`,
`conic-gradient()`를 직접 지원하지 않는다. Xeri Material을 필요한 요소에만 지정한다.

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
| `_Color0` ~ `_Color7` | 최대 8개 Gradient 색상 |
| `_ColorCount` | 사용할 색상 수, 2~8 |
| `_Stop0` ~ `_Stop7` | 각 색상의 시작·종료 위치, 0~1 |
| `_Angle` | Linear 방향 또는 Conic 시작 각도 |
| `_Center` | Radial·Conic 중심점 |
| `_Radius` | Radial X·Y 반경 |
| `_Tiling` | Linear·Radial 반복 횟수 |

Material 출력에는 요소의 `background-color`가 곱해진다. Material 색상을 그대로 표시하려면
`background-color: white`를 사용한다.

### 여러 Gradient 겹치기

한 `VisualElement`에는 `-unity-material` 하나만 지정한다. CSS의 다중 배경처럼 Linear,
Radial과 Conic Gradient를 함께 보이게 하려면 Gradient마다 자식 `VisualElement`를 만들고
같은 영역에 겹친다. UXML에서 뒤에 선언된 요소가 앞 요소 위에 그려진다.

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

.gradient-card__content {
    position: relative;
}
```

부모의 `overflow: hidden`과 `border-radius`는 겹친 모든 자식 Gradient를 같은 둥근 영역으로
자른다. Gradient 전용 요소는 `picking-mode="Ignore"`로 두고 Button과 입력 요소는 Content
Layer에 둔다. USS `border-radius`에는 `50%` 같은 백분율을 그대로 사용할 수 있다.

### Gamma Compositor

Gradient Material은 Gamma Compositor 없이도 표시된다. Gamma 합성은 Linear Color
Space에서 USS 색상을 웹의 sRGB 결과에 가깝게 보이도록 만드는 Layer 단위 색상 경로다.

`GameUIUITKLayer.prefab`의 `Use Gamma Compositing`은 기본으로 켜져 있다. Linear Color
Space에서 Layer가 활성화되면 `UITKLayerPanel`이 다음을 자동 관리한다.

```text
원본 UIDocument
    → Layer 전용 Linear UNORM RenderTexture
    → 화면용 합성 UIDocument
    → gamma-to-linear 합성
```

호출자가 별도 RenderTexture나 합성 UIDocument를 만들 필요는 없다. Runtime은 다음 항목도
함께 관리하고 Layer 해제 시 기존 `PanelSettings` 값으로 복원한다.

- RenderTexture Depth/Stencil
- `PanelSettings.clearDepthStencil`
- Target Texture
- `forceGammaRendering`
- Clear Color

따라서 Gamma 합성을 사용하는 Layer에서도 USS에 `border-radius`와 `overflow: hidden`만
선언하면 둥근 Stencil Mask가 정상 적용된다.

Gamma Color Space에서는 합성 경로를 만들지 않고 원본 UIDocument를 직접 표시한다. 활성
Gamma Layer마다 화면 크기의 RenderTexture 하나를 사용하므로, 색상 일치가 필요하지 않은
Layer만 Prefab Variant에서 `Use Gamma Compositing`을 끈다.

### XeriLoopAnimator

`XeriLoopAnimator`는 USS Transition 값을 직접 계산하지 않는다. Transition 완료 시 다음
USS Class를 적용해 선언된 단계를 반복한다.

반복할 `UIDocument`와 같은 GameObject에 Component를 추가한다. 기본 Layer Prefab에는
포함되지 않으며, Animator 하나가 문서 아래의 `xeri-loop` 요소를 관리한다.

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

여러 Property를 동시에 Transition하면 `--xeri-loop-trigger`로 완료 기준을 지정한다.
Material Transition의 이름은 `-unity-material`이다. `--xeri-next`가 없거나 완료 Event가
발생하지 않으면 반복은 멈춘다.

Gamma Compositor와 Loop Animator는 독립 기능이다. 반복이 필요한 문서에만 Animator를
추가한다.

## 종료와 소유권

| 획득 결과 | 소유자가 종료할 항목 | 종료 전 조건 |
|---|---|---|
| `GameUIProfileHandle` | Profile Layer | 해당 Layer 소비자 종료 |
| `ScreenRegistrationHandle` | Screen 등록 | 해당 Source의 열린 Session 종료 |
| `ScreenSession` | View, Layer Usage, Input과 자식 Handle | 정상 Close는 현재 top |
| `OverlayHandle<TView>` | Overlay View와 Layer Usage | 없음 |
| `ModalHandle` | Modal Stack 항목과 전달된 Handle | 없음 |
| `Lease` | Visibility, Highlight 또는 Input Block 요청 | 없음 |
| `DragVisualHandle` | Drag 부모와 Transform | 없음 |

일반적인 종료 순서는 다음과 같다.

1. `runtime.Screens.Clear()`로 열린 Session과 자식 Handle을 닫는다.
2. `ScreenRegistrationHandle`을 해제한다.
3. Source, Binding과 선택 기능 Controller를 해제한다.
4. 추가 `GameUIProfileHandle`을 해제한다.
5. `runtime.Shutdown()`을 호출한다.

일반 `Dispose`, `Release`와 Callback 정리는 attempt-once다. 정리가 시작된 객체를 재시도
Registry나 복구 상태 머신에 넣지 않는다. 일부 정리가 실패하더라도 소유자가 정의한 독립
정리 항목은 끝까지 시도하고 오류를 전달한다.

상태 변경 전 사전 조건이 거부되고 소유권이 유지되는 경우는 구분한다. 활성 Layer 소비자가
남은 `GameUIProfileHandle.Dispose()`가 해당하며, 소비자를 종료한 뒤 같은 Handle로 다시
요청할 수 있다.

Shutdown이 시작된 Runtime은 오류가 발생해도 Terminal 상태로 끝난다. 같은 Runtime을 다시
초기화하지 않는다.

## AI 작업 절차

AI가 Xeri Game UI를 사용하는 코드를 만들 때는 아래 순서로 판단한다.

### 새 UI를 추가하기 전

1. Runtime 참조를 전달할 수명 소유자를 찾는다.
2. UI가 사용할 Layer ID와 Profile을 확인한다.
3. Root가 `RectTransform`인지 `VisualElement`인지 결정한다.
4. View 획득과 반환을 담당할 Source를 정한다.
5. Session, 등록 Handle과 Profile Handle의 보관 위치를 정한다.
6. 종료 순서를 코드로 먼저 확정한다.

### 새 Screen 구현 순서

1. 필요한 Layer를 Profile에 구성한다.
2. `ScreenOptions`을 만든다.
3. UI 기술에 맞는 `IScreenSource`를 구현한다.
4. `Acquire` 실패가 이번 호출에서 만든 View와 Binding을 정리하도록 한다.
5. `Release`가 Binding을 끊고 View를 원래 공급 경로로 반환하도록 한다.
6. Profile 획득 뒤 `ScreenRegistry.Register`를 호출한다.
7. 반환된 등록 Handle을 수명 소유자에 보관한다.
8. 닫기 UI는 `scope.Session.Close()`를 호출한다.
9. Session → 등록 → Source → Profile 순서로 종료한다.

### 기존 확장점

| 필요한 역할 | 사용할 계약 |
|---|---|
| 새 Layer Root | `IPresentationLayerDriver<TRoot>` |
| 새 Screen 표시 backend | `IScreenDriver` |
| Screen View 획득과 반환 | `IScreenSource` |
| Screen 상태 관찰 | `IScreenStateHandler` |
| Focus backend | `IFocusDriver` |
| Input backend | `IScreenInputDriver` |
| Transition backend | `IPresentationTransitioner` |
| Overlay View | `IOverlaySource<TView>` |
| Modal 표시 | `IModalDriver` |
| Visibility 대상 | `IVisibilityTarget` |

실제 호출 경로 없이 전역 Manager, Backend enum, Dispose 재시도 관리자, 복구 Registry 또는
범용 Source를 추가하지 않는다. 먼저 위 공개 계약으로 표현 가능한지 확인한다.

## 문제 확인

| 증상 | 확인 순서 |
|---|---|
| Runtime 초기화 실패 | Host 필수 Component → Settings → Default Profile → Fade Layer |
| UI 입력 없음 | Input Module Action Reference → UI Map → Gameplay Asset와 Map |
| Layer 등록 실패 | Profile 활성 여부 → ID 중복 → Order 중복 → Driver Validate 결과 |
| Screen Open 거부 | Screen 등록 → Layer ID → 중복 정책 → 현재 Stack 명령 상태 |
| Focus 없음 | Options Focus → Driver Focus → Focus Driver fallback |
| UITK Layer 미표시 | UIDocument → PanelSettings → Root Name → Target Texture 미지정 |
| Gradient가 어두움 | `background-color: white` → Color Space → Gamma 옵션 |
| 둥근 `overflow:hidden`에서 자식이 사라짐 | Gamma 옵션 → Runtime RT Depth/Stencil → Console Shader 오류 |
| Gamma Layer 메모리 증가 | 활성 Gamma Layer 수와 화면 해상도 |
| Modal이나 Overlay가 남음 | 반환 Handle 소유자와 Session 자식 등록 여부 |
| Profile Dispose 거부 | 남은 Screen, Overlay, Modal 또는 Drag Layer Usage |
| Loop Animation 정지 | `xeri-loop` → `--xeri-next` → Duration → Trigger Property |

## 소스 탐색표

README의 공개 사용법보다 내부 동작 확인이 필요한 경우에만 아래 경로를 읽는다.

| 관심사 | 경로 |
|---|---|
| Runtime 조립 | `Runtime/GameUIRuntime.cs` |
| Settings | `GameUISettingsAsset.cs` |
| Profile | `GameUIProfileAsset.cs`, `Runtime/Profile` |
| Layer Core | `Core/Presentation/Layer` |
| Screen Core | `Core/Screen` |
| Fade, Modal, Overlay, Visibility | `Core/Presentation` |
| Focus와 Input | `Core/Interaction`, `InputSystem` |
| UGUI Adapter | `UGUI` |
| UI Toolkit Adapter | `UITK` |
| DOTween Adapter | `DOTween` |
| Gamma Compositor | `../XeriUI/Presentation/Gamma` |
| Gradient Material과 Shader | `../XeriUI/Resources/XeriUI` |
| Loop Animator | `../XeriUI/Animation/Loop` |
