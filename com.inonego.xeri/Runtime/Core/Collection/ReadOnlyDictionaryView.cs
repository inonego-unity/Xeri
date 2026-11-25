/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : ReadOnlyDictionaryView.cs
수정일 : 2026-11-25

# 설명
Dictionary의 원본 데이터를 복사하지 않고 읽기 전용으로 노출하는 런타임 View.
원본 딕셔너리의 변경을 즉시 반영하며 구체 View 열거에서는 struct Enumerator를 사용한다.

# 특이사항, 제약사항
원본 Dictionary의 수명과 변경은 호출자가 소유한다. 동시 수정 중 열거는 지원하지 않는다.
View 자체는 직렬화 상태를 소유하지 않는다.
ReadOnlyListView·ReadOnlySetView와 같은 읽기 전용 struct View이며 소유자가 View 객체를 따로 보관하지 않아도 되고 접근마다 할당하지 않는다.
IReadOnlyDictionary 변수로 받으면 View가 박싱되므로 할당 없이 읽으려면 var나 View 타입으로 받는다.
Keys와 Values는 원본의 읽기 전용 컬렉션을 그대로 돌려주며 구체 타입으로 읽으면 struct 열거자를 쓴다. 쓰기 호출은 NotSupportedException이다.
default 값은 빈 딕셔너리처럼 동작하고 생성자는 null 원본을 거절한다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

namespace inonego.Xeri
{
    // ======================================================================
    /// <summary>
    /// <br/> Dictionary를 복사하지 않고 읽기 기능만 제공하는 Live View.
    /// <br/> 원본 딕셔너리의 변경은 즉시 반영되며 수정 API는 노출하지 않는다.
    /// </summary>
    // ======================================================================
    public readonly struct ReadOnlyDictionaryView<TKey, TValue> : IReadOnlyDictionary<TKey, TValue>
    {

    #region 필드

        // default View가 읽는 빈 딕셔너리이며 읽기만 하고 수정하지 않는다.
        private static readonly Dictionary<TKey, TValue> empty = new();

        private readonly Dictionary<TKey, TValue> source;

        private Dictionary<TKey, TValue> Source => source ?? empty;

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// 읽기 전용으로 조회할 원본 Dictionary를 지정한다.
        /// </summary>
        // ------------------------------------------------------------
        public ReadOnlyDictionaryView(Dictionary<TKey, TValue> source)
        {
            this.source = source ?? throw new ArgumentNullException(nameof(source));
        }

    #endregion

    #region 조회

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 원본 딕셔너리의 항목 수. default View는 0이다.
        /// </summary>
        // ------------------------------------------------------------
        public int Count => Source.Count;

        // ------------------------------------------------------------
        /// <summary>
        /// 키의 값을 조회한다. 키가 없으면 KeyNotFoundException을 던진다.
        /// </summary>
        // ------------------------------------------------------------
        public TValue this[TKey key] => Source[key];

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 원본 딕셔너리에 지정한 키가 있는지 조회한다.
        /// </summary>
        // ------------------------------------------------------------
        public bool ContainsKey(TKey key) => Source.ContainsKey(key);

        // ------------------------------------------------------------
        /// <summary>
        /// 키의 값을 조회하고 있으면 true를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public bool TryGetValue(TKey key, out TValue value) => Source.TryGetValue(key, out value);

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 원본 딕셔너리의 키 목록을 읽기 전용으로 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public Dictionary<TKey, TValue>.KeyCollection Keys => Source.Keys;

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 원본 딕셔너리의 값 목록을 읽기 전용으로 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public Dictionary<TKey, TValue>.ValueCollection Values => Source.Values;

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> 구체 foreach에 struct 열거자를 반환한다.
        /// <br/> 열거 중 원본 변경은 허용하지 않는다.
        /// </summary>
        // ------------------------------------------------------------
        public Dictionary<TKey, TValue>.Enumerator GetEnumerator() => Source.GetEnumerator();

        // ------------------------------------------------------------
        /// <summary>
        /// 읽기 전용 딕셔너리 인터페이스의 키 열거를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys => Source.Keys;

        // ------------------------------------------------------------
        /// <summary>
        /// 읽기 전용 딕셔너리 인터페이스의 값 열거를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values => Source.Values;

        // ------------------------------------------------------------
        /// <summary>
        /// 읽기 전용 컬렉션 인터페이스 열거자를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator() => Source.GetEnumerator();

        // ------------------------------------------------------------
        /// <summary>
        /// 비제네릭 컬렉션 인터페이스 열거자를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        IEnumerator IEnumerable.GetEnumerator() => Source.GetEnumerator();

    #endregion

    }
}
