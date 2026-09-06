# Xeri 구조

## 배경

Xeri는 하나의 거대한 Manager를 제공하는 대신, 서로 다른 책임을 가진 Runtime 모듈을 공통 계약으로 조합하는 구조를 사용합니다.

## 핵심 모델

```text
Application / Game Code
        ↓
모듈별 공개 계약
        ↓
Runtime 서비스와 상태
        ↓
Unity / File / Addressables / UI backend
```

상위 코드가 구체 backend에 직접 결합하지 않아야 하는 영역은 interface, source, driver, handler 같은 확장 계약을 통해 분리합니다.

## 모듈 경계

- `Core`는 여러 모듈에서 사용하는 기반 계약과 생명주기를 제공합니다.
- `IO`, `Serializable`은 데이터 접근과 변환 책임을 분리합니다.
- `UI`, `Playback`, `Workspace`, `Game`은 독립적인 Runtime 도메인을 제공합니다.
- 작은 보조 기능은 `Rendering`, `Tracking`, `Generation`, `Utility` 같은 영역에 분리합니다.
## 규칙

- 특정 도메인 책임을 범용 하위 계층으로 내리지 않습니다.
- 기존 확장 지점으로 표현 가능한 기능은 새로운 전역 Manager를 만들기 전에 기존 계약을 사용합니다.
- Runtime 상태와 UI 표시 상태, IO와 serializer 같은 서로 다른 책임을 한 타입에 합치지 않습니다.
- 모듈 간 의존은 실제 사용 흐름에 필요한 방향으로만 둡니다.

## 관련 문서

- [소유권과 수명](ownership-and-lifetime.md)
- [확장 계약](extension-contracts.md)
- [모듈 목록](../modules/index.md)