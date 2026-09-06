# Xeri Generation

## 개요


Xeri Generation은 절차적 생성 흐름에서 재현 가능한 seed/random과 결과 검증을 위한 작은 기반 모듈입니다.

## 왜 필요한가

하나의 전역 Random 호출 순서에 모든 생성 결과가 의존하면 작은 로직 변경이 다른 하위 결과까지 바꿀 수 있습니다. Generation은 stable key 기반 Seed 파생과 최소 Validation 계약을 제공해 결정성과 프로젝트 생성 알고리즘을 분리합니다.

## 언제 사용하는가

- 같은 Root Seed에서 같은 결과를 재현해야 할 때
- 하위 생성 작업마다 독립 Random stream이 필요할 때
- 생성 결과의 Warning/Error를 공통 형식으로 반환할 때

실제 생성 알고리즘, retry/backtracking 정책은 프로젝트가 소유합니다. [결정적 생성 흐름 만들기](../../Documentation~/guides/generation/deterministic-generation.md)에서 범용 조합 예를 확인합니다.

## 어디서 시작하는가

Seed 파생과 Validation의 책임 경계는 [Generation 상세](../../Documentation~/modules/generation/generation.md), stable ID 기반 하위 Random stream을 실제 Generator에 연결하는 방법은 [결정적 생성 흐름 만들기](../../Documentation~/guides/generation/deterministic-generation.md)에서 시작합니다.

## 핵심 개념

- `GenerationSeed`: 생성 입력 seed 표현
- `GenerationRandom`: seed를 기반으로 사용하는 random 흐름
- `GenerationValidation`: 생성 결과의 issue와 validation result 구성

## 책임 범위

Generation은 특정 맵, 건물, 아이템 생성 알고리즘을 제공하지 않습니다. 프로젝트 생성기가 공통 seed와 검증 결과 표현을 필요로 할 때 사용하는 기반 계약입니다.

## 관련 문서

- [Generation 상세](../../Documentation~/modules/generation/generation.md)
- [Xeri 구조](../../Documentation~/concepts/architecture.md)
