# Xeri Tests

## 개요

패키지 공통 test assembly와 테스트 보조 객체를 두는 영역입니다. 기능별 테스트는 해당 Runtime 하위의 `TEST` 디렉터리에도 존재합니다.

## 검증 구조

- `EDIT`: Edit Mode 검증용 공통 영역
- `PLAY`: Play Mode 검증용 공통 영역
- `Editor`: Editor 전용 검증용 공통 영역
- `Runtime/**/TEST`: 특정 Runtime 기능과 가까이 유지하는 기능별 검증

테스트 배치는 실제 assembly 참조와 검증 대상의 성격을 기준으로 결정합니다.

Game Entity/Actor 계열은 transaction 경계를 직접 검증합니다.

- `TEST_EntityActorController`: Entity → Actor 0..N, exact-one lookup, 선택적 Registry binding
- `TEST_EntityActorFactory`: Actor 생성 실패 cleanup
- `TEST_EntityActorRuntime`: Actor + Presentation create/rollback/release와 오류 aggregate
- `TEST_EntityPresentationCoordinator`: Provider 정순 Spawn, 역순 rollback/release

개별 primitive 테스트와 상위 orchestration 테스트를 분리해, 상위 테스트가 하위 구현 세부를 다시 복제하지 않게 유지합니다.

## 관련 문서

- [문서 시작점](../Documentation~/index.md)
