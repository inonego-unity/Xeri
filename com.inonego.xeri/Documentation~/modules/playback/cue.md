# Playback Cue

Playback Cue는 Audio, Particle System, VFX Graph 같은 재생 정의를 공통 실행 계약으로 묶는 계층입니다.
Cue는 재사용 가능한 정의를 소유하고, 실행마다 달라지는 대상·위치 같은 값은 `ICueBinding`으로 전달합니다.

## 왜 필요한가

Audio, Particle, VFX마다 프로젝트 Presenter가 서로 다른 재생 API와 수명을 직접 관리하면 같은 timeline 안에서도 재생·중지·정리 방식이 달라집니다. Playback Cue는 "재사용 가능한 정의"와 "이번 실행의 Binding"을 분리하고, `CuePlaybackService`가 호환 Player 선택과 Playback 추적을 공통화합니다.

## 언제 사용하는가

- Audio/VFX/Particle을 같은 사건·timeline에서 공통 방식으로 실행할 때
- 재생 backend와 프로젝트 사건 해석을 분리하고 싶을 때
- 여러 Playback을 하나의 상위 수명에서 `StopAll()` 해야 할 때

단순한 Audio 한 번 재생만 필요하면 `AudioManager.Current.Play()` 직접 호출이 더 간단합니다.

## 기본 사용

```csharp
var service = new CuePlaybackService(players);
ICuePlayback playback = service.Play(cue);

// frame 정리
service.Tick();

// owner 종료
service.StopAll();
```

프로젝트 Presenter가 어떤 사건에서 어떤 Cue를 실행할지 결정하고 Service는 Player 선택과 Playback 수명을 맡는 구성이 일반적입니다. [Cue Presenter 구성하기](../../guides/playback/create-cue-presenter.md)에서 전체 흐름을 확인할 수 있습니다.

## 핵심 모델

```text
IPlaybackCue
   + ICueBinding
        ↓
ICuePlayer<TBinding>
        ↓
ICuePlayback
        ↓
실행 자원과 상태
```

`IPlaybackCue`는 언제 재생할지나 어떤 순서로 배치할지를 소유하지 않습니다.
Sequence, Timeline 또는 호출자가 시작 시점과 실행 순서를 결정합니다.

## Player 선택

`CuePlaybackService`는 생성 시 명시적으로 전달받은 Player 목록에서 Cue와 Binding을 처리할 수 있는 Player를 찾습니다.

- 처리 가능한 Player가 없으면 오류입니다.
- 둘 이상의 Player가 같은 Cue/Binding 조합을 처리하면 조립 오류입니다.
- 선택된 Player는 실패 시 자신이 만든 부분 자원을 정리한 뒤 예외를 전달해야 합니다.

```csharp
var service = new CuePlaybackService(players);
ICuePlayback playback = service.Play(cue, binding);
```

Binding이 필요 없는 Cue는 `NoCueBinding.Default`를 사용하는 `Play(cue)` 경로를 사용할 수 있습니다.

## Playback 수명

Player가 반환하는 `ICuePlayback`이 한 번의 실제 재생 인스턴스이자 종료 책임입니다.
`CuePlaybackService`는 반환된 Playback을 추적하지만 개별 Playback의 내부 자원이나 의미를 소유하지 않습니다.

`Tick()`은 `Released` 상태의 Playback을 추적 목록에서 제거합니다.
`StopAll()`은 현재 추적 중인 모든 Playback을 역순으로 `Dispose()`하고, 여러 종료 실패가 있으면 모아서 전달합니다.
## 제공 Cue 계열

현재 Runtime에는 공통 Cue 외에 다음 Unity backend가 있습니다.

- `UnityAudioClipCue` / `UnityAudioCuePlayer`
- `UnityParticleSystemCue` / `UnityParticleSystemCuePlayer`
- `UnityVFXGraphCue` / `UnityVFXGraphCuePlayer`
- `VariantCue` 계열의 여러 재생 후보 선택
- Transform fixed/tracked Binding과 VFX Graph Binding

Audio는 DSP 예약, Bus, Music Layer 등 추가 계약이 있으므로 별도 Audio 문서를 사용합니다.

## 확장 지점

| 목적 | 계약 |
|---|---|
| 새 Cue 정의 | `IPlaybackCue` |
| 새 runtime Binding | `ICueBinding` |
| 새 실행 backend | `ICuePlayer<TBinding>` |
| 새 실행 인스턴스 | `ICuePlayback` |

새 Player는 `CanPlay()`에서 실제 지원 범위를 좁게 선언해야 합니다.
범용 fallback Player가 모든 Cue를 처리한다고 응답하면 다른 backend와의 조립 모호성이 생길 수 있습니다.

## 제약과 주의사항

- Cue 정의에 실행 대상 Transform 같은 runtime 상태를 넣지 않습니다.
- Player 선택 우선순위로 중복 지원을 해결하지 않습니다. 둘 이상 지원하면 조립 오류로 봅니다.
- `CuePlaybackService`가 자동으로 frame update되지 않으므로 Released 추적 정리가 필요하면 소유자가 `Tick()`을 호출합니다.
- 장기 재생의 종료 책임을 Service 추적만 믿고 잃어버리지 않습니다.

## 관련 문서

- [Xeri Playback](../../../Runtime/Playback/README.md)
- [Xeri Audio](../../../Runtime/Playback/Cue/Audio/README.md)
- [소유권과 수명](../../concepts/ownership-and-lifetime.md)
- [확장 계약](../../concepts/extension-contracts.md)
