# Xeri Utility

## 개요


Xeri Utility는 독립적인 재사용 가치가 있지만 별도 Runtime 도메인으로 분리할 필요가 없는 보조 기능을 모읍니다.

## 왜 필요한가

GameObject 공급, Pooling, Timer, Paging처럼 여러 모듈에서 재사용되지만 특정 도메인에 속하지 않는 작은 기능이 있습니다. Utility는 이런 기능을 모으되, 상태와 lifecycle이 커져 독립 도메인이 되면 별도 모듈로 분리하는 것을 원칙으로 합니다.

## 언제 사용하는가

- GameObject 공급 backend를 `IGameObjectProvider`로 숨길 때
- 반복 객체의 Acquired/Released 수명을 Pool로 관리할 때
- MonoBehaviour 비종속 Timer나 UI Paging 상태가 필요할 때
- 공통 Logger/helper가 특정 도메인에 속하지 않을 때

Utility를 새로운 프로젝트 도메인 코드의 기본 보관소로 사용하지 않습니다. 사용 예는 각 [GameObject Provider](../../Documentation~/modules/utility/game-object-provider.md), [Pooling](../../Documentation~/modules/utility/pooling.md), [Timer](../../Documentation~/modules/utility/timer.md), [Paging](../../Documentation~/modules/utility/paging.md) 문서에서 확인합니다.

## 어디서 시작하는가

외부 공급 방식 차이를 숨기려면 [GameObject Provider](../../Documentation~/modules/utility/game-object-provider.md), 재사용 객체 수명이면 [Pooling](../../Documentation~/modules/utility/pooling.md)과 [Pool Lease 가이드](../../Documentation~/guides/utility/use-pool-lease.md), 시간 상태면 [Timer](../../Documentation~/modules/utility/timer.md), 목록 page 상태면 [Paging](../../Documentation~/modules/utility/paging.md)에서 시작합니다.

## 주요 영역

- 게임 오브젝트 프로바이더: prefab/Addressables 기반 GameObject 획득 계약
- 로그: `ILogger`, `LoggerBase`, Unity Debug adapter
- 오브젝트 풀링: 일반 Pool과 GameObject/Component pool
- 타이머: Runtime timer와 상태 계약
- 페이징: `Paginator`, `PageRange`
- 확장 유틸리티: collection, string, vector, layer, tween, Unity helper
- 에디터 보조: gizmo와 attribute 관련 공통 기능

## 책임 범위

Utility에는 특정 도메인의 핵심 상태나 workflow를 두지 않습니다. 기능이 독립 lifecycle, 상태 소유권, 명확한 도메인을 갖게 되면 별도 모듈로 분리하는 것을 우선합니다.

## 제약과 주의사항

- 단순히 여러 곳에서 쓰인다는 이유만으로 프로젝트 도메인 코드를 Utility로 이동하지 않습니다.
- helper가 상태와 소유권을 갖기 시작하면 해당 책임의 자연스러운 모듈을 다시 검토합니다.
- Addressables처럼 release가 필요한 provider는 반환 수명 계약을 사용처가 지켜야 합니다.

## 관련 문서

- [GameObject Provider](../../Documentation~/modules/utility/game-object-provider.md)
- [Object Pooling](../../Documentation~/modules/utility/pooling.md)
- [Timer](../../Documentation~/modules/utility/timer.md)
- [Paging](../../Documentation~/modules/utility/paging.md)
- [소유권과 수명](../../Documentation~/concepts/ownership-and-lifetime.md)
- [Xeri 구조](../../Documentation~/concepts/architecture.md)
