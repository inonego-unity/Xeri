# Xeri 문서 사이트 유지보수

이 문서는 Xeri의 DocFX 사이트 빌드, API Reference snapshot과 GitHub Pages 배포 파이프라인을 유지보수하는 개발자용 문서입니다.

사용자-facing 설명은 루트 README나 `com.inonego.xeri/Documentation~`에 작성하고, 빌드·배포 운영 절차는 이 문서에만 둡니다.

## 구성

```text
Manual Markdown
    +
Generated API metadata snapshot
    ↓
DocFX
    ↓
_site/
    ↓
GitHub Pages
```

관련 파일:

- `docfx.json`: DocFX metadata/build 설정
- `dotnet-tools.json`: 저장소 로컬 DocFX 버전
- `build-docs.ps1`: 로컬 API metadata와 사이트 빌드
- `Docs/generated-api.zip`: CI용 API metadata snapshot
- `Docs/generated-api.sha256`: snapshot 최신성 검증값
- `.github/workflows/docs-pages.yml`: GitHub Pages 배포 workflow

## 로컬 빌드

Unity 개발 환경에서 Manual과 API Reference를 함께 갱신하려면 저장소 루트에서 실행합니다.

```powershell
.\build-docs.ps1
```

이 명령은 Unity가 생성한 `inonego.Xeri*.csproj`를 읽어 API metadata를 만들고, CI에서 사용할 snapshot을 갱신한 뒤 `_site/`를 빌드합니다.

로컬 서버까지 실행하려면:

```powershell
.\build-docs.ps1 -Serve
```

Unity-generated `.csproj`, `.asmdef`, `package.json`, C# 소스는 DocFX 빌드가 수정하지 않습니다.

## API snapshot

GitHub hosted runner에는 Unity가 설치되어 있지 않으므로 API metadata를 CI에서 직접 재생성하지 않습니다.

`build-docs.ps1`은 로컬 metadata를 `Docs/generated-api.zip`으로 저장하고, Runtime/Editor C#, asmdef와 `package.json`의 내용에서 `Docs/generated-api.sha256`을 생성합니다.

public API 또는 assembly/package 구성이 바뀌었다면 반드시 로컬에서 `build-docs.ps1`을 실행해 두 파일을 함께 갱신합니다.

## CI와 동일한 검증

snapshot이 현재 소스와 일치하는지 확인합니다.

```powershell
pwsh .\build-docs.ps1 -VerifySnapshot
```

Unity 없이 snapshot만으로 사이트가 만들어지는지 확인합니다.

```powershell
pwsh .\build-docs.ps1 -SkipMetadata
```

## GitHub Pages

`.github/workflows/docs-pages.yml`은 `main` push 또는 수동 실행에서 다음을 수행합니다.

```text
Checkout
→ API snapshot 최신성 검증
→ snapshot 기반 DocFX build
→ _site artifact 업로드
→ GitHub Pages 배포
```

현재 Pages 사이트는 다음 주소를 사용합니다.

https://inonego-unity.github.io/Xeri/

Pages는 GitHub Actions workflow 모드로 구성하며 `_site/`와 `.docfx/`는 Git에 포함하지 않습니다.
