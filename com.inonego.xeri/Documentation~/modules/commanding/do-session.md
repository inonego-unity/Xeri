# Commanding과 DoSession

Commanding은 Command Pattern 기반으로 실행 history를 관리하고 Undo/Redo를 제공하는 Xeri Runtime 모듈입니다.
핵심 타입은 `IDoCommand`와 `DoSession`이며, 기존 DoSession 패키지의 공개 타입과 동작을 Xeri namespace로 편입합니다.

## 왜 필요한가

Undo/Redo가 필요한 시스템이 각자 stack, 이전 값, 그룹 처리와 이벤트를 구현하면 동일한 history 규칙이 여러 곳에 반복됩니다.
Commanding은 실제 변경을 Command가 소유하고, history 이동은 `DoSession`이 소유하도록 역할을 분리합니다.

## 언제 사용하는가

- Runtime 편집기나 사용자 제작 도구에서 변경을 되돌려야 할 때
- 인벤토리, 설정, 배치 같은 서로 다른 기능의 history를 독립적으로 유지할 때
- 여러 변경을 하나의 Undo 단계로 묶어야 할 때
- Command 실행 이후 UI나 로그를 history 이벤트에 연결할 때

## 기본 Command

```csharp
using UnityEngine;

using inonego.Xeri.Commanding;

public sealed class MoveCommand : IDoCommand
{
    public bool CanUndo => target != null;
    public string Desc => "Move object";

    private readonly Transform target;
    private readonly Vector3 oldPosition;
    private readonly Vector3 newPosition;

    public MoveCommand(Transform target, Vector3 newPosition)
    {
        this.target = target;
        oldPosition = target.position;
        this.newPosition = newPosition;
    }

    public void Do() => target.position = newPosition;
    public void Undo() => target.position = oldPosition;
}

var session = new DoSession();
session.Do(new MoveCommand(transform, new Vector3(1, 2, 3)));
session.Undo();
session.Redo();
```

`CanUndo`가 `false`이면 해당 Command는 Undo barrier로 동작합니다. stack에서 제거되지 않으며 더 오래된 history로도 넘어가지 않습니다.

## 간단한 변경과 제공 Command

Command 타입을 따로 만들 필요가 없는 경우 Action overload를 사용할 수 있습니다.

```csharp
var oldPosition = transform.position;
var nextPosition = new Vector3(1, 2, 3);

session.Do(
    () => transform.position = nextPosition,
    () => transform.position = oldPosition,
    "Change position"
);
```

제공 Command는 다음과 같습니다.

| 타입 | 용도 |
|---|---|
| `DoPropertyCommand<TTarget, TValue>` | getter/setter를 이용한 값 변경 |
| `DoCollectionCommand<T>` | `ICollection<T>`의 Add/Remove |
| `DoDictionaryCommand<TKey, TValue>` | `IDictionary<TKey, TValue>`의 Add/Remove |

## 그룹

여러 변경을 하나의 Undo 단계로 묶으려면 `BeginGroup()`과 `EndGroup()`을 사용합니다.

```csharp
session.BeginGroup("Full reset");
session.Do(() => hp = 100, () => hp = oldHp, "Reset HP");
session.Do(() => mp = 50, () => mp = oldMp, "Reset MP");
session.EndGroup();

session.Undo();
```

그룹 안의 각 Command는 `Do()` 호출 시 즉시 실행됩니다. `EndGroup()`이 호출되면 누적된 Command가 하나의 history 항목으로 기록됩니다.
중첩 그룹은 지원하지 않으며 이미 그룹이 열린 상태에서 `BeginGroup()`을 호출하면 예외가 발생합니다.

## 독립 세션과 history 제한

```csharp
var editorSession = new DoSession { MaxSize = 200 };
var inventorySession = new DoSession { MaxSize = 50 };
```

각 `DoSession`은 자신의 Undo/Redo history를 소유합니다. 관련 없는 기능의 history를 분리해야 하면 세션도 분리합니다.
`MaxSize`의 기본값은 `100`이며 새 `Do()`가 발생하면 기존 Redo history는 모두 제거됩니다.

## history 조회와 이벤트

```csharp
var nextUndo = session.PeekUndo;
var nextRedo = session.PeekRedo;
var undoHistory = session.UndoHistory;
var redoHistory = session.RedoHistory;
var undoCount = session.UndoCount;
var redoCount = session.RedoCount;

session.OnDo += command => Debug.Log(command.Desc);
session.OnUndo += command => Debug.Log(command.Desc);
session.OnRedo += command => Debug.Log(command.Desc);
session.OnChange += () => RefreshHistoryUI();
```

`Clear()`, `ClearUndo()`, `ClearRedo()`로 history를 비울 수 있습니다.

## 제약과 주의사항

- `DoSession`은 main thread 사용을 전제로 합니다.
- `DoCollectionCommand<T>`는 `ICollection<T>`의 Add/Remove를 단순 반전하므로 순서나 중복 상태까지 복원하는 계약은 아닙니다.
- `DoDictionaryCommand.Remove()`는 Command 생성 시점에 해당 key의 현재 값을 읽으므로 key가 없으면 원래 `IDictionary` 동작에 따라 예외가 발생합니다.
- `CanUndo == false`인 Command는 제거되지 않고 Undo barrier가 됩니다.
- 그룹을 시작한 뒤에는 반드시 `EndGroup()`으로 종료해야 합니다.

## 관련 문서

- [Xeri Commanding Runtime README](../../../Runtime/Commanding/README.md)
- [Runtime 모듈](../index.md)
- [모듈 선택 가이드](../../getting-started/choosing-modules.md)
