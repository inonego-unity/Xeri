---
name: xeri-editor
description: Use when creating PropertyDrawer, DecoratorDrawer, or other editor extensions for com.inonego.xeri.
user-invocable: false
---

# UniXeri 에디터 확장 구현 규칙

## UI Toolkit 기반

모든 에디터 드로어는 **UI Toolkit**으로 구현한다. IMGUI(`OnGUI`, `GetPropertyHeight`) 사용 금지.

```csharp
// ❌ IMGUI
public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) { }

// ✅ UI Toolkit
public override VisualElement CreatePropertyGUI(SerializedProperty property) { }
```

DecoratorDrawer도 동일:

```csharp
public override VisualElement CreatePropertyGUI() { }  // property 없음
```

---

## UXML / USS

단순 드로어는 인라인 VisualElement로 충분 — 별도 파일 불필요.
복잡한 레이아웃이나 재사용 스타일이 필요할 때만 분리한다.

| 상황 | 방식 |
|---|---|
| 요소 수 적음, 스타일 단순 | 인라인 `style.*` |
| 레이아웃 복잡 또는 스타일 재사용 | `.uxml` / `.uss` 파일 분리 |

UXML/USS 파일은 드로어 `.cs` 파일과 같은 `Editor/` 폴더에 둔다.
경로는 `EditorAssetHelper.GetScriptDirectory(typeof(MyDrawer))`로 동적으로 가져온다.

---

## Editor/유틸리티

`Editor/유틸리티/`에 공용 헬퍼가 있다. 직접 구현 전에 확인한다.

### `EditorAssetHelper.GetScriptDirectory(Type)`
스크립트 파일이 위치한 폴더 경로 반환. UXML/USS 로드 시 사용.

```csharp
var dir = EditorAssetHelper.GetScriptDirectory(typeof(MyDrawer));
var uxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>($"{dir}/MyDrawer.uxml");
```

### `SerializedPropertyHelper.GetTargetObject<T>(SerializedProperty)`
SerializedProperty로부터 실제 C# 인스턴스를 리플렉션으로 가져온다.
배열/리스트 요소 경로(`Array.data[n]`)도 지원.

```csharp
var target = SerializedPropertyHelper.GetTargetObject<MyClass>(property);
```

> **⚠ 인스턴스 필드에 저장 금지**
>
> PropertyDrawer 인스턴스는 타입당 하나를 공유한다.
> `GetTargetObject` 결과를 인스턴스 필드에 저장하면, 동일 타입의 다른 필드가
> `CreatePropertyGUI`를 호출할 때 덮어써져 잘못된 객체를 참조하게 된다.
>
> 반드시 `CreatePropertyGUI` 내부 로컬 변수에 저장하고 클로저로 캡처한다.
>
> ```csharp
> // ❌ 인스턴스 필드 — 공유되어 덮어써짐
> private MyClass _target;
> public override VisualElement CreatePropertyGUI(SerializedProperty property)
> {
>     _target = SerializedPropertyHelper.GetTargetObject<MyClass>(property);
>     ...
> }
>
> // ✅ 로컬 변수 — 호출마다 격리됨
> public override VisualElement CreatePropertyGUI(SerializedProperty property)
> {
>     var target = SerializedPropertyHelper.GetTargetObject<MyClass>(property);
>     root.schedule.Execute(() => Refresh(target)).Every(50);
>     ...
> }
> ```
>
> `SerializedProperty` 기반 접근은 이 문제가 없다. `GetTargetObject`를 사용할 때만 주의한다.
