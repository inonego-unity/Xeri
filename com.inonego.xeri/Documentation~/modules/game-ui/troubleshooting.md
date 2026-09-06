# Game UI 구현과 문제 해결

## 목적

새 UI 기능을 추가할 때 기존 확장 계약과 소유권 경계를 먼저 선택하고, Runtime 초기화·Layer·Screen·Focus·UITK 표현 문제를 증상별로 점검하는 유지보수/문제 해결 문서입니다.

## 언제 읽는가

- 새 Layer/Screen/Overlay/Focus backend를 추가하기 전
- Runtime 초기화나 Screen Open이 실패할 때
- Focus/Input/World UI 위치가 예상과 다를 때
- UITK Gradient/Gamma/Loop 표현 문제가 있을 때

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
