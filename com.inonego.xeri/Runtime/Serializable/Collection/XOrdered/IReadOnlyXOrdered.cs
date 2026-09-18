/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : IReadOnlyXOrdered.cs
수정일 : 2026-09-18

# 설명
XOrdered의 읽기 전용 컬렉션 계약을 정의한다.
Keyed 변형은 Key 기반 조회 계약을 추가로 제공한다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

namespace inonego.Xeri.Serializable
{
    // ============================================================
    /// <summary>
    /// Order 정렬 Entry 목록의 읽기 전용 계약.
    /// </summary>
    // ============================================================
    public interface IReadOnlyXOrdered<TOrder, TValue> :
        IReadOnlyList<XOrdered<TOrder, TValue>.Entry>
    where TOrder : struct
    where TValue : class
    {
        // ------------------------------------------------------------
        /// <summary>
        /// Value와 같은 항목이 존재하는지 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        bool Contains(TValue value);

        // ------------------------------------------------------------
        /// <summary>
        /// Value와 같은 첫 항목의 index를 반환한다. 없으면 -1.
        /// </summary>
        // ------------------------------------------------------------
        int IndexOf(TValue value);
    }

    // ============================================================
    /// <summary>
    /// Key 조회를 제공하는 Order 정렬 Entry 목록의 읽기 전용 계약.
    /// </summary>
    // ============================================================
    public interface IReadOnlyXOrdered<TOrder, TKey, TValue> :
        IReadOnlyList<XOrdered<TOrder, TKey, TValue>.Entry>
    where TOrder : struct
    where TKey : IEquatable<TKey>
    where TValue : class
    {
        // ------------------------------------------------------------
        /// <summary>
        /// Key에 해당하는 항목이 존재하는지 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        bool ContainsKey(TKey key);

        // ------------------------------------------------------------
        /// <summary>
        /// Key에 해당하는 Value를 조회한다.
        /// </summary>
        // ------------------------------------------------------------
        bool TryGetValue(TKey key, out TValue value);

        // ------------------------------------------------------------
        /// <summary>
        /// Key에 해당하는 Entry를 조회한다.
        /// </summary>
        // ------------------------------------------------------------
        bool TryGetEntry
        (
            TKey key,
            out XOrdered<TOrder, TKey, TValue>.Entry entry
        );
    }
}
