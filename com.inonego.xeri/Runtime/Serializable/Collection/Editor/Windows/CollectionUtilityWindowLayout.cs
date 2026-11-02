/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : CollectionUtilityWindowLayout.cs
수정일 : 2026-10-31

# 설명
Collection의 작은 입력용 EditorWindow에 Resources 기반 공통 USS와 콘텐츠 영역을 구성한다.
입력 필드 및 명령 상태는 개별 EditorWindow가 소유한다.
========================================================================= BLOCK_HEADER_END */

using System;

using UnityEngine;
using UnityEngine.UIElements;

namespace inonego.Xeri.Serializable.Editor
{
    // ============================================================
    /// <summary>
    /// Collection 보조 입력 창의 공통 레이아웃을 구성한다.
    /// </summary>
    // ============================================================
    internal static class CollectionUtilityWindowLayout
    {

    #region 생성

        // ------------------------------------------------------------
        /// <summary>
        /// 기존 콘텐츠를 비우고 입력 창 공통 스타일이 적용된 영역을 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public static VisualElement Build(VisualElement root)
        {
            if (root == null)
            {
                throw new ArgumentNullException(nameof(root));
            }

            var styleSheet = CollectionEditorResources.LoadUtilityWindowStyle();
            root.Clear();
            if (!root.styleSheets.Contains(styleSheet))
            {
                root.styleSheets.Add(styleSheet);
            }

            var content = new VisualElement();
            content.AddToClassList("xeri-collection-utility");
            root.Add(content);
            return content;
        }

    #endregion

    }
}
