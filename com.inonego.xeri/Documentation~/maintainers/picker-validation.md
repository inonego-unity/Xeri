# Picker 검증 지침

Editor 수동 테스트는 `TEST_PickerManualEditorWindow`에 있습니다.

```text
Runtime/UI/Picker/TEST/Editor/TEST_PickerManualEditorWindow.cs
```

기본 Picker manual은 preview와 column layout 검증을 담당합니다.

- 긴 `preview-name` ellipsis
- 긴 `preview-sub-label` ellipsis
- 여러 줄 preview desc
- fixed column
- flexible column
- right/center alignment
- sortable false column
- searchable false column
- hidden column
- 긴 table cell text overflow

ListPicker와 DictionaryPicker manual은 facade 선택 흐름 확인용으로 유지합니다.

테스트는 `[Explicit]`, `[Category("Manual")]`로 분리되어 있어 일반 테스트 실행에서 자동으로 멈추지 않습니다.
