# UI Utilities

## 개요

이 영역은 Drag & Drop과 Picker 및 이들이 재사용하는 공통 목록·Popup 기반을 제공합니다. Screen/Modal/Window 등 Application UI lifecycle은 별도 Xeri UI 패키지가 소유합니다.

## 하위 모듈

| 영역 | 역할 | 문서 |
|---|---|---|
| Common | PagedListChrome 셸, SearchPagedList 검색·페이지 상태, PaginationBar 표시 | 아래 공통 목록 UI |
| Drag & Drop | backend 공통 drag/drop 상태와 UGUI/UITK adapter | [README](Drag_Drop/README.md) |
| Picker | UI Toolkit 기반 선택 UI와 table/filter/paging | [README](Picker/README.md) |
| Popup | 앵커 배치 Solver와 Editor/Runtime 공유 표시 호스트 | 아래 Popup 공통 기반 |

## 공통 목록 UI

Runtime/UI/Common은 Picker와 Collection Editor에서 재사용하는 목록 UI만 소유합니다.
PagedListChrome은 UXML/USS 레이아웃과 요소 삽입 지점을 제공하고 검색·선택·직렬화 정책은 소유하지 않습니다.
검색·필터·정렬·페이지 상태는 SearchPagedList<T>, 페이지 이동 버튼은 PaginationBar가 담당합니다.
Picker의 선택·필터 동작과 Collection Editor의 Undo·Mutation은 각각의 Session/Model에 남깁니다.

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
