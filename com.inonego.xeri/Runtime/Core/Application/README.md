# Xeri Application

## 목적

Application은 게임별 화면 흐름이나 저장 정책이 아니라 **실행 세대의 시작·종료 및 소유 자원의 수명**을 담당합니다. 게임은 초기화 순서, 사용할 서비스, Readiness를 소비하는 시점과 전환 정책을 구성합니다.

공개 API는 원래의 `inonego.Xeri.Application` 네임스페이스를 유지합니다. Unity API의 `Application` 타입이 필요한 곳에서는 `UnityEngine.Application`을 명시해 식별자 충돌을 피합니다. 순수 `ApplicationRuntime`과 `ApplicationScope`는 Bootstrapper나 Unity Scene 없이도 사용할 수 있습니다.

## 구성 요소

| 타입 | 책임 |
|---|---|
| `ApplicationRuntime` | 단일 실행 세대, 순차 모듈 시작, 시작 실패 정리, 종료 결과 |
| `ApplicationScope` | 동기/비동기 자원, 자식 Scope, 임시 자원 이전, 추적 작업 종료 |
| `ApplicationContext` | Runtime·Scope·취소 신호를 모듈에 전달 |
| `IApplicationModule` | 순차 초기화를 표현하는 독립적인 C# 계약 |
| `ApplicationHost` | Unity Play Session에서 기본 Runtime 하나의 시작·종료 요청 |

## 독립 Runtime 최소 예제

Unity의 자동 Bootstrapper를 거치지 않고도 실행 수명 하나를 직접 구성할 수 있습니다. 모듈은 자신이 획득한 자원의 종료 책임을 즉시 Scope에 등록하고, 호출자는 시작과 종료를 명시적으로 기다립니다.

```csharp
using System;
using System.Threading;
using System.Threading.Tasks;

using inonego;
using inonego.Xeri;
using inonego.Xeri.Application;

public sealed class ExampleModule : IApplicationModule
{
    public Task StartAsync(ApplicationContext context, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        context.Scope.Own(new Lease(() => Console.WriteLine("Module released")));
        return Task.CompletedTask;
    }
}

public static class ExampleApplication
{
    public static async Task RunAsync(Func<Task> run)
    {
        var runtime = new ApplicationRuntime();
        try
        {
            await runtime.StartAsync(new IApplicationModule[] { new ExampleModule() });
            await run();
        }
        finally
        {
            await runtime.StopAsync();
        }
    }
}
```

위 예제의 종료 콜백은 사용 예시이며 실제 모듈은 구독 해제, 서비스 Dispose 등 자신이 획득한 자원을 정리해야 합니다. 자동 Bootstrapper를 사용하는 Unity Play Session에서는 별도로 같은 서비스를 시작하지 않습니다.

## 실행 수명

- 새 Runtime은 `Created → Starting → Running → Stopping → Stopped` 순서로 실행됩니다. 정상 종료 요청으로 중단된 초기화는 `Stopped`로 끝나고, 실제 초기화·자원 정리 오류는 `Failed`로 끝납니다.
- 한 번 시작한 Runtime은 재사용하지 않습니다. `StopAsync()`의 중복 호출은 동일한 종료 작업을 공유합니다.
- `IApplicationModule.StartAsync(context, token)`의 완료는 *해당 초기화 단계의 완료*입니다. 백그라운드 준비 작업의 Ready와 동일하지 않습니다.
- 준비 완료가 필요한 모듈/화면은 자신의 공개 Task를 소비자가 명시적으로 기다리게 합니다. Scope는 준비 정책을 선택하지 않습니다.

## Scope와 작업 소유권

- `Own(resource)`: 참조형 `IDisposable`의 종료 책임을 등록합니다. 소유자가 아닌 객체는 등록하지 않습니다. 구조체 해제 책임은 `Lease`와 같은 참조형 핸들로 감쌉니다.
- 서비스 조회는 기존 `Singleton<T>`에 맡기고 Scope는 인스턴스와 `Lease`의 종료 책임만 소유합니다. 별도 서비스 레지스트리는 도입하지 않습니다.
- `OnClose(asyncCleanup)`: 비동기 종료 책임을 등록합니다. 명시적으로 종료 순서를 배치합니다.
- `CreateChild()`: 부모가 종료 책임을 보유하는 자식 Scope를 만듭니다.
- `TransferTo(destination, resource)`: 확정된 참조형 자원의 **Dispose 책임만** 다른 열린 Scope로 넘깁니다. 이전 호출 순서가 아니라 원래 자원 등록 순서가 유지됩니다. 원래 Scope의 취소 토큰에 종속된 작업 수명은 이전되지 않습니다.
- 별도 취소 콜백은 `CancellationToken.Register()`로 구독하고 Xeri `Lease`를 `Own()`에 등록해 종료 시 구독을 해제합니다. 구독은 초기화 중, 종료 시작 전에 구성합니다. 로더가 Dispose에서만 취소하는 구조라면 중단 신호를 독립적으로 노출해야 합니다.
- `Track(task)`: 호출자가 이미 시작한 작업을 종료 대기 목록에 등록합니다. Scope는 작업을 시작하거나 스케줄링하지 않습니다.
- `OwnedEntryCount`와 `ActiveOperationCount`: 해당 Scope가 소유한 자원·콜백·자식 수와 직접 추적하는 미완료 작업 수를 별도로 관찰합니다. 자식의 자원·작업 수는 합산하지 않습니다.
- `CloseAsync()`: 하위 Scope 트리까지 신규 등록·이전을 차단하고 취소를 전달합니다. **해당 트리의 모든 추적 작업 완료를 기다린 뒤 자식 Scope부터 종료**하고, 각 Scope의 자원을 원래 등록 역순으로 정리합니다. 일부 실패 시에도 나머지를 정리하고 집계된 오류를 반환합니다.
- `RequestCancellationAsync()`는 취소 요청과 콜백 완료·오류를 동일한 Task로 반환합니다. 이후 새 작업 추적은 거절하고 이미 확보한 자원은 종료 단계까지 등록할 수 있습니다. `CloseAsync()`는 해당 취소 결과를 한 번 관찰한 뒤 작업 완료와 자원 정리를 진행합니다. 취소 콜백 안에서는 이 Task의 완료를 동기적으로 기다리지 않습니다.
- 게임별 Composition은 `IApplicationModule.StartAsync(context, token)`에서 서비스를 선택·등록합니다. Bootstrapper와 독립적으로 `ApplicationRuntime.StartAsync(modules)`를 호출할 수 있습니다. 별도의 DI Container는 필요하지 않습니다.
- Singleton을 사용하는 서비스라면 Scope에 **등록 해제용 `Lease`를 먼저, 서비스 인스턴스를 나중에** 소유시키고 `TryRegister(instance)`로 충돌을 확인합니다. Scope의 역순 종료에서 Dispose 후 `Unregister(instance)`가 실행됩니다. 구성 실패 시 Runtime이 Scope를 닫아 획득한 자원을 정리합니다.
- Unity `SubsystemRegistration` 단계에서 다른 RuntimeInitialize 콜백의 실행 순서에 의존해 `ApplicationHost.Current`를 읽지 않습니다.
- 취소를 무시하는 Task는 스스로 완료할 때까지 `CloseAsync()`가 대기합니다. 임의의 강제 중단·타임아웃을 제공하지 않습니다.

### Singleton 서비스 구성 예시

```csharp
var service = new GameService(); // Singleton<GameService>, IDisposable
scope.Own(new Lease(() => GameService.Unregister(service)));
scope.Own(service);

if (!GameService.TryRegister(service))
{
    throw new InvalidOperationException("GameService 슬롯이 이미 점유되어 있습니다.");
}
```

이 코드는 게임별 모듈 구성 예시이며 Xeri의 Singleton 구현을 변경하지 않습니다. `Singleton<T>.Scope()`는 **조회 슬롯 선택**이고 `ApplicationScope`는 **소유권 수명**이므로 서로 다른 개념입니다.

## Bootstrapper 실행

Unity `SubsystemRegistration`에서는 이전 Play Session의 종료를 요청하고 새 세대 공개를 보류합니다. Bootstrapper가 `ApplicationHost.StartAsync()`를 호출하면 이전 Scope 정리 완료를 기다린 뒤 새 Runtime이 생성·공개됩니다. 이전 종료가 실패하면 새 세대 시작도 실패합니다. Initial Scene 전후 phase와 씬 로드는 하나의 `IApplicationModule` 작업으로 실행합니다.

`BootstrapperModuleAsset`은 Unity Scene phase 전용 Asset 계약이며 `IApplicationModule`을 구현하지 않습니다. Bootstrapper 내부 Adapter가 실행할 때만 Application의 초기화 목록에 들어갑니다. 각 Asset은 `StartAsync(context, token)`을 제공하고, 생성한 자원은 `context.Scope`가 소유합니다. 이 기본 Unity Host는 Play Session당 활성 Runtime 하나를 관리하며, 다중 Application Host를 제공하지 않습니다.

## Unity Play 시작 완료 관찰

Unity 자동 Bootstrapper를 사용하는 Scene 코드에서는 `ApplicationHost.StartupTask`를 기다린 뒤 기본 Runtime을 사용합니다. 이 Task는 `SubsystemRegistration`에서 Play Session별로 생성되므로, `ApplicationHost.Current`가 아직 공개되지 않은 초기화 중에도 접근할 수 있습니다. 정상적으로 수락된 `ApplicationHost.StartAsync()` 호출이 받는 Task와 같은 완료 작업입니다.

```csharp
using System;

using UnityEngine;

using inonego;
using inonego.Xeri;
using inonego.Xeri.Application;

public sealed class ExampleSceneEntry : MonoBehaviour
{
    private async void Start()
    {
        try
        {
            await ApplicationHost.StartupTask;

            var runtime = ApplicationHost.Current;
            if (runtime == null || runtime.State != ApplicationState.Running) return;

            // 이 시점에 모든 Bootstrapper phase가 완료되었습니다.
        }
        catch (OperationCanceledException)
        {
            // 초기화 도중 Play Session이 종료된 경우
        }
        catch (Exception exception)
        {
            Debug.LogException(exception, this);
        }
    }
}
```

- `StartupTask`는 모든 Bootstrapper phase를 포함한 시작에 성공하면 완료됩니다. Bootstrapper 설정 오류·이전 세대 정리 실패·초기화 실패는 예외로 전달하며, 시작 중 Play 종료는 취소로 전달합니다.
- 성공 후 종료되는 경우에도 `StartupTask`는 완료된 상태를 유지합니다. **현재 실행 여부**는 `ApplicationHost.Current.State`, **서비스의 유효 수명**은 `ApplicationScope.CancellationToken`으로 판단합니다.
- 시작을 수행하는 Bootstrapper Module 안에서 `StartupTask`를 기다리면 자신의 초기화 완료를 다시 기다리는 순환 의존이 생길 수 있습니다. Module 간에는 phase와 해당 모듈의 준비 계약을 사용합니다.
- `StartupTask`는 Unity Play Session을 연 뒤에 접근해야 합니다. 다른 Unity 초기화 콜백의 `SubsystemRegistration` 실행 순서에 의존해 조회하지 않습니다.

## 제한 및 책임 경계

- `Singleton<T>.Clear()`는 Dispose하지 않습니다. Singleton의 슬롯 선택은 ApplicationScope의 **자원 소유권**과 별개입니다.
- Unity의 `Application.quitting` 및 Editor Play 종료 신호는 비동기 종료를 **요청**할 수 있지만, 프로세스 강제 종료와 Editor Play 종료가 정리를 기다리지는 않습니다.
- Unity 오브젝트를 소유한 `ApplicationRuntime.StopAsync()` 및 `ApplicationScope.CloseAsync()`는 **Unity 메인 스레드**에서 호출해야 합니다. Core의 중복 종료 동기화는 메인 스레드 Dispatcher를 제공한다는 뜻이 아닙니다.
- 씬 트랜잭션, 게임별 UI/세션, 저장 및 일시정지 정책은 포함하지 않습니다.

## 검증 경계

새 EditMode 계약 테스트는 `Runtime/Core/Application/TEST/EDIT`에 위치합니다. 독립 .NET NUnit 실행은 Scope와 Runtime 계약을 확인하지만, Unity Editor의 Domain Reload 설정·씬 로딩·실제 `Application.quitting` 완료는 별도 PlayMode 검증이 필요합니다.
