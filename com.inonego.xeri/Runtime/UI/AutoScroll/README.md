# Xeri Auto Scroll

## 개요

`AutoScroll`은 휠 클릭 자동 스크롤(브라우저처럼 가운데 버튼을 누르고 마우스를 움직이는 방향으로 계속 스크롤)을 제공합니다.
Unity의 `ScrollView`·`ScrollRect`에는 이 기능이 없습니다.

Drag & Drop과 같은 구조로, Core는 UI 프레임워크에 의존하지 않고 UI Toolkit 구현은 입력·주기 실행·표시만 연결합니다.

## 동작

| 입력 | 결과 |
| --- | --- |
| 붙인 요소 위에서 가운데 버튼 클릭(거의 움직이지 않고 뗌) | 고정 상태. 기준점에서 멀어지는 방향·거리에 따라 계속 스크롤 |
| 고정 상태에서 붙인 요소 위를 누름 / Escape | 끝냄. 그 누름은 아래 요소로 넘기지 않음 |
| 고정 상태에서 붙인 요소 밖을 누름 | 끝냄. 그 누름은 원래 요소로 감(다른 표에서 가운데 버튼을 누르면 그 표가 시작) |
| 붙인 요소 위에서 가운데 버튼을 누른 채 끌기 | 누르는 동안 스크롤, 떼면 끝냄 |

시작은 붙인 요소 위에서 누를 때만이고, 대상에 움직일 내용이 없으면(`IAutoScrollTarget.CanScroll`이 false, 예: 행이 높이에 다 들어간 표) 시작하지 않고 누름을 아래 요소로 넘깁니다. 버튼을 누르고 있는 동안만 포인터를 잡고, 고정 상태에서는 포인터를 잡지 않고 패널 최상위에서 위치를 받으므로 다른 요소의 입력을 가로채지 않습니다.

기준점 둘레의 정지 거리 안에서는 움직이지 않고, 그 밖에서는 거리에 비례한 속도(최대 속도까지)로 움직입니다. 속도는 초 단위라 프레임률과 무관합니다.

## 구조

```text
Runtime/UI/AutoScroll
├─ Core
│  ├─ AutoScroller.cs           단계(Idle·Pressed·Holding·Latched)와 속도 계산, Tick으로 대상 이동
│  ├─ AutoScrollSettings.cs     정지 거리·거리당 속도·최대 속도·끌기 판정 거리·축 허용 ([Serializable])
│  ├─ IAutoScrollTarget.cs      스크롤 대상 계약: ScrollBy(delta)
│  └─ IAutoScrollIndicator.cs   표시기 계약: Show·SetVelocity·Hide
├─ UITK
│  ├─ UITKAutoScrollManipulator.cs   포인터·Escape 입력, 16ms 주기 Tick
│  ├─ UITKScrollViewTarget.cs        ScrollView를 대상으로
│  └─ UITKAutoScrollIndicator.cs     패널 최상위에 띄우는 기준점 표식
└─ Resources/Xeri/UI/AutoScroll/AutoScrollIndicator.uss
```

입력 지점은 Drag & Drop의 `InputPoint`를 함께 씁니다.

## UI Toolkit 사용법

```csharp
using UnityEngine.UIElements;

using inonego.Xeri.UI.AutoScroll;

var scrollView = root.Q<ScrollView>("list");
scrollView.AddManipulator(new UITKAutoScrollManipulator(new UITKScrollViewTarget(scrollView)));
```

- 스크롤 대상과 입력을 받을 요소는 달라도 됩니다. 예를 들어 고정 열과 스크롤 영역을 함께 가진 표는 본문 요소에 붙이고 대상은 스크롤 영역의 `ScrollView`로 둡니다.
- 자체 스크롤 영역은 `IAutoScrollTarget`(`CanScroll`, `ScrollBy`)을 구현해 넘깁니다. `delta`는 화면 방향(x+ 오른쪽, y+ 아래쪽 내용이 보이는 쪽)입니다.
- 시작 버튼은 `Button`(기본 2), 속도는 `AutoScrollSettings`로 바꿉니다.
- 표시기를 바꾸려면 `Indicator`에 다른 `IAutoScrollIndicator`를 넣습니다. 기본 표식 모양은 `--xeri-auto-scroll-*` 변수로 바꿉니다(표식이 패널 최상위에 붙으므로 패널 루트에 지정).

Editor 창과 Runtime 패널 모두 UI Toolkit 기본 API만 써서 같은 방식으로 동작합니다.

## 다른 UI 프레임워크

UGUI 등은 같은 `AutoScroller`에 입력(`Begin`·`Move`·`Release`·`End`)과 매 프레임 `Tick(deltaTime)`을 넘기고, `IAutoScrollTarget`으로 `ScrollRect` 등을 감싸면 됩니다. 좌표계 방향은 대상 구현에서 맞춥니다.

끌던 중 떼면 `Release`가 스스로 끝내므로, 표시 숨기기·입력 잡기 해제 같은 정리는 `End`를 부른 곳이 아니라 `Ended` 이벤트에서 한 번에 합니다(UI Toolkit Manipulator도 이렇게 합니다).

## 관련 문서

- [UI Controls](../README.md)
- [Drag & Drop](../DragDrop/README.md)
