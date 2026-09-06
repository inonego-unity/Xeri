# Document Workspace 구성하기

이 가이드는 직렬화 가능한 문서 Body를 만들고 Xeri Document Workspace에서 Create, Edit, Save, Close하는 최소 흐름을 설명합니다.

## 목적

파일 IO, 문서 Session, 사용자 Save/Close 흐름과 UI를 분리해서 같은 문서 작업 규칙을 EditorWindow, Runtime Tool, 테스트에서 재사용합니다.

## 1. 문서 Body 정의

Body는 프로젝트가 실제로 편집하고 저장할 데이터입니다.

```csharp
using System;

[Serializable]
public sealed class SampleDocumentBody
{
    public string Title;
    public string Content;
}
```

selection, scroll, focused tab 같은 View 상태는 Body에 넣지 않는 편이 좋습니다.

## 2. Handler 구성

Body 자체를 JSON root로 저장하는 가장 단순한 file handler는 다음처럼 만들 수 있습니다.
```csharp
using inonego.Xeri.Serializable;
using inonego.Xeri.Workspace.Document;

var handler = BodySerializedHandler.CreateForFile<SampleDocumentBody>
(
    "sample.document",
    "1",
    UnityJsonSerializer.Pretty,
    name => new SampleDocumentBody { Title = name }
);
```

파일 내부에 Xeri metadata까지 저장해야 한다면 `EnvelopeSerializedHandler`를 선택합니다.

## 3. Workspace와 Controller 조립

```csharp
var workspace = new DocumentWorkspace();
var service = new DocumentWorkspaceService
(
    workspace,
    new IDocumentHandler[] { handler }
);
var controller = new DocumentWorkspaceController(service);
```

`Service`는 저수준 실행을, `Controller`는 사용자-facing Save/Close 의미를 해석합니다.
## 4. 새 문서 생성과 편집

```csharp
var created = controller.Create("sample.document", "Untitled");
if (!created.Success) return;

var session = (IDocumentSession<SampleDocumentBody>)created.Session;
session.Body.Content = "Changed";
session.SetDirty();
```

Body 변경 감지는 자동이 아닙니다. 편집한 계층이 `SetDirty()`를 호출합니다.

## 5. Save와 위치 선택

새 문서는 아직 `Location`이 없으므로 첫 `Save()`는 위치가 필요하다는 flow를 반환합니다.

```csharp
var save = controller.Save(session);

if (save.NeedLoc)
{
    save = controller.SaveAs
    (
        session,
        new FileDocumentLocation("C:/Temp/sample.json")
    );
}
```

파일 패널을 실제로 여는 것은 프로젝트 UI 책임입니다.
## 4. 문서 생성과 편집

```csharp
var create = controller.Create("sample.document", "Untitled");
if (!create.Success)
{
    return;
}

var session = (IDocumentSession<SampleDocumentBody>)create.Session;
session.Body.Content = "Edited";
session.SetDirty();
```

Body 변경은 자동 dirty 감지 대상이 아닙니다. 편집을 수행한 계층이 명시적으로 `SetDirty()`를 호출합니다.

## 5. 저장

새 문서는 아직 기준 `Location`이 없으므로 `Save()`가 위치 입력 필요 상태를 반환할 수 있습니다.

```csharp
var save = controller.Save(session);
if (save.NeedLoc)
{
    var location = new FileDocumentLocation("C:/Temp/sample.json");
    save = controller.SaveAs(session, location);
}
```
`SaveAs`는 저장 성공 뒤 Session의 기준 Location을 새 위치로 변경하고 dirty를 해제합니다. 반대로 `SaveTo`는 같은 Handler 포맷으로 다른 위치에 복사하지만 기준 Location과 dirty 상태를 유지합니다.

## 6. 닫기

```csharp
var close = controller.Close(session);
if (close.PendingUser)
{
    // 프로젝트 UI가 저장/폐기/취소를 사용자에게 묻는다.
}
```

Dirty Session은 Controller가 임의로 닫지 않습니다. 사용자 확인이 끝나고 변경을 폐기하기로 했다면 `CloseDiscardingChanges(session)`을 호출합니다.

## 7. Recovery

Workspace 자체를 저장 포맷으로 사용하지 않고, Host 재생성이나 domain reload를 넘기기 위한 recovery record만 별도로 만들 수 있습니다.

```csharp
var record = service.RecordRecovery();
if (record.Success)
{
    string recoveryText = record.Record;
    // EditorPrefs, SessionState, 임시 파일 등 Host가 보관한다.
}
```

복구 시에는 같은 Service의 `Recover(recoveryText)`를 사용합니다. Recovery는 일반 `Open`/`Save`를 대체하지 않습니다.

## 책임 분리

- `Handler`: 문서 타입별 Create/Open/Save/Recovery 규칙
- `Workspace`: 열린 Session 목록
- `Service`: 저수준 실행과 성공 후 상태 반영
- `Controller`: `NeedLoc`, `PendingUser`, `AlreadyOpen` 같은 사용자 흐름 해석
- 프로젝트 UI: 파일 선택 창, 저장 확인, 탭/포커스 상태

이 경계를 유지하면 파일 패널이나 EditorWindow 없이도 Document Core를 테스트하고 재사용할 수 있습니다.

## 관련 문서

- [Workspace Document](../../../Runtime/Workspace/Document/README.md)
- [Workspace](../../../Runtime/Workspace/README.md)
- [IO](../../../Runtime/IO/README.md)
- [Serializer](../../modules/serialization/serializer.md)
- [Xeri 통합 패턴](../../concepts/integration-patterns.md)
