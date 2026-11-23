/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TableGridTheming.cs
수정일 : 2026-11-21

# 설명
표·팝오버·쿼리 칩 줄·검색창 루트에 테마 수정 클래스(xeri-table--editor, xeri-table--light)를 붙인다.
TableGridTheme.uss가 이 클래스에 따라 --xeri-table-* 변수 값을 고른다.

# 특이사항, 제약사항
테마는 #if UNITY_EDITOR가 아니라 요소가 놓인 패널 종류로 정한다. Player 패널에는 Unity 에디터 USS 변수가 없으므로 Editor 테마를 Default로 바꾼다.
Runtime 어셈블리는 UnityEditor를 쓸 수 없으므로 Unity 스킨은 Editor 어셈블리가 IsEditorSkinDark에 연결한다. 연결이 없으면 다크로 본다.
각 루트는 자기 패널 기준으로 정한다(팝오버는 Editor에서 별도 창, Runtime에서 패널 위 프레임에 놓인다).
========================================================================= BLOCK_HEADER_END */

using System;

using UnityEngine;
using UnityEngine.UIElements;

namespace inonego.Xeri.UI.TableGrid
{
    // ============================================================
    /// <summary>
    /// 표 계열 루트 요소에 테마 수정 클래스를 붙인다.
    /// </summary>
    // ============================================================
    internal static class TableGridTheming
    {

    #region 클래스 이름

        // ------------------------------------------------------------
        /// <summary>
        /// Editor 테마(Unity 에디터 변수 사용)를 나타내는 클래스.
        /// </summary>
        // ------------------------------------------------------------
        public const string EDITOR_CLASS = "xeri-table--editor";

        // ------------------------------------------------------------
        /// <summary>
        /// 라이트 팔레트를 나타내는 클래스.
        /// </summary>
        // ------------------------------------------------------------
        public const string LIGHT_CLASS = "xeri-table--light";

    #endregion

    #region 스킨 연결

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> Unity 에디터 스킨이 다크인지 알려 준다. Editor 어셈블리가 지정한다.
        /// <br/> null이면 다크 스킨으로 본다.
        /// </summary>
        // ----------------------------------------------------------------------
        internal static Func<bool> IsEditorSkinDark;

    #endregion

    #region 적용

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> 요소가 패널에 붙을 때마다 테마 클래스를 붙이도록 연결한다.
        /// <br/> 이미 패널에 있으면 바로 적용한다.
        /// </summary>
        // ------------------------------------------------------------
        public static void Attach(VisualElement element, TableGridOptions options)
        {
            if (element == null)
            {
                throw new ArgumentNullException(nameof(element));
            }

            element.RegisterCallback<AttachToPanelEvent, TableGridOptions>(HandleAttachToPanel, options);
            if (element.panel != null)
            {
                Apply(element, options);
            }
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> 현재 패널과 옵션으로 테마 클래스를 정해 붙인다.
        /// <br/> options가 null이면 기본 옵션(Default 테마, Auto 색 모드)으로 본다.
        /// </summary>
        // ----------------------------------------------------------------------
        public static void Apply(VisualElement element, TableGridOptions options)
        {
            if (element == null)
            {
                throw new ArgumentNullException(nameof(element));
            }

            // Unity 에디터 USS 변수는 Editor 패널에만 있으므로 Player 패널과 패널 밖은 Default로 본다.
            var editorPanel = element.panel != null && element.panel.contextType == ContextType.Editor;
            var editorTheme = editorPanel && options?.Theme == TableGridTheme.Editor;

            // Editor 테마는 Unity 스킨을 그대로 따르고, Default 테마는 ColorMode로 고른다.
            var light = editorTheme ? !IsSkinDark() : IsDefaultLight(options?.ColorMode ?? TableGridColorMode.Auto, editorPanel);

            element.EnableInClassList(EDITOR_CLASS, editorTheme);
            element.EnableInClassList(LIGHT_CLASS, light);
        }

        private static bool IsDefaultLight(TableGridColorMode colorMode, bool editorPanel)
        {
            return colorMode switch
            {
                TableGridColorMode.Light => true,
                TableGridColorMode.Dark => false,
                _ => editorPanel && !IsSkinDark(),
            };
        }

        private static bool IsSkinDark()
        {
            return IsEditorSkinDark?.Invoke() ?? true;
        }

    #endregion

    #region 이벤트 핸들러

        private static void HandleAttachToPanel(AttachToPanelEvent evt, TableGridOptions options)
        {
            Apply((VisualElement)evt.currentTarget, options);
        }

    #endregion

    }
}
