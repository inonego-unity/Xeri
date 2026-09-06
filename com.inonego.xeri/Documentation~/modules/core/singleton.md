# Singleton과 슬롯

Xeri Singleton은 하나의 전역 인스턴스만 강제하는 구조가 아니라, 이름이 붙은 여러 인스턴스를 등록하고 현재 컨텍스트에서 사용할 슬롯을 선택하는 Registry 기반 구조입니다.

## 왜 필요한가

전역 static 하나만 두면 테스트·미리보기·서브시스템별 인스턴스를 동시에 유지하기 어렵습니다. 슬롯 Registry는 여러 인스턴스를 이름으로 보관하고 `Scope()` 안에서만 현재 Context를 바꿀 수 있어 전역 접근 편의성과 다중 인스턴스를 함께 사용할 수 있게 합니다.

## 언제 사용하는가

- 기본 Runtime과 Preview/Test Runtime을 동시에 등록해야 할 때
- 특정 작업 범위에서만 `Current`가 다른 인스턴스를 가리켜야 할 때
- 이름으로 직접 접근하는 `Named`와 Context 기반 `Current`를 함께 제공하고 싶을 때

의존성을 생성자 주입으로 충분히 전달할 수 있는 로컬 객체에는 Singleton을 추가하지 않는 편이 좋습니다.

## 기본 사용

```csharp
SampleService.Register(new SampleService());
SampleService.Register("preview", new SampleService());

using (SampleService.Scope("preview"))
{
    SampleService current = SampleService.Current;
}

SampleService normal = SampleService.Current;
```

## 핵심 구조

```text
InstanceRegistry<T>
├─ DEFAULT_SLOT
├─ Named[slot]
└─ CurrentKey (AsyncLocal)
      ↓
    Current
```

`Singleton<T>`는 타입별 `InstanceRegistry<T>`를 정적 보유하고 등록·조회·Scope API를 노출합니다.

## 등록

- `Register(instance)`: 기본 슬롯 등록
- `Register(slot, instance)`: 지정 슬롯 등록, 기존 값을 교체할 수 있음
- `TryRegister()`: 다른 인스턴스가 이미 점유하면 기존 값을 유지하고 false
- `Unregister(slot)` 또는 `Unregister(instance)`
- `Clear()`: 모든 슬롯 제거

`Named[slot]`은 현재 Scope와 관계없이 특정 슬롯에 직접 접근합니다.
## Scope

`Scope(slot)`은 현재 async/thread 컨텍스트의 슬롯을 일시적으로 전환하고 `Dispose()` 시 이전 슬롯으로 복원합니다.

```csharp
using (MyService.Scope("Preview"))
{
    MyService.Current.DoWork();
}
```

Scope stack은 `AsyncLocal`을 사용하므로 async 컨텍스트별 현재 슬롯이 독립적입니다. `Scope()`, `OpenScope()`, `CloseScope()`는 같은 LIFO stack을 공유합니다.

프로덕션에서는 `using + Scope()`를 우선하고, 수동 `OpenScope/CloseScope`는 명시적 setup/teardown이 필요한 경우에만 사용합니다.

## 제약과 주의사항

- null 슬롯 key는 허용하지 않습니다. 기본 슬롯은 `DEFAULT_SLOT`을 사용합니다.
- `Current`는 현재 슬롯이 등록되지 않았으면 예외를 발생시킵니다. 선택적 환경에서는 `TryCurrent()`를 사용합니다.
- 슬롯 전환은 인스턴스 등록을 바꾸지 않습니다.
- Scope를 LIFO 순서로 닫지 않는 사용 패턴을 만들지 않습니다.

## 사용 예

Xeri의 `Localization`, `DataPackage`, `AudioManager` 등은 이 슬롯/Registry 패턴을 사용해 기본 인스턴스와 선택적 named context를 구분합니다.

## 관련 문서

- [DataPackage](../data/data-package.md)
- [Localization](../localization/localization.md)
- [Xeri Core](../../../Runtime/Core/README.md)
