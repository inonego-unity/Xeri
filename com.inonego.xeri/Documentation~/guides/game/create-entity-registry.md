# Entity Registry 구현하기

이 가이드는 프로젝트의 논리 Entity를 Xeri `EntitySpawnRegistry`에 연결하는 기본 구조를 설명합니다.

## 목적

프로젝트는 구체 Entity의 생성·반환 방식만 구현하고, Key 발급과 Spawn/Despawn 상태 전이·조회·rollback은 Xeri Registry 계약에 맡깁니다.

## 왜 Registry를 두는가

Entity 생성 코드를 여러 곳에서 직접 호출하면 Key 발급, Spawn 상태, Despawn 정리와 조회 기준이 분산됩니다. Registry는 이 공통 lifecycle을 한곳에 모으고, 프로젝트는 실제 객체 생성과 외부 자원 해제 방식만 구현합니다.

## 1. Entity 타입 정의

`EntityBase`를 상속하고 HP와 Group 상태를 제공합니다.

```csharp
using System;
using inonego.Xeri.Game;
using inonego.Xeri.Serializable;

[Serializable]
public sealed class SampleEntity : EntityBase
{
    private readonly Value<int> group = new(0);
    private readonly HP_I hp = new();

    public override IValue<int> Group => group;
    public override IHP HP => hp;

    public void Initialize(int maxHP)
    {
        hp.MaxValue = maxHP;
        hp.MakeAlive();
    }
}
```

Spawn 전에 HP가 살아 있는 상태여야 합니다.
## 2. Registry 구현

프로젝트가 실제 Entity를 어떻게 가져오고 반환하는지만 구현합니다.

```csharp
using System;
using inonego.Xeri.Game;

[Serializable]
public sealed class SampleEntityRegistry : EntitySpawnRegistry<SampleEntity>
{
    protected override SampleEntity Acquire()
    {
        var entity = new SampleEntity();
        entity.Initialize(maxHP: 100);
        return entity;
    }

    protected override void Release(SampleEntity entity, DespawnReason reason)
    {
        // Pool 반환, 외부 reference 해제 등 프로젝트 정리를 수행한다.
    }
}
```

Registry가 Key와 `SpawnState` 전이를 관리하므로 `Acquire()`에서 임의 Key를 부여하거나 Spawn 상태를 직접 바꾸지 않습니다.

## 3. Spawn과 조회

```csharp
var registry = new SampleEntityRegistry();

if (registry.TrySpawn(out SampleEntity entity))
{
    ulong key = entity.Key;
    SampleEntity same = registry.Find(key);
}
```

Entity의 HP가 Dead 상태로 바뀌면 `EntityBase`는 Registry에 `Dead` 사유의 Despawn을 요청합니다.
## 4. Despawn과 정리

일반 제거는 Entity의 Registry 연결을 통해 요청할 수 있습니다.

```csharp
entity.Despawn();
```

Despawn 과정에서는 Registry가 등록 해제와 상태 전이를 수행한 뒤 프로젝트 `Release()`로 외부 자원을 반환합니다. Pool-backed Entity라면 `Release()`에서 Pool로 되돌리고, 별도 Presentation이 있다면 해당 Provider의 lifecycle과 대칭되게 정리합니다.

## 파라미터 초기화가 필요한 경우

Spawn Context가 필요하면 `EntitySpawnRegistry<TEntity, TParam>`을 사용하고 Entity가 `INeedToInit<TParam>`을 구현하게 합니다. `Acquire(param)`은 객체 획득만, `Init(param)`은 해당 Spawn의 초기 상태 적용을 담당하도록 나누면 rollback 경계가 명확해집니다.

## 관련 문서

- [Entity와 Spawn 수명](../../modules/game/entity-lifecycle.md)
- [HP](../../modules/game/hp.md)
- [Object Pooling](../../modules/utility/pooling.md)
- [소유권과 수명](../../concepts/ownership-and-lifetime.md)