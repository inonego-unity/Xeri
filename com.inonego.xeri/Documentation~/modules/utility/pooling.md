# Object Pooling

Xeri Pool은 대기 중인 `Released` 항목과 사용 중인 `Acquired` 항목을 명시적으로 구분하고, 일반 소비자에게는 `Lease<T>`로 일회 반환 책임을 전달하는 풀링 시스템입니다.

## 왜 필요한가

풀링에서 "현재 사용 중인가", "이미 반환됐는가", "오래된 반환 핸들이 새 획득까지 건드리는가"를 호출자 관례에만 맡기면 이중 반환과 소유권 꼬임이 생기기 쉽습니다. Xeri Pool은 참조 동일성과 acquisition generation을 추적하고 `Lease<T>`로 해당 획득 한 번의 반환 책임을 묶습니다.

## 언제 사용하는가

- 같은 객체를 반복 생성/반환하며 획득 상태를 검증해야 할 때
- 일반 소비자가 반환 호출을 잊지 않도록 Lease 수명을 쓰고 싶을 때
- Pool 사이에서 Acquired/Released 소유권을 명시적으로 이동해야 할 때

생성 비용이 작고 재사용이 필요 없는 단순 값 객체에는 Pool을 추가하지 않는 편이 낫습니다.

## 기본 사용

일반 소비자는 `AcquireLease()`를 우선합니다.

```csharp
var pool = new Pool<ReusableItem>();

using (Lease<ReusableItem> lease = pool.AcquireLease())
{
    ReusableItem item = lease.Value;
    item.Execute();
}
// Dispose 시 현재 acquisition이 아직 유효하면 Pool로 반환
```

직접 `Acquire()`를 사용한 경우에는 소유자가 `try/finally`나 동등한 수명 경계에서 `Release(item)`을 보장해야 합니다.

## 소유권 모델

```text
Released
   ↓ Acquire
Acquired + Generation
   ↓ Release / Lease.Dispose
Released
```

`PoolBase<T>`는 참조 동일성으로 항목을 추적합니다. 같은 객체가 현재 Pool의 `Acquired`와 `Released`에 동시에 존재할 수 없습니다.

## 직접 획득과 Lease 획득

두 가지 사용 경계가 있습니다.

- `Acquire()` / `AcquireAsync()`: 항목만 반환하며 호출자가 직접 `Release()`해야 합니다.
- `AcquireLease()` / `AcquireLeaseAsync()`: 항목과 해당 획득 generation의 일회 반환 책임을 함께 반환합니다.

일반 consumer는 Lease 경계를 우선합니다.

오래된 Lease는 항목이 이미 직접 반환되거나 다른 generation으로 다시 획득된 경우 현재 소유권을 변경하지 않습니다.
## 반환 실패

Lease 반환 중 파생 Pool의 반환 작업이 실패하고 같은 generation을 여전히 소유하고 있으면 acquisition record를 종료하고 `OnDiscard()`로 실패 항목을 최종 정리합니다.

`ReleaseAll()`은 초기 `Acquired` 항목 전체에 대해 반환을 끝까지 시도하고 오류를 모아 마지막에 `AggregateException`으로 전달합니다.

## Pool 간 이동

`MoveAcquiredOneTo()`와 `MoveReleasedOneTo()`는 항목 소유권을 다른 Pool로 이동합니다.

- Acquired 이동은 대상 Pool의 인수가 성공한 뒤 기존 Pool 소유권을 제거합니다.
- Released 이동은 대상 인수가 실패하면 꺼낸 항목을 원래 Pool로 되돌립니다.
- 대상 Pool이 이미 같은 객체를 관리하면 거부합니다.

## 구현 종류

- `Pool<T>`: 일반 class를 `new T()`로 생성
- `GOCompPool<T>`: Unity GameObject/Component 계열의 생성·활성 상태와 반환을 관리
- 파생 Pool: `AcquireNew`, `AcquireNewAsync`, 필요 시 acquire/release hook을 구현

## 제약과 주의사항

- Thread-safe 사용은 지원하지 않습니다.
- `ReleaseAll()`의 반환 hook에서 같은 Pool의 Acquired 구조를 변경하는 재진입을 하지 않습니다.
- 파생 `Release()`는 예외 가능한 작업을 `base.Release()` 이전에 끝내야 합니다.
- 일반 소비자가 직접 `Acquire()`를 사용하면 반드시 대응하는 반환 책임을 명시적으로 보유해야 합니다.

## 관련 문서

- [Pool Lease 사용하기](../../guides/utility/use-pool-lease.md)
- [소유권과 수명](../../concepts/ownership-and-lifetime.md)
- [Utility 모듈](../../../Runtime/유틸리티/README.md)
