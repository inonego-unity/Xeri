/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : ReadOnlySetView.cs
수정일 : 2026-11-15

# 설명
HashSet의 원본 데이터를 복사하지 않고 읽기 전용으로 노출하는 런타임 View.
원본 집합의 변경을 즉시 반영하며 구체 View 열거에서는 struct Enumerator를 사용한다.

# 특이사항, 제약사항
원본 HashSet의 수명과 변경은 호출자가 소유한다. 동시 수정 중 열거는 지원하지 않는다.
View 자체는 직렬화 상태를 소유하지 않는다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

namespace inonego.Xeri
{
    // ============================================================
    /// <summary>
    /// <br/> HashSet을 복사하지 않고 읽기 기능만 제공하는 Live View.
    /// <br/> 원본 집합의 변경은 즉시 반영되며 수정 API는 노출하지 않는다.
    /// </summary>
    // ============================================================
    public sealed class ReadOnlySetView<T> : IReadOnlyCollection<T>
    {

    #region 필드

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 원본 HashSet의 요소 수.
        /// </summary>
        // ------------------------------------------------------------
        public int Count => source.Count;

        private readonly HashSet<T> source;

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// 읽기 전용으로 조회할 원본 HashSet을 지정한다.
        /// </summary>
        // ------------------------------------------------------------
        public ReadOnlySetView(HashSet<T> source)
        {
            this.source = source ?? throw new ArgumentNullException(nameof(source));
        }

    #endregion

    #region 조회

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 원본 집합에 지정한 요소가 있는지 조회한다.
        /// </summary>
        // ------------------------------------------------------------
        public bool Contains(T item) => source.Contains(item);

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> 구체 foreach에 struct 열거자를 반환한다.
        /// <br/> 열거 중 원본 변경은 허용하지 않는다.
        /// </summary>
        // ------------------------------------------------------------
        public HashSet<T>.Enumerator GetEnumerator() => source.GetEnumerator();

        // ------------------------------------------------------------
        /// <summary>
        /// 읽기 전용 컬렉션 인터페이스 열거자를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        IEnumerator<T> IEnumerable<T>.GetEnumerator() => source.GetEnumerator();

        // ------------------------------------------------------------
        /// <summary>
        /// 비제네릭 컬렉션 인터페이스 열거자를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        IEnumerator IEnumerable.GetEnumerator() => source.GetEnumerator();

    #endregion

    }
}
