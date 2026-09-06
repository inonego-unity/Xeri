# Xeri

**Xeri**는 Unity 프로젝트에서 반복되는 Runtime 구조와 생명주기 문제를 공통 계약으로 분리하는 모듈형 프레임워크입니다.

게임마다 달라지는 규칙과 콘텐츠는 프로젝트에 남기고, 여러 프로젝트에서 반복되는 **초기화, 소유권, 데이터 접근, UI, 재생, 직렬화, 작업 상태와 게임 Runtime 기반 기능**을 재사용 가능한 형태로 제공합니다.

> Unity 6 · UPM package · Runtime-first modular framework

## Xeri가 해결하려는 문제

Unity 프로젝트가 커질수록 기능 자체보다 다음과 같은 경계가 반복해서 복잡해집니다.

- 누가 Runtime 객체를 만들고 언제 해제하는가
- Scene 전환 전후의 초기화 순서를 어떻게 보장하는가
- 파일, 메모리, Resources, Addressables 차이를 소비 코드에서 어떻게 숨기는가
- Screen, Modal, Overlay와 Focus/Input 수명을 어떻게 일관되게 관리하는가
- 게임 도메인 로직과 범용 Entity/State/HP/AI 기반을 어떻게 분리하는가
- 저장 가능한 상태와 화면 표시 상태를 어디까지 나눌 것인가

Xeri는 이 문제들을 하나의 거대한 Manager로 통합하지 않고, 책임별 작은 계약과 Runtime으로 나누는 방향을 취합니다.

## 설계 방향

Xeri의 공통 설계 기준은 다음과 같습니다.

- **명시적 소유권** — `Acquire ↔ Release`, `Register ↔ Unregister`, `Bind ↔ Unbind`처럼 시작과 종료를 짝으로 표현합니다.
- **프로젝트 정책 분리** — Xeri는 범용 Runtime 계약을 제공하고, 실제 게임 규칙은 Adapter/Service/Presenter 계층에 둡니다.
- **완전 구성 후 공개** — Registry나 Current Context에는 필요한 구성이 끝난 객체만 노출합니다.
- **Backend 분리** — IO, UI, Playback 등은 구체 공급 경로나 표시 backend와 상위 소비 코드를 분리합니다.
- **작은 모듈 조합** — 모든 기능을 하나의 전역 Runtime에 넣지 않고 필요한 시스템만 선택해서 사용합니다.

## 주요 모듈

| 영역 | 주요 역할 |
|---|---|
| **Core** | Bootstrapper, Lease, Singleton, 공통 primitive와 lifecycle 계약 |
| **IO** | File, Memory, Resources, Addressables 데이터 접근 |
| **Serializable** | Unity 직렬화 보조 컬렉션, serializer, `MValue`, managed reference |
| **Data** | Table, `DataPackage`, `REF<T>` 기반 데이터 Context |
| **Game** | Entity, Spawn, State Machine, HP, AI Group, Board, Zone, Use/Reaction |
| **UI** | Game UI, Drag & Drop, Picker, Xeri Window/Tray/View |
| **Playback** | Cue, Playback lifecycle, Unity Audio |
| **Tracking** | resolve → transition → commit 반복 갱신과 Lease 수명 |
| **Rendering** | 대량 Mesh instance batch와 runtime instancing |
| **Generation** | 결정적 Seed/Random과 생성 결과 validation |
| **Localization** | Locale 상태와 localized UI 갱신 |
| **Workspace** | Document create/open/save/close/recovery workflow |
| **Utility** | GameObject Provider, Pool, Timer, Paging 등 독립 보조 기능 |

## 빠른 시작

Xeri는 Unity Package Manager 패키지 `com.inonego.xeri`로 제공됩니다.

로컬 checkout을 사용하는 경우 Unity 프로젝트의 `Packages/manifest.json`에서 패키지 경로를 연결할 수 있습니다.

```json
"com.inonego.xeri": "file:../External/UniXeri/com.inonego.xeri"
```

현재 패키지 최소 Unity 버전과 직접 의존성은 [`com.inonego.xeri/package.json`](com.inonego.xeri/package.json)을 기준으로 확인합니다.

처음 적용한다면 다음 순서로 보는 것을 권장합니다.

1. [설치와 요구 환경](com.inonego.xeri/Documentation~/getting-started/installation.md)
2. [모듈 선택 가이드](com.inonego.xeri/Documentation~/getting-started/choosing-modules.md)
3. [프로젝트 통합 패턴](com.inonego.xeri/Documentation~/concepts/integration-patterns.md)
4. 사용할 시스템의 [Runtime 모듈 문서](com.inonego.xeri/Documentation~/modules/index.md)
5. 실제 조립 절차가 필요하면 [사용 가이드](com.inonego.xeri/Documentation~/guides/index.md)

## 문서

- **Documentation Site** — https://inonego-unity.github.io/Xeri/
- [사용자 Manual](com.inonego.xeri/Documentation~/index.md)
- [Unity 패키지 README](com.inonego.xeri/README.md)
- [문서 작성 규칙](Docs/documentation-style.md)

Manual은 개념과 실제 사용법을 설명하고, 사이트의 **API Reference**는 public C# API에서 자동 생성됩니다.

## 저장소 구조

```text
UniXeri/
├─ com.inonego.xeri/      Unity Package
│  ├─ Runtime/
│  ├─ Editor/
│  ├─ Samples~/
│  ├─ Tests/
│  └─ Documentation~/
├─ Docs/                  문서 작성·유지보수 자료
└─ README.md
```
