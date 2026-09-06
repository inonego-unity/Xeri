# HP

Xeri HP는 int/float 계열 값을 공통 `INumeric` 계약으로 다루는 체력 모델입니다. 현재 값, 최대값, 생존 상태와 Heal/Damage 이벤트를 하나의 상태 경계로 관리합니다.

## 왜 필요한가

체력 값과 생존 상태를 별도 필드로 관리하면 `HP == 0`인데 Alive인 상태처럼 모순이 생길 수 있습니다. Xeri HP는 값 범위, Alive/Dead 전이, Heal/Damage 이벤트를 같은 모델에서 처리해 상위 Entity나 UI가 하나의 일관된 상태를 관찰하게 합니다.

## 언제 사용하는가

- 현재/최대 체력과 생존 상태를 함께 관리할 때
- damage/heal 이벤트와 값 변경 이벤트를 구분해 소비해야 할 때
- int와 float HP 구현을 같은 계약으로 다루고 싶을 때

체력이 단순 표시값이고 Alive/Dead lifecycle과 연결되지 않는다면 일반 `Value<T>`가 더 단순할 수 있습니다.

## 기본 사용

```csharp
var hp = new HP_I();
hp.MaxValue = 100;
hp.MakeAlive();

hp.ApplyDamage(25);
float ratio = hp.Ratio;

hp.ApplyHeal(10);
```

`EntityBase`와 함께 사용하면 HP가 Dead로 전환될 때 Registry Despawn 요청으로 연결됩니다.

## 상태 모델

```text
Value > 0  ↔ Alive
Value == 0 ↔ Dead
```

`Value`가 0 경계를 넘으면 상태가 자동으로 전환됩니다. `MakeAlive()`와 `MakeDead()`를 직접 호출하면 기본적으로 값도 각각 `MaxValue`와 0으로 맞춥니다.

## 값과 최대값

- `Value`: 항상 0~`MaxValue` 범위로 clamp
- `MaxValue`: 항상 0 이상
- `Ratio`: `Value / MaxValue`, MaxValue가 0이면 0

MaxValue를 현재 Value보다 작게 낮추면 Value도 새 최대값으로 조정됩니다.

## Heal과 Damage

`ApplyHeal()`과 `ApplyDamage()`는 Dead 상태에서는 동작하지 않습니다. 음수와 0 amount도 적용하지 않습니다.

```text
ApplyDamage
→ Value 감소
→ 0 도달 시 Dead 전환
→ OnDamage
```

`CalculateApplyAmount()`는 현재값, 최대값, 손실값 중 하나를 기준으로 비율 amount를 계산합니다.
## 이벤트

- `OnValueChange`
- `OnMaxValueChange`
- `OnStateChange`
- `OnHeal`
- `OnDamage`

상태 자동 전환과 값 변경 이벤트는 같은 변경에서 함께 발생할 수 있으므로 구독자는 각각 독립된 의미로 처리합니다.

## Entity 연동

`EntityBase`는 Spawned 상태에서 HP 상태 변경을 구독하고 `Dead`로 전환되면 `DespawnReason.Dead`로 Registry에 디스폰을 요청합니다.

따라서 Entity에서 HP를 사용할 때는 HP의 생존 상태와 Entity Spawn lifecycle을 별도 상태로 취급하되, 사망 전환 시 Registry 소유권 해제로 연결된다는 점을 고려합니다.

## 책임 범위

HP는 값·상태·적용 이벤트만 소유합니다. 방어력, 피해 타입, 무적 시간, 공격 판정과 같은 전투 정책은 consumer가 Damage amount를 계산하기 전에 처리합니다.

## 관련 문서

- [Entity와 Spawn 수명](entity-lifecycle.md)
- [Xeri Game](../../../Runtime/게임/README.md)
