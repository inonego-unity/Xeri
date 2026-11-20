# Xeri Editor

Xeri Editor 영역은 Runtime 계약을 직접 확장하거나 authoring을 돕는 Unity Editor 전용 구현을 둡니다.

## 개요

현재 Editor assembly에는 SerializedObject/SerializedProperty 보조 기능, generic type picker와 tree UI 등 base Xeri Runtime을 위한 Editor 구현이 있습니다.

특정 Runtime 기능에 딸린 Editor 코드는 이 폴더가 아니라 그 기능 폴더 안 `Editor/`에 두고 `Editor.asmref`로 이 어셈블리에 넣습니다(예: `Runtime/UI/Popup/Editor`, `Runtime/UI/Display/Editor`, `Runtime/Serializable/Collection/Editor`). 이 폴더에는 여러 기능이 함께 쓰는 Editor 도구만 남깁니다.

Application UI 전용 Editor 기능은 Xeri UIFrame package가 소유합니다.

## 책임 범위

- Editor에서만 필요한 inspector/authoring 보조 기능
- Runtime API를 사용하는 Editor adapter와 등록 코드
- Runtime assembly에 들어가면 안 되는 `UnityEditor` 의존 구현

## 관련 문서

- [Runtime 모듈](../Documentation~/modules/index.md)
- [Xeri UIFrame](https://github.com/inonego-unity/Xeri-UI)
