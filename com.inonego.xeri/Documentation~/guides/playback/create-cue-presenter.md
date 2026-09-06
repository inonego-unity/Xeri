# Cue Presenter 구성하기

Xeri Playback은 어떤 게임 사건에서 Cue를 재생할지 결정하지 않습니다. 프로젝트 Presenter가 사건과 타이밍을 해석하고, `CuePlaybackService`는 그 Cue를 처리할 Player를 선택하고 생성된 Playback 수명을 추적합니다.

## 목적

프로젝트 사건 해석과 재생 backend를 분리해서 Presenter는 **언제 어떤 Cue를 재생할지**만 결정하고, Xeri는 Player 선택과 Playback 수명을 공통으로 관리하게 합니다.

## 역할 분리

```text
Project Event / Timeline
        ↓
Project Presenter
        ↓ IPlaybackCue + optional Binding
CuePlaybackService
        ↓
ICuePlayer
        ↓
ICuePlayback
```

Audio, Particle, VFX를 하나의 프로젝트 timeline에서 다뤄도 실제 backend 선택은 Xeri Player 계약에 맡길 수 있습니다.

## 1. Runtime Cue 준비

Asset 기반 Cue는 owner 수명에서 runtime Cue를 한 번 만들어 재사용하는 편이 좋습니다. Variant 선택 이력처럼 runtime 상태가 Asset과 분리되기 때문입니다.

```csharp
using inonego.Xeri.Playback;

public sealed class SampleCuePresenter
{
    private readonly CuePlaybackService playbackService;
    private readonly AudioCue confirmCue;

    public SampleCuePresenter
    (
        CuePlaybackService playbackService,
        AudioCueAsset confirmCueAsset
    )
    {
        this.playbackService = playbackService;
        confirmCue = confirmCueAsset.CreateCue();
    }

    public ICuePlayback PlayConfirm()
    {
        return playbackService.Play(confirmCue);
    }
}
```

단순 Audio만 사용하는 곳은 `AudioManager.Current.Play()`를 직접 사용해도 됩니다. 여러 Cue backend를 같은 수명에서 다루거나 공통 종료 경계가 필요할 때 `CuePlaybackService`가 유용합니다.

## 2. Service에 Player 구성

Service는 외부에서 전달한 Player 목록 중 해당 Cue와 Binding을 처리할 수 있는 유일한 Player를 선택합니다.

```csharp
var service = new CuePlaybackService
(
    new ICuePlayer[]
    {
        AudioManager.Current,
        particleCuePlayer,
        vfxCuePlayer,
    }
);
```

같은 Cue/Binding을 둘 이상의 Player가 처리하면 조립 오류로 거부합니다. 프로젝트 composition 단계에서 Player 역할이 겹치지 않게 구성합니다.

## 3. Project Event에서 재생

Presenter는 게임 사건을 관찰하고 해당 시점에 Xeri Cue를 실행합니다.

```csharp
private void OnConfirmed()
{
    ICuePlayback playback = playbackService.Play(confirmCue);
    ownedPlaybacks.Add(playback);
}
```

어떤 사건에 어떤 Cue를 연결할지, action-relative timeline을 어떻게 평가할지는 프로젝트 책임입니다.
## 4. Playback 수명 정리

`CuePlaybackService`는 생성한 Playback을 추적하고 `Tick()`에서 이미 `Released`된 항목을 제거합니다. Presenter가 소유한 장기 Playback은 자신의 수명에서 직접 `Dispose()`하거나, 상위 runtime 종료 시 `StopAll()`로 정리합니다.

```csharp
private void LateUpdate()
{
    playbackService.Tick();
}

public void Dispose()
{
    playbackService.StopAll();
}
```

One-shot처럼 자연 종료되는 재생과, loop·지속 효과처럼 owner보다 오래 살아서는 안 되는 재생을 구분합니다.

## 관련 문서

- [Playback Cue](../../modules/playback/cue.md)
- [Xeri Audio](../../../Runtime/Playback/Cue/Audio/README.md)
- [소유권과 수명](../../concepts/ownership-and-lifetime.md)