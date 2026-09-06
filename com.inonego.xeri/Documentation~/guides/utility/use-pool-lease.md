# Pool Lease 사용하기

이 가이드는 Xeri Pool에서 일반 소비자가 `Lease<T>`로 획득과 반환 책임을 함께 관리하는 방식을 설명합니다.

## 목적

반환 누락과 오래된 반환 핸들이 새 획득을 건드리는 문제를 줄이고, 소비자 수명과 Pool 반환 시점을 같은 코드 경계에 둡니다.

## 1. 재사용 타입 준비

`Pool<T>`는 `class, new()` 타입을 기본 생성자로 만듭니다.

```csharp
public sealed class ReusableItem
{
    public int Value { get; set; }

    public void Reset()
    {
        Value = 0;
    }
}
```

## 2. Pool 생성

```csharp
var pool = new Pool<ReusableItem>();
```

대기 항목이 없으면 `new T()`로 새 항목을 생성합니다.
## 3. Lease로 획득

```csharp
using (Lease<ReusableItem> lease = pool.AcquireLease())
{
    ReusableItem item = lease.Value;
    item.Value = 10;

    Use(item);
}
```

`Dispose()` 시점에 해당 acquisition generation이 아직 유효하면 Pool로 반환됩니다.

## 4. 직접 Acquire가 필요한 경우

소유권을 다른 객체로 넘기거나 Lease보다 긴 수명을 직접 관리해야 하면 `Acquire()`를 사용할 수 있습니다.

```csharp
ReusableItem item = pool.Acquire();

try
{
    Use(item);
}
finally
{
    item.Reset();
    pool.Release(item);
}
```

직접 획득을 선택한 호출자가 반환 책임도 직접 보유합니다.
## 오래된 Lease와 generation

Pool은 acquisition generation을 추적합니다. 항목을 직접 반환하고 다시 획득한 뒤 예전 Lease를 Dispose해도 새 acquisition을 반환하지 않습니다.

이 계약 때문에 Lease를 소비자에게 안전하게 전달할 수 있습니다.

## 종료 시 전체 반환

Pool 소유자 자체가 종료될 때는 `ReleaseAll()`로 현재 Acquired 항목 전체의 반환을 시도할 수 있습니다. 한 항목 실패가 다른 초기 항목 반환을 막지 않으며 오류는 마지막에 집계됩니다.

## 주의사항

- Xeri Pool은 thread-safe하지 않습니다.
- 직접 `Acquire()`와 Lease 획득 방식을 섞을 때 누가 반환 책임을 갖는지 명시합니다.
- Pool item의 프로젝트별 reset/bind 정책은 파생 Pool이나 상위 Adapter에서 처리합니다.

## 관련 문서

- [Object Pooling](../../modules/utility/pooling.md)
- [소유권과 수명](../../concepts/ownership-and-lifetime.md)