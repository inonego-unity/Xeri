# Xeri 문서

Xeri는 Unity 프로젝트에서 반복되는 Runtime 수명, 데이터 접근, UI, 게임 객체, 재생과 직렬화 문제를 독립 모듈과 명시적인 계약으로 다루는 프레임워크입니다.

이 문서는 단순한 타입 목록보다 **왜 이 기능이 존재하는지, 언제 선택해야 하는지, 어떻게 프로젝트 코드와 조합하는지**를 설명하는 것을 목표로 합니다.

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
| 데이터를 로드하고 현재 Context에서 조회한다 | [DataPackage](modules/data/data-package.md) |
| Entity를 Spawn/Despawn한다 | [Entity와 Spawn 수명](modules/game/entity-lifecycle.md) |
| Screen/Modal/Overlay UI를 운영한다 | [Game UI 설정과 시작](modules/game-ui/setup.md) |
| Window/Tool UI를 만든다 | [Xeri Window](modules/xeri-ui/window.md) |
| Audio/VFX Cue를 공통 재생한다 | [Playback Cue](modules/playback/cue.md) |
| 결정적 procedural generation을 만든다 | [Generation](modules/generation/generation.md) |
| 반복 객체의 반환 수명을 관리한다 | [Object Pooling](modules/utility/pooling.md) |
## 문서 구분

- **Getting Started**: 설치, 첫 조립, 모듈 선택처럼 처음 시작할 때 필요한 문서
- **개념 문서**: 여러 모듈에 공통으로 적용되는 구조, 소유권, 통합 규칙
- **모듈 문서**: 각 시스템이 무엇이고 왜 필요한지, 언제 사용하고 어떤 계약을 제공하는지 설명
- **사용 가이드**: 특정 작업을 실제 코드와 순서로 완료하는 절차
- **유지보수 문서**: 내부 구현, 테스트, 확장 시 지켜야 하는 구조
- **API Reference**: 향후 DocFX가 public API와 XML documentation에서 생성할 상세 멤버 문서

## 프로젝트와 Xeri의 경계

실제 프로젝트에서는 Xeri 타입을 도메인 곳곳에 직접 퍼뜨리기보다 Adapter, Service, Presenter, Registry 같은 프로젝트 경계에서 Xeri 계약을 조합하는 방식을 권장합니다.

```text
프로젝트 정책 / 도메인 상태
        ↓
Adapter / Service / Presenter
        ↓
Xeri 계약과 Runtime 수명
        ↓
Unity / IO / Rendering backend
```

Xeri가 소유하는 범용 lifecycle과 프로젝트가 선택하는 정책을 구분하는 기준은 [프로젝트 통합 패턴](concepts/integration-patterns.md)에서 설명합니다.

## 더 보기

- [전체 Runtime 모듈](modules/index.md)
- [사용 가이드 전체 목록](guides/index.md)
- [확장 계약](concepts/extension-contracts.md)
- [유지보수 문서](maintainers/index.md)