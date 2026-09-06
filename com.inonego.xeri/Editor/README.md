# Xeri Editor

Xeri Editor 영역은 Runtime 계약을 직접 확장하거나 authoring을 돕는 Unity Editor 전용 구현을 둡니다.

## 개요

현재 Editor assembly에는 SerializedObject/SerializedProperty 보조 기능, generic type picker, tree UI와 Game UI의 HDRP editor 등록 구현이 있습니다.

## 책임 범위

- Editor에서만 필요한 inspector/authoring 보조 기능
- Runtime API를 사용하는 Editor adapter와 등록 코드
- Runtime assembly에 들어가면 안 되는 `UnityEditor` 의존 구현

## 제약과 주의사항

Runtime에서도 의미가 있는 상태 모델이나 계약을 Editor 편의 때문에 이 영역으로 이동하지 않습니다. Editor 타입은 Runtime 공개 계약을 소비하는 방향을 우선합니다.

## 관련 문서

- [Runtime 모듈](../Documentation~/modules/index.md)
