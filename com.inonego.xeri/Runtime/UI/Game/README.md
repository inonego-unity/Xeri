# Xeri Game UI

`Xeri Game UI`는 게임 화면을 구성하는 공통 수명과 표시 정책을 제공한다.
한 App 수명의 `GameUIRuntime`이 Screen Stack, Presentation Layer, Focus,
Input, Modal, Overlay, Visibility와 Scene Fade를 조립한다.

UGUI와 UI Toolkit은 별도 Runtime으로 나뉘지 않는다.
각 Layer와 View Source가 사용할 UI 기술을 선택하며, 같은 Runtime과
Screen Stack 안에서 두 기술을 함께 사용할 수 있다.

## 책임 범위

UI Core가 소유하는 책임:

- Screen 등록, Open, Replace, Close, Clear와 Stack 상태
- Screen View와 Presenter의 획득·반환 수명
- UGUI·UI Toolkit 공통 Presentation Layer ID와 Order
- 기본·마지막·대체 Focus 선택
- UI·Gameplay Input Map과 Cursor 정책 합성
- Modal Stack, Overlay와 Visibility Handle
- Scene Fade 표시 수명과 Transition
- App·Scene·게임 모드별 Layer Profile 수명

UI Core가 소유하지 않는 책임:

- 타이틀, 로비, 로딩, 인게임 같은 App Flow 상태 전환
- 새 게임, 저장, 불러오기와 복구 정책
- 화면별 게임 데이터와 도메인 명령
- UXML, Prefab, Sprite, Material과 화면별 시각 디자인
- Camera·World Space Canvas와 Render Texture UI의 정렬

App Flow는 어떤 Screen을 언제 열고 닫을지 결정한다.
UI Core는 요청받은 화면의 표시와 대칭 정리만 소유한다.

## 의존성

패키지는 다음 Unity Package를 직접 사용한다.

- Addressables
- Input System
- UGUI

Runtime Assembly는 `DOTween.Modules`를 참조하며,
Game UI Transition의 기본 구현은 `DOTweenPresentationTransitioner`다.
따라서 소비 프로젝트에는 DOTween과 Modules Assembly가 준비되어 있어야 한다.

## Runtime 구성

### Host Prefab

App 전체에서 유지할 Host Prefab Root에 다음 Component를 정확히 한 벌 둔다.

- `GameUIRuntime`
- `EventSystem`
- `InputSystemUIInputModule`
- `InputSystemScreenInputDriver`
- `GameUIFocusDriver`
- `UGUIFocusDriver`
- `UITKFocusDriver`
- `UGUISceneFadeSource` 또는 `UITKSceneFadeSource` 중 하나

`GameUIRuntime`에는 다음 참조를 연결한다.

- Layer Prefab 인스턴스의 부모가 될 `layerRoot`
- `GameUIFocusDriver`
- 선택한 Scene Fade Source
- Host의 `EventSystem`
- Host의 `InputSystemUIInputModule`
- Host의 `InputSystemScreenInputDriver`

`GameUIFocusDriver`에는 UGUI와 UI Toolkit Focus Driver를 모두 연결한다.
UGUI Focus Driver는 Host의 같은 `EventSystem`을 사용해야 한다.
현재 Runtime은 실제 화면 기술과 관계없이 두 Focus Driver 구성을 모두 검증한다.

`InputSystemUIInputModule`에는 사용하는 Input Action Asset과
Point, Move, Submit, Cancel, Click 계열 Action Reference를 연결한다.
Runtime은 Input Module의 활성 여부를 검증하지만 개별 Action Reference를
자동으로 채우지 않는다.

로드된 Scene 전체에는 이 Host의 `GameUIRuntime`과 `EventSystem`만 존재해야 한다.
일반 Scene에 별도 Runtime이나 EventSystem을 추가하지 않는다.

### Settings

`Assets > Create > Xeri > UI > Game > Settings`에서
`GameUISettingsAsset`을 생성하고 다음 값을 설정한다.

- `DefaultProfile`: App 수명 Layer Profile
- `SceneFadeLayerID`: 기본 Profile 안의 Fade Layer ID
- `DefaultFadeColor`, `DefaultFadeDuration`
- `UIActionMap`, `GameplayActionMap`
- `ReleaseActionNames`: 화면 종료 뒤 입력 해제를 기다릴 Action 이름

UI와 Gameplay Action Map 이름은 서로 달라야 한다.
Release Action은 UI와 Gameplay Map에서 같은 이름을 사용할 수 있으며,
현재 눌린 입력이 해제된 뒤 이전 입력·Cursor 정책이 복원된다.

### Bootstrapper

Xeri Bootstrapper를 사용하면
`Assets > Create > Xeri > Bootstrapper > Game UI Module`에서
`GameUIBootstrapperModuleAsset`을 생성한다.

Module에는 준비된 Host Prefab과 Settings를 연결한다.
Bootstrapper가 Host를 생성하고 다음 초기화를 한 번 수행한다.

```csharp
runtime.Initialize(settings);
```

Bootstrapper를 사용하지 않는 프로젝트도 동일하게 Host를 한 번 생성하고
`Initialize`를 명시적으로 호출할 수 있다. 두 초기화 경로를 함께 사용하지 않는다.

`GameUIRuntime`은 전역 Singleton이 아니라 App 수명의 Composition Root다.
프로젝트 코드는 Host에 하나의 Composition Component를 두고 Runtime 참조를
보관하거나 필요한 객체에 주입한다.

## Layer와 Profile

### Presentation Layer Asset

`Assets > Create > Xeri > UI > Game > Presentation Layer`에서
Layer마다 `PresentationLayerAsset`을 만든다.

- `ID`: 활성 Profile 전체에서 유일한 stable string ID
- `Order`: UGUI와 UI Toolkit이 공유하는 Screen Overlay 정렬 순서

Order는 두 UI 기술이 동일하게 표현할 수 있는 `short` 범위만 허용한다.
값이 클수록 다른 공통 Layer보다 앞에 표시된다.

### UGUI Layer Prefab

UGUI Layer Prefab Root에는 `UGUILayerCanvas`를 하나 둔다.

- `Root`: View를 배치할 `RectTransform`
- `Canvas`: 이 Layer의 독립 Canvas
- Canvas Render Mode: `Screen Space - Overlay`
- Target Display: 기본 Display
- Sorting Layer: Default

Root는 Canvas 자신이거나 Canvas의 하위 Transform이어야 한다.
Runtime이 Canvas의 `overrideSorting`과 `sortingOrder`를 Layer Order에 맞춘다.

### UI Toolkit Layer Prefab

UI Toolkit Layer Prefab Root에는 `UITKLayerPanel`을 하나 둔다.

- 활성 `UIDocument`
- `PanelSettings`
- UXML 안의 Layer Root 이름

PanelSettings는 기본 Display를 사용하고 Target Texture를 가지면 안 된다.
Runtime은 Layer마다 PanelSettings 복제본을 만들어 원본 Asset을 변경하지 않고
`PanelSettings.sortingOrder`와 `UIDocument.sortingOrder`에 공통 Order를 적용한다.

### 혼합 Layer

한 Profile에 UGUI와 UI Toolkit Layer를 함께 넣을 수 있다.
두 기술은 같은 `PresentationLayerRegistry`와 Order 공간을 사용한다.

한 Layer Prefab Root에는 `IPresentationLayerDriver` 구현이 정확히 하나 있어야 한다.
UGUI와 UI Toolkit을 같은 표시 구간에 사용하려면 서로 다른 Layer Prefab과
Layer ID로 구성한다.

Camera·World Space Canvas, 다른 Display와 Render Texture Panel은
공통 Screen Overlay 정렬 공간에 포함되지 않는다.
이 UI들은 Game UI Layer Registry 밖에서 해당 렌더링 경로가 직접 관리한다.

### Profile

`Assets > Create > Xeri > UI > Game > Profile`에서
`GameUIProfileAsset`을 만든다.
각 Entry는 다음 두 항목을 묶는다.

- `PresentationLayerAsset`
- 해당 Layer Prefab을 반환하는 `IGameObjectProvider`

기본 Profile에는 App 전체에서 유지할 Layer와 Scene Fade Layer를 둔다.
Scene이나 게임 모드마다 구성이 달라지면 별도 Profile을 획득한다.

```csharp
GameUIProfileHandle sceneProfileHandle =
    runtime.AcquireProfile(sceneProfile);
```

추가 Profile은 획득한 호출자가 Handle을 소유한다.
종료할 때는 해당 Layer를 사용하는 Screen, Overlay, Modal과 Drag Visual을 먼저
닫은 뒤 Profile Handle을 해제한다.

```csharp
sceneProfileHandle.Dispose();
sceneProfileHandle = null;
```

활성 Layer 소비자가 남아 있으면 Profile 종료는 상태 변경 전에 거부된다.
이 경우 소비자를 정상 종료한 뒤 같은 Handle로 다시 요청할 수 있다.
검증을 통과해 실제 종료가 시작된 뒤의 반환 실패는 재시도하지 않는다.

## 프로젝트 Composition

Host에는 프로젝트 UI 구성을 소유하는 Component를 하나 두는 것을 권장한다.
이 Component가 `GameUIRuntime.OnInitialized`에서 Screen Source와 프로젝트
Controller를 만들고, `OnReleasing`에서 자신이 만든 항목을 역순으로 정리한다.

`OnReleasing`은 표준 multicast event다.
여러 독립 정리 파이프라인을 구독시키기보다 하나의 Composition이 프로젝트
소유 항목을 모아 명시적인 순서로 해제한다.

Runtime 종료 시 Screen은 `OnReleasing`보다 먼저 모두 닫힌다.
Composition은 일반적으로 다음 순서로 나머지 소유권을 정리한다.

1. Screen 등록 Handle
2. Screen·Overlay Source와 UI 기능 연결
3. 프로젝트 소유 Controller
4. 추가 Profile Handle

App 종료는 다음 공개 경로를 사용한다.

```csharp
runtime.Shutdown();
```

Host 비활성화, 파괴와 Application 종료 callback은 누락된 명시적 종료를
보완하지만 정상 App Flow는 `Shutdown`을 호출한다.

## Screen 만들기

Screen 하나는 다음 세 요소로 구성한다.

- `ScreenOptions`: ID, Layer, 중복, Focus, Input, Transition 정책
- `IScreenSource`: View와 Presenter를 획득하고 대칭으로 반환하는 소유자
- `ScreenInstance`: `IScreenDriver`와 선택적 `IScreenStateHandler`의 실행 묶음

Xeri는 UGUI와 UI Toolkit의 표시 Driver를 제공하지만,
화면별 데이터 Binding이 다르므로 범용 `IScreenSource` 구현은 제공하지 않는다.
프로젝트 Source가 View 생성과 Presenter Binding을 소유한다.

### ScreenOptions

```csharp
var options = new ScreenOptions
(
    id: "MainMenu",
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

`DefaultFocus`는 `ScreenOptions`의 값이 먼저 사용되고,
없으면 Source가 만든 `IScreenDriver.DefaultFocus`가 사용된다.
동적으로 만들어지는 View의 Focus 대상은 Driver에 설정하는 편이 자연스럽다.

### IScreenSource 계약

`Acquire(ScreenViewScope scope)`는 다음 작업을 하나의 획득으로 처리한다.

1. `scope.Layer`가 필요한 typed Layer인지 확인
2. Prefab 인스턴스 생성 또는 UXML Clone
3. `scope.OpenParams.Payload` 검증
4. 버튼 callback과 Presenter Binding
5. UI 기술에 맞는 `IScreenDriver` 생성 또는 조회
6. 선택적 `IScreenStateHandler`와 함께 `ScreenInstance` 반환

중간에 실패하면 Source가 이번 호출에서 만든 View와 Binding을 즉시 정리하고
예외를 전달한다. 실패한 View를 내부 소유 목록에 남기지 않는다.

`Release(ScreenInstance instance)`는 다음 순서를 따른다.

1. Presenter와 callback Unbind
2. Source 소유 매핑 제거
3. Prefab을 Provider에 반환하거나 Visual Tree에서 제거

반환 실패가 발생해도 동일 View 반환을 재시도하지 않는다.
Screen Controller는 Source가 만든 View를 직접 파괴하지 않는다.

### UGUI Source

UGUI Source는 Layer를 다음 타입으로 확인한다.

```csharp
if (!(scope.Layer is IPresentationLayerDriver<RectTransform> layer))
{
    throw new InvalidOperationException("UGUI Screen에는 RectTransform Layer가 필요합니다.");
}
```

Provider의 Parent를 `layer.Root`로 잠시 설정해 Prefab을 획득하고,
Prefab Root의 `UGUIScreenDriver`를 사용한다.
화면 훅이 필요하면 같은 Prefab의 `UGUIScreenStateHandler` 파생 Component를
`ScreenInstance`에 함께 전달한다.

```csharp
var screen = new ScreenInstance(driver, stateHandler);
```

`UGUIScreenDriver`에는 다음 참조를 연결한다.

- 표시할 Root GameObject
- 표시·상호작용을 적용할 CanvasGroup
- 선택적 기본 Focus GameObject

### UI Toolkit Source

UI Toolkit Source는 Layer를 다음 타입으로 확인한다.

```csharp
if (!(scope.Layer is IPresentationLayerDriver<VisualElement> layer))
{
    throw new InvalidOperationException("UI Toolkit Screen에는 VisualElement Layer가 필요합니다.");
}
```

Source가 UXML을 Clone해 `layer.Root`에 추가하고 Presenter callback을 연결한다.
추가된 Screen Root와 선택적 기본 Focus로 Driver를 만든다.

```csharp
var driver = new UITKScreenDriver(screenRoot, defaultFocus);
var screen = new ScreenInstance(driver, stateHandler);
```

`Release`에서는 callback과 Binding을 먼저 해제한 뒤
Screen Root를 Visual Tree에서 제거한다.

### 등록

Layer Profile이 활성화된 뒤 Screen을 등록한다.

```csharp
ScreenRegistrationHandle registration =
    runtime.ScreenRegistry.Register(options, source);
```

등록 Handle을 해제하면 이후 새 Open 조회에서만 제거된다.
이미 열린 Screen Session은 계속 Source를 사용하므로,
Source를 해제하기 전에 해당 Session을 먼저 닫아야 한다.

### Open과 Payload

```csharp
var response = runtime.Screens.Open
(
    "MainMenu",
    new ScreenOpenParams(payload)
);

if (!response.Accepted)
{
    Debug.LogError($"{response.Kind}: {response.Error}");
}
```

`Payload`는 호출자가 소유하는 선택적 객체다.
Source나 Presenter는 기대 타입을 확인해 읽되 수명을 임의로 종료하지 않는다.

Open 결과:

| Kind | 의미 |
|---|---|
| `Accepted` | Open이 수락되고 `Session`이 생성됨 |
| `Rejected` | 등록, 중복 정책 또는 현재 상태에서 거부됨 |
| `Cancelled` | `OnOpening`에서 취소됨 |
| `SourceFailed` | View 획득 또는 Binding 실패 |
| `TransitionFailed` | Open Transition 시작 실패 |

같은 ID를 동시에 열 수 있는지는 `ScreenDuplicatePolicy`로 결정한다.

### Open, Replace, Close와 Clear

```csharp
runtime.Screens.Open("Inventory");
runtime.Screens.Replace("Pause");
runtime.Screens.Close();
runtime.Screens.Clear();
```

- `Open`: 현재 top을 Covered로 만들고 새 Screen을 Stack에 추가
- `Replace`: 새 Screen이 수락된 뒤 이전 top을 대체
- `Close`: 현재 top 하나를 취소 가능한 정상 경로로 종료
- `Clear`: 모든 생존 Screen을 최신 항목부터 Transition 없이 강제 종료

화면의 닫기·취소 버튼은 View를 직접 숨기거나 제거하지 않는다.
Source가 `scope.Session`을 Presenter에 전달하고 버튼 callback에서 다음을 호출한다.

```csharp
session.Close();
```

`ScreenSession.Close()`는 해당 Session이 현재 top일 때만 성공한다.

### Screen 상태 훅

화면은 `IScreenStateHandler`를 선택적으로 구현한다.
UGUI MonoBehaviour는 `UGUIScreenStateHandler`를 상속할 수 있다.

```csharp
public sealed class InventoryScreen : UGUIScreenStateHandler
{
    public override void OnOpening(ScreenStateContext context)
    {
    }

    public override void OnOpened(ScreenStateContext context)
    {
    }

    public override void OnClosing(ScreenStateContext context)
    {
    }

    public override void OnClosed(ScreenStateContext context)
    {
    }
}
```

훅은 모두 동기식이다.

- `OnOpening`: Open Transition 전, 취소 가능
- `OnOpened`: Open Transition 완료 후
- `OnClosing`: Close Transition 전, 정상 Close에서는 취소 가능
- `OnClosed`: 자식 표시와 Close Transition 정리 후

취소가 허용되는 훅에서는 다음을 사용할 수 있다.

```csharp
if (context.CanCancel)
{
    context.Cancel();
}
```

상태 훅 안에서 다시 Open, Close, Replace 또는 Clear를 호출하지 않는다.
후속 흐름 전환은 훅이 끝난 뒤 App Flow가 수행한다.

### Screen 소유 하위 표시

Screen과 함께 닫혀야 하는 Overlay, Modal, Drag Binding 등의 Handle은
Session에 등록한다.

```csharp
session.RegisterChild(handle);
```

자식 Handle은 Screen 종료 시 등록 역순으로 각각 한 번 해제된다.

## Overlay

Overlay는 Screen Stack 밖에서 Layer를 잠시 사용하는 표시 요소다.

```csharp
OverlayHandle<TView> overlay = OverlayHandle<TView>.Acquire
(
    runtime.LayerRegistry,
    "Overlay",
    source
);
```

반환된 Handle이 View와 Layer Usage를 함께 소유한다.
Screen에 종속되면 `session.RegisterChild(overlay)`로 넘기고,
독립 수명이면 소유자가 직접 `Dispose`한다.

UGUI Prefab은 `GameObjectProviderOverlaySource<TView>`로 연결할 수 있다.
이 Source는 `RectTransform` Layer에 Provider Prefab을 배치한다.
UI Toolkit Overlay는 프로젝트 Source가 Visual Tree 생성과 제거를 구현한다.

Source를 해제하기 전에 모든 Overlay Handle을 먼저 닫는다.

## Modal

`ModalController`는 View를 생성하지 않고 Modal Stack과 top 상호작용만 소유한다.

- UGUI: `UGUIModalDriver`
- UI Toolkit: `UITKModalDriver`

일반적으로 Overlay로 View를 획득한 뒤 Modal Stack에 넘긴다.

```csharp
OverlayHandle<UGUIModalDriver> overlay =
    OverlayHandle<UGUIModalDriver>.Acquire
    (
        runtime.LayerRegistry,
        "Modal",
        modalSource
    );

ModalHandle modal = null;

try
{
    modal = runtime.Modals.Open(overlay.View, overlay);
    overlay = null;
    session.RegisterChild(modal);
    modal = null;
}
finally
{
    modal?.Dispose();
    overlay?.Dispose();
}
```

`Open` 성공 시 전달한 `ownedHandles`의 소유권이 Modal Handle로 이동한다.
Open이 실패하면 호출자가 아직 소유하므로 직접 해제한다.
Session 등록이 실패하면 호출자가 Modal Handle을 직접 해제한다.
Modal Handle을 닫으면 현재 항목을 Stack에서 제거하고 이전 top을 복원한다.

## Scene Fade

Runtime은 Settings의 Scene Fade Layer와 Host의 Fade Source를 사용해
`SceneFader`를 하나 만든다.

```csharp
runtime.SceneFader.Cover
(
    runtime.DefaultSceneFadeParams,
    onCompleted: BeginSceneLoad,
    onFailed: Debug.LogException
);
```

화면을 완전히 덮은 뒤 App Flow가 Scene 로드를 수행하고,
로드와 UI 구성이 끝나면 같은 Fade를 걷어낸다.

```csharp
runtime.SceneFader.Reveal
(
    runtime.DefaultSceneFadeParams,
    onCompleted: OnSceneRevealed,
    onFailed: Debug.LogException
);
```

`Cover`는 Covered 상태와 Fade View를 유지한다.
`Reveal`은 기존 Fade View가 있을 때만 가능하며 완료 후 View를 반환한다.
새 Fade 요청은 진행 중인 이전 Transition을 취소한다.
Scene 로드 자체와 실패 복구 정책은 App Flow의 책임이다.

## Visibility

`VisibilityController`는 같은 Target에 중첩된 표시 요청을 적용하고
마지막 Handle 해제 시 원래 상태를 복원한다.

```csharp
Lease hidden = runtime.Visibility.Set(target, false);

// 표시 억제 수명이 끝난 시점
hidden.Dispose();
```

Target은 `IVisibilityTarget`을 구현한다.
`UGUIScreenDriver`와 `UITKScreenDriver`도 이 계약을 제공한다.

## Focus와 Input

Screen이 활성화되면 Focus는 다음 순서로 선택된다.

1. 해당 Screen의 마지막 유효 Focus
2. `ScreenOptions.DefaultFocus`
3. `IScreenDriver.DefaultFocus`
4. native Focus Driver의 fallback

Screen이 가려지기 전에 현재 Focus를 기록하고 다시 노출될 때 복원한다.
UGUI Focus와 UI Toolkit Focus는 동시에 남지 않도록 기술 전환 시 이전 선택을 비운다.

`ScreenOptions`의 Input 항목은 열린 모든 Screen에서 합성된다.

- 하나라도 `BlocksGameplayInput`이면 Gameplay Map 차단
- Cursor 정책은 가장 높은 `InputPriority`의 Screen이 결정
- 동률이면 나중에 획득한 Screen이 우선
- Screen 종료 후 Release Action이 모두 해제되면 이전 정책 복원

## UGUI Drag Visual

기존 Xeri `Drag_Drop`의 `DraggableUI`를 Presentation Layer와 연결하려면
`DragVisualController`를 사용한다.

```csharp
var dragVisualController =
    new DragVisualController(runtime.LayerRegistry);

IDisposable binding = dragVisualController.Bind
(
    draggable,
    new DragVisualParams(target, "Drag")
);

session.RegisterChild(binding);
```

Drag 시작 시 Target을 지정 UGUI Layer의 마지막 sibling으로 옮기고,
정상 종료와 강제 취소 모두에서 원래 부모·sibling·Transform 상태와
Layer Usage를 복원한다.

`DragVisualController`는 프로젝트 Composition이 소유한다.
모든 Binding과 Screen을 먼저 닫고, Controller를 해제한 뒤
관련 Profile Handle을 해제한다.

드래그 판정과 Drop 규칙은 [Drag_Drop README](../Drag_Drop/README.md)를 따른다.

## UGUI 보조 기능

| 기능 | 타입 | 책임 |
|---|---|---|
| Safe Area | `UGUILayoutController` | Screen Safe Area Root 갱신 |
| Placement | `PlacementController` | UI Bounds 안 배치와 Clamp |
| World Projection | `ProjectionController` | World 위치를 UGUI Local 위치로 변환 |
| Focus Highlight | `FocusHighlightController` | Focus 대상 강조 표시 수명 |
| Input Block | `UGUIInteractionBlocker` | 중첩 UGUI 상호작용 차단 Lease |

이 기능들은 `GameUIRuntime`이 자동 생성하지 않는다.
필요한 Screen 또는 프로젝트 Composition이 생성·연결하고,
반환된 Handle은 Screen Session이나 해당 기능 소유자가 해제한다.

## 종료와 오류 계약

일반 `Dispose`, `Release`와 callback 정리는 attempt-once다.

- 정리가 시작되면 논리 소유권을 먼저 Terminal 상태로 전환
- 일부 정리가 실패해도 독립적인 나머지 정리를 계속 시도
- 최초 오류와 정리 오류를 숨기지 않고 호출자에게 전달
- 실패한 동일 Handle이나 View를 재시도 대상으로 보관하지 않음

예외는 활성 Layer 소비자처럼 상태 변경 전 사전 조건에서 거부되고,
기존 소유권이 명확히 유지되는 경우다.
`GameUIProfileHandle.Dispose()`의 활성 소비자 검사가 이 계약을 사용한다.

Runtime의 정상 종료 순서:

1. Screen 명령 중지와 모든 Screen Source 반환
2. 프로젝트 Composition `OnReleasing`
3. Scene Fader와 Fade Source
4. Modal과 Visibility
5. Screen Registry, Input과 Focus
6. 추가 Profile과 기본 Profile
7. Transitioner와 Layer Registry

`Shutdown`은 오류가 발생해도 Runtime을 Terminal 상태로 끝낸다.
같은 Runtime을 재초기화하거나 일반 정리를 재시도하지 않는다.

## 구현 체크리스트

새 화면이나 프로젝트 구성을 추가할 때 다음을 확인한다.

- App에 `GameUIRuntime`과 `EventSystem`이 하나씩만 있는가
- Host의 Input Action Reference와 두 Focus Driver가 연결됐는가
- Scene Fade Source가 정확히 하나이며 Fade Layer 기술과 맞는가
- 활성 Profile 전체에서 Layer ID가 중복되지 않는가
- UGUI·UI Toolkit Layer Order가 공통 정렬 범위에 있는가
- Screen Source가 획득 실패를 원자적으로 정리하는가
- View callback이 직접 숨기지 않고 `ScreenSession.Close()`를 호출하는가
- 등록 Handle을 닫기 전에 해당 Screen Session을 닫았는가
- Source를 닫기 전에 그 Source가 만든 View를 모두 반환했는가
- Profile을 닫기 전에 Layer 소비자를 모두 해제했는가
- 일반 Dispose 실패에 재시도 구조를 추가하지 않았는가
- App Flow, 저장·불러오기와 게임 도메인 상태를 UI Core에 넣지 않았는가

새 backend enum, 전역 UI Manager, 복구 Registry나 재시도 상태 머신을
기본 확장점으로 추가하지 않는다.
새 UI 기술은 기존 `IPresentationLayerDriver`, `IScreenDriver`,
`IScreenSource`, `IFocusDriver`와 Source 소유권 계약으로 연결한다.
