/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : XOrdered.cs
수정일 : 2026-11-24

# 설명
명시적 Order를 기준으로 항상 정렬 상태를 유지하는 Unity 직렬화 가능 컬렉션을 정의한다.
Keyed 변형은 Order와 독립적인 unique Key lookup을 제공한다.

# 특이사항, 제약사항
항목 목록(entries)이 직렬화 원본이며 정렬·삽입·열거는 XOrderedBase가 맡는다.
동일 Order는 기존 항목 뒤에 삽입하여 안정적인 순서를 유지한다.
Keyed 변형은 항목이 8개를 넘으면 Key 조회용 인덱스를 만들어 쓰고, 그 이하는 목록을 직접 훑는다.
인덱스는 직렬화하지 않는 조회용 캐시이며 역직렬화·Editor 정규화·전체 삭제·복제 재구성 뒤에는 다시 만든다.
직렬화 데이터에 중복 Key가 있으면 앞쪽 항목이 조회에 쓰인다.
CloneFrom은 두 변형 모두 XOrderedBase의 같은 규칙을 따르며 Key가 있으면 같은 Key, 없으면 같은 위치의 기존 값을 짝으로 재사용한다.
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
        XOrderedBase<TOrder, TValue, XOrderedEntry<TOrder, TValue>>,
        IReadOnlyXOrdered<TOrder, TValue>,
        IDeepCloneable<XOrdered<TOrder, TValue>>
    where TOrder : struct
    where TValue : class
    {

    #region 복제

        // ------------------------------------------------------------
        /// <summary>
        /// 복제에 사용할 같은 구체 타입의 빈 컬렉션을 만든다.
        /// </summary>
        // ------------------------------------------------------------
        public virtual XOrdered<TOrder, TValue> @new() => new();

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> source의 Order와 Value를 순서 그대로 복사한다.
        /// <br/> 구성이 같으면 기존 Value에 CloneFrom하고 아니면 목록을 다시 만든다.
        /// <br/> source가 가진 Value 인스턴스는 재사용하지 않는다.
        /// </summary>
        // ----------------------------------------------------------------------
        public void CloneFrom(XOrdered<TOrder, TValue> source)
        {
            CloneFrom(source, default(NoCloneHook));
        }

    #endregion

    #region 복제 규칙

        // ------------------------------------------------------------
        /// <summary>
        /// Key가 없으므로 두 항목의 Key는 항상 같다.
        /// </summary>
        // ------------------------------------------------------------
        protected override bool IsSameKey(in XOrderedEntry<TOrder, TValue> current, in XOrderedEntry<TOrder, TValue> other) => true;

        // ------------------------------------------------------------
        /// <summary>
        /// template의 Order와 새 Value로 항목을 만든다.
        /// </summary>
        // ------------------------------------------------------------
        protected override XOrderedEntry<TOrder, TValue> CreateEntry(in XOrderedEntry<TOrder, TValue> template, TValue value)
        {
            return new XOrderedEntry<TOrder, TValue>(template.Order, value);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Key가 없으므로 같은 위치의 기존 Value를 짝으로 본다.
        /// </summary>
        // ------------------------------------------------------------
        protected override bool TryGetReusable(int sourcePosition, in XOrderedEntry<TOrder, TValue> sourceEntry, out TValue current)
        {
            if (sourcePosition < Entries.Count)
            {
                current = Entries[sourcePosition].Value;

                return true;
            }

            current = null;

            return false;
        }

    #endregion

    #region 항목 관리

        // ------------------------------------------------------------
        /// <summary>
        /// Order에 따른 정렬 위치에 Value를 추가한다.
        /// </summary>
        // ------------------------------------------------------------
        public void Add(TOrder order, TValue value)
        {
            InsertEntry(new XOrderedEntry<TOrder, TValue>(order, value));
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

            Entries.RemoveAt(index);

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
            for (var index = 0; index < Entries.Count; index++)
            {
                if (EqualityComparer<TValue>.Default.Equals(Entries[index].Value, value))
                {
                    return index;
                }
            }

            return -1;
        }

    #endregion

    }

    // ================================================================================
    /// <summary>
    /// <br/> Order 오름차순과 unique Key lookup을 함께 제공하는 직렬화 가능 컬렉션.
    /// <br/> Order, Key, Value는 하나의 항목이 소유하며 Key는 정렬 순서와 독립적이다.
    /// </summary>
    // ================================================================================
    [Serializable]
    public class XOrdered<TOrder, TKey, TValue> :
        XOrderedBase<TOrder, TValue, XOrderedEntry<TOrder, TKey, TValue>>,
        IReadOnlyXOrdered<TOrder, TKey, TValue>,
        IDeepCloneable<XOrdered<TOrder, TKey, TValue>>
    where TOrder : struct
    where TKey : IEquatable<TKey>
    where TValue : class
    {

    #region 필드

        // Key 조회용 캐시이며 직렬화하지 않는다. 목록이 바뀌었다고 알리면 비운다.
        [NonSerialized]
        private Dictionary<TKey, XOrderedEntry<TOrder, TKey, TValue>> index;

        // 인덱스를 만들 때 같은 Key가 둘 이상 있었는지 나타낸다. 그러면 삭제 뒤에 인덱스를 다시 만든다.
        [NonSerialized]
        private bool hasDuplicateKeys;

    #endregion

    #region 복제

        // ------------------------------------------------------------
        /// <summary>
        /// 복제에 사용할 같은 구체 타입의 빈 컬렉션을 만든다.
        /// </summary>
        // ------------------------------------------------------------
        public virtual XOrdered<TOrder, TKey, TValue> @new() => new();

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> source의 Order·Key·Value를 순서 그대로 복사한다.
        /// <br/> 구성이 같으면 기존 Value에 CloneFrom하고 아니면 목록을 다시 만든다.
        /// <br/> source가 가진 Value 인스턴스는 재사용하지 않는다.
        /// </summary>
        // ----------------------------------------------------------------------
        public void CloneFrom(XOrdered<TOrder, TKey, TValue> source)
        {
            CloneFrom(source, default(NoCloneHook));
        }

    #endregion

    #region 복제 규칙

        // ------------------------------------------------------------
        /// <summary>
        /// 두 항목의 Key가 같은지 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        protected override bool IsSameKey(in XOrderedEntry<TOrder, TKey, TValue> current, in XOrderedEntry<TOrder, TKey, TValue> other)
        {
            return EqualityComparer<TKey>.Default.Equals(current.Key, other.Key);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// template의 Order·Key와 새 Value로 항목을 만든다.
        /// </summary>
        // ------------------------------------------------------------
        protected override XOrderedEntry<TOrder, TKey, TValue> CreateEntry(in XOrderedEntry<TOrder, TKey, TValue> template, TValue value)
        {
            return new XOrderedEntry<TOrder, TKey, TValue>(template.Order, template.Key, value);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// source 항목과 같은 Key의 기존 Value를 짝으로 찾는다.
        /// </summary>
        // ------------------------------------------------------------
        protected override bool TryGetReusable(int sourcePosition, in XOrderedEntry<TOrder, TKey, TValue> sourceEntry, out TValue current)
        {
            return TryGetValue(sourceEntry.Key, out current);
        }

    #endregion

    #region 항목 관리

        // ------------------------------------------------------------
        /// <summary>
        /// Order에 따른 정렬 위치에 unique Key와 Value를 추가한다.
        /// </summary>
        // ------------------------------------------------------------
        public void Add(TOrder order, TKey key, TValue value)
        {
            if (ContainsKey(key))
            {
                throw new ArgumentException($"이미 존재하는 키({key})입니다.", nameof(key));
            }

            var entry = new XOrderedEntry<TOrder, TKey, TValue>(order, key, value);

            InsertEntry(entry);

            if (index != null && key is not null)
            {
                index.TryAdd(key, entry);
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Key에 해당하는 항목을 제거한다.
        /// </summary>
        // ------------------------------------------------------------
        public bool Remove(TKey key)
        {
            if (!TryGetEntry(key, out var entry))
            {
                return false;
            }

            Entries.RemoveAt(FindPos(entry));

            // 같은 Key가 더 남아 있을 수 있으면 인덱스를 다시 만든다.
            if (index != null)
            {
                if (hasDuplicateKeys)
                {
                    index = null;
                }
                else if (key is not null)
                {
                    index.Remove(key);
                }
            }

            return true;
        }

    #endregion

    #region Key 조회

        // ------------------------------------------------------------
        /// <summary>
        /// 지정한 Key의 포함 여부를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public bool ContainsKey(TKey key) => TryGetEntry(key, out _);

        // ------------------------------------------------------------
        /// <summary>
        /// Key에 해당하는 Value를 조회한다.
        /// </summary>
        // ------------------------------------------------------------
        public bool TryGetValue(TKey key, out TValue value)
        {
            if (TryGetEntry(key, out var entry))
            {
                value = entry.Value;

                return true;
            }

            value = default;

            return false;
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// Key에 해당하는 항목을 조회한다. 같은 Key가 둘 이상이면 앞쪽 항목이다.
        /// </summary>
        // ----------------------------------------------------------------------
        public bool TryGetEntry(TKey key, out XOrderedEntry<TOrder, TKey, TValue> entry)
        {
            // null Key는 인덱스에 담을 수 없으므로 항상 목록을 직접 훑는다.
            if (Entries.Count > LINEAR_SCAN_LIMIT && key is not null)
            {
                EnsureIndex();

                return index.TryGetValue(key, out entry);
            }

            var comparer = EqualityComparer<TKey>.Default;

            for (var position = 0; position < Entries.Count; position++)
            {
                if (!comparer.Equals(Entries[position].Key, key)) continue;

                entry = Entries[position];

                return true;
            }

            entry = default;

            return false;
        }

    #endregion

    #region 내부 처리

        // ------------------------------------------------------------
        /// <summary>
        /// 항목 목록이 바뀌었으므로 Key 인덱스를 비운다.
        /// </summary>
        // ------------------------------------------------------------
        protected override void OnEntriesChanged()
        {
            index = null;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 아직 없으면 항목 목록에서 Key 인덱스를 만든다.
        /// </summary>
        // ------------------------------------------------------------
        private void EnsureIndex()
        {
            if (index != null)
            {
                return;
            }

            index = new Dictionary<TKey, XOrderedEntry<TOrder, TKey, TValue>>(Entries.Count);
            hasDuplicateKeys = false;

            for (var position = 0; position < Entries.Count; position++)
            {
                var entry = Entries[position];

                if (entry.Key is null) continue;

                // 같은 Key는 앞쪽 항목만 남긴다.
                if (!index.TryAdd(entry.Key, entry))
                {
                    hasDuplicateKeys = true;
                }
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> 항목이 목록에서 놓인 위치를 반환한다.
        /// <br/> 같은 Order 구간 안에서 가장 앞쪽의 같은 Key를 찾는다.
        /// </summary>
        // ------------------------------------------------------------
        private int FindPos(XOrderedEntry<TOrder, TKey, TValue> entry)
        {
            var comparer = EqualityComparer<TKey>.Default;
            var found = -1;

            // 구간의 오른쪽 끝에서 왼쪽으로 훑으며 일치할 때마다 갱신해 가장 왼쪽 일치를 남긴다.
            for (var position = FindIndex(entry.Order) - 1; position >= 0; position--)
            {
                if (CompareOrder(Entries[position].Order, entry.Order) != 0)
                {
                    break;
                }

                if (comparer.Equals(Entries[position].Key, entry.Key))
                {
                    found = position;
                }
            }

            if (found < 0)
            {
                throw new InvalidOperationException("항목이 목록에 없습니다.");
            }

            return found;
        }

    #endregion

    }
}
