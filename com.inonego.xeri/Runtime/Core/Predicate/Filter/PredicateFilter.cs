/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : PredicateFilter.cs
수정일 : 2026-10-31

# 설명
함수 기반 조건을 범용 IFilter 계약에 연결하는 어댑터.
동일 인스턴스를 반복 사용할 수 있으며 필터의 상태 수명은 호출자가 소유한다.
========================================================================= BLOCK_HEADER_END */

using System;

namespace inonego.Xeri
{
    // ============================================================
    /// <summary>
    /// Func<T, bool> 조건 함수를 IFilter<T> 계약으로 연결한다.
    /// </summary>
    // ============================================================
    public sealed class PredicateFilter<T> : IFilter<T>
    {

    #region 필드

        private readonly Func<T, bool> predicate;

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// 조건 함수가 null이면 모든 값을 허용하도록 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        public PredicateFilter(Func<T, bool> predicate)
        {
            this.predicate = predicate ?? (_ => true);
        }

    #endregion

    #region 메서드

        // ------------------------------------------------------------
        /// <summary>
        /// 주어진 값이 조건 함수에 부합하는지 확인한다.
        /// </summary>
        // ------------------------------------------------------------
        public bool IsMatch(T value) => predicate(value);

    #endregion

    }
}
