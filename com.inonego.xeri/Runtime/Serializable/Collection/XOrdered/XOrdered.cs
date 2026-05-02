/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : XOrdered.cs
수정일 : 2026-05-02

# 설명
항상 Order 오름차순으로 정렬된 직렬화 가능 컬렉션의 두 변형.
- XOrdered<TOrder, TValue>          : 단순 변형 (Order + Value)
- XOrdered<TOrder, TKey, TValue>    : Key 기반 lookup을 추가한 변형 (보조 XDictionary_VR 사용)
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace inonego.Xeri.Serializable
{
    // ============================================================
    /// <summary>
    /// 항상 Order 오름차순으로 정렬된 직렬화 가능 컬렉션.
    /// </summary>
    // ============================================================
    [Serializable]
    public class XOrdered<TOrder, TValue> :
    XOrderedBase<TOrder, TValue>, 
    IDeepCloneable<XOrdered<TOrder, TValue>>
    where TOrder : struct, IComparable<TOrder>
    where TValue : class
    {

    #region 생성자

        public XOrdered() : base() {}

    #endregion

    #region 메서드

        // ------------------------------------------------------------
        /// <summary>
        /// Order에 따라 정렬된 위치에 Value를 삽입한다.
        /// </summary>
        // ------------------------------------------------------------
        public void Add(TOrder order, TValue value) => _Add(order, value);

        // ------------------------------------------------------------
        /// <summary>
        /// 일치하는 첫 Value를 제거한다.
        /// </summary>
        // ------------------------------------------------------------
        public bool Remove(TValue value) => _Remove(value);

        // ------------------------------------------------------------
        /// <summary>
        /// 모든 요소를 제거한다.
        /// </summary>
        // ------------------------------------------------------------
        public void Clear() => _Clear();

    #endregion

    #region 복제

        // ------------------------------------------------------------
        /// <summary>
        /// 복제용 빈 인스턴스를 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        public XOrdered<TOrder, TValue> @new() => new();

        // ------------------------------------------------------------
        /// <summary>
        /// source의 정렬 리스트를 깊은 복제하여 this에 채운다.
        /// </summary>
        // ------------------------------------------------------------
        public void CloneFrom(XOrdered<TOrder, TValue> source) => base.CloneFrom(source);

    #endregion

    }

    // ==========================================================================
    /// <summary>
    /// <br/> Order, Key, Value를 함께 가지며 Order 오름차순으로 정렬된 직렬화 가능 컬렉션.
    /// <br/> 보조 XDictionary_VR로 O(1) Key lookup을 지원한다.
    /// <br/> IReadOnlyDictionary&lt;TKey, TValue&gt;를 명시적 구현으로 노출한다.
    /// </summary>
    // ==========================================================================
    [Serializable]
    public class XOrdered<TOrder, TKey, TValue> :
    XOrderedBase<TOrder, TValue>, 
    IReadOnlyDictionary<TKey, TValue>, 
    IDeepCloneable<XOrdered<TOrder, TKey, TValue>>
    where TOrder : struct, IComparable<TOrder>
    where TKey   : IEquatable<TKey>
    where TValue : class
    {

    #region 내부 데이터

        // ================================================================
        /// <summary>
        /// <br/> Order, Key, Value를 묶는 공개 트리플 구조체.
        /// <br/> Deconstruct 분해 — `var (order, key, value) = pair;`.
        /// </summary>
        // ================================================================
        public readonly struct KeyedPair
        {
            public readonly TOrder Order;
            public readonly TKey   Key;
            public readonly TValue Value;

            public KeyedPair(TOrder order, TKey key, TValue value)
            {
                Order = order;
                Key   = key;
                Value = value;
            }

            // ------------------------------------------------------------
            /// <summary>
            /// 튜플 분해를 지원한다.
            /// </summary>
            // ------------------------------------------------------------
            public void Deconstruct(out TOrder order, out TKey key, out TValue value)
            {
                order = Order;
                key   = Key;
                value = Value;
            }
        }

    #endregion

    #region 필드

        [SerializeField]
        private XDictionary_VR<TKey, TValue> dictionary = new();

    #endregion

    #region 생성자

        public XOrdered() : base() {}

    #endregion

    #region Key 기반 조회 (직접 API — 키 없을 때 null)

        // -------------------------------------------------------------------------------
        /// <summary>
        /// Key로 Value를 조회한다. 없으면 null. (BCL IReadOnlyDictionary와 다른 lenient 동작)
        /// </summary>
        // -------------------------------------------------------------------------------
        public TValue this[TKey key] => dictionary.TryGetValue(key, out var value) ? value : null;

        // ------------------------------------------------------------
        /// <summary>
        /// Key로 Value를 안전하게 조회한다.
        /// </summary>
        // ------------------------------------------------------------
        public bool TryGetValue(TKey key, out TValue value) => dictionary.TryGetValue(key, out value);

        // ------------------------------------------------------------
        /// <summary>
        /// Key가 존재하는지 확인한다.
        /// </summary>
        // ------------------------------------------------------------
        public bool ContainsKey(TKey key) => dictionary.ContainsKey(key);

        // -----------------------------------------------------------------
        /// <summary>
        /// <br/> 모든 Key를 dictionary 내부 순서로 반환한다 (정렬 순서 아님).
        /// <br/> 정렬 순서가 필요하면 AsKeyed() 사용.
        /// </summary>
        // -----------------------------------------------------------------
        public IEnumerable<TKey> Keys => dictionary.Keys;

        // -----------------------------------------------------------------
        /// <summary>
        /// <br/> 모든 Value를 dictionary 내부 순서로 반환한다 (정렬 순서 아님).
        /// <br/> 정렬 순서가 필요하면 foreach(Pair) 또는 AsKeyed() 사용.
        /// </summary>
        // -----------------------------------------------------------------
        public IEnumerable<TValue> Values => dictionary.Values;

    #endregion

    #region Keyed 순회

        // ----------------------------------------------------------------------------
        /// <summary>
        /// <br/> 정렬 순서로 KeyedPair(Order, Key, Value)를 순회한다.
        /// <br/> Value → Key 역방향 맵을 매 호출마다 빌드한다 (O(N) per call).
        /// </summary>
        // ----------------------------------------------------------------------------
        public IEnumerable<KeyedPair> AsKeyed()
        {
            var reverse = new Dictionary<TValue, TKey>(dictionary.Count);

            foreach (var (key, value) in dictionary)
            {
                if (value != null)
                {
                    reverse[value] = key;
                }
            }

            foreach (var pair in list)
            {
                if (pair.Value != null && reverse.TryGetValue(pair.Value, out var key))
                {
                    yield return new KeyedPair(pair.Order, key, pair.Value);
                }
            }
        }

    #endregion

    #region IReadOnlyDictionary<TKey, TValue> 명시적 구현

        // --------------------------------------------------------------------------------------------
        /// <summary>
        /// <br/> BCL IReadOnlyDictionary 계약 준수 인덱서 — 키 없으면 KeyNotFoundException throw.
        /// <br/> 직접 API의 this[TKey](null 반환)와 의도적으로 다른 동작.
        /// </summary>
        // --------------------------------------------------------------------------------------------
        TValue IReadOnlyDictionary<TKey, TValue>.this[TKey key]
        {
            get
            {
                if (!dictionary.TryGetValue(key, out var value))
                {
                    throw new KeyNotFoundException($"키({key})를 찾을 수 없습니다.");
                }

                return value;
            }
        }

        // ----------------------------------------------------------------------------
        /// <summary>
        /// <br/> KeyValuePair<TKey, TValue>를 dictionary 내부 순서로 순회한다.
        /// <br/> 정렬 순서가 필요하면 AsKeyed() 또는 직접 foreach(Pair) 사용.
        /// </summary>
        // ----------------------------------------------------------------------------
        IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator()
        {
            return ((IEnumerable<KeyValuePair<TKey, TValue>>)dictionary).GetEnumerator();
        }

    #endregion

    #region 메서드

        // -----------------------------------------------------------------
        /// <summary>
        /// Order, Key, Value를 함께 추가한다. Order 오름차순 정렬 위치에 삽입.
        /// </summary>
        // -----------------------------------------------------------------
        public void Add(TOrder order, TKey key, TValue value)
        {
            if (dictionary.ContainsKey(key))
            {
                throw new ArgumentException($"이미 존재하는 키({key})입니다.");
            }

            _Add(order, value);

            dictionary.Add(key, value);
        }

        // -------------------------------------------------------------------------------------
        /// <summary>
        /// <br/> Key로 Value를 찾아 제거한다.
        /// <br/> list에서는 reference 동등성으로 검색하여 TValue.Equals 오버라이드의 영향을 받지 않는다.
        /// </summary>
        // -------------------------------------------------------------------------------------
        public bool Remove(TKey key)
        {
            if (!dictionary.TryGetValue(key, out var value))
            {
                return false;
            }

            for (int i = 0; i < list.Count; i++)
            {
                if (ReferenceEquals(list[i].Value, value))
                {
                    list.RemoveAt(i);
                    break;
                }
            }

            dictionary.Remove(key);

            return true;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 모든 요소를 제거한다.
        /// </summary>
        // ------------------------------------------------------------
        public void Clear()
        {
            _Clear();

            dictionary.Clear();
        }

    #endregion

    #region 복제

        // ------------------------------------------------------------
        /// <summary>
        /// 복제용 빈 인스턴스를 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        public XOrdered<TOrder, TKey, TValue> @new() => new();

        // ----------------------------------------------------------------------------------------------
        /// <summary>
        /// <br/> source의 list와 dictionary를 깊은 복제하여 this에 채운다.
        /// <br/> reference 단위 cache를 사용해 동일 Value 참조가 여러 번 등장해도 단 한 번만 Clone()되며,
        /// <br/> 결과적으로 list와 dictionary가 동일 복제 인스턴스(cross-reference identity)를 가리킨다.
        /// </summary>
        // ----------------------------------------------------------------------------------------------
        public void CloneFrom(XOrdered<TOrder, TKey, TValue> source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            list.Clear();
            dictionary.Clear();

            var cloneCache = new Dictionary<TValue, TValue>(ReferenceValueComparer.Instance);

            foreach (var pair in source.list)
            {
                list.Add(new Pair(pair.Order, CloneOnce(pair.Value, cloneCache)));
            }

            foreach (var (key, original) in source.dictionary)
            {
                dictionary.Add(key, CloneOnce(original, cloneCache));
            }
        }

        // --------------------------------------------------------------------------------------------
        /// <summary>
        /// reference 단위 cache로 단 한 번만 Clone하고, 동일 reference 재방문 시 캐시된 복제를 반환한다.
        /// </summary>
        // --------------------------------------------------------------------------------------------
        private static TValue CloneOnce(TValue value, Dictionary<TValue, TValue> cache)
        {
            if (value == null) return null;

            if (cache.TryGetValue(value, out var cached))
            {
                return cached;
            }

            var cloned = value is IDeepCloneable<TValue> cloneable ? cloneable.Clone() : value;

            cache[value] = cloned;

            return cloned;
        }

        // ================================================================================
        /// <summary>
        /// <br/> reference 동등성 기반 IEqualityComparer&lt;TValue&gt;.
        /// <br/> CloneFrom의 cloneCache가 TValue.Equals 오버라이드의 영향을 받지 않도록 보장.
        /// </summary>
        // ================================================================================
        private sealed class ReferenceValueComparer : IEqualityComparer<TValue>
        {
            public static readonly ReferenceValueComparer Instance = new();

            public bool Equals(TValue x, TValue y) => ReferenceEquals(x, y);

            public int GetHashCode(TValue obj) => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
        }

    #endregion

    }
}
