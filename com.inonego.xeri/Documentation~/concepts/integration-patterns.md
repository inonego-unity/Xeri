# Xeri 통합 패턴

Xeri는 프로젝트의 도메인 구조를 대체하는 프레임워크가 아니라, 여러 프로젝트에서 반복되는 수명·등록·재생·표시·직렬화 문제를 공통 계약으로 분리하는 기반입니다. 실제 프로젝트에서는 Xeri 타입을 모든 게임 로직에서 직접 호출하기보다 프로젝트 Adapter, Service, Presenter가 Xeri 계약을 감싸는 구성이 일반적입니다.

## 왜 이런 구조를 사용하는가

프로젝트 규칙과 Xeri 계약을 직접 섞으면 프레임워크를 교체하거나 재사용하기 어려워지고, 반대로 Xeri 안에 프로젝트 정책을 넣으면 패키지 자체가 특정 게임에 종속됩니다.

```text
Project Domain
     ↓
Adapter / Service / Presenter
     ↓
Xeri Contract
     ↓
Unity / IO / Rendering Backend
```

중간 Adapter는 프로젝트의 용어와 Xeri의 범용 계약을 서로 변환합니다.

## 수명은 대칭으로 연결한다

Xeri의 여러 시스템은 획득과 종료가 짝을 이룹니다.

```text
Register ↔ Unregister
Acquire  ↔ Release
Bind     ↔ Unbind
Track    ↔ Lease.Dispose
Open     ↔ Close
```

프로젝트 객체가 Xeri 자원을 획득하면 같은 프로젝트 객체나 명확한 상위 소유자가 종료 책임도 보관하는 편이 안전합니다.
## 완전한 상태가 된 뒤 공개한다

Registry나 전역 Current에 부분 구성 상태를 먼저 노출하면 다른 시스템이 아직 준비되지 않은 객체를 읽을 수 있습니다. 여러 Source나 의존 자원을 조립하는 경우에는 필요한 구성을 먼저 완료하고 마지막에 공개합니다.

```text
Load / Validate / Compose
          ↓ 성공
Register / Publish
```

실패하면 이미 획득한 `Lease`, Handle, View, Source를 역순으로 정리하고 외부에는 준비되지 않은 상태를 노출하지 않습니다.

## 안정 ID와 Key를 사용한다

`DataPackage.Source`, Window ID, Generation stable key처럼 저장·복원·결정성에 참여하는 식별자는 collection index나 일시적인 object name보다 의미가 안정적인 문자열이나 key를 사용합니다.

특히 결정적 생성에서는 하위 난수 흐름을 배열 순서 하나에 묶지 않고 역할과 안정 ID에서 Seed를 파생하면 다른 하위 시스템의 변경이 결과 전체에 전파되는 것을 줄일 수 있습니다.

## Xeri와 프로젝트가 각각 결정하는 것

| Xeri가 제공하는 것 | 프로젝트가 결정하는 것 |
|---|---|
| `UseController`의 후보 선택과 사용 전달 | 후보를 어떻게 탐색할지, 어떤 입력을 사용할지 |
| `CuePlaybackService`의 Player 선택과 Playback 수명 | 어떤 게임 사건에서 어떤 Cue를 재생할지 |
| `DataPackage`의 Table/Source/Scope 계약 | 데이터를 어디서 읽고 어떤 시점에 등록할지 |
| `GenerationSeed`의 결정적 파생 | 실제 생성 알고리즘과 stable key 체계 |
| `TrackingBinding`의 resolve/commit 수명 | 무엇을 추적하고 어떤 좌표계로 변환할지 |
| `EntitySpawnRegistry`의 Spawn/Despawn 계약 | 구체 Entity 생성과 외부 자원 해제 방식 |
## 관련 문서

- [Xeri 구조](architecture.md)
- [소유권과 수명](ownership-and-lifetime.md)
- [확장 계약](extension-contracts.md)
- [Runtime 모듈](../modules/index.md)