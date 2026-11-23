/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TableGridEditorSkin.cs
수정일 : 2026-11-21

# 설명
Runtime 어셈블리의 TableGrid 테마 판정에 Unity 에디터 스킨(다크·라이트)을 연결한다.

# 특이사항, 제약사항
Editor 어셈블리(inonego.Xeri.Editor)에서만 컴파일된다. 도메인 재로드마다 다시 연결한다.
========================================================================= BLOCK_HEADER_END */

using System;

using UnityEditor;

using inonego;
using inonego.Xeri;
using inonego.Xeri.UI;
using inonego.Xeri.UI.TableGrid;

namespace inonego.Xeri.UI.TableGrid.Editor
{
    // ============================================================
    /// <summary>
    /// TableGrid 테마에 Unity 에디터 스킨을 연결한다.
    /// </summary>
    // ============================================================
    internal static class TableGridEditorSkin
    {

    #region 초기화

        [InitializeOnLoadMethod]
        private static void Connect()
        {
            TableGridTheming.IsEditorSkinDark = () => EditorGUIUtility.isProSkin;
        }

    #endregion

    }
}
