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
## 여러 변경을 한 번에 반영하기

한 frame에 여러 Modifier가 바뀌는 시스템은 각 조작에서 이벤트를 발행하지 않고 마지막에 `Refresh()`로 최종 값을 한 번 확정할 수 있습니다.

```csharp
scale.AddModifier("effect.a", modifierA, 100, invokeEvent: false);
scale.AddModifier("effect.b", modifierB, 200, invokeEvent: false);
scale.Refresh();
```

이미 등록된 Modifier 객체의 내부 값이 바뀌었다면 `MValue`는 그 변경을 자동 감지하지 않습니다. 최종 사용 경계에서 `Refresh()`를 호출합니다.

## 최종 값의 단일 작성자 두기

`MValue`는 값을 계산할 뿐 Unity 전역 상태나 Component property를 직접 바꾸지 않습니다. 한 Controller가 `Modified`를 실제 대상에 적용하도록 두면 여러 효과의 쓰기 충돌을 줄일 수 있습니다.

```csharp
private void LateUpdate()
{
    scale.Refresh();
    target.speed = scale.Modified;
}
```

## 관련 문서

- [Serializable Value와 Modifier](../../modules/serialization/value.md)
- [소유권과 수명](../../concepts/ownership-and-lifetime.md)