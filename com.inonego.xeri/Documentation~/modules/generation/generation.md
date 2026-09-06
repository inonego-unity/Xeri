# Generation

Xeri Generation은 재현 가능한 생성 결과를 만들기 위한 Seed, 결정적 난수열과 검증 진단의 최소 계약을 제공합니다.

## 왜 필요한가

절차적 생성이 하나의 전역 Random 호출 순서에 의존하면 하위 로직 한 곳의 난수 호출 수가 바뀌는 것만으로 다른 영역 결과까지 연쇄적으로 달라질 수 있습니다. Xeri는 Root Seed에서 역할·안정 ID별 하위 Seed를 파생해 생성 흐름을 분리하고, 결과 검증을 생성 알고리즘과 별도 계약으로 둡니다.

## 언제 사용하는가

- 같은 Seed에서 같은 결과를 재현해야 할 때
- 여러 하위 생성기가 서로의 난수 호출 순서에 덜 영향을 받아야 할 때
- 생성 결과를 Warning/Error 진단으로 검증하고 싶을 때

비결정성이 목적이거나 단발성 `UnityEngine.Random` 호출 몇 번이면 충분한 기능에는 별도 Generation 구조가 필요하지 않을 수 있습니다.

## 기본 사용

```csharp
var root = new GenerationSeed(0x1234UL);
var random = root.Derive("room:main:size").CreateRandom();

int width = random.NextInt(4, 9);
float ratio = random.NextFloat01();
```

stable key는 collection index보다 의미가 오래 유지되는 식별자를 사용합니다. 전체 패턴은 [결정적 생성 흐름 만들기](../../guides/generation/deterministic-generation.md)를 참고합니다.

## 핵심 모델

```text
Root GenerationSeed
        ↓ Derive("stable-key")
독립 Subtree Seed
        ↓ CreateRandom()
GenerationRandom
        ↓
프로젝트 생성 알고리즘
        ↓
IGenerationValidator<TResult>
        ↓
GenerationValidationResult
```

Generation은 실제 던전, 맵, 아이템, 배치 알고리즘을 제공하는 시스템이 아니라 그 알고리즘들이 공유할 결정성·분리·검증 경계를 제공합니다.

## Seed 파생

`GenerationSeed`는 64비트 값을 보관하며 `Derive(stableKey)`로 안정적인 하위 Seed를 만듭니다.

같은 부모 Seed와 같은 key는 항상 같은 결과를 만들고, 서로 다른 stable key는 독립된 생성 흐름을 구성하는 데 사용합니다.
## 결정적 난수

`GenerationRandom`은 `GenerationSeed`에서 시작하는 결정적 의사 난수열입니다.

- `NextUInt64()`
- `NextInt(minInclusive, maxExclusive)`
- `NextFloat01()`

난수 분포, noise, 가중치, 후보 평가와 같은 도메인 알고리즘은 포함하지 않습니다.

## 검증

`IGenerationValidator<TResult>`는 생성 결과를 검사하고 `GenerationValidationResult`를 반환합니다.

진단은 `Warning`과 `Error`로 구분하며, Error가 하나라도 있으면 `IsValid`가 `false`가 됩니다.

```text
Generation Result
      ↓
Validator
      ↓
Issue[]
├─ Warning
└─ Error → 결과 사용 차단
```

진단의 `Code`는 도메인 Validator가 정하는 안정 식별자이고, `Message`는 제작자나 로그에 보여줄 설명입니다.
## 책임 범위

Generation은 다음 정책을 강제하지 않습니다.

- Recipe/Pass 구조
- Retry와 Backtracking
- 생성 결과 Manifest
- Runtime instance 생성 방식
- 배치 점수, noise, weight와 도메인별 분포

프로젝트 생성 시스템은 Xeri의 Seed와 검증 계약을 사용하되, 실제 생성 구조와 실패 복구 정책은 상위 도메인이 소유합니다.

## 관련 문서

- [Xeri 구조](../../concepts/architecture.md)
- [Core Primitive](../core/primitive.md)
