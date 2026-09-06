# 월드 값을 UI에 Tracking하기

`TrackingBinding<T>`은 매 frame 값을 조회하고, 필요하면 전이한 뒤 실제 대상에 적용하는 흐름을 공통화합니다. 이 가이드는 월드 Transform을 화면 UI Marker로 따라가게 하는 예를 사용합니다.

## 목적

월드→화면 변환, 표시 가능 여부, 실제 UI 적용과 해제 시 정리를 하나의 Binding 수명으로 묶고, View/Presenter가 반환받은 Lease를 소유하게 합니다.

## 1. Runner 준비

Scene의 적절한 Host에 `TrackingRunner`를 둡니다. Runner는 등록된 Binding을 `LateUpdate`에서 갱신하고 GameObject가 파괴되면 모든 Binding을 종료합니다.

## 2. Binding 만들기

```csharp
using UnityEngine;
using inonego.Xeri;

public sealed class WorldMarker : MonoBehaviour
{
    [SerializeField] private TrackingRunner runner;
    [SerializeField] private Camera worldCamera;
    [SerializeField] private Transform target;
    [SerializeField] private RectTransform marker;

    private Lease trackingLease;

    private void OnEnable()
    {
        var binding = new TrackingBinding<Vector2>
        (
            resolve: ResolvePosition,
            commit: CommitPosition,
            clear: ClearMarker
        );

        trackingLease = runner.Track(binding);
    }

    private (bool Available, Vector2 Value) ResolvePosition()
    {
        if (target == null || worldCamera == null)
        {
            return (false, default);
        }

        Vector3 screen = worldCamera.WorldToScreenPoint(target.position);
        return screen.z > 0f
            ? (true, (Vector2)screen)
            : (false, default);
    }

    private Vector2 CommitPosition(Vector2 screenPosition)
    {
        marker.position = screenPosition;
        marker.gameObject.SetActive(true);
        return screenPosition;
    }

    private void ClearMarker()
    {
        marker.gameObject.SetActive(false);
    }

    private void OnDisable()
    {
        trackingLease?.Dispose();
        trackingLease = null;
    }
}
```

## transition이 필요한 경우

`transition(current, resolved, deltaTime)`을 전달하면 마지막 commit 결과에서 새 목표값으로 보간할 수 있습니다. 화면 clamp나 smoothing이 필요한 경우 프로젝트 정책을 이 함수에 둡니다.
## 수명 규칙

`Track()`이 반환한 `Lease`가 Binding의 소유권입니다. View나 Presenter가 사라질 때 Lease를 해제하면 마지막 적용 상태도 `clear` callback으로 정리됩니다.

Binding을 여러 Controller에 재등록하거나 해제된 Binding을 다시 사용하지 않습니다.

## 관련 문서

- [Tracking](../../modules/tracking/tracking.md)
- [소유권과 수명](../../concepts/ownership-and-lifetime.md)