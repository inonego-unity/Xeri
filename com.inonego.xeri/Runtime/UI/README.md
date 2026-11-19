# UI Utilities

## 개요

이 영역은 Drag & Drop과 Picker 및 이들이 재사용하는 공통 목록·Popup 기반을 제공합니다. Screen/Modal/Window 등 Application UI lifecycle은 별도 Xeri UI 패키지가 소유합니다.

## 하위 모듈

| 영역 | 역할 | 문서 |
|---|---|---|
| Common | PagedListChrome 셸, SearchPagedList 검색·페이지 상태, PaginationBar 표시 | 아래 공통 목록 UI |
| Display | Badge·ColorSwatch 같은 작은 공용 표시 요소 | 아래 공용 표시 요소 |
| Drag & Drop | backend 공통 drag/drop 상태와 UGUI/UITK adapter | [README](Drag_Drop/README.md) |
| Picker | UI Toolkit 기반 선택 UI와 table/filter/paging | [README](Picker/README.md) |
| Popup | 앵커 배치 Solver와 Editor/Runtime 공유 표시 호스트 | 아래 Popup 공통 기반 |

## 공통 목록 UI

Runtime/UI/Common은 Picker와 Collection Editor에서 재사용하는 목록 UI만 소유합니다.
PagedListChrome은 UXML/USS 레이아웃과 요소 삽입 지점을 제공하고 검색·선택·직렬화 정책은 소유하지 않습니다.
검색·필터·정렬·페이지 상태는 SearchPagedList<T>, 페이지 이동 버튼은 PaginationBar가 담당합니다.
Picker의 선택·필터 동작은 PickerSession에, Collection Editor의 편집 적용과 Undo는 CollectionSession에 남깁니다.

## 공용 표시 요소

Runtime/UI/Display는 Runtime UI Toolkit과 Editor UI Toolkit에서 함께 쓰는 작은 표시 요소를 제공합니다. 모두 `[UxmlElement]`라 UXML·UI Builder에서도 쓸 수 있고, 기본 스타일은 `Resources/Xeri/Display/*.uss`를 요소에 직접 붙입니다.

| 요소 | 역할 | 코드 | USS |
|---|---|---|---|
| `Badge` | 짧은 상태·분류 표식 | `Sizing`(Fit·Fixed), `Tone`(Neutral·Accent), `TextColor`, `BackgroundColor` | `.xeri-badge`, `--fixed`, `--accent` 클래스와 `--xeri-badge-*` 변수(높이·여백·모서리·글자 크기·고정 폭·배경·강조색) |
| `ColorSwatch` | 색 하나를 보이는 견본 | `Color` | `.xeri-color-swatch` 클래스와 `--xeri-color-swatch-*` 변수(크기·모서리·테두리색) |

기본 스타일이 요소 자신에 붙어 있어 같은 우선순위의 바깥 USS보다 먼저 적용됩니다. 바깥에서 모양을 바꿀 때는 상위 요소에 USS 변수를 지정하거나(상속됨) `.my-panel .xeri-badge`처럼 더 구체적인 선택자를 씁니다. 코드 프로퍼티는 인라인 스타일이라 USS보다 우선합니다.

## Popup 공통 기반

`Runtime/UI/Popup`은 팝업의 앵커·사용 영역·열림 방향·배치 결과를 `PopupPlacementSolver` 등 공통 계약으로 계산합니다. `PopupHost`와 `IPopupDisplay`는 Picker/Editor 컨텍스트의 팝업 표시·닫기·패널 입력 경계를 공유하는 내부 구현이며, 게임의 Screen/Modal/Window 수명이나 앱 수준 Navigation 정책을 소유하지 않습니다.

Editor 쪽 표시는 `Editor/Popup`이 담당합니다. 팝업 인프라가 존재한다는 사실이 별도 Xeri UI 패키지의 Application UI 기능을 이 패키지에 다시 포함한다는 뜻은 아닙니다.

## Drag & Drop

Drag & Drop은 drag 상태와 drop 판정, 좌표 변환, UGUI/UI Toolkit adapter를 분리해 제공합니다. 프로젝트는 필요한 coordinator, resolver, rule과 adapter를 조합해서 사용합니다.

## Picker

Picker는 검색·필터·paging 가능한 선택 UI를 구성하기 위한 model/view/editor 기능을 제공합니다. List와 Dictionary 같은 구체 facade를 통해 공통 선택 흐름을 재사용할 수 있습니다.

## 관련 문서

- [Xeri 구조](../../Documentation~/concepts/architecture.md)
- [확장 계약](../../Documentation~/concepts/extension-contracts.md)
- [Drag & Drop](Drag_Drop/README.md)
- [Picker](Picker/README.md)
