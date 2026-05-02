/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : XOrderedBase.cs
수정일 : 2026-05-02

# 설명
항상 Order 오름차순으로 정렬된 직렬화 가능 컬렉션의 추상 기본 클래스.
Pair(Order, Value) 정렬 리스트 + IReadOnlyList<Pair> + Find / Contains / 깊은 복제 수신을 제공한다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace inonego.Xeri.Serializable
{
    // ========================================================================================
    /// <summary>
    /// <br/> 항상 Order 오름차순으로 정렬된 직렬화 가능 컬렉션의 기본 클래스.
    /// <br/> _Add / _Remove / _Clear는 protected이며, 파생 클래스가 public API로 노출한다.
    /// </summary>
    // ========================================================================================
    [Serializable]
    public abstract class XOrderedBase<TOrder, TValue> :
    IReadOnlyList<XOrderedBase<TOrder, TValue>.Pair>,
    IDeepCloneableFrom<XOrderedBase<TOrder, TValue>>
    where TOrder : struct, IComparable<TOrder>
    where TValue : class
    {

    #region 내부 데이터

        // ==========================================================================
        /// <summary>
        /// <br/> Order와 Value를 묶는 공개 쌍 구조체.
        /// <br/> Order 기준 standard(symmetric) 비교 + Deconstruct 분해 지원.
        /// </summary>
        // ==========================================================================
        [Serializable]
        public struct Pair : IComparable<Pair>
        {
            [SerializeField]     public TOrder Order;
            [SerializeReference] public TValue Value;

            public Pair(TOrder order, TValue value)
            {
                Order = order;
                Value = value;
            }

            // ------------------------------------------------------------
            /// <summary>
            /// 튜플 분해 — `var (order, value) = pair;`.
            /// </summary>
            // ------------------------------------------------------------
            public readonly void Deconstruct(out TOrder order, out TValue value)
            {
                order = Order;
                value = Value;
            }

            // ------------------------------------------------------------
            /// <summary>
            /// Order 오름차순 비교(symmetric).
            /// </summary>
            // ------------------------------------------------------------
            public readonly int CompareTo(Pair other) => Order.CompareTo(other.Order);
        }

    #endregion

    #region 비교자

        // ------------------------------------------------------------------------------------------------
        /// <summary>
        /// <br/> Upper Bound 검색용 비교 함수.
        /// <br/> 같은 Order는 "less"로 취급해 BinarySearch 결과가 항상 음수(~insertionPoint)가 되게 한다.
        /// <br/> 결과적으로 동일 Order 그룹의 오른쪽 끝 다음 인덱스를 얻는다.
        /// </summary>
        // ------------------------------------------------------------------------------------------------
        private static int CompareForUpperBound(Pair a, Pair b)
        {
            var cmp = a.Order.CompareTo(b.Order);

            return cmp != 0 ? cmp : -1;
        }

        private static readonly IComparer<Pair> upperBoundComparer = Comparer<Pair>.Create(CompareForUpperBound);

    #endregion

    #region 필드

        [SerializeField]
        protected List<Pair> list = new();

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 요소 수.
        /// </summary>
        // ------------------------------------------------------------
        public int Count => list.Count;

    #endregion

    #region 생성자

        protected XOrderedBase() {}

    #endregion

    #region 인덱스 접근 / 순회 (IReadOnlyList<Pair>)

        // ------------------------------------------------------------
        /// <summary>
        /// 정렬 순서의 i번째 Pair를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public Pair this[int index] => list[index];

        // ------------------------------------------------------------
        /// <summary>
        /// 정렬 순서로 Pair를 순회한다. foreach + LINQ에서 직접 사용 가능.
        /// </summary>
        // ------------------------------------------------------------
        public IEnumerator<Pair> GetEnumerator() => list.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    #endregion

    #region 검색

        // ---------------------------------------------------------------------------------
        /// <summary>
        /// <br/> Binary Search로 order의 Upper Bound 인덱스를 반환한다.
        /// <br/> 동일 Order가 없으면 order보다 큰 첫 요소의 인덱스(없으면 Count)를 반환한다.
        /// <br/> 동일 Order가 있으면 그 그룹의 오른쪽 끝 다음 인덱스를 반환한다.
        /// <br/> 결과는 항상 "order로 삽입해야 할 위치"이며 Insert(result, ...)에 그대로 사용 가능.
        /// </summary>
        // ---------------------------------------------------------------------------------
        public int Find(TOrder order)
        {
            return ~list.BinarySearch(new Pair(order, default), upperBoundComparer);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Value와 일치하는 인덱스를 선형 탐색한다. 없으면 -1.
        /// </summary>
        // ------------------------------------------------------------
        public int Find(TValue value)
        {
            for (int i = 0; i < list.Count; i++)
            {
                var other = list[i].Value;

                if (value != null && other != null)
                {
                    if (value.Equals(other)) return i;
                }

                if (value == null && other == null) return i;
            }

            return -1;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Value가 컬렉션 안에 있는지 확인한다.
        /// </summary>
        // ------------------------------------------------------------
        public bool Contains(TValue value) => Find(value) != -1;

    #endregion

    #region 변경 (protected)

        // ------------------------------------------------------------
        /// <summary>
        /// Order 오름차순 정렬 위치에 Value를 삽입한다.
        /// </summary>
        // ------------------------------------------------------------
        protected void _Add(TOrder order, TValue value)
        {
            int index = Find(order);

            list.Insert(index, new Pair(order, value));
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 일치하는 첫 Value를 제거한다. 반환값은 제거 성공 여부.
        /// </summary>
        // ------------------------------------------------------------
        protected bool _Remove(TValue value)
        {
            int index = Find(value);

            if (index == -1) return false;

            list.RemoveAt(index);

            return true;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 모든 요소를 제거한다.
        /// </summary>
        // ------------------------------------------------------------
        protected void _Clear() => list.Clear();

    #endregion

    #region 복제

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> source의 정렬 리스트를 깊은 복제하여 this에 채운다.
        /// <br/> Value가 IDeepCloneable&lt;TValue&gt;면 Clone()을 사용한다.
        /// </summary>
        // ------------------------------------------------------------
        public virtual void CloneFrom(XOrderedBase<TOrder, TValue> source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            list.Clear();

            foreach (var pair in source.list)
            {
                var value  = pair.Value;
                var cloned = value is IDeepCloneable<TValue> cloneable ? cloneable.Clone() : value;

                list.Add(new Pair(pair.Order, cloned));
            }
        }

    #endregion

    }
}
