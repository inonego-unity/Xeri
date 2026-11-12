# Xeri Core

`com.inonego.xeri`는 Unity 공통 Runtime·Editor 기능을 제공하는 Xeri Core 패키지입니다. Unity 6 이상을 기준으로 합니다.

Entity·Spawn·Actor·WorldObject·State Machine 및 Ability·Execution·HP·AI 등의 게임 기능은 [Xeri Game](https://github.com/inonego-unity/Xeri-Game) 패키지(`com.inonego.xeri.game`)가 담당합니다. Game은 Core의 키 생성·직렬화·Condition·일반 유틸리티를 사용하며, Core는 Game에 의존하지 않습니다.

## 시작하기

현재 저장소를 로컬 패키지로 사용하는 경우 Unity `Packages/manifest.json`에서 이 디렉터리를 참조합니다.

```json
"com.inonego.xeri": "file:../External/UniXeri/com.inonego.xeri"
```

패키지의 실제 의존성은 [package.json](package.json)을 기준으로 확인합니다.

Application UI lifecycle, Window, Tray와 Bar는 [Xeri UI](https://github.com/inonego-unity/Xeri-UI) package에서 제공합니다. 이 패키지의 `Runtime/UI`는 Drag & Drop·Picker와 이를 지원하는 공통 목록/Popup 기반만 담당하며 Application UI 수명 관리를 대체하지 않습니다.

## 문서 구조

- [Documentation~/index.md](Documentation~/index.md): 사용자 문서 시작점
- [Documentation~/getting-started/installation.md](Documentation~/getting-started/installation.md): 설치와 요구 환경
- [Documentation~/getting-started/choosing-modules.md](Documentation~/getting-started/choosing-modules.md): 문제에 맞는 모듈 선택
- [Documentation~/guides/index.md](Documentation~/guides/index.md): 실제 작업 절차와 코드 예제
- [Runtime/Core/Application/README.md](Runtime/Core/Application/README.md): Application 실행 세대와 자원 소유권, 공개 API 사용 예제
- [Documentation~/modules/core/bootstrapper.md](Documentation~/modules/core/bootstrapper.md): Unity 자동 초기화와 Scene phase
- [Xeri Game Manual](https://inonego-unity.github.io/Xeri-Game/): 별도 게임 패키지의 Foundation과 기능 모듈
- [Documentation~/guides/serialization/edit-collections-in-inspector.md](Documentation~/guides/serialization/edit-collections-in-inspector.md): Collection Editor 사용법
- [Documentation~/concepts/integration-patterns.md](Documentation~/concepts/integration-patterns.md): 프로젝트 Adapter와 Xeri 책임 경계
- [Runtime/](Runtime/): Runtime 모듈과 각 모듈 README
- [Editor/README.md](Editor/README.md): Editor 전용 구현
- [Tests/README.md](Tests/README.md): 패키지 검증 코드

처음 사용한다면 **설치 → 모듈 선택 → 프로젝트 통합 패턴 → 사용할 시스템의 모듈 문서 → 작업 가이드** 순서로 읽는 것을 권장합니다.
