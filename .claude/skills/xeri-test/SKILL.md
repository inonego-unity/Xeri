---
name: xeri-test
description: Use when writing test code for com.inonego.xeri.
user-invocable: false
---

# UniXeri 테스트 작성 규칙

## 테스트 파일 region 구성

테스트 코드는 관심사/주제별로 region을 나누고 이름도 그에 맞게 작성한다.
전체를 하나로 통으로 묶지 않으며, 헬퍼 메서드처럼 테스트가 아닌 코드도 적절히 분리한다.

테스트를 추가할 때는 단순히 파일 끝에 붙이지 않는다.
관련 region이 이미 있으면 그 안에 삽입하고, 필요하면 region 구조 자체를 재조정한다.
