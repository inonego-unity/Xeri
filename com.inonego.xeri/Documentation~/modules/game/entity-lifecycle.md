# Entity와 Spawn 수명

Xeri Entity 시스템은 게임 객체의 식별, HP·Group 상태와 Spawn Registry의 소유 관계를 분리해 관리합니다.
`EntitySpawnRegistry`가 Key 부여와 등록을 소유하고, `EntityBase`는 Registry에 연결된 동안 필요한 내부 runtime 연결을 구성합니다.

## 왜 필요한가

Entity 생성 위치마다 Key 발급, 현재 Spawn 상태, Despawn 정리와 조회 규칙을 직접 구현하면 동일 객체가 여러 Registry에 섞이거나 실패한 Spawn이 반쯤 등록된 상태로 남기 쉽습니다. Xeri는 이 lifecycle을 Registry 계약으로 고정하고, 프로젝트는 구체 객체를 어디서 가져오고 어디로 반환할지만 결정하게 합니다.

## 언제 사용하는가

- Runtime Entity에 안정적인 Key와 중앙 조회가 필요할 때
- Spawn/Despawn 이벤트와 rollback 경계를 한곳에서 관리하고 싶을 때
- 논리 Entity와 View/Presentation 수명을 분리하고 싶을 때
- HP 사망 같은 상태 변화가 Registry lifecycle과 연결되어야 할 때

단순히 Scene에 고정된 MonoBehaviour를 몇 개 참조하는 정도라면 별도 Entity Registry가 필요하지 않을 수 있습니다.

## 기본 사용

프로젝트는 `EntityBase` 파생 타입과 `EntitySpawnRegistry<TEntity>` 파생 Registry를 만들고 `Acquire`/`Release`만 구현합니다. Key와 `SpawnState`는 Registry가 소유합니다.

```csharp
var registry = new SampleEntityRegistry();

if (registry.TrySpawn(out SampleEntity entity))
{
    ulong key = entity.Key;
    entity.Despawn();
}
```

구체 구현 절차는 [Entity Registry 구현하기](../../guides/game/create-entity-registry.md)를 참고합니다.

## 핵심 모델

```text
Acquire
  ↓
Key 부여
  ↓
Spawning
  ↓
Registry 등록 + runtime 연결
  ↓
Spawned
  ↓
게임 사용
  ↓
Despawning
  ↓
등록 해제 + runtime 연결 정리
  ↓
Despawned + Key 해제
```

## 주요 계약

| 계약 | 역할 |
|---|---|
| `IReadOnlyEntity` | 외부에 Key, HP, Group, SpawnState를 읽기 전용 노출 |
| `IEntity` | Registry 내부에서 Key와 runtime 연결을 관리 |
| `ISpawnRegistry<TKey,T>` | 등록 객체 조회와 lifecycle 이벤트 |
| `EntitySpawnRegistry` | Entity용 `ulong` Key 생성과 Spawn 흐름 |
| `EntityBase` | HP 연결과 Entity lifecycle 기본 구현 |
| `DespawnReason` | 종료 원인 전달 |

## Key와 Registry 소유권

Entity Key는 Entity가 임의로 생성하지 않습니다.
`EntitySpawnRegistryBase`가 `IKeyGenerator<ulong>`으로 Key를 만들고 Despawned Entity에 부여합니다.

Spawn이 공통 Registry 흐름에 진입하기 전에 실패하면 이미 부여한 Key를 제거하고, 아직 Registry가 소유하지 않은 객체는 획득 출처로 직접 반환합니다.
## Spawn lifecycle

Registry의 공개 이벤트는 다음 단계에 대응합니다.

1. `OnSpawning`: 객체가 Spawning 상태에 들어갔지만 아직 등록 전입니다.
2. `OnSpawned`: Registry 등록과 Spawned 상태 전환이 완료됐습니다.
3. `OnDespawning`: 등록 해제 또는 Spawn 실패 정리가 시작됩니다.
4. `OnDespawned`: 등록 해제와 상태 전환이 끝났습니다.

`EntityBase.OnRegistrationAttached()`는 Spawned 소유 관계가 확정될 때 HP runtime 연결을 구성합니다.
기본 구현은 HP가 살아 있는지 확인하고 상태 변경 이벤트를 구독합니다.

HP가 `Dead`로 전환되면 Entity는 현재 Registry에 `DespawnReason.Dead`를 요청합니다.
다른 파생 훅이 먼저 디스폰했다면 해당 결정을 보존합니다.

## Despawn과 정리

`IDespawnable.Despawn()` 확장은 현재 Registry가 연결한 callback을 통해 디스폰을 요청합니다.
Registry에 등록되지 않은 객체에서 호출하면 오류입니다.

Despawn 과정에서는 Entity의 runtime 연결을 해제하고, 파생 `OnDespawned` 실행이 실패하더라도 최종적으로 이전 Registry Key를 제거합니다.

전체 Registry를 정리할 때는 각 객체 정리를 끝까지 시도하는 계약을 사용합니다.
실패한 하나 때문에 나머지 객체의 정리를 생략하지 않는 흐름과 공개 이벤트의 일반 multicast 예외 전달은 서로 다른 경계입니다.

## 직렬화 후 복원

`SpawnRegistryBase`는 직렬화된 Spawned 사전을 복구한 뒤 각 entry를 검증하고 Registry 내부 runtime 연결을 다시 구성합니다.
진행 중 Spawn 집합, 외부 binding, 전체 despawn 상태 같은 실행 중 정보는 새 runtime 상태로 초기화합니다.

따라서 직렬화된 "등록 결과"와 실행 중 transition 상태를 같은 데이터로 취급하지 않습니다.
## Presentation 수명

Entity View 표현은 Entity 자체의 lifecycle과 분리됩니다.
`EntityPresentationCoordinator`는 여러 `IEntityPresentationProvider`를 등록 순서대로 실행하고, 생성 중 실패하면 완료된 Provider만 역순으로 rollback합니다.

```text
Provider A Spawn
Provider B Spawn
Provider C 실패
      ↓
Provider B Release(SpawnRollback)
Provider A Release(SpawnRollback)
```

정상 Release에서도 Provider 정리를 역순으로 끝까지 시도하며 여러 실패가 있으면 모아서 전달합니다.
Coordinator는 concrete presentation, Registry, Pool 또는 독립적인 despawn 정책을 소유하지 않습니다.

## 확장 지점

- 새 Entity 모델: `IEntity` 또는 `EntityBase`
- 새 획득 정책: `EntitySpawnRegistry.Acquire()` 구현
- 새 Key 정책: `IKeyGenerator<ulong>` 주입
- 새 View 생성: `IEntityViewFactory`
- 새 Presentation 구성: `IEntityPresentationProvider`

## 제약과 주의사항

- Spawned/Despawning 소유 관계를 우회해 Key나 Registry callback을 직접 조작하지 않습니다.
- 죽은 HP를 명시적 부활 없이 다시 Spawn하지 않습니다.
- Entity 모델과 View/Presentation 객체의 소유권을 합치지 않습니다.
- Spawn 실패 rollback과 정상 Despawn을 같은 사유로 처리하지 않습니다.

## 관련 문서

- [Xeri Game](../../../Runtime/게임/README.md)
- [소유권과 수명](../../concepts/ownership-and-lifetime.md)
- [확장 계약](../../concepts/extension-contracts.md)
