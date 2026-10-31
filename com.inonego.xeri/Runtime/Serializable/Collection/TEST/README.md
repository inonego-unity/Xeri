# 컬렉션 테스트 구성 (Unity 7)

컬렉션 런타임, 직렬화, Editor Model, 편집창과 UI를 책임별로 검증한다.

## 폴더와 어셈블리

- **EDIT**: Runtime 동작 계약. 상위 inonego.Xeri.TEST.EDIT.asmref를 사용한다.
  - TEST_CollectionRuntime.cs: 공통 Apply, Update, 직렬화 콜백
  - TEST_XOrdered.cs: 기본 정렬·키 기반 고유성·조회 및 대량 데이터 일관성
  - TEST_XOrdered_Serialization.cs: JSON 직렬화와 관리 참조 공유 계약
  - TEST_XPriorityQueue.cs: Heap과 우선순위
- **Editor**: 상위 inonego.Xeri.Editor.TEST.EDIT.asmref를 사용한다.
  - TEST_CollectionEditor.cs: 공통 픽스처와 변경 확정, Undo
  - Model/: 컬렉션별 직렬화 동기화와 값 검증
  - Integration/: 상세 편집창과 원본 항목 추적
  - View/: ReadOnly 편집 차단과 실제 값 편집창 동작
- **Tests/Editor**: 공통 Editor 테스트 어셈블리.
  - TEST_SerializedPropertyClipboard.cs: 값·관리 참조 링크 복사와 복원

하위 폴더는 상위 .asmref를 상속하므로 새 .asmdef를 만들지 않는다.
기존 테스트 파일과 Unity .meta GUID는 보존한다.

TEST_XOrdered는 기본·키 기반 동작과 직렬화 계약을 분리하되 테스트 값을 공유하고,
TEST_CollectionEditor는 변경 확정·모델·편집창·화면 영역의 픽스처를 공유한다.
이 두 그룹만 필요한 테스트 데이터와 Unity EditorWindow 준비를 위해 partial class를 사용한다.
SerializedPropertyClipboard는 공통 Editor 테스트 어셈블리에서 별도 검증한다.

## 설계 원칙

1. 단순 기본값·인터페이스 형태보다 실제 편집 결과와 상태 보존을 검증한다.
2. 고유 불변식은 개별 검증한다. Dictionary의 키, HashSet의 중복,
   Ordered의 정렬, PriorityQueue의 Heap, Queue와 Stack의 순서는 서로 다르다.
3. 실패한 추가·붙여넣기·확정은 원본 데이터나 Undo 상태를 훼손하지 않아야 한다.
4. SerializedProperty는 기본 값, Generic, 배열·목록, SerializeReference,
   UnityEngine.Object 참조와 null을 구분한다.
5. UI 테스트는 사용자 동작과 최종 상태를 검증하고, 모델 내부 규칙은
   Editor Model 테스트에서 검증한다.
6. 테스트는 programmer·test-design·dev-unity의 현행 로컬 규칙을 따른다.
   헤더에는 # 테스트 구성으로 카테고리를 선언하고 region 번호는 논리적 순서를 사용한다.
   헬퍼와 픽스처는 테스트 region에서 분리한다.
   메서드는 TEST_{ClassName}_{시나리오} 형식을 사용한다.
7. 파일·어셈블리는 실제 asmdef/asmref 경계를 따르고, 여러 줄 선언·호출의 여는 괄호는 다음 줄에 배치한다.

## 실행 방법

빠른 EditMode 대상:
- TEST_SerializedPropertyClipboard
- TEST_CollectionRuntime
- TEST_XOrdered
- TEST_XPriorityQueue

Editor UI 통합: TEST_CollectionEditor

~~~powershell
Unity.exe -batchmode -projectPath "<Unity7-TestProject>" -runTests -testPlatform EditMode -testFilter "inonego.Xeri.TEST.Serializable._Collection.TEST_CollectionEditor" -testResults "<Result.xml>" -logFile "<Editor.log>"
~~~

EditorWindow.ShowUtility 또는 실제 UI 패널을 사용하는 테스트에는
-nographics를 사용하지 않는다. 무그래픽 환경에서는 테스트 대상과 무관한
그래픽 장치 초기화 오류가 발생할 수 있다.

Domain Reload·씬 재열기 검증은 별도 수명주기 통합 프로젝트에서 실행한다.
실패한 기존 테스트는 Ignore로 숨기지 않고 Unity 버전, 테스트 이름,
실패 원인과 결과 XML을 기록한다.
