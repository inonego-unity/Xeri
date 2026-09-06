# Bootstrapper Module 추가하기

이 가이드는 프로젝트의 초기화 작업 하나를 Xeri Bootstrapper 실행 순서에 연결하는 방법을 설명합니다.

## 목적

프로젝트 초기화 로직을 임의의 `Awake()` 순서에 의존시키지 않고, Initial Scene 전후의 명시적인 phase와 Asset 목록에서 실행 순서를 관리합니다.

## 언제 사용하는가

다음처럼 Initial Scene과의 순서가 중요한 초기화에 사용합니다.

- 게임 데이터나 설정 준비를 Initial Scene 전에 시작해야 할 때
- UI/Audio 같은 전역 Host를 특정 phase에서 구성해야 할 때
- 여러 초기화 작업의 순서를 Asset 목록으로 명시하고 싶을 때

특정 Scene 오브젝트가 활성화될 때만 필요한 기능은 Bootstrapper Module보다 그 오브젝트의 자연스러운 lifecycle에 두는 편이 낫습니다.

## 1. Module Asset 구현

`BootstrapperModuleAsset`을 상속하고 실행 phase와 `Init()`을 정의합니다.

```csharp
using UnityEngine;
using inonego.Xeri.Bootstrapper;

[CreateAssetMenu(menuName = "Sample/Bootstrapper/Data Module")]
public sealed class DataBootstrapperModuleAsset : BootstrapperModuleAsset
{
    public override BootstrapperModulePhase Phase =>
        BootstrapperModulePhase.BeforeInitialScene;

    public override async Awaitable Init()
    {
        await ProjectDataService.InitializeAsync();
    }
}
```
## 2. Phase 선택

- `BeforeInitialScene`: Initial Scene이 로드되기 전에 필요한 작업
- `AfterInitialScene`: Initial Scene의 오브젝트가 존재한 뒤 실행해야 하는 작업

Module 목록은 같은 phase 안에서 `BootstrapperSettings.Modules` 순서대로 실행됩니다. Module 간 암묵적 의존 관계를 만들기보다 목록 순서와 phase를 명확히 합니다.

## 3. Settings에 등록

`BootstrapperSettings`의 Modules 목록에 만든 Asset을 추가합니다. 같은 초기화 기능을 Scene Host와 Bootstrapper 양쪽에서 동시에 실행하지 않습니다.

## 비동기 작업을 어디까지 기다릴지

`Init()`이 반환될 때까지 Bootstrapper는 해당 Module이 완료되지 않은 것으로 봅니다. 반드시 startup을 막아야 하는 준비는 `await`하고, 의도적으로 background preparation을 시작만 하는 경우에는 프로젝트가 별도의 readiness 경계를 소유해야 합니다.

```text
Module.Init
├─ await 완료 → 다음 Module/Scene 진행
└─ background 시작 → readiness는 프로젝트가 별도로 보장
```

## 실패 처리

Module이 예외를 던지면 부분 생성 자원은 Module 또는 그 하위 Service가 정리해야 합니다. 전역 Registry에 객체를 공개해야 한다면 필요한 구성이 모두 성공한 뒤 마지막에 등록하는 편이 안전합니다.

## 관련 문서

- [Bootstrapper](../../modules/core/bootstrapper.md)
- [Xeri 통합 패턴](../../concepts/integration-patterns.md)