# Core Primitive

Xeri Primitive는 Unity 환경에서 여러 Runtime 시스템이 공유하는 수치·범위·접근자 기반 값 계약을 제공합니다.

## 왜 필요한가

HP, RangeValue처럼 값 타입을 제네릭으로 다루는 시스템이 `int`와 `float`마다 별도 구현을 가지면 산술·비교·Clamp 규칙이 중복됩니다. Primitive는 Unity 프로젝트에서 사용할 수 있는 공통 수치 계약과 직렬화 가능한 범위 표현을 제공합니다.

## 언제 사용하는가

- Xeri의 제네릭 수치 시스템을 확장할 새로운 numeric wrapper가 필요할 때
- `Min ≤ Max` 불변 조건을 가진 직렬화 가능한 범위를 보관할 때
- 값 getter/setter 계약을 여러 시스템에서 공통으로 사용해야 할 때

일반 프로젝트 코드에서 단순 `float` 계산만 하는 경우에는 Primitive 타입을 억지로 감쌀 필요가 없습니다.

## 기본 사용

```csharp
var range = new Range<float>(0f, 1f);
float clamped = range.Clamp(1.5f); // 1.0

XNumericF value = 2f;
XNumericF doubled = value.Mul(2f);
```

## INumeric

`INumeric<TSelf, TValue>`는 Unity에서 사용할 수 없는 최신 C# generic math 의존 없이 제네릭 수치 연산을 표현하기 위한 계약입니다.

```text
산술 Add/Sub/Mul/Div/Mod
비교 IEquatable / IComparable
부호 IsPositive / IsNegative / IsZero
범위 Min / Max / Clamp
변환 ToFloat / FromFloat
원시 값 Get / Set
```

CRTP 형태의 `TSelf`를 사용해 연산 결과 타입을 동일한 수치 wrapper로 유지합니다.

현재 `XNumericF`와 `XNumericI`가 float/int 기반 구현을 제공합니다.

## Range

`Range<T>`는 `Min ≤ Max` 불변 조건을 강제하는 직렬화 가능한 값 타입입니다.

- `Clamp(value)`
- `Includes(value)`
- `Encloses(other)`
- `Begin` / `End`는 `Min` / `Max`의 도메인 별칭

잘못된 Min/Max 순서는 생성자와 setter에서 즉시 거부합니다.
## 사용 경계

Primitive는 도메인 상태를 소유하지 않습니다. HP가 `INumeric`을 통해 int/float 구현 차이를 숨기거나 `RangeValue`가 공통 Range 계약을 사용하는 식으로 상위 시스템의 기반 타입으로 사용합니다.

새 도메인 타입이 단순 산술 wrapper가 아니라 lifecycle과 정책을 갖기 시작하면 Primitive에 넣지 않습니다.

## 관련 문서

- [HP](../game/hp.md)
- [Serializable Value와 Modifier](../serialization/value.md)
- [Xeri Core](../../../Runtime/Core/README.md)
