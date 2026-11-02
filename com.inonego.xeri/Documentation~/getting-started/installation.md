# 설치와 요구 환경

Xeri는 Unity 6 계열 프로젝트에서 사용하는 UPM foundation package입니다.

## 요구 Unity 버전

현재 `package.json`의 최소 Unity 버전은 `6000.0`입니다.

## 패키지 의존성

Xeri package는 다음 Unity Package를 직접 의존합니다.

| 패키지 | 현재 요구 버전 | 사용 영역 |
|---|---:|---|
| Addressables | `2.8.0` | GameObject Provider 등 |
| Input System | `1.18.0` | package PlayMode 검증과 입력 기반 테스트 |
| UGUI | `2.6.0` | Drag & Drop UGUI adapter |

## 선택적 외부 연동

- `Newtonsoft.Json` 3.0.0 이상이 프로젝트에 설치돼 있으면 `inonego.Xeri` Assembly Definition의 `XERI_NEWTONSOFT_JSON` 심볼이 자동 정의됩니다. 이때 `XVec`의 `[JsonProperty]` 어트리뷰트를 포함합니다. 설치되지 않아도 Unity 및 XML 직렬화는 그대로 컴파일됩니다.
- `TweenHelper`는 DOTween이 설치되고 프로젝트의 `DOTWEEN` 심볼이 활성화된 경우 `DG.Tweening.Ease`를 사용합니다. 미설치 환경에서는 Xeri의 `Ease.None`을 사용하며 Core Assembly는 `DOTween.Modules`를 필수 참조하지 않습니다. DOTween을 제거한 프로젝트에서는 해당 전역 심볼도 해제해야 합니다.
- VFX Graph 지원은 별도 `inonego.Xeri.VFXGraph` Assembly Definition이 해당 Unity 패키지의 설치 여부로 조건부 컴파일합니다.

## 로컬 패키지로 연결

```json
"com.inonego.xeri": "file:../External/UniXeri/com.inonego.xeri"
```

Application UI 기능도 사용하는 프로젝트는 Xeri UI package를 함께 연결합니다.

```json
"com.inonego.xeri.ui": "https://github.com/inonego-unity/Xeri-UI.git?path=/com.inonego.xeri.ui#main"
```

## 설치 후 확인

패키지 설치만으로 모든 Runtime이 자동 생성되는 것은 아닙니다.

- Core/Serializable/Generation 같은 순수 Runtime 타입은 필요한 위치에서 직접 생성합니다.
- Audio처럼 Host가 필요한 기능은 Bootstrapper Module 또는 프로젝트 Host를 통해 초기화합니다.
- Addressables 자원을 읽는 기능은 호출자가 해당 자원의 수명 계약을 함께 관리해야 합니다.
- Screen/Modal/Presentation/Window UI lifecycle은 Xeri UI package의 설정을 따릅니다.

## 관련 문서

- [첫 설정](first-setup.md)
- [모듈 선택 가이드](choosing-modules.md)
- [Xeri 통합 패턴](../concepts/integration-patterns.md)
- [Xeri UI Documentation](https://inonego-unity.github.io/Xeri-UI/)
