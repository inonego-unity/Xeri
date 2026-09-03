/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : InstancedRenderBatchCollection.cs
수정일 : 2026-09-03

# 설명
Opaque/Alpha-Clipped 대량 Mesh instance를 spatial/resource batch로 묶어 Graphics.RenderMeshInstanced로 제출한다.

# 제약사항
Per-instance transparent sorting, indirect rendering, BatchRendererGroup 수명은 담당하지 않는다.
호출자는 resource 수명과 프레임 호출 시점을 소유하며 Add 이후 Build를 완료해야 Render할 수 있다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Rendering;

using inonego;
using inonego.Xeri;
using inonego.Xeri.Primitive;

namespace inonego.Xeri.Rendering
{
    // ============================================================
    /// <summary>
    /// Spatial/resource 기준으로 instance batch를 수집한다.
    /// </summary>
    // ============================================================
    public sealed class InstancedRenderBatchCollection
    {

    #region 내부 데이터

        public const int MaximumInstancesPerDraw = 1023;

        // ============================================================
        /// <summary>
        /// Batch key별 instance와 bounds를 수집한다.
        /// </summary>
        // ============================================================
        private sealed class BatchBuilder
        {

            // ------------------------------------------------------------
            /// <summary>
            /// Builder가 수집하는 고정 batch key.
            /// </summary>
            // ------------------------------------------------------------
            public InstancedRenderBatchKey Key { get; }

            // ------------------------------------------------------------
            /// <summary>
            /// 현재 key에 수집된 instance 목록.
            /// </summary>
            // ------------------------------------------------------------
            public List<InstancedRenderInstance> Instances { get; } = new();


            // ------------------------------------------------------------
            /// <summary>
            /// 수집된 모든 instance를 포함하는 conservative world bounds.
            /// </summary>
            // ------------------------------------------------------------
            public Bounds Bounds { get; private set; }

            // ------------------------------------------------------------
            /// <summary>
            /// Bounds가 한 번 이상 초기화됐는지 여부.
            /// </summary>
            // ------------------------------------------------------------
            public bool HasBounds { get; private set; }

            // ------------------------------------------------------------
            /// <summary>
            /// 고정된 batch key로 transient builder를 만든다.
            /// </summary>
            // ------------------------------------------------------------
            public BatchBuilder(InstancedRenderBatchKey key) : base()
            {
                Key = key;
            }

            // ----------------------------------------------------------------------
            /// <summary>
            /// Instance와 해당 instance의 conservative world bounds를 누적한다.
            /// </summary>
            // ----------------------------------------------------------------------
            public void Add(InstancedRenderInstance instance, Bounds bounds)
            {
                // Instance 목록과 aggregate bounds는 같은 builder 수명에서 함께 누적한다.
                Instances.Add(instance);

                // 첫 instance는 기존 aggregate가 없으므로 전달된 bounds로 바로 초기화한다.
                if (!HasBounds)
                {
                    Bounds = bounds;
                    HasBounds = true;
                    return;
                }

                // 이후 instance는 기존 bounds를 보존하면서 전체 batch envelope로 확장한다.
                var combined = Bounds;
                combined.Encapsulate(bounds);
                Bounds = combined;
            }
        }

        // ======================================================================
        /// <summary>
        /// Frame render path에서 allocation 없이 제출할 fixed instance batch.
        /// </summary>
        // ======================================================================
        private sealed class RenderBatch
        {

            // ------------------------------------------------------------
            /// <summary>
            /// Fixed batch의 spatial/resource/render policy key.
            /// </summary>
            // ------------------------------------------------------------
            public InstancedRenderBatchKey Key { get; }

            // ------------------------------------------------------------
            /// <summary>
            /// Frame render path에서 직접 제출할 fixed instance 배열.
            /// </summary>
            // ------------------------------------------------------------
            public InstancedRenderInstance[] Instances { get; }

            // ------------------------------------------------------------
            /// <summary>
            /// Fixed batch 전체를 포함하는 conservative world bounds.
            /// </summary>
            // ------------------------------------------------------------
            public Bounds Bounds { get; }

            // ------------------------------------------------------------
            /// <summary>
            /// Builder 내용을 fixed array와 bounds로 고정한다.
            /// </summary>
            // ------------------------------------------------------------
            public RenderBatch(BatchBuilder source) : base()
            {
                Key = source.Key;
                Instances = source.Instances.ToArray();
                Bounds = source.Bounds;
            }
        }

    #endregion

    #region 필드

        private readonly Dictionary<InstancedRenderBatchKey, BatchBuilder> builders = new();


        // ------------------------------------------------------------
        /// <summary>
        /// Spatial batching에 사용하는 XZ cell 한 변의 meter 크기.
        /// </summary>
        // ------------------------------------------------------------
        public float SpatialCellSize => spatialCellSize;

        private readonly float spatialCellSize;


        // ------------------------------------------------------------
        /// <summary>
        /// 현재 collection에 수집된 전체 instance 수.
        /// </summary>
        // ------------------------------------------------------------
        public int InstanceCount => instanceCount;

        private int instanceCount = 0;


        // ------------------------------------------------------------
        /// <summary>
        /// Build 이후 고정된 spatial/resource batch 수.
        /// </summary>
        // ------------------------------------------------------------
        public int BatchCount => batches.Count;

        private readonly List<RenderBatch> batches = new();


        // ------------------------------------------------------------
        /// <summary>
        /// 현재 Build 상태의 RenderMeshInstanced 호출 수를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public int DrawCallCount => drawCallCount;

        private int drawCallCount = 0;


        // ----------------------------------------------------------------------
        /// <summary>
        /// 마지막 Add/Clear 이후 현재 batch가 Render 가능한 Build 상태인지 반환한다.
        /// </summary>
        // ----------------------------------------------------------------------
        public bool IsBuilt => isBuilt;

        private bool isBuilt = true;

    #endregion

    #region 생성자

        // --------------------------------------------------------------------------------
        /// <summary>
        /// 지정한 spatial cell 크기로 비어 있는 instanced render collection을 만든다.
        /// </summary>
        // --------------------------------------------------------------------------------
        public InstancedRenderBatchCollection(float spatialCellSize) : base()
        {
            // Spatial key 계산 전 cell 크기 계약을 먼저 확정한다.
            ValidateSpatialCellSize(spatialCellSize);
            this.spatialCellSize = spatialCellSize;
        }

    #endregion

    #region 수집

        // ------------------------------------------------------------
        /// <summary>
        /// 수집 중인 builder와 Build된 render batch를 모두 비운다.
        /// </summary>
        // ------------------------------------------------------------
        public void Clear()
        {
            // Transient builder와 fixed batch를 함께 비워 collection snapshot을 완전히 초기화한다.
            builders.Clear();
            batches.Clear();
            instanceCount = 0;
            drawCallCount = 0;
            isBuilt = true;
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// Mesh instance를 batch에 추가하고 필요하면 기존 Build 상태를 폐기한다.
        /// </summary>
        // ----------------------------------------------------------------------
        public void Add
        (
            Mesh mesh,
            Material material,
            int submeshIndex,
            Matrix4x4 objectToWorld,
            Bounds worldBounds,
            ShadowCastingMode shadowCastingMode = ShadowCastingMode.On,
            bool receiveShadows = true,
            uint renderingLayerMask = 1u
        )
        {
            // Batch key를 만들기 전에 공유 render resource 계약을 보장한다.
            ValidateResource(mesh, material);

            // Build 이후 새 instance가 들어오면 fixed snapshot을 폐기하고 다시 수집 상태로 전환한다.
            if (isBuilt)
            {
                batches.Clear();
                builders.Clear();
                instanceCount = 0;
                drawCallCount = 0;
                isBuilt = false;
            }

            // World bounds 중심을 spatial cell로 양자화해 resource key와 함께 batch identity를 만든다.
            var cell = GetSpatialCell(worldBounds.center);
            var key = new InstancedRenderBatchKey
            (
                cell,
                mesh,
                material,
                submeshIndex,
                shadowCastingMode,
                receiveShadows
            );

            // 같은 key의 builder가 없을 때만 새 transient batch를 만든다.
            if (!builders.TryGetValue(key, out var builder))
            {
                builder = new BatchBuilder(key);
                builders.Add(key, builder);
            }

            // Instance와 bounds를 같은 builder에 누적하고 collection 통계를 함께 갱신한다.
            builder.Add
            (
                new InstancedRenderInstance(objectToWorld, renderingLayerMask),
                worldBounds
            );
            instanceCount++;
        }

    #endregion

    #region Build

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 builder 내용을 frame render용 fixed batch로 고정한다.
        /// </summary>
        // ------------------------------------------------------------
        public void Build()
        {
            // 이미 fixed snapshot이면 불필요한 재구성을 건너뛴다.
            if (isBuilt)
            {
                return;
            }

            // 현재 transient builder만 기준으로 fixed batch와 draw 통계를 다시 계산한다.
            batches.Clear();
            drawCallCount = 0;

            foreach (var pair in builders)
            {
                var builder = pair.Value;
                var batch = new RenderBatch(builder);
                batches.Add(batch);
                drawCallCount += Mathf.CeilToInt
                (
                    batch.Instances.Length / (float)MaximumInstancesPerDraw
                );
            }

            // Build가 끝나면 transient 상태를 폐기하고 render 가능한 immutable snapshot으로 전환한다.
            builders.Clear();
            isBuilt = true;
        }

    #endregion

    #region Render

        // ------------------------------------------------------------
        /// <summary>
        /// Build된 모든 batch를 지정 Unity layer로 현재 frame에 제출한다.
        /// </summary>
        // ------------------------------------------------------------
        public void Render(int layer)
        {
            // Render 경로는 Build로 고정된 snapshot만 소비하도록 상태 계약을 강제한다.
            if (!isBuilt)
            {
                throw new InvalidOperationException("Instanced render collection은 Add 이후 Build가 필요합니다.");
            }

            // Fixed batch 순서대로 현재 frame draw를 제출한다.
            for (var index = 0; index < batches.Count; index++)
            {
                RenderBatchNow(batches[index], layer);
            }
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// Fixed batch 하나를 Unity instance 제한에 맞춰 여러 draw로 제출한다.
        /// </summary>
        // ----------------------------------------------------------------------
        private static void RenderBatchNow(RenderBatch batch, int layer)
        {
            // Batch key의 공통 render policy와 aggregate bounds를 한 RenderParams에 고정한다.
            var renderParams = new RenderParams(batch.Key.Material)
            {
                layer = layer,
                shadowCastingMode = batch.Key.ShadowCastingMode,
                receiveShadows = batch.Key.ReceiveShadows,
                worldBounds = batch.Bounds,
            };

            // Unity instance 상한을 넘지 않도록 fixed instance 배열을 chunk 단위로 제출한다.
            for
            (
                var start = 0;
                start < batch.Instances.Length;
                start += MaximumInstancesPerDraw
            )
            {
                var count = Mathf.Min
                (
                    MaximumInstancesPerDraw,
                    batch.Instances.Length - start
                );
                Graphics.RenderMeshInstanced
                (
                    renderParams,
                    batch.Key.Mesh,
                    batch.Key.SubmeshIndex,
                    batch.Instances,
                    count,
                    start
                );
            }
        }

    #endregion

    #region Spatial key

        // ------------------------------------------------------------
        /// <summary>
        /// World position을 XZ spatial cell 좌표로 양자화한다.
        /// </summary>
        // ------------------------------------------------------------
        private Vector2Int GetSpatialCell(Vector3 position)
        {
            return new Vector2Int
            (
                Mathf.FloorToInt(position.x / spatialCellSize + 0.5f),
                Mathf.FloorToInt(position.z / spatialCellSize + 0.5f)
            );
        }

    #endregion

    #region 필수 계약

        // ------------------------------------------------------------
        /// <summary>
        /// Spatial cell 크기가 유한한 양수인지 확인한다.
        /// </summary>
        // ------------------------------------------------------------
        private static void ValidateSpatialCellSize(float value)
        {
            if (!value.IsFinite() || value <= 0f)
            {
                throw new ArgumentOutOfRangeException
                (
                    nameof(value),
                    "Spatial cell 크기는 유한한 양수여야 합니다."
                );
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Batch key를 만들기 위해 필요한 resource reference를 확인한다.
        /// </summary>
        // ------------------------------------------------------------
        private static void ValidateResource(Mesh mesh, Material material)
        {
            if (mesh == null)
            {
                throw new ArgumentNullException(nameof(mesh));
            }

            if (material == null)
            {
                throw new ArgumentNullException(nameof(material));
            }
        }

    #endregion

    }
}
