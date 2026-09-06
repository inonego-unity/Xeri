# Xeri Unity Package

`com.inonego.xeri`는 Xeri의 Unity Package Manager 패키지입니다. Unity 6 이상을 기준으로 하며 Runtime, Editor, Sample과 문서를 함께 제공합니다.

## 시작하기

현재 저장소를 로컬 패키지로 사용하는 경우 Unity `Packages/manifest.json`에서 이 디렉터리를 참조합니다.

```json
"com.inonego.xeri": "file:../External/UniXeri/com.inonego.xeri"
```

패키지의 실제 의존성은 [`package.json`](package.json)을 기준으로 확인합니다.

## 문서 구조

- [`Documentation~/index.md`](Documentation~/index.md): 사용자 문서 시작점
- [`Documentation~/getting-started/installation.md`](Documentation~/getting-started/installation.md): 설치와 요구 환경
- [`Documentation~/getting-started/choosing-modules.md`](Documentation~/getting-started/choosing-modules.md): 문제에 맞는 모듈 선택
- [`Documentation~/guides/index.md`](Documentation~/guides/index.md): 실제 작업 절차와 코드 예제
- [`Documentation~/concepts/integration-patterns.md`](Documentation~/concepts/integration-patterns.md): 프로젝트 Adapter와 Xeri 책임 경계
- [`Runtime/`](Runtime/): Runtime 모듈과 각 모듈 README
- [`Editor/README.md`](Editor/README.md): Editor 전용 구현
- [`Samples~/README.md`](Samples~/README.md): Package Manager에서 가져올 수 있는 샘플
- [`Tests/README.md`](Tests/README.md): 패키지 검증 코드

처음 사용한다면 **설치 → 모듈 선택 → 프로젝트 통합 패턴 → 사용할 시스템의 모듈 문서 → 작업 가이드** 순서로 읽는 것을 권장합니다.