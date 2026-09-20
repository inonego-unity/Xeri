# UI Utilities

## 개요

이 영역은 독립적으로 재사용할 수 있는 Drag & Drop과 Picker를 제공합니다.

## 하위 모듈

| 영역 | 역할 | 문서 |
|---|---|---|
| Drag & Drop | backend 공통 drag/drop 상태와 UGUI/UITK adapter | [README](Drag_Drop/README.md) |
| Picker | UI Toolkit 기반 선택 UI와 table/filter/paging | [README](Picker/README.md) |

## Drag & Drop

Drag & Drop은 drag 상태와 drop 판정, 좌표 변환, UGUI/UI Toolkit adapter를 분리해 제공합니다. 프로젝트는 필요한 coordinator, resolver, rule과 adapter를 조합해서 사용합니다.

## Picker

Picker는 검색·필터·paging 가능한 선택 UI를 구성하기 위한 model/view/editor 기능을 제공합니다. List와 Dictionary 같은 구체 facade를 통해 공통 선택 흐름을 재사용할 수 있습니다.

## 관련 문서

- [Xeri 구조](../../Documentation~/concepts/architecture.md)
- [확장 계약](../../Documentation~/concepts/extension-contracts.md)
- [Drag & Drop](Drag_Drop/README.md)
- [Picker](Picker/README.md)
