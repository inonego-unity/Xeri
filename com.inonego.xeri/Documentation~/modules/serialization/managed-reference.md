# Managed Reference와 SerializeReference Picker

Xeri의 Managed Reference 도구는 Unity `SerializeReference` 필드에서 구현 타입을 선택하고, 필요한 경우 generic argument를 구성하며, 값 복사와 동일 root 내 참조 연결을 지원하는 Editor authoring 시스템입니다.

## 왜 필요한가

`SerializeReference`는 다형 객체를 저장할 수 있지만 Inspector에서 어떤 구현 타입을 만들지, generic argument를 어떻게 고를지, 같은 reference identity를 다시 연결할지 직접 authoring하기 불편합니다. Xeri Picker는 Unity 직렬화 가능성과 선언 타입 제약을 확인한 후보만 보여주고 값/Link 복사 의미도 분리합니다.

## 언제 사용하는가

- interface/abstract field에 여러 구현 타입을 Inspector에서 선택해야 할 때
- `SerializeReference` generic 구현을 authoring해야 할 때
- 같은 serialized root 안에서 동일 managed-reference identity를 공유해야 할 때
- 도메인 Action/Policy처럼 작은 전략 객체를 Asset/Component에 직접 저장할 때

Runtime DI나 Scene object reference lookup을 대체하는 도구는 아닙니다.

## 기본 사용

```csharp
[SerializeReference, SerializeReferencePicker]
private IActionTarget target;
```

Inspector에서 현재 선언 타입에 할당 가능한 구현을 선택하면 새 managed-reference instance가 필드에 저장됩니다. Copy as Value는 독립 복제, Copy as Link는 같은 serialized root 안에서 같은 identity를 재사용한다는 차이가 있습니다.

## 적용 방법

기본 진입점은 `SerializeReferencePickerAttribute`입니다.

```csharp
[SerializeReference, SerializeReferencePicker]
private IActionTarget target;
```

`SerializeReferencePickerAttribute`는 opt-in 표시 계약만 제공하고, 실제 후보 탐색과 UI는 Editor 구현이 담당합니다.

## 전체 흐름

```text
SerializeReference field
        ↓
선언 타입 해석
        ↓
ManagedReferenceTypeCatalog
        ↓
생성 가능한 구현 타입 후보
        ↓
필요하면 generic argument 구성
        ↓
managedReferenceValue 교체
```
## 타입 후보

`ManagedReferenceTypeCatalog`는 현재 선언 타입에 대입할 수 있는 구현 타입 후보를 수집합니다.

- Unity Player assembly의 직렬화 가능 타입을 domain reload 단위로 캐시합니다.
- 추상 클래스와 interface는 직접 생성 후보가 아닙니다.
- 선언 타입과의 관계에서 이미 확정 가능한 generic argument는 먼저 고정합니다.
- 남은 generic parameter만 Picker UI에서 선택하도록 넘깁니다.
- 지원 container와 Unity 기본 직렬화 타입도 generic argument 후보에 포함할 수 있습니다.

후보 목록은 표시 이름으로 정렬되며, 선언 타입에 실제로 대입할 수 있는지 검증된 타입만 노출합니다.

## 값 복사와 Link 복사

Clipboard에는 두 의미가 있습니다.

| 방식 | 의미 |
|---|---|
| Copy/Paste as Value | 실제 runtime type과 직렬화 payload를 복사하고 붙여 넣을 때 새 instance를 생성 |
| Copy/Paste as Link | 같은 serialized root 안에서 동일 managed-reference identity를 다시 연결 |

Value 복사는 `UnityJsonSerializer`를 실제 runtime type으로 호출합니다. Multi-object 편집에서는 각 host마다 별도 역직렬화 결과를 생성하므로 서로 같은 object instance를 공유하지 않습니다.

Link 복사는 Unity의 managed-reference ID와 실제 object identity를 함께 검증합니다. 다른 `UnityEngine.Object` root로는 Link를 넘길 수 없습니다.
## 책임 범위

이 시스템은 `SerializeReference` authoring을 돕지만 runtime DI container나 범용 object graph registry는 아닙니다.

- 구현 타입의 프로젝트 의미를 자동 판단하지 않습니다.
- 다른 serialized root 사이의 object identity 공유를 만들지 않습니다.
- generic type 구성은 Unity에서 직렬화 가능한 후보와 타입 제약을 기준으로 합니다.
- 실제 필드 데이터의 runtime 사용 계약은 해당 도메인 타입이 소유합니다.

## 관련 문서

- [Serializer](serializer.md)
- [Serializable Collections](collections.md)
- [Serializable Value와 Modifier](value.md)
- [확장 계약](../../concepts/extension-contracts.md)
