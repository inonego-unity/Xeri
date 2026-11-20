/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : EditorTypeIndex.cs
수정일 : 2026-10-30

# 설명
Unity TypeCache 기반의 공통 Editor 타입 인덱스.
Player assembly 타입 목록을 도메인당 한 번 구성하고 다른 Editor 기능에 제공한다.

# 특이사항
Unity TypeCache를 복제하지 않고, Editor 초기화 이후 타입 목록만 미리 준비한다.
선언 타입별 후보와 UI 캐시는 호출자가 소유한다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEditor;
using UnityEditor.Compilation;

namespace inonego.Xeri.Editor
{
    // ============================================================
    /// <summary>
    /// Unity TypeCache의 Player 타입 인덱스를 제공한다.
    /// </summary>
    // ============================================================
    [InitializeOnLoad]
    internal static class EditorTypeIndex
    {

    #region 초기화

        // ------------------------------------------------------------
        /// <summary>
        /// Domain reload 이후 Editor 초기화가 끝나면 타입 목록을 준비한다.
        /// </summary>
        // ------------------------------------------------------------
        static EditorTypeIndex()
        {
            EditorApplication.delayCall += WarmUp;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Picker 최초 실행 시 타입 열거 비용을 피하기 위해 준비한다.
        /// </summary>
        // ------------------------------------------------------------
        private static void WarmUp()
        {
            _ = PlayerTypeList;
        }

    #endregion

    #region 조회

        // ------------------------------------------------------------
        /// <summary>
        /// Player 및 UnityEngine 타입의 캐시된 목록을 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        internal static IReadOnlyList<Type> PlayerTypeList
        {
            get
            {
                if (playerTypes == null)
                {
                    playerTypes = BuildPlayerTypeList();
                }

                return playerTypes;
            }
        }
        private static IReadOnlyList<Type> playerTypes;

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 domain의 Player 및 UnityEngine 타입 목록을 구성한다.
        /// </summary>
        // ------------------------------------------------------------
        private static IReadOnlyList<Type> BuildPlayerTypeList()
        {
            var assemblyNames = new HashSet<string>(StringComparer.Ordinal);
            foreach (var assembly in CompilationPipeline.GetAssemblies())
            {
                if ((assembly.flags & AssemblyFlags.EditorAssembly) == 0)
                {
                    assemblyNames.Add(assembly.name);
                }
            }

            var types = new List<Type>();
            var visited = new HashSet<Type>();
            foreach (var type in TypeCache.GetTypesDerivedFrom<object>())
            {
                var assemblyName = type.Assembly.GetName().Name;
                if ((assemblyNames.Contains(assemblyName)
                    || assemblyName.StartsWith(nameof(UnityEngine), StringComparison.Ordinal))
                    && visited.Add(type))
                {
                    types.Add(type);
                }
            }

            return Array.AsReadOnly(types.ToArray());
        }

    #endregion

    }
}
