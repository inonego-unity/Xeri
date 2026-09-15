# Xeri Commanding

Xeri Commanding은 Command Pattern 기반으로 작업을 실행하고 Undo/Redo history를 관리하는 Runtime 모듈입니다.
기존 DoSession 패키지의 기능을 Xeri에 편입한 것으로, 각 시스템은 독립적인 `DoSession`을 만들거나 관련 기능끼리 세션을 공유할 수 있습니다.

## 왜 필요한가

상태 변경 코드가 직접 이전 값을 보관하고 Undo/Redo stack까지 관리하면 도메인 로직과 history 관리가 강하게 결합됩니다.
Commanding은 변경을 `IDoCommand`로 표현하고 `DoSession`이 실행 순서와 history 이동을 담당하도록 분리합니다.

## 언제 사용하는가

- Runtime 편집기나 도구에서 Undo/Redo가 필요할 때
- 여러 변경을 하나의 Undo 단위로 묶어야 할 때
- 서로 다른 시스템의 history를 독립적으로 관리해야 할 때
- 단순 변경을 lambda 기반 Command로 빠르게 등록할 때

## 기본 사용

```csharp
using inonego.Xeri.Commanding;

var session = new DoSession();

session.Do(command);
session.Undo();
session.Redo();
```

## 제공 Command

| 타입 | 역할 |
|---|---|
| `IDoCommand` | 실행, Undo 가능 여부와 설명을 정의하는 기본 계약 |
| `DoPropertyCommand<TTarget, TValue>` | getter/setter를 이용한 값 변경 |
| `DoCollectionCommand<T>` | `ICollection<T>` Add/Remove |
| `DoDictionaryCommand<TKey, TValue>` | `IDictionary<TKey, TValue>` Add/Remove |

간단한 작업은 별도 Command 타입 없이 `Do(Action, Action, string)` overload로 실행할 수 있습니다.

## 그룹과 history

`BeginGroup()`과 `EndGroup()` 사이의 Command는 실행 시점에는 각각 적용되지만 history에는 하나의 그룹으로 기록됩니다.

```csharp
session.BeginGroup("Reset");
session.Do(() => hp = 100, () => hp = oldHp, "Reset HP");
session.Do(() => mp = 50, () => mp = oldMp, "Reset MP");
session.EndGroup();
```

`MaxSize`는 보관할 Undo history의 최대 크기이며 기본값은 `100`입니다.
새 `Do()`가 발생하면 기존 Redo history는 제거됩니다.
`CanUndo == false`인 Command가 Undo stack 최상단에 있으면 그 지점이 barrier가 되어 이전 history로 넘어가지 않습니다.

## 이벤트와 조회

`OnDo`, `OnUndo`, `OnRedo`는 각각 해당 동작 이후 실행된 Command를 전달합니다.
`OnChange`는 history 변경을 알리며, `PeekUndo`, `PeekRedo`, `UndoHistory`, `RedoHistory`, `UndoCount`, `RedoCount`로 현재 상태를 조회할 수 있습니다.

## 책임 범위

Commanding은 Command 실행 순서와 Undo/Redo history를 관리합니다.
프로젝트별 Command의 실제 도메인 의미, 저장 형식, 네트워크 동기화나 replay 정책은 이 모듈이 결정하지 않습니다.

상세 사용법은 [Commanding 문서](../../Documentation~/modules/commanding/do-session.md)를 확인합니다.
