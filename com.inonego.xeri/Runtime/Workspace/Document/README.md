# Xeri Workspace Document

`Runtime/Workspace/Document`는 Unity Editor와 Runtime 양쪽에서 재사용할 수 있는 문서 편집 기반입니다.
문서 설명 정보, 실제 편집 데이터, 열린 작업 상태, 저장 위치, 저장 흐름을 분리해서 여러 종류의 editor나 runtime tool이 같은 core 흐름을 사용할 수 있게 합니다.

이 문서는 API 레퍼런스와 구현 가이드의 중간 지점을 목표로 합니다.
세부 구현은 코드와 테스트를 기준으로 보고, 여기서는 어떤 개념을 어디에 두어야 하는지와 확장 시 지켜야 할 계약을 설명합니다.

## 핵심 모델

```text
Document   = 문서 타입, 버전, 표시 이름 같은 설명 정보
Model      = 실제 편집 대상 데이터
Location   = 문서를 열거나 저장할 위치 또는 참조
Session    = 열린 문서의 document, model, location, dirty 상태
Handler    = 문서 타입별 create/open/save 처리 규칙
Workspace  = 열린 session 목록
Service    = 저수준 create/open/save/close 실행
Controller = 사용자-facing open/save/close 흐름 해석
```

이 계층은 view, editor UI, undo/redo, domain reload 복구를 직접 책임지지 않습니다.
그 기능들은 실제 요구가 생긴 뒤 이 core 위에 별도 계층으로 붙입니다.

## 기본 사용 흐름

가장 낮은 실행 흐름은 `DocumentWorkspaceService`에서 시작합니다.

```csharp
var workspace = new DocumentWorkspace();
var handler = new MyDocumentHandler();
var service = new DocumentWorkspaceService(workspace, new[] { handler });

var create = service.Create("my.document", "Untitled");
if (!create.Success) return;

var session = create.Session;
```

`Create`는 아직 저장 위치가 없는 session을 만들 수 있습니다.
이 경우 `session.Location`은 `null`이고, 사용자가 저장을 원하면 Save As 흐름으로 이어져야 합니다.

문서를 열 때는 문서 종류와 location을 함께 넘깁니다.

```csharp
var loc = new FileDocumentLocation("C:/Temp/sample.mydoc");
var open = service.Open("my.document", loc);
if (!open.Success) return;

var session = open.Session;
```

`Open`은 같은 문서 종류와 같은 location의 session이 이미 열려 있으면 기존 session을 반환합니다.
이때 `IDocumentLocation.Equals`는 "같은 외부 위치를 가리키는가"를 판단하는 기준이며, session 자체의 identity는 아닙니다.

## Service와 Controller

`DocumentWorkspaceService`는 저수준 실행 API입니다.
입력이 부족하거나 처리할 수 없으면 실패를 반환하고, 사용자에게 무엇을 물어볼지는 결정하지 않습니다.

`DocumentWorkspaceController`는 사용자 흐름을 해석합니다.
예를 들어 새 문서에서 사용자가 Save를 누르면 service 기준으로는 location이 없어 저장 실패지만, controller는 `NeedLoc`을 반환합니다.

```csharp
var controller = new DocumentWorkspaceController(service);

var save = controller.Save(session);
if (save.NeedLoc)
{
   var loc = new FileDocumentLocation("C:/Temp/sample.mydoc");
   save = controller.SaveAs(save.Session, loc);
}
```

Controller는 UI를 직접 띄우지 않습니다.
`NeedLoc`, `PendingUser`, `AlreadyOpen` 같은 결과를 보고 파일 패널, 확인 창, tab focus를 처리하는 책임은 view/editor 쪽에 있습니다.

## 저장 흐름

저장 흐름은 세 가지로 나뉩니다.

| 흐름 | 의미 | session location | dirty |
|---|---|---|---|
| `Save` | 현재 기준 location에 저장 | 유지 | 성공 시 해제 |
| `SaveAs` | 새 기준 location으로 저장 | 새 location으로 변경 | 성공 시 해제 |
| `SaveTo` | 다른 location에 복사 저장 | 유지 | 유지 |

`SaveTo`는 export와 비슷해 보일 수 있지만, 현재 document handler의 native save 계약을 그대로 사용합니다.
다른 포맷으로 변환하는 import/export는 별도 요구가 생긴 뒤 분리합니다.

## Dirty 규칙

모델을 수정한 쪽이 명시적으로 session을 dirty로 표시합니다.

```csharp
var model = (MyDocumentModel)session.Model;
model.Value.Name = "Changed";
session.SetDirty();
```

기본 규칙:

- 저장되는 model 또는 document metadata 변경은 dirty 대상입니다.
- selection, scroll, zoom, focused tab 같은 view 상태는 기본 dirty 대상이 아닙니다.
- `SetDirty()` 중복 호출은 문제로 보지 않습니다.
- 저장 성공 후 dirty 해제는 service가 처리합니다.

## Handler 구현 규칙

문서 타입 하나는 하나의 `IDocumentHandler`가 담당합니다.
Handler는 type별 처리 규칙이며, 개별 session 상태를 내부에 저장하지 않습니다.

```csharp
public sealed class MyDocumentHandler : IDocumentHandler
{
   public string TypeID => "my.document";

   public DocumentCreateResponse Create(string name)
   {
      var document = new Document(TypeID, "1", name);
      var model = new MyDocumentModel();
      var session = new DocumentSession(document, model, null);

      return DocumentCreateResponse.Succeed(session);
   }

   public bool CanOpen(IDocumentLocation location)
   {
      return location is FileDocumentLocation;
   }

   public DocumentOpenResponse Open(IDocumentLocation location)
   {
      // location에서 데이터를 읽고 model을 만든 뒤, 같은 location을 가진 session을 반환합니다.
      return DocumentOpenResponse.Fail("구현 필요");
   }

   public bool CanSave(IDocumentSession session, IDocumentLocation location)
   {
      return session?.Document?.TypeID == TypeID
         && session.Model is MyDocumentModel
         && location is FileDocumentLocation;
   }

   public DocumentSaveResponse Save(IDocumentSession session, IDocumentLocation location)
   {
      // model을 location에 저장합니다. session 상태는 변경하지 않습니다.
      return DocumentSaveResponse.Fail("구현 필요");
   }
}
```

Handler가 성공 응답으로 반환하는 session은 core가 사용할 수 있는 최소 계약을 만족해야 합니다.

- `Create` 성공 session은 `Document`와 `Model`을 가져야 합니다.
- `Open` 성공 session은 `Document`, `Model`, 요청 location과 같은 `Location`을 가져야 합니다.
- 반환 session의 `Document.TypeID`는 handler의 `TypeID`와 같아야 합니다.
- `Save`는 session의 `Location`이나 `IsDirty`를 직접 바꾸지 않습니다.
- `Save`, `SaveAs`, `SaveTo`의 상태 전이는 `DocumentWorkspaceService`가 결정합니다.

`CanOpen`과 `CanSave`는 handler 선택이 아니라, 이미 선택된 handler가 입력을 처리할 수 있는지 확인하는 계약입니다.
Handler 선택은 `TypeID`로 이루어집니다.

## Serialized handler

문자열 serializer 기반 문서라면 `SerializedDocumentHandler<TModel, TLocation>` 또는 `FileSerializedDocumentHandler<TModel>`를 상속해서 시작할 수 있습니다.

```csharp
public sealed class MyFileDocumentHandler : FileSerializedDocumentHandler<MyDocumentModel>
{
   public MyFileDocumentHandler(ISerializer serializer)
      : base("my.document", "1", serializer)
   {

   }

   protected override MyDocumentModel CreateModel(string name)
   {
      return new MyDocumentModel();
   }
}
```

주의할 점:

- `SerializedDocumentHandler`는 `TModel` 자체를 serialize/deserialize 합니다.
- concrete model은 주입한 serializer가 처리할 수 있는 구조여야 합니다.
- Unity JSON 기반 serializer라면 `[Serializable]`, `[SerializeField]` 필드 구조가 필요할 수 있습니다.
- document location과 실제 IO location이 다르면 `TryMapIOLocation`에서 변환합니다.

## Workspace 책임

`DocumentWorkspace`는 열린 session 목록과 add/remove 이벤트만 관리합니다.

```csharp
foreach (var session in workspace.Sessions)
{
   if (session.IsDirty)
   {
      // 저장 확인 대상
   }
}
```

Workspace가 직접 하지 않는 것:

- active session 관리
- selected/focused session 관리
- save/open 의미 해석
- dirty session 목록 캐싱
- view state 저장
- file dialog나 사용자 확인 UI

## 확장 기준

새 문서 타입은 우선 `IDocumentHandler` 구현으로 추가합니다.
새 저장 위치는 `IDocumentLocation` 구현으로 추가합니다.
파일, 메모리, Unity object 같은 location 종류는 handler가 지원 여부를 판단합니다.

다음 기능은 core에 미리 넣지 않고 실제 요구가 생겼을 때 추가합니다.

- import/export
- undo/redo edit layer
- domain reload restore data
- view/editor state
- resolver 기반 자동 문서 타입 판별

## AI 작업 가이드

AI가 이 영역을 수정하거나 확장할 때는 다음 순서로 판단합니다.

- 변경하려는 값이 document 설명인지, model 데이터인지, session 상태인지, location인지 먼저 구분합니다.
- 저장 실행은 service, 사용자 분기는 controller에 둡니다.
- 새 추상화를 만들기 전에 기존 `IDocumentHandler`, `IDocumentLocation`, `IDocumentSession` 조합으로 가능한지 확인합니다.
- `SaveAs`는 session 기준 location을 바꾸고 dirty를 해제합니다.
- `SaveTo`는 session 기준 location과 dirty를 유지합니다.
- 모델 변경은 자동 감지하지 않습니다. 변경한 쪽이 `SetDirty()`를 호출합니다.
- view state, resolver, importer/exporter, undo/redo, domain reload는 실제 요구가 생긴 뒤 추가합니다.

잘못된 방향:

```text
Document가 model 데이터를 직접 소유
Model이 session이나 workspace를 참조
Handler.Save가 session.SetLocation 또는 ClearDirty 호출
Workspace가 active session을 강제로 하나만 관리
SaveTo가 SaveAs처럼 session location을 바꿈
IO나 serializer 책임을 DocumentWorkspaceService에 넣음
```
