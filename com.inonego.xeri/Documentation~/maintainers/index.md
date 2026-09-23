# 유지보수 문서

이 영역은 일반 사용법보다 내부 책임 경계, 확장 시 주의사항과 검증 절차를 설명합니다.

- [Picker 검증 지침](picker-validation.md)
- [테스트 구조](../../Tests/README.md): Actor/Presentation transaction을 포함한 Runtime 테스트 배치와 검증 경계

Game Entity/Actor 계층을 변경할 때는 primitive와 orchestration을 분리해 검증합니다. `EntityActorController`/`EntityPresentationCoordinator` 개별 계약과 `EntityActorRuntime` 조합 transaction 테스트가 모두 유지되어야 합니다.

공개 사용자 문서에서 구현 세부사항이나 테스트 파일 경로가 길어지면 이 영역으로 분리합니다.
