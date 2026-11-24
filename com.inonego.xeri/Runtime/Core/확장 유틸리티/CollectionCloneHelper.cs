/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : CollectionCloneHelper.cs
수정일 : 2026-11-24

# 설명
Dictionary, HashSet, Queue, Stack, List에 CloneFrom을 붙이는 확장 메서드를 정의한다.
XDictionary·XHashSet·XQueue·XStack도 이 구현을 호출하며 BCL 컬렉션과 같은 규칙으로 복제한다.

# 특이사항, 제약사항
값 형식·string·UnityEngine.Object·delegate·Type 요소는 그대로 복사하고, 그 밖의 참조 요소는 CollectionClone 규칙으로 복제한다. 복제를 지원하지 않는 참조 요소는 예외를 던진다.
Dictionary의 Key와 비교자는 복제하지 않고 target의 비교자를 그대로 쓴다. Key는 공유한다.
Dictionary의 참조 Value는 같은 Key·같은 타입의 기존 인스턴스를 재사용하되 source가 가진 인스턴스는 재사용하지 않는다.
복제는 source와 target이 같은 구체 타입인지 확인하지 않으므로 파생 컬렉션이 가진 추가 상태는 복사하지 않는다.
새 Value 복제(준비)가 실패하면 target을 바꾸지 않는다.
재사용 Value의 CloneFrom은 되돌릴 수 없으므로 실패해도 나머지를 끝까지 복사하고 Key 구성을 source에 맞춘 뒤 실패를 모아 던진다.
실패는 하나면 원래 예외를, 둘 이상이면 AggregateException을 던지며 실패한 Value는 이전 상태로 남는다.
임시 버퍼는 풀에서 빌려 쓰므로 값 형식 요소와 같은 Key의 재사용은 반복 호출에서 할당하지 않는다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Pool;

namespace inonego.Xeri
{
    // ============================================================
    /// <summary>
    /// 기본 컬렉션의 내용을 복사하는 CloneFrom 확장 메서드 모음.
    /// </summary>
    // ============================================================
    public static class CollectionCloneHelper
    {

    #region Dictionary

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> source의 내용을 외부 알림 없이 target에 복사한다.
        /// <br/> Key는 공유하고 값 형식·string Value는 그대로 복사한다.
        /// <br/> 참조 Value는 같은 Key의 기존 인스턴스를 재사용하거나 복제한다.
        /// </summary>
        // ----------------------------------------------------------------------
        public static void CloneFrom<TKey, TValue>(this Dictionary<TKey, TValue> target, Dictionary<TKey, TValue> source)
        {
            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (ReferenceEquals(target, source))
            {
                return;
            }

            // 복제가 필요 없는 Value는 비우고 그대로 채운다.
            if (CollectionClone<TValue>.IsPlain)
            {
                target.Clear();

                foreach (var pair in source)
                {
                    target.Add(pair.Key, pair.Value);
                }

                return;
            }

            var prepared = ListPool<TValue>.Get();
            var reused = ListPool<bool>.Get();
            var sourceInstances = CollectionClone<TValue>.RentInstanceSet();
            var reservedInstances = CollectionClone<TValue>.RentInstanceSet();

            try
            {
                foreach (var pair in source)
                {
                    if (pair.Value is not null)
                    {
                        sourceInstances.Add(pair.Value);
                    }
                }

                // 새 Value 복제는 실패할 수 있으므로 target을 바꾸기 전에 모두 준비한다.
                foreach (var pair in source)
                {
                    // source가 가진 인스턴스에 CloneFrom하면 source 상태까지 바뀌므로 재사용하지 않는다.
                    if
                    (
                        target.TryGetValue(pair.Key, out var current)
                        && CollectionClone<TValue>.CanReuse(current, pair.Value)
                        && !sourceInstances.Contains(current)
                        && reservedInstances.Add(current)
                    )
                    {
                        prepared.Add(current);
                        reused.Add(true);
                    }
                    else
                    {
                        prepared.Add(CollectionClone<TValue>.CloneElement(pair.Value, "Dictionary"));
                        reused.Add(false);
                    }
                }

                // 덮어쓰기는 되돌릴 수 없으므로 한 Value가 실패해도 Key 구성을 source에 맞추고 나서 모아서 던진다.
                var failures = new ExceptionBuffer();
                var index = 0;

                foreach (var pair in source)
                {
                    if (reused[index])
                    {
                        try
                        {
                            CollectionClone<TValue>.ReuseFrom(prepared[index], pair.Value);
                        }
                        catch (Exception exception)
                        {
                            failures.Add(exception);
                        }
                    }

                    index++;
                }

                target.Clear();

                index = 0;

                foreach (var pair in source)
                {
                    target.Add(pair.Key, prepared[index]);
                    index++;
                }

                failures.ThrowWhenAny("Dictionary 복제 중 여러 Value가 실패했습니다.");
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

    #region HashSet

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> source의 내용을 외부 알림 없이 target에 복사한다.
        /// <br/> 참조 요소는 복제하며 복제가 실패하면 target을 바꾸지 않는다.
        /// </summary>
        // ------------------------------------------------------------
        public static void CloneFrom<T>(this HashSet<T> target, HashSet<T> source)
        {
            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (ReferenceEquals(target, source))
            {
                return;
            }

            // 복제가 필요 없는 요소는 비우고 그대로 채운다.
            if (CollectionClone<T>.IsPlain)
            {
                target.Clear();

                foreach (var item in source)
                {
                    target.Add(item);
                }

                return;
            }

            var prepared = ListPool<T>.Get();

            try
            {
                // 새 요소 복제는 실패할 수 있으므로 target을 바꾸기 전에 모두 준비한다.
                foreach (var item in source)
                {
                    prepared.Add(CollectionClone<T>.CloneElement(item, "HashSet"));
                }

                target.Clear();

                for (var index = 0; index < prepared.Count; index++)
                {
                    target.Add(prepared[index]);
                }
            }
            finally
            {
                ListPool<T>.Release(prepared);
            }
        }

    #endregion

    #region Queue

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> source의 내용을 front 순서 그대로 외부 알림 없이 target에 복사한다.
        /// <br/> 참조 요소는 복제하며 복제가 실패하면 target을 바꾸지 않는다.
        /// </summary>
        // ----------------------------------------------------------------------
        public static void CloneFrom<T>(this Queue<T> target, Queue<T> source)
        {
            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (ReferenceEquals(target, source))
            {
                return;
            }

            // 복제가 필요 없는 요소는 비우고 그대로 채운다.
            if (CollectionClone<T>.IsPlain)
            {
                target.Clear();

                foreach (var item in source)
                {
                    target.Enqueue(item);
                }

                return;
            }

            var prepared = ListPool<T>.Get();

            try
            {
                // 새 요소 복제는 실패할 수 있으므로 target을 바꾸기 전에 모두 준비한다.
                foreach (var item in source)
                {
                    prepared.Add(CollectionClone<T>.CloneElement(item, "Queue"));
                }

                target.Clear();

                for (var index = 0; index < prepared.Count; index++)
                {
                    target.Enqueue(prepared[index]);
                }
            }
            finally
            {
                ListPool<T>.Release(prepared);
            }
        }

    #endregion

    #region Stack

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> source의 내용을 top 순서 그대로 외부 알림 없이 target에 복사한다.
        /// <br/> 참조 요소는 복제하며 복제가 실패하면 target을 바꾸지 않는다.
        /// </summary>
        // ----------------------------------------------------------------------
        public static void CloneFrom<T>(this Stack<T> target, Stack<T> source)
        {
            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (ReferenceEquals(target, source))
            {
                return;
            }

            var prepared = ListPool<T>.Get();

            try
            {
                // 열거는 top부터이므로 그 순서로 준비한다. 새 요소 복제는 target을 바꾸기 전에 모두 끝낸다.
                foreach (var item in source)
                {
                    prepared.Add(CollectionClone<T>.CloneElement(item, "Stack"));
                }

                target.Clear();

                // top-first 순서를 지키도록 역순으로 Push한다.
                for (var index = prepared.Count - 1; index >= 0; index--)
                {
                    target.Push(prepared[index]);
                }
            }
            finally
            {
                ListPool<T>.Release(prepared);
            }
        }

    #endregion

    #region List

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> source의 내용을 순서 그대로 외부 알림 없이 target에 복사한다.
        /// <br/> 참조 요소는 복제하며 복제가 실패하면 target을 바꾸지 않는다.
        /// </summary>
        // ----------------------------------------------------------------------
        public static void CloneFrom<T>(this List<T> target, List<T> source)
        {
            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (ReferenceEquals(target, source))
            {
                return;
            }

            // 복제가 필요 없는 요소는 비우고 그대로 채운다.
            if (CollectionClone<T>.IsPlain)
            {
                target.ClearAndAddRange(source);

                return;
            }

            var prepared = ListPool<T>.Get();

            try
            {
                // 새 요소 복제는 실패할 수 있으므로 target을 바꾸기 전에 모두 준비한다.
                for (var index = 0; index < source.Count; index++)
                {
                    prepared.Add(CollectionClone<T>.CloneElement(source[index], "List"));
                }

                target.ClearAndAddRange(prepared);
            }
            finally
            {
                ListPool<T>.Release(prepared);
            }
        }

    #endregion

    }
}
