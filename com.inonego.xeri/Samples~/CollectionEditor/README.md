# Collection Editor Inspector 샘플

Xeri 패키지의 컬렉션 Drawer와 `[CollectionEditor]` 속성을 Unity Inspector에서 직접 확인할 수 있는 샘플 씬입니다.

## 가져오기

1. Unity에서 **Window > Package Manager**를 엽니다.
2. **Xeri** 패키지를 선택하고 **Samples** 탭에서 **Collection Editor Inspector**를 Import합니다.
3. 가져온 `TEST_E_CollectionValidation.unity` 씬을 엽니다.
4. Hierarchy의 `TEST_E Collection Validation` 오브젝트를 선택하고 Inspector에서 각 컬렉션을 펼칩니다.

## 확인할 수 있는 기능

- `XDictionary_RR/RV/VR/VV`: Key·Value 직렬화 조합, 중복·무효 Key 진단, Key 편집
- `XHashSet`, `XQueue`, `XStack`, `XOrdered`, `XPriorityQueue`: 컬렉션별 항목 순서와 편집 정책
- `[CollectionEditor]`가 적용된 `List<T>`와 배열: 검색, 페이지 이동, 항목 추가·삭제·편집
- `[CollectionEditor(Unique = true)]`: 중복 원소 진단
- `SerializeReference`: 다형 관리 참조 및 지원 타입 선택
- `Range`, `Value`, `MValue`, `XNullable` 등의 인접 Inspector 표시

필드 선언과 초기값은 함께 Import되는 `TEST_E.cs`에서 확인할 수 있습니다. 샘플은 API 사용 형태를 보여주는 예시이며, 프로젝트에 필요한 필드만 선택해서 사용하세요.

자세한 적용법은 [Collection Editor Inspector 사용 가이드](../../Documentation~/guides/serialization/edit-collections-in-inspector.md)와 [Serializable Collections API](../../Documentation~/modules/serialization/collections.md)를 참고하세요.
