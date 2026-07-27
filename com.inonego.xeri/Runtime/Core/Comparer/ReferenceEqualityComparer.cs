/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : ReferenceEqualityComparer.cs
수정일 : 2026-07-28

# 설명
참조 타입을 인스턴스 동일성으로 비교하는 공용 Equality Comparer.
재정의된 Equals와 GetHashCode의 영향을 받지 않는 컬렉션 비교 기준을 제공한다.
========================================================================= BLOCK_HEADER_END */

using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace inonego.Xeri
{
    // ============================================================
    /// <summary>
    /// 참조 타입을 인스턴스 동일성으로 비교합니다.
    /// </summary>
    // ============================================================
    public sealed class ReferenceEqualityComparer<T> : IEqualityComparer<T>
    where T : class
    {
    #region 필드

        // ------------------------------------------------------------
        /// <summary>
        /// 공유 비교자 인스턴스입니다.
        /// </summary>
        // ------------------------------------------------------------
        public static ReferenceEqualityComparer<T> Instance { get; } = new();

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// 외부 생성을 막고 공유 인스턴스 사용을 강제합니다.
        /// </summary>
        // ------------------------------------------------------------
        private ReferenceEqualityComparer() : base() {}

    #endregion

    #region 메서드

        // ------------------------------------------------------------
        /// <summary>
        /// 두 값이 같은 인스턴스를 참조하는지 반환합니다.
        /// </summary>
        // ------------------------------------------------------------
        public bool Equals(T x, T y) => ReferenceEquals(x, y);

        // ------------------------------------------------------------
        /// <summary>
        /// 인스턴스 참조를 기준으로 해시 코드를 반환합니다.
        /// </summary>
        // ------------------------------------------------------------
        public int GetHashCode(T obj)
        {
            if (ReferenceEquals(obj, null))
            {
                return 0;
            }

            return RuntimeHelpers.GetHashCode(obj);
        }

    #endregion

    }
}
