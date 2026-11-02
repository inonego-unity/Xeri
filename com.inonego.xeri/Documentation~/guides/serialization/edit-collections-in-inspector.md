# Collection Editor로 Inspector 항목 편집하기

Xeri Collection Editor는 Unity Inspector에서 직렬화된 컬렉션을 검색하고 항목을 편집하기 위한 UI입니다. Xeri 컬렉션은 전용 Drawer로 표시되고, 일반 `List<T>`와 1차원 배열은 `[CollectionEditor]`를 붙여 사용합니다.

## 1. 필드 준비하기

```csharp
using System.Collections.Generic;
using UnityEngine;
using inonego.Xeri.Serializable;

public sealed class CollectionAuthoringExample : MonoBehaviour
{
    // Xeri 컬렉션은 별도 속성 없이 전용 Drawer로 표시됩니다.
    [SerializeField] private XDictionary_VV<string, int> scores = new();
    [SerializeField] private XHashSet_V<string> tags = new();

    // 일반 List와 1차원 배열은 속성으로 직접 선택합니다.
    [CollectionEditor]
    [SerializeField] private List<string> names = new();

    [CollectionEditor(Unique = true)]
    [SerializeField] private string[] uniqueIds = new string[0];
}
```

`using inonego.Xeri.Serializable;` 네임스페이스에 `CollectionEditorAttribute`와 컬렉션 타입이 있습니다. `[CollectionEditor]`는 기본 Unity 직렬화 가능 필드를 대체하지 않습니다. 직렬화된 `List<T>` 또는 1차원 배열 필드에 적용하세요.

`Unique = true`는 **중복된 행을 Inspector에서 진단**하는 옵션입니다. Runtime 목록을 `HashSet`으로 바꾸거나 중복을 자동으로 제거하지 않습니다.

직접 실행 가능한 예제를 보려면 Unity **Package Manager > Xeri > Samples > Collection Editor Inspector > Import**를 선택하세요. 가져온 `TEST_E_CollectionValidation.unity` 씬에서 컬렉션 타입별 Inspector 표시와 편집을 확인할 수 있습니다.

## 2. 컬렉션 펼치고 행 편집하기

Inspector에서 해당 컴포넌트의 컬렉션 헤더를 눌러 목록을 펼칩니다.

- 헤더의 항목 수를 확인하고 검색 필드로 행을 필터링할 수 있습니다.
- 항목 수가 많으면 페이지 탐색으로 다른 항목을 확인할 수 있습니다.
- **+** 버튼은 컬렉션 정책에 맞게 항목을 추가합니다.
- 단순 값은 셀에서 직접 편집합니다. 복합 값은 셀의 편집 동작으로 별도 상세 편집창을 엽니다.
- 행의 삭제 버튼 또는 행 메뉴에서 항목 삭제·복제·이동·복사·붙여넣기 등을 사용할 수 있습니다. 실제 제공 명령은 컬렉션 종류와 읽기 전용 상태에 따라 달라집니다.
- 이동할 수 있는 행은 드래그 핸들을 표시합니다. 검색·필터링된 결과나 다른 정렬 상태에서는 안전을 위해 드래그가 제한될 수 있습니다.

값을 변경하면 Unity의 Undo/Redo를 사용할 수 있습니다. 다른 Inspector나 Undo/Redo로 원본이 바뀌면 이미 열려 있던 편집 동작은 갱신 또는 재시도가 필요할 수 있습니다.

## 3. 값이 복잡하거나 다형성인 경우

`SerializeReference`로 저장된 인터페이스·추상 클래스 값은 지원되는 타입 중 구체 타입을 선택하여 작성할 수 있습니다. 일반적인 복합 class/struct는 간략한 셀 표시 대신 상세 편집창에서 하위 필드를 편집합니다.

다형성 선택이 필요한 필드에는 올바른 `R` 계열 컬렉션 타입을 사용하세요.

```csharp
// 관리 참조 Value를 갖는 Dictionary
[SerializeField]
private XDictionary_VR<string, IModifier> modifiers = new();
```

위 `IModifier`는 프로젝트에서 정의한 인터페이스의 예시입니다. 실제 사용할 인터페이스와 해당 구현 타입은 프로젝트 코드에 존재하고 Unity가 직렬화할 수 있어야 합니다.

## 4. 컬렉션 종류별로 주의할 점

| 컬렉션 | Inspector 동작과 제약 |
|---|---|
| `XDictionary_**` | Key/Value 2열. null·중복 Key를 진단합니다. 구조적인 Key 교체는 별도 검증을 거쳐 적용합니다. |
| `XHashSet_*` | 한 열의 값 목록. 중복 값은 유효하지 않은 행으로 표시합니다. |
| `XQueue_*` | Front → Back 순서. 새 항목은 Back에 추가됩니다. |
| `XStack_*` | Top → Bottom 순서. 새 항목은 Top에 추가됩니다. |
| `XOrdered<...>` | Order 기준 정렬. Key가 있는 변형은 Key의 고유성도 검사하며 Key/Order 변경은 검증 후 적용합니다. |
| `XPriorityQueue<...>` | Priority가 높은 요소를 먼저 꺼내는 큐입니다. 화면 표시 순서와 실제 Heap 저장 순서가 다를 수 있습니다. |
| `List<T>`, `T[]` | `[CollectionEditor]` 필드에만 적용합니다. `Unique`를 통해 선택적으로 중복을 진단합니다. |

Dictionary에서 `Key`는 원소의 정체성이므로, 복합 Key를 일반 Value처럼 하위 필드에서 직접 수정하지 못할 수 있습니다. 변경 가능한 Key는 검증과 적용 단계를 통해 교체하세요.

## 5. 컬렉션 전체 작업

헤더의 **⋯** 메뉴에서 컬렉션 관리 명령을 이용할 수 있습니다. 가능한 항목에는 전체 삭제, 무효한 행 제거, 컬렉션 전체 편집창 열기 등이 있습니다. Dictionary의 Key 채우기 등 종류별 추가 명령은 해당 모델이 지원할 때만 나타납니다.

명령을 실행할 때는 검색 결과와 실제 직렬화 항목이 다를 수 있음을 유의하세요. 검색은 표시 항목을 제한하지만 Runtime 컬렉션 자체를 바꾸지는 않습니다.

## 자주 발생하는 상황

**항목이 검색 결과에서 보이지 않아요.** 검색어를 지우거나 페이지를 이동하여 확인하세요. 필터는 원본 항목을 삭제하지 않습니다.

**중복 Key 또는 값 오류가 보여요.** 표시된 진단을 확인하고 중복 항목을 수정하거나 제거하세요. `Unique` 검사나 Dictionary/Set 검증은 자동 삭제를 의미하지 않습니다.

**Runtime에서 값을 바꿨는데 Inspector의 저장 값이 그대로예요.** 일부 Xeri 컬렉션은 Runtime 상태와 직렬화된 항목 목록을 별도로 유지합니다. Runtime API 호출이 직렬화 목록에 자동 저장되는 것은 아닙니다. [컬렉션의 저장 계약](../../modules/serialization/collections.md#저장-관련-주의사항)을 확인하세요.

## 관련 문서

- [Serializable Collections: 타입 선택과 C# API](../../modules/serialization/collections.md)
- [SerializeReference와 Managed Reference](../../modules/serialization/managed-reference.md)
