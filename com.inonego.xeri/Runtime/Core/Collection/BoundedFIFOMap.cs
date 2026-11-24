/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : BoundedFIFOMap.cs
수정일 : 2026-11-24

# 설명
가장 최근에 추가한 N개의 (키, 값)만 보관하는 고정 용량 창.
용량이 가득 찬 상태에서 새 키를 추가하면 가장 오래된 항목을 밀어낸다.
중복 요청 제거, 최근 결과 재전송처럼 "최근 N개 안에 이 키가 있었는가"를 O(1)로 묻는 용도다.

# 특이사항, 제약사항
키 존재 확인은 Dictionary, 추가 순서는 Queue가 맡고 두 구조를 항상 함께 갱신한다. 생성 뒤 정상 경로에서 할당하지 않는다.
이미 있는 키를 다시 추가하면 값과 순서를 바꾸지 않고 false를 반환한다(최신으로 갱신하지 않는다).
개별 제거는 지원하지 않는다. 비우려면 Clear를 쓰며 Clear는 밀어냄으로 보지 않아 OnDropped를 호출하지 않는다.
밀어냄 알림은 새 항목을 담은 뒤에 보내므로 구독자 예외는 호출자에게 전파되지만 창의 상태는 이미 일관된다.
직렬화 상태, Revision, 스레드 안전성을 제공하지 않는다. 열거 중 변경하면 InvalidOperationException이 발생한다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

namespace inonego.Xeri
{
    // ================================================================================
    /// <summary>
    /// <br/> 가장 최근에 추가한 N개의 (키, 값)만 보관하는 고정 용량 창.
    /// <br/> 가득 찬 상태에서 새 키를 추가하면 가장 오래된 항목을 밀어낸다.
    /// </summary>
    // ================================================================================
    public sealed class BoundedFIFOMap<TKey, TValue> : IReadOnlyCollection<KeyValuePair<TKey, TValue>>
    {

    #region 필드

        private readonly Dictionary<TKey, TValue> entries;
        private readonly Queue<TKey> order;

        // ------------------------------------------------------------
        /// <summary>
        /// 보관할 수 있는 최대 항목 수.
        /// </summary>
        // ------------------------------------------------------------
        public int Capacity { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 보관 중인 항목 수.
        /// </summary>
        // ------------------------------------------------------------
        public int Count => entries.Count;

    #endregion

    #region 이벤트

        // ------------------------------------------------------------
        /// <summary>
        /// 가득 찬 창에서 가장 오래된 항목이 밀려날 때 그 키와 값을 알린다.
        /// </summary>
        // ------------------------------------------------------------
        public event Action<TKey, TValue> OnDropped;

    #endregion

    #region 생성자

        // --------------------------------------------------------------------------------
        /// <summary>
        /// <br/> 최대 항목 수(1 이상)와 키 비교자로 창을 만든다.
        /// <br/> 비교자를 생략하면 키 형식의 기본 비교를 쓴다.
        /// </summary>
        // --------------------------------------------------------------------------------
        public BoundedFIFOMap(int capacity, IEqualityComparer<TKey> comparer = null)
        {
            if (capacity < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity), "용량은 1 이상이어야 합니다.");
            }

            Capacity = capacity;
            entries = new Dictionary<TKey, TValue>(capacity, comparer);
            order = new Queue<TKey>(capacity);
        }

    #endregion

    #region 조회

        // ------------------------------------------------------------
        /// <summary>
        /// 키가 현재 창 안에 있는지 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public bool ContainsKey(TKey key) => entries.ContainsKey(key);

        // ------------------------------------------------------------
        /// <summary>
        /// 키가 현재 창 안에 있으면 값을 꺼내고 true를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public bool TryGetValue(TKey key, out TValue value) => entries.TryGetValue(key, out value);

    #endregion

    #region 추가와 비우기

        // --------------------------------------------------------------------------------
        /// <summary>
        /// <br/> 키가 창에 없으면 가장 최근 항목으로 추가하고 true를 반환한다.
        /// <br/> 이미 있으면 값과 순서를 바꾸지 않고 false를 반환한다.
        /// <br/> 가득 찬 창이면 가장 오래된 항목을 먼저 밀어내고 추가한 뒤 OnDropped로 알린다.
        /// </summary>
        // --------------------------------------------------------------------------------
        public bool TryAdd(TKey key, TValue value)
        {
            if (entries.ContainsKey(key))
            {
                return false;
            }

            // 가득 찼으면 가장 오래된 항목을 먼저 빼서 두 구조의 개수를 용량 안에 둔다.
            var hasDropped = false;
            var droppedKey = default(TKey);
            var droppedValue = default(TValue);

            if (entries.Count >= Capacity)
            {
                droppedKey = order.Dequeue();
                droppedValue = entries[droppedKey];
                entries.Remove(droppedKey);
                hasDropped = true;
            }

            entries.Add(key, value);
            order.Enqueue(key);

            // 상태를 모두 갱신한 뒤에 알려서 구독자 예외가 창을 깨뜨리지 않게 한다.
            if (hasDropped)
            {
                OnDropped?.Invoke(droppedKey, droppedValue);
            }

            return true;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 모든 항목을 비운다. 밀어냄이 아니므로 OnDropped를 호출하지 않는다.
        /// </summary>
        // ------------------------------------------------------------
        public void Clear()
        {
            entries.Clear();
            order.Clear();
        }

    #endregion

    #region 열거

        // ------------------------------------------------------------
        /// <summary>
        /// 오래된 항목부터 최근 항목 순서로 열거한다.
        /// </summary>
        // ------------------------------------------------------------
        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            foreach (var key in order)
            {
                yield return new KeyValuePair<TKey, TValue>(key, entries[key]);
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    #endregion

    }
}
