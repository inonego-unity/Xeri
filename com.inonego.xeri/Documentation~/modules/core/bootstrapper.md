# Bootstrapper

Xeri Bootstrapper는 Unity 게임 실행 시 Initial Scene 전후 초기화 모듈을 **자동으로 한 번 실행하는 기본 진입점**입니다. Audio·UI·설정·데이터 모듈은 `BootstrapperSettings`에 등록된 순서와 phase에 따라 실행됩니다. 설정 누락은 자동 초기화 생략이 아니라 구성 오류입니다.

Bootstrapper는 Initial Scene 전후의 순차 초기화 절차만 정의합니다. 실제 실행 수명, 자원 소유권, 취소 및 정리는 `ApplicationRuntime`과 `ApplicationScope`가 담당합니다. 기본 Unity Host는 한 Play Session에 활성 Runtime 하나를 관리합니다.

## 왜 필요한가

애플리케이션 시작 단계에서 여러 전역 기능이 제각각 `Awake()`나 임의 Scene 순서에 의존하면 초기화 선후관계를 추적하기 어렵습니다. Bootstrapper는 "Initial Scene 전에 무엇을 끝내야 하는가"와 "Scene이 준비된 뒤 무엇을 실행할 것인가"를 명시적인 phase와 Module 목록으로 드러냅니다.

## 언제 사용하는가

- 전역 데이터, 설정, UI/Audio Host처럼 시작 순서가 중요한 기능을 준비할 때
- 여러 초기화 작업의 순서를 Asset 구성으로 확인 가능하게 만들고 싶을 때
- Initial Scene 로드 자체를 초기화 경계로 사용하고 싶을 때

특정 Scene 오브젝트의 수명에만 필요한 기능은 Bootstrapper에 넣기보다 해당 Host가 직접 소유하는 편이 낫습니다.

## 기본 사용

프로젝트별 Module은 `BootstrapperModuleAsset`을 상속하고 `Phase`와 `StartAsync(ApplicationContext, CancellationToken)`을 구현합니다. 이는 `IApplicationModule`과 **별도의 Scene phase 계약**입니다. Bootstrapper 내부 Adapter만 `IApplicationModule`을 구현하며, 각 Module이 획득한 자원은 `context.Scope`에 소유 등록합니다.

자세한 구현 절차는 [Bootstrapper Module 추가하기](../../guides/core/add-bootstrapper-module.md)를 참고합니다.

## 실행 흐름

```text
SubsystemRegistration → 이전 실행 세대 종료 요청
   ↓
BeforeSceneLoad
   ↓
Bootstrapper Scene 보장
   ↓
이전 종료 완료 → 새 Runtime 생성·공개
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
   ↓
ApplicationRuntime은 Play Session 종료까지 유지
```

## Settings

`BootstrapperSettings`는 `Assets/Resources/BootstrapperSettings.asset`에 위치합니다. 실행 시 설정 조회는 에셋을 생성하지 않습니다. 설정이 없거나 Scene 인덱스가 잘못됐다면 Editor에서는 Play 종료, Player에서는 종료를 요청합니다. 에셋 생성은 Project Settings 또는 Build Profile의 설정 편집 경로에서 명시적으로 수행합니다. 두 Scene 인덱스는 필수이며, 서로 달라야 하고 **Build Settings의 활성화된 Scene 순서**를 기준으로 유효해야 합니다. 비활성화된 Scene은 선택 목록과 인덱스 계산에서 제외합니다.

주요 설정은 다음과 같습니다.

| 설정 | 의미 |
|---|---|
| `BootstrapperSceneIndex` | 가장 먼저 load할 Bootstrapper Scene의 Build Settings index |
| `SceneIndexToLoad` | 초기화 후 load할 Application Scene index |
| `Modules` | 실행할 `BootstrapperModuleAsset` 목록 |

Editor에서 일반 작업 Scene으로 Play를 시작하면 해당 Scene을 초기 Application Scene으로 복구합니다. Bootstrapper Scene에서 직접 Play를 시작하면 `SceneIndexToLoad`를 사용합니다.

## Module phase

`BootstrapperModulePhase`는 두 단계입니다.

- `BeforeInitialScene`: Scene과 무관한 application service나 persistent host 준비
- `AfterInitialScene`: 최초 Application Scene이 load되고 active가 된 뒤 필요한 초기화

`BootstrapperRunner`는 Settings의 Module 목록 순서를 유지하면서 해당 phase의 `StartAsync(context, token)`만 실행합니다.

## Module 구현

새 초기화 기능은 `BootstrapperModuleAsset`을 상속해 실행 phase와 `StartAsync()`를 정의합니다.
Module은 다른 Module의 구체 타입을 직접 탐색하기보다 필요한 service의 공개 진입점이나 명시적 설정을 사용합니다.

```csharp
using System.Threading;
using System.Threading.Tasks;

using inonego.Xeri.Application;
using inonego.Xeri.Bootstrapper;

public sealed class ExampleBootstrapperModuleAsset : BootstrapperModuleAsset
{
    public override BootstrapperModulePhase Phase => BootstrapperModulePhase.BeforeInitialScene;

    public override Task StartAsync(ApplicationContext context, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.CompletedTask;
    }
}
```

## 자동 시작 완료 관찰

Initial Scene 오브젝트의 `Awake()`와 `OnEnable()`은 `AfterInitialScene` 초기화가 끝나기 전에 실행될 수 있습니다. 게임 로직 진입 시점을 조정해야 하는 Scene 코드는 `await ApplicationHost.StartupTask`로 모든 시작 phase가 완료됐는지 관찰한 뒤 `ApplicationHost.Current`를 사용합니다. 이 Task는 성공 시 완료되고, Settings 검증 또는 초기화 실패 시 예외를 전달하며, 시작 중 Play 종료 시 취소됩니다.

`StartupTask`는 **외부 소비자가 시작을 기다리기 위한 API**입니다. Bootstrapper Module의 `StartAsync()` 안에서 기다리면 자신의 완료를 기다리는 순환 의존이 되므로 사용하지 않습니다. 실제 코드 예제와 종료 후 수명 경계는 [Application README](../../../Runtime/Core/Application/README.md#unity-play-시작-완료-관찰)를 참고합니다.

## 순서와 실패

같은 phase 안에서는 `Modules` 목록 순서가 실행 순서입니다.
`StartAsync()` 예외는 ApplicationRuntime으로 전달되고, Runtime은 Scope에 등록된 자원을 정리합니다. 이전 실행 세대의 정리가 끝나지 않았다면 새 Runtime은 시작하지 않습니다. Runner는 실패를 숨기고 다음 Module을 실행하지 않습니다. 획득한 자원은 즉시 Scope에 소유 등록해야 합니다. 게임 Scene 로드 후 초기화가 실패하면 Bootstrapper Scene으로 복귀해 불완전하게 준비된 게임 Scene을 종료합니다.

따라서 Module 간 실제 선행 관계가 있으면 목록 순서와 공개 준비 상태를 명시적으로 맞춰야 합니다.
단순히 "언젠가 초기화될 것"을 전제로 서로 경쟁하도록 만들지 않습니다.

## 제약과 주의사항

- 동일한 기능을 수동 초기화와 Bootstrapper 양쪽에서 시작하지 않습니다. 순수 `ApplicationRuntime`은 Bootstrapper와 독립적인 수명 관리 계약이지만, Unity 게임의 기본 자동 Bootstrapper를 생략할 수 있다는 의미는 아닙니다.
- `BeforeInitialScene`에서 Application Scene 오브젝트가 이미 존재한다고 가정하지 않습니다.
- `AfterInitialScene`은 최초 Scene이 load 완료되고 active임을 전제로 사용할 수 있습니다.
- Module phase는 두 개뿐이므로 실제 요구 없이 세부 startup state machine을 Module 내부에 중복해서 만들지 않습니다.
- Bootstrapper 실행 객체는 startup phase 종료 후 파괴됩니다. Bootstrapper Module이 생성한 지속 자원의 종료는 해당 기본 `ApplicationScope`가 소유합니다. 따라서 이 Host에서 Application을 종료하면 해당 자원도 종료됩니다. 외부 소유 자원을 이중 등록하지 않습니다.
- Scene 로드 전 프레임 대기는 Application 취소 토큰을 따르며, 이미 진행 중인 Scene 로드의 완료는 기다립니다. 완료 뒤 취소를 전파하므로 종료 정리가 Scene 로드보다 앞서지 않습니다.
- 초기화 중 Scene에 이미 생성된 오브젝트는 Awake/OnEnable이 수행됐을 수 있습니다. 게임 로직의 정상 진입은 `ApplicationRuntime.Running` 이후로 게이트해야 하며, 실패 시 Runtime은 소유 자원을 정리하고 Bootstrapper Scene으로 복귀합니다.
- 프로세스 강제 종료 시 비동기 정리 완료는 보장할 수 없습니다.

## 관련 문서

- [Application Lifetime](../../../Runtime/Core/Application/README.md)
- [Xeri Core](../../../Runtime/Core/README.md)
- [Xeri Audio](../../../Runtime/Playback/Cue/Audio/README.md)
- [Xeri 구조](../../concepts/architecture.md)
