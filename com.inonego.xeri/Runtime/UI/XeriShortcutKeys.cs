/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : XeriShortcutKeys.cs
수정일 : 2026-11-23

# 설명
플랫폼에 따른 단축키 판정과 메뉴 단축키 표기를 한 곳에서 정한다.
macOS(에디터·플레이어)는 Cmd(⌘)·Option(⌥), 그 밖은 Ctrl·Alt를 쓴다.

# 특이사항, 제약사항
판정은 실행 중인 플랫폼(UnityEngine.Application.platform)으로 한다. ContextMenu·TableGrid·Picker처럼
여러 UI가 쓰는 공용 기반이며, 메뉴 항목의 단축키 글자와 키 처리(예: macOS의 Backspace 지우기,
Ctrl+클릭 우클릭, Enter 계열 확정 키)가 같은 판정을 쓰게 한다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace inonego.Xeri.UI
{
    // ============================================================
    /// <summary>
    /// 플랫폼 단축키 판정과 표기.
    /// </summary>
    // ============================================================
    internal static class XeriShortcutKeys
    {

    #region 판정

        // ------------------------------------------------------------
        /// <summary>
        /// macOS(에디터·플레이어)에서 실행 중인지 여부.
        /// </summary>
        // ------------------------------------------------------------
        public static bool IsMac
        {
            get
            {
                var platform = UnityEngine.Application.platform;
                return platform == RuntimePlatform.OSXEditor || platform == RuntimePlatform.OSXPlayer;
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 확정 키(Enter, 숫자패드 Enter)인지 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public static bool IsSubmit(KeyCode keyCode)
        {
            return keyCode == KeyCode.Return || keyCode == KeyCode.KeypadEnter;
        }

    #endregion

    #region 표기

        // --------------------------------------------------------------------------------
        /// <summary>
        /// <br/> 단축키 표기를 만든다. 예: macOS는 "⌘C"·"⌥↑", 그 밖은 "Ctrl+C"·"Alt+↑".
        /// <br/> action은 명령 키(⌘ 또는 Ctrl)이고 alt는 Option(⌥) 또는 Alt다.
        /// <br/> 키 처리와 같은 KeyCode로 만들어 메뉴 표기와 실제 동작이 어긋나지 않게 한다.
        /// </summary>
        // --------------------------------------------------------------------------------
        public static string Format(KeyCode key, bool action = false, bool alt = false)
        {
            var name = KeyName(key);

            if (IsMac)
            {
                return (alt ? "⌥" : string.Empty) + (action ? "⌘" : string.Empty) + name;
            }

            return (action ? "Ctrl+" : string.Empty) + (alt ? "Alt+" : string.Empty) + name;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 표기에 쓸 키 이름. 방향키는 화살표, Enter 계열은 "Enter", 그 밖은 KeyCode 이름이다.
        /// </summary>
        // ------------------------------------------------------------
        private static string KeyName(KeyCode key)
        {
            switch (key)
            {
                case KeyCode.UpArrow: return "↑";
                case KeyCode.DownArrow: return "↓";
                case KeyCode.LeftArrow: return "←";
                case KeyCode.RightArrow: return "→";
                case KeyCode.Return:
                case KeyCode.KeypadEnter: return "Enter";
                default: return key.ToString();
            }
        }

    #endregion

    }
}
