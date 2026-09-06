# Xeri Rendering

## 개요


Xeri Rendering은 Runtime 렌더링을 위한 제한된 공통 보조 기능을 둡니다. 현재 구현은 GPU instancing용 batch key, instance와 batch collection을 중심으로 구성됩니다.

## 왜 필요한가

대량 표시 데이터를 개별 GameObject/Renderer로 표현하지 않고 제출해야 할 때 프로젝트마다 batching key, spatial grouping, 1023 instance draw 분할을 다시 구현할 필요가 있습니다. Rendering 모듈은 이 최소 공통 제출 구조만 제공합니다.

## 언제 사용하는가

- 동일 Mesh/Material을 공유하는 많은 Opaque/Alpha-Clipped instance를 그릴 때
- 표시 데이터와 Scene GameObject 수를 분리하고 싶을 때
- 데이터 변경 시 batch snapshot을 만들고 frame에서는 제출만 하고 싶을 때

Per-instance transparent sorting이나 독립 Renderer lifecycle이 핵심이면 다른 렌더링 경로를 사용합니다. 실제 흐름은 [Instanced Renderer 구성하기](../../Documentation~/guides/rendering/build-instanced-renderer.md)를 참고합니다.

## 어디서 시작하는가

Batch의 `Add → Build → Render` 상태와 제약은 [Rendering Instancing](../../Documentation~/modules/rendering/instancing.md), 실제 Component/Presenter 조립은 [Instanced Renderer 구성하기](../../Documentation~/guides/rendering/build-instanced-renderer.md)에서 시작합니다.

## 책임 범위

### 담당하는 것

- instanced render instance 표현
- 동일 렌더 조건을 묶는 batch key와 collection

### 담당하지 않는 것

- Render Pipeline 전체 추상화
- 프로젝트별 shader/material authoring 정책
- Game UI의 렌더링 composition

## 핵심 개념

`InstancedRenderBatchCollection`은 instancing 대상으로 묶을 데이터를 관리하는 Runtime 자료구조입니다. 실제 사용자는 material, mesh와 instance 수명을 자신의 렌더링 범위에 맞춰 관리해야 합니다.

## 관련 문서

- [Rendering Instancing](../../Documentation~/modules/rendering/instancing.md)
- [Xeri 구조](../../Documentation~/concepts/architecture.md)
