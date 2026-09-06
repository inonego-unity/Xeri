# 첫 설정

Xeri를 처음 적용할 때는 모든 모듈을 한꺼번에 초기화할 필요가 없습니다. 먼저 프로젝트가 실제로 필요한 범용 책임을 고르고, 프로젝트 Adapter가 Xeri 계약을 연결하도록 구성합니다.

## 1. 애플리케이션 시작 경계를 정한다

초기 Scene 전후에 반드시 준비되어야 하는 전역 기능이 있다면 `Bootstrapper`를 사용합니다.

```text
Application Start
      ↓
BeforeInitialScene Modules
      ↓
Initial Scene Load
      ↓
AfterInitialScene Modules
```

단순히 특정 Scene이나 객체가 활성화될 때만 필요한 기능이라면 Bootstrapper에 넣지 않고 그 기능의 자연스러운 Host가 수명을 소유하게 합니다.

## 2. 프로젝트 Adapter를 둔다

Xeri 타입을 프로젝트 도메인 전체에서 직접 호출하기보다, 프로젝트 용어를 Xeri 계약으로 변환하는 얇은 Adapter/Service/Presenter를 둡니다.

```text
Project Event / Data
       ↓
Project Adapter
       ↓
Xeri API
```

예를 들어 후보 탐색 방식은 프로젝트 Scanner가 담당하고, `UseController`에는 발견한 `UseOffer`만 공급하는 식입니다.
## 3. 수명 소유자를 먼저 정한다

새 기능을 연결하기 전에 다음 질문에 답합니다.

- 누가 `Register`, `Acquire`, `Bind`, `Track` 하는가?
- 언제 `Unregister`, `Release`, `Unbind`, `Dispose` 하는가?
- 부분 초기화가 실패했을 때 이미 획득한 자원은 누가 되돌리는가?

`Lease`나 Handle을 반환하는 API는 반환값을 버리지 말고 자연스러운 상위 수명에 보관합니다.

## 4. 완전 구성 후 공개한다

전역 Registry나 `Current`에 올릴 객체는 필요한 Source와 dependency 구성이 성공한 뒤 등록합니다. 로딩 중간 상태를 다른 시스템이 관찰하지 않게 하는 것이 중요합니다.

```text
Create → Load → Validate → Compose → Register
                 실패 ↘ Cleanup
```

## 5. 작은 흐름부터 검증한다

처음부터 전체 프레임워크를 연결하기보다 각 시스템의 최소 흐름을 먼저 확인합니다.

- Data: Table 하나를 `DataPackage`에 등록하고 `REF<T>`로 읽기
- Generation: 같은 Seed에서 같은 결과가 재현되는지 확인
- UI: Screen 하나를 열고 닫으며 Acquire/Release 대칭 확인
- Tracking: 하나의 Binding을 등록하고 Lease 해제 시 clear되는지 확인

## 관련 문서

- [모듈 선택 가이드](choosing-modules.md)
- [Xeri 통합 패턴](../concepts/integration-patterns.md)
- [소유권과 수명](../concepts/ownership-and-lifetime.md)