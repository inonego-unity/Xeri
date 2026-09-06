# Instanced Renderer 구성하기

`InstancedRenderBatchCollection`은 많은 Mesh instance를 spatial/resource key로 묶고 `Graphics.RenderMeshInstanced` 호출로 제출할 때 사용합니다. 개별 instance마다 GameObject/Renderer를 만들 필요가 없는 정적·준정적 표시 데이터에 적합합니다.

## 목적

프로젝트의 표시 데이터와 Unity GameObject/Renderer 수를 분리하고, 데이터 변경 시 batch snapshot을 재구성한 뒤 frame render에서는 이미 준비된 instance만 제출합니다.

## 기본 lifecycle

```text
Clear
  ↓
Add × N
  ↓
Build
  ↓
Render every frame
```

`Add()` 이후에는 반드시 `Build()`를 호출해야 `Render()`할 수 있습니다.

## 1. Collection 생성

```csharp
using inonego.Xeri.Rendering;

var batches = new InstancedRenderBatchCollection(spatialCellSize: 16f);
```

Spatial cell은 서로 멀리 떨어진 instance가 하나의 큰 bounds로 묶이는 것을 줄이는 용도입니다. 프로젝트의 표시 밀도와 culling 단위에 맞춰 결정합니다.

## 2. 표시 데이터 추가

```csharp
batches.Clear();

foreach (var item in items)
{
    Matrix4x4 matrix = Matrix4x4.TRS
    (
        item.Position,
        item.Rotation,
        item.Scale
    );

    batches.Add
    (
        mesh,
        material,
        submeshIndex: 0,
        objectToWorld: matrix,
        worldBounds: item.WorldBounds
    );
}

batches.Build();
```
## 3. Frame마다 Render

Build된 snapshot은 데이터가 바뀌기 전까지 반복해서 사용할 수 있습니다.

```csharp
private void LateUpdate()
{
    batches.Render(gameObject.layer);
}
```

Instance data가 바뀌면 다시 `Clear → Add → Build`합니다. `Render()` 호출 자체에서 프로젝트 데이터 모델을 다시 해석하지 않는 편이 좋습니다.

## 어떤 데이터를 instance로 둘 것인가

이 방식은 동일 Mesh/Material을 공유하는 많은 표시 항목에 적합합니다.

- 건물, 상자, 장식물처럼 개별 MonoBehaviour가 필요 없는 표시
- 생성 결과를 대량으로 시각화하는 경우
- transform과 rendering layer mask 정도만 instance마다 달라지는 경우

개별 transparent sorting, 복잡한 per-instance material property, 독립 GameObject lifecycle이 필요한 경우에는 다른 렌더링 경로가 더 적합할 수 있습니다.

## 자원 수명

Collection은 전달한 Mesh와 Material의 생성·파괴를 소유하지 않습니다. 프로젝트 Renderer/Presenter가 shared resource를 준비하고 자신의 수명에서 정리합니다.
## 관련 문서

- [Rendering Instancing](../../modules/rendering/instancing.md)
- [Generation](../../modules/generation/generation.md)
- [Xeri 통합 패턴](../../concepts/integration-patterns.md)