# 확장 계약

## 배경

Xeri는 프로젝트마다 달라지는 구현을 프레임워크 내부에 하드코딩하기보다 공개 interface와 source, driver, handler를 통해 연결하는 방식을 우선합니다.

## 핵심 모델

```text
Xeri Runtime
    ↓ 공개 계약
Project Adapter / Source / Driver / Handler
    ↓
프로젝트 데이터 또는 Unity backend
```

## 규칙

- 새 타입을 만들기 전에 해당 모듈의 기존 확장 계약을 확인합니다.
- 확장 구현은 자신이 맡은 획득과 반환, 등록과 해제를 대칭적으로 처리합니다.
- 프로젝트 도메인 판단은 범용 backend 구현으로 내려보내지 않습니다.
- 구체 backend가 필요 없는 상위 Controller는 interface 계약을 통해 동작하게 유지합니다.
- 실제 사용 경로가 없는 추상화나 미래용 adapter를 미리 추가하지 않습니다.

## 대표 예

| 영역 | 확장 계약 예 |
|---|---|
| IO | `IDataReader`, `IDataWriter` |
| Game UI | `IScreenSource`, `IScreenDriver`, `IPresentationLayerDriver` |
| Workspace Document | `IDocumentHandler`, `IDocumentLocation` |
| Playback | `ICuePlayer`, `ICueBinding` |

세부 계약은 각 모듈 README와 API Reference를 기준으로 확인합니다.

## 관련 문서

- [Xeri 구조](architecture.md)
- [소유권과 수명](ownership-and-lifetime.md)
- [모듈 목록](../modules/index.md)