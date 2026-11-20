/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : InstancedRenderBatchKey.cs
수정일 : 2026-09-03

# 설명
RenderMeshInstanced draw를 공유할 spatial/resource/render policy identity를 정의한다.

# 제약사항
Mesh와 Material은 Add 시점의 Unity EntityId로 비교하여 key hash를 안정적으로 유지한다.
========================================================================= BLOCK_HEADER_END */

using System;

using UnityEngine;
using UnityEngine.Rendering;

namespace inonego.Xeri.Rendering
{
    // ================================================================================
    /// <summary>
    /// 같은 spatial cell과 render resource/policy를 공유하는 instance batch의 key.
    /// </summary>
    // ================================================================================
    internal readonly struct InstancedRenderBatchKey : IEquatable<InstancedRenderBatchKey>
    {

    #region 필드

        // ------------------------------------------------------------
        /// <summary>
        /// XZ spatial cell 좌표.
        /// </summary>
        // ------------------------------------------------------------
        public Vector2Int SpatialCell { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// Batch가 공유하는 Mesh.
        /// </summary>
        // ------------------------------------------------------------
        public Mesh Mesh { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// Batch가 공유하는 Material.
        /// </summary>
        // ------------------------------------------------------------
        public Material Material { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// Mesh에서 제출할 submesh index.
        /// </summary>
        // ------------------------------------------------------------
        public int SubmeshIndex { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// Batch가 공유하는 shadow casting policy.
        /// </summary>
        // ------------------------------------------------------------
        public ShadowCastingMode ShadowCastingMode { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// Batch가 shadow를 수신하는지 여부.
        /// </summary>
        // ------------------------------------------------------------
        public bool ReceiveShadows { get; }

        private readonly EntityId meshEntityID;
        private readonly EntityId materialEntityID;

    #endregion

    #region 생성자

        // --------------------------------------------------------------------------------
        /// <summary>
        /// Spatial cell과 draw resource/policy를 하나의 stable batch identity로 고정한다.
        /// </summary>
        // --------------------------------------------------------------------------------
        public InstancedRenderBatchKey
        (
            Vector2Int spatialCell,
            Mesh mesh,
            Material material,
            int submeshIndex,
            ShadowCastingMode shadowCastingMode,
            bool receiveShadows
        )
        {
            SpatialCell = spatialCell;
            Mesh = mesh;
            Material = material;
            SubmeshIndex = submeshIndex;
            ShadowCastingMode = shadowCastingMode;
            ReceiveShadows = receiveShadows;
            meshEntityID = mesh.GetEntityId();
            materialEntityID = material.GetEntityId();
        }

    #endregion

    #region 비교

        // ----------------------------------------------------------------------
        /// <summary>
        /// 두 batch key가 같은 spatial/resource/render policy인지 반환한다.
        /// </summary>
        // ----------------------------------------------------------------------
        public bool Equals(InstancedRenderBatchKey other)
        {
            return
                SpatialCell == other.SpatialCell &&
                meshEntityID.Equals(other.meshEntityID) &&
                materialEntityID.Equals(other.materialEntityID) &&
                SubmeshIndex == other.SubmeshIndex &&
                ShadowCastingMode == other.ShadowCastingMode &&
                ReceiveShadows == other.ReceiveShadows;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Object 값이 같은 batch key인지 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public override bool Equals(object obj)
        {
            return obj is InstancedRenderBatchKey other && Equals(other);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Stable resource EntityId와 policy로 hash code를 만든다.
        /// </summary>
        // ------------------------------------------------------------
        public override int GetHashCode()
        {
            unchecked
            {
                var hash = SpatialCell.GetHashCode();
                hash = hash * 397 ^ meshEntityID.GetHashCode();
                hash = hash * 397 ^ materialEntityID.GetHashCode();
                hash = hash * 397 ^ SubmeshIndex;
                hash = hash * 397 ^ (int)ShadowCastingMode;
                hash = hash * 397 ^ ReceiveShadows.GetHashCode();
                return hash;
            }
        }

    #endregion

    }
}
