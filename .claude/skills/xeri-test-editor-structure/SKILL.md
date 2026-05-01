---
name: xeri-test-editor-structure
description: UniXeri(com.inonego.xeri) 패키지에서 테스트 파일(Edit Mode / Play Mode) 또는 Unity 에디터 확장 코드를 생성할 때 사용.
user-invocable: false
---

# UniXeri 테스트 / 에디터 파일 구조

## 개요

asmdef는 이미 생성 완료. 새 테스트·에디터 코드를 추가할 때는 asmref만 추가한다.
모든 폴더는 내용이 생길 때 만든다 — 미리 생성 금지.

---

## 기존 asmdef 위치

```
Tests/
├── EDIT/   → inonego.Xeri.TEST.EDIT.asmdef        (런타임 Edit Mode)
├── PLAY/   → inonego.Xeri.TEST.PLAY.asmdef        (런타임 Play Mode)
└── Editor/ → inonego.Xeri.Editor.TEST.EDIT.asmdef (에디터 Edit Mode)
```

한 폴더에 asmdef 하나. 새 어셈블리가 필요하면 폴더를 분리해서 추가한다.

---

## 소스 모듈 옆 폴더 패턴 (asmref)

```
Runtime/{모듈}/
├── {소스}.cs
├── TEST/
│   ├── EDIT/   → inonego.Xeri.TEST.EDIT.asmref      (Edit Mode 테스트)
│   ├── PLAY/   → inonego.Xeri.TEST.PLAY.asmref      (Play Mode 테스트)
│   └── Editor/ → inonego.Xeri.Editor.TEST.EDIT.asmref (에디터 테스트)
└── Editor/     → Editor.asmref                       (에디터 확장)
```

**에디터 코드는 Play Mode 참조 불가** → `TEST/Editor/`에 Play asmref 없음.

---

## asmref 규칙

| 파일 경로 | 파일명 | reference 값 |
|---|---|---|
| `TEST/EDIT/` | `inonego.Xeri.TEST.EDIT.asmref` | `inonego.Xeri.TEST.EDIT` |
| `TEST/PLAY/` | `inonego.Xeri.TEST.PLAY.asmref` | `inonego.Xeri.TEST.PLAY` |
| `TEST/Editor/` | `inonego.Xeri.Editor.TEST.EDIT.asmref` | `inonego.Xeri.Editor.TEST.EDIT` |
| `Editor/` | `Editor.asmref` | `inonego.Xeri.Editor` |

**파일 내용 형식 (GUID 사용 금지, 이름 직접 사용):**
```json
{ "reference": "{어셈블리명}" }
```

---

## Unity Test Runner 설정

테스트가 Test Runner에 표시되려면 사용 프로젝트의 `Packages/manifest.json`에 추가:

```json
{
  "testables": ["com.inonego.xeri"]
}
```
