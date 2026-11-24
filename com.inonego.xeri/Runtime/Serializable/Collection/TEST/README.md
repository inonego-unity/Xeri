# 컬렉션 테스트 구성 (Unity 7)

컬렉션 런타임과 직렬화 계약은 EditMode 테스트로, 편집 세션·편집창·표 UI는 Validation 씬에서 수동으로 검증한다.
편집 UI(EditorWindow·표)는 자동 테스트를 두지 않는다.

## 폴더와 어셈블리

- **EDIT**: Runtime 동작 계약. 상위 inonego.Xeri.TEST.EDIT.asmref를 사용한다.
  - TEST_CollectionRuntime.cs: 공통 Apply, Update, 직렬화 콜백
  - TEST_XOrdered.cs: 기본 정렬·키 기반 고유성·조회, 대량 데이터 일관성, Key 인덱스 일관성, JSON 직렬화와 관리 참조 공유 계약
  - TEST_XPriorityQueue.cs: Heap과 우선순위
  - TEST_CollectionClone.cs: 컬렉션 CloneFrom 계약(키 없는·키 있는 XOrdered는 같은 항목 구성으로 대칭 검증), 확장 메서드, 같은 구성 복제의 GC 할당
- **Tests/Editor**: 공통 Editor 테스트 어셈블리.
  - TEST_SerializedPropertyClipboard.cs: 값·관리 참조 링크 복사와 복원
- **Validation**: 수동 검증 씬(CollectionValidation.unity)과 컬렉션 종류별 필드·원소 형식. inonego.Xeri.Collection.Validation(Editor 전용 정의).
  - Validation/Editor/: 검증 원소 형식의 셀 뷰 예시. inonego.Xeri.Collection.Validation.Editor(Editor 플랫폼).

하위 폴더는 상위 .asmref를 상속하므로 새 .asmdef를 만들지 않는다.
기존 테스트 파일과 Unity .meta GUID는 보존한다.

SerializedPropertyClipboard는 공통 Editor 테스트 어셈블리에서 별도 검증한다.

## 설계 원칙

1. 단순 기본값·인터페이스 형태보다 실제 편집 결과와 상태 보존을 검증한다.
2. 고유 불변식은 개별 검증한다. Dictionary의 키, HashSet의 중복,
   Ordered의 정렬, PriorityQueue의 Heap, Queue와 Stack의 순서는 서로 다르다.
3. SerializedProperty는 기본 값, Generic, 배열·목록, SerializeReference,
   UnityEngine.Object 참조와 null을 구분한다.
4. 편집 세션·편집창·표 UI는 Validation 씬에서 실제 Inspector로 확인한다.
5. 테스트는 programmer·test-design·dev-unity의 현행 로컬 규칙을 따른다.
   헤더에는 # 테스트 구성으로 카테고리를 선언하고 region 번호는 논리적 순서를 사용한다.
   헬퍼와 픽스처는 테스트 region에서 분리한다.
   메서드는 TEST_{ClassName}_{시나리오} 형식을 사용한다.
6. 파일·어셈블리는 실제 asmdef/asmref 경계를 따르고, 여러 줄 선언·호출의 여는 괄호는 다음 줄에 배치한다.

## 실행 방법

빠른 EditMode 대상:
- TEST_SerializedPropertyClipboard
- TEST_CollectionRuntime
- TEST_XOrdered
- TEST_XPriorityQueue
- TEST_CollectionClone

Domain Reload·씬 재열기 검증은 별도 수명주기 통합 프로젝트에서 실행한다.
실패한 기존 테스트는 Ignore로 숨기지 않고 Unity 버전, 테스트 이름,
실패 원인과 결과 XML을 기록한다.
