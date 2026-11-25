/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : ReadOnlyListView.cs
수정일 : 2026-11-25

# 설명
읽기 전용 목록의 원본 데이터를 복사하지 않고 노출하는 런타임 View.
원본 목록의 변경을 즉시 반영하며 구체 View 열거에서는 struct Enumerator를 사용한다.

# 특이사항, 제약사항
원본 목록의 수명과 변경은 호출자가 소유한다. 동시 수정 중 열거는 지원하지 않는다.
View 자체는 직렬화 상태를 소유하지 않는다.
ReadOnlySetView·ReadOnlyDictionaryView와 같은 읽기 전용 struct View이며 소유자가 View 객체를 따로 보관하지 않아도 되고 접근마다 할당하지 않는다.
원본이 IReadOnlyList라서 원소 타입을 넓힌 목록(예: List<Entry>를 IReadOnlyStatus 목록으로)도 감쌀 수 있다.
IReadOnlyList 변수로 받으면 View가 박싱되므로 할당 없이 읽으려면 var나 View 타입으로 받는다.
default 값은 빈 목록처럼 동작하고 생성자는 null 원본을 거절한다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

namespace inonego.Xeri
{
    // ======================================================================
    /// <summary>
    /// <br/> 읽기 전용 목록을 복사하지 않고 읽기 기능만 제공하는 Live View.
    /// <br/> 원본 목록의 변경은 즉시 반영되며 수정 API는 노출하지 않는다.
    /// </summary>
    // ======================================================================
    public readonly struct ReadOnlyListView<T> : IReadOnlyList<T>
    {

    #region 필드

        private readonly IReadOnlyList<T> source;

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// 읽기 전용으로 조회할 원본 목록을 지정한다.
        /// </summary>
        // ------------------------------------------------------------
        public ReadOnlyListView(IReadOnlyList<T> source)
        {
            this.source = source ?? throw new ArgumentNullException(nameof(source));
        }

    #endregion

    #region 조회

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 원본 목록의 요소 수. default View는 0이다.
        /// </summary>
        // ------------------------------------------------------------
        public int Count => source?.Count ?? 0;

        // ----------------------------------------------------------------------
        /// <summary>
        /// 위치의 요소를 조회한다. 범위 밖이면 ArgumentOutOfRangeException을 던진다.
        /// </summary>
        // ----------------------------------------------------------------------
        public T this[int index]
        {
            get
            {
                if (source == null)
                {
                    throw new ArgumentOutOfRangeException(nameof(index));
                }

                return source[index];
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> 구체 foreach에 struct 열거자를 반환한다.
        /// <br/> 열거 중 원본 변경은 허용하지 않는다.
        /// </summary>
        // ------------------------------------------------------------
        public Enumerator GetEnumerator() => new Enumerator(source);

        // ------------------------------------------------------------
        /// <summary>
        /// 읽기 전용 컬렉션 인터페이스 열거자를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();

        // ------------------------------------------------------------
        /// <summary>
        /// 비제네릭 컬렉션 인터페이스 열거자를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    #endregion

    #region 열거자

        // ============================================================
        /// <summary>
        /// 목록 View를 위치 순서대로 읽는 struct 열거자.
        /// </summary>
        // ============================================================
        public struct Enumerator : IEnumerator<T>
        {
            private readonly IReadOnlyList<T> source;
            private int index;

            internal Enumerator(IReadOnlyList<T> source)
            {
                this.source = source;
                index = -1;
            }

            // ------------------------------------------------------------
            /// <summary>
            /// 현재 위치의 요소를 조회한다.
            /// </summary>
            // ------------------------------------------------------------
            public T Current => source[index];

            object IEnumerator.Current => Current;

            // ------------------------------------------------------------
            /// <summary>
            /// 다음 요소로 이동하고 요소가 있으면 true를 반환한다.
            /// </summary>
            // ------------------------------------------------------------
            public bool MoveNext()
            {
                if (source == null)
                {
                    return false;
                }

                index++;
                return index < source.Count;
            }

            // ------------------------------------------------------------
            /// <summary>
            /// 처음 위치로 되돌린다.
            /// </summary>
            // ------------------------------------------------------------
            public void Reset()
            {
                index = -1;
            }

            // ------------------------------------------------------------
            /// <summary>
            /// 해제할 자원이 없다.
            /// </summary>
            // ------------------------------------------------------------
            public void Dispose()
            {
                // NONE
            }
        }

    #endregion

    }
}
