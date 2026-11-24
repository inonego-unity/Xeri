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

Base를 변경하거나 modifier를 추가·제거하면 `MValue<T>`가 Modified를 다시 계산합니다.
`IModifier<T>`는 상태 변경 알림 계약을 포함하므로 modifier 내부 상태 변경도 자동으로 반영됩니다.
`LambdaModifier<T>`는 delegate가 참조하는 계산 의존성이 불변이라는 전제로 사용하며, 외부 mutable closure 변경은 지원하지 않습니다.

## 기본 Modifier

| Modifier | 값 | 연산 |
| --- | --- | --- |
| `NumericFModifier`, `NumericIModifier` | `float`, `int` | `SET`, `ADD`, `SUB`, `MUL`, `DIV`, `MIN`, `MAX` |
| `Vector2FModifier`, `Vector3FModifier`, `Vector4FModifier` | `Vector2`, `Vector3`, `Vector4` | `NumericFOperation`을 성분마다 적용 |
| `Vector2IModifier`, `Vector3IModifier` | `Vector2Int`, `Vector3Int` | `NumericIOperation`을 성분마다 적용 |
| `BooleanModifier`, `StringModifier` | `bool`, `string` | 논리 연산, `SET` |
| `LambdaModifier<T>` | 임의 | delegate |

연산 규칙은 `NumericFOperationHelper.Apply`, `NumericIOperationHelper.Apply`에 있어 Modifier 객체 없이도 같은 규칙을 쓸 수 있습니다. `DIV`의 0은 입력 값을 유지하고, float의 `MIN`·`MAX`는 NaN인 피연산자를 무시합니다. 벡터의 `MUL`은 성분별 곱이므로 스칼라 배율은 `(s, s, s)`로 표현하며, `MIN`·`MAX`도 성분별 비교라 벡터 크기 제한이 아닙니다.

`IModifier<T>.Replaces`는 결과가 입력과 무관한지(`SET`처럼 앞 단계 결과를 덮어쓰는지) 알려 줍니다. `LambdaModifier<T>`는 알 수 없으므로 항상 `false`입니다.

## Modifier 순서

`MValue<T>`는 modifier를 `order` 오름차순으로 적용합니다.
각 modifier는 `IModifier<T>.Modify(T value)`로 이전 단계 결과를 받아 다음 값을 반환합니다.
동일 modifier의 의미적 식별이 필요하면 문자열 key 또는 `IKeyable<string>`을 사용합니다.
`IReadOnlyMValue<T>.Modifiers`는 `IReadOnlyXOrdered<int, string, IModifier<T>>`로 노출되며 등록의 Order, Key, Value를 읽을 수 있습니다.
동일한 Modifier 인스턴스를 여러 Key로 등록해도 `MValue<T>`는 해당 인스턴스의 `OnChange`를 한 번만 구독합니다.

## 이벤트

- `OnBaseChange`: 실제 Base 값이 바뀔 때 발생합니다.
- `OnModifiedChange`: modifier 적용 결과가 바뀔 때 발생합니다.
- 변경 메서드의 `invokeEvent: false`는 외부 이벤트만 억제하며 Modified 캐시는 즉시 갱신합니다. 단, [묶음 범위](#묶음-갱신) 안에서는 재계산이 범위 종료로 미뤄집니다.

## 묶음 갱신

여러 Modifier 값이나 Base를 한꺼번에 바꿀 때는 `MValue<T>.BeginBatch()` 범위 안에서 바꿉니다. 범위 안의 변경은 재계산과 이벤트를 미뤘다가 가장 바깥 범위가 닫힐 때 한 번 처리합니다.

```csharp
using (speed.BeginBatch())
{
    slow.Value = 0.5f;
    haste.Value = 1.2f;
    speed.Set(baseSpeed);
}
```

- 범위는 중첩할 수 있으며 `using`으로 연 순서의 역순으로 한 번씩 닫아야 합니다. 이미 닫힌 범위를 다시 닫으면 상태를 바꾸지 않고, 바깥 범위를 먼저 닫으면 안쪽 범위까지 닫아 처리한 뒤 `InvalidOperationException`을 던집니다.
- 범위 안에서 `Modified`는 범위를 연 시점의 값입니다.
- 닫힐 때 범위를 연 시점의 값과 비교하며, 이벤트를 요청한 변경 종류(Base·Modified)만 한 번 통지합니다. 범위 안에서 바꿨다가 되돌린 값은 통지하지 않습니다.
- 범위 객체는 `ref struct`라 할당이 없고 필드에 보관할 수 없습니다.

## 복제와 상태 복원

`Value<T>`, `RangeValue<T>`, `MValue<T>`와 built-in Modifier는 Xeri 깊은 복제 계약(`IDeepCloneable`)을 구현합니다. 미리 만든 인스턴스에 `CloneFrom`으로 담아 두었다가 다시 `CloneFrom`으로 되돌리면 상태를 저장·복원할 수 있습니다.

```csharp
var snapshot = new MValue<float>();
snapshot.CloneFrom(speed);   // 저장

// ... 상태 변경 ...

speed.CloneFrom(snapshot);   // 복원
```

- `CloneFrom`은 상태 복사이며 상태 전이가 아니므로 `ProcessBase`를 거치지 않고 외부 이벤트를 발행하지 않습니다. 내부 동기화와 Modified 갱신 hook은 수행합니다.
- 같은 구체 타입끼리만 복제할 수 있으며 다른 타입이면 `ArgumentException`입니다.
- `RangeValue<T>`는 Range를 먼저 복원한 뒤 Base를 복원하므로 이전 범위로 값이 잘리지 않습니다.
- `MValue<T>`는 키·Order 구성과 구체 타입이 같으면 목록을 그대로 두고 각 Modifier 인스턴스에 상태만 복사합니다(할당 없음). 같은 구성으로 반복 저장·복원하는 롤백은 이 경로를 탑니다. 구성이 다르면 목록을 다시 만들며, 같은 키·같은 타입 인스턴스는 재사용하고 나머지는 새로 복제합니다(목록 항목과 새 Modifier 할당 발생). 복제본은 원본과 Modifier 인스턴스를 공유하지 않으므로 여러 키·여러 MValue 사이의 인스턴스 공유 관계는 복제되지 않습니다.
- 파생 타입은 상태를 추가하지 않아도 `@new()`를 재정의해 자기 타입을 만들어야 합니다. 재정의하지 않으면 `Clone()`이 타입 불일치(`ArgumentException`)로 실패합니다. 기존 Modifier를 상속할 때도 같습니다.
- 상태를 추가한 파생 값 타입은 `CloneState()`를, 상태를 추가한 Modifier는 `CloneFrom()`도 재정의해야 복제가 완전합니다. Modifier의 `CloneFrom`은 상태가 바뀌면 `OnChange`를 발행합니다.
- `MValue<T>`의 Modifier 목록 복제는 `XOrdered.CloneFrom`이 맡으며, `MValue<T>`는 Base 복사 시점과 구독·`Modified` 재계산을 맡습니다.
- `MValue<T>.CloneFrom`은 새 Modifier 복제(`Clone`)가 실패하면 대상 MValue를 바꾸지 않고 예외를 전달합니다. 기존 Modifier 인스턴스의 `CloneFrom`이 실패해도 나머지를 끝까지 복사하고 Modifier 목록을 source에 맞춘 뒤(실패한 인스턴스는 이전 상태로 남음), 구독과 `Modified`를 그 상태에 맞추고 예외를 전달합니다.
- `Clone()`은 `IDeepCloneable<Value<T>>`의 기본 구현이라 `Value<T>` 형식으로 호출하고 결과를 원래 타입으로 변환합니다. 예: `var copy = (MValue<float>)((Value<float>)speed).Clone();` 반복 저장·복원에는 미리 만든 인스턴스와 `CloneFrom`을 쓰는 편이 할당이 없습니다.

## 직렬화

`Value<T>`, `MValue<T>`와 built-in Modifier는 Unity 직렬화 가능한 상태를 보관합니다.
`MValue<T>`의 `Modified` 캐시와 Modifier 이벤트 구독은 runtime derived state이며, 역직렬화 후 Base와 Modifier 상태에서 다시 구성합니다.

## 확장 지점

- 값 입력 전처리: `Value<T>.ProcessBase()` 재정의
- 새 modifier: `IModifier<T>` 구현. 연산 종류와 피연산자를 가진 Modifier는 `OperationModifier<T, TOperation>`을 상속하면 변경 알림과 복제를 그대로 씁니다
- 파생 값 타입의 상태 복제: `@new()`, `CloneState()` 재정의
- 읽기 전용 외부 노출: `IReadOnlyValue<T>`, `IReadOnlyMValue<T>`
- 변경 가능한 시스템 내부 노출: `IValue<T>`, `IMValue<T>`

## 제약과 주의사항

- Base와 Modified를 같은 의미의 값으로 취급하지 않습니다.
- modifier 적용 순서가 결과에 영향을 주면 `order`를 명시합니다.
- `IModifier<T>.Modify()`는 Base와 Modifier 상태만으로 결과를 계산하는 순수 값 변환으로 사용합니다.
- UI 표시를 위해 Value가 직접 View를 참조하게 만들지 않습니다.

## 관련 문서

- [Xeri Serializable](../../../Runtime/Serializable/README.md)
- [소유권과 수명](../../concepts/ownership-and-lifetime.md)
- [확장 계약](../../concepts/extension-contracts.md)
