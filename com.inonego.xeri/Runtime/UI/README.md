# Xeri UI

## 개요


Xeri UI는 UGUI와 UI Toolkit을 사용하는 Runtime/Editor UI 기능을 모은 상위 모듈입니다. 각 하위 시스템은 독립된 책임을 가지며 필요할 때 조합해서 사용합니다.

## 왜 필요한가

UI 문제는 Screen lifecycle, Drag/Drop, 목록 선택, Window/Tray처럼 서로 다른 책임을 가집니다. 이를 하나의 거대한 UI Runtime으로 묶지 않고 공통 namespace 아래 독립 시스템으로 제공해 필요한 기능만 선택하도록 합니다.

## 언제 사용하는가

- 게임 화면 stack/focus/input 수명이 필요하면 **Game UI**
- UGUI/UITK 공통 Drag/Drop 규칙이 필요하면 **Drag & Drop**
- 검색·필터 가능한 선택 창이 필요하면 **Picker**
- 데스크톱형 Window/Tray와 View Session이 필요하면 **Xeri UI**

하위 시스템끼리 자동으로 모두 연결되는 것은 아닙니다. 프로젝트가 필요한 UI 책임만 조합합니다.

## 어디서 시작하는가

게임 화면 lifecycle이면 [Game UI](Game/README.md), Drag/Drop이면 [Drag & Drop](Drag_Drop/README.md), Editor 선택 UI면 [Picker](Picker/README.md), Window/Tray 도구형 UI면 [Xeri Window](../../Documentation~/modules/xeri-ui/window.md)와 [View](../../Documentation~/modules/xeri-ui/view.md)에서 시작합니다.

## 하위 모듈

| 영역 | 역할 | 문서 |
|---|---|---|
| Bar | 값과 진행 상태를 표현하는 UGUI/UITK bar | 코드/API Reference |
| Drag & Drop | backend 공통 drag/drop 상태와 UGUI/UITK adapter | [README](Drag_Drop/README.md) |
| Game UI | Layer, Screen, Modal, Focus, Input, Transition Runtime | [README](Game/README.md) |
| Picker | UI Toolkit 기반 선택 UI와 table/filter/paging | [README](Picker/README.md) |
| Xeri UI | Window, Tray, View Source/Session과 UI Toolkit 표현 기반 | [Window](../../Documentation~/modules/xeri-ui/window.md) · [Tray](../../Documentation~/modules/xeri-ui/tray.md) · [View](../../Documentation~/modules/xeri-ui/view.md) |

## 책임 범위

UI 상위 모듈은 하나의 전역 UI 방식을 강제하지 않습니다. Game UI처럼 application 범위 Runtime이 필요한 기능과 Picker처럼 독립 Editor UI 기능을 같은 namespace 계열에서 제공하되, 각 시스템의 lifecycle은 해당 하위 모듈이 소유합니다.

## 선택 기준

- 게임 화면의 Layer/Screen/Modal/Input 수명이 필요하면 Game UI를 사용합니다.
- 임의 객체의 drag/drop 관계가 필요하면 Drag & Drop을 사용합니다.
- Editor에서 목록 검색·필터·선택 창이 필요하면 Picker를 사용합니다.
- 하위 기능의 표시 backend와 소유권 계약은 해당 README를 우선합니다.

## 관련 문서

- [Xeri 구조](../../Documentation~/concepts/architecture.md)
- [확장 계약](../../Documentation~/concepts/extension-contracts.md)
