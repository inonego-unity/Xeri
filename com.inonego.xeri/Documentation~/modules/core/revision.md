# Revision

Revision은 상태가 바뀔 때마다 오르는 순서 있는 발급 번호입니다. 캡처해 둔 선택·표시·검증 결과가 아직 현재 상태에서 나온 것인지 판단하는 데 씁니다.

## 왜 필요한가

여러 모듈이 변경 카운터를 `int`, `long`, `ulong`으로 따로 들고 "없음"을 0이나 -1로 따로 표현하면, 증가 정책과 비교 규칙이 모듈마다 달라집니다. 같은 번호의 표현과 "캡처한 값이 지금도 현재인가"를 확인하는 방법을 한 곳에 두면 소비 코드가 같은 모양이 됩니다.

## 구성

| 타입 | 역할 |
|---|---|
| `Revision` | 순서 있는 발급 번호 값. 0은 `None`(미발급)이고 발급은 1부터 한다 |
| `IRevisioned` | 주된 Revision 축 하나를 읽기 전용으로 내보내는 소유자 계약 |
| `RevisionMark` | 캡처한 소유자와 번호. 소유자의 현재 번호가 같을 때만 `IsCurrent` |

## 기본 사용

```csharp
public sealed class Gauge : IRevisioned
{
    public Revision Revision => revision;
    private Revision revision;

    public void Set(float value)
    {
        // 변경이 일어났다면 알림보다 먼저 번호를 올린다.
        revision = revision.Inc();
        ...
    }
}

var mark = RevisionMark.Capture(gauge);
...
if (!mark.IsCurrent)
{
    // 캡처한 뒤 gauge가 바뀌었으므로 다시 만든다.
}
```

## 규칙

- 번호를 올리는 시점과 복원 때의 동작은 소유자가 정합니다. `Revision`은 표현과 비교 규칙만 맡습니다.
- `Inc()`는 `checked`라 넘치면 `OverflowException`을 던집니다.
- 같음·순서 비교 연산자만 있고 덧셈·뺄셈과 정수 변환 연산자는 없습니다. 전송·저장용 원시 값은 `ToUInt64()`·`FromUInt64()`로만 얻고 만듭니다.
- 번호는 인스턴스 수명 안에서만 의미가 있습니다. 직렬화하지 않으며 다른 소유자나 재시작 전후의 값끼리는 비교하지 않습니다.
- `RevisionMark`는 소유자 참조를 함께 들고 있어서 번호가 같아도 다른 소유자의 값은 현재로 보지 않습니다. 아무것도 캡처하지 않은 `default` 값은 현재가 아닙니다.
- 축이 여러 개인 소유자는 `IRevisioned`로 가장 주된 축 하나만 내보냅니다.

## 포함하지 않는 것

- 번호를 올리는 장치(카운터 부품): 쓰기 권한은 소유자의 필드 접근 제한이 지킵니다.
- 네트워크 Epoch처럼 번호의 비교 범위를 정하는 개념: 해당 Adapter가 정합니다.
- 저장소가 발급하는 불투명 ID(RevisionID, ETag)와 스키마 버전: 순서가 없거나 수명이 달라 Revision으로 옮기지 않습니다.
