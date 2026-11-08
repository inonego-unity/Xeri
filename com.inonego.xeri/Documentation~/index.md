# Xeri 문서

Xeri는 Unity 프로젝트에서 반복되는 Runtime 수명, 데이터 접근, 게임 객체, 재생과 직렬화 문제를 독립 모듈과 명시적인 계약으로 다루는 foundation framework입니다.

Application UI lifecycle, Window, Tray와 Bar는 별도 [Xeri UI](https://inonego-unity.github.io/Xeri-UI/) package가 담당합니다.

## 처음이라면

1. [설치와 요구 환경](getting-started/installation.md)
2. [프로젝트에 처음 연결하기](getting-started/first-setup.md)
3. [어떤 모듈을 선택할까](getting-started/choosing-modules.md)
4. [프로젝트 통합 패턴](concepts/integration-patterns.md)
5. [사용 가이드](guides/index.md)

Xeri의 내부 설계 원칙까지 이해하려면 이후 [Xeri 구조](concepts/architecture.md)와 [소유권과 수명](concepts/ownership-and-lifetime.md)을 읽습니다.

## 목적별 찾기

| 하고 싶은 일 | 시작 문서 |
|---|---|
| 시작 순서를 구성한다 | [Bootstrapper](modules/core/bootstrapper.md) |
| 작업 실행과 Undo/Redo history를 관리한다 | [Commanding과 DoSession](modules/commanding/do-session.md) |
| Entity를 Spawn/Despawn한다 | [Entity와 Spawn 수명](modules/game/entity-lifecycle.md) |
| Ability·Execution·Cost·Spatial·Delivery·Effect 소유권을 설계한다 | [Gameplay Framework 시스템 구조](modules/game/gameplay-framework.md) |
| Entity와 Scene Actor를 0..N으로 분리하고 Presentation까지 조립한다 | [Entity Actor Runtime 조립하기](guides/game/compose-entity-actor-runtime.md) |
| Screen/Modal/Window UI를 운영한다 | [Xeri UI Documentation](https://inonego-unity.github.io/Xeri-UI/) |
| UGUI/UITK Drag & Drop 또는 Picker를 구성한다 | [UI Utilities](../Runtime/UI/README.md) |
| Picker/Editor가 공유하는 팝업 배치 경계를 확인한다 | [UI Popup 공통 기반](../Runtime/UI/README.md) |
| 컬렉션을 Inspector에서 편집한다 | [Collection Editor 가이드](guides/serialization/edit-collections-in-inspector.md) |
| Audio/VFX Cue를 공통 재생한다 | [Playback Cue](modules/playback/cue.md) |
| 결정적 procedural generation을 만든다 | [Generation](modules/generation/generation.md) |
| 반복 객체의 반환 수명을 관리한다 | [Object Pooling](modules/utility/pooling.md) |

## 프로젝트와 Xeri의 경계

실제 프로젝트에서는 Xeri 타입을 도메인 곳곳에 직접 퍼뜨리기보다 Adapter, Service, Presenter, Registry 같은 프로젝트 경계에서 Xeri 계약을 조합하는 방식을 권장합니다.

```text
프로젝트 정책 / 도메인 상태
        ↓
Adapter / Service / Presenter
        ↓
Xeri 계약과 Runtime 수명
        ↓
Unity / External Storage / Rendering backend
```

Xeri가 소유하는 범용 lifecycle과 프로젝트가 선택하는 정책을 구분하는 기준은 [프로젝트 통합 패턴](concepts/integration-patterns.md)에서 설명합니다.

## 더 보기

- [전체 Runtime 모듈](modules/index.md)
- [사용 가이드 전체 목록](guides/index.md)
- [확장 계약](concepts/extension-contracts.md)
