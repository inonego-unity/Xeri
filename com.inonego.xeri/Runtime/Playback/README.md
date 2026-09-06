# Xeri Playback

Xeri Playback은 시간에 따라 시작되고 종료되는 Cue 재생을 공통 계약으로 다루는 Runtime 영역입니다. 현재 Cue service, binding, clock과 Unity Audio 기반 구현을 포함합니다.

## 개요

Playback core는 재생 대상의 구체 backend보다 Cue, Player, Playback 수명과 시간 조회 계약을 중심으로 구성됩니다.

## 왜 필요한가

Audio, Particle, VFX처럼 서로 다른 backend를 프로젝트 사건 코드가 직접 호출하면 재생 시작·중지·수명 추적 방식이 시스템마다 달라집니다. Playback은 재사용 가능한 Cue 정의와 이번 실행의 Binding, Player 선택, Playback 수명을 공통 계약으로 분리합니다.

## 언제 사용하는가

- 여러 재생 backend를 같은 Presenter/Timeline에서 공통 처리할 때
- 실행마다 달라지는 대상·위치와 재사용 가능한 Cue 설정을 분리할 때
- 생성된 Playback을 상위 수명에서 추적·종료해야 할 때

단순 Audio만 필요하면 [Xeri Audio](Cue/Audio/README.md)부터 시작하고, 여러 Cue를 공통화하려면 [Playback Cue](../../Documentation~/modules/playback/cue.md)를 봅니다.

## 어디서 시작하는가

Unity Audio Host와 Cue authoring이 목적이면 [Xeri Audio](Cue/Audio/README.md), Audio/VFX/Particle을 같은 Presenter에서 공통 실행하려면 [Playback Cue](../../Documentation~/modules/playback/cue.md)와 [Cue Presenter 구성하기](../../Documentation~/guides/playback/create-cue-presenter.md)에서 시작합니다.

## 책임 범위

### 담당하는 것

- `IPlaybackCue`, `ICuePlayer`, `ICuePlayback` 계약
- Cue와 대상의 binding
- `PlaybackClock` 기반 재생 시간 조회
- Audio Cue와 `AudioManager` 기반 Unity Audio 재생
- 여러 Cue를 공통 service에서 시작·정리하는 흐름

### 담당하지 않는 것

- 프로젝트별 BGM 선곡이나 전투 Mood 판단
- Dialogue 진행 정책
- FMOD/Wwise 같은 외부 backend를 기본 구현으로 강제하는 것

## 핵심 개념

```text
Cue -> Player -> Playback
          ↓
       Clock / Binding
```

## 상세 문서

- [Playback Cue](../../Documentation~/modules/playback/cue.md)
- [Xeri Audio](Cue/Audio/README.md)
- [소유권과 수명](../../Documentation~/concepts/ownership-and-lifetime.md)
