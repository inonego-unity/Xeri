# Serializable Value와 Modifier

Xeri Serializable Value는 Unity 직렬화가 가능한 값 컨테이너와 변경 이벤트, 수정자 기반 파생 값을 제공합니다.
단순 값은 `Value<T>`, modifier가 적용되는 값은 `MValue<T>`, 범위 제한 값은 `RangeValue<T>` 계열로 표현합니다.

## 왜 필요한가

여러 기능이 하나의 최종 값을 직접 덮어쓰면 효과 제거 순서에 따라 원래 값을 복원하기 어렵습니다. `MValue<T>`는 원본 `Base`와 Modifier 목록, 계산된 `Modified`를 분리해서 각 기능이 자기 Modifier만 소유하도록 합니다.

## 언제 사용하는가

- 버프·디버프·배율처럼 여러 효과가 하나의 값을 합성할 때
- 원본 값과 계산된 최종 값을 모두 관찰해야 할 때
- 값 변경 이벤트와 Unity 직렬화를 함께 사용해야 할 때
- 범위 제한 자체를 직렬화 가능한 상태로 보관하고 싶을 때

단일 필드에 단순 대입만 필요하면 일반 값이나 `Value<T>`가 더 적합합니다.

## 기본 사용

```csharp
var speed = new MValue<float>(1f);
speed.AddModifier
(
    "slow",
    new NumericFModifier(NumericFOperation.MUL, 0.5f),
    order: 100
);

float finalSpeed = speed.Modified;
speed.RemoveModifier("slow");
```

실제 외부 대상에는 별도 Controller가 `Modified`를 적용하는 단일 작성자 역할을 맡는 편이 좋습니다. 자세한 패턴은 [MValue로 Modifier 합성하기](../../guides/serialization/use-mvalue.md)를 참고합니다.

## 핵심 모델

```text
Value<T>
  Base
  OnBaseChange

MValue<T>
  Base
    ↓ modifier(order 순)
  Modified
  OnModifiedChange
```

`Value<T>.Set()`은 `ProcessBase()` 훅을 거친 최종 값을 비교한 뒤 실제 변경일 때만 `OnBaseChange`를 발행합니다.
`MValue<T>`는 Base와 modifier 목록을 입력으로 `Modified` 캐시를 계산합니다.

## Base와 Modified

`Base`는 원래 값이고 `Modified`는 현재 modifier를 모두 적용한 결과입니다.
`MValue<T>`는 `IReadOnlyMValue<T>`를 통해 외부에 Base, Modified, modifier 목록과 변경 이벤트를 읽기 전용으로 노출할 수 있습니다.

```csharp
var value = new MValue<float>(100f);
value.AddModifier("buff", modifier, order: 10);

float baseValue = value.Base;
float result = value.Modified;
```

Base를 변경하거나 modifier를 추가·제거하면 `Refresh()`가 Modified를 다시 계산합니다.
modifier 내부 상태만 외부에서 바뀌었다면 해당 소유자가 `Refresh()`를 명시적으로 호출해야 합니다.

## Modifier 순서

`MValue<T>`는 modifier를 `order` 오름차순으로 적용합니다.
각 modifier는 `IModifier<T>.Modify(T value)`로 이전 단계 결과를 받아 다음 값을 반환합니다.
동일 modifier의 의미적 식별이 필요하면 문자열 key 또는 `IKeyable<string>`을 사용합니다.
## 이벤트와 강제 갱신

- `OnBaseChange`: 실제 Base 값이 바뀔 때 발생합니다.
- `OnModifiedChange`: modifier 적용 결과가 바뀔 때 발생합니다.
- `InvokeOnBaseChange()`와 `InvokeOnModifiedChange()`는 Undo 복원처럼 backing field가 이미 바뀐 상태에서 이벤트만 다시 전달해야 할 때 사용합니다.

일반 값 변경에서 강제 이벤트 API를 기본 경로로 사용하지 않습니다.

## 복제와 직렬화

`Value<T>`와 `MValue<T>`는 Xeri 깊은 복제 계약을 구현합니다.
`IModifier<T>`도 `IDeepCloneable<IModifier<T>>`를 요구하므로 modifier를 포함한 값 그래프를 독립적으로 복제할 수 있는 구조를 전제로 합니다.

## 확장 지점

- 값 입력 전처리: `Value<T>.ProcessBase()` 재정의
- 새 modifier: `IModifier<T>` 구현
- 읽기 전용 외부 노출: `IReadOnlyValue<T>`, `IReadOnlyMValue<T>`
- 변경 가능한 시스템 내부 노출: `IValue<T>`, `IMValue<T>`

## 제약과 주의사항

- Base와 Modified를 같은 의미의 값으로 취급하지 않습니다.
- modifier 적용 순서가 결과에 영향을 주면 `order`를 명시합니다.
- modifier의 내부 상태가 바뀌었는데 목록 자체는 바뀌지 않았다면 `Refresh()` 책임을 놓치지 않습니다.
- UI 표시를 위해 Value가 직접 View를 참조하게 만들지 않습니다.

## 관련 문서

- [Xeri Serializable](../../../Runtime/Serializable/README.md)
- [소유권과 수명](../../concepts/ownership-and-lifetime.md)
- [확장 계약](../../concepts/extension-contracts.md)
