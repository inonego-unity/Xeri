# Xeri Audio

## 개요

Xeri Audio는 Unity `AudioSource` 기반 Cue 재생과 Master·Bus 출력 제어를 제공한다. `UnityAudioClipCueAsset`은 하나 이상의 interchangeable Variant를 authoring하고, 소비자는 owner 수명에 맞춰 `CreateCue()`로 runtime `UnityAudioClipCue`를 한 번 생성해 재사용한다.

## 왜 필요한가

AudioSource 생성·재사용, Variant 선택 이력, 3D emitter 추적, Bus/Master volume과 Playback 종료를 각 Presenter가 직접 구현하면 재생 정책과 자원 수명이 분산된다. Xeri Audio는 Unity Audio backend와 voice 수명을 공통화하고, 프로젝트는 어떤 사건에서 어떤 Cue를 재생할지에 집중하게 한다.

## 언제 사용하는가

- 여러 Audio Cue가 같은 voice pool과 Bus/Master 설정을 공유할 때
- runtime Cue별 Variant 선택 상태를 Asset과 분리해야 할 때
- loop, scheduled playback, 3D emitter follow 같은 재생 수명을 명시적으로 제어할 때
- Audio를 VFX 등 다른 Cue와 `CuePlaybackService`에서 함께 관리할 때

One-shot SFX 몇 개를 고정 AudioSource로 재생하는 작은 기능에는 전체 Audio Runtime이 필요하지 않을 수 있다.

## 가장 빠른 시작

1. Audio Host에 `AudioManager`와 `UnityAudioCuePlayer`를 구성한다.
2. `UnityAudioClipCueAsset`을 만들고 `CreateCue()`로 runtime Cue를 준비한다.
3. `AudioManager.Current.Play(cue)`로 재생한다.
4. loop나 owner보다 오래 살아서는 안 되는 Playback은 owner 종료 시 명시적으로 `Dispose()`한다.

## Audio Host 구성

Audio Host Prefab Root에 다음 컴포넌트를 둔다.

```text
Audio Host
├── AudioManager
└── UnityAudioCuePlayer
```

`AudioManager`를 추가하면 `[RequireComponent]`에 의해 `UnityAudioCuePlayer`도 함께 추가된다. 등록에 성공한 Audio Host는 항상 `DontDestroyOnLoad`로 유지된다.

`UnityAudioCuePlayer`에는 다음 값을 지정한다.

- `Source Prefab`: voice로 복제할 `AudioSource` Prefab
- `Initial Voice Count`: 시작할 때 Pool에 미리 준비할 voice 수

Source Prefab의 `Play On Awake`는 꺼야 한다. Clip, Pitch, Loop, 출력 Group과 공간 설정은 Cue를 재생할 때마다 덮어쓴다. 그 밖의 `AudioSource` 설정은 Source Prefab의 값을 사용한다.

## Bootstrapper 설정

Project 창에서 다음 Asset을 만든다.

```text
Create/Xeri/Bootstrapper/Audio Module
```

`AudioBootstrapperModuleAsset`의 `Host Prefab`에 Audio Host Prefab을 지정하고 `BootstrapperSettings.Modules`에 등록한다. Audio를 사용할 다른 모듈보다 앞에 배치한다.

```text
BootstrapperSettings.Modules
├── AudioBootstrapperModuleAsset
└── GameUIBootstrapperModuleAsset
```

Bootstrapper를 사용하지 않는 프로젝트는 시작 Scene에 Audio Host Prefab을 직접 배치할 수 있다. 두 방식을 동시에 사용하면 같은 Singleton Slot의 중복 Host 중 나중에 등록한 객체가 제거된다.

## Audio Cue 만들기

Project 창에서 다음 Asset을 만든다.

```text
Create/Xeri/Playback/Unity Audio Clip Cue
```

`UnityAudioClipCueAsset`에는 `Variants`와 선택 정책을 설정한다.

- `Exclude Previous`: Variant가 2개 이상일 때 직전 선택을 다음 선택에서 제외한다.
- `Variants`: 같은 의미적 Cue 안에서 교체 가능한 재생 Variant 목록.
- 각 Variant의 `Clip`: 재생할 `AudioClip`.
- 각 Variant의 `Bus`: `Music`, `SFX`, `UI`, `Voice`, `Ambience`.
- 각 Variant의 `Volume`, `Pitch`, `Is Looping`.
- 각 Variant의 `Spatial Blend`, `Rolloff Mode`, `Min Distance`, `Max Distance`.

`Exclude Previous`의 선택 이력은 Asset이 아니라 `CreateCue()`로 생성한 runtime Cue에 저장된다. 따라서 같은 Asset을 서로 다른 actor가 사용해도 각 actor가 runtime Cue를 따로 만들면 선택 이력이 섞이지 않는다.

## 기본 재생

```csharp
using inonego.Xeri.Playback;
using UnityEngine;

public sealed class CharacterAudio : MonoBehaviour
{
    [SerializeField]
    private UnityAudioClipCueAsset attackCueAsset;

    private AudioCue attackCue;

    private void Awake()
    {
        attackCue = attackCueAsset.CreateCue();
    }

    public void PlayAttack()
    {
        AudioManager.Current.Play(attackCue);
    }
}
```

호출별 볼륨 배율을 전달할 수도 있다.

```csharp
AudioManager.Current.Play(attackCue, volumeScale: 0.5f);
```

최종 출력에는 다음 값이 함께 적용된다.

```text
Playback Volume × Bus Volume × Master Volume
```

Manager의 `Play`에 전달하는 `volumeScale`은 초기 Playback Volume에 Cue Volume과 함께 반영된다.

## DSP 예약 재생

Audio Cue를 Unity Audio DSP 절대 시간축에 예약할 수 있다.

```csharp
var startTime = AudioSettings.dspTime + 0.1;
IAudioPlayback playback = AudioManager.Current.PlayScheduled(cue, startTime);
```

예약된 Playback은 시작 시각 전에도 유효한 수명을 가지며, 시작 전 `isPlaying == false`를 자연 완료로 처리하지 않는다.

## 동기 Music Layer 재생

같은 Timeline을 공유하는 여러 Music Cue는 `MusicLayerGroup` Asset으로 묶는다.

```text
MusicLayerGroup
├── MusicLayer → UnityAudioClipCueAsset
├── MusicLayer → UnityAudioClipCueAsset
└── MusicLayer → UnityAudioClipCueAsset
```

```csharp
IMusicPlayback music = AudioManager.Current.Play(layerGroup);

music.SetLayerVolume(0, 1.0f);
music.SetLayerVolume(1, 0.4f);
music.SetLayerVolume(2, 0.0f);
```

`AudioManager.Play(MusicLayerGroup)`은 Group 전체를 검증한 뒤 모든 Layer를 같은 미래 DSP 시각에 예약하고 하나의 `IMusicPlayback`을 반환한다. Group의 모든 Cue는 `Music` Bus를 사용하고 동일한 sample frequency, sample count, Pitch와 Loop 설정을 가져야 한다.

`IMusicPlayback`은 Layer별 Volume과 Group 단위 Pause, Resume, Stop만 제공한다. 개별 Layer의 Pause, Seek, Pitch 변경은 노출하지 않아 Layer Timeline을 하나의 재생 단위로 유지한다.

게임별 BGM 선택, Combat·Mood 해석, Layer Weight 계산, Cross Fade 정책은 Consumer가 소유한다.

## 3D 재생

고정된 월드 위치에서 재생한다. 재생이 시작된 뒤 호출 대상이 움직여도 재생 위치는 이동하지 않는다.

```csharp
AudioManager.Current.Play(explosionCue, transform.position);
```

재생 중 `Transform`의 위치를 따라가게 하려면 emitter를 전달한다.

```csharp
IAudioPlayback enginePlayback = AudioManager.Current.Play
(
    engineCue,
    transform
);
```

emitter가 파괴되면 해당 Playback도 종료된다.

## Playback 제어

`Play`가 반환하는 `IAudioPlayback`으로 개별 재생을 제어한다.

```csharp
IAudioPlayback playback = AudioManager.Current.Play(musicCue);

playback.Pause();
playback.Resume();

playback.Volume = 0.7f;
playback.Pitch = 1.1f;

playback.Stop(CueStopMode.Natural);
```

- `Stop()` 또는 `Stop(Immediate)`: 즉시 종료
- `Stop(Natural)`: Loop를 해제하고 현재 Clip이 끝난 뒤 종료
- `Dispose()`: 즉시 종료

One-shot은 실제 재생이 끝나면 자동으로 `Released`가 되고 voice를 Pool에 반환한다. Loop 재생이나 소유 객체보다 오래 지속되면 안 되는 재생은 소비자가 명시적으로 종료한다.

```csharp
if (playback.State != CuePlaybackState.Released)
{
    playback.Dispose();
}
```

## 재생 시간 조회

`Clock`은 실제 AudioSource 재생 위치를 읽기 전용으로 제공한다.

```csharp
float currentTime = playback.Clock.Time;
float duration = playback.Clock.Duration;
bool isPlaying = playback.Clock.IsPlaying;
bool isPaused = playback.Clock.IsPaused;
```

Pause, Resume과 Stop은 `Clock`이 아니라 `IAudioPlayback`에서 호출한다.

## Master와 Bus 제어

```csharp
var audio = AudioManager.Current;

audio.SetMasterVolume(0.8f);
audio.SetMuted(false);

audio.SetBusVolume(AudioBus.Music, 0.6f);
audio.SetBusMuted(AudioBus.Voice, true);

float musicVolume = audio.GetBusVolume(AudioBus.Music);
bool isVoiceMuted = audio.IsBusMuted(AudioBus.Voice);
```

Master·Bus 변경은 이미 재생 중인 Playback에도 즉시 반영된다. `AudioManager`의 `Bus Settings`에서 Bus별 초기 Volume, Mute와 선택적 `AudioMixerGroup`을 지정할 수 있다. 출력 Group이 비어 있으면 Unity 기본 Audio 출력 경로를 사용한다.

특정 Bus 또는 모든 Audio를 종료할 수 있다.

```csharp
audio.StopAll(AudioBus.SFX);
audio.StopAll();
```

## 선택적 접근

Audio Host가 없는 Scene도 허용한다면 `TryCurrent`를 사용한다.

```csharp
if (AudioManager.TryCurrent(out var audio))
{
    audio.Play(uiClickCue);
}
```

별도 Singleton Slot을 사용하도록 Host를 구성했다면 이름으로 접근할 수 있다.

```csharp
AudioManager.Named["Preview"].Play(previewCue);
```

## CuePlaybackService와 함께 사용

Audio Cue를 Dialogue, VFX 등 다른 Cue와 같은 Session에서 묶어 종료해야 할 때 `AudioManager`를 `CuePlaybackService`의 Player로 등록한다.

```csharp
var service = new CuePlaybackService
(
    new ICuePlayer[] { AudioManager.Current }
);

ICuePlayback playback = service.Play(audioCue);

service.Tick();
service.StopAll();
```

단순한 SFX·BGM 재생은 `AudioManager.Current.Play`를 직접 사용하는 것으로 충분하다.

## 현재 제공 범위

기본 구현은 `UnityAudioClipCue`, DSP 예약 재생과 `MusicLayerGroup` 기반 동기 Layer Playback을 Unity `AudioSource` backend로 지원한다. BGM 선곡, Cross Fade, Adaptive Music, Dialogue 진행과 FMOD·Wwise 연동은 Consumer 프로젝트의 상위 서비스 또는 선택적 Adapter에서 구성한다.

## 관련 문서

- [Playback](../../README.md)
- [소유권과 수명](../../../../Documentation~/concepts/ownership-and-lifetime.md)
