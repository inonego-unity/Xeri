/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : EditorAssetHelper.cs
수정일 : 2026-10-25

# 설명
에디터 어셈블리의 스크립트 위치를 패키지 내 MonoScript 검색으로 찾는다.
찾은 디렉터리를 어셈블리 리로드 전까지 타입별로 재사용한다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

using UnityEditor;
using UnityEngine;

namespace inonego.Xeri
{
    // ============================================================
    /// <summary>
    /// 에디터 에셋 헬퍼.
    /// </summary>
    // ============================================================
    public static class EditorAssetHelper
    {

    #region 필드

        private static readonly Dictionary<Type, string> scriptDirectories = new();

    #endregion

    #region 경로 조회

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> scriptType의 .cs 파일이 위치한 폴더 경로를 반환한다.
        /// <br/> 패키지 내 검색 결과는 어셈블리 리로드 전까지 재사용한다.
        /// </summary>
        // ------------------------------------------------------------
        public static string GetScriptDirectory(Type scriptType)
        {
            if (scriptDirectories.TryGetValue(scriptType, out var directory))
            {
                return directory;
            }

            var package = UnityEditor.PackageManager.PackageInfo.FindForAssembly(scriptType.Assembly);
            var guids = AssetDatabase.FindAssets
            (
                $"t:MonoScript {scriptType.Name}",
                new[]
                {
                    package.assetPath
                }
            );

            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                directory = Path.GetDirectoryName(path).Replace('\\', '/');
                scriptDirectories.Add(scriptType, directory);
                return directory;
            }

            scriptDirectories.Add(scriptType, null);
            return null;
        }

    #endregion

    #region 디렉터리 생성

        // ------------------------------------------------------------
        /// <summary>
        /// AssetDatabase를 통해 에셋 폴더를 재귀적으로 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        public static void MakeDirectory(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;

            var (parent, current) = (Path.GetDirectoryName(path), Path.GetFileName(path));

            if (!AssetDatabase.IsValidFolder(parent))
            {
                MakeDirectory(parent);
            }

            AssetDatabase.CreateFolder(parent, current);
        }

    #endregion

    }
}
