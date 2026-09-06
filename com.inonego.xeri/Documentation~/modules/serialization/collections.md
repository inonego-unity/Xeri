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

Unity가 직접 직렬화하기 어려운 컬렉션 상태를 별도 serialized 표현으로 보관하고, 직렬화 전후에 Runtime 컬렉션과 동기화합니다.

```text
Runtime collection
    ↓ OnBeforeSerialize
serialized representation
    ↓ OnAfterDeserialize
Runtime collection 복원
```

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

`XOrdered<TOrder, TValue>`는 `Order` 오름차순을 유지하는 직렬화 컬렉션입니다. Key 기반 변형은 정렬된 list와 `XDictionary_VR` lookup을 함께 유지합니다.

`XOrdered<TOrder, TKey, TValue>`의 직접 인덱서는 없는 key에 `null`을 반환하지만, `IReadOnlyDictionary` 명시 구현 인덱서는 BCL 계약대로 `KeyNotFoundException`을 발생시킵니다.

정렬 순서가 필요하면 `AsKeyed()` 또는 기본 순회를 사용합니다. `Keys`와 `Values`는 내부 dictionary 순서이므로 정렬 순서를 의미하지 않습니다.

## 복제와 참조 동일성

Key 기반 `XOrdered`의 깊은 복제는 reference cache를 사용해 list와 dictionary가 같은 복제 인스턴스를 계속 가리키도록 합니다.

## 선택 기준

- 일반 key/value lookup + Unity 직렬화가 필요하면 `XDictionary_*`를 사용합니다.
- 항상 정렬된 값 순서가 필요하면 `XOrdered`를 사용합니다.
- `SerializeReference`가 필요한 위치만 `R`, 일반 Unity value serialization이면 `V` 변형을 선택합니다.
- 단순 API 편의 때문에 Xeri collection을 사용하지 말고 Unity 직렬화 요구가 실제로 있는지 먼저 확인합니다.

## 제약과 주의사항

- 역직렬화 시 serialized 목록을 기준으로 Runtime collection을 다시 만듭니다.
- `XDictionary`의 중복 serialized key는 후행 값이 앞선 값을 덮어씁니다.
- `XOrdered.AsKeyed()`는 호출 시 reverse map을 만들기 때문에 O(N) 추가 비용이 있습니다.
- 직렬화 표현과 Runtime collection을 외부에서 별도로 수정하는 구조를 만들지 않습니다.

## 관련 문서

- [Serializable Value와 Modifier](value.md)
- [Serializable 모듈](../../../Runtime/Serializable/README.md)
