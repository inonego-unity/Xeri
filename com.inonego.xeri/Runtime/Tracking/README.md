# Xeri Tracking

## 개요


Xeri Tracking은 매 frame 원하는 값을 조회하고, 필요하면 전이한 뒤 실제 대상에 반영하는 반복 갱신 흐름을 공통화합니다.

## 왜 필요한가

월드 위치→UI 위치, 외부 상태→표시값처럼 반복되는 `resolve / apply / hide` 로직을 각 Presenter의 `Update()`에 직접 쓰면 수명과 unavailable 처리, smoothing이 중복됩니다. Tracking은 이 반복 관계를 Binding으로 묶고 `Lease`로 해제 책임을 반환합니다.

## 언제 사용하는가

- 외부 값을 매 frame 다시 계산해 UI/표시 객체에 반영할 때
- 목표가 사라지면 기존 적용 상태를 `clear`해야 할 때
- transition/smoothing과 실제 commit을 분리하고 싶을 때

실제 연결 예는 [월드 값을 UI에 Tracking하기](../../Documentation~/guides/tracking/track-ui-element.md)를 참고합니다.

## 어디서 시작하는가

Binding의 `resolve → transition → commit → clear` 의미와 수명은 [Tracking 상세](../../Documentation~/modules/tracking/tracking.md), Presenter/View에서 `Lease`를 실제로 보관하는 흐름은 [Tracking 사용 가이드](../../Documentation~/guides/tracking/track-ui-element.md)에서 시작합니다.

## 핵심 개념

```text
resolve -> transition -> commit
   ↓ unavailable
 clear
```

- `TrackingBinding<T>`: resolve, transition, commit, clear 계약을 한 단위로 구성
- `TrackingController`: binding의 현재 적용 상태와 갱신을 관리
- `TrackingRunner`: Unity frame에서 여러 tracking binding을 실행

## 소유권과 수명

`TrackingRunner.Track(...)`로 시작한 추적은 `Lease`로 수명을 관리합니다. 표시 객체나 Screen처럼 실제 사용 범위의 소유자가 Lease를 보관하고 종료합니다.

## 제약과 주의사항

Tracking은 무엇을 추적해야 하는지 결정하지 않습니다. 대상 선택, projection, clamp 같은 도메인 정책은 사용하는 기능에서 구성합니다.

## 관련 문서

- [Tracking 상세](../../Documentation~/modules/tracking/tracking.md)
- [소유권과 수명](../../Documentation~/concepts/ownership-and-lifetime.md)
- [Game UI](../UI/Game/README.md)
