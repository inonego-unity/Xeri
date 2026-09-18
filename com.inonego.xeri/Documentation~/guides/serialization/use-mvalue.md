# MValue로 Modifier 합성하기

`MValue<T>`는 원본 `Base` 값과 여러 Modifier를 분리하고, Order 순서로 합성한 `Modified` 값을 제공합니다. 버프, 배율, 임시 효과처럼 여러 시스템이 하나의 최종 값을 함께 만드는 경우에 사용합니다.

## 목적

각 기능이 최종 값을 직접 덮어쓰지 않고 자신의 Modifier만 추가·제거하게 하며, 한 Controller가 계산된 `Modified`를 실제 대상에 적용하는 구조를 만듭니다.

## 왜 Base와 Modified를 나누는가

각 기능이 최종 값을 직접 덮어쓰면 어떤 효과를 먼저 제거했는지에 따라 값 복원이 꼬일 수 있습니다.

```text
Base = 1.0
  ↓
Modifier A × 0.5
  ↓
Modifier B × 0.8
  ↓
Modified = 0.4
```

각 효과는 자신의 Modifier만 추가·제거하고 최종 값 계산은 `MValue`가 담당합니다.

## 기본 사용

```csharp
using inonego.Xeri.Serializable;

var scale = new MValue<float>(1.0f);

scale.AddModifier
(
    "slow",
    new NumericFModifier(NumericFOperation.MUL, 0.5f),
    order: 100
);

float current = scale.Modified; // 0.5

scale.RemoveModifier("slow");
current = scale.Modified; // 1.0
```

Key는 Modifier의 소유자를 구분할 수 있는 안정적인 값을 사용합니다.
등록 목록은 `scale.Modifiers`를 통해 `IReadOnlyXOrdered<int, string, IModifier<float>>`로 읽을 수 있으며, Order·Key·Value를 조회할 수 있지만 외부에서 목록 자체를 변경할 수는 없습니다.

## Modifier 내부 상태 변경

`IModifier<T>`는 상태 변경 알림 계약을 포함합니다. 등록된 Modifier의 `Operation` 또는 `Value`가 실제로 변경되면 `MValue`가 자동으로 `Modified`를 다시 계산합니다.

```csharp
var slow = new NumericFModifier(NumericFOperation.MUL, 0.5f);

scale.AddModifier("slow", slow, 100);
slow.Value = 0.25f;

float current = scale.Modified; // 자동으로 0.25
```

재계산 결과가 이전 `Modified`와 같으면 `OnModifiedChange`는 발생하지 않습니다.
동일한 Modifier 인스턴스를 여러 Key로 등록해도 `MValue`는 해당 인스턴스의 `OnChange`를 한 번만 구독하며, 마지막 등록이 제거될 때 구독을 해제합니다.

`LambdaModifier<T>`는 생성 시 전달한 delegate를 이후 변경하지 않는 런타임 전용 Modifier입니다. Lambda가 참조하는 계산 의존성도 등록 후 불변이어야 하며, 외부 mutable closure를 변경하는 사용 방식은 지원하지 않습니다.

`invokeEvent: false`는 해당 조작에서 이벤트만 억제하고 `Modified` 캐시는 즉시 갱신합니다.

## 최종 값의 단일 작성자 두기

`MValue`는 값을 계산할 뿐 Unity 전역 상태나 Component property를 직접 바꾸지 않습니다. 한 Controller가 `Modified`를 실제 대상에 적용하도록 두면 여러 효과의 쓰기 충돌을 줄일 수 있습니다.

```csharp
private void LateUpdate()
{
    target.speed = scale.Modified;
}
```

## 관련 문서

- [Serializable Value와 Modifier](../../modules/serialization/value.md)
- [소유권과 수명](../../concepts/ownership-and-lifetime.md)