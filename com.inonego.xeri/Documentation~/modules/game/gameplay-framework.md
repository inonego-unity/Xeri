# Gameplay Framework — 시스템 구조와 사용 계약

> **문서 기준:** 2026-11-09 UniXeri Core Activation 단순화 및 전체 작업 트리 EditMode 검증. **현행 API**와 **설계 목표·외부 통합 미검증**을 명확하게 구별합니다.
>
> 이 문서는 Xeri의 게임 Runtime을 **큰 구조 → 하위 시스템 → 한 번의 사용 → 종료/실패** 순서로 설명합니다. 전체 게임 전투 엔진이나 캐릭터별 스킬 카탈로그가 아닙니다.

## 목차

1. [왜 필요한가](#1-왜-필요한가)
2. [전체 아키텍처](#2-전체-아키텍처)
3. [Ability의 정의·상태·사용 선택](#3-ability의-정의상태사용-선택)
4. [Activation Cost와 Host 실행 승인](#4-activation-cost와-host-실행-승인)
5. [Execution·Spatial·Targeting·Delivery](#5-executionspatialtargetingdelivery)
6. [GameplayOperation·지속 결과의 수명](#6-gameplayoperation지속-결과의-수명)
7. [대표 연결 시나리오와 실패 조건](#7-대표-연결-시나리오와-실패-조건)
8. [현재 구현 범위와 확장 기준](#8-현재-구현-범위와-확장-기준)

## 1. 왜 필요한가

하나의 버튼이 여러 실행 중 하나를 선택하고, 공유 자원·상태를 소비하며, 실행이 끝난 뒤에도 공격이나 효과가 남는 게임에서는 **입력, 지속 상태, 실행, 적용 결과, 실제 Owner**를 분리해야 합니다.

- 사용 가능한 Ability를 해석하는 것과 이번 사용에서 실행할 행동을 선택하는 것은 다릅니다.
- `AbilityState`의 Cooldown·Charge·Values는 **지속 상태**입니다. `ExecutionSession`은 **한 번의 실행 수명**입니다.
- Ability 비용을 소비하는 `AbilityUseSelection.TryActivate()`와 실행 내부의 `ExecutionSession.TryCommit()`은 **같은 연산이 아닙니다**.
- 실제 HP·피해·반응·파티·동작 모션은 프로젝트 소유입니다. Xeri는 범용 접점과 수명 계약을 제공합니다.
- 새 공개 프레임워크 타입은 **현재 계약으로 해결할 수 없고 독립 소비자가 반복될 때만** 고려합니다.


## 2. 전체 아키텍처

### 2.1 가장 바깥쪽부터 본 소유 관계

```text
프로젝트 Gameplay Host (Player / AI / 외부 Rule)
├─ Game Owner: Party·HP·Mana·Input·AI·Animation·Motor
├─ Logical Ability Owner: Identity / 지속 State / Save / Tick
│   └─ Xeri AbilityBindings → 실제 AbilityInstance
├─ Xeri Activation
│   ├─ [목표] State + Context → 실행·Requirements·Costs 한 번 선택
│   └─ [현행] Requirement·Cost 전체 Evaluate → 순차 Pay → 선택 상태 전이
├─ Game Host 승인/배타 Slot (실제 선점 정책)
│   └─ Xeri ExecutionSession / Flow / Trigger / Behavior
│        ├─ Xeri Spatial → TargetSelection → Physics/Projectile Delivery
│        └─ Xeri GameplayOperation → 게임의 실제 Result/Rule
└─ Game Effect / World Object Owner (실행과 독립 가능)
    └─ Timer/Lease/Entity/Spawn·지속 소비·종료·늦은 Hit 출처
```

이 구조는 **논리적인 책임 지도**입니다. 각 상자가 별도 Xeri 공개 타입이나 하나의 전역 Manager라는 뜻이 아닙니다. 특히 게임 Actor와 지속 Ability Owner를 동일 인스턴스로 강제하지 않습니다.

### 2.2 하위 시스템 책임표

| 구역 | 입력 → 결과 | 실제 소유 | 제외할 책임 | 상태 |
|---|---|---|---|---|
| Identity·Binding | Entry → 현재 Ability | `AbilityBindings`와 논리 Owner | 과거 실행의 재바인딩 | 현행 기반 |
| Persistent State | Ability별 Cooldown/Charge/Values/Tick | `AbilityState`와 게임 logical Owner | Attack Animation·World 객체 목록 | 현행 기반, 통합 복원 미검증 |
| Use Choice | State + 요청 Context → 한 사용의 Execution/Requirement/Cost | `AbilityUseSelector/Choice/Selection` (Xeri Core) | 외부 Host가 선택 결과를 소비하고 실행 | **Xeri Core 구현 및 단독 패키지 EditMode 검증 / 외부 Host 연결 미검증** |
| Requirement/Cost | 조건·비용 전체 검사 → 비용 소비 | `AbilityActivationPolicy` + 실제 Resource Owner | 외부 비용의 원자성·지속 효과 수명 | 현행 기반, Fake Host 실패 검증 완료 |
| Host admission | 선택 결과 + Slot/기존 Action → 실행 승인 | **게임 Host** | 특정 Motor를 Core로 이관 | 게임 구현, 실패 계약 검증 |
| Execution | 승인된 단일 행동의 시간·Flow·Trigger | `ExecutionSession` | 영구 Resource State·파티 교체 | 현행 기반 |
| Spatial/Targeting | 위치 출처·시간·대상 선택 | SpatialSession/TargetSelection + Provider | 실제 피해/면역 | 현행 기반 |
| Delivery | 충돌/투사체 접촉·경과 | Physics/Projectile Delivery | 피해·Heal·처치 | 현행 기반 |
| Operation | Origin/Recipient → 게임 작업 호출 | `GameplayOperationRunner` + 게임 Rule | 실제 HP 변화량의 의미 | 현행 기반 |
| Persistent Effect/World | 실행 이후 결과의 Tick/End·회수 | **게임 Effect/World Owner** | 전역 EffectManager 강제 | 기존 도구 조합 검증 |
| Save/Restore | Owner 상태·Runtime 연결 재구성 | 프로젝트 Save/Owner | 모든 Session 직렬화 | 게임별 통합 미검증 |

### 2.3 경계가 다른 세 종류의 사용

| 이름 | 목적 | 실제 시스템 |
|---|---|---|
| 상호작용 `UseController` | 문·아이템·NPC 등의 `UseOffer` 중 후보를 선택하고 Signal 전달 | [Use System](use.md) |
| Ability Activation | 실제 Ability 사용 조건·비용·실행 후보 결정 | **이 문서의 Ability/Activation** |
| Execution Command/Signal | 시작된 실행의 후속 입력/신호 | `ExecutionSession`·`ExecutionFlow` |

`UseController.TryUse`는 Ability 사용 정책을 실행하는 `AbilityUseSelection.TryActivate()`와 별개입니다.

## 3. Ability의 정의·상태·사용 선택

### 3.1 누가 어떤 상태를 보관하는가

| 개념 | 뜻 | 소유 수명 |
|---|---|---|
| `AbilityEntryKey` | 입력·슬롯의 식별자(예: Q) | 프로젝트 입력/Binding |
| `AbilityKey`·Definition | 스킬의 정의를 식별 | 데이터/정의 |
| `AbilityInstance` | 실제 소유자에 연결된 Definition과 State | logical Ability Owner |
| `AbilityBindings` | 현재 Entry가 가리키는 Instance를 Layer로 해석 | Layer 등록/해제 Lease |
| `AbilityState` | Cooldown·선택적 Charge·Values 등 지속 값 | logical Ability Owner의 Tick·저장 |
| `ExecutionSession` | 한 번 시작된 행동 | 해당 실행의 Begin/End |

현행 `AbilityBindings`는 **높은 Priority, 같은 Priority면 나중에 등록된 Layer**를 우선합니다. Override Lease 해제 시 아래 Layer가 드러납니다. **현재 Binding을 바꾸어도 과거 Execution의 Identity/결과가 변경되면 안 됩니다.**

### 3.2 한 번의 사용 선택 — 목표 계약

```text
사용 요청(Entry, Request/Target/Mode Context)
  → 현재 Binding 해석(실제 Ability Identity)
  → AbilityState / Modifier / 외부 Context 읽기
  → 이번 Use Candidate를 무효과로 결정
       ├─ 선택된 Execution/Flow
       ├─ 이번에 적용할 Requirements
       ├─ 이번에 적용할 Costs
       └─ 승인 후 수행할 State 전이 규칙
  → 게임 Host Slot 승인(필요한 경우)
  → Selection.TryActivate()의 검사·비용 소비·상태 전이
  → 선택된 Execution 시작
```

**현행 Xeri 구현:** `AbilityActivation.cs`에는 `AbilityUseSelector<TDefinition,TProjectContext,TExecution>`, `AbilityUseChoice<...>`, `AbilityUseSelection<...>`가 있습니다. Selector는 규칙을 순서대로 평가해 첫 Choice를 선택합니다. Choice는 실행·Activation Policy·선택 현재성 조건·상태 전이를 묶습니다. Selection의 `TryActivate()`는 선택을 한 번만 사용하며, Requirement와 모든 Cost를 무효과로 평가한 다음 순차 소비하고 상태 전이를 호출합니다.

**현재 검증과 통합 경계:** 2026-11-09 Unity 6000.7.0a6 전체 작업 트리 EditMode는 **571 Total / 568 Passed / 0 Failed / 3 Skipped**이며, Ability Activation 20개·Charge Cost 9개·기반 Ability 30개를 포함합니다. Fake Host의 Slot 거부·Begin 예외·정상 실행 사례도 검증했습니다. 별도 게임 Host와 실제 Game Save/World의 통합 완료를 의미하지는 않습니다.

### 3.3 상태 선택의 불변식

- 선택 함수는 Cost Pay, Slot 선점, Actor 이동, Spawn 같은 **부작용을 일으키지 않습니다**.
- 실행·요구·비용·상태 전이는 **같은 선택의 입력 상태와 문맥**을 기준으로 정합니다.
- 선택과 실제 승인 사이 Binding/Resource/State가 바뀌면 재검증하거나 후보를 무효화해야 합니다.
- 후속 사용이 열려 있는 경우 일반 Cooldown이 이를 무조건 차단해서는 안 됩니다.
- 선택된 실행이 시작되면 그 실행은 이후 Binding 변화로 다시 선택되지 않습니다.

### 3.4 지속 상태와 실행 상태를 혼합하지 않는 이유

| 예시 | Persistent State | Execution Runtime |
|---|---|---|
| 공격 3회 뒤 강화 | 사용 가능 Stack/Stage | 현재 진행 중인 1~3타의 시간·명령 |
| Charge 2회 | 남은 사용 횟수·재충전 | 이번 공격의 발사 단계 |
| 무기 A ↔ B | 무기별 독립 Cooldown·Charge | A에서 시작한 기존 Projectile |
| Toggle | 유지 여부·실제 Resource Owner | Toggle On을 승인한 순간의 실행 |
| 개별 만료 Stack | 선택적 지속 항목·만료 Tick | 해당 스택을 획득/소비시킨 Hit 이벤트 |

개별 만료 Stack 상태는 **조건부 모델**입니다. 현행 `AbilityState.Values`·`AbilityChargeState`·Timer로 표현할 수 있으면 새 Stack Engine이 필요하지 않습니다.

## 4. Activation Cost와 Host 실행 승인

### 4.1 실제 코드에 존재하는 계약

현행 `AbilityActivationContext<TDefinition,TProjectContext>`는 Owner·Instigator·Binding·Project Context를 전달합니다. `IAbilityActivationRequirement.Evaluate()`와 `IAbilityActivationCost.Evaluate()`는 상태를 변경하지 않는 검사이고, `IAbilityActivationCost.Pay()`는 비용을 실제로 소비하는 명령입니다.

`AbilityActivationPolicy.TryActivate()`는 Requirement 전체를 먼저 평가한 다음 Cost 전체를 평가하며, 하나라도 거부되면 어떤 비용도 소비하지 않습니다. 모두 통과하면 Cost를 등록 순서대로 `Pay()`합니다. `AbilityUseSelection.TryActivate()`는 선택의 `IsCurrent`를 검사 전후에 확인하고 한 번만 사용한 뒤, 비용 소비 성공 시 선택의 상태 전이를 적용합니다. Ability Core는 비용 복원을 위한 별도 수명 객체를 제공하지 않습니다.

**책임과 한계:** Cost 검사와 소비 사이, 또는 서로 다른 Cost의 소비 사이에는 외부 상태 변경이나 콜백 예외가 발생할 수 있습니다. 소비가 시작된 후 예외가 나면 이미 적용된 비용은 자동 환급하지 않습니다. 같은 Charge 등 하나의 자원을 여러 Cost가 사용하는 경우에는 개별 검사만으로 합산 수량을 보장하지 않으므로, 해당 Resource Owner가 합산 검사·소비를 하나의 Cost로 제공해야 합니다. 서로 독립된 자원을 반드시 원자적으로 갱신해야 한다면 그 경계는 실제 Owner 또는 게임 Host가 구현해야 합니다.

### 4.2 서로 다른 세 가지 사건

| 사건 | 담당 | 되돌리기 위험 |
|---|---|---|
| Slot 선점·기존 실행 Revoke | 게임 Host | 이미 중단된 기존 행동은 원상복구 불가능할 수 있음 |
| Resource Cost Pay | `AbilityActivationPolicy` + 실제 Resource Owner | 모든 비용을 사전 검사하지만 Pay 도중의 예외·외부 변경은 자동 복구하지 않음 |
| Execution Begin | `ExecutionSession`과 Host | Observer·Spawn·Animation 등 외부 부작용이 발생할 수 있음 |

**목표:** Host는 먼저 가능성을 부작용 없이 평가하되, `CanBegin` 같은 사전 검사는 **실행 권한 예약이 아닙니다**. 검증된 Fake Host 통합에서는 Slot 획득 후 `TryActivate()`를 호출하고, 선택된 Execution을 시작합니다. Begin 실패 시 Host Slot·Session은 정리하지만 이미 소비한 Charge는 자동 복구하지 않습니다. 실제 게임 Host에서도 같은 실패 책임을 명시해야 합니다.

### 4.3 비용 검사·소비 실패와 사후 상태

- Requirement 또는 Cost의 `Evaluate()` 거부: **비용 소비 없이** 사용을 거부합니다. 검사는 순수해야 합니다.
- `Pay()` 예외: 원래 예외를 호출자에게 전달합니다. 이전 비용과 현재 비용 일부가 이미 적용됐을 수 있으므로 **자동 Rollback을 시도하지 않습니다**.
- 선택한 상태 전이 예외: 이전 비용은 그대로 유지합니다. 전이의 부분 적용 여부와 후속 처리는 게임 규칙이 판단합니다.
- 같은 Resource Owner의 복합 비용: 해당 Owner에서 합산 검사·소비를 하나의 명령으로 제공해야 합니다. 서로 다른 Owner의 전체 원자성은 Core의 보장 범위가 아닙니다.
- Host 시작 실패: 새 Session과 Slot의 상태는 Host가 정리하되, 이미 지급한 자원을 임의로 환급하지 않습니다. 필요할 경우 별도의 **명시적 게임 환급 정책**을 구현합니다.

이 방식은 실패 후 무조건 비용을 복구하겠다고 약속하는 복잡한 공통 Transaction을 제거한 것입니다. 실패한 비용 검사가 무소비라는 보장과, Pay 도중의 예외가 부분 적용을 남길 수 있다는 한계를 구별합니다.

### 4.4 세 수명의 구분

| 수명 | 답하는 질문 | 구현 경계 |
|---|---|---|
| Host Token/Slot | 지금 이 행동을 **실행할 권한**이 누구에게 있는가? | 게임 Host가 선점·검증·해제 |
| `ExecutionSession` | 이 행동이 **내부에서 무엇을 실행**하며 언제 종료되는가? | Xeri Execution |
| `Lease` | 특정 자원/연결을 해제할 **종료 책임**은 누구에게 있는가? | Xeri 공통 Lease + 실제 Owner |

이 셋을 같은 Coordinator로 합치지 않습니다. 특히 게임 Coordinator의 Motor·Root Motion·점프·착지 정책까지 Xeri Core에 옮기지 않습니다. 재사용 가능한 배타 Slot 권한이 여러 Host에서 필요하다는 증거가 나오면 그 책임만 별도 검토합니다.

### 4.5 승인 상태 변화의 관찰 기준

| 결과 | 확인해야 할 외부 상태 |
|---|---|
| Requirement 거부 | Cost·Slot·Session 시작 없음 |
| Cost Evaluate 거부 | 어떤 Cost도 Pay하지 않으며 새 Session이 시작되지 않았는지 확인 |
| Host 승인 거부 | Cost 확정 여부·기존 행동 상태·새 세션 유무를 명시 |
| Cost Pay 예외 | 이미 적용된 자원 소비와 Host Slot·Session 상태를 각각 확인; 자동 환급 없음 |
| Begin false/throw·Observer 예외 | 새 Session·Slot 정리, 이미 발생한 게임 효과와 이전 Action의 비가역 중단을 별도 기록 |
| 정상 종료·Interrupt | 현재 Token만 Release, 지속 객체는 인수한 Owner의 규칙으로 종료 |

## 5. Execution·Spatial·Targeting·Delivery

### 5.1 Execution은 하나의 행동만 소유한다

`ExecutionSession`은 Flow·Behavior·Trigger/Command/Signal·실행 시간·Child·End 수명을 보관합니다. `Begin()` 이후 `Advance()`를 진행하고, 종료 또는 취소 사유를 확정합니다. 한 실행에 사용한 `ValueRegistry` 변수는 다른 Session과 공유하지 않습니다.

- `ExecutionSession.TryCommit()`은 **그 실행 내부의 Commit**입니다. Ability Activation의 Resource Cost를 확정하지 않습니다.
- `Command()`는 Flow가 수락/거부를 판단하고, `PublishSignal()`은 Behavior에 신호를 전달합니다. 둘을 같은 입력 경로로 합치지 않습니다.
- `AttachChild()`로 붙은 실행은 부모 수명과 결합합니다. `DetachChild()`는 관계를 끊지만 **Child 실행을 자동으로 종료하지 않습니다**.
- 게임이 설치한 World Object나 지속 Effect는 모두 Child Execution이어야 하는 것이 아닙니다. 별도 Owner에 인수된 결과는 Session 종료 뒤에도 존속할 수 있습니다.

### 5.2 Spatial은 위치와 시각만 판단한다

| 계약 | 담당 | 게임이 제공해야 할 것 |
|---|---|---|
| `SpatialSource` / `SpatialRequest` | 어느 위치/참조를 어느 규칙으로 계산할지 | 실제 Transform·Target·원점 |
| `SpatialSession` / `SpatialSample` | 이번 실행의 좌표 포착·추적·고정 | Tick 시간, `ISpatialSourceProvider` |
| Capture / Follow / LockAtTime | 고정/매 평가/시각 도달 시 고정 | 게임 움직임·지형·Target 소실 정책 |
| TargetSelection | 적합한 대상의 후보·순위·Top N | 팀/적·면역·실제 Collider Resolver |
| Physics/Projectile Delivery | 비할당 접촉·접촉 시각·다중 결과·경로 진행 | 피해/회피·HP·중복 Hit 규칙 |

`SpatialSession`은 **과거 위치를 자동으로 복원하는 History Engine이 아닙니다**. 지연 판정이나 되감기가 필요한 게임은 자체 기록 Provider를 제공해야 합니다.

### 5.3 접촉과 실제 적용은 다른 사건이다

```text
Execution Trigger
  → Spatial Sample → Target Selection
  → Physics/Projectile Contact
  → Game Combat Rule (면역/회피/Shield·Damage)
  → GameplayOperation / 적용 완료 게임 Result
  → Game State Control / 후속 Effect
```

Contact 자체를 실제 피해나 Kill로 취급하지 않습니다. 물리 쿼리와 투사체 처리의 기존 계약을 새 Combat Engine으로 대체할 이유는 없습니다.

## 6. GameplayOperation·지속 결과의 수명

### 6.1 실제 적용 결과는 프로젝트가 결정한다

`GameplayOperationRunner.Invoke`는 `IGameplayOperation`을 호출하면서 Owner·Instigator·Origin·Recipient·Project Context를 전달합니다. 반환 타입 `GameplayOperationResult`의 `Applied`, `NoOp`, `Rejected`는 **Operation의 처리 상태**입니다.

| Operation 상태 | 여기서 알 수 있는 것 | 아직 알 수 없는 것 |
|---|---|---|
| Applied | 게임 Operation이 적용됨 | 실제 HP 감소량·Shield 흡수량·Kill 여부 |
| NoOp | 대상 상태 변화가 발생하지 않을 수 있음 | 실제 Hit/면역/회피의 게임별 의미 |
| Rejected | 프로젝트가 해당 요청을 거부 | Host 승인·자원 Cost까지 취소되었는지 |

이미 종료된 공격에서 늦게 발생한 Hit가 현재 사용 중인 다른 Ability에 잘못 연결되지 않도록, 필요한 원본 Ability/Execution/Result 식별자는 **게임 Result 또는 Operation Context에 보존**해야 합니다. Xeri에 전체 Combat Result 타입을 강제하지 않습니다.

### 6.2 외부 사건은 새 Activation이 아니다

```text
게임에서 적용 완료 사건 발생
  ├─ Hit / Kill / Recall / Heal / Shield Applied Result
  └─ 원래 출처와 Recipient 식별
       → 게임 Gameplay Rule에서 중복 적용 판단
       → 실제 AbilityState.Control/Resource Owner 변경
```

피해를 넣었다고 스킬을 다시 발동하는 식으로 `Activation`을 남용하지 않습니다. 실제 Result를 기준으로 Cooldown·Charge·Stack을 수정할 수 있으며, 어떤 사건에 몇 번 발동할지는 게임 정책입니다.

### 6.3 실행 종료와 지속 결과의 인수

| 결과 | 기본 수명 Owner | 종료 사건·검증 |
|---|---|---|
| Session 안에서만 동작하는 Trigger/Child | 부모 `ExecutionSession` | 부모 End/취소에 결합; Detach 여부 별도 |
| 설치된 폭탄·단검·덫 | 게임 `World Object`/Entity Owner | 기폭·회수·파괴·타임아웃; 원본 출처 유지 |
| 오프필드 소환·자동 공격 | 게임 Entity/Effect Owner | Actor 교체·소환자 소실·중복 Spawn 정책 |
| Target Buff·Debuff·Stack | 게임 Target Effect Owner | 대상 소실·중첩·만료·Dispel; 다른 대상과 독립 |
| 한시적 Form·Aura | 게임 Ability/Effect/Resource Owner 조합 | 유지 비용 부족·강제 종료·Actor 교체 |

**인수 경계:** 실행 과정에서 독립 결과를 생성할 때 **어느 실제 Owner가 받았는지 명시**합니다. 인수 뒤 Tick·End·장기 Reservation 해제는 해당 Owner가 담당하고, 사용을 승인한 Session이 끝났다는 이유만으로 무조건 제거하지 않습니다.

### 6.4 비용 세 가지의 수명 구분

| 구분 | 시점 | 올바른 소유자 |
|---|---|---|
| Activation Cost | 사용 승인 때 한 번 소비 | Cost Policy + 실제 게임 Resource Owner |
| Sustained Cost | Execution 또는 Effect가 유지될 동안 반복 지불 | 실행/Effect Owner + 게임 Resource Owner |
| Long-lived Reservation | 특정 Effect 생존 동안 가용량 일부 점유 | 게임 Resource Owner + Effect의 Handle |

사용 순간에 한 번 지급하는 Activation Cost와 장기간 자원을 점유하는 Reservation Handle은 **서로 다른 게임 Owner 계약**입니다. Resource의 실제 총량·가용량과 유지 종료 시점은 프로젝트가 정의합니다. Target·Owner·World가 같은 Tick에 소멸하더라도 End·구독 해제·예약 반환은 중복 수행하지 않아야 합니다.

### 6.5 저장·복원 경계

- `AbilityState` 등 지속 게임 상태는 **logical Owner**의 Save로 복원합니다.
- `ValueRegistry`는 Unity 직렬화 전 런타임 값을 저장 목록에 반영하고 역직렬화 후 복원합니다. AbilityState의 정수·문자열·실수·플래그와 진행 중 상태는 격리 Unity JSON 왕복 사례로 확인했습니다.
- `ExecutionSession`·일시적 Lease·Actor Token은 일반적으로 저장 데이터 원본이 아닙니다.
- World Object/Effect를 저장할지는 게임의 Save 요구에 달렸습니다. 저장해야 한다면 해당 객체 Owner가 데이터와 런타임 재구성을 맡습니다.
- Actor가 교체되어도 Ability Identity·Cooldown·Charge·오프필드 결과는 **실제 Owner 수명 정책에 따라** 유지됩니다.

## 7. 대표 연결 시나리오와 실패 조건

### 7.1 상태에 따른 실행 선택

```text
무기 A의 Entry Q → A Ability/State 선택
  → A에서 사용 승인 → A Execution 생성
  → 무기 B로 Binding 교체 → B의 새로운 Q 사용
  → 나중에 A의 Projectile 적중 → A의 원본 Ability/Result
  → 다시 A로 바인딩 → A의 기존 Cooldown 유지
```

| 성공 조건 | 실패 사례 |
|---|---|
| 무기 A/B의 지속 상태가 독립 | Binding 변경 때 A Cooldown이 B로 덮임 |
| 기존 실행은 시작 순간 Identity 보존 | 오래된 Projectile이 B Ability로 재해석됨 |
| 사용 후보 선택은 부작용이 없음 | 선택만으로 Resource·Slot 변경 |
| Host가 선택 결과를 재선택하지 않음 | 비용은 A 기준인데 Execution은 B 기준 |

### 7.2 지속 설치물과 별도 기폭

```text
Ability 실행 → 설치물 생성 → Game World Owner에 인수
  → 시전 Session 종료 → 설치물은 존속
  → 다른 Ability 또는 외부 사건으로 기폭·회수
  → World Object 종료 및 원본 Ability 출처로 Result 전달
```

확인할 것은 **객체 ID·Owner·중복 기폭·대상 소실·늦은 Hit의 출처·종료 한 번**입니다. 설치물의 개수 제한과 폭발 피해 계산은 게임 정책입니다.

### 7.3 승인 실패와 종료 경계

| 조건 | 요구하는 외부 사후조건 |
|---|---|
| Cost 전체 검사 중 하나가 거부 | 어떤 비용도 소비하지 않음; Slot/Session의 상태 별도 확인 |
| Pay 도중 일부 실패 | 이미 소비한 비용은 유지할 수 있음; 예외와 실제 Resource 최종값을 Host가 확인 |
| 기존 Action 선점 뒤 새 Action 실패 | 이전 Action의 비가역 종료와 새 실행의 정리를 구분 |
| Cost Pay 뒤 Begin 실패 | 새 Session·Slot은 Host가 정리하지만 지급한 비용은 자동 환급하지 않음. 별도의 환급 정책이 있다면 게임 Owner가 명시적으로 처리 |
| Revoke 도중 재진입 | 오래된 Token은 새 Slot을 Release할 수 없음 |
| Effect 만료와 Owner 소실 경합 | 한 번만 End·Lease/예약 해제 |

위 항목은 **요구되는 검증 사례**이며 현재 모두 실행·통과했다는 뜻은 아닙니다.

## 8. 현재 구현 범위와 확장 기준

Xeri의 기존 기능이 존재하는 것과, **여러 게임에서 Host까지 연결되어 올바르게 작동하는 것**은 다른 주장입니다.

| 범위 | 현재 판단 |
|---|---|
| AbilityState·AbilityBindings·Activation Policy/Selection | Xeri 코드 존재. Requirement·Cost의 Evaluate/Pay와 단일 TryActivate 계약 및 관련 테스트 검증 완료. 실제 게임 Host와 Resource Owner의 통합은 별도 근거 필요 |
| ExecutionSession·Spatial·Targeting·Delivery | 현행 기반 존재, Host·지연 Result 수명 연결 검증 필요 |
| State+Context 기반 단일 사용 후보 | `AbilityUseSelector/Choice/Selection` Xeri Core 구현 존재. 외부 Host 연결은 미검증. 현재 단독 패키지 Unity EditMode 검증은 통과 |
| 독립 Effect·World Object 수명 인수 | 실제 게임 Owner와 기존 Lease/Entity/Spawn 조합부터 검증 |
| 배타 Slot·시간제 Stack·장기 Resource Reservation 공통 모듈 | 재사용/현재 계약 부족을 확인한 뒤에만 추가 |

### 8.1 확장 결정을 내리는 순서

1. 새 기능의 실제 Owner와 발생·종료 사건을 확인합니다.
2. 현행 AbilityState·Bindings·ExecutionSession·Spatial·GameplayOperation·Lease·Entity로 구성해 봅니다.
3. 충족할 수 없는 **재현 가능한 불변식**을 명확히 기록합니다.
4. 다른 게임/Host에서도 같은 불변식이 반복될 때만 최소 공통 계약을 고려합니다.
5. 캐릭터별 피해·원소 반응·협주·파티·AI·Motor 등의 고유 정책은 게임에 남깁니다.

### 8.2 완료 기준과 미검증 구간

| 확인 지점 | 완료로 인정할 증거 |
|---|---|
| 사용 후보·비용 일관성 | 선택된 실행/Requirement/Cost가 같은 상태를 기준으로 승인됨 |
| Cost·Host·Begin 정합성 | 거부·예외·재진입·기존 Action 선점 후 명확한 최종 상태 |
| Actor/Binding 교체 | Ability별 지속 State와 과거 Execution/Hit 출처 유지 |
| 독립 Effect/World Object | Session 종료 이후 실제 Owner의 생존·중복 End/Handle 정리 |
| 프로젝트 Save/Unity | 실제 프로젝트의 Clone/Save/Restore와 관련 EditMode/PlayMode 결과 |

이 표의 “실제 프로젝트” 증거는 Xeri 패키지 자체 검증과 외부 Host 프로젝트 통합 검증을 구별합니다. Xeri 소스·테스트가 존재해도 외부 Host가 선택 결과를 사용하고 실패 경계를 충족하는지는 별도로 확인해야 합니다.

위 검증은 **목표 상태의 인수 조건**입니다. 구현 여부·검증 날짜·회귀 상태는 별도 작업 기록에서 확인해야 하며, 이 문서를 게시하는 것만으로 완료되지 않습니다.

### 관련 문서

- [Entity·Actor·Spawn 수명](entity-lifecycle.md)
- [Use System (상호작용 후보 선택)](use.md)
- [Reaction (Signal과 Action Target)](reaction.md)
- [소유권과 수명](../../concepts/ownership-and-lifetime.md)
- [프로젝트 통합 패턴](../../concepts/integration-patterns.md)
- [Xeri Game 개요](../../../Runtime/게임/README.md)
