/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : XPriorityQueue.cs
수정일 : 2026-09-29

# 설명
Unity 직렬화를 지원하는 최대 힙 기반 우선순위 큐.
높은 TPriority 값이 먼저 Dequeue된다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace inonego.Xeri.Serializable
{
    // ============================================================
    /// <summary>
    /// <br/> 직렬화 가능한 우선순위 큐.
    /// <br/> 최대 힙 기반으로 높은 우선순위의 요소가 먼저 나온다.
    /// </summary>
    // ============================================================
    [Serializable]
    public class XPriorityQueue<TElement, TPriority>
        : ISerializedCollectionNormalizer
    where TElement : class
    where TPriority : struct, IComparable<TPriority>
    {

    #region 내부 데이터

        // ============================================================
        /// <summary>
        /// 요소와 우선순위를 묶는 내부 쌍 구조체.
        /// </summary>
        // ============================================================
        [Serializable]
        protected struct Pair : IComparable<Pair>
        {
            [SerializeReference] public TElement  Element;
            [SerializeField]     public TPriority Priority;

            // ------------------------------------------------------------
            /// <summary>
            /// Pair 인스턴스를 초기화한다.
            /// </summary>
            // ------------------------------------------------------------
            public Pair(TElement element, TPriority priority)
            {
                Element  = element;
                Priority = priority;
            }

            // ------------------------------------------------------------
            /// <summary>
            /// Priority 기준 내림차순 비교. 높은 우선순위가 앞에 온다.
            /// </summary>
            // ------------------------------------------------------------
            public int CompareTo(Pair other) => other.Priority.CompareTo(Priority);
        }

    #endregion

    #region 필드

        // ------------------------------------------------------------
        /// <summary>
        /// Heap 값을 반환하거나 설정한다.
        /// </summary>
        // ------------------------------------------------------------
        protected virtual List<Pair> Heap => heap;

        [SerializeField]
        private List<Pair> heap = new();

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 요소 수.
        /// </summary>
        // ------------------------------------------------------------
        public int Count => heap.Count;

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// XPriorityQueue 인스턴스를 초기화한다.
        /// </summary>
        // ------------------------------------------------------------
        public XPriorityQueue()
        {
            // NONE
        }

    #endregion

    #region 메서드

        // ------------------------------------------------------------
        /// <summary>
        /// 요소를 우선순위와 함께 추가한다.
        /// </summary>
        // ------------------------------------------------------------
        public void Enqueue(TElement element, TPriority priority)
        {
            heap.Add(new(element, priority));

            HeapifyUp(heap.Count - 1);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 가장 높은 우선순위의 요소를 꺼내 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public (TElement Element, TPriority Priority) Dequeue()
        {
            if (heap.Count == 0)
            {
                throw new InvalidOperationException("우선순위 큐가 비어있습니다.");
            }

            var pair = heap[0];

            // 마지막 요소를 루트로 올린 뒤 아래로 내린다.
            heap[0] = heap[heap.Count - 1];
            heap.RemoveAt(heap.Count - 1);

            if (heap.Count > 0)
            {
                HeapifyDown(0);
            }

            return (pair.Element, pair.Priority);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 가장 높은 우선순위의 요소를 제거 없이 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public (TElement Element, TPriority Priority) Peek()
        {
            if (heap.Count == 0)
            {
                throw new InvalidOperationException("우선순위 큐가 비어있습니다.");
            }

            var pair = heap[0];

            return (pair.Element, pair.Priority);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 모든 요소를 제거한다.
        /// </summary>
        // ------------------------------------------------------------
        public void Clear()
        {
            heap.Clear();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 요소가 큐 안에 있는지 선형 탐색으로 확인한다.
        /// </summary>
        // ------------------------------------------------------------
        public bool Contains(TElement element)
        {
            var comparer = EqualityComparer<TElement>.Default;

            for (var index = 0; index < heap.Count; index++)
            {
                if (comparer.Equals(element, heap[index].Element))
                {
                    return true;
                }
            }

            return false;
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// 현재 serialized heap 전체에서 최대 힙 invariant를 다시 구성한다.
        /// </summary>
        // ----------------------------------------------------------------------
        internal void RebuildHeap()
        {
            for (var index = (heap.Count / 2) - 1; index >= 0; index--)
            {
                HeapifyDown(index);
            }
        }

        void ISerializedCollectionNormalizer.NormalizeSerializedState()
        {
            RebuildHeap();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 상향식 힙 정렬. 삽입 후 루트 방향으로 버블업한다.
        /// </summary>
        // ------------------------------------------------------------
        private void HeapifyUp(int index)
        {
            while (index > 0)
            {
                int parent = (index - 1) / 2;

                if (heap[index].CompareTo(heap[parent]) >= 0) break;

                (heap[index], heap[parent]) = (heap[parent], heap[index]);

                index = parent;
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 하향식 힙 정렬. 루트 교체 후 리프 방향으로 버블다운한다.
        /// </summary>
        // ------------------------------------------------------------
        private void HeapifyDown(int index)
        {
            while (true)
            {
                int lChild  = 2 * index + 1;
                int rChild  = 2 * index + 2;
                int highest = index;

                if (lChild < heap.Count && heap[lChild].CompareTo(heap[highest]) < 0)
                {
                    highest = lChild;
                }

                if (rChild < heap.Count && heap[rChild].CompareTo(heap[highest]) < 0)
                {
                    highest = rChild;
                }

                if (highest == index) break;

                (heap[index], heap[highest]) = (heap[highest], heap[index]);

                index = highest;
            }
        }

    #endregion

    }
}
