# Rendering Instancing

Xeri Rendering Instancing은 대량의 Opaque/Alpha-Clipped Mesh instance를 공간과 render resource 기준으로 묶어 `Graphics.RenderMeshInstanced`로 제출하는 batch 수집기입니다.

## 왜 필요한가

동일한 Mesh/Material을 공유하는 수백~수천 개 표시 항목을 각각 GameObject와 Renderer로 만들면 표시 데이터보다 Component 수명과 Transform 관리 비용이 더 커질 수 있습니다. 이 시스템은 프로젝트가 계산한 최종 transform/bounds를 받아 draw 단위로만 묶어서 표시 모델과 Scene 오브젝트 수를 분리합니다.

## 언제 사용하는가

- 많은 항목이 동일 Mesh/Material을 공유할 때
- 개별 MonoBehaviour나 GameObject 상호작용이 필요 없는 표시 데이터일 때
- 생성 결과나 대량 장식물을 spatial batch로 제출하고 싶을 때

Per-instance transparent sorting, 독립 material property, GameObject lifecycle이 핵심인 대상에는 다른 렌더링 경로가 더 적절할 수 있습니다.

## 기본 사용

```csharp
var batches = new InstancedRenderBatchCollection(16f);

batches.Clear();
foreach (var item in items)
{
    batches.Add(mesh, material, 0, item.Matrix, item.Bounds);
}
batches.Build();

// frame render 경계
batches.Render(layer);
```

데이터가 바뀔 때 `Clear → Add → Build`, frame마다 `Render`만 호출하는 구성이 일반적입니다. 자세한 절차는 [Instanced Renderer 구성하기](../../guides/rendering/build-instanced-renderer.md)를 참고합니다.

## 상태 흐름

```text
Add
 ↓
Transient BatchBuilder
 ↓ Build
Fixed RenderBatch snapshot
 ↓ Render
현재 Frame draw 제출
```

`Add()` 뒤에는 반드시 `Build()`를 완료해야 `Render()`할 수 있습니다. Build 이후 새 instance를 추가하면 기존 snapshot은 폐기되고 다시 수집 상태로 돌아갑니다.

## Batch Key

Instance는 다음 값이 모두 같은 경우 같은 batch 후보가 됩니다.

- XZ spatial cell
- Mesh
- Material
- submesh index
- shadow casting mode
- receive shadows

Mesh와 Material은 Add 시점의 Unity `EntityId`를 key에 보관해 hash identity를 안정적으로 유지합니다.
## Spatial Batch

`SpatialCellSize`로 world bounds 중심의 XZ 위치를 cell로 양자화합니다. 같은 resource라도 멀리 떨어진 instance는 다른 spatial batch가 되어 conservative world bounds와 culling 범위를 분리합니다.

각 batch는 포함 instance들의 world bounds를 합친 envelope를 보관합니다.

## Draw 분할

Unity instance 제한에 맞춰 하나의 batch도 최대 1023개 단위로 여러 draw에 나눠 제출합니다.

`DrawCallCount`는 Build된 snapshot 기준 예상 `RenderMeshInstanced` 호출 수입니다.

## 책임 범위

- spatial/resource 기준 instance grouping
- fixed render snapshot 생성
- conservative world bounds
- `RenderMeshInstanced` 제출과 1023개 chunking

다음은 담당하지 않습니다.

- per-instance transparent sorting
- indirect rendering
- `BatchRendererGroup` 수명
- Mesh/Material resource 소유와 해제
- frame에서 언제 Render할지 결정하는 scheduling

## 제약과 주의사항

- `SpatialCellSize`는 유한한 양수여야 합니다.
- Mesh와 Material은 null일 수 없습니다.
- Add 시 전달한 transform, bounds와 rendering layer mask는 호출자가 정확하게 계산해야 합니다.
- 투명 object처럼 instance별 정렬이 필요한 표현에는 이 경로를 사용하지 않습니다.

## 관련 문서

- [Rendering 모듈](../../../Runtime/Rendering/README.md)
