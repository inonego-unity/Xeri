# Xeri Core

Xeri Core는 여러 Runtime 모듈이 공통으로 사용하는 기반 계약과 생명주기 도구를 제공합니다. 특정 게임 도메인보다 Bootstrapper, Lease, Singleton, primitive와 공통 interface처럼 다른 모듈의 기반이 되는 요소를 다룹니다.

## 개요

Core는 애플리케이션 시작 순서, 일회성 수명 소유권, 공통 값/인터페이스와 key 생성 같은 저수준 기능을 한 영역에 모읍니다.

## 왜 필요한가

여러 모듈이 각자 초기화 순서, 일회 반환 책임, 현재 Runtime instance 선택과 numeric 보조 계약을 따로 만들면 프레임워크 전체의 수명 규칙이 달라집니다. Core는 특정 도메인에 속하지 않는 공통 실행·소유권 기반을 한곳에 둡니다.

## 언제 사용하는가

- Initial Scene 전후 초기화 순서를 조립하려면 `Bootstrapper`
- 외부 자원이나 등록의 일회 종료 책임을 전달하려면 `Lease`
- 여러 named instance와 임시 Current Context가 필요하면 `Singleton`/`InstanceRegistry`
- Xeri의 제네릭 수치·범위 시스템을 확장하려면 Primitive

프로젝트 도메인 규칙 자체를 Core로 끌어올리지는 않습니다.

## 어디서 시작하는가

애플리케이션 초기화 문제면 [Bootstrapper](../../Documentation~/modules/core/bootstrapper.md), 여러 Runtime Context면 [Singleton과 슬롯](../../Documentation~/modules/core/singleton.md), 수치 기반 계약 확장이면 [Primitive](../../Documentation~/modules/core/primitive.md)에서 시작합니다. 반환 책임 자체의 공통 규칙은 [소유권과 수명](../../Documentation~/concepts/ownership-and-lifetime.md)을 먼저 봅니다.

## 책임 범위

### 담당하는 것

- `Bootstrapper`와 module phase를 통한 초기화 순서
- `Lease` 기반 일회성 종료 책임
- Singleton 등록과 조회
- numeric/범위 primitive, comparer, key generator
- 여러 모듈에서 재사용하는 공통 lifecycle/interface

### 담당하지 않는 것

- 게임 Entity나 UI 같은 상위 도메인 정책
- 특정 저장 포맷이나 외부 데이터 접근
- 프로젝트별 composition 정책

## 핵심 개념

| 개념 | 설명 |
|---|---|
| Bootstrapper | 초기 Scene 전후의 module 실행 순서를 조립 |
| `Lease` | 호출자가 종료해야 하는 일회성 수명 책임 |
| Singleton | Runtime instance의 명시적 등록·조회 기반 |
| Primitive | 여러 시스템에서 재사용하는 값 표현과 보조 연산 |

## 관련 문서

- [Bootstrapper](../../Documentation~/modules/core/bootstrapper.md)
- [Singleton과 슬롯](../../Documentation~/modules/core/singleton.md)
- [Core Primitive](../../Documentation~/modules/core/primitive.md)
- [Xeri 구조](../../Documentation~/concepts/architecture.md)
- [소유권과 수명](../../Documentation~/concepts/ownership-and-lifetime.md)
