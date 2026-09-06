# Runtime 모듈

Xeri Runtime은 책임이 다른 여러 모듈로 구성됩니다. 각 모듈 README는 상세 API 목록보다 책임 경계, 핵심 개념, 사용 진입점을 설명합니다.

| 모듈 | 개요 문서 | 주요 역할 |
|---|---|---|
| Core | [README](../../Runtime/Core/README.md) | Bootstrapper, Lease, Singleton, 공통 계약 |
| IO | [README](../../Runtime/IO/README.md) | 파일·메모리·Unity asset 데이터 접근 |
| Localization | [README](../../Runtime/Localization/README.md) | locale 저장과 localized string/UI 연결 |
| Playback | [README](../../Runtime/Playback/README.md) | Cue, 재생 수명, Audio |
| Rendering | [README](../../Runtime/Rendering/README.md) | Runtime rendering 보조 기능 |
| Serializable | [README](../../Runtime/Serializable/README.md) | Unity 직렬화 보조 타입과 serializer |
| Tracking | [README](../../Runtime/Tracking/README.md) | 값 resolve·transition·commit 추적 |
| UI | [README](../../Runtime/UI/README.md) | Game UI, Drag & Drop, Picker, Xeri UI |
| Workspace | [README](../../Runtime/Workspace/README.md) | 장기 작업 상태와 Document workflow |
| Game | [README](../../Runtime/게임/README.md) | Entity, Spawn, State, HP, AI 등 |
| Data | [README](../../Runtime/데이터/README.md) | Table, DataPackage, REF |
| Generation | [README](../../Runtime/생성/README.md) | seed, random, validation |
| Utility | [README](../../Runtime/유틸리티/README.md) | pooling, timer, paging, logging 등 |

## 주요 세부 시스템

- Core: [Bootstrapper](core/bootstrapper.md), [Singleton과 슬롯](core/singleton.md), [Primitive](core/primitive.md)
- Localization: [Localization](localization/localization.md)
- Playback: [Playback Cue](playback/cue.md)
- Rendering: [Instancing](rendering/instancing.md)
- Serializable: [Value와 Modifier](serialization/value.md), [Collections](serialization/collections.md), [Serializer](serialization/serializer.md), [Managed Reference](serialization/managed-reference.md)
- Tracking: [Tracking](tracking/tracking.md)
- Xeri UI: [Window](xeri-ui/window.md), [Tray](xeri-ui/tray.md), [View](xeri-ui/view.md)
- Game: [Entity와 Spawn 수명](game/entity-lifecycle.md), [State Machine](game/state-machine.md), [Board](game/board.md), [Controller](game/controller.md), [HP](game/hp.md), [Physics Query](game/physics-query.md), [AI Group](game/ai-group.md), [Use](game/use.md), [Reaction](game/reaction.md), [Zone Graph](game/zone-graph.md), [Level](game/level.md)
- Data: [DataPackage](data/data-package.md)
- Generation: [Generation](generation/generation.md)
- Utility: [GameObject Provider](utility/game-object-provider.md), [Object Pooling](utility/pooling.md), [Timer](utility/timer.md), [Paging](utility/paging.md)
- Game UI: [설정과 시작](game-ui/setup.md), [구조와 수명](game-ui/architecture.md), [Screen과 입력](game-ui/screens.md), [표시와 배치](game-ui/presentation.md)

## 읽는 순서

처음 Xeri를 프로젝트에 연결하는 중이라면 [어떤 모듈을 선택할까](../getting-started/choosing-modules.md)와 [프로젝트 통합 패턴](../concepts/integration-patterns.md)을 먼저 확인합니다.

특정 시스템을 이해하려면 해당 모듈 문서에서 `왜 필요한가`, `언제 사용하는가`, `기본 사용`과 책임 경계를 확인하고, 실제 구현 절차가 필요하면 [사용 가이드](../guides/index.md)로 이동합니다.

여러 모듈에 공통으로 적용되는 수명 규칙은 [소유권과 수명](../concepts/ownership-and-lifetime.md), 전체 설계는 [Xeri 구조](../concepts/architecture.md)를 기준으로 합니다.
