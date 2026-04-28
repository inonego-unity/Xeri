# com.inonego.xeri 개발 가이드

## 테스트 / 에디터 구조

### 어셈블리 정의 (이미 생성됨)

```
Tests/
├── EDIT/
│   └── inonego.Xeri.TEST.EDIT.asmdef              ← 런타임 Edit Mode 테스트
├── PLAY/
│   └── inonego.Xeri.TEST.PLAY.asmdef              ← 런타임 Play Mode 테스트
└── Editor/
    └── inonego.Xeri.Editor.TEST.EDIT.asmdef       ← 에디터 Edit Mode 테스트
```

한 폴더에 asmdef 하나만. 새 어셈블리가 필요하면 폴더를 분리해서 추가한다.

---

### 소스 코드 옆 폴더 구조 (asmref 패턴)

테스트·에디터 코드 모두 동일한 패턴. asmdef는 이미 있으므로 asmref만 추가한다.

모든 폴더는 내용이 생길 때 추가한다. 미리 만들지 않는다.

```
Runtime/{모듈}/
├── {소스}.cs
├── TEST/
│   ├── EDIT/                                        ← Edit Mode 테스트 있을 때
│   │   └── inonego.Xeri.TEST.EDIT.asmref
│   ├── PLAY/                                        ← Play Mode 테스트 있을 때
│   │   └── inonego.Xeri.TEST.PLAY.asmref
│   └── Editor/                                      ← 에디터 테스트 있을 때
│       └── inonego.Xeri.Editor.TEST.EDIT.asmref
└── Editor/                                          ← 에디터 확장 있을 때
    └── Editor.asmref
```

**asmref 파일명:** `{어셈블리명}.asmref`
**asmref 내용:** `{ "reference": "{어셈블리명}" }` — GUID 아닌 직접 이름 사용

| asmref 파일 | reference 값 |
|---|---|
| `TEST/EDIT/inonego.Xeri.TEST.EDIT.asmref` | `inonego.Xeri.TEST.EDIT` |
| `TEST/PLAY/inonego.Xeri.TEST.PLAY.asmref` | `inonego.Xeri.TEST.PLAY` |
| `TEST/Editor/inonego.Xeri.Editor.TEST.EDIT.asmref` | `inonego.Xeri.Editor.TEST.EDIT` |
| `Editor/Editor.asmref` | `inonego.Xeri.Editor` |

에디터 코드는 Play Mode 참조 불가이므로 `TEST/Editor/`에 Play 대응 asmref는 만들지 않는다.

---

### Unity Test Runner 설정

테스트가 Test Runner에 표시되려면 사용 프로젝트의 `Packages/manifest.json`에 추가:

```json
{
  "testables": ["com.inonego.xeri"],
  "dependencies": { ... }
}
```
