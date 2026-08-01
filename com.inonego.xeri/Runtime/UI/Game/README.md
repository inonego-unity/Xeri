# Xeri Game UI

`Xeri Game UI`는 UGUI와 UI Toolkit의 표시 순서, Screen Stack, Focus, Input,
Transition과 표시 객체 수명을 하나의 Runtime에서 관리하는 Unity UI 라이브러리다.

이 문서는 사람과 AI가 Xeri Game UI를 설정하고 확장할 때 사용하는 기준 가이드다.
소비 코드의 기능이나 화면 흐름은 정의하지 않는다. 이 문서에 나온 공개 API와 수명
계약만 사용하고, 세부 구현이 필요할 때 마지막의 소스 탐색표를 따른다.

- Core Namespace: `inonego.Xeri.UI.Game`
- UITK Loop Component Namespace: `inonego.Xeri.UI`
- 지원 UI: UGUI, UI Toolkit, 두 기술의 혼합 구성
- 기준 Unity: `6000.0` 이상

## 먼저 지킬 규칙

1. `GameUIRuntime`은 전역 Singleton이 아니라 명시적으로 생성·초기화되는 Runtime이다.
2. 동시에 활성화되는 Layer의 ID와 Order는 UGUI·UI Toolkit 전체에서 각각 고유해야 한다.
3. View 생성·Binding·반환은 `IScreenSource`가 대칭으로 소유한다.
4. `Open`, `Acquire`, `Register`가 반환한 Session이나 Handle은 수명 소유자가 한 번 해제한다.
5. Screen의 닫기 동작은 View를 직접 숨기지 않고 `ScreenSession.Close()`로 요청한다.
6. 일반 `Dispose`와 `Release`는 attempt-once다. 실패 재시도 관리자나 복구 Registry를 추가하지 않는다.
7. Gamma Compositor는 내부 구현이다. `UITKLayerPanel`의 옵션으로만 사용한다.
8. UI 기술을 선택하는 Backend enum이나 별도 Manager를 추가하지 않는다. 각 Layer와 Driver의 타입으로 결정한다.
9. Screen 상태 훅 안에서 Screen Stack을 재진입 호출하지 않는다.
10. 라이브러리 밖의 정책과 데이터를 Xeri Core 타입에 추가하지 않는다.

## 제공 기능

| 기능 | 공개 진입점 |
|---|---|
| Runtime 초기화와 종료 | `GameUIRuntime.Initialize`, `Shutdown` |
| Layer 묶음 획득 | `GameUIRuntime.AcquireProfile` |
| Layer 등록과 사용 수명 | `PresentationLayerRegistry` |
| Screen 등록 | `ScreenRegistry.Register` |
| Screen Stack | `ScreenController.Open`, `Replace`, `Close`, `Clear` |
| Focus 선택과 복원 | `FocusController` |
| Input Action Map과 Cursor 합성 | `InputSystemScreenInputDriver` |
| 전체 화면 Fade | `SceneFader` |
| Modal Stack | `ModalController` |
| 임시 표시 View | `OverlayHandle<TView>` |
| 중첩 표시 상태 | `VisibilityController` |
| UGUI Drag Visual | `DragVisualController` |
| UI Toolkit Gradient | `LinearGrad`, `RadialGrad`, `ConicGrad` |
| UI Toolkit Gamma 합성 | `UITKLayerPanel`의 `Use Gamma Compositing` |
| UI Toolkit 반복 Transition | `XeriLoopAnimator` |

## 의존성

패키지는 다음 Unity Package를 사용한다.

- Addressables
- Input System
- UGUI
- UI Toolkit

Runtime Assembly는 `DOTween.Modules`를 참조하고 기본 Transition 구현으로
`DOTweenPresentationTransitioner`를 사용한다. 소비 환경에서 DOTween과 Modules
Assembly가 해석되어야 한다.

Xeri Gradient Shader는 Universal Render Pipeline용 Custom UI Shader다.
Gradient 기능은 Built-in Render Pipeline이나 HDRP에서 동일 동작을 보장하지 않는다.

## 제공 에셋

경로는 이 README가 있는 `Runtime/UI/Game`을 기준으로 한다.

| 에셋 | 경로 | 역할 |
|---|---|---|
| Runtime Host | `Assets/Host/GameUIHost.prefab` | Runtime, EventSystem, Input, Focus, Fade Source |
| UGUI Layer | `Assets/Layer/GameUIUGUILayer.prefab` | Screen Space Overlay UGUI Layer |
| UI Toolkit Layer | `Assets/Layer/GameUIUITKLayer.prefab` | UIDocument, PanelSettings, 선택형 Gamma 합성 |
| Fade Layer | `Assets/Layer/GameUIFadeLayer.asset` | 기본 Fade Layer ID와 Order |
| Fade View | `Assets/Fade/GameUIFadeView.prefab` | 기본 UGUI Fade View |
| Default Profile | `Assets/Profile/GameUIDefaultProfile.asset` | 기본 Fade Layer Profile |
| Panel Settings | `Assets/UITK/GameUIPanelSettings.asset` | Screen Space Overlay UI Toolkit 설정 |
| Runtime Theme | `Assets/UITK/GameUIRuntimeTheme.tss` | 기본 Runtime Theme |
| UI Input Actions | `InputSystem/GameUIInputActions.inputactions` | Navigate, Submit, Cancel, Pointer 입력 |
| Gradient Materials | `../XeriUI/Resources/XeriUI/Materials` | Linear, Radial, Conic Material |
| Gradient Shaders | `../XeriUI/Resources/XeriUI/Shaders` | UI Toolkit Custom Shader |

Default Profile은 Fade에 필요한 `SystemFade` Layer만 포함한다. 다른 표시 Layer는
별도의 `GameUIProfileAsset`에 명시적으로 구성한다.

## 빠른 설정

### Settings 생성

`Assets > Create > Xeri > UI > Game > Settings`에서 `GameUISettingsAsset`을 만든다.

| 필드 | 계약 |
|---|---|
| `DefaultProfile` | Runtime 초기화와 함께 획득할 Profile |
| `SceneFadeLayerID` | Default Profile에 존재하는 Fade Layer ID |
| `DefaultFadeColor` | 기본 Fade 색상 |
| `DefaultFadeDuration` | 0 이상의 기본 Fade 시간 |
| `UIActionMap` | UI 전용 Input Action Map 이름 |
| `GameplayActionsAsset` | 소비 코드가 사용하는 Gameplay Input Action Asset |
| `GameplayActionMap` | UI가 활성 상태를 합성할 Gameplay Map 이름 |
| `ReleaseActionNames` | Screen 종료 뒤 입력 해제를 기다릴 UI Action 이름 |

`UIActionMap`과 `GameplayActionMap`은 서로 다른 Map이어야 한다.
패키지 `GameUIInputActions`는 UI 입력만 제공하며 Gameplay Input을 대신하지 않는다.

### Bootstrapper 연결

`Assets > Create > Xeri > Bootstrapper > Game UI Module`에서
`GameUIBootstrapperModuleAsset`을 만든다.

Module에 다음 참조를 지정한다.

- `Host Prefab`: `Assets/Host/GameUIHost.prefab`
- `Settings`: 생성한 `GameUISettingsAsset`

Module을 Xeri `BootstrapperSettings`에 등록하면 초기화 시 Host를 한 번 생성하고
`GameUIRuntime.Initialize(settings)`를 호출한다. Bootstrapper 경로를 사용할 때
Scene에 별도의 `GameUIRuntime`이나 `EventSystem`을 배치하지 않는다.

Bootstrapper를 사용하지 않으면 Host Prefab을 한 번 생성하고 Root의
`GameUIRuntime.Initialize`를 직접 호출할 수 있다. 두 초기화 경로를 함께 사용하지 않는다.

### Runtime 참조

Xeri는 `GameUIRuntime.Instance` 같은 전역 접근점을 제공하지 않는다. Runtime을 만든
수명 소유자가 참조를 보관하고 필요한 객체에 전달한다.

Bootstrapper가 Host를 생성하는 경우 패키지 Host의 Prefab Variant를 만들고, Runtime을
사용할 통합 Component를 Variant에 추가할 수 있다. 패키지 원본 Prefab은 수정하지 않는다.
통합 Component는 같은 Host의 Runtime에 다음 이벤트로 연결한다.

- `OnInitialized`: Profile 획득, Source 생성, Screen 등록
- `OnReleasing`: 자신이 소유한 항목을 역순으로 해제

독립적인 여러 종료 구독자를 정리 파이프라인으로 사용하지 않는다. 하나의 수명 소유자가
자신이 만든 Handle과 Source의 종료 순서를 명시적으로 관리한다.

초기화된 Runtime의 공개 접근점은 다음과 같다.

| 접근점 | 역할 |
|---|---|
| `LayerRegistry` | Layer 등록과 사용 수명 |
| `ScreenRegistry` | Screen Options와 Source 등록 |
| `Screens` | Screen Stack 조작 |
| `Focus` | Focus 선택과 복원 |
| `SceneFader` | Fade Cover와 Reveal |
| `Visibility` | 중첩 Visibility 요청 |
| `Modals` | Modal Stack |
| `Settings` | 현재 Runtime 설정 |

## Layer와 Profile

### PresentationLayerAsset

`Assets > Create > Xeri > UI > Game > Presentation Layer`에서 만든다.

- `ID`: Screen, Overlay와 기타 소비자가 조회하는 stable string ID
- `Order`: UGUI와 UI Toolkit이 함께 사용하는 Screen Space Overlay 순서

동시에 활성화되는 Profile 전체에서 ID와 Order는 각각 중복될 수 없다.

### Layer Prefab

Layer Prefab Root에는 `IPresentationLayerDriver` 구현이 정확히 하나 있어야 한다.

| Driver | Root 타입 | 필수 구성 |
|---|---|---|
| `UGUILayerCanvas` | `RectTransform` | Screen Space Overlay Canvas, 기본 Display, Default Sorting Layer |
| `UITKLayerPanel` | `VisualElement` | UIDocument, PanelSettings, 기본 Display, Target Texture 미지정 |

UGUI와 UI Toolkit Layer는 같은 Registry와 Order 공간에 등록된다. 한 Layer에 두 기술을
섞지 않고 기술마다 별도 Layer Prefab과 ID를 사용한다.

Camera Canvas, World Space Canvas, 다른 Display, 호출자가 지정한 RenderTexture Panel은
이 공통 Screen Space Overlay Order 계약에 포함되지 않는다.

`UITKLayerPanel`은 공유 PanelSettings Asset을 직접 변경하지 않는다. Layer마다 Runtime
복제본을 만들고 `PanelSettings.sortingOrder`와 `UIDocument.sortingOrder`를 적용한다.
`Root Name`이 비어 있으면 `UIDocument.rootVisualElement`가 Layer Root이며, 이름이 있으면
해당 하위 `VisualElement`를 사용한다.

### GameUIProfileAsset

`Assets > Create > Xeri > UI > Game > Profile`에서 만든다. 각 Entry는 다음을 묶는다.

- `PresentationLayerAsset`
- Layer Prefab을 획득·반환하는 `IGameObjectProvider`

Runtime 수명보다 짧은 Profile은 필요한 시점에 획득한다.

```csharp
GameUIProfileHandle profileHandle = runtime.AcquireProfile(profile);
```

Handle 소유자는 Profile의 Layer를 사용하는 Screen, Overlay, Modal과 Drag Visual을
모두 닫은 뒤 Handle을 해제한다.

```csharp
profileHandle.Dispose();
profileHandle = null;
```

활성 Layer 소비자가 남아 있으면 `Dispose`는 상태 변경 전에 거부되고 소유권을 유지한다.
소비자를 해제한 뒤 같은 Handle로 다시 요청할 수 있다. 실제 종료가 시작된 뒤 발생한
반환 실패는 attempt-once 계약에 따라 재시도하지 않는다.

## Screen 구현

Screen 하나는 다음 세 계약으로 구성된다.

| 타입 | 역할 |
|---|---|
| `ScreenOptions` | ID, Layer, 중복, Focus, Input, Transition 정책 |
| `IScreenSource` | View 생성, Binding, 대칭 반환 |
| `ScreenInstance` | `IScreenDriver`와 선택적 `IScreenStateHandler` |

Xeri는 UGUI와 UI Toolkit Driver를 제공하지만 모든 View에 적용되는 범용
`IScreenSource` 구현은 제공하지 않는다. View 생성과 Binding 방식이 정해지는 위치에서
Source를 구현한다.

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

`DefaultFocus`가 유효하면 먼저 사용하고, 다음으로 `IScreenDriver.DefaultFocus`,
마지막으로 Focus Driver의 fallback을 사용한다. 동적으로 생성되는 View의 기본 Focus는
Driver에 지정하는 편이 적합하다.

### IScreenSource 구현

`Acquire`는 하나의 원자적 획득으로 다음을 수행한다.

1. `scope.Layer`의 typed Root 확인
2. View 생성 또는 획득
3. `scope.OpenParams.Payload` 검증
4. Callback과 Binding 연결
5. Driver와 선택적 State Handler를 `ScreenInstance`로 반환

중간 단계가 실패하면 이번 `Acquire`가 만든 View와 Binding을 Source가 즉시 정리하고
예외를 전달한다. 실패한 View를 소유 목록에 남기지 않는다.

`Release`는 다음 순서를 지킨다.

1. Callback과 Binding 해제
2. Source 소유 매핑 제거
3. View를 원래 Provider나 Visual Tree에 반환

`ScreenController`는 Source가 만든 View를 직접 제거하지 않는다.

```csharp
public sealed class ExampleScreenSource : IScreenSource
{
    public ScreenInstance Acquire(ScreenViewScope scope)
    {
        // typed Layer 확인, View 획득, Binding 연결
        // 실패하면 이 호출에서 만든 항목을 여기서 정리한다.
        return new ScreenInstance(driver, stateHandler);
    }

    public void Release(ScreenInstance instance)
    {
        // Binding 해제 후 Acquire 경로로 View를 반환한다.
    }
}
```

위 코드는 책임 순서를 보여주는 골격이다. `driver`와 `stateHandler`의 생성 방식은
선택한 UI 기술과 View 구조에 맞게 구현한다.

### UGUI Source

UGUI Source는 Layer를 다음 타입으로 확인한다.

```csharp
if (!(scope.Layer is IPresentationLayerDriver<RectTransform> layer))
{
    throw new InvalidOperationException("RectTransform Layer가 필요합니다.");
}
```

View Prefab을 `layer.Root` 아래에 획득하고 Root의 `UGUIScreenDriver`를 사용한다.
상태 훅이 필요하면 같은 View의 `UGUIScreenStateHandler` 파생 Component를
`ScreenInstance`에 함께 전달한다.

`UGUIScreenDriver`에는 표시 Root, CanvasGroup과 선택적 기본 Focus를 연결한다.

### UI Toolkit Source

UI Toolkit Source는 Layer를 다음 타입으로 확인한다.

```csharp
if (!(scope.Layer is IPresentationLayerDriver<VisualElement> layer))
{
    throw new InvalidOperationException("VisualElement Layer가 필요합니다.");
}
```

`VisualTreeAsset`을 Clone해 `layer.Root`에 추가하고 Callback과 Binding을 연결한다.

```csharp
var driver = new UITKScreenDriver(screenRoot, defaultFocus);
return new ScreenInstance(driver, stateHandler);
```

`Release`에서는 Callback과 Binding을 먼저 해제한 뒤 Screen Root를 Visual Tree에서 제거한다.

### 등록과 열기

Layer Profile이 활성화된 뒤 Screen을 등록한다.

```csharp
ScreenRegistrationHandle registration =
    runtime.ScreenRegistry.Register(options, source);
```

등록 Handle을 해제하면 이후 Open 조회에서 제거된다. 이미 열린 Session은 Source를 계속
사용하므로 모든 해당 Session을 닫은 뒤 Source와 등록 Handle을 해제한다.

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

`Payload`는 호출자가 소유하는 선택 값이다. Source는 필요한 타입을 확인해 읽되 Payload의
수명을 종료하지 않는다.

| `ScreenOpenKind` | 의미 |
|---|---|
| `Accepted` | Session 생성과 Open 시작이 수락됨 |
| `Rejected` | 등록, 중복 정책 또는 현재 상태에서 거부됨 |
| `Cancelled` | `OnOpening`에서 취소됨 |
| `SourceFailed` | View 획득 또는 Binding 실패 |
| `TransitionFailed` | Open Transition 시작 실패 |

### Stack 명령

```csharp
runtime.Screens.Open("example.first");
runtime.Screens.Replace("example.second");
runtime.Screens.Close();
runtime.Screens.Clear();
```

- `Open`: 현재 top을 Covered로 만들고 새 Session을 추가한다.
- `Replace`: 새 Open이 수락된 뒤 이전 top을 대체한다.
- `Close`: 현재 top을 정상 Transition 경로로 닫는다.
- `Clear`: 모든 생존 Session을 최신 항목부터 Transition 없이 종료한다.

View의 닫기 Callback은 자신이 받은 Session을 사용한다.

```csharp
scope.Session.Close();
```

`ScreenSession.Close()`는 해당 Session이 현재 top일 때만 성공한다.

### 상태 훅

선택적 `IScreenStateHandler`가 다음 동기 훅을 제공한다.

| 훅 | 호출 시점 |
|---|---|
| `OnOpening` | Open Transition 전, 취소 가능 |
| `OnOpened` | Open Transition 완료 후 |
| `OnClosing` | Close Transition 전, 정상 Close에서 취소 가능 |
| `OnClosed` | 자식 Handle과 Close Transition 정리 후 |

```csharp
if (context.CanCancel)
{
    context.Cancel();
}
```

UGUI MonoBehaviour는 `UGUIScreenStateHandler`를 상속할 수 있다. 모든 훅은 동기식이며
훅 내부에서 `Open`, `Replace`, `Close` 또는 `Clear`를 다시 호출하지 않는다.

Screen과 함께 닫혀야 하는 Handle은 Session에 넘긴다.

```csharp
session.RegisterChild(handle);
```

등록된 자식은 Session 종료 시 등록 역순으로 한 번 해제된다.

## 표시 기능

### Overlay

Overlay는 Screen Stack과 독립적으로 Layer를 잠시 점유하는 View다.

```csharp
OverlayHandle<TView> overlay = OverlayHandle<TView>.Acquire
(
    runtime.LayerRegistry,
    layerID,
    source
);
```

`IOverlaySource<TView>`가 View의 획득과 반환을 소유하고, Handle이 View와 Layer Usage를
함께 보관한다. Screen 수명에 종속되면 `RegisterChild`로 넘기고, 독립 수명이면
획득한 소유자가 `Dispose`한다.

UGUI Prefab에는 `GameObjectProviderOverlaySource<TView>`를 사용할 수 있다.
UI Toolkit View는 `IOverlaySource<TView>`에서 Visual Tree 추가와 제거를 대칭 구현한다.

### Modal

`ModalController`는 View를 만들지 않고 Stack과 top 상태만 관리한다.

- UGUI Driver: `UGUIModalDriver`
- UI Toolkit Driver: `UITKModalDriver`

Overlay로 Modal View를 획득했다면 소유 Handle을 Modal에 넘길 수 있다.

```csharp
ModalHandle modal = runtime.Modals.Open(driver, ownedHandle);
```

Modal Handle이 닫히면 다음 Modal을 top으로 복원하고 전달받은 Handle을 역순으로 해제한다.

### Visibility

`VisibilityController.Set`은 같은 Target에 대한 중첩 요청을 Handle로 관리한다.

```csharp
IDisposable visibility = runtime.Visibility.Set(target, visible: false);
```

가장 최근 요청이 실제 상태를 결정한다. 마지막 요청을 해제하면 최초 상태로 복원한다.

### Scene Fade

Runtime의 `SceneFader`는 Settings에 지정된 Fade Layer와 Source를 사용한다.

```csharp
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

`Cover` 완료 후 Overlay를 유지하고 `Reveal` 완료 후 반환한다. 새 요청은 이전 Transition을
취소한다. 완료와 실패는 Callback으로 전달되며 실패는 `LastFailure`에도 보존된다.

### Focus와 Input

Screen 활성 상태에 따라 Runtime이 다음을 합성한다.

- Options 또는 Driver의 기본 Focus
- 마지막 Focus 복원과 fallback
- Gameplay Action별 활성 상태
- UI Action Map
- Cursor 표시와 Lock Mode
- 입력 우선순위

Screen Source는 Focus나 Input Map을 별도 전역 상태로 중복 관리하지 않는다.

### UGUI Drag Visual

`DragVisualController`는 Xeri `Drag_Drop`의 `DraggableUI`와 연결할 수 있다.

```csharp
var controller = new DragVisualController(runtime.LayerRegistry);
IDisposable binding = controller.Bind
(
    draggable,
    new DragVisualParams(target, layerID)
);
```

Drag 중 Target을 지정 Layer로 옮기고 종료 시 부모, sibling과 Transform을 복원한다.
Binding과 활성 Drag를 먼저 닫고 Controller를 해제한 뒤 해당 Profile을 해제한다.
드래그 판정과 Drop 계약은 [Drag_Drop README](../Drag_Drop/README.md)를 따른다.

### UGUI 보조 기능

| 기능 | 타입 | 반환 수명 |
|---|---|---|
| Safe Area | `UGUILayoutController` | Controller 소유자가 `Dispose` |
| Placement | `PlacementController` | 값 반환 |
| World Projection | `ProjectionController` | 값 반환 |
| Focus Highlight | `FocusHighlightController` | `Lease` |
| Input Block | `UGUIInteractionBlocker` | `Lease` |

이 기능들은 `GameUIRuntime`이 자동 생성하지 않는다. 필요한 위치에서 생성하고 반환된
Handle은 해당 수명 소유자가 해제한다.

## UI Toolkit 웹 스타일 표현

UI Toolkit의 USS는 웹 CSS와 유사하지만 VisualElement 배경에서
`linear-gradient()`, `radial-gradient()`, `conic-gradient()`를 직접 지원하지 않는다.
Xeri는 UXML·USS 구조를 유지하면서 Gradient Material로 해당 표현을 보완한다.

| 웹 CSS | Xeri Material | 주요 Property |
|---|---|---|
| `linear-gradient()` | `LinearGrad` | Color, Stop, Angle, Tiling |
| `radial-gradient()` | `RadialGrad` | Color, Stop, Center, Radius, Tiling |
| `conic-gradient()` | `ConicGrad` | Color, Stop, Center, Angle |

Xeri는 CSS 문법 전체를 구현하지 않는다. 일반 레이아웃과 스타일은 기본 UXML·USS로
작성하고 Gradient가 필요한 요소에만 Material을 지정한다.

웹 CSS:

```css
.example-gradient {
    background: linear-gradient(90deg, #7c3aed 0%, #06b6d4 100%);
}
```

Xeri USS:

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

| Property | 계약 |
|---|---|
| `_Color0` ~ `_Color7` | 최대 8개 Gradient 색상 |
| `_ColorCount` | 사용할 색상 수, 2~8 |
| `_Stop0` ~ `_Stop7` | 각 색상의 시작·종료 위치, 0~1 |
| `_Angle` | Linear 방향 또는 Conic 시작 각도 |
| `_Center` | Radial·Conic 중심점 |
| `_Radius` | Radial X·Y 반경 |
| `_Tiling` | Linear·Radial 반복 횟수 |

Shader는 요소의 `background-color`와 Material 색상을 곱한다. Material 색상을 그대로
표시하려면 `background-color: white`를 지정한다. `border-radius`와 `overflow: hidden`은
기본 USS로 표시 영역을 자를 때 사용한다.

### Gamma 합성

Gradient Material은 Gamma 합성 없이도 표시된다. Gamma 합성은 Linear Color Space에서
USS 색상을 웹과 가까운 sRGB 결과로 표시하기 위한 Layer 단위 색상 경로다.

`GameUIUITKLayer.prefab`의 `Use Gamma Compositing`은 기본 활성 상태다.
Linear Color Space에서 Layer가 활성화되면 `UITKLayerPanel`이 다음 자원을 자동 관리한다.

```text
UIDocument
    -> Layer 전용 UNORM RenderTexture
    -> 화면용 UI Toolkit 합성 Panel
    -> gamma-to-linear 합성
```

호출자는 RenderTexture, 합성 UIDocument, Runtime PanelSettings를 직접 만들거나 해제하지
않는다. Gamma Color Space에서는 합성 경로를 만들지 않고 원본 UIDocument를 직접 표시한다.

활성 Gamma Layer마다 화면 크기의 RenderTexture 하나를 사용한다. 색상 일치보다 메모리와
대역폭을 우선하는 Layer만 Prefab Variant에서 `Use Gamma Compositing`을 끈다.

### XeriLoopAnimator

`XeriLoopAnimator`는 USS Transition의 프레임 값을 직접 계산하지 않는다. Transition
완료 시 다음 USS Class를 적용해 선언된 단계를 반복하는 `UIDocument` 단위 Component다.

반복이 필요한 `UIDocument`와 같은 GameObject에 Component를 추가한다. 기본
`GameUIUITKLayer.prefab`에는 포함되지 않는다. Animator 하나가 해당 문서 아래에서
`xeri-loop` Class를 가진 모든 요소를 관리한다.

```xml
<ui:VisualElement class="example-pulse xeri-loop" />
```

```css
.example-pulse {
    opacity: 0.5;
    transition: opacity 0.8s ease-in-out;
    --xeri-next: "example-pulse-on";
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

Animator는 현재 단계 Transition이 끝나면 단계 Class를 Transition 없이 제거해 기본
Class로 돌아가고 다음 프레임에 다음 Class를 적용한다. 단계 간 직접 보간이 아니라
항상 기본 Class에서 다음 단계로 전환한다.

여러 Property를 동시에 Transition할 때 완료 기준을 지정한다.

```css
--xeri-loop-trigger: "opacity";
```

Material Transition은 다음 이름을 사용한다.

```css
--xeri-loop-trigger: "-unity-material";
```

Trigger가 없으면 먼저 도착한 `TransitionEndEvent`가 다음 단계를 시작한다.
`--xeri-next`가 없거나 Transition 완료 이벤트가 발생하지 않으면 진행을 멈춘다.
런타임에 추가된 요소도 탐색하며, 요소 분리 또는 Component 비활성화 시 Callback,
예약 단계와 현재 단계 Class를 정리한다.

Gamma 합성과 Loop Animator는 서로 독립적이다. 반복 Animation이 필요한 문서에만
Animator를 추가한다.

## 소유권과 종료

| 획득 결과 | 소유자 책임 | 종료 전 조건 |
|---|---|---|
| `GameUIProfileHandle` | Profile Layer 반환 | 해당 Layer 소비자 종료 |
| `ScreenRegistrationHandle` | Screen 등록 제거 | 해당 Source의 열린 Session 종료 |
| `ScreenSession` | Screen View와 자식 Handle 종료 | top Session이면 `Close` 가능 |
| `OverlayHandle<TView>` | View와 Layer Usage 반환 | 없음 |
| `ModalHandle` | Modal Stack과 전달받은 Handle 반환 | 없음 |
| `Lease` | 중첩 요청 하나 반환 | 없음 |
| `DragVisualHandle` | Drag 부모와 Transform 복원 | 없음 |

일반적인 종료 순서는 다음과 같다.

1. `ScreenController.Clear`로 열린 Session과 자식 Handle 종료
2. Screen 등록 Handle 해제
3. Source와 선택 기능의 Handle 및 Controller 해제
4. 추가 Profile Handle 해제
5. `GameUIRuntime.Shutdown`

일반 `Dispose`, `Release`와 Callback 정리는 attempt-once다.

- 정리가 시작되면 논리 소유권을 Terminal 상태로 전환한다.
- 일부 정리가 실패해도 독립적인 나머지 정리를 계속 시도한다.
- 최초 오류와 정리 오류를 숨기지 않는다.
- 실패한 동일 Handle이나 View를 재시도 대상으로 보관하지 않는다.

예외는 상태 변경 전 사전 조건이 거부되고 기존 소유권이 유지되는 경우다.
활성 Layer 소비자가 있는 `GameUIProfileHandle.Dispose()`가 이 경우에 해당한다.

Runtime은 Shutdown 오류가 발생해도 Terminal 상태로 끝난다. 같은 Runtime을 다시
초기화하거나 일반 정리를 재시도하지 않는다.

## AI 구현 가이드

### 작업 전 확인

AI는 코드를 추가하기 전에 다음을 확인한다.

1. 사용할 Runtime 참조를 어느 수명 소유자가 전달하는가
2. 필요한 Layer ID가 활성 Profile에 존재하는가
3. View Root가 `RectTransform`인지 `VisualElement`인지
4. View를 누가 생성하고 누가 대칭 반환하는가
5. 등록 Handle, Session과 자식 Handle을 누가 보관하는가
6. Profile을 해제하기 전에 어떤 Layer 소비자를 먼저 닫아야 하는가

### 새 Screen 추가 순서

1. 기존 `PresentationLayerAsset`을 사용하거나 새 Layer와 Profile Entry를 만든다.
2. `ScreenOptions`을 정의한다.
3. 선택한 UI 기술에 맞는 `IScreenSource`를 구현한다.
4. `Acquire` 실패가 이번 호출의 View와 Binding을 정리하는지 확인한다.
5. `Release`가 Binding을 먼저 끊고 View를 원래 공급 경로로 반환하는지 확인한다.
6. Layer Profile을 획득한 뒤 `ScreenRegistry.Register`를 호출한다.
7. 반환된 등록 Handle을 명확한 수명 소유자에 보관한다.
8. View의 닫기 Callback은 `ScreenSession.Close`를 호출한다.
9. 종료 시 Session, 등록, Source, Profile 순서를 지킨다.

### 기존 확장점

| 필요한 역할 | 사용할 계약 |
|---|---|
| 새 Layer 기술 | `IPresentationLayerDriver<TRoot>` |
| 새 Screen 표시 방식 | `IScreenDriver` |
| View 생성과 반환 | `IScreenSource` |
| Focus Backend | `IFocusDriver` |
| Input Backend | `IScreenInputDriver` |
| Transition Backend | `IPresentationTransitioner` |
| Overlay View | `IOverlaySource<TView>` |
| Modal 표시 | `IModalDriver` |
| Visibility 대상 | `IVisibilityTarget` |

실제 호출자와 수명 계약이 없는 새로운 추상화는 추가하지 않는다.

### 추가하지 않을 구조

- 전역 `GameUIManager` 또는 Runtime Singleton
- UGUI·UI Toolkit 선택용 Backend enum
- 모든 View에 적용하려는 범용 Source 구현
- 일반 Dispose 재시도 관리자
- Generation, 복구 Registry 또는 복구 상태 머신
- 내부 Gamma Compositor의 공개 API
- Screen 상태 훅 내부의 Stack 재진입 우회
- 독립 구독자 전체 실행을 보장하려는 범용 Event 예외 격리 도구

## 검증 체크리스트

### Runtime

- Runtime Host와 EventSystem이 각각 하나인가
- Bootstrapper Module이 Host와 Settings를 참조하는가
- Settings의 Default Profile, Fade Layer와 Input 설정이 유효한가
- Bootstrapper 초기화와 수동 초기화를 함께 사용하지 않는가

### Layer와 Profile

- 동시에 활성화되는 Layer ID와 Order가 중복되지 않는가
- Layer Prefab Root에 Driver가 정확히 하나 있는가
- UGUI Canvas와 UITK Panel이 공통 Screen Space Overlay 조건을 만족하는가
- Profile 해제 전에 모든 Layer 소비자를 닫았는가

### Screen

- Source의 Acquire와 Release가 대칭인가
- Acquire 실패가 이번 호출에서 만든 상태를 모두 정리하는가
- View 닫기가 Session을 통하는가
- 상태 훅에서 Stack을 재진입하지 않는가
- Source보다 해당 Source의 Session을 먼저 닫는가

### UI Toolkit

- PanelSettings가 기본 Display를 사용하고 Target Texture가 비어 있는가
- Gradient Material 경로가 `XeriUI/Materials/{LinearGrad|RadialGrad|ConicGrad}`인가
- Material 색상을 그대로 표시할 요소가 흰색 `background-color`를 사용하는가
- Gamma Layer의 RenderTexture 비용을 확인했는가
- Loop Animator의 모든 단계가 의도한 다음 Class를 가리키는가
- 여러 Transition Property에 `--xeri-loop-trigger`를 지정했는가

### 종료

- Session 자식 Handle을 `RegisterChild`로 넘겼는가
- Handle과 Controller를 획득 역순으로 해제하는가
- 일반 Dispose 실패에 재시도 구조를 추가하지 않았는가
- Runtime 종료 후 공개 접근점을 다시 호출하지 않는가

## 문제 확인

| 증상 | 먼저 확인할 항목 |
|---|---|
| Runtime 초기화 실패 | Host Root의 `GameUIRuntime`, Settings 필수 참조 |
| Screen Open 거부 | Screen 등록, Layer 활성 상태, 중복 정책 |
| Layer 등록 실패 | ID·Order 중복, Driver `Validate` 결과 |
| UI 입력 없음 | UI Action Reference, Action Map과 Input Module Binding |
| Focus 없음 | Options Focus, Driver Focus, Focus Driver fallback |
| UITK Layer 미표시 | UIDocument, PanelSettings, Root Name |
| Gamma Layer 비용 증가 | 활성 Gamma Layer 수와 화면 해상도 |
| Profile Dispose 거부 | 남아 있는 Screen, Overlay, Modal, Drag Layer Usage |
| Loop Animation 정지 | `--xeri-next`, Transition Duration, Trigger Property |

## 소스 탐색표

| 관심사 | 기준 경로 |
|---|---|
| Runtime 조립 | `Runtime/GameUIRuntime.cs` |
| Settings | `GameUISettingsAsset.cs` |
| Profile | `GameUIProfileAsset.cs`, `Runtime/Profile` |
| Layer Core | `Core/Presentation/Layer` |
| Screen Core | `Core/Screen` |
| Fade, Modal, Overlay, Visibility | `Core/Presentation` |
| Input과 Focus | `Core/Interaction`, `InputSystem` |
| UGUI Adapter | `UGUI` |
| UI Toolkit Adapter | `UITK` |
| DOTween Adapter | `DOTween` |
| Gamma 내부 구현 | `../XeriUI/Presentation/Gamma` |
| Gradient Material과 Shader | `../XeriUI/Resources/XeriUI` |
| Loop Animator | `../XeriUI/Animation/Loop` |
