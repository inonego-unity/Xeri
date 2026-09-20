# Runtime 모듈

Xeri Runtime은 책임이 다른 여러 모듈로 구성됩니다. 각 모듈 README는 상세 API 목록보다 책임 경계, 핵심 개념, 사용 진입점을 설명합니다.

| 모듈 | 개요 문서 | 주요 역할 |
|---|---|---|
| Core | [README](../../Runtime/Core/README.md) | Bootstrapper, Lease, Singleton, 공통 계약 |
| Commanding | [README](../../Runtime/Commanding/README.md) | Command 실행, Undo/Redo history, 그룹 |
| Playback | [README](../../Runtime/Playback/README.md) | Cue, 재생 수명, Audio |
| Rendering | [README](../../Runtime/Rendering/README.md) | Runtime rendering 보조 기능 |
| Serializable | [README](../../Runtime/Serializable/README.md) | Unity 직렬화 보조 타입과 serializer |
| Tracking | [README](../../Runtime/Tracking/README.md) | 값 resolve·transition·commit 추적 |
| UI Utilities | [README](../../Runtime/UI/README.md) | Drag & Drop, Picker |
| Game | [README](../../Runtime/게임/README.md) | Entity, Spawn, State, HP, AI 등 |
| Generation | [README](../../Runtime/생성/README.md) | seed, random, validation |
| Utility | [README](../../Runtime/유틸리티/README.md) | pooling, timer, paging, logging 등 |

## 주요 세부 시스템

- Core: [Bootstrapper](core/bootstrapper.md), [Singleton과 슬롯](core/singleton.md), [Primitive](core/primitive.md)
- Commanding: [DoSession](commanding/do-session.md)
- Playback: [Playback Cue](playback/cue.md)
- Rendering: [Instancing](rendering/instancing.md)
- Serializable: [Value와 Modifier](serialization/value.md), [Collections](serialization/collections.md), [Serializer](serialization/serializer.md), [Managed Reference](serialization/managed-reference.md)
- Tracking: [Tracking](tracking/tracking.md)
- Game: [Entity와 Spawn 수명](game/entity-lifecycle.md), [State Machine](game/state-machine.md), [Board](game/board.md), [Controller](game/controller.md), [HP](game/hp.md), [Physics Query](game/physics-query.md), [AI Group](game/ai-group.md), [Use](game/use.md), [Reaction](game/reaction.md), [Zone Graph](game/zone-graph.md), [Level](game/level.md)
- Generation: [Generation](generation/generation.md)
- Utility: [GameObject Provider](utility/game-object-provider.md), [Object Pooling](utility/pooling.md), [Timer](utility/timer.md), [Paging](utility/paging.md)
- UI Utilities: [Drag & Drop](../../Runtime/UI/Drag_Drop/README.md), [Picker](../../Runtime/UI/Picker/README.md)
- Application UI: [Xeri UI Documentation](https://inonego-unity.github.io/Xeri-UI/)

## 읽는 순서

처음 Xeri를 프로젝트에 연결하는 중이라면 [어떤 모듈을 선택할까](../getting-started/choosing-modules.md)와 [프로젝트 통합 패턴](../concepts/integration-patterns.md)을 먼저 확인합니다.

여러 모듈에 공통으로 적용되는 수명 규칙은 [소유권과 수명](../concepts/ownership-and-lifetime.md), 전체 설계는 [Xeri 구조](../concepts/architecture.md)를 기준으로 합니다.
