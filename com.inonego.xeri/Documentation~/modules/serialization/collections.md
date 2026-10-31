# Serializable Collections

Xeri Serializable Collections는 Unity 직렬화가 필요한 Dictionary, Set, Queue, Stack, 정렬 컬렉션과 우선순위 큐를 제공합니다. C#에서는 컬렉션 API를 사용하고 Inspector에서는 직렬화된 항목을 편집할 수 있습니다.

## 타입 선택

| 목적 | 타입 | 주요 API |
|---|---|---|
| Key로 Value 조회 | `XDictionary_VV<TKey, TValue>` 등 | `Add`, `Remove`, 인덱서, `TryGetValue` |
| 중복 없는 값 집합 | `XHashSet_V<T>`, `XHashSet_R<T>` | `Add`, `Remove`, `Contains` |
| FIFO 큐 | `XQueue_V<T>`, `XQueue_R<T>` | `Enqueue`, `Dequeue`, `Peek` |
| LIFO 스택 | `XStack_V<T>`, `XStack_R<T>` | `Push`, `Pop`, `Peek` |
| Order 순서의 항목 | `XOrdered<TOrder, TValue>` | `Add`, `Remove`, 인덱서 |
| Order와 고유 Key | `XOrdered<TOrder, TKey, TValue>` | `Add`, `ContainsKey`, `TryGetValue`, `TryGetEntry` |
| 높은 우선순위부터 처리 | `XPriorityQueue<TElement, TPriority>` | `Enqueue`, `Dequeue`, `Peek` |

Unity 직렬화가 필요하지 않다면 기본 .NET 컬렉션이 더 단순합니다.

## V와 R 직렬화 방식

`V`는 `[SerializeField]`, `R`은 `[SerializeReference]`를 뜻하며 성능 등급이 아닙니다. 인터페이스나 추상 클래스 같은 다형 관리 참조가 필요한 자리에만 `R`을 선택하세요.

| Dictionary 타입 | Key | Value |
|---|---|---|
| `XDictionary_VV<TKey, TValue>` | SerializeField | SerializeField |
| `XDictionary_VR<TKey, TValue>` | SerializeField | SerializeReference |
| `XDictionary_RV<TKey, TValue>` | SerializeReference | SerializeField |
| `XDictionary_RR<TKey, TValue>` | SerializeReference | SerializeReference |

`XHashSet`, `XQueue`, `XStack`에도 각각 `_V<T>`, `_R<T>`가 있습니다. `XOrdered`의 `Value`와 `XPriorityQueue`의 `Element`는 `SerializeReference`이며 클래스 형식을 사용합니다.

## C#에서 사용하기

다음 예제는 `using inonego.Xeri.Serializable;`을 사용합니다.

### Dictionary와 Set

```csharp
var scores = new XDictionary_VV<string, int>
{
    ["player"] = 100,
    ["enemy"] = 30,
};
scores["player"] += 10;

var tags = new XHashSet_V<string>();
tags.Add("Boss");
bool isBoss = tags.Contains("Boss");
```

### Queue, Stack, Priority Queue

```csharp
var events = new XQueue_V<string>();
events.Enqueue("Start");
events.Enqueue("Finish");
string firstEvent = events.Dequeue(); // "Start"

var history = new XStack_V<string>();
history.Push("First");
history.Push("Second");
string latest = history.Pop(); // "Second"

var tasks = new XPriorityQueue<string, int>();
tasks.Enqueue("Background", 1);
tasks.Enqueue("Urgent", 10);
var next = tasks.Dequeue(); // ("Urgent", 10)
```

`XPriorityQueue`는 **높은 Priority를 먼저** 반환합니다. 빈 큐의 `Peek()`와 `Dequeue()`는 `InvalidOperationException`을 발생시킵니다.

### Order를 갖는 컬렉션

```csharp
var ordered = new XOrdered<int, string>();
ordered.Add(20, "Later");
ordered.Add(10, "Earlier"); // 순회 순서는 Earlier, Later

var keyed = new XOrdered<int, string, string>();
keyed.Add(20, "music", "Music");
keyed.Add(10, "sfx", "Sound Effects");

if (keyed.TryGetValue("music", out var value))
{
    // value == "Music"
}
```

`XOrdered`는 Order 오름차순을 유지합니다. 같은 Order는 기존 항목 뒤에 추가합니다. Key가 있는 변형에서 중복 Key를 추가하면 `ArgumentException`이 발생하며, Key는 정렬 기준이 아닙니다.

## Inspector에서 사용하기

```csharp
using UnityEngine;
using inonego.Xeri.Serializable;

public sealed class CollectionExample : MonoBehaviour
{
    [SerializeField] private XDictionary_VV<string, int> scores = new();
    [SerializeField] private XHashSet_V<string> tags = new();
    [SerializeField] private XOrdered<int, string> messages = new();
}
```

위 필드는 Xeri 컬렉션별 PropertyDrawer로 표시됩니다. 검색, 페이지 이동, 값 편집, 행 추가·삭제 등을 사용할 수 있습니다.

일반 `List<T>`와 1차원 배열은 자동 적용되지 않으며 `[CollectionEditor]`로 opt-in합니다. 자세한 사용 방법은 [Collection Editor Inspector 가이드](../../guides/serialization/edit-collections-in-inspector.md)를 참고하세요.

## 저장 관련 주의사항

- `XDictionary`, `XHashSet`, `XQueue`, `XStack`은 Runtime 컬렉션 값과 Unity 직렬화 목록을 별도로 관리합니다. Runtime에서 `Add`, `Remove` 등을 호출했다고 해서 변경이 자동으로 Inspector 직렬화 목록에 저장되지는 않습니다.
- Unity 역직렬화 시 직렬화 목록을 기반으로 Runtime 값을 재구성합니다. Runtime API와 Inspector 변경을 하나의 자동 영속 저장 흐름으로 가정하지 마세요.
- `XDictionary`는 직렬화 목록의 null Key를 Runtime 재구성 시 무시합니다. Key가 중복되면 **뒤에 기록된 항목의 Value**가 Runtime에서 사용되므로 Inspector 검증 메시지를 확인하세요.
- `XOrdered`는 API의 `Add`에서 정렬하지만 외부에서 직렬화된 항목을 직접 수정할 경우 자동 정렬을 가정하면 안 됩니다.
- `XPriorityQueue`의 내부 저장 구조는 Heap입니다. Inspector에서 보는 표시 순서와 실제 저장 순서는 다를 수 있습니다.

## 관련 문서

- [Collection Editor Inspector 가이드](../../guides/serialization/edit-collections-in-inspector.md)
- [Serializable Value와 Modifier](value.md)
- [Serializable 모듈](../../../Runtime/Serializable/README.md)
