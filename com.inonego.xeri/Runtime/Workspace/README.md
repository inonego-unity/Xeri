# Xeri Workspace

`Runtime/Workspace`는 사용자가 만들고, 열고, 수정하고, 저장하고, 닫는 작업 단위를 다루는 영역입니다.
현재 제공되는 기본 workspace 모듈은 `Document`입니다.

Workspace는 UI, 파일 포맷, serializer, Unity EditorWindow에 직접 묶이지 않는 작업 상태를 다룹니다.
사용자 입력과 표시 방식은 view/editor 계층에서 붙이고, 사용자 흐름 해석은 각 workspace 모듈의 controller가 담당합니다.

## 현재 모듈

| 모듈 | 역할 |
|---|---|
| `Document` | 문서 설명 정보, 모델, 위치, 열린 session, create/open/save/close 흐름 |

Document 사용법은 [Document README](./Document/README.md)를 기준으로 봅니다.

## Workspace에 둘 것

Workspace에는 장기간 유지되는 작업 상태와 그 상태 전이를 둡니다.

- 열린 작업 단위 목록
- 작업 단위의 생성, 열기, 저장, 닫기 흐름
- Runtime과 Unity Editor 양쪽에서 재사용할 수 있는 상태 모델
- UI 없이도 검증 가능한 작업 규칙

반대로 단순 utility, serializer, 파일 IO, UI component, EditorWindow 구현은 Workspace에 넣지 않습니다.

## 책임 경계

```text
Workspace  = 열린 작업 상태 container
Service    = 작업 상태 전이 실행
Controller = 사용자 흐름 해석
View       = 표시와 입력
IO         = 외부 데이터 읽기/쓰기
Serializer = 데이터 형식 변환
```

이 경계를 유지하면 같은 workspace를 Unity Editor, runtime tool, 테스트, 다른 UI에서 재사용하기 쉽습니다.

## 확장 기준

새 workspace 모듈은 다음 질문에 대부분 `yes`일 때 추가합니다.

- 여러 작업 단위를 동시에 보관하거나 전환해야 하는가?
- 작업 단위에 생성, 열기, 저장, 닫기 같은 생명주기가 있는가?
- UI 표시 상태와 실제 작업 상태를 분리해야 하는가?
- 특정 파일 포맷이나 Unity EditorWindow 없이도 의미가 있는가?
- 기존 `Document` 모듈로 표현하기 어려운 독립 도메인인가?

위 조건에 맞지 않으면 Workspace보다 IO, UI, serializer, domain-specific runtime 모듈이 더 적절할 수 있습니다.

## AI 작업 가이드

Workspace 영역을 수정하거나 확장할 때는 먼저 대상 기능이 실제 작업 상태인지 확인합니다.

- 작업 상태면 기존 workspace 모듈로 표현 가능한지 먼저 봅니다.
- 외부 데이터 접근이면 `Runtime/IO` 쪽을 우선 검토합니다.
- 표시와 입력이면 UI 또는 Editor 계층에 둡니다.
- serializer 포맷 분기는 Workspace가 아니라 serializer/handler 쪽에 둡니다.
- active view, focused tab, scroll 같은 view 상태를 Workspace의 전역 정책으로 고정하지 않습니다.

잘못된 방향:

```text
Workspace가 EditorWindow를 직접 참조
Workspace가 파일 다이얼로그를 직접 띄움
Workspace가 serializer 포맷을 직접 분기
Workspace가 active view 상태를 하나로 고정
```
