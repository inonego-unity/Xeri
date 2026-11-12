# Xeri Core 문서 사이트 유지보수

Xeri Core의 공개 Manual과 API Reference는 DocFX로 생성하고 GitHub Pages로 배포합니다.

## 문서 원본과 도구

- `com.inonego.xeri/Documentation~/`: Xeri Core Manual
- `docfx.json`, `index.md`, `toc.yml`: DocFX 설정과 사이트 내비게이션
- `dotnet-tools.json`: DocFX 도구 버전
- `build-docs.ps1`: API metadata 생성 및 DocFX 사이트 빌드
- `Docs/api-snapshot.ps1`: production 어셈블리별 metadata 프로젝트·snapshot 관리
- `Docs/generated-api.zip`, `Docs/generated-api.sha256`: CI용 API metadata와 소스 검증 해시
- `.github/workflows/docs-pages.yml`: GitHub Pages 배포

Entity·Spawn·Ability 등 게임 기능의 설명은 [Xeri Game 문서](https://inonego-unity.github.io/Xeri-Game/)에서 소유합니다. Core에는 범용 Runtime 기능과 다른 패키지로 향하는 링크만 유지합니다.

## 로컬 API Reference 갱신

Unity 개발 프로젝트에 현재 Xeri Core 패키지가 설치돼 있어야 하며, 해당 프로젝트에서 다음 파일을 생성한 상태여야 합니다.

- `inonego.Xeri.csproj`
- `inonego.Xeri.Editor.csproj`
- `inonego.Xeri.VFXGraph.csproj`

```powershell
./build-docs.ps1 -UnityProjectRoot "C:\Path\To\UnityProject"
```

위 명령은 Core의 production C#·asmdef·asmref만 대상으로 API 문서를 생성합니다. 테스트 코드는 API 문서에 포함하지 않습니다.

## 스냅샷과 CI

로컬 DocFX 빌드가 끝나면 `Docs/generated-api.zip`과 `Docs/generated-api.sha256`을 함께 갱신합니다. GitHub Actions는 Unity를 설치하지 않으므로 이미 생성된 snapshot을 사용합니다.

```powershell
./build-docs.ps1 -VerifySnapshot
./build-docs.ps1 -SkipMetadata
```

첫 명령은 snapshot이 현재 production 소스와 일치하는지 확인하고, 두 번째 명령은 저장된 metadata로 사이트를 빌드합니다. Core 공개 API의 코드 또는 어셈블리 구성이 변경됐다면 개발 환경에서 metadata와 snapshot을 모두 새로 생성해야 합니다.

Pages는 저장소의 `main` Push 또는 수동 실행에서 검증·빌드한 뒤 `_site/`를 배포합니다.

https://inonego-unity.github.io/Xeri/

`_site/`와 `.docfx/`는 생성물로 Git에 포함하지 않습니다.
