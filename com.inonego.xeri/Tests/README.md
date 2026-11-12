# Xeri Tests

## 개요

패키지 공통 test assembly와 테스트 보조 객체를 두는 영역입니다. 기능별 테스트는 해당 Runtime 하위의 `TEST` 디렉터리에도 존재합니다.

## 검증 구조

- `EDIT`: Edit Mode 검증용 공통 영역
- `PLAY`: Play Mode 검증용 공통 영역
- `Editor`: Editor 전용 검증용 공통 영역
- `Runtime/**/TEST`: 특정 Runtime 기능과 가까이 유지하는 기능별 검증

테스트 배치는 실제 assembly 참조와 검증 대상의 성격을 기준으로 결정합니다.

Core는 공통 Runtime·Editor의 계약을 검사합니다. 기능별 테스트는 자신의 구현 근처에 두되, 별도 패키지의 검증을 Core 테스트로 포함하지 않습니다.

Entity·Actor·Presentation과 Game 기능 테스트는 [Xeri Game](https://github.com/inonego-unity/Xeri-Game)의 `Tests/`가 소유합니다.

## 관련 문서

- [문서 시작점](../Documentation~/index.md)
