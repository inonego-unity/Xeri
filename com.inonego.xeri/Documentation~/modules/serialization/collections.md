# Serializable Collections

Xeri Serializable Collections는 Unity 직렬화와 일반 컬렉션 API를 함께 사용하기 위한 컬렉션 구현입니다. `XDictionary`, `XOrdered`, `XHashSet`, `XQueue`, `XStack`, `XPriorityQueue`를 제공합니다.

## 왜 필요한가

일반 .NET 컬렉션을 그대로 쓰면 Unity Inspector/serialization과 맞지 않는 경우가 있고, `SerializeReference`가 필요한 key/value 조합은 별도 직렬화 표현이 필요합니다. Xeri Collection은 runtime collection API를 유지하면서 Unity가 저장할 표현을 함께 관리합니다.

## 언제 사용하는가

- Dictionary/Set/Queue 같은 컬렉션을 Unity 직렬화 상태로 보관해야 할 때
- key/value 중 일부만 `SerializeReference` 다형성을 사용해야 할 때
- 항상 정렬된 값 목록과 key lookup을 동시에 유지해야 할 때

런타임에서만 존재하고 직렬화할 필요가 없는 컬렉션은 기본 .NET 컬렉션을 사용하는 편이 더 단순합니다.

## 기본 사용

```csharp
var values = new XDictionary_VV<string, int>
{
    ["one"] = 1,
    ["two"] = 2,
};

var ordered = new XOrdered<int, string>();
ordered.Add(20, "late");
ordered.Add(10, "early");
```

`R`/`V` 변형은 성능 등급이 아니라 **Unity 직렬화 방식**을 의미합니다. 실제 필드가 다형 참조를 필요로 하는 위치에만 `R`을 선택합니다.

## 핵심 모델

직렬화 authoritative state는 컬렉션 종류에 따라 다릅니다.

- `XDictionary` 계열은 Runtime dictionary와 serialized pair 표현을 분리하고 `ISerializationCallbackReceiver`로 동기화합니다.
- `XOrdered`는 직렬화된 Entry 목록 자체가 authoritative state입니다. 별도 serialization callback으로 정렬이나 상태를 복구하지 않습니다.
- Keyed `XOrdered`의 lookup만 비직렬화 runtime 파생 상태이며 필요할 때 Entry 목록에서 lazy 구성합니다.

## XDictionary

`XDictionaryBase<TKey, TValue, TPair>`는 `Dictionary<TKey, TValue>`를 상속하고 `ISerializationCallbackReceiver`로 직렬화 표현을 동기화합니다.

키와 값의 직렬화 방식에 따라 네 변형이 있습니다.

| 타입 | Key | Value |
|---|---|---|
| `XDictionary_RR` | `SerializeReference` | `SerializeReference` |
| `XDictionary_RV` | `SerializeReference` | `SerializeField` |
| `XDictionary_VR` | `SerializeField` | `SerializeReference` |
| `XDictionary_VV` | `SerializeField` | `SerializeField` |

## XOrdered

`XOrdered<TOrder, TValue>`는 `Order` 오름차순을 유지하는 직렬화 컬렉션입니다.

`XOrdered<TOrder, TKey, TValue>`는 각 Entry가 `Order`, unique `Key`, `Value`를 함께 소유합니다. 직렬화된 Entry 목록이 authoritative state이고, Key lookup은 역직렬화 이후 필요할 때 Entry 목록에서 다시 구성하는 runtime 파생 상태입니다.

두 변형 모두 기본 순회와 index 접근이 Order 순서를 따릅니다. 읽기 전용 노출은 `IReadOnlyXOrdered`를 사용하고, Keyed 변형은 `ContainsKey()`, `TryGetValue()`, `TryGetEntry()`로 Key 조회를 제공합니다.

## 선택 기준

- 일반 key/value lookup + Unity 직렬화가 필요하면 `XDictionary_*`를 사용합니다.
- 항상 정렬된 값 순서가 필요하면 `XOrdered`를 사용합니다.
- `SerializeReference`가 필요한 위치만 `R`, 일반 Unity value serialization이면 `V` 변형을 선택합니다.
- 단순 API 편의 때문에 Xeri collection을 사용하지 말고 Unity 직렬화 요구가 실제로 있는지 먼저 확인합니다.

## 제약과 주의사항

- `XDictionary`는 역직렬화 시 serialized pair 목록을 기준으로 Runtime dictionary를 다시 만듭니다.
- `XDictionary`의 중복 serialized key는 후행 값이 앞선 값을 덮어씁니다.
- `XOrdered`는 저장된 Entry 순서를 그대로 신뢰하며 serialization 과정에서 자동 정렬·복구하지 않습니다.
- Keyed `XOrdered`의 Key lookup은 비직렬화 runtime 파생 상태이며 필요할 때 Entry 목록에서 lazy 구성됩니다.
- 직렬화 표현과 Runtime collection을 외부에서 별도로 수정하는 구조를 만들지 않습니다.

## 관련 문서

- [Serializable Value와 Modifier](value.md)
- [Serializable 모듈](../../../Runtime/Serializable/README.md)
