/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : EditorAssetHelper.cs
수정일 : 2026-05-07

# 설명
에디터 어셈블리용 에셋 경로 헬퍼.
스크립트 파일이 위치한 폴더 경로를 반환한다.
패키지 내 MonoScript 검색으로 이름 충돌을 방지하며, 파일 이동에도 자동 추적된다.
========================================================================= BLOCK_HEADER_END */

using System;
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
        // -------------------------------------------------------------------
        /// <summary>
        /// <br/> scriptType의 .cs 파일이 위치한 폴더 경로를 반환한다.
        /// <br/> 패키지 내 검색으로 이름 충돌을 방지하며, 파일 이동에도 자동 추적된다.
        /// </summary>
        // -------------------------------------------------------------------
        public static string GetScriptDirectory(Type scriptType)
        {
            var package = UnityEditor.PackageManager.PackageInfo.FindForAssembly(scriptType.Assembly);
            var guids   = AssetDatabase.FindAssets
            (
                $"t:MonoScript {scriptType.Name}",
                new[] { package.assetPath }
            );

            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                return Path.GetDirectoryName(path).Replace('\\', '/');
            }

            return null;
        }

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

    }
}
