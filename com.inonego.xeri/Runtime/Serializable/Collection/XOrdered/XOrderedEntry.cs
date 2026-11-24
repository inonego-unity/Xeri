/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : XOrderedEntry.cs
수정일 : 2026-11-24

# 설명
XOrdered가 Order 순서로 보관하는 항목을 정의한다.
Order와 Value를 가지는 변형과 Order, Key, Value를 가지는 변형이 있고 둘 다 값 형식이다.

# 특이사항, 제약사항
항목은 값 형식이므로 목록에 담을 때 힙 할당이 없고, 조회 결과는 복사본이다.
필드는 Unity 직렬화를 위해 readonly로 선언하지 않으며 컬렉션만 internal 생성자로 만든다.
Value는 SerializeReference로 직렬화하므로 클래스 형식만 쓴다.
========================================================================= BLOCK_HEADER_END */

using System;

using UnityEngine;

namespace inonego.Xeri.Serializable
{
    // ============================================================
    /// <summary>
    /// Order 기준 정렬과 Value 복제에 필요한 항목 계약.
    /// </summary>
    // ============================================================
    public interface IXOrderedEntry<TOrder, TValue>
    where TOrder : struct
    where TValue : class
    {
        // ------------------------------------------------------------
        /// <summary>
        /// 정렬 기준 Order를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        TOrder Order { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 항목의 Value를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        TValue Value { get; }
    }

    // ============================================================
    /// <summary>
    /// Order와 Value를 함께 가지는 항목.
    /// </summary>
    // ============================================================
    [Serializable]
    public struct XOrderedEntry<TOrder, TValue> : IXOrderedEntry<TOrder, TValue>
    where TOrder : struct
    where TValue : class
    {

    #region 필드

        // ------------------------------------------------------------
        /// <summary>
        /// 정렬 기준 Order를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public TOrder Order => order;

        [SerializeField]
        private TOrder order;

        // ------------------------------------------------------------
        /// <summary>
        /// 항목의 Value를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public TValue Value => value;

        [SerializeReference]
        private TValue value;

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// Order와 Value로 항목을 만든다.
        /// </summary>
        // ------------------------------------------------------------
        internal XOrderedEntry(TOrder order, TValue value)
        {
            this.order = order;
            this.value = value;
        }

    #endregion

    #region 메서드

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

    #endregion

    }

    // ============================================================
    /// <summary>
    /// Order, Key, Value를 함께 가지는 항목.
    /// </summary>
    // ============================================================
    [Serializable]
    public struct XOrderedEntry<TOrder, TKey, TValue> : IXOrderedEntry<TOrder, TValue>
    where TOrder : struct
    where TKey : IEquatable<TKey>
    where TValue : class
    {

    #region 필드

        // ------------------------------------------------------------
        /// <summary>
        /// 정렬 기준 Order를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public TOrder Order => order;

        [SerializeField]
        private TOrder order;

        // ------------------------------------------------------------
        /// <summary>
        /// Order와 독립적인 고유 Key를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public TKey Key => key;

        [SerializeField]
        private TKey key;

        // ------------------------------------------------------------
        /// <summary>
        /// 항목의 Value를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public TValue Value => value;

        [SerializeReference]
        private TValue value;

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// Order, Key, Value로 항목을 만든다.
        /// </summary>
        // ------------------------------------------------------------
        internal XOrderedEntry(TOrder order, TKey key, TValue value)
        {
            this.order = order;
            this.key = key;
            this.value = value;
        }

    #endregion

    #region 메서드

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

    #endregion

    }
}
