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

- 항목(`XOrderedEntry<TOrder, TValue>`, `XOrderedEntry<TOrder, TKey, TValue>`)은 값 형식이라 인덱서·열거·`TryGetEntry`의 결과는 복사본입니다. `Order`, `Key`, `Value`와 `Deconstruct`로 읽습니다.
- `TOrder`는 구조체이며 `Comparer<TOrder>.Default`로 비교합니다(`int`, `float`, `enum` 등). `IComparable<TOrder>`를 구현한 형식은 박싱 없이 비교합니다.
- Key가 있는 변형은 항목이 8개를 넘으면 Key 조회에 인덱스(직렬화하지 않는 캐시)를 만들어 `ContainsKey`, `TryGetValue`, `TryGetEntry`, 중복 검사를 O(1)로 처리하고, 그 이하는 목록을 직접 훑습니다. 역직렬화·Inspector 정규화·전체 삭제·복제 재구성 뒤에는 다시 만듭니다.
- 직렬화 데이터에 같은 Key가 둘 이상 있으면 앞쪽 항목이 조회에 쓰입니다.

## 복제 (CloneFrom)

`XOrdered`, `XDictionary`, `XHashSet`, `XQueue`, `XStack`, `XPriorityQueue`는 `IDeepCloneable`을 구현합니다. 일반 `Dictionary`, `HashSet`, `Queue`, `Stack`, `List<T>`에도 같은 이름의 확장 메서드(`CollectionCloneHelper`)가 있습니다. 미리 만든 인스턴스에 `CloneFrom`으로 담아 두었다가 다시 `CloneFrom`으로 되돌리면 상태를 저장·복원할 수 있습니다.

```csharp
snapshot.CloneFrom(live);   // 저장
live.CloneFrom(snapshot);   // 복원
```

- 복제는 외부 알림 없이 Runtime 내용만 복사합니다. `XDictionary` 등의 serialized 목록은 `ApplyRuntime`이 맡습니다.
- 값 형식, `string`, `UnityEngine.Object`(에셋·씬 객체 참조), delegate, `Type` 요소는 복제 없이 그대로 복사(공유)합니다. 요소 타입이 `object`나 인터페이스여도 런타임 값이 이런 것이면 공유합니다. 그 밖의 참조 요소는 `IDeepCloneable<T>`로 복제하며, 복제를 지원하지 않으면 공유하지 않고 `InvalidOperationException`을 던집니다. 이때 대상은 바뀌지 않습니다.
- `XDictionary_VR`처럼 이름의 V·R은 직렬화 방식일 뿐이며 복제 규칙은 요소의 실제 타입으로 정해집니다.
- Dictionary의 Key는 복제하지 않고 공유합니다. Key가 있는 컬렉션(`Dictionary`, 키 있는 `XOrdered`)의 참조 값은 같은 Key·같은 구체 타입의 기존 인스턴스를 재사용해 `CloneFrom`으로 덮어쓰고, 없으면 `Clone()`합니다. source가 가진 인스턴스는 재사용하지 않으므로 복제본은 source와 인스턴스를 공유하지 않습니다.
- 키 없는 `XOrdered`는 같은 위치·같은 구체 타입의 기존 인스턴스를 재사용합니다. Order·Key·구체 타입이 순서대로 같으면 항목 목록을 그대로 두고 각 값에 `CloneFrom`만 하므로 할당이 없습니다. 이 규칙은 키 있는 `XOrdered`와 같은 `XOrderedBase` 구현을 씁니다.
- 그 밖의 키 없는 컬렉션(`HashSet`, `Queue`, `Stack`, `List<T>` 등)은 참조 요소를 항상 새로 복제합니다. 같은 인스턴스가 여러 번 들어 있어도 각각 따로 복제하므로 인스턴스 공유 관계는 복제되지 않습니다.
- 값 형식 요소가 내부에 참조를 가지면 그 참조는 그대로 공유됩니다.
- 구체 타입은 확인하지 않습니다. `XDictionary`는 일반 `Dictionary`에서, 일반 `Dictionary`는 `XDictionary`에서 복제할 수 있습니다. 파생 컬렉션이 가진 추가 상태는 복사하지 않으며, 파생 컬렉션에서 `Clone()`이 같은 타입을 돌려주게 하려면 `@new`를 재정의합니다.
- 복제 준비(새 요소 `Clone`)가 실패하면 대상은 바뀌지 않습니다.
- 재사용한 기존 요소의 `CloneFrom`은 되돌릴 수 없으므로, 실패해도 나머지를 끝까지 복사하고 목록(Key 구성)을 source에 맞춘 뒤 실패를 모아서 던집니다. 실패가 하나면 원래 예외, 둘 이상이면 `AggregateException`이며 실패한 요소는 이전 상태로 남습니다. 이 정책은 기존 요소를 덮어쓰는 `Dictionary`·`XDictionary`·`XOrdered`에 적용되고, 항상 새로 복제하는 `HashSet`·`Queue`·`Stack`·`List<T>`·`XPriorityQueue`는 준비 단계만 있어 실패하면 대상이 그대로입니다.
- 값 형식 요소와, 같은 Key의 참조 값을 재사용하는 같은 구성의 복제는 반복 호출에서 GC를 할당하지 않습니다. 임시 버퍼는 풀에서 빌려 씁니다.

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

위 필드는 Xeri 컬렉션별 PropertyDrawer로 표시됩니다. 검색, 스크롤 목록, 값 편집, 행 추가·삭제 등을 사용할 수 있습니다.

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
