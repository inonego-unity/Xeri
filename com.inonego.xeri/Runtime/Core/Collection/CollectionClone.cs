/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : CollectionClone.cs
수정일 : 2026-11-24

# 설명
컬렉션 CloneFrom이 함께 쓰는 요소 복제 규칙을 정의한다.
값 형식, string, UnityEngine.Object, delegate, Type 요소는 복제 없이 그대로 복사(공유)하고, 그 밖의 참조 요소는 IDeepCloneable<T>로 복제한다.

# 특이사항, 제약사항
UnityEngine.Object는 에셋·씬 객체 참조이고 delegate와 Type은 복제 대상이 아니므로 공유한다.
요소 타입이 object나 인터페이스여도 런타임 값이 string, 박싱된 값 형식, UnityEngine.Object, delegate, Type이면 공유한다.
그 밖의 참조 요소가 복제를 지원하지 않으면 공유하지 않고 InvalidOperationException을 던진다.
값 형식 요소가 내부에 참조를 가져도 그대로 복사하므로 그 참조는 공유된다.
같은 인스턴스가 한 컬렉션에 여러 번 있어도 각각 따로 복제하므로 인스턴스 공유 관계는 복제되지 않는다.
인스턴스 비교 집합은 스레드마다 재사용해 복제 중 반복 할당을 만들지 않는다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

namespace inonego.Xeri
{
    // ============================================================
    /// <summary>
    /// 컬렉션 복제가 준비를 마친 시점에 호출되는 훅.
    /// </summary>
    // ============================================================
    internal interface ICloneReadyHook
    {
        // ------------------------------------------------------------
        /// <summary>
        /// 새 요소 준비가 끝나 더는 실패하지 않을 때 한 번 호출된다.
        /// </summary>
        // ------------------------------------------------------------
        void OnCloneReady();
    }

    // ============================================================
    /// <summary>
    /// 아무 일도 하지 않는 복제 훅.
    /// </summary>
    // ============================================================
    internal readonly struct NoCloneHook : ICloneReadyHook
    {
        // ------------------------------------------------------------
        /// <summary>
        /// 아무 일도 하지 않는다.
        /// </summary>
        // ------------------------------------------------------------
        public void OnCloneReady()
        {
            // NONE
        }
    }

    // ============================================================
    /// <summary>
    /// 컬렉션 요소 하나를 복제하는 공통 규칙.
    /// </summary>
    // ============================================================
    internal static class CollectionClone<T>
    {

    #region 판정

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> 형식만으로 복제 없이 그대로 복사하기로 정해지는지 나타낸다.
        /// <br/> 값 형식, string, UnityEngine.Object, delegate, Type이 해당한다.
        /// </summary>
        // ----------------------------------------------------------------------
        internal static readonly bool IsPlain = IsSharedType(typeof(T));

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> current를 source 상태로 덮어쓸 수 있는지 반환한다.
        /// <br/> 둘 다 null이 아니고 서로 다른 인스턴스이며
        /// <br/> 구체 타입이 같고 IDeepCloneableFrom을 구현해야 한다.
        /// </summary>
        // ------------------------------------------------------------
        internal static bool CanReuse(T current, T source)
        {
            if (IsPlain || current is null || source is null)
            {
                return false;
            }

            if (ReferenceEquals(current, source) || current.GetType() != source.GetType())
            {
                return false;
            }

            return current is IDeepCloneableFrom<T>;
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// current에 source 상태를 복사한다. CanReuse가 true일 때만 부른다.
        /// </summary>
        // ----------------------------------------------------------------------
        internal static void ReuseFrom(T current, T source)
        {
            ((IDeepCloneableFrom<T>)current).CloneFrom(source);
        }

    #endregion

    #region 복제

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> 요소의 복사본을 만든다. 공유하는 요소와 null은 그대로 돌려준다.
        /// <br/> 복제를 지원하지 않는 참조 요소는 공유하지 않고 예외를 던진다.
        /// </summary>
        // ----------------------------------------------------------------------
        internal static T CloneElement(T source, string owner)
        {
            if (IsPlain || source is null || IsSharedInstance(source))
            {
                return source;
            }

            if (source is IDeepCloneable<T> cloneable)
            {
                return cloneable.Clone();
            }

            throw new InvalidOperationException
            (
                $"{owner}의 요소({source.GetType().Name})를 복제할 수 없습니다. IDeepCloneable<{typeof(T).Name}>를 구현해야 합니다."
            );
        }

    #endregion

    #region 공유 판정

        // ------------------------------------------------------------
        /// <summary>
        /// 형식이 값 형식이거나 공유해야 하는 참조 형식인지 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        private static bool IsSharedType(Type type)
        {
            return type.IsValueType
                || type == typeof(string)
                || typeof(Delegate).IsAssignableFrom(type)
                || typeof(Type).IsAssignableFrom(type)
                || typeof(UnityEngine.Object).IsAssignableFrom(type);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 런타임 값이 복제 없이 공유해야 하는 값인지 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        private static bool IsSharedInstance(T source)
        {
            return source is string
                || source is ValueType
                || source is Delegate
                || source is Type
                || source is UnityEngine.Object;
        }

    #endregion

    #region 인스턴스 집합

        // 스레드마다 비워서 돌려받은 인스턴스 비교 집합을 쌓아 둔다. 복제 안에서 복제가 중첩돼도 서로 다른 집합을 받는다.
        [ThreadStatic]
        private static Stack<HashSet<T>> instanceSets;

        // ------------------------------------------------------------
        /// <summary>
        /// 비어 있는 인스턴스 비교 집합을 빌린다.
        /// </summary>
        // ------------------------------------------------------------
        internal static HashSet<T> RentInstanceSet()
        {
            var pool = instanceSets;

            if (pool != null && pool.Count > 0)
            {
                return pool.Pop();
            }

            // 기존 참조 비교자를 공변성으로 재사용한다. 참조 형식 T에서만 호출한다.
            return new HashSet<T>((IEqualityComparer<T>)(object)ReferenceEqualityComparer<object>.Instance);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 빌린 인스턴스 비교 집합을 비워서 돌려준다.
        /// </summary>
        // ------------------------------------------------------------
        internal static void ReturnInstanceSet(HashSet<T> set)
        {
            set.Clear();

            instanceSets ??= new();
            instanceSets.Push(set);
        }

    #endregion

    }
}
