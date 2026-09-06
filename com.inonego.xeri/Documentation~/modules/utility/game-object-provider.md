# GameObject Provider

`IGameObjectProvider`는 GameObject의 획득 위치와 공급 backend 차이를 상위 시스템에서 숨기기 위한 생성·반환 계약입니다.

## 왜 필요한가

상위 UI/Presentation 시스템이 Prefab Instantiate와 Addressables Instantiate 차이를 직접 알게 되면 공급 방식이 바뀔 때 호출 코드까지 바뀝니다. Provider는 획득과 반환을 같은 backend 경계로 묶어 상위 시스템이 "GameObject 하나를 빌리고 돌려준다"는 계약만 보게 합니다.

## 언제 사용하는가

- 같은 상위 기능에서 Prefab/Addressables 공급 방식을 교체할 수 있어야 할 때
- 생성 위치 Parent와 반환 backend를 한 객체로 캡슐화하고 싶을 때
- Overlay/Window/Profile 같은 시스템이 구체 Instantiate 방식을 몰라야 할 때

프로젝트 도메인 초기화까지 자동으로 해주는 factory가 필요하다면 Provider 위에 별도 Adapter/Factory를 두는 편이 낫습니다.

## 기본 사용

```csharp
IGameObjectProvider provider = new PrefabGameObjectProvider(prefab, parent);

GameObject instance = provider.Acquire(worldPositionStays: false);
try
{
    BindDomainState(instance);
}
finally
{
    provider.Release(instance);
}
```

Addressables 구현도 호출자는 같은 `AcquireAsync`/`Release` 계약을 사용합니다. 획득한 객체는 반드시 같은 Provider로 반환합니다.

## 핵심 계약

```text
IGameObjectProvider
├─ Parent
├─ Acquire()
├─ AcquireAsync()
└─ Release()
```

Provider는 획득한 GameObject를 어떤 방식으로 만들었는지에 따라 반환도 같은 backend로 처리합니다.

현재 대표 구현은 다음과 같습니다.

- `PrefabGameObjectProvider`: Prefab 기반 생성/반환
- `AddressableGameObjectProvider`: Addressables 기반 비동기 공급과 반환

## Parent와 Transform

`Parent`가 null이면 root에 생성합니다. `worldPositionStays`는 부모 변경 시 Transform의 world 상태 보존 여부를 전달합니다.

Provider가 반환하는 객체의 활성 상태는 공급 원본과 구현 계약을 따르며, 공통 interface가 임의로 활성화 상태를 강제하지 않습니다.

## 반환 책임

`Acquire()` 또는 `AcquireAsync()`로 얻은 유효한 객체는 같은 Provider에 `Release()`해야 합니다.

`Release()` 호출과 함께 호출자의 반환 책임은 종료됩니다. 반환 중 예외가 발생하더라도 같은 인스턴스를 재시도하는 일반 계약은 아닙니다.
## 사용 경계

Game UI Profile, Overlay, Window 등 상위 시스템이 GameObject 생성 방식을 알 필요가 없을 때 Provider를 주입합니다.

```text
상위 시스템
   ↓ IGameObjectProvider
Prefab / Addressables
```

새 공급 경로가 필요하면 상위 기능별 별도 factory를 만들기 전에 `IGameObjectProvider`로 표현 가능한지 먼저 확인합니다.

## 제약과 주의사항

- 서로 다른 Provider에서 획득한 객체를 임의로 다른 Provider에 반환하지 않습니다.
- Addressables처럼 외부 handle 수명이 있는 구현은 Provider 내부에서 획득/반환 쌍을 보존해야 합니다.
- Provider는 프로젝트 도메인 초기화나 binding을 자동으로 수행하는 범용 composition service가 아닙니다.

## 관련 문서

- [Object Pooling](pooling.md)
- [소유권과 수명](../../concepts/ownership-and-lifetime.md)
- [Utility 모듈](../../../Runtime/유틸리티/README.md)
