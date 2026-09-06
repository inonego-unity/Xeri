# 모듈 선택 가이드

Xeri는 하나의 거대한 Runtime을 강제하지 않습니다. 해결하려는 문제에 맞는 모듈을 선택하고 필요한 계약만 조합합니다.

## 애플리케이션 시작과 공통 수명

| 필요 | 모듈 |
|---|---|
| Initial Scene 전후 초기화 순서 | Core / Bootstrapper |
| 일회성 반환 책임 | Core / Lease |
| 이름별 Runtime 인스턴스와 임시 Context | Core / Singleton |

## 데이터와 직렬화

| 필요 | 모듈 |
|---|---|
| 파일·메모리·Resources·Addressables 읽기/쓰기 | IO |
| 객체와 JSON/XML 문자열 변환 | Serializable / Serializer |
| Unity 직렬화용 Dictionary/Ordered Collection | Serializable / Collections |
| Base 값과 Modifier 합성 | Serializable / Value |
| 여러 Table과 Source를 하나의 조회 Context로 제공 | Data / DataPackage |
| Key만 저장하고 현재 데이터 Context에서 늦게 해석 | Data / `REF<T>` |

## 게임 Runtime

| 필요 | 모듈 |
|---|---|
| 객체 등록과 Spawn/Despawn 수명 | Game / Entity + Spawn |
| 단순 FSM | Game / State Machine |
| HP와 Alive/Dead 상태 | Game / HP |
| 2D/3D 보드 배치 | Game / Board |
| 상호작용 후보 선택과 사용 전달 | Game / Use |
| Signal → Guard → Action 연결 | Game / Reaction |
| AI 판단 대상 수명 경계 | Game / AI Group + Brain |
## UI와 표현

| 필요 | 모듈 |
|---|---|
| 게임 Screen/Modal/Overlay/Focus/Input Runtime | Game UI |
| UGUI/UITK 공통 Drag & Drop | UI / Drag & Drop |
| 검색·필터·테이블 선택 UI | UI / Picker |
| 데스크톱형 Window/Tray | Xeri UI |
| Stable ID 기반 UITK View와 Session 복원 | Xeri UI / View |
| 월드 값을 화면 위치로 반복 반영 | Tracking |
| 대량 Mesh의 `RenderMeshInstanced` 제출 | Rendering / Instancing |

## 재생과 생성

| 필요 | 모듈 |
|---|---|
| Audio/VFX/Particle을 공통 Cue로 실행 | Playback |
| 재현 가능한 procedural generation Seed | Generation |
| 생성 결과 Warning/Error 검증 | Generation Validation |

## Utility를 먼저 쓰지 말아야 하는 경우

새 기능이 명확한 상태, 소유권, lifecycle을 갖는다면 단순히 여러 곳에서 쓴다는 이유로 Utility에 넣지 않습니다. Xeri의 기존 모듈 계약에 자연스럽게 들어가는지 먼저 확인합니다.

## 다음 단계

1. [첫 설정](first-setup.md)에서 프로젝트 통합 방식을 정합니다.
2. [Runtime 모듈](../modules/index.md)에서 선택한 시스템의 개념과 계약을 읽습니다.
3. 실제 작업은 해당 시스템의 [사용 가이드](../guides/index.md)를 따릅니다.