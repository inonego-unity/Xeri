/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : BoundedFIFOSet.cs
수정일 : 2026-11-24

# 설명
가장 최근에 추가한 N개의 항목만 보관하는 고정 용량 집합.
값이 필요 없는 중복 제거 창으로, BoundedFIFOMap에 값 없이 키만 담는 얇은 래퍼다.

# 특이사항, 제약사항
동작 규칙은 BoundedFIFOMap과 같다. 이미 있는 항목을 다시 추가하면 순서를 바꾸지 않고 false를 반환한다.
Clear는 밀어냄이 아니므로 OnDropped를 호출하지 않는다. 직렬화 상태와 스레드 안전성을 제공하지 않는다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

namespace inonego.Xeri
{
    // ================================================================================
    /// <summary>
    /// <br/> 가장 최근에 추가한 N개의 항목만 보관하는 고정 용량 집합.
    /// <br/> 가득 찬 상태에서 새 항목을 추가하면 가장 오래된 항목을 밀어낸다.
    /// </summary>
    // ================================================================================
    public sealed class BoundedFIFOSet<T> : IReadOnlyCollection<T>
    {

    #region 필드

        private readonly BoundedFIFOMap<T, bool> map;

        // ------------------------------------------------------------
        /// <summary>
        /// 보관할 수 있는 최대 항목 수.
        /// </summary>
        // ------------------------------------------------------------
        public int Capacity => map.Capacity;

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 보관 중인 항목 수.
        /// </summary>
        // ------------------------------------------------------------
        public int Count => map.Count;

    #endregion

    #region 이벤트

        // ------------------------------------------------------------
        /// <summary>
        /// 가득 찬 집합에서 가장 오래된 항목이 밀려날 때 그 항목을 알린다.
        /// </summary>
        // ------------------------------------------------------------
        public event Action<T> OnDropped;

    #endregion

    #region 생성자

        // --------------------------------------------------------------------------------
        /// <summary>
        /// <br/> 최대 항목 수(1 이상)와 비교자로 집합을 만든다.
        /// <br/> 비교자를 생략하면 항목 형식의 기본 비교를 쓴다.
        /// </summary>
        // --------------------------------------------------------------------------------
        public BoundedFIFOSet(int capacity, IEqualityComparer<T> comparer = null)
        {
            map = new BoundedFIFOMap<T, bool>(capacity, comparer);
            map.OnDropped += HandleDropped;
        }

    #endregion

    #region 조회와 추가

        // ------------------------------------------------------------
        /// <summary>
        /// 항목이 현재 집합 안에 있는지 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public bool Contains(T item) => map.ContainsKey(item);

        // --------------------------------------------------------------------------------
        /// <summary>
        /// <br/> 항목이 없으면 가장 최근 항목으로 추가하고 true를 반환한다.
        /// <br/> 이미 있으면 순서를 바꾸지 않고 false를 반환한다.
        /// </summary>
        // --------------------------------------------------------------------------------
        public bool TryAdd(T item) => map.TryAdd(item, true);

        // ------------------------------------------------------------
        /// <summary>
        /// 모든 항목을 비운다. 밀어냄이 아니므로 OnDropped를 호출하지 않는다.
        /// </summary>
        // ------------------------------------------------------------
        public void Clear() => map.Clear();

    #endregion

    #region 열거

        // ------------------------------------------------------------
        /// <summary>
        /// 오래된 항목부터 최근 항목 순서로 열거한다.
        /// </summary>
        // ------------------------------------------------------------
        public IEnumerator<T> GetEnumerator()
        {
            foreach (var pair in map)
            {
                yield return pair.Key;
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    #endregion

    #region 이벤트 핸들러

        private void HandleDropped(T item, bool value)
        {
            OnDropped?.Invoke(item);
        }

    #endregion

    }
}
