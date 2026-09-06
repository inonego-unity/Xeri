# Game UI 설정과 시작

## 목적

새 프로젝트에서 Game UI Runtime을 실제로 띄우기 위해 필요한 패키지, Host, Settings, Profile과 Bootstrapper 연결 순서를 설명합니다.

## 언제 읽는가

- Game UI를 처음 설치·초기화할 때
- `GameUIRuntime.Current`가 생성되지 않을 때
- Settings/Profile/Fade Layer/Input Actions의 최소 구성을 확인할 때
- 제공 Sample을 기준으로 환경을 검증할 때

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
