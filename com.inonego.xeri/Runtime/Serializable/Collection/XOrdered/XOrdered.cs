/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : XOrdered.cs
수정일 : 2026-09-29

# 설명
명시적 Order를 기준으로 항상 정렬 상태를 유지하는 Unity 직렬화 가능 컬렉션을 정의한다.
Keyed 변형은 Order와 독립적인 unique Key lookup을 제공한다.

# 특이사항, 제약사항
Entry 목록이 직렬화 authoritative state이며 Key 조회도 해당 목록을 기준으로 수행한다.
동일 Order는 기존 항목 뒤에 삽입하여 안정적인 순서를 유지한다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace inonego.Xeri.Serializable
{
    // ============================================================
    /// <summary>
    /// <br/> Order 오름차순을 유지하는 직렬화 가능 컬렉션.
    /// <br/> 동일 Order에서는 기존 항목 뒤에 새 항목을 삽입한다.
    /// </summary>
    // ============================================================
    [Serializable]
    public class XOrdered<TOrder, TValue> :
        IReadOnlyXOrdered<TOrder, TValue>,
        ISerializedCollectionNormalizer
    where TOrder : struct
    where TValue : class
    {

    #region 내부 데이터

        // ============================================================
        /// <summary>
        /// Order와 Value를 함께 가지는 읽기 전용 항목.
        /// </summary>
        // ============================================================
        [Serializable]
        public sealed class Entry
        {
            // ------------------------------------------------------------
            /// <summary>
            /// Order 값을 반환하거나 설정한다.
            /// </summary>
            // ------------------------------------------------------------
            public TOrder Order => order;

            [SerializeField]
            private TOrder order;

            // ------------------------------------------------------------
            /// <summary>
            /// 현재 값을 반환한다.
            /// </summary>
            // ------------------------------------------------------------
            public TValue Value => value;

            [SerializeReference]
            private TValue value;

            // ------------------------------------------------------------
            /// <summary>
            /// Entry 인스턴스를 초기화한다.
            /// </summary>
            // ------------------------------------------------------------
            internal Entry(TOrder order, TValue value)
            {
                this.order = order;
                this.value = value;
            }

            // ------------------------------------------------------------
            /// <summary>
            /// Order와 Value를 각각 출력한다.
            /// </summary>
            // ------------------------------------------------------------
            public void Deconstruct(out TOrder order, out TValue value)
            {
                order = Order;
                value = Value;
            }
        }

    #endregion

    #region 필드

        [SerializeField]
        private List<Entry> entries = new();

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 항목 수를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public int Count => entries.Count;

        // ------------------------------------------------------------
        /// <summary>
        /// 지정한 인덱스의 항목을 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public Entry this[int index] => entries[index];

    #endregion

    #region 항목 관리

        // ------------------------------------------------------------
        /// <summary>
        /// Order에 따른 정렬 위치에 Value를 추가한다.
        /// </summary>
        // ------------------------------------------------------------
        public void Add(TOrder order, TValue value)
        {
            var index = FindIndex(order);

            entries.Insert(index, new Entry(order, value));
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Value와 같은 첫 항목을 제거한다.
        /// </summary>
        // ------------------------------------------------------------
        public bool Remove(TValue value)
        {
            var index = IndexOf(value);

            if (index < 0) return false;

            entries.RemoveAt(index);

            return true;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 지정한 Value의 포함 여부를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public bool Contains(TValue value) => IndexOf(value) >= 0;

        // ------------------------------------------------------------
        /// <summary>
        /// Value와 같은 첫 항목의 index를 반환한다. 없으면 -1.
        /// </summary>
        // ------------------------------------------------------------
        public int IndexOf(TValue value)
        {
            for (var index = 0; index < entries.Count; index++)
            {
                if (EqualityComparer<TValue>.Default.Equals(entries[index].Value, value))
                {
                    return index;
                }
            }

            return -1;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 모든 항목을 제거한다.
        /// </summary>
        // ------------------------------------------------------------
        public void Clear() => entries.Clear();

    #endregion

    #region 정렬

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 Entry 목록을 Order 기준으로 안정 정렬한다.
        /// </summary>
        // ------------------------------------------------------------
        internal void NormalizeOrder()
        {
            var comparer = Comparer<TOrder>.Default;

            for (var index = 1; index < entries.Count; index++)
            {
                var entry = entries[index];
                var insertIndex = index - 1;

                while (insertIndex >= 0
                    && comparer.Compare(entries[insertIndex].Order, entry.Order) > 0)
                {
                    entries[insertIndex + 1] = entries[insertIndex];
                    insertIndex--;
                }

                entries[insertIndex + 1] = entry;
            }
        }

        void ISerializedCollectionNormalizer.NormalizeSerializedState()
        {
            NormalizeOrder();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 동일 Order 그룹의 오른쪽 끝 다음 index를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        private int FindIndex(TOrder order)
        {
            var comparer = Comparer<TOrder>.Default;
            var low = 0;
            var high = entries.Count;

            while (low < high)
            {
                var middle = low + ((high - low) / 2);

                if (comparer.Compare(entries[middle].Order, order) <= 0)
                {
                    low = middle + 1;
                }
                else
                {
                    high = middle;
                }
            }

            return low;
        }

    #endregion

    #region 열거

        // ------------------------------------------------------------
        /// <summary>
        /// GetEnumerator 결과를 조회한다.
        /// </summary>
        // ------------------------------------------------------------
        public List<Entry>.Enumerator GetEnumerator() => entries.GetEnumerator();

        IEnumerator<Entry> IEnumerable<Entry>.GetEnumerator() => entries.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => entries.GetEnumerator();

    #endregion

    }

    // ================================================================================
    /// <summary>
    /// <br/> Order 오름차순과 unique Key lookup을 함께 제공하는 직렬화 가능 컬렉션.
    /// <br/> Order, Key, Value는 하나의 Entry가 소유하며 Key는 정렬 순서와 독립적이다.
    /// </summary>
    // ================================================================================
    [Serializable]
    public class XOrdered<TOrder, TKey, TValue> :
        IReadOnlyXOrdered<TOrder, TKey, TValue>,
        ISerializedCollectionNormalizer
    where TOrder : struct
    where TKey : IEquatable<TKey>
    where TValue : class
    {

    #region 내부 데이터

        // ============================================================
        /// <summary>
        /// Order, Key, Value를 함께 가지는 읽기 전용 항목.
        /// </summary>
        // ============================================================
        [Serializable]
        public sealed class Entry
        {
            // ------------------------------------------------------------
            /// <summary>
            /// Order 값을 반환하거나 설정한다.
            /// </summary>
            // ------------------------------------------------------------
            public TOrder Order => order;

            [SerializeField]
            private TOrder order;

            // ------------------------------------------------------------
            /// <summary>
            /// 직렬화할 키 값을 보관한다.
            /// </summary>
            // ------------------------------------------------------------
            public TKey Key => key;

            [SerializeField]
            private TKey key;

            // ------------------------------------------------------------
            /// <summary>
            /// 현재 값을 반환한다.
            /// </summary>
            // ------------------------------------------------------------
            public TValue Value => value;

            [SerializeReference]
            private TValue value;

            // ------------------------------------------------------------
            /// <summary>
            /// Entry 인스턴스를 초기화한다.
            /// </summary>
            // ------------------------------------------------------------
            internal Entry(TOrder order, TKey key, TValue value)
            {
                this.order = order;
                this.key = key;
                this.value = value;
            }

            // ------------------------------------------------------------
            /// <summary>
            /// Order, Key, Value를 각각 출력한다.
            /// </summary>
            // ------------------------------------------------------------
            public void Deconstruct(out TOrder order, out TKey key, out TValue value)
            {
                order = Order;
                key = Key;
                value = Value;
            }
        }

    #endregion

    #region 필드

        [SerializeField]
        private List<Entry> entries = new();

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 항목 수를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public int Count => entries.Count;

        // ------------------------------------------------------------
        /// <summary>
        /// 지정한 인덱스의 항목을 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public Entry this[int index] => entries[index];

    #endregion

    #region 항목 관리

        // ------------------------------------------------------------
        /// <summary>
        /// Order에 따른 정렬 위치에 unique Key와 Value를 추가한다.
        /// </summary>
        // ------------------------------------------------------------
        public void Add(TOrder order, TKey key, TValue value)
        {
            for (var index = 0; index < entries.Count; index++)
            {
                if (EqualityComparer<TKey>.Default.Equals(entries[index].Key, key))
                {
                    throw new ArgumentException($"이미 존재하는 키({key})입니다.", nameof(key));
                }
            }

            var entry = new Entry(order, key, value);
            var insertIndex = FindIndex(order);

            entries.Insert(insertIndex, entry);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Key에 해당하는 항목을 제거한다.
        /// </summary>
        // ------------------------------------------------------------
        public bool Remove(TKey key)
        {
            for (var index = 0; index < entries.Count; index++)
            {
                if (!EqualityComparer<TKey>.Default.Equals(entries[index].Key, key)) continue;

                entries.RemoveAt(index);
                return true;
            }

            return false;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 모든 항목을 제거한다.
        /// </summary>
        // ------------------------------------------------------------
        public void Clear() => entries.Clear();

    #endregion

    #region Key 조회

        // ------------------------------------------------------------
        /// <summary>
        /// 지정한 Key의 포함 여부를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public bool ContainsKey(TKey key)
        {
            for (var index = 0; index < entries.Count; index++)
            {
                if (EqualityComparer<TKey>.Default.Equals(entries[index].Key, key)) return true;
            }

            return false;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Key에 해당하는 Value를 조회한다.
        /// </summary>
        // ------------------------------------------------------------
        public bool TryGetValue(TKey key, out TValue value)
        {
            for (var index = 0; index < entries.Count; index++)
            {
                if (!EqualityComparer<TKey>.Default.Equals(entries[index].Key, key)) continue;

                value = entries[index].Value;
                return true;
            }

            value = default;
            return false;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// TryGetEntry 동작을 시도하고 성공 여부를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public bool TryGetEntry(TKey key, out Entry entry)
        {
            for (var index = 0; index < entries.Count; index++)
            {
                if (!EqualityComparer<TKey>.Default.Equals(entries[index].Key, key)) continue;

                entry = entries[index];
                return true;
            }

            entry = default;
            return false;
        }

    #endregion

    #region 정렬

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 Entry 목록을 Order 기준으로 안정 정렬한다.
        /// </summary>
        // ------------------------------------------------------------
        internal void NormalizeOrder()
        {
            var comparer = Comparer<TOrder>.Default;

            for (var index = 1; index < entries.Count; index++)
            {
                var entry = entries[index];
                var insertIndex = index - 1;

                while (insertIndex >= 0
                    && comparer.Compare(entries[insertIndex].Order, entry.Order) > 0)
                {
                    entries[insertIndex + 1] = entries[insertIndex];
                    insertIndex--;
                }

                entries[insertIndex + 1] = entry;
            }
        }

        void ISerializedCollectionNormalizer.NormalizeSerializedState()
        {
            NormalizeOrder();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 동일 Order 그룹의 오른쪽 끝 다음 index를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        private int FindIndex(TOrder order)
        {
            var comparer = Comparer<TOrder>.Default;
            var low = 0;
            var high = entries.Count;

            while (low < high)
            {
                var middle = low + ((high - low) / 2);

                if (comparer.Compare(entries[middle].Order, order) <= 0)
                {
                    low = middle + 1;
                }
                else
                {
                    high = middle;
                }
            }

            return low;
        }

    #endregion

    #region 열거

        // ------------------------------------------------------------
        /// <summary>
        /// GetEnumerator 결과를 조회한다.
        /// </summary>
        // ------------------------------------------------------------
        public List<Entry>.Enumerator GetEnumerator() => entries.GetEnumerator();

        IEnumerator<Entry> IEnumerable<Entry>.GetEnumerator() => entries.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => entries.GetEnumerator();

    #endregion

    }
}
