# UI Utilities

## 개요

이 영역은 Drag & Drop, 휠 클릭 자동 스크롤, Picker 및 이들이 재사용하는 공통 목록·Popup 기반을 제공합니다. Screen/Modal/Window 등 Application UI lifecycle은 별도 Xeri UI 패키지가 소유합니다.

## 하위 모듈

| 폴더 | 역할 | 문서 |
|---|---|---|
| AutoScroll | 휠 클릭 자동 스크롤. backend 공통 상태와 UITK adapter | [README](AutoScroll/README.md) |
| ContextMenu | `XeriContextMenu` 컨텍스트 메뉴(하위 메뉴·단축키 표시·체크) | 아래 Popup 공통 기반 |
| Display | Badge·ColorSwatch 같은 작은 공용 표시 요소. `Editor/`에 진단 목록·HelpBox·심각도 표시 | 아래 공용 표시 요소 |
| DragDrop | backend 공통 drag/drop 상태와 UGUI/UITK adapter | [README](DragDrop/README.md) |
| Icons | `XeriIcon`과 공용 Lucide 아이콘(`Resources/Xeri/Icons/Lucide`) | 아래 공용 아이콘 |
| List | PagedListChrome 셸, SearchPagedList 검색·페이지 상태, PaginationBar 표시 | 아래 공통 목록 UI |
| Picker | UI Toolkit 기반 선택 UI와 table/filter/paging | [README](Picker/README.md) |
| Popup | 앵커 배치 Solver와 Editor/Runtime 공유 표시 호스트. `Editor/`에 Editor 표시 구현 | 아래 Popup 공통 기반 |
| Table | 표 열 폭 정책 `TableColumnLayout`과 배분 계산 | 아래 공통 목록 UI |

## 폴더 규칙

- 기능마다 폴더 하나를 두고, Runtime 코드와 그 기능의 Editor 전용 코드를 함께 둡니다.
- Editor 전용 코드는 기능 폴더 안 `Editor/`에 두고 `Editor.asmref`로 `inonego.Xeri.Editor` 어셈블리에 넣습니다. Core의 다른 영역(Serializable, Core, 유틸리티)과 같은 규칙입니다.
- 스타일·레이아웃은 기능 폴더 안 `Resources/Xeri/UI/<기능>/`에 두고 `Xeri/UI/<기능>/...` 키로 읽습니다. 키가 `Runtime/UI/<기능>` 폴더와 대칭이고, Xeri UI 패키지의 `Xeri/UI/<영역>/` 규칙과 같은 공간을 씁니다. 아이콘은 UI 밖에서도 쓰므로 `Resources/Xeri/Icons/`(`Xeri/Icons/...`)에 둡니다.
- 테스트는 기능 폴더 안 `TEST/EDIT`·`TEST/PLAY`·`TEST/Editor`에 두고 asmref로 테스트 어셈블리에 넣습니다.

## 네임스페이스 규칙

- 단순한 기능(ContextMenu, Display, List, Popup, Table)은 `inonego.Xeri.UI`를 함께 씁니다.
- backend 계층이나 하위 구조가 큰 기능(AutoScroll, DragDrop, Picker)은 `inonego.Xeri.UI.<기능>`을 씁니다.
- `XeriIcon`은 UI 요소가 아니라 UI 밖에서도 쓰는 자산 로더라 `inonego.Xeri`에 둡니다.
- Editor 코드는 런타임 네임스페이스 뒤에 `.Editor`를 붙입니다(`inonego.Xeri.UI.Editor`, `inonego.Xeri.UI.Picker.Editor`, `inonego.Xeri.UI.DragDrop.Editor`).
- 테스트는 `inonego.Xeri.TEST.UI._<기능>`을 씁니다.

## 공용 아이콘

`XeriIcon.Load(이름)`은 `Resources/Xeri/Icons/Lucide`의 Lucide 아이콘을 `VectorImage`로 읽어 캐시합니다. USS에서는 `resource("Xeri/Icons/Lucide/<이름>")`으로 씁니다. SVG는 24 격자를 그대로 가져와(`preserveViewport`) 아이콘마다 비율과 안쪽 여백이 같습니다.

## 공통 목록 UI

Runtime/UI/List는 Picker와 Collection Editor에서 재사용하는 목록 UI만 소유합니다.
PagedListChrome은 UXML/USS 레이아웃과 요소 삽입 지점을 제공하고 검색·선택·직렬화 정책은 소유하지 않습니다.
검색·필터·정렬·페이지 상태는 SearchPagedList<T>, 페이지 이동 버튼은 PaginationBar가 담당합니다.
Picker의 선택·필터 동작은 PickerSession에, Collection Editor의 편집 적용과 Undo는 CollectionSession에 남깁니다.

Runtime/UI/Table의 열 폭 정책은 공개 모델 `TableColumnLayout`(고정·가변, 최소·최대 폭, 가중치)으로 선언합니다. `TableColumnLayout.Distribute`는 주어진 전체 폭을 열마다 나누는 순수 계산이라, MultiColumnListView를 쓰지 않는 자체 표 구현도 같은 배분 규칙을 씁니다. MultiColumnListView에 적용하는 부분은 내부 `TableColumnSizing`이 맡습니다.

## 공용 표시 요소

Runtime/UI/Display는 Runtime UI Toolkit과 Editor UI Toolkit에서 함께 쓰는 작은 표시 요소를 제공합니다. 모두 `[UxmlElement]`라 UXML·UI Builder에서도 쓸 수 있고, 기본 스타일은 `Resources/Xeri/UI/Display/*.uss`를 요소에 직접 붙입니다.

| 요소 | 역할 | 코드 | USS |
|---|---|---|---|
| `Badge` | 짧은 상태·분류 표식 | `Sizing`(Fit·Fixed), `Tone`(Neutral·Accent), `TextColor`, `BackgroundColor` | `.xeri-badge`, `--fixed`, `--accent` 클래스와 `--xeri-badge-*` 변수(높이·여백·모서리·글자 크기·고정 폭·배경·강조색) |
| `ColorSwatch` | 색 하나를 보이는 견본 | `Color` | `.xeri-color-swatch` 클래스와 `--xeri-color-swatch-*` 변수(크기·모서리·테두리색) |

기본 스타일이 요소 자신에 붙어 있어 같은 우선순위의 바깥 USS보다 먼저 적용됩니다. 바깥에서 모양을 바꿀 때는 상위 요소에 USS 변수를 지정하거나(상속됨) `.my-panel .xeri-badge`처럼 더 구체적인 선택자를 씁니다. 코드 프로퍼티는 인라인 스타일이라 USS보다 우선합니다.

Editor 전용 표시 요소는 `Display/Editor`에 있습니다. `DiagnosticListView`(진단 목록), `HelpBoxElement`, `SeverityIndicator`(심각도 아이콘)이며, 스타일은 각 스크립트 옆 USS를 스크립트 위치 기준으로 읽습니다.

## Popup 공통 기반

`Runtime/UI/Popup`은 팝업의 앵커·사용 영역·열림 방향·배치 결과를 `PopupPlacementSolver` 등 공통 계약으로 계산합니다. 공개 `PopupHost`는 앵커 팝업의 패널 입력 차단, 바깥 클릭·패널 크기 변경·소유 요소 분리 시 닫기, 좌표 변환을 맡습니다. `XeriContextMenu`, Picker 드롭다운, 다른 패키지의 팝오버(예: TableGrid 열 메뉴)가 `PopupHost.Open` → `Solve` → `OpenLevel` 순서로 같은 정책을 씁니다. 표시 방식 계약 `IPopupDisplay`와 Editor 표시 구현 등록은 내부 구현입니다. 게임의 Screen/Modal/Window 수명이나 앱 수준 Navigation 정책은 소유하지 않습니다.

Editor 쪽 표시는 `Popup/Editor`(`XeriPopupWindow`, `EditorWorkArea`)가 담당합니다. 팝업 인프라가 존재한다는 사실이 별도 Xeri UI 패키지의 Application UI 기능을 이 패키지에 다시 포함한다는 뜻은 아닙니다.

## Drag & Drop

Drag & Drop은 drag 상태와 drop 판정, 좌표 변환, UGUI/UI Toolkit adapter를 분리해 제공합니다. 프로젝트는 필요한 coordinator, resolver, rule과 adapter를 조합해서 사용합니다.

## Picker

Picker는 검색·필터·paging 가능한 선택 UI를 구성하기 위한 model/view/editor 기능을 제공합니다. List와 Dictionary 같은 구체 facade를 통해 공통 선택 흐름을 재사용할 수 있습니다.

## 관련 문서

- [Xeri 구조](../../Documentation~/concepts/architecture.md)
- [확장 계약](../../Documentation~/concepts/extension-contracts.md)
- [Drag & Drop](DragDrop/README.md)
- [Auto Scroll](AutoScroll/README.md)
- [Picker](Picker/README.md)
