# Xeri Unity Package

`com.inonego.xeri`는 Xeri의 Unity Package Manager foundation package입니다. Unity 6 이상을 기준으로 하며 Runtime, Editor와 문서를 제공합니다.

Game 영역은 논리 Entity lifecycle과 Scene Actor를 분리하며, `EntityActorRegistry`의 Entity → Actor 0..N 관계와 `EntityActorRuntime`의 Actor + Presentation create/rollback/release transaction을 제공합니다. AbilityState·Binding 및 Activation의 Requirement·Cost 평가·소비 계약은 Xeri가 제공하고, 게임별 사용 승인 규칙·자원 정책·실행 Host 연결 및 Save 루트는 소비 프로젝트가 소유합니다. AI/Control의 게임별 정책 역시 소비 프로젝트의 책임입니다.

## 시작하기

현재 저장소를 로컬 패키지로 사용하는 경우 Unity `Packages/manifest.json`에서 이 디렉터리를 참조합니다.

```json
"com.inonego.xeri": "file:../External/UniXeri/com.inonego.xeri"
```

패키지의 실제 의존성은 [package.json](package.json)을 기준으로 확인합니다.

Application UI lifecycle, Window, Tray와 Bar는 [Xeri UI](https://github.com/inonego-unity/Xeri-UI) package에서 제공합니다. 이 패키지의 `Runtime/UI`는 Drag & Drop·Picker와 이를 지원하는 공통 목록/Popup 기반만 담당하며 Application UI 수명 관리를 대체하지 않습니다.

## 문서 구조

- [Documentation~/index.md](Documentation~/index.md): 사용자 문서 시작점
- [Gameplay Framework 시스템 구조](Documentation~/modules/game/gameplay-framework.md): Ability·Execution·Spatial·Delivery·실제 결과/Owner 계약 (현행/목표 구분)
- [Documentation~/getting-started/installation.md](Documentation~/getting-started/installation.md): 설치와 요구 환경
- [Documentation~/getting-started/choosing-modules.md](Documentation~/getting-started/choosing-modules.md): 문제에 맞는 모듈 선택
- [Documentation~/guides/index.md](Documentation~/guides/index.md): 실제 작업 절차와 코드 예제
- [Runtime/Core/Application/README.md](Runtime/Core/Application/README.md): Application 실행 세대와 자원 소유권, 공개 API 사용 예제
- [Documentation~/modules/core/bootstrapper.md](Documentation~/modules/core/bootstrapper.md): Unity 자동 초기화와 Scene phase
- [Documentation~/guides/game/compose-entity-actor-runtime.md](Documentation~/guides/game/compose-entity-actor-runtime.md): Entity → Actor 0..N과 Actor + Presentation transaction 조립
- [Documentation~/guides/serialization/edit-collections-in-inspector.md](Documentation~/guides/serialization/edit-collections-in-inspector.md): Collection Editor 사용법
- [Documentation~/concepts/integration-patterns.md](Documentation~/concepts/integration-patterns.md): 프로젝트 Adapter와 Xeri 책임 경계
- [Runtime/](Runtime/): Runtime 모듈과 각 모듈 README
- [Editor/README.md](Editor/README.md): Editor 전용 구현
- [Tests/README.md](Tests/README.md): 패키지 검증 코드

처음 사용한다면 **설치 → 모듈 선택 → 프로젝트 통합 패턴 → 사용할 시스템의 모듈 문서 → 작업 가이드** 순서로 읽는 것을 권장합니다.
