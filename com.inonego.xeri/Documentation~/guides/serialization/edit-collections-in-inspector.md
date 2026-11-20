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

`VisibleRows`는 처음 표시할 행 수(기본 5)입니다. `Columns`는 class/struct 요소의 직렬화 필드를 열로 나눠 표에서 바로 편집하게 합니다. 열 헤더는 필드의 `[InspectorName]`, 없으면 Unity 표시 이름을 사용합니다. 열로 지정하지 않은 필드가 남아 있으면 행의 편집 버튼으로 요소 전체를 편집합니다. `Columns`는 `[SerializeReference]` 요소에는 사용할 수 없습니다.

열 폭은 필드 형식에 맞춰 정해집니다. 첫 열(행 이름)과 문자열 열이 남는 폭을 더 많이 나눠 갖고, 숫자 열은 그보다 좁게, `bool` 체크박스 열은 가장 좁게 잡힙니다. `enum` 드롭다운 열은 선택지 이름이 잘리지 않도록 고정 폭입니다. 열 경계를 끌어 폭을 직접 조절할 수도 있습니다.

```csharp
[Serializable]
public sealed class LayerRow
{
    [InspectorName("이름")] public string displayName;
    [InspectorName("ID")] public string layerID;
    [InspectorName("순서")] public int order;
}

[CollectionEditor(VisibleRows = 8, Columns = new[] { "displayName", "layerID", "order" })]
[SerializeField] private List<LayerRow> layers = new();
```

## 목록 높이

- 목록 아래의 손잡이를 위아래로 끌면 표시 행 수가 바뀝니다. 조절한 높이는 같은 대상·필드에 대해 Editor 세션 동안 유지되며, 손잡이를 더블 클릭하면 기본 행 수로 돌아갑니다.
- 전용 편집 창처럼 목록이 남은 공간을 모두 써야 하는 호스트는 USS에서 `--xeri-collection-fill: 1;`을 지정합니다. 이 경우 목록은 부모 높이를 채우고, 들어가는 행 수만큼 페이지를 나누므로 내부 스크롤과 바깥 스크롤이 겹치지 않습니다. 채움 모드에서는 목록을 접지 않으며 바깥 테두리는 호스트가 그립니다. 부모 요소(예: `PropertyField`)도 `flex-grow: 1`이어야 합니다.

```css
.my-editor .xeri-collection { --xeri-collection-fill: 1; }
.my-editor .unity-property-field { flex-grow: 1; }
```

직접 실행 가능한 예제를 보려면 Unity **Package Manager > Xeri > Samples > Collection Editor Inspector > Import**를 선택하세요. 가져온 `TEST_E_CollectionValidation.unity` 씬에서 컬렉션 타입별 Inspector 표시와 편집을 확인할 수 있습니다.

## 2. 컬렉션 펼치고 행 편집하기

Inspector에서 해당 컴포넌트의 컬렉션 헤더를 눌러 목록을 펼칩니다.

- 헤더의 항목 수를 확인하고 검색 필드로 행을 필터링할 수 있습니다.
- 항목 수가 많으면 페이지 탐색으로 다른 항목을 확인할 수 있습니다.
- **+** 버튼은 컬렉션 정책에 맞게 항목을 추가합니다.
- 단순 값은 셀에서 직접 편집합니다. 복합 값은 셀의 편집 동작으로 별도 상세 편집창을 엽니다.
- 복합 값 셀은 요약을 보여 줍니다. 직접 하위 필드가 모두 단순 값이고 두 개 이하이면 `값1 · 값2`처럼 값을, 그보다 많거나 목록이 섞여 있으면 타입 이름을 표시합니다. 배열·리스트는 `N items`, 비어 있는 참조는 `None`입니다. 표시를 직접 정하려면 [셀 뷰](#셀-표시-직접-지정하기)를 등록합니다.
- Dictionary의 Key, XOrdered의 Order·Key처럼 행의 위치·정체성을 정하는 열은 셀에서 바로 바뀌지 않고, 편집 버튼으로 연 별도 창에서 중복·형식을 검증한 뒤 적용됩니다.
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

### 셀 표시 직접 지정하기

복합 값 셀의 요약(`값1 · 값2` 또는 타입 이름) 대신 원하는 UI를 보이려면 Editor 어셈블리에 셀 뷰를 만들고 값 형식을 지정합니다. UI Toolkit `ListView`의 `makeItem`/`bindItem`처럼 셀 UI는 한 번 만들고 행이 바뀔 때마다 다시 연결합니다.

```csharp
using UnityEditor;
using UnityEngine.UIElements;
using inonego.Xeri;
using inonego.Xeri.Serializable.Editor;
using inonego.Xeri.UI;

[CustomCollectionCell(typeof(LayerRow))]
internal sealed class LayerRowCell : CollectionCellView
{
    // 셀 UI를 만듭니다. 셀마다 한 번만 호출됩니다.
    public override VisualElement MakeCell()
    {
        var row = CreateRow();                 // 셀 높이에 맞춘 가로 행
        row.Add(CreateText("name"));           // 기본 요약 글자와 같은 위치·크기
        var order = new Badge();               // 공용 배지 (inonego.Xeri.UI)
        order.name = "order";
        order.style.marginLeft = 6f;
        row.Add(order);
        return row;
    }

    // 현재 행의 값을 표시합니다.
    public override void BindCell(VisualElement element, SerializedProperty property)
    {
        element.Q<Label>("name").text = property.FindPropertyRelative("displayName").stringValue;
        element.Q<Badge>("order").text = property.FindPropertyRelative("order").intValue.ToString();
    }

    // 선택 사항: 검색에 쓸 문자열입니다. null이면 기본 요약으로 검색합니다.
    public override string GetSearchText(SerializedProperty property)
        => property.FindPropertyRelative("displayName").stringValue;
}
```

- 위 예시는 `Columns` 없이 쓴 `List<LayerRow>`처럼 한 셀에 `LayerRow` 전체가 들어가는 경우에 적용됩니다. `Columns`로 필드를 나누면 각 셀의 형식은 필드 형식입니다.
- 별도 등록 코드는 필요 없습니다. Editor가 `[CustomCollectionCell]`이 붙은 클래스를 한 번 찾아 두고, 형식별 조회 결과를 캐시합니다.
- `UseForChildren = true`를 지정하면 파생 클래스와 인터페이스 구현 형식에도 적용됩니다. `SerializeReference` 셀은 행마다 현재 값의 실제 형식으로 뷰를 찾습니다.
- 셀 뷰는 요약 자리만 바꿉니다. 단순 값의 입력 필드와 상세 편집·작업 버튼은 그대로 남고, 값 변경은 상세 편집을 거칩니다.
- 한 뷰 인스턴스를 모든 셀이 공유하므로 뷰 클래스에 행별 상태를 저장하지 마세요. 정리할 것이 있으면 `UnbindCell`을 재정의합니다.
- `CreateRow()`·`CreateText()`는 표 스타일의 행·텍스트 클래스(`CollectionCellView.RowClassName`·`TextClassName`)를 붙여 기본 셀과 높이·글자 위치를 맞춥니다. 직접 만든 요소에 같은 클래스를 붙여도 됩니다.
- 상태 표식과 색 견본은 공용 요소 `Badge`·`ColorSwatch`(`inonego.Xeri.UI`)를 쓸 수 있습니다. 폭(`Sizing`)·색(`Tone`, `TextColor`, `BackgroundColor`)은 코드로, 같은 값은 USS 변수(`--xeri-badge-*`)로도 바꿉니다.

## 4. 컬렉션 종류별로 주의할 점

| 컬렉션 | Inspector 동작과 제약 |
|---|---|
| `XDictionary_**` | Key/Value 2열. null Key 행은 제외되고, 중복 Key는 Runtime처럼 **마지막 행**이 사용되며 앞쪽 행은 무효로 표시됩니다. Key 교체는 별도 검증을 거쳐 적용합니다. |
| `XHashSet_*` | 한 열의 값 목록. 중복 값은 **첫 행**만 사용되고 뒤쪽 행은 무효로 표시됩니다. |
| `XQueue_*` | Front → Back 순서. 새 항목은 Back에 추가됩니다. |
| `XStack_*` | Top → Bottom 순서. 새 항목은 Top에 추가됩니다. |
| `XOrdered<...>` | Order 기준 정렬. 같은 Order 안에서는 드래그로 순서를 바꿀 수 있습니다. Key가 있는 변형은 중복 Key 중 **앞쪽 행**이 사용되며, Key/Order 변경은 검증 후 적용합니다. |
| `XPriorityQueue<...>` | Priority가 높은 요소를 먼저 꺼내는 큐입니다. 표는 Priority가 높은 순서로 보이며 실제 Heap 저장 순서와 다를 수 있습니다. Priority는 입력을 마쳤을 때 적용됩니다. |
| `List<T>`, `T[]` | `[CollectionEditor]` 필드에만 적용합니다. `Unique`를 켜면 중복 값 중 첫 행을 유지하고 뒤쪽 행을 진단합니다. |

Dictionary에서 `Key`는 원소의 정체성이므로, 복합 Key를 일반 Value처럼 하위 필드에서 직접 수정하지 못할 수 있습니다. 변경 가능한 Key는 검증과 적용 단계를 통해 교체하세요.

## 5. 컬렉션 전체 작업

헤더의 **⋯** 메뉴에서 컬렉션 관리 명령을 이용할 수 있습니다. 모든 항목 삭제와 무효한 항목 정리가 있고, 정수·열거형 Key를 쓰는 Dictionary에는 Key 자동 채우기(추가·제거·지정 범위만 유지)가 함께 나타납니다.

명령을 실행할 때는 검색 결과와 실제 직렬화 항목이 다를 수 있음을 유의하세요. 검색은 표시 항목을 제한하지만 Runtime 컬렉션 자체를 바꾸지는 않습니다.

## 자주 발생하는 상황

**항목이 검색 결과에서 보이지 않아요.** 검색어를 지우거나 페이지를 이동하여 확인하세요. 필터는 원본 항목을 삭제하지 않습니다.

**중복 Key 또는 값 오류가 보여요.** 표시된 진단을 확인하고 중복 항목을 수정하거나 제거하세요. `Unique` 검사나 Dictionary/Set 검증은 자동 삭제를 의미하지 않습니다.

**Runtime에서 값을 바꿨는데 Inspector의 저장 값이 그대로예요.** 일부 Xeri 컬렉션은 Runtime 상태와 직렬화된 항목 목록을 별도로 유지합니다. Runtime API 호출이 직렬화 목록에 자동 저장되는 것은 아닙니다. [컬렉션의 저장 계약](../../modules/serialization/collections.md#저장-관련-주의사항)을 확인하세요.

## 관련 문서

- [Serializable Collections: 타입 선택과 C# API](../../modules/serialization/collections.md)
- [SerializeReference와 Managed Reference](../../modules/serialization/managed-reference.md)
