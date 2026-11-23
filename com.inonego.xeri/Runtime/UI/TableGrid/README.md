# Xeri TableGrid

Unity UI Toolkit 범용 표 컨트롤입니다(`inonego.Xeri.UI.TableGrid`). Picker·Collection Editor·데이터 작업공간 표를 이 컨트롤 하나로 통일하는 중이며, Runtime(인게임)과 Editor 양쪽에서 씁니다.

> 재설계 진행 중입니다. 현재는 표의 화면 구조(헤더, 고정 열, 세로 가상화, 셀 구성(값 규칙·셀 뷰·편집 방식), 선택 표시, 상태 줄), 열 폭 조절(헤더 경계 끌기, 더블클릭 내용 맞춤), 포인터·키보드 선택(셀 범위 끌기·자동 스크롤, 행 선택, 방향키·Tab·Home/End·PageUp/Down), 열 메뉴·필터 편집·열 표시·순서·빠른 이동 팝오버, 쿼리 칩, 본문 우클릭 메뉴, 셀 편집(입력칸·선택형 메뉴)과 복사·잘라내기·붙여넣기·지우기·행 삭제 요청, 행 이동 요청(끌기 손잡이·Alt+방향키·메뉴·잘라낸 행 삽입), 삽입 붙여넣기(복사한 행 삽입), 열 선택·열 이동(헤더 클릭·끌기, Ctrl+Space·Ctrl+Alt+←/→)까지 구현되어 있습니다. Picker와 Collection Editor는 이 표로 옮겼고(Collection은 셀 drawer·`CollectionCellView`를 `TableGridItemCellView` 어댑터로 감쌈), 팝업 편집기 위치와 장식 열·호스트 명령 목록이 남아 있습니다. Collection Editor 연결부(본문 메뉴 대체, 행 선택·활성 열 조회)는 internal입니다.

## 사용

```csharp
// 행 항목(Item)은 데이터 그대로이고, 열이 항목에서 형식 있는 값을 꺼낸다.
var edit = TableGridCellEdit.Editor();
var columns = new TableGridColumn[]
{
    new TableGridColumn<Item, string>("id", "ID", item => item.ID, layout: TableGridColumnLayout.Fixed(96f)),
    new TableGridColumn<Item, string>("name", "이름", item => item.Name, TableGridFormats.Text, edit: edit),
    new TableGridColumn<Item, string>
    (
        "category", "분류", item => item.Category, TableGridFormats.Choice(new[] { "Weapon", "Armor" }), edit: edit
    ),
    new TableGridColumn<Item, int>
    (
        "price", "가격", item => item.Price, TableGridFormats.Integer("N0"),
        alignment: TableGridAlignment.Right, edit: edit
    ),
};

// 사용처별 프리셋: Picker()(읽기 전용 목록), Inspector()(편집 목록), Workspace(access)(작업공간)
var options = TableGridOptions.Workspace(TableGridAccess.Editable);
options.RowHeight = 32f;
var grid = new TableGrid(options);
grid.SetColumns(columns);
grid.FrozenColumnCount = 1;
grid.SetSource(new TableGridListSource<Item>(items, item => item.ID));

// 정렬·필터·그룹 요청은 호스트가 조회한 뒤 표시 상태로 돌려준다.
grid.QueryRequested += (_, request) =>
{
    grid.SetSource(Query(items, request.Requested));
    grid.QueryState = request.Requested;
};

// 편집 요청은 열 값 형식의 값(object)을 담는다. 호스트가 항목에 반영한 뒤 표가 다시 읽게 한다.
grid.EditRequested += (_, request) =>
{
    Save(request.Edits); // edit.Value: string·int 등 열 값 형식
    grid.ApplyEditResults(request.Edits);
};

rootVisualElement.Add(new TableGridQueryChipBar(grid));
rootVisualElement.Add(grid);
```

- 열 폭은 `TableGridColumnLayout`(고정·가변, 최소·최대 폭, 가중치)으로 선언합니다.
- 행은 `TableGridRow(id, item, kind)`입니다. 그룹 머리·요약 행은 제목 글자를 `Item`에 담습니다(`Title`). 표시 순서·숨김은 `SetVisibleColumns`로 바꾸며 행 데이터는 그대로입니다.
- 선택형 열(`TableGridFormats.Choice`)은 Xeri Core `Badge` 칩으로 보이고, 색은 `Choices` 순서로 6가지 톤을 돌려 씁니다.
- 소스(`ITableGridSource`)는 동기입니다: `Count`, `GetRow(index)`, `IndexOf(rowID)`. 표는 선택·Shift 범위·키보드·클립보드를 모두 이 조회로 처리하므로 화면 밖 행도 바로 다룹니다. SQL·DB·파일 같은 외부 데이터는 호스트가 가져와 목록 소스로 넘기고, 가져오는 동안의 표시는 빈 상태 요소 속성 `EmptyContent`로 합니다.
- `ApplyEditResults`는 항목에 쓰지 않습니다. 바뀐 행 중 결과에 든 행이 있으면 스크롤·선택을 지킨 채 보이는 셀을 다시 연결하므로, 호스트는 먼저 데이터(행 항목)를 바꿔 둡니다.
- 결과·순서가 바뀔 때(정렬, 필터, 검색, `SetSource(..., keepView: true)`) 행 선택은 ID로 남고 결과에서 빠진 행만 빠집니다. 셀 선택은 Excel처럼 같은 자리를 유지하고 행·열이 줄면 남은 안으로 잘립니다. 행 이동을 요청한 뒤에는 옮긴 행을 따라갑니다. 바뀌면 `SelectionChanged`가 옵니다.
- 검색(`SearchText`)은 `ITableGridSearchableSource.SetSearch(text, columns)`로 소스가 거르며, 검색 글자는 `Searchable` 열의 `column.GetText(item)`(값 규칙)을 공백으로 이은 글자입니다. 일치 규칙은 Picker·Collection과 같습니다(공백뿐이면 검색 안 함, 대소문자 무시 포함).

## 목록 source 파이프라인

`TableGridListSource<T>`는 메모리 목록을 **검색 → 필터 → 정렬** 순으로 적용한 결과를 표에 줍니다. 페이지는 source가 아니라 표(`Paged` 모드)가 나눕니다. 결과는 조건이 바뀐 뒤 처음 읽을 때 한 번 다시 계산합니다.

| 멤버 | 동작 |
| --- | --- |
| `TableGridListSource(items, rowID, canReorder = false, searchText = null)` | `searchText`를 주면 검색 열 대신 이 함수의 글자로 검색합니다(열에 없는 값까지 검색할 때) |
| `SetSearch(text, columns)` / `SearchText` | 검색 글자(와 검색 열). 표의 `SearchText`가 `SetSearch`를 부릅니다 |
| `SetFilter(Func<T, bool>)` | 검색을 통과한 항목에 더 적용할 조건. `null`이면 거르지 않음 |
| `SetSort(Comparison<T>)` / `SetSort(column, direction)` | 정렬 규칙 또는 열의 형식 있는 값 비교(값 규칙 `Compare`)와 방향. 같은 순위는 원본 순서를 지킵니다(안정 정렬) |
| `Refresh()` / `Invalidate()` | 필터·정렬 함수가 읽는 상태가 바뀌었을 때 다시 적용 / 항목이 바뀌었을 때 항목별 캐시(검색 글자·행·ID 색인)까지 버리고 다시 적용 |
| `SearchText`, `Count`, `this[index]`, `Items`, `IndexOf(item)`, `GetRow(index)` | 결과 읽기(읽기 전용). 행 ID 위치는 `ITableGridSource.IndexOf(rowID)` |

- 항목별 검색 글자는 캐시합니다. 검색 열이 바뀌거나(`SetSearch`에 다른 열 목록) `Invalidate`를 부르면 버립니다.
- 행(`TableGridRow`)과 ID→원본 위치 색인은 항목마다 한 번 만들어 둡니다. 검색·필터·정렬이 바뀌면 원본 위치→결과 위치 배열만 재사용해 다시 채웁니다. 항목 목록을 바꿨으면 `Invalidate`를 부릅니다.
- `Capabilities.CanReorder`는 생성 인수 그대로입니다. 보기 정렬 중 행 이동 여부는 표가 쿼리 표시 상태로 정합니다.

## 검색창

`TableGridSearchField`는 표 밖에 호스트가 놓는 공개 요소(`[UxmlElement]`)입니다. 검색 아이콘·입력칸(안내 글자 `Placeholder`)·지우기 버튼(Xeri Core `x` 아이콘)과 선택적 결과 수(`ShowResultCount`)로 이뤄집니다.

```csharp
// 표 소스가 거르는 경우: 입력이 grid.SearchText로 들어간다.
var options = TableGridOptions.Workspace(TableGridAccess.ReadOnly);
options.Navigation = TableGridNavigation.Paged;
var grid = new TableGrid(options);
grid.SetColumns(columns);
grid.SetSource(new TableGridListSource<Item>(items, item => item.ID));

var search = new TableGridSearchField(grid)
{
    Placeholder = "이름·분류 검색",
    ShowResultCount = true,
};

rootVisualElement.Add(search);
rootVisualElement.Add(grid);
```

- 기본(`SearchesGrid = true`)은 입력을 `Grid.SearchText`로 보낸 뒤 `SearchTextChanged`를 알립니다. 결과가 바뀌면 `Paged` 표는 페이지 이동 컨트롤을 다시 계산하고, 고른 행이 남아 있으면 그 행의 페이지, 없으면 첫 페이지를 보입니다.
- 호스트가 직접 거르면(Collection Editor) `SearchesGrid = false`로 두고 `SearchTextChanged`에서 `Text`를 읽습니다. 이때 `Grid`는 테마와 결과 수에만 씁니다. 거른 결과만 소스로 넘기므로 거른 동안 `Grid.IsSourceNarrowed = true`로 알리면, 행이 없을 때 `EmptyText` 대신 `NoResultText`가 보입니다. 상태를 맞출 때는 `SetTextWithoutNotify`를 씁니다.
- 키는 가로채지 않습니다. 위·아래·Enter·Escape는 호스트가 정합니다.
- 테마는 쿼리 칩 줄과 같이 자기 패널과 `Grid.Options`로 정하고, 모양은 `--xeri-table-*` 변수(`field`·`field-border`·`accent`·`control-height` 등)를 씁니다. 루트 클래스는 `xeri-table-search`, 포커스 중 `xeri-table-search--focused`입니다.

## 셀 구성

셀은 표가 그리는 셀 틀 안에 **값 규칙 · 셀 뷰 · 편집 방식** 세 가지로 구성합니다. 값은 `TableGridColumn<TItem, TValue>` 안에서 형식 그대로 흐르고(글자·비교·셀 연결에서 박싱 없음), 편집 요청·클립보드 경계에서만 `object`가 됩니다.

| 구성 | 형식 | 맡는 일 |
| --- | --- | --- |
| 값 규칙 | `TableGridValueFormat<TValue>` (`TableGridFormats.Text`·`Number(format)`·`Integer(format)`·`NumberText`·`Choice(choices)`·`Default<T>()`) | `ToText`(화면·검색·복사 글자), `TryParse`(붙여넣기·입력칸), `Validate`, `Compare`, `ClearValue`(`CanClear`), `CopyValue`(값 묶음), `Kind`·`Choices`(필터 UI) |
| 셀 뷰 | `TableGridCellView<TValue>`(값을 받아 그림), `TableGridItemCellView`(행 항목·문맥만으로 그림), `TableGridInputCellView<TField, TValue>`(Inline 열의 입력칸) | 기본 셀 뷰(글자 Label, 선택형 칩)만 대체합니다. `MeasureWidth`가 있으면 내용 맞춤 폭으로 씁니다. Inline 열은 읽기용 셀 뷰 대신 Inline 셀 뷰를 씁니다 |
| 편집 방식 | `TableGridCellEdit.None`·`Editor(editor)`·`Inline(commit)`·`Detail` | 아래 표 |

| 편집 방식 | 동작 |
| --- | --- |
| `None` | 보기만 합니다(복사에는 들고 붙여넣기·지우기는 조용히 건너뜀) |
| `Editor` | Enter·더블클릭·글자 입력으로 편집기를 엽니다. 편집기는 열 지정 → 등록소 → 선택형이면 선택 메뉴 → 글자를 읽을 수 있으면 입력칸 순으로 정합니다. 확정 때 `TryGetValue` → `Validate`, 틀리면 편집기를 열어 둔 채 오류를 보입니다. 커스텀 편집기는 `TableGridCellEditor<TValue>`(`Begin`·`TryGetValue`·`End`, `Placement`) |
| `Inline` | 셀 뷰가 셀 안 컨트롤을 두고 `context.Host.Replace(value)`로 요청합니다. 셀 뷰를 지정하지 않으면 값 형식의 기본 입력 셀 뷰를 씁니다(`string`·`int`·`long`·`float`·`double`·`bool`·모든 `enum`, Editor는 `UnityEngine.Object`의 `ObjectField`와 `[Flags]` enum의 `EnumFlagsField`). 글자·숫자 입력칸은 `Immediate` 열이면 입력을 마칠 때(Enter·포커스 이탈) 한 번, `Confirm` 열이면 입력마다 보류 값을 갱신하며 확정은 표가 합니다. `bool`·`enum`·오브젝트는 값이 바뀌는 즉시 `Replace` 뒤에 `Commit`까지 요청합니다. `Immediate`는 바로, `Confirm`은 `Host.Commit()`·Enter·셀 밖 포커스 이탈 때 `EditRequested`(`Source = Inline`)로 갑니다. 표는 컨트롤의 포인터·포커스를 빼앗지 않고, 글자 입력으로 편집을 시작하지 않으며, Enter는 셀 안 첫 컨트롤로, Escape는 표로 돌아갑니다 |
| `Detail` | 셀은 보기만 하고 Enter·더블클릭·`Host.OpenDetail()`이 `DetailRequested`(행·열 ID)를 보냅니다. 값 묶음 붙여넣기만 받고 지우지 않습니다 |

- 등록소 `TableGridCellRegistry`: 열이 지정하지 않은 값 규칙·셀 뷰·편집기를 값 형식으로 찾습니다(정확한 형식 → 기반 형식 → 인터페이스, 기반·인터페이스는 `UseForChildren` 등록만 → 기본). Inline 열은 읽기용 셀 뷰 표(`RegisterView`)가 아니라 Inline 셀 뷰 표(`RegisterInlineView`·`FindInlineView`)에서 찾으며, 이 표는 기본 입력 셀 뷰를 미리 담고 있어 같은 값 형식에 다시 등록하면 대체됩니다. 셀 안에 셀 뷰를 얹는 호스트(예: Collection Editor)는 `FindInlineView`로 찾은 셀 뷰를 `MakeCell`·`BindBoxed`로 쓰고, 열 값 형식을 모르면 `context.WithValueType(type)`으로 형식을 줍니다. Runtime은 `Register*`를 직접 부르고, Editor는 `[TableGridValueFormat(typeof(T))]`·`[TableGridCellView(...)]`·`[TableGridInlineCellView(...)]`·`[TableGridCellEditor(...)]`을 붙인 형식을 자동 등록합니다. 열은 만들 때 찾으므로 등록은 열을 만들기 전에 합니다.
- 클립보드: 복사는 시스템 클립보드에 TSV(`ToText`)를 넣고 표가 셀 값 묶음(`CopyValue`, 기본은 값 형식·글자 그대로, `[Serializable]` 클래스 깊은 복사)을 보관합니다. 붙여넣는 글자가 마지막 복사와 같으면 값 묶음으로(형식이 안 맞으면 거부 셀), 다르면 열마다 `TryParse`로 읽습니다(읽을 수 없는 열은 건너뜀). 지우기는 `ClearValue`를 요청합니다(`CanClear`가 아니면 건너뜀). 값 묶음은 표마다 보관하므로 다른 표에 붙여넣으면 글자 경로입니다.
- 붙여넣기(Ctrl+V)는 늘 덮어씁니다.
- 잘라내기: 복사에 잘라낸 표시(원본을 흐리게)를 더하며 데이터는 바꾸지 않습니다. 같은 표에 붙여넣으면 덮어쓰고 원본을 비워 값을 옮깁니다(셀·열·행 모두, 한 편집 요청 `TableGridEditSource.Cut`). 원본은 값이 실제로 들어간 칸만 비웁니다. 다른 표나 외부 앱에 붙여넣으면 복사와 같고 원본은 그대로입니다. 표시는 옮기기 한 번, Escape, 다른 복사, 그 표의 편집·행 이동·행 삭제·행 삽입 요청으로 풀립니다.
- 삽입 붙여넣기(우클릭, 행 단위만): 클립보드 행을 고른 행 위에 넣습니다. 복사한 행(이 표의 행 복사나 밖에서 온 글자)은 "복사한 행 삽입"으로 새 행을 요청하고(`AddRowRequested`, `Capabilities.CanAddRows`일 때, 행 값 `RowValues` 포함, 첫 표시 열부터 채움), 같은 표에서 잘라낸 행은 "잘라낸 행 삽입"으로 행 이동을 요청합니다(`MoveRowsRequested`, `Cut`). 셀·열을 복사한 클립보드는 넣지 않습니다.
- `Popup` 편집기 위치는 아직 셀 안(`InCell`)과 같이 놓입니다.

## 구성

기능을 하나씩 켜고 끄지 않고 두 축만 고릅니다. 나머지 능력은 `grid.Capabilities`가 두 축과 연결된 요청 이벤트에서 파생합니다.

| 축 | 값 | 뜻 |
| --- | --- | --- |
| `Access` | `ReadOnly` / `Editable` | 편집·붙여넣기·지우기·잘라내기·행 삭제·행 이동을 허용하는지 |
| `Tools` | `List` / `Workspace` | 목록 수준(헤더 클릭 정렬)인지, 작업공간 수준(열 메뉴·필터 편집·열 패널·빠른 이동·열 이름 포함 복사·상태 줄)인지 |

`Navigation`(`Scroll`·`Paged`)이 행을 넘겨 보는 방식을 정합니다. `Paged`이고 `Selection`이 `Row`·`MultiRow`이면 ←/→는 활성 열 대신 이전·다음 페이지로 넘기고, 넘긴 페이지에 고른 행이 없으면 이전·마지막으로 넘겼을 때는 페이지 끝, 그 밖(다음·처음·직접 입력)은 페이지 처음 데이터 행을 고릅니다(페이지 이동 컨트롤도 같다).

`Selection`(`None`·`Row`·`MultiRow`·`Cell`)이 셀 범위·행 선택 칸을 정합니다. 행 선택 칸은 Excel의 행 머리처럼 **데이터 행 번호**(1부터, 그룹 머리 행은 세지 않음)를 보이고(`ShowRowSelector`, 기본은 `MultiRow`·`Cell`에서 보임), 누르거나 끌어 행을 고릅니다. 고른 행은 번호가 진해지고 강조색 표식이 붙으며, 셀 범위가 지나는 행은 번호가 진해집니다. 번호 칸 폭은 데이터 행 수의 자릿수에 맞춰 늘어납니다. 상태 줄의 범위·"전체 N행", Paged 상태 줄 가운데의 페이지 이동 컨트롤(범위·전체 개수는 상태 줄이 보인다), 검색창의 결과 수, 빠른 이동(Ctrl+G)·위치로 이동의 번호도 데이터 행만 셉니다(`TableGrid.DataRowCount`). `RowCount`는 그룹 머리 행을 포함한 결과 행 수(위치의 범위)입니다.

## 행 이동

표는 순서를 직접 바꾸지 않고 `MoveRowsRequested`(옮길 행 ID, 대상 행 ID, `Before`·`After`, 출처)로 요청만 보냅니다. 호스트가 반영한 뒤 다시 읽은 결과를 그대로 보입니다. `Editable` + `MoveRowsRequested` 연결 + 소스 `CanReorder`이고 보기 정렬·그룹이 없을 때만 옮길 수 있습니다.

- 끌기 손잡이 열: 행 앞(행 선택 칸 앞)의 고정 칸에 `grip-vertical` 아이콘이 보입니다. `ShowReorderHandle`이 `null`이면 데이터를 옮길 수 있는 표(`Editable` + 요청 연결 + `CanReorder`)에서 보이고, 값은 `Refresh` 때 다시 정합니다. 선택·복사·보기 상태에는 들지 않습니다.
- 손잡이를 누른 행이 선택에 들어 있으면 그 선택을, 아니면 그 행 하나를 골라 끕니다. 끄는 동안 옮길 행이 흐려지고 가장 가까운 행 경계에 삽입선이 보이며, 위·아래 가장자리에서 자동 스크롤합니다. `Paged`에서는 더 스크롤할 수 없을 때 가장자리에 0.5초 머물면 앞·뒤 페이지로 넘어갑니다.
- 놓으면 `TableGridMoveSource.Drag`로 요청합니다. 옮길 덩어리 안이나 바로 앞뒤처럼 순서가 그대로인 자리에서는 요청하지 않습니다. Escape를 누르거나 포인터 점유를 잃으면 취소됩니다.
- 보기 정렬·그룹 중에는 손잡이가 흐리게 남고, 누르면 상태 줄에 정렬·그룹을 풀라는 안내가 보입니다.
- 손잡이 색은 `--xeri-table-text-muted`·`--xeri-table-text-strong`, 삽입선은 `--xeri-table-drop-line`(색)·`--xeri-table-drop-line-width`(두께)입니다.

## 열 선택과 열 이동

`Workspace` + `Selection = Cell`인 표(`Capabilities.CanSelectColumns`)에서는 Excel·구글 시트처럼 헤더로 열을 고르고 옮깁니다. 열 메뉴는 헤더 우클릭으로만 엽니다. `List` 표의 헤더는 왼쪽 클릭이 정렬이고 우클릭은 아무것도 하지 않습니다.

| 입력 | 동작 |
| --- | --- |
| 헤더 클릭 | 그 열 하나를 고릅니다. 그 열이 활성 열이 되고 활성 행은 유지합니다(없으면 첫 데이터 행) |
| Ctrl+헤더 클릭 / Shift+헤더 클릭 | 그 열을 더하거나 뺍니다 / 기준 열부터 그 열까지 고릅니다 |
| 고르지 않은 헤더에서 끌기 | 끈 범위의 열을 고릅니다. 왼쪽·오른쪽 가장자리 밖에서 가로로 자동 스크롤합니다 |
| 고른 헤더에서 끌기 | 고른 열을 옮깁니다. 열 경계에 삽입선이 보이고 놓으면 보기 순서가 바뀝니다. Escape·포인터 점유 상실은 취소입니다 |
| Ctrl+Space / Ctrl+Alt+←·→ | 활성 셀의 열을 고릅니다 / 고른 열을 한 칸 옮깁니다 |
| 헤더 우클릭 | 열 메뉴. 그 열이 선택 밖이면 먼저 그 열을 고릅니다 |

- 열 선택은 `TableGridSelection.Columns(ids)`(`Kind = Column`, `ColumnIDs`)입니다. ID로 남으므로 열을 옮겨도 그 열을 따라가고, 숨긴 열은 빠지며 남은 열이 없으면 해제됩니다. 행이 늘거나 줄어도 "그 열 전체"라는 뜻은 그대로입니다. 셀을 누르면 셀 선택, 행 선택 칸을 누르면 행 선택으로 돌아갑니다.
- 고른 열은 헤더가 강조되고(`xeri-table__header-cell--selected`) 모든 셀에 범위 바탕이 생기며, 이어진 열이면 셀 범위와 같은 외곽선이 보입니다. 상태 줄에는 "N개 열 선택"이 보입니다.
- 열 이동은 보기 순서만 바꿉니다. 열 패널과 같이 `SetVisibleColumns` 경로로 적용되어 `ViewStateChanged`로 보기 상태에 남고, 데이터·호스트 요청은 없습니다. 고정 열 수(`FrozenColumnCount`)는 그대로이며, 놓은 자리의 구역(고정·스크롤)에 들어가고 밀려난 열이 반대 구역으로 넘어갑니다.
- 클립보드: 복사는 모든 데이터 행 × 고른 열(보기 순서, 떨어진 열도 이어 붙임)을 TSV·값 묶음으로 만들고, "열 이름 포함 복사"는 첫 줄에 열 이름을 넣습니다(같은 표에 되붙이면 그 줄은 빠집니다). 붙여넣기는 고른 열의 첫 데이터 행부터 아래로 채우고 값이 하나면 고른 열 전체를 채웁니다. 지우기는 그 열 전체, 잘라내기는 이어진 열일 때만 됩니다. 셀 수가 상한(10만)을 넘으면 전체를 거부합니다.

## 열 폭과 보기 상태

- 헤더 경계를 끌면 고정 열은 새 px 폭이 되고, 가변 열은 지금 표 폭에서 그 폭이 되도록 비율(가중치)이 바뀌어 이후에도 비율로 동작합니다. 경계를 두 번 누르면 같은 규칙으로 내용에 맞춥니다.
- `CaptureViewState(includeQuery, includeSession)`로 열 표시·순서·사용자 폭(고정 `Width`, 가변 `Weight`)·고정 열 수와 선택적으로 쿼리·세션을 꺼내고, `ToJson()`/`TableGridViewState.TryFromJson`으로 저장·복원합니다. 저장 위치는 호스트가 정합니다.
- `ViewStateChanged`는 열 표시·순서·폭·고정 수·쿼리 표시 상태가 바뀐 뒤 한 번 알립니다(경계를 끄는 동안에는 알리지 않음).
- Editor 호스트는 `TableGridViewStatePrefs.Bind(grid, tableKey)`(`inonego.Xeri.UI.TableGrid.Editor`)로 같은 경로를 EditorPrefs에 저장합니다. 키에는 `PlayerSettings.productGUID`가 붙습니다.

## 테마

두 테마 × 다크·라이트를 제공합니다. 모든 색·크기는 `TableGridTheme.uss`의 `--xeri-table-*` 변수입니다.

| `Theme` | 색 | 다크·라이트 | 기본 프리셋 |
| --- | --- | --- | --- |
| `Default` | 표 자체 팔레트 | `ColorMode`(`Auto`는 Editor 패널이면 Unity 스킨, Player 패널이면 다크) | `Workspace()` |
| `Editor` | Unity 에디터 변수(`--unity-colors-*`) | Unity 스킨을 따름(`ColorMode` 무시) | `Picker()`, `Inspector()` |

- 표·팝오버·쿼리 칩 줄·검색창이 각자 놓인 패널 종류로 정합니다. Player 패널(인게임, Editor에서 플레이 중인 게임 화면 포함)에는 Unity 에디터 변수가 없으므로 `Editor`도 `Default`로 그립니다.
- 루트에 붙는 수정 클래스: `xeri-table--light`(라이트), `xeri-table--editor`(Editor 테마).
- 변수는 표 루트(`.xeri-table`) 자신에 정의되므로 상위 요소에만 지정하면 바뀌지 않습니다. 호스트 USS에서 더 구체적인 선택자로 루트 자신을 지정하거나, 표 루트에 호스트 클래스를 더해 `.xeri-table.my-theme`으로 변수 묶음을 정의합니다.

```css
/* 모든 테마의 값 */
.my-host .xeri-table,
.my-host .xeri-table-chips,
.my-host .xeri-table-search { --xeri-table-accent: #e07a4f; }

/* 라이트만 */
.my-host .xeri-table--light { --xeri-table-canvas: #ffffff; }
```

- `Editor` 테마의 라이트 스킨 고정값(`.xeri-table--editor.xeri-table--light`)까지 덮으려면 클래스 3개 이상의 선택자가 필요합니다.
- 팝오버는 표 밖 팝업 창·프레임에 놓여 호스트 USS가 닿지 않으므로, 지금은 호스트의 변수 재지정이 팝오버에 적용되지 않습니다.

## 책임 경계

| TableGrid | 호스트·데이터 소스 |
| --- | --- |
| 헤더·행·셀 표시, 고정 열, 세로 가상화 | 데이터 조회와 변환 |
| 선택 표시와 선택 상태(ID 기준) | 데이터 수정·저장·Undo/Redo(표는 이력을 두지 않음) |
| 쿼리 표시 상태(정렬·필터·그룹 표시), 검색 글자 전달(`SearchText`) | 정렬·필터·그룹의 실제 연산(메모리 목록은 `TableGridListSource<T>` 파이프라인을 쓸 수 있음) |
| 결과·순서가 바뀔 때 선택 유지 규칙 | 외부 데이터 가져오기·로딩 표시·저장(가져온 뒤 목록 소스로 넘김) |

실행 취소: 표는 한 조작(붙여넣기·잘라낸 셀 옮기기·지우기·행 삭제·이동·삽입)을 한 요청으로 보내고, 편집 요청에 변경 전 값(`PreviousValue`)을, 모든 요청에 요청 시점 선택(`Selection`)을 담습니다. 호스트는 요청 하나를 한 단계로 기록하고, 반영하거나 되돌린 뒤 `ApplyEditResults`(행 구성이 바뀌면 `SetSource`)로 보이고 기록한 선택을 다시 지정합니다. 에디터 예제 창(`Samples~/TableGrid`)이 Unity Undo로 이렇게 연결합니다. `PreviousValue`는 표가 셀을 읽지 못하면 null이라 실제 null 값과 구분되지 않습니다.

## 예제

Package Manager의 Xeri Core Samples에서 **TableGrid Editor**를 가져온 뒤 `Xeri > Validation > Table Grid`를 엽니다. 1만 행, 고정 열 2개, 선택형 칩, 열 메뉴·필터 편집, 표 위 한 줄의 검색창과 쿼리 칩이 있고, 창 메뉴(탭의 ⋮)의 "페이지 모드"로 스크롤과 페이지를 전환해 볼 수 있습니다. 예제 창이 호스트 역할로 정렬·필터·그룹을 실제로 적용합니다.
