# 설치와 요구 환경

Xeri는 Unity 6 계열 프로젝트에서 사용하는 UPM 패키지입니다. 패키지 전체를 설치한 뒤 필요한 모듈만 선택해서 사용하는 방식을 전제로 합니다.

## 요구 Unity 버전

현재 `package.json`의 최소 Unity 버전은 `6000.0`입니다.

## 패키지 의존성

Xeri 패키지는 다음 Unity Package를 직접 의존합니다.

| 패키지 | 현재 요구 버전 | 사용 영역 |
|---|---:|---|
| Addressables | `2.8.0` | IO, GameObject Provider 등 |
| Input System | `1.18.0` | Game UI 입력 |
| UGUI | `2.6.0` | Game UI와 UGUI adapter |

Game UI Runtime은 현재 DOTween과 `DOTween.Modules`를 직접 사용합니다. DOTween은 UPM `package.json`에 포함되지 않으므로 Game UI를 사용하는 프로젝트가 별도로 준비해야 합니다.

## 로컬 패키지로 연결

같은 저장소나 외부 디렉터리의 Xeri를 개발 중이라면 프로젝트 `Packages/manifest.json`에서 file dependency로 연결할 수 있습니다.

```json
"com.inonego.xeri": "file:../External/UniXeri/com.inonego.xeri"
```

경로는 프로젝트 구조에 맞게 조정합니다.
## 설치 후 확인

패키지 설치만으로 모든 Runtime이 자동 생성되는 것은 아닙니다. 시스템마다 초기화 방식이 다릅니다.

- Core/Serializable/Generation 같은 순수 Runtime 타입은 필요한 위치에서 직접 생성합니다.
- `Localization`은 기본 슬롯을 자동 등록합니다.
- Game UI와 Audio처럼 Host가 필요한 기능은 Bootstrapper Module 또는 프로젝트 Host를 통해 초기화합니다.
- Addressables 자원을 읽는 기능은 호출자가 해당 자원의 수명 계약을 함께 관리해야 합니다.

Game UI를 사용한다면 먼저 [Game UI 설정과 시작](../modules/game-ui/setup.md)을 확인합니다.

## 관련 문서

- [첫 설정](first-setup.md)
- [모듈 선택 가이드](choosing-modules.md)
- [Xeri 통합 패턴](../concepts/integration-patterns.md)