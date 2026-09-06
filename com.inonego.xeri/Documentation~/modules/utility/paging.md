# Paging

Xeri Paging은 컬렉션 자체를 소유하지 않고 전체 개수, 페이지 크기와 현재 페이지 index만 관리하는 상태 모델입니다.

## 왜 필요한가

UI 목록마다 페이지 수 계산, index clamp, 페이지 크기 변경 시 현재 위치 보존을 다시 구현하면 미세한 경계 규칙이 달라집니다. `Paginator`는 데이터 자체를 알지 않고 페이징 상태와 현재 slice 범위만 일관되게 계산합니다.

## 언제 사용하는가

- 검색/필터 결과를 페이지 단위로 표시할 때
- 데이터 Source와 UI를 분리한 채 공통 Page 상태가 필요할 때
- `PerPage` 변경 시 사용자가 보고 있던 첫 항목 위치를 최대한 유지하고 싶을 때

무한 스크롤이나 cursor 기반 서버 pagination은 다른 상태 모델이 더 적합합니다.

## 기본 사용

```csharp
var paginator = new Paginator(perPage: 20)
{
    TotalCount = filteredItems.Count,
};

PageRange range = paginator.Range;
for (var i = range.BeginIndex; i < range.EndIndex; i++)
{
    Show(filteredItems[i]);
}

paginator.MoveNext();
```

Paginator는 정렬·필터·실제 slice 생성을 하지 않습니다. 먼저 프로젝트가 전체 결과를 결정하고, 그 개수와 Range만 Paging에 맡깁니다.

## 핵심 상태

`Paginator`는 다음 값을 관리합니다.

- `TotalCount`: 전체 항목 수
- `PerPage`: 페이지당 항목 수
- `PageIndex`: 0-based 현재 페이지
- `PageCount`: 전체 페이지 수
- `Range`: 현재 페이지의 `[BeginIndex, EndIndex)` 범위

빈 목록에서는 `PageCount`와 `PageNumber`가 0이고 `Range`는 `(0, 0)`입니다.

## 상태 보정

TotalCount나 PageIndex가 바뀌면 현재 유효 페이지 범위로 자동 clamp합니다.

`PerPage`를 변경할 때는 기존 `Range.BeginIndex`를 기준으로 새 PageIndex를 계산해 사용자가 보고 있던 첫 항목 위치를 가능한 한 유지합니다.

## 이동 API

`MoveFirst`, `MovePrev`, `MoveNext`, `MoveLast`, `MoveTo`를 제공합니다. 이동 메서드 역시 최종 상태에서 유효한 PageIndex로 clamp됩니다.
## 이벤트

상태 변경은 세부 이벤트 뒤 통합 `OnChange` 순서로 알립니다.

- `OnTotalCountChange`
- `OnPerPageChange`
- `OnPageIndexChange`
- `OnChange`

따라서 `OnChange` 구독자는 이미 보정된 최신 상태를 읽을 수 있습니다.

## PageRange

`PageRange`는 실제 컬렉션을 자르지 않고 0-based slice 범위만 표현합니다. `EndIndex`는 포함하지 않습니다.

호출자는 `Range.BeginIndex`와 `Range.Count`를 자신의 list/array/query에 적용합니다.

## 책임 범위

- Paging 상태와 범위 계산
- 페이지 이동 가능 여부
- 입력 상태 검증과 clamp

데이터 정렬, 필터, 실제 slice 생성과 UI 표시 방식은 호출자가 소유합니다.

## 관련 문서

- [Picker](../../../Runtime/UI/Picker/README.md)
- [Utility 모듈](../../../Runtime/유틸리티/README.md)
