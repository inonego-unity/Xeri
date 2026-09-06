# Bootstrapper

Xeri Bootstrapper는 Play 시작 시 애플리케이션 초기화 순서와 최초 Scene 전환 경계를 정의합니다.
각 기능은 `BootstrapperModuleAsset`으로 자신의 초기화 로직을 제공하고, Bootstrapper는 Initial Scene 전후 phase를 순서대로 실행합니다.

## 왜 필요한가

애플리케이션 시작 단계에서 여러 전역 기능이 제각각 `Awake()`나 임의 Scene 순서에 의존하면 초기화 선후관계를 추적하기 어렵습니다. Bootstrapper는 "Initial Scene 전에 무엇을 끝내야 하는가"와 "Scene이 준비된 뒤 무엇을 실행할 것인가"를 명시적인 phase와 Module 목록으로 드러냅니다.

## 언제 사용하는가

- 전역 데이터, 설정, UI/Audio Host처럼 시작 순서가 중요한 기능을 준비할 때
- 여러 초기화 작업의 순서를 Asset 구성으로 확인 가능하게 만들고 싶을 때
- Initial Scene 로드 자체를 초기화 경계로 사용하고 싶을 때

특정 Scene 오브젝트의 수명에만 필요한 기능은 Bootstrapper에 넣기보다 해당 Host가 직접 소유하는 편이 낫습니다.

## 기본 사용

프로젝트별 Module은 `BootstrapperModuleAsset`을 상속하고 phase와 `Init()`만 구현합니다. 같은 phase의 Module은 Settings 목록 순서대로 실행됩니다.

자세한 구현 절차는 [Bootstrapper Module 추가하기](../../guides/core/add-bootstrapper-module.md)를 참고합니다.

## 실행 흐름

```text
BeforeSceneLoad
   ↓
Bootstrapper Scene 보장
   ↓
BeforeInitialScene Module 순차 실행
   ↓
다음 frame
   ↓
Initial Application Scene load + active 검증
   ↓
AfterInitialScene Module 순차 실행
   ↓
Bootstrap 실행 객체 제거
```

## Settings

`BootstrapperSettings`는 `Assets/Resources/BootstrapperSettings.asset`에 위치합니다.
Editor에서는 설정 Asset이 없으면 접근 시 생성할 수 있습니다.

주요 설정은 다음과 같습니다.

| 설정 | 의미 |
|---|---|
| `BootstrapperSceneIndex` | 가장 먼저 load할 Bootstrapper Scene의 Build Settings index |
| `SceneIndexToLoad` | 초기화 후 load할 Application Scene index |
| `Modules` | 실행할 `BootstrapperModuleAsset` 목록 |

Editor에서 Play Mode 시작 Scene을 별도로 지정한 경우에도 Bootstrapper Scene과 충돌하는 override를 정리하고 원래 시작 대상 Scene을 초기 Application Scene으로 복구하는 경로가 있습니다.

## Module phase

`BootstrapperModulePhase`는 두 단계입니다.

- `BeforeInitialScene`: Scene과 무관한 application service나 persistent host 준비
- `AfterInitialScene`: 최초 Application Scene이 load되고 active가 된 뒤 필요한 초기화

`BootstrapperRunner`는 Settings의 Module 목록 순서를 그대로 유지하며 해당 phase의 Module만 `await module.Init()`로 실행합니다.
## Module 구현

새 초기화 기능은 `BootstrapperModuleAsset`을 상속해 실행 phase와 `Init()`을 정의합니다.
Module은 다른 Module의 구체 타입을 직접 탐색하기보다 필요한 service의 공개 진입점이나 명시적 설정을 사용합니다.

```csharp
public sealed class ExampleBootstrapperModuleAsset : BootstrapperModuleAsset
{
    public override BootstrapperModulePhase Phase => BootstrapperModulePhase.BeforeInitialScene;

    public override async Awaitable Init()
    {
        // 초기화
        await Awaitable.NextFrameAsync();
    }
}
```

## 순서와 실패

같은 phase 안에서는 `Modules` 목록 순서가 실행 순서입니다.
`Init()` 예외는 해당 bootstrap 실행 흐름으로 전달되며, Runner가 실패를 숨기고 다음 Module을 계속 실행하는 구조가 아닙니다.

따라서 Module 간 실제 선행 관계가 있으면 목록 순서와 공개 준비 상태를 명시적으로 맞춰야 합니다.
단순히 "언젠가 초기화될 것"을 전제로 서로 경쟁하도록 만들지 않습니다.

## 제약과 주의사항

- 수동 초기화와 같은 기능의 Bootstrapper 초기화를 동시에 사용하지 않습니다.
- `BeforeInitialScene`에서 Application Scene 오브젝트가 이미 존재한다고 가정하지 않습니다.
- `AfterInitialScene`은 최초 Scene이 load 완료되고 active임을 전제로 사용할 수 있습니다.
- Module phase는 두 개뿐이므로 실제 요구 없이 세부 startup state machine을 Module 내부에 중복해서 만들지 않습니다.
- Bootstrapper 실행 객체는 startup phase 종료 후 파괴되므로 장기 service는 자신의 host 또는 별도 persistent 객체가 소유해야 합니다.

## 관련 문서

- [Xeri Core](../../../Runtime/Core/README.md)
- [Xeri Game UI 설정](../game-ui/setup.md)
- [Xeri Audio](../../../Runtime/Playback/Cue/Audio/README.md)
- [Xeri 구조](../../concepts/architecture.md)
