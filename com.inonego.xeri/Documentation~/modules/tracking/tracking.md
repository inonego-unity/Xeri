# Tracking

Xeri Tracking은 외부에서 계산한 원하는 값을 매 Tick 다시 조회하고, 선택적으로 전이한 뒤 실제 대상에 반영하는 범용 관계 시스템입니다.

## 왜 필요한가

월드 좌표를 UI 좌표로 투영하거나, 외부 상태를 화면 표시로 따라가게 하는 코드는 `Update()`마다 `if`와 보정 로직을 반복하기 쉽습니다. Tracking은 `resolve → transition → commit → clear` 흐름과 그 수명을 하나의 Binding으로 묶어 표시 대상과 추적 정책을 분리합니다.

## 언제 사용하는가

- 월드 대상의 위치·상태를 UI에 지속적으로 반영할 때
- 목표값이 없어지는 순간 표시 상태를 정리해야 할 때
- smoothing/clamp 같은 전이 정책을 실제 적용 코드와 분리하고 싶을 때
- 여러 추적 관계를 하나의 Runner/Controller 수명에서 관리할 때

단순히 값 하나를 한 번 복사하는 작업에는 Tracking이 필요하지 않습니다.

## 기본 사용

```csharp
var binding = new TrackingBinding<Vector2>
(
    resolve: ResolvePosition,
    commit: ApplyPosition,
    clear: HideMarker
);

Lease lease = trackingRunner.Track(binding);
```

View/Presenter가 사라질 때 `lease.Dispose()`하면 마지막 적용 상태도 `clear` callback으로 정리됩니다. 전체 예제는 [월드 값을 UI에 Tracking하기](../../guides/tracking/track-ui-element.md)를 참고합니다.

## 핵심 흐름

```text
resolve
  ↓ Available?
transition (선택)
  ↓
commit
  ↓
실제 적용값을 다음 current로 보관
```

`TrackingBinding<T>`의 `commit` 반환값은 다음 Tick의 현재값이 됩니다. Clamp나 실제 UI 배치처럼 후보값과 적용값이 다를 수 있는 경우에도 다음 전이가 실제 적용 상태에서 이어집니다.

## Binding

Binding은 네 callback으로 구성됩니다.

- `resolve`: 현재 원하는 값과 사용 가능 여부를 조회
- `transition`: 현재 적용값에서 원하는 값으로 전이
- `commit`: 실제 대상에 반영하고 적용된 최종값 반환
- `clear`: 적용 상태가 사라질 때 선택적으로 정리

`resolve`가 `Available = false`를 반환하면 마지막 적용 상태를 정리합니다.
## Controller와 수명

`TrackingController.Track()`은 Binding을 자신의 소유로 확정하고 해제용 `Lease`를 반환합니다. 같은 Binding은 하나의 Controller에 한 번만 등록할 수 있으며 해제 뒤 재등록할 수 없습니다.

Controller는 등록 순서대로 Binding을 갱신합니다. Tick 중 새로 등록된 Binding은 다음 Tick부터 실행되고, 중첩 `Tick()`은 허용하지 않습니다.

Lease를 해제하면 Binding의 마지막 적용 상태를 먼저 종료하고 `clear`를 최대 한 번 호출합니다.

`TrackingController.Dispose()`는 모든 Binding 정리를 끝까지 시도한 뒤 오류를 집계합니다.

## TrackingRunner

`TrackingRunner`는 선택적 Scene Component로 Controller를 `LateUpdate`에 연결합니다.

- 비활성화: 갱신만 멈춤
- GameObject 파괴: Controller와 모든 Binding 종료
- `UsesUnscaledTime`: 전이에 `Time.unscaledDeltaTime` 사용

Game UI World Marker처럼 특정 수명 객체에 Tracking을 종속시킬 때는 반환된 Lease를 해당 Session이나 Presenter가 소유합니다.

## 제약과 주의사항

- Tracking은 목표 값을 어떻게 계산할지 알지 않습니다.
- 외부 callback 안에서 Binding이 해제되면 남은 갱신 단계는 중단됩니다.
- `clear` 실패를 다음 해제에서 재시도하지 않습니다.
- 실제 대상이 없어졌을 때 `resolve`가 `Available = false`를 반환하도록 구성합니다.

## 관련 문서

- [Tracking 모듈](../../../Runtime/Tracking/README.md)
- [Game UI 표시와 배치](../game-ui/presentation.md)
- [소유권과 수명](../../concepts/ownership-and-lifetime.md)
