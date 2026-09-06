# Serializer

Xeri Serializer는 객체와 문자열 사이의 변환을 `ISerializer` 하나의 계약으로 분리합니다. 파일·메모리·Addressables 같은 저장 위치 접근은 IO 계층에 남깁니다.

## 왜 필요한가

파일 읽기와 JSON/XML 변환을 한 타입에 합치면 저장 위치를 바꾸는 것만으로 직렬화 로직까지 다시 작성하게 됩니다. `ISerializer`는 "객체 ↔ 문자열"만 담당해서 IO, Workspace, Clipboard 같은 서로 다른 저장 경계가 같은 변환기를 재사용할 수 있게 합니다.

## 언제 사용하는가

- 같은 데이터 모델을 파일·메모리 등 여러 위치에 저장할 때
- JSON/XML 포맷을 호출 코드와 분리하고 싶을 때
- Workspace Document Handler나 Editor 도구가 공통 문자열 변환 계약을 필요로 할 때

바이너리 스트림이나 schema-aware protocol처럼 문자열 변환이 중심이 아닌 포맷은 별도 serializer 계약이 더 적합할 수 있습니다.

## 기본 사용

```csharp
var serializer = UnityJsonSerializer.Pretty;

string text = serializer.Serialize(settings);
Settings restored = serializer.Deserialize<Settings>(text);
```

외부 위치에 저장할 때는 `ISerializer` 결과를 IO 계층에 넘기고, serializer 자체에는 파일 경로나 Addressables address를 넣지 않습니다.

## 공통 계약

```text
T object
  ↕ ISerializer
string
  ↕ IDataReader / IDataWriter
external storage
```

`ISerializer`는 `Serialize<T>()`와 `Deserialize<T>()`만 정의하며 실제 포맷과 라이브러리 정책은 구현체 내부에 둡니다.

## Unity JSON

`UnityJsonSerializer`는 `JsonUtility`를 사용하므로 Unity 직렬화 규칙을 그대로 따릅니다.

- `Default`: 일반 출력
- `Pretty`: 들여쓰기 출력
- 인스턴스별 `PrettyPrint` 설정

Unity가 직렬화하지 않는 멤버나 다형성 제약도 `JsonUtility`의 계약을 따릅니다.

## XML

`XeriXmlSerializer`는 `System.Xml.Serialization.XmlSerializer`를 사용합니다. 타입별 `XmlSerializer`를 제네릭 static cache에 보관해 반복 생성 비용을 줄입니다.
XML 출력은 XML declaration을 생략하고 `PrettyPrint`에 따라 들여쓰기를 결정합니다.

## 조합 기준

Serializer는 포맷만 책임집니다.

```text
TextFileIO + UnityJsonSerializer
MemoryIO<string> + XeriXmlSerializer
Document Handler + ISerializer
```

파일 IO와 serializer를 `JsonFileIO`처럼 하나의 타입에 합치기보다 두 계약을 조합하는 방식을 우선합니다.

## 제약과 주의사항

- serializer 구현이 파일 경로나 Addressables address를 알게 하지 않습니다.
- domain validation과 schema migration을 serializer 자체에 넣지 않습니다.
- 기존 외부 포맷의 root 구조가 중요하면 Workspace Document Handler의 저장 형태를 함께 확인합니다.
- JSON과 XML 사이에서 지원 가능한 serialization feature가 다르므로 단순 포맷 교체가 항상 동등하다고 가정하지 않습니다.

## 관련 문서

- [IO](../../../Runtime/IO/README.md)
- [Workspace Document](../../../Runtime/Workspace/Document/README.md)
- [Serializable 모듈](../../../Runtime/Serializable/README.md)
