# Xeri Picker

Unity UI Toolkit 기반 선택 창입니다. `EditorWindow` 전체를 직접 상속해서 새 창을 만드는 대신, `PickerSpec`으로 표시 규칙을 만들고 `PickerWindow.Show(...)`로 모달 선택 창을 엽니다.

현재 Picker는 단일 선택을 기준으로 합니다. 항목은 더블 클릭 또는 Enter로 선택되고, 선택 없이 창을 닫으면 취소 콜백을 받을 수 있습니다.

## 기본 구조

Picker를 열 때는 두 타입을 정합니다.

```csharp
TEntry  // 목록에 표시할 원본 데이터 타입
TValue  // 선택 완료 시 반환할 값 타입
```

예를 들어 학생 데이터 목록에서 학번 문자열을 선택하려면 `TEntry`는 `Student`, `TValue`는 `string`입니다.

```csharp
var spec = PickerSpec<Student, string>
   .Create("학생 선택")
   .Value(entry => entry.ID)
   .Label(entry => entry.Name)
   .Desc(entry => entry.Desc)
   .Tag("나이", entry => entry.Age.ToString())
   .Tag("상태", entry => entry.Status)
   .Column("이름", entry => entry.Name, 160f)
   .Column("나이", entry => entry.Age, 80f)
   .Column("학번", entry => entry.ID, 140f)
   .Column("상태", entry => entry.Status, 100f)
   .Build();

PickerWindow.Show
(
   spec,
   students,
   currentValue: currentStudentID,
   onSelected: selectedID =>
   {
      currentStudentID = selectedID;
   },
   onCanceled: () =>
   {
      Debug.Log("선택 없이 닫힘");
   }
);
```

## PickerSpec 설정

`PickerSpec`은 Picker가 원본 데이터를 어떻게 표시하고 어떤 값을 반환할지 정의합니다.

| 설정 | 의미 |
| --- | --- |
| `Value(...)` | 선택 완료 시 반환할 값 |
| `Label(...)` | preview와 검색에서 사용할 대표 이름 |
| `Desc(...)` | preview 설명 |
| `Image(...)` | preview 이미지 |
| `Tag(...)` | preview tag 및 검색 대상 |
| `DefaultPreviewTags(...)` | 선택 없음 상태에서 보일 tag label |
| `Column(...)` | table에 표시할 열 |
| `FilterByEntry(...)` | 원본 entry 기준 필터 |
| `Filter(...)` | picker entry 기준 필터 |
| `DisabledWhen(...)` | 선택 불가 조건 |
| `Preview(false)` | preview 영역 숨김 |

`Column(...)`은 기본적으로 정렬 가능합니다. 이미지나 비교 불가능한 값처럼 정렬을 막고 싶은 열은 `sortable: false`를 지정합니다.

```csharp
.Column("아이콘", entry => entry.Icon, 36f, sortable: false)
```

## ListPicker

단순 list는 `ListPicker`를 사용하면 기본 `Value` column만 가진 PickerSpec을 빠르게 만들 수 있습니다. ListPicker는 기본적으로 preview를 끕니다.

```csharp
var entries = new[]
{
   "ALPHA",
   "BETA",
   "GAMMA",
};

PickerWindow.ShowList
(
   "문자열 선택",
   entries,
   currentValue: "BETA",
   onSelected: value =>
   {
      Debug.Log(value);
   },
   onCanceled: () =>
   {
      Debug.Log("취소");
   }
);
```

원본 entry와 선택값 타입을 분리하고 싶으면 `ListPicker.Spec<TEntry, TValue>(...)`를 사용합니다.

```csharp
var spec = ListPicker
   .Spec<Item, string>("아이템 선택", entry => entry.ID)
   .Label(entry => entry.Name)
   .Column("이름", entry => entry.Name, 180f)
   .Column("ID", entry => entry.ID, 120f)
   .Build();

PickerWindow.Show(spec, items, currentID, selectedID => currentID = selectedID);
```

ListPicker에서도 preview가 필요하면 다시 켤 수 있습니다.

```csharp
var spec = ListPicker
   .Spec<Item, string>("아이템 선택", entry => entry.ID)
   .Preview(true)
   .Label(entry => entry.Name)
   .Desc(entry => entry.Desc)
   .Build();
```

## DictionaryPicker

Dictionary는 기본적으로 key를 선택값으로 반환하고, table에는 `Key`, `Value` 두 열을 보여줍니다. DictionaryPicker도 기본적으로 preview를 끕니다.

```csharp
var dictionary = new Dictionary<string, int>
{
   { "ALPHA", 18 },
   { "BETA", 20 },
   { "GAMMA", 22 },
};

PickerWindow.ShowDictionary
(
   "키 선택",
   dictionary,
   currentKey: "BETA",
   onSelected: selectedKey =>
   {
      Debug.Log(selectedKey);
   },
   onCanceled: () =>
   {
      Debug.Log("취소");
   }
);
```

직접 Spec을 만들고 싶으면 `DictionaryPicker.Spec<TKey, TValue>(...)`를 사용합니다.

```csharp
var spec = DictionaryPicker
   .Spec<string, int>("키 선택")
   .Build();

PickerWindow.Show
(
   spec,
   DictionaryPicker.Entries(dictionary),
   currentValue: currentKey,
   onSelected: selectedKey => currentKey = selectedKey
);
```

## Preview 옵션

기본 PickerSpec은 preview를 표시합니다.

```csharp
PickerSpec<Item, string>.Create("아이템 선택") // preview ON
```

ListPicker와 DictionaryPicker는 단순 선택에 더 적합하도록 preview를 기본으로 숨깁니다.

```csharp
ListPicker.Spec<string>("문자열 선택")              // preview OFF
DictionaryPicker.Spec<string, int>("키 선택")      // preview OFF
```

preview 표시 여부는 `PickerViewOptions`에 저장됩니다. 호출자는 보통 `.Preview(bool)`만 사용하면 됩니다.

```csharp
.Preview(false) // preview 숨김
.Preview(true)  // preview 표시
```

## 필터

필터는 Picker 상단의 toggle 버튼으로 표시됩니다. `defaultEnabled`가 `true`면 처음 열 때부터 적용됩니다.

```csharp
.FilterByEntry
(
   "active",
   "활성",
   defaultEnabled: false,
   entry => entry.IsActive
)
```

Xeri의 `IFilter<T>` 구현체가 있으면 직접 넘길 수도 있습니다.

```csharp
.FilterByEntry("valid", "유효", false, myFilter)
```

## 취소 처리

선택 없이 창을 닫으면 `onCanceled`가 호출됩니다. 선택이 확정된 경우에는 `onSelected`만 호출되고 `onCanceled`는 호출되지 않습니다.

```csharp
PickerWindow.Show
(
   spec,
   entries,
   currentValue,
   onSelected: value =>
   {
      Debug.Log($"선택: {value}");
   },
   onCanceled: () =>
   {
      Debug.Log("선택 취소");
   }
);
```

`onCanceled`가 필요 없으면 기존처럼 생략할 수 있습니다.

```csharp
PickerWindow.Show(spec, entries, currentValue, value => { });
```

## 키보드 조작

| 입력 | 동작 |
| --- | --- |
| `Enter` | 현재 항목 선택 |
| `Double Click` | 항목 선택 |
| `Esc` | 현재 선택 해제 |
| `Up / Down` | 항목 이동 |
| `Left / Right` | 페이지 이동 |

첫 항목에서 위로 이동하면 이전 페이지 마지막 항목으로 이동하고, 마지막 항목에서 아래로 이동하면 다음 페이지 첫 항목으로 이동합니다.

## 수동 확인

Editor 수동 테스트는 `TEST_PickerManualEditorWindow`에 있습니다.

```text
Runtime/UI/Picker/TEST/Editor/TEST_PickerManualEditorWindow.cs
```

포함된 수동 확인 항목:

- 기본 Picker 선택 UI
- ListPicker 선택 UI
- DictionaryPicker 선택 UI
- 선택 없이 닫을 때 취소 처리

테스트는 `[Explicit]`, `[Category("Manual")]`로 분리되어 있어 일반 테스트 실행에서 자동으로 멈추지 않습니다.

## 확장 방향

REF, DataPackage, Addressable 같은 도메인 전용 Picker는 현재 기본 Picker 위에 얇은 facade로 추가하는 것을 권장합니다.

```csharp
REFPicker.Spec<T>()
REFPicker.Show<T>()
AddressablePicker.Spec(...)
AddressablePicker.Show(...)
```

핵심 구조는 유지합니다.

```text
데이터 소스 수집 -> PickerSpec 구성 -> PickerWindow.Show(...)
```

이렇게 하면 UI 동작, 검색, 필터, 컬럼, 정렬, preview, 취소 처리는 공통 Picker가 담당하고, 도메인별 코드는 entry 수집과 표시 규칙만 담당합니다.
