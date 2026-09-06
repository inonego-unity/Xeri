# Timer

Xeri `Timer`는 `MonoBehaviour`에 의존하지 않는 float 기반 타이머입니다. 호출자가 원하는 update 경계에서 `Tick(deltaTime)`을 직접 호출합니다.

## 왜 필요한가

쿨다운, 입력 버퍼, 유예 시간처럼 작은 시간 상태를 각각 Coroutine이나 MonoBehaviour로 만들면 실행 주기와 pause semantics가 분산됩니다. Xeri Timer는 시간 상태만 소유하고 어떤 delta time을 쓸지와 어느 Tick에서 진행시킬지는 상위 시스템이 결정하게 합니다.

## 언제 사용하는가

- Coyote time, input buffer, cooldown처럼 명시적 상태가 필요한 짧은 타이머
- `Update`, `FixedUpdate`, unscaled time 등 서로 다른 시간 경계에서 같은 타이머 모델을 쓰고 싶을 때
- Pause/Resume와 종료 이벤트를 데이터 객체 수준에서 관리할 때

일회 지연 후 함수 하나를 실행하는 정도라면 Coroutine/Awaitable이 더 간단할 수 있습니다.

## 기본 사용

```csharp
var timer = new Timer();
timer.OnEnd += (_, _) => CompleteCooldown();
timer.Start(0.5f);

// 호출자의 update 경계
timer.Tick(deltaTime);
```

`Timer`는 자동으로 갱신되지 않습니다. 상위 Controller가 자신의 시간 의미에 맞는 delta를 전달해야 합니다.

## 상태 모델

```text
Ready
  ↓ Start
Run
  ↔ Pause / Resume
  ↓ Stop 또는 시간 종료
Ready
```

`TimerState`는 `Ready`, `Run`, `Pause` 세 상태를 가집니다.

## 시간 값

- `Duration`: 현재 실행의 전체 시간
- `ElapsedTime`: 경과 시간
- `RemainingTime`: 남은 시간
- `ElapsedTime01`, `RemainingTime01`: 0~1 비율

`Duration`, `ElapsedTime`, `RemainingTime`은 Run 또는 Pause 상태에서만 직접 변경할 수 있습니다.

`Duration == 0`이면 비율 값은 0으로 강제하지 않고 float 나눗셈 결과인 `NaN`이 될 수 있습니다.

## 실행 흐름

`Start(duration)`은 실행 중이거나 Pause 상태에서는 `AlreadyRunningException`을 발생시킵니다. 시작 중 유효하지 않은 duration 때문에 실패하면 상태를 다시 `Ready`로 복구합니다.
`Tick()`은 Run 상태에서만 경과 시간을 진행시키며 종료 시 `OnEnd`를 호출한 뒤 `Stop()`으로 `Ready` 상태에 들어갑니다.

`Pause()`와 `Resume()`은 해당 상태에서만 동작하고, `Reset()`은 Run/Pause 상태에서는 거부됩니다.

## 이벤트

- `OnEnd`: duration에 도달했을 때 발생
- `OnStateChange`: `Ready`, `Run`, `Pause` 상태가 바뀔 때 발생

## 사용 경계

Timer는 시간을 어디서 받을지 결정하지 않습니다. Gameplay 시간은 `Time.deltaTime`, physics는 `fixedDeltaTime`, UI는 unscaled time처럼 호출자가 의미에 맞는 delta를 전달합니다.

```csharp
var timer = new Timer();
timer.Start(0.2f);

// 호출자의 update 경계
timer.Tick(deltaTime);
```

## 제약과 주의사항

- 자동 update runner가 없으므로 `Tick()` 호출을 누락하면 진행하지 않습니다.
- `Stop()`은 경과 시간 값을 지우지 않습니다. `Reset()` 또는 다음 `Start()` 전까지 유지됩니다.
- `Reset()`은 duration과 elapsed를 0으로 초기화합니다.
- Timer를 사용하는 상위 시스템이 pause semantics와 delta time 종류를 소유해야 합니다.

## 관련 문서

- [Game Controller](../game/controller.md)
- [Utility 모듈](../../../Runtime/유틸리티/README.md)
