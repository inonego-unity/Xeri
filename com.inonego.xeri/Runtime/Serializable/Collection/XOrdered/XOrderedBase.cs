/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : XOrderedBase.cs
수정일 : 2026-11-24

# 설명
Order 오름차순을 유지하는 직렬화 가능 목록의 공통 구현을 정의한다.
항목 목록, 정렬 삽입, 정렬 복구, 열거, CloneFrom을 맡고 Key와 값 조회는 파생 컬렉션이 정한다.

# 특이사항, 제약사항
항목 목록(entries)이 직렬화 원본이다. 파생 컬렉션의 조회용 캐시는 목록이 바뀌었다는 알림(OnEntriesChanged)으로 무효화한다.
동일 Order는 기존 항목 뒤에 삽입하여 안정적인 순서를 유지한다.
Unity 역직렬화와 Editor 정규화 뒤에는 항목 목록이 외부에서 바뀌었으므로 OnEntriesChanged를 호출한다.
CloneFrom은 외부 알림 없이 Order·Key·Value를 source 순서 그대로 복사한다.
값 복제는 CollectionClone 규칙을 따르며, 복제를 지원하지 않는 참조 값은 공유하지 않고 예외를 던진다.
Order·Key·구체 타입이 순서대로 같으면 항목 목록을 두고 각 값에 CloneFrom한다(할당 없음).
구성이 다르면 파생 컬렉션이 정한 짝(Key 있으면 같은 Key, 없으면 같은 위치)의 같은 타입 기존 값은 재사용하고 나머지는 Clone한다.
source가 가진 인스턴스는 재사용하지 않는다.
새 값 복제(준비)가 실패하면 이 컬렉션을 바꾸지 않는다.
재사용 값의 CloneFrom은 되돌릴 수 없으므로 실패해도 나머지를 끝까지 복사하고 항목 목록을 source에 맞춘 뒤 실패를 모아 던진다.
실패는 하나면 원래 예외를, 둘 이상이면 AggregateException을 던지며 실패한 값은 이전 상태로 남는다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Pool;

namespace inonego.Xeri.Serializable
{
    // ============================================================
    /// <summary>
    /// <br/> Order 오름차순을 유지하는 직렬화 가능 목록의 공통 기반.
    /// <br/> 동일 Order에서는 기존 항목 뒤에 새 항목을 삽입한다.
    /// </summary>
    // ============================================================
    [Serializable]
    public abstract class XOrderedBase<TOrder, TValue, TEntry> :
        IReadOnlyList<TEntry>,
        ISerializedCollectionNormalizer,
        ISerializationCallbackReceiver
    where TOrder : struct
    where TValue : class
    where TEntry : struct, IXOrderedEntry<TOrder, TValue>
    {

    #region 필드

        // 항목이 이 수 이하이면 인스턴스 비교와 Key 조회를 집합이나 인덱스 없이 목록을 직접 훑는다.
        protected const int LINEAR_SCAN_LIMIT = 8;

        // 복제 실패 메시지에 쓰는 컬렉션 이름이다.
        private const string CLONE_OWNER = "XOrdered";

        // 재사용 값의 CloneFrom이 둘 이상 실패했을 때의 예외 메시지이다.
        private const string CLONE_FAILURE = "XOrdered 복제 중 여러 값이 실패했습니다.";

        // Order 비교에 쓴다. IComparable<TOrder>를 구현한 형식은 박싱 없이 비교한다.
        private static readonly Comparer<TOrder> orderComparer = Comparer<TOrder>.Default;

        // ------------------------------------------------------------
        /// <summary>
        /// 파생 컬렉션이 항목 목록을 다루도록 노출한다.
        /// </summary>
        // ------------------------------------------------------------
        protected List<TEntry> Entries => entries;

        [SerializeField]
        private List<TEntry> entries = new();

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
        public TEntry this[int index] => entries[index];

    #endregion

    #region 항목 관리

        // ------------------------------------------------------------
        /// <summary>
        /// 모든 항목을 제거한다.
        /// </summary>
        // ------------------------------------------------------------
        public void Clear()
        {
            entries.Clear();

            OnEntriesChanged();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 항목을 Order에 따른 정렬 위치에 삽입한다.
        /// </summary>
        // ------------------------------------------------------------
        protected void InsertEntry(TEntry entry)
        {
            entries.Insert(FindIndex(entry.Order), entry);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 항목 목록이 Add·Remove 밖에서 바뀌었음을 파생 컬렉션에 알린다.
        /// </summary>
        // ------------------------------------------------------------
        protected virtual void OnEntriesChanged()
        {
            // NONE
        }

    #endregion

    #region 복제

        // ------------------------------------------------------------
        /// <summary>
        /// 두 항목의 Key가 같은지 반환한다. Key가 없는 항목은 항상 같다.
        /// </summary>
        // ------------------------------------------------------------
        protected abstract bool IsSameKey(in TEntry current, in TEntry other);

        // ------------------------------------------------------------
        /// <summary>
        /// template의 Order와 Key를 가지고 Value만 다른 새 항목을 만든다.
        /// </summary>
        // ------------------------------------------------------------
        protected abstract TEntry CreateEntry(in TEntry template, TValue value);

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> source 항목과 짝이 되는 현재 Value를 찾는다.
        /// <br/> Key가 있으면 같은 Key의 Value, 없으면 같은 위치의 Value가 짝이다.
        /// </summary>
        // ----------------------------------------------------------------------
        protected abstract bool TryGetReusable(int sourcePosition, in TEntry sourceEntry, out TValue current);

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> source를 복사하되 새 Value 준비가 끝난 뒤 기존 Value를
        /// <br/> 바꾸기 전에 훅을 한 번 호출한다.
        /// <br/> 새 Value 복제가 실패하면 훅을 호출하지 않고
        /// <br/> 이 컬렉션을 바꾸지 않는다.
        /// </summary>
        // ------------------------------------------------------------
        internal void CloneFrom<THook>(XOrderedBase<TOrder, TValue, TEntry> source, THook hook)
        where THook : struct, ICloneReadyHook
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (ReferenceEquals(source, this))
            {
                return;
            }

            // 구성이 같으면 목록을 그대로 두고 각 Value에 상태만 복사해 할당을 만들지 않는다.
            if (CanCloneInPlace(source))
            {
                hook.OnCloneReady();

                // 덮어쓰기는 되돌릴 수 없으므로 한 값이 실패해도 나머지를 끝까지 복사한 뒤 모아서 던진다.
                var failures = new ExceptionBuffer();

                for (var position = 0; position < entries.Count; position++)
                {
                    var value = entries[position].Value;

                    if (value != null)
                    {
                        try
                        {
                            CollectionClone<TValue>.ReuseFrom(value, source.entries[position].Value);
                        }
                        catch (Exception exception)
                        {
                            failures.Add(exception);
                        }
                    }
                }

                failures.ThrowWhenAny(CLONE_FAILURE);

                return;
            }

            RebuildEntries(source, hook);
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> 현재 목록과 source 목록의 Order·Key·구체 타입이 순서대로 같고
        /// <br/> 모든 Value를 그대로 재사용할 수 있는지 확인한다.
        /// </summary>
        // ----------------------------------------------------------------------
        private bool CanCloneInPlace(XOrderedBase<TOrder, TValue, TEntry> source)
        {
            if (entries.Count != source.entries.Count)
            {
                return false;
            }

            for (var position = 0; position < entries.Count; position++)
            {
                var current = entries[position];
                var other = source.entries[position];

                if (CompareOrder(current.Order, other.Order) != 0 || !IsSameKey(current, other))
                {
                    return false;
                }

                if (current.Value == null || other.Value == null)
                {
                    if (current.Value != other.Value) return false;

                    continue;
                }

                if (!CollectionClone<TValue>.CanReuse(current.Value, other.Value))
                {
                    return false;
                }
            }

            return !HasSharedInstances(source);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> 여러 항목이 같은 Value 인스턴스를 공유하거나
        /// <br/> source와 인스턴스를 공유하는지 확인한다.
        /// </summary>
        // ------------------------------------------------------------
        private bool HasSharedInstances(XOrderedBase<TOrder, TValue, TEntry> source)
        {
            var count = entries.Count;

            // 항목이 적으면 직접 비교하는 편이 집합을 쓰는 것보다 싸다.
            if (count <= LINEAR_SCAN_LIMIT)
            {
                for (var position = 0; position < count; position++)
                {
                    var value = entries[position].Value;

                    if (value == null) continue;

                    for (var previous = 0; previous < position; previous++)
                    {
                        if (ReferenceEquals(entries[previous].Value, value)) return true;
                    }

                    for (var sourcePosition = 0; sourcePosition < source.entries.Count; sourcePosition++)
                    {
                        if (ReferenceEquals(source.entries[sourcePosition].Value, value)) return true;
                    }
                }

                return false;
            }

            var seen = CollectionClone<TValue>.RentInstanceSet();
            var sourceInstances = CollectionClone<TValue>.RentInstanceSet();

            try
            {
                for (var sourcePosition = 0; sourcePosition < source.entries.Count; sourcePosition++)
                {
                    var value = source.entries[sourcePosition].Value;

                    if (value != null)
                    {
                        sourceInstances.Add(value);
                    }
                }

                for (var position = 0; position < count; position++)
                {
                    var value = entries[position].Value;

                    if (value == null) continue;

                    if (sourceInstances.Contains(value) || !seen.Add(value)) return true;
                }

                return false;
            }
            finally
            {
                CollectionClone<TValue>.ReturnInstanceSet(sourceInstances);
                CollectionClone<TValue>.ReturnInstanceSet(seen);
            }
        }

        // --------------------------------------------------------------------------------
        /// <summary>
        /// <br/> 재사용할 기존 Value를 고르고 나머지는 새로 복제해 목록을 다시 만든다.
        /// <br/> 새 Value 복제가 실패하면 이 컬렉션을 바꾸지 않는다.
        /// <br/> 재사용 Value의 CloneFrom이 실패해도 목록을 source에 맞춘 뒤 실패를 모아 던진다.
        /// </summary>
        // --------------------------------------------------------------------------------
        private void RebuildEntries<THook>(XOrderedBase<TOrder, TValue, TEntry> source, THook hook)
        where THook : struct, ICloneReadyHook
        {
            var prepared = ListPool<TValue>.Get();
            var reused = ListPool<bool>.Get();
            var sourceInstances = CollectionClone<TValue>.RentInstanceSet();
            var reservedInstances = CollectionClone<TValue>.RentInstanceSet();

            try
            {
                for (var position = 0; position < source.entries.Count; position++)
                {
                    var value = source.entries[position].Value;

                    if (value != null)
                    {
                        sourceInstances.Add(value);
                    }
                }

                // 새 Value 복제는 실패할 수 있으므로 이 컬렉션을 바꾸기 전에 모두 준비한다.
                for (var position = 0; position < source.entries.Count; position++)
                {
                    var entry = source.entries[position];

                    // source가 가진 인스턴스에 CloneFrom하면 source 상태까지 바뀌므로 재사용하지 않는다.
                    if
                    (
                        TryGetReusable(position, entry, out var current)
                        && CollectionClone<TValue>.CanReuse(current, entry.Value)
                        && !sourceInstances.Contains(current)
                        && reservedInstances.Add(current)
                    )
                    {
                        prepared.Add(current);
                        reused.Add(true);
                    }
                    else
                    {
                        prepared.Add(CollectionClone<TValue>.CloneElement(entry.Value, CLONE_OWNER));
                        reused.Add(false);
                    }
                }

                hook.OnCloneReady();

                // 덮어쓰기는 되돌릴 수 없으므로 한 값이 실패해도 목록을 source에 맞추고 나서 모아서 던진다.
                var failures = new ExceptionBuffer();

                for (var position = 0; position < prepared.Count; position++)
                {
                    if (reused[position])
                    {
                        try
                        {
                            CollectionClone<TValue>.ReuseFrom(prepared[position], source.entries[position].Value);
                        }
                        catch (Exception exception)
                        {
                            failures.Add(exception);
                        }
                    }
                }

                // source 순서 그대로 다시 채워 같은 Order 안의 순서도 옮긴다.
                entries.Clear();

                for (var position = 0; position < prepared.Count; position++)
                {
                    entries.Add(CreateEntry(source.entries[position], prepared[position]));
                }

                OnEntriesChanged();

                failures.ThrowWhenAny(CLONE_FAILURE);
            }
            finally
            {
                CollectionClone<TValue>.ReturnInstanceSet(reservedInstances);
                CollectionClone<TValue>.ReturnInstanceSet(sourceInstances);
                ListPool<bool>.Release(reused);
                ListPool<TValue>.Release(prepared);
            }
        }

    #endregion

    #region 정렬

        // ------------------------------------------------------------
        /// <summary>
        /// 두 Order를 비교한다.
        /// </summary>
        // ------------------------------------------------------------
        protected static int CompareOrder(TOrder left, TOrder right) => orderComparer.Compare(left, right);

        // ------------------------------------------------------------
        /// <summary>
        /// 동일 Order 그룹의 오른쪽 끝 다음 index를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        protected int FindIndex(TOrder order)
        {
            var low = 0;
            var high = entries.Count;

            while (low < high)
            {
                var middle = low + ((high - low) / 2);

                if (CompareOrder(entries[middle].Order, order) <= 0)
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

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 항목 목록을 Order 기준으로 안정 정렬한다.
        /// </summary>
        // ------------------------------------------------------------
        internal void NormalizeOrder()
        {
            for (var index = 1; index < entries.Count; index++)
            {
                var entry = entries[index];
                var insertIndex = index - 1;

                while (insertIndex >= 0
                    && CompareOrder(entries[insertIndex].Order, entry.Order) > 0)
                {
                    entries[insertIndex + 1] = entries[insertIndex];
                    insertIndex--;
                }

                entries[insertIndex + 1] = entry;
            }
        }

    #endregion

    #region 직렬화

        void ISerializedCollectionNormalizer.NormalizeSerializedState()
        {
            NormalizeOrder();
            OnEntriesChanged();
        }

        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
            // NONE
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            OnEntriesChanged();
        }

    #endregion

    #region 열거

        // ------------------------------------------------------------
        /// <summary>
        /// 항목을 Order 순서로 열거하는 열거자를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public List<TEntry>.Enumerator GetEnumerator() => entries.GetEnumerator();

        IEnumerator<TEntry> IEnumerable<TEntry>.GetEnumerator() => entries.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => entries.GetEnumerator();

    #endregion

    }
}
