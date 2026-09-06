# Xeri Workspace

## 개요

`Runtime/Workspace`는 사용자가 만들고, 열고, 수정하고, 저장하고, 닫는 작업 단위를 다루는 영역입니다.
현재 제공되는 기본 workspace 모듈은 `Document`입니다.

Workspace는 UI, 파일 포맷, serializer, Unity EditorWindow에 직접 묶이지 않는 작업 상태를 다룹니다.
사용자 입력과 표시 방식은 view/editor 계층에서 붙이고, 사용자 흐름 해석은 각 workspace 모듈의 controller가 담당합니다.

## 왜 필요한가

파일 읽기/쓰기만으로는 “현재 열려 있는 작업”, dirty 상태, Save/SaveAs/SaveTo 의미, 중복 Open, 사용자에게 위치를 물어봐야 하는 상태를 표현하기 어렵습니다. Workspace는 외부 저장소와 UI 사이에 장기 작업 상태를 두어 같은 편집 흐름을 Runtime Tool, Unity Editor, 테스트에서 재사용할 수 있게 합니다.

## 언제 사용하는가

- 여러 작업 단위를 동시에 열고 저장·닫아야 할 때
- 저장 위치와 실제 작업 상태를 분리해야 할 때
- UI가 바뀌어도 같은 Create/Open/Save/Close 규칙을 재사용해야 할 때
- domain reload나 Host 재생성 후 열린 작업 상태를 복구해야 할 때

단순 설정 파일 하나를 읽고 즉시 덮어쓰는 흐름에는 Workspace보다 IO + Serializer 조합이 더 적합합니다.

## 어디서 시작하는가

문서형 작업이라면 [Document README](./Document/README.md)에서 Session, Handler, Service/Controller와 기본 저장 흐름부터 확인합니다. 프로젝트 UI는 Controller 결과를 해석하되 파일 패널이나 탭 상태를 Workspace Core에 넣지 않습니다.

## 현재 모듈

| 모듈 | 역할 |
|---|---|
| `Document` | 문서 설명 정보, 모델, 위치, 열린 session, create/open/save/close 흐름 |

Document 사용법은 [Document README](./Document/README.md)를 기준으로 봅니다.

## 책임 범위

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

## 관련 문서

- [Workspace Document](Document/README.md)
- [Document Workspace 구성하기](../../Documentation~/guides/workspace/build-document-workspace.md)
- [Workspace 유지보수 지침](../../Documentation~/maintainers/workspace.md)
