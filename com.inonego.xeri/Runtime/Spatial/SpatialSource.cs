/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : SpatialSource.cs
수정일 : 2026-10-31

# 설명
월드 지점과 참조를 명시적으로 구별하는 공간 출처 값.
========================================================================= BLOCK_HEADER_END */

using UnityEngine;

namespace inonego.Xeri.Spatial
{
    // ============================================================
    /// <summary>
    /// 공간 위치를 얻는 출처를 구별한다.
    /// </summary>
    // ============================================================
    public enum SpatialSourceKind { None, Point, Reference }

    // ============================================================
    /// <summary>
    /// 월드 지점 또는 외부 참조로 공간 출처를 지정한다.
    /// </summary>
    // ============================================================
    public readonly struct SpatialSource<TReference>
    {

    #region 필드

        public SpatialSourceKind Kind { get; }
        public Vector3 Position { get; }
        public TReference Reference { get; }

    #endregion

    #region 생성자

        private SpatialSource(SpatialSourceKind kind, Vector3 position, TReference reference)
        {
            Kind = kind;
            Position = position;
            Reference = reference;
        }

    #endregion

    #region 생성

        public static SpatialSource<TReference> None => default;

        // ------------------------------------------------------------
        /// <summary>
        /// 지정한 월드 지점으로 공간 출처를 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        public static SpatialSource<TReference> Point(Vector3 position) =>
            new(SpatialSourceKind.Point, position, default);

        // ------------------------------------------------------------
        /// <summary>
        /// 공급자가 관측할 참조로 공간 출처를 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        public static SpatialSource<TReference> FromReference(TReference reference) =>
            new(SpatialSourceKind.Reference, default, reference);

    #endregion

    }
}
