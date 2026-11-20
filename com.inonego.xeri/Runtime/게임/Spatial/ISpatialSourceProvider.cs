/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : ISpatialSourceProvider.cs
수정일 : 2026-10-31

# 설명
외부 참조의 월드 위치와 선택적 회전을 한 번에 조회한다.
========================================================================= BLOCK_HEADER_END */

using UnityEngine;

namespace inonego.Xeri.Spatial
{
    // ============================================================
    /// <summary>
    /// 외부 참조의 위치와 선택적 회전을 함께 조회한다.
    /// </summary>
    // ============================================================
    public interface ISpatialSourceProvider<TReference>
    {
        // ------------------------------------------------------------
        /// <summary>
        /// 참조 위치와 선택적 회전을 단일 관측으로 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        bool TryResolve
        (
            in TReference reference,
            out Vector3 position,
            out Quaternion rotation,
            out bool hasRotation
        );
    }
}
