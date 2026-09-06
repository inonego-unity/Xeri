# Xeri Game

Xeri Game은 게임 Runtime에서 반복되는 Entity, Spawn, State, HP, AI, Zone, Board와 controller 기반 기능을 제공합니다.

## 개요

이 영역은 특정 프로젝트의 콘텐츠 데이터보다 게임 객체의 상태와 관계, 등록·해제 lifecycle을 재사용 가능한 계약으로 표현합니다.

## 왜 필요한가

Entity Key/Spawn, HP, FSM, AI Group, Board, Use/Reaction 같은 게임 Runtime 문제는 프로젝트마다 반복되지만 실제 전투 규칙·콘텐츠 정책은 서로 다릅니다. Xeri Game은 반복되는 상태와 lifecycle만 범용 계약으로 제공하고 게임 규칙 자체는 프로젝트에 남깁니다.

## 언제 사용하는가

- 논리 Entity의 Spawn/Despawn과 중앙 조회가 필요할 때
- 단순 FSM, HP, Level 같은 재사용 상태 모델이 필요할 때
- AI 판단 대상 수명, Zone topology, Board 배치 같은 기반 모델이 필요할 때
- 상호작용 탐색/입력과 실제 Action 실행을 분리하고 싶을 때

무엇을 먼저 쓸지 모르겠다면 [모듈 선택 가이드](../../Documentation~/getting-started/choosing-modules.md)에서 문제 기준으로 고른 뒤 해당 상세 문서로 이동합니다.

## 어디서 시작하는가

객체 lifecycle이면 [Entity와 Spawn](../../Documentation~/modules/game/entity-lifecycle.md), 상태 전이면 [State Machine](../../Documentation~/modules/game/state-machine.md), 상호작용이면 [Use](../../Documentation~/modules/game/use.md) + [Reaction](../../Documentation~/modules/game/reaction.md), AI 집단 판단이면 [AI Group과 Brain](../../Documentation~/modules/game/ai-group.md)에서 시작합니다. 실제 조립 절차는 [Game 사용 가이드](../../Documentation~/guides/index.md)의 해당 항목을 따릅니다.

## 주요 영역

- `엔티티`: Entity key, view, presentation과 registry 연결
- `스폰`: spawn/despawn 상태와 registry
- `상태`: 상태 머신과 상태 계약
- `체력`: HP 상태와 변경 이벤트
- `AI`: entity/group brain과 AI group
- `구역`: zone, graph, link
- `보드`: 2D/3D board와 space/view 기반
- `물리`: physics query와 moving/physics volume
- `사용`, `반응`, `진영`, `레벨`, `월드`, `컨트롤러`: 각 게임 Runtime 보조 계약

## 책임 범위

### 담당하는 것

- Runtime 객체의 상태와 lifecycle 계약
- registry를 통한 소유 관계와 key 관리
- 상위 게임 기능이 조합할 수 있는 공통 모델

### 담당하지 않는 것

- 특정 게임의 퀘스트, 전투 규칙, 밸런스 데이터
- 프로젝트 콘텐츠를 Xeri 내부 타입으로 강제하는 것
- Scene 전체 composition을 대신하는 것

## 소유권과 수명

Entity/Spawn 계열은 registry가 등록 상태와 key를 관리합니다. 개별 Entity는 registry lifecycle과 자신의 HP/Group 같은 Runtime 상태의 경계를 구분해야 합니다.

## 관련 문서

- [Entity와 Spawn 수명](../../Documentation~/modules/game/entity-lifecycle.md)
- [State Machine](../../Documentation~/modules/game/state-machine.md)
- [Board](../../Documentation~/modules/game/board.md)
- [Game Controller](../../Documentation~/modules/game/controller.md)
- [HP](../../Documentation~/modules/game/hp.md)
- [Physics Query](../../Documentation~/modules/game/physics-query.md)
- [AI Group](../../Documentation~/modules/game/ai-group.md)
- [Use System](../../Documentation~/modules/game/use.md)
- [Reaction](../../Documentation~/modules/game/reaction.md)
- [Zone Graph](../../Documentation~/modules/game/zone-graph.md)
- [Level](../../Documentation~/modules/game/level.md)
- [소유권과 수명](../../Documentation~/concepts/ownership-and-lifetime.md)
- [Xeri 구조](../../Documentation~/concepts/architecture.md)
