# 결정적 생성 흐름 만들기

이 가이드는 하나의 Root Seed에서 하위 생성 작업별 Seed를 분리하고, 생성 결과를 Validator로 검사하는 범용 패턴을 설명합니다.

## 목적

생성 결과를 재현 가능하게 유지하면서 서로 다른 하위 생성 작업의 난수 호출 순서가 불필요하게 결합되지 않도록 Seed 경계를 설계합니다.

## 왜 하위 Seed를 분리하는가

하나의 `GenerationRandom`을 모든 생성 작업이 공유하면 앞 단계에서 난수 호출 하나가 추가되는 것만으로 뒤의 모든 결과가 바뀔 수 있습니다.

```text
나쁜 예
Root Random → Terrain → Loot → Decoration
                호출 수 변경 ───────→ 뒤 결과 전부 변경
```

역할과 안정 ID에서 Seed를 파생하면 서로 다른 하위 흐름의 결합을 줄일 수 있습니다.

```text
Root Seed
├─ "terrain"       → Terrain Random
├─ "loot"          → Loot Random
└─ "room:<id>"     → Room-local Random
```

## 1. Root Seed 준비

```csharp
using inonego.Xeri.Generation;

var rootSeed = new GenerationSeed(0x1234UL);
```

Seed 값은 저장 데이터, 사용자 입력, 월드 Seed 등 프로젝트가 결정합니다.
## 2. 작업별 Seed 파생

stable key는 배열 index보다 의미가 오래 유지되는 식별자를 사용합니다.

```csharp
var terrainRandom = rootSeed.Derive("terrain").CreateRandom();
var lootRandom = rootSeed.Derive("loot").CreateRandom();

int hillCount = terrainRandom.NextInt(3, 8);
float rareRoll = lootRandom.NextFloat01();
```

동적으로 생성되는 항목은 해당 항목의 안정 ID를 key에 포함할 수 있습니다.

```csharp
GenerationSeed roomSeed = rootSeed.Derive("room:" + roomID);
var sizeRandom = roomSeed.Derive("size").CreateRandom();
var propRandom = roomSeed.Derive("props").CreateRandom();
```

이렇게 하면 `props` 생성 로직의 난수 호출이 바뀌어도 `size` 결과는 영향을 받지 않습니다.

## 3. Generator는 Seed를 입력으로 받는다

Generator 내부에서 임의의 전역 Random을 만들기보다 Seed를 명시적으로 전달하면 재현과 테스트가 쉬워집니다.

```csharp
public static LayoutResult Generate(LayoutProfile profile, GenerationSeed seed)
{
    var random = seed.Derive("layout").CreateRandom();
    // profile과 random으로 결과 구성
    return new LayoutResult();
}
```
## 4. 생성 결과 검증

Generator와 Validator를 분리하면 생성 알고리즘이 실패 표시 방식이나 재시도 정책을 직접 소유하지 않아도 됩니다.

```csharp
using System.Collections.Generic;
using inonego.Xeri.Generation;

public sealed class LayoutValidator : IGenerationValidator<LayoutResult>
{
    public GenerationValidationResult Validate(LayoutResult result)
    {
        var issues = new List<GenerationValidationIssue>();

        if (result.RoomCount == 0)
        {
            issues.Add(new GenerationValidationIssue
            (
                "layout.empty",
                GenerationIssueSeverity.Error,
                "생성된 공간이 없습니다."
            ));
        }

        return new GenerationValidationResult(issues);
    }
}
```

`Error`가 있으면 `IsValid`가 `false`가 되지만, 결과를 재시도할지 버릴지는 프로젝트 생성 pipeline이 결정합니다.

## 관련 문서

- [Generation](../../modules/generation/generation.md)
- [Xeri 통합 패턴](../../concepts/integration-patterns.md)