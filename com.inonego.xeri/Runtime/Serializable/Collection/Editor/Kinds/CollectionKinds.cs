/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : CollectionKinds.cs
수정일 : 2026-11-19

# 설명
컬렉션 속성에 맞는 종류 정의를 만드는 Provider 계약과 레지스트리를 둔다.
Provider 구현은 TypeCache로 찾고 FullName 순서로 먼저 맞는 것을 쓴다.
종류 정의를 만들 때 공통으로 쓰는 형식 해석과 비교자 생성도 여기에 둔다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using UnityEditor;

using inonego;
using inonego.Xeri;

namespace inonego.Xeri.Serializable.Editor
{
    // ============================================================
    /// <summary>
    /// 컬렉션 속성에 맞는 종류 정의를 만드는 Provider.
    /// </summary>
    // ============================================================
    internal interface ICollectionKindProvider
    {
        // ------------------------------------------------------------
        /// <summary>
        /// 이 Provider가 다루는 속성이면 종류 정의를 만든다.
        /// </summary>
        // ------------------------------------------------------------
        bool TryCreate(SerializedProperty property, FieldInfo field, out CollectionKind kind);
    }

    // ============================================================
    /// <summary>
    /// 종류 Provider 레지스트리와 공통 형식 해석.
    /// </summary>
    // ============================================================
    internal static class CollectionKinds
    {

    #region 등록

        private static readonly Lazy<IReadOnlyList<ICollectionKindProvider>> providers = new(LoadProviders);

        private static IReadOnlyList<ICollectionKindProvider> LoadProviders()
        {
            return TypeCache.GetTypesDerivedFrom<ICollectionKindProvider>()
                .Where(type => !type.IsAbstract && !type.IsInterface)
                .OrderBy(type => type.FullName, StringComparer.Ordinal)
                .Select(type => (ICollectionKindProvider)Activator.CreateInstance(type, nonPublic: true))
                .ToArray();
        }

    #endregion

    #region 조회

        // ------------------------------------------------------------
        /// <summary>
        /// 속성에 맞는 종류 정의를 찾는다.
        /// </summary>
        // ------------------------------------------------------------
        public static bool TryCreate(SerializedProperty property, FieldInfo field, out CollectionKind kind)
        {
            foreach (var provider in providers.Value)
            {
                if (provider.TryCreate(property, field, out kind))
                {
                    return true;
                }
            }

            kind = null;
            return false;
        }

    #endregion

    #region 형식 해석

        // ------------------------------------------------------------
        /// <summary>
        /// 상속 계층에서 지정한 제네릭 정의의 형식을 찾는다.
        /// </summary>
        // ------------------------------------------------------------
        public static Type FindGenericType(Type type, params Type[] definitions)
        {
            for (; type != null; type = type.BaseType)
            {
                if (type.IsGenericType
                    && Array.IndexOf(definitions, type.GetGenericTypeDefinition()) >= 0)
                {
                    return type;
                }
            }

            return null;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 형식이 정해진 비교자를 object 비교자로 감싼다.
        /// </summary>
        // ------------------------------------------------------------
        public static IEqualityComparer<object> CreateComparer(Type type, object runtimeComparer = null)
        {
            var adapter = typeof(BoxedEqualityComparer<>).MakeGenericType(type ?? typeof(object));
            return (IEqualityComparer<object>)Activator.CreateInstance(adapter, runtimeComparer);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Runtime 컬렉션 인스턴스의 Comparer 속성 값을 읽는다.
        /// </summary>
        // ------------------------------------------------------------
        public static object GetRuntimeComparer(object instance)
        {
            return instance?.GetType().GetProperty("Comparer")?.GetValue(instance);
        }

    #endregion

    }
}
