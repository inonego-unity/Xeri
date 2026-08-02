# Game UI Validation

Xeri Game UI Core의 공개 경로를 한 Scene에서 실행하는 선택형 Package Sample이다.
Screen Stack, Modal, Overlay, Scene Fade, Focus, Input, UITK Layer와 Gradient/Gamma 합성을
실제 Handle과 Controller 수명으로 확인한다.

이 샘플 에셋은 Xeri의 공개 API와 Package 내부 에셋만 사용한다. 프로젝트 전용 Settings,
Input Actions, Bootstrapper, Render Pipeline Component는 참조하지 않는다. Xeri Runtime의
설치 의존성은 Game UI 사용 가이드의 `최초 설정 > 의존성`을 따른다.

## 가져오기

1. Unity에서 `Window > Package Manager`를 연다.
2. `Xeri` Package를 선택한다.
3. `Samples`의 `Game UI Validation`에서 `Import`를 누른다.
4. 가져온 `GameUIValidation.unity`를 연다.

Unity는 샘플을 다음 형식의 프로젝트 경로로 복사한다.

```text
Assets/Samples/Xeri/<version>/Game UI Validation/
```

샘플을 사용하지 않는 프로젝트에는 Scene, 폰트와 시각 기준본이 복사되지 않는다.

## 실행

1. `GameUIValidation.unity`를 연다.
2. Play Mode로 진입한다.
3. Mouse 또는 Keyboard/Gamepad Navigation과 Submit으로 버튼을 조작한다.

샘플은 `GameUIValidationSettings.asset`과 Xeri 표준 `GameUIHost.prefab`으로 독립 Runtime을
만들고 Scene이 닫힐 때 자신이 만든 Host 전체를 정리한다. 이 소유권 경계 덕분에
`Clear & Restore`도 애플리케이션의 Screen Stack을 변경하지 않는다.

프로젝트의 Game UI Bootstrapper가 자동으로 App Runtime을 생성한다면 검증 Scene을 실행하는
동안 해당 Bootstrapper Module을 비활성화한다. 이미 활성 Runtime Host가 있으면 샘플은 그
Runtime이나 Screen을 정리하지 않고 명확한 오류와 함께 시작을 거부한다.

## 검증 경로

| 동작 | 확인하는 공개 경로 |
|---|---|
| `PUSH DETAIL` | 같은 `IScreenSource`로 새 Screen Session을 Stack에 추가 |
| `PUSH ANOTHER` | 동일 Screen ID의 허용된 중복 Session 추가 |
| `REPLACE TOP` | 현재 top을 같은 등록의 새 Session으로 교체 |
| `POP SCREEN` | 일반 Close Transition과 이전 Focus 복원 |
| `OPEN MODAL` | Modal Handle을 현재 Screen의 자식 수명으로 소유 |
| `Overlay Toast` | `OverlayHandle`로 Layer Usage와 동적 View를 함께 획득·반환 |
| `Cover → Reveal` | 기본 `SceneFader`의 Cover와 Reveal 실행 |
| `Clear & Restore` | Stack을 정리한 뒤 Dashboard를 새로 획득 |

Dashboard는 Stack, Modal, Fade, Input Device와 Screen 상태 훅을 표시한다. 화면 표현에는
Xeri Linear/Radial/Conic Gradient Material, Layer Gamma 합성과 `XeriLoopAnimator`를 사용한다.

## 파일 구성

```text
GameUIValidation/
├── GameUIValidation.unity
├── GameUIValidationSettings.asset
├── GameUIValidationGameplay.inputactions
├── Runtime/   # 샘플 조립 코드와 전용 Assembly
├── UI/        # UXML, USS, Layer, Panel Settings와 Profile
├── Fonts/     # Unity와 HTML이 함께 사용하는 Inter와 OFL 1.1
└── Web~/      # HTML/CSS 시각 기준본과 1920×1080 Reference
```

`GameUIValidationGameplay.inputactions`는 UI가 Gameplay 입력을 차단하고 복원하는 경로를
독립적으로 실행하기 위한 최소 Action Map이다. 실제 애플리케이션의 입력 계약을 예시로
복제하지 않는다.

## HTML/CSS 시각 기준

`Web~/index.html`을 브라우저로 열면 Unity 화면의 1920×1080 기준본을 확인할 수 있다.
`Web~/style.css`와 `UI/GameUIValidationScreen.uss`는 USS에서 직접 표현 가능한 크기, 간격,
색, Gradient 각도와 Stop을 같은 값으로 유지한다.

HTML은 JavaScript로 1920×1080 Canvas를 균일 축소한다. Unity 화면은 고정 크기 Root와 전용
Panel Settings의 `Shrink` 모드로 같은 contain 배율을 적용한다.

다음 CSS 표현은 UI Toolkit에 직접 대응하는 기능이 없으므로 시각 비교 범위에서 제외한다.

- `backdrop-filter`
- `filter: saturate`
- CSS Gaussian `box-shadow`
- 브라우저와 Unity Text Engine의 글리프 Rasterization 차이

## 폰트 라이선스

Inter Font 파일은 SIL Open Font License 1.1로 배포된다. 저작권과 전체 라이선스 원문은
`Fonts/Inter-LICENSE.txt`에 함께 보관한다. 폰트를 수정하지 않은 상태로 Unity Sample과
HTML 기준본에 함께 사용한다.

## 제거

Package Manager의 샘플 Import는 Xeri Runtime을 변경하지 않는다. 검증 화면이 필요 없으면
프로젝트의 `Assets/Samples/Xeri/<version>/Game UI Validation` 폴더만 제거한다.
