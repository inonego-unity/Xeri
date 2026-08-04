# Xeri Picker

Unity UI Toolkit 기반 단일 선택 UI입니다.
호출자는 `PickerSpec<TEntry, TValue>`로 데이터 표시 규칙을 만들고 `Picker.Show(...)`로
modal window 또는 호출 위치에 연결된 dropdown을 엽니다.

```text
원본 데이터 목록 -> PickerSpec -> Picker.Show(...) -> 선택값 TValue
```

Picker는 다음 책임을 공통으로 처리합니다.

- preview 표시
- table column 표시
- 검색
- filter
- column 정렬
- paging
- double click / Enter 선택
- 선택 없이 닫기 취소 처리

## 기본 구조

Picker는 두 타입을 기준으로 동작합니다.

```csharp
TEntry  // 목록에 표시할 원본 데이터 타입
TValue  // 선택 완료 시 반환할 값 타입
```

예를 들어 학생 목록에서 학번 문자열을 선택하려면 `TEntry`는 `Student`, `TValue`는 `string`입니다.

```csharp
var spec = PickerSpec<Student, string>
   .Create("학생 선택")
   .Value(entry => entry.ID)
   .Label(entry => entry.Name)
   .Desc(entry => entry.Description)
   .Image(entry => entry.PreviewImage)
   .Tag("상태", entry => entry.Status)
   .Column
   (
      "이름",
      entry => entry.Name,
      PickerColumnOptions.Flexible(width: 180f, minWidth: 120f)
   )
   .Column
   (
      "나이",
      entry => entry.Age,
      PickerColumnOptions.Fixed(width: 64f, alignment: PickerColumnAlignment.Right)
   )
   .Column
   (
      "학번",
      entry => entry.ID,
      PickerColumnOptions.Flexible(width: 140f, minWidth: 100f)
   )
   .Column
   (
      "상태",
      entry => entry.Status,
      PickerColumnOptions.Fixed(width: 90f, sortable: false)
   )
   .Build();

Picker.Show
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

위 호출은 modal window를 엽니다. Inspector field나 toolbar처럼 특정 control에서 선택을
시작했다면, 같은 `PickerSpec`과 callback에 해당 control의 화면 좌표 `Rect`를 전달합니다.
이 경우에만 Picker가 해당 위치에 연결된 dropdown으로 표시됩니다.

```csharp
Picker.Show
(
   spec,
   students,
   currentValue: currentStudentID,
   onSelected: selectedID =>
   {
      currentStudentID = selectedID;
   },
   rect: buttonScreenRect,
   onCanceled: () =>
   {
      Debug.Log("선택 없이 닫힘");
   }
);
```

## PickerSpec 설정

| 설정 | 의미 |
|---|---|
| `Value(...)` | 선택 완료 시 반환할 값 |
| `Label(...)` | preview name과 검색에 사용할 대표 이름 |
| `Desc(...)` | preview 설명 |
| `Image(...)` | preview 이미지 |
| `Tag(...)` | preview tag 및 검색 대상 |
| `DefaultPreviewTags(...)` | 선택 없음 상태에서 보일 tag label |
| `Column(...)` | table column 정의 |
| `FilterByEntry(...)` | 원본 entry 기준 필터 |
| `Filter(...)` | picker entry 기준 필터 |
| `DisabledWhen(...)` | 선택 불가 조건 |
| `Preview(false)` | preview 영역 숨김 |

## Column Options

`Column(...)`은 column 표시/동작 정책을 개별 파라미터로 받지 않습니다.
폭, 정렬 가능 여부, 검색 포함 여부, text 정렬, overflow, 표시 여부는 모두 `PickerColumnOptions`로 표현합니다.

```csharp
.Column
(
   "이름",
   entry => entry.Name,
   PickerColumnOptions.Flexible
   (
      width: 180f,
      minWidth: 120f,
      overflow: PickerColumnOverflow.Ellipsis
   )
)
.Column
(
   "점수",
   entry => entry.Score,
   PickerColumnOptions.Fixed
   (
      width: 72f,
      alignment: PickerColumnAlignment.Right
   )
)
.Column
(
   "내부 코드",
   entry => entry.InternalCode,
   PickerColumnOptions.Flexible
   (
      width: 180f,
      searchable: false,
      visibility: PickerColumnVisibility.Hidden
   )
)
```

### Fixed

`Fixed(...)`는 column 폭을 고정합니다.
짧은 수치, 상태, 타입처럼 폭이 예측 가능한 값에 사용합니다.

```csharp
PickerColumnOptions.Fixed(width: 80f)
```

### Flexible

`Flexible(...)`은 column이 남는 공간에 적응할 수 있게 합니다.
이름, 경로, 코드처럼 길이가 다양하고 table에서 주로 읽는 값에 사용합니다.

```csharp
PickerColumnOptions.Flexible(width: 180f, minWidth: 120f)
```

`stretchWeight`는 현재 정확한 `1:2:1` 비율을 보장하는 값이 아닙니다.
정확한 비율 기반 width 계산이 필요하면 별도 layout calculator로 확장합니다.

### 옵션 항목

| 옵션 | 의미 |
|---|---|
| `Layout` | fixed/flexible 폭 정책 |
| `Sortable` | column header 정렬 가능 여부 |
| `Searchable` | column 표시 문자열을 검색 문자열에 포함할지 여부 |
| `Alignment` | cell text 정렬 |
| `Overflow` | 긴 cell text 처리 정책 |
| `Visibility` | table 표시 여부 |

## Preview Overflow

Preview의 대표 이름은 `Label(...)`에서 만든 값입니다.
표시 흐름은 다음과 같습니다.

```text
PickerEntry.Label -> PickerPreviewModel.Name -> preview-name Label
```

`preview-name`은 한 줄 ellipsis로 표시되어야 하며, 오른쪽 `선택` 버튼을 밀면 안 됩니다.
`preview-sub-label`도 긴 선택값이 들어올 수 있으므로 같은 overflow 정책을 따릅니다.
데이터 자체는 자르지 않고 UI 표시에서만 overflow를 처리합니다.

## ListPicker

단순 list는 `ListPicker`로 빠르게 spec을 만들 수 있습니다.
ListPicker는 기본적으로 preview를 숨깁니다.

```csharp
var spec = ListPicker
   .Spec<string>("문자열 선택")
   .Build();

Picker.Show(spec, entries, currentValue, selected => currentValue = selected);
```

원본 entry와 선택값 타입을 분리할 수도 있습니다.

```csharp
var spec = ListPicker
   .Spec<Item, string>("아이템 선택", entry => entry.ID)
   .Label(entry => entry.Name)
   .Column
   (
      "이름",
      entry => entry.Name,
      PickerColumnOptions.Flexible(width: 180f, minWidth: 120f)
   )
   .Column
   (
      "ID",
      entry => entry.ID,
      PickerColumnOptions.Fixed(width: 120f)
   )
   .Build();
```

## DictionaryPicker

Dictionary는 기본적으로 key를 선택값으로 반환하고, table에는 `Key`, `Value` column을 보여줍니다.
DictionaryPicker도 기본적으로 preview를 숨깁니다.

```csharp
var dictionary = new Dictionary<string, int>
{
   { "ALPHA", 18 },
   { "BETA", 20 },
   { "GAMMA", 22 },
};

Picker.ShowDictionary
(
   "키 선택",
   dictionary,
   currentKey: "BETA",
   onSelected: selectedKey =>
   {
      Debug.Log(selectedKey);
   }
);
```

## Filter

필터는 Picker 상단의 toggle 버튼으로 표시됩니다.
`defaultEnabled`가 `true`이면 처음 열 때부터 적용됩니다.

```csharp
.FilterByEntry
(
   "active",
   "활성",
   defaultEnabled: false,
   entry => entry.IsActive
)
```

Xeri의 `IFilter<T>` 구현체가 있으면 직접 넘길 수 있습니다.

```csharp
.FilterByEntry("valid", "유효", false, myFilter)
```

## 취소 처리

선택 없이 창을 닫으면 `onCanceled`가 호출됩니다.
선택이 확정된 경우에는 `onSelected`만 호출되고 `onCanceled`는 호출되지 않습니다.

```csharp
Picker.Show
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

`onCanceled`가 필요 없으면 생략할 수 있습니다.

```csharp
Picker.Show(spec, entries, currentValue, value => { });
```

## 키보드 조작

| 입력 | 동작 |
|---|---|
| `Enter` | 현재 항목 선택 |
| `Double Click` | 항목 선택 |
| `Esc` | 현재 선택 해제 |
| `Up / Down` | 항목 이동 |
| `Left / Right` | 페이지 이동 |

첫 항목에서 위로 이동하면 이전 페이지 마지막 항목으로 이동하고,
마지막 항목에서 아래로 이동하면 다음 페이지 첫 항목으로 이동합니다.

## 수동 확인

Editor 수동 테스트는 `TEST_PickerManualEditorWindow`에 있습니다.

```text
Runtime/UI/Picker/TEST/Editor/TEST_PickerManualEditorWindow.cs
```

기본 Picker manual은 preview와 column layout 검증을 담당합니다.

- 긴 `preview-name` ellipsis
- 긴 `preview-sub-label` ellipsis
- 여러 줄 preview desc
- fixed column
- flexible column
- right/center alignment
- sortable false column
- searchable false column
- hidden column
- 긴 table cell text overflow

ListPicker와 DictionaryPicker manual은 facade 선택 흐름 확인용으로 유지합니다.

테스트는 `[Explicit]`, `[Category("Manual")]`로 분리되어 있어 일반 테스트 실행에서 자동으로 멈추지 않습니다.

## 확장 방향

REF, DataPackage, Addressable 같은 도메인 전용 Picker는 기본 Picker 위에 얇은 facade로 추가하는 것을 권장합니다.

```csharp
REFPicker.Spec<T>()
REFPicker.Show<T>()
AddressablePicker.Spec(...)
AddressablePicker.Show(...)
```

핵심 구조는 유지합니다.

```text
데이터 소스 수집 -> PickerSpec 구성 -> Picker.Show(...)
```

UI 동작, 검색, 필터, 컬럼, 정렬, preview, 취소 처리는 공통 Picker가 담당하고,
도메인별 코드는 entry 수집과 표시 규칙만 담당합니다.
