# Xeri Serializable

Xeri Serializable은 Unity 직렬화 환경에서 일반 C# 모델을 다루기 위한 보조 컬렉션, nullable/wrapper, serializer와 값 모델을 제공합니다.

## 개요

이 모듈은 직렬화 가능한 자료구조와 값 표현, managed reference 선택 보조, serializer adapter를 한 영역에 모읍니다. 저장 위치를 읽고 쓰는 책임은 `IO`와 분리합니다.

## 왜 필요한가

Unity 직렬화 규칙과 일반 C# 자료구조 사이에는 Dictionary, 다형 reference, nullable, 값 wrapper처럼 반복되는 간극이 있습니다. Serializable은 이 간극을 보조하되 파일 위치나 프로젝트 저장 workflow까지 소유하지 않습니다.

## 언제 사용하는가

- Unity에 저장 가능한 Dictionary/Ordered Collection이 필요할 때
- `SerializeReference` 타입 선택과 authoring을 편하게 만들 때
- 객체↔JSON/XML 문자열 변환을 저장 위치에서 분리할 때
- Base 값과 Modifier가 합성된 최종 값을 관리할 때

문제에 따라 [Collections](../../Documentation~/modules/serialization/collections.md), [Serializer](../../Documentation~/modules/serialization/serializer.md), [Value](../../Documentation~/modules/serialization/value.md), [Managed Reference](../../Documentation~/modules/serialization/managed-reference.md)에서 시작합니다.

## 어디서 시작하는가

- 여러 효과가 하나의 값을 합성하면 [MValue 사용 가이드](../../Documentation~/guides/serialization/use-mvalue.md)
- Unity에 저장할 Collection이면 [Serializable Collections](../../Documentation~/modules/serialization/collections.md)
- JSON/XML 변환이면 [Serializer](../../Documentation~/modules/serialization/serializer.md)
- 다형 `SerializeReference` authoring이면 [Managed Reference](../../Documentation~/modules/serialization/managed-reference.md)

저장 위치가 문제라면 Serializable이 아니라 [IO](../IO/README.md)에서 시작합니다.

## 책임 범위

### 담당하는 것

- `XDictionary`, `XHashSet`, queue/stack과 ordered/priority collection
- `XNullable`, `XType`, `SerializeReferenceWrapper` 같은 직렬화 보조 타입
- `ISerializer`, `UnityJsonSerializer`, `XeriXmlSerializer`
- `Value`, `MValue`, `RangeValue`와 modifier 기반 값 모델
- managed reference와 일부 inspector/editor 보조 구현

### 담당하지 않는 것

- 파일·Addressables 등 저장 위치 접근
- 특정 게임 도메인의 save/load workflow
- 모든 C# 타입을 Unity가 자동 직렬화할 수 있게 만드는 범용 대체 serializer

## 핵심 개념

| 개념 | 설명 |
|---|---|
| Serializable Collection | Unity 직렬화에 맞춘 collection 표현 |
| Serializer | 객체와 문자열/직렬화 표현 사이의 변환 계약 |
| Value Model | 읽기/쓰기 값과 modifier를 조합하는 Runtime 값 표현 |

## 관련 문서

- [Serializable Value와 Modifier](../../Documentation~/modules/serialization/value.md)
- [Serializable Collections](../../Documentation~/modules/serialization/collections.md)
- [Serializer](../../Documentation~/modules/serialization/serializer.md)
- [Managed Reference와 SerializeReference Picker](../../Documentation~/modules/serialization/managed-reference.md)
- [IO](../IO/README.md)
- [Workspace](../Workspace/README.md)
- [확장 계약](../../Documentation~/concepts/extension-contracts.md)
