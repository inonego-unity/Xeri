/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : XeriContextMenuResources.cs
수정일 : 2026-11-23

# 설명
컨텍스트 메뉴가 쓰는 UXML·USS 리소스. 로드·보관은 공용 XeriUIResources가 맡아 모든 메뉴가 공유한다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UIElements;

using inonego;
using inonego.Xeri;

namespace inonego.Xeri.UI
{
    // ============================================================
    /// <summary>
    /// 컨텍스트 메뉴 UXML·USS 리소스 공유 저장소.
    /// </summary>
    // ============================================================
    internal static class XeriContextMenuResources
    {

    #region 리소스

        private const string LEVEL_LAYOUT_PATH = "Xeri/UI/ContextMenu/XeriContextMenu";
        private const string ACTION_LAYOUT_PATH = "Xeri/UI/ContextMenu/XeriContextMenuAction";
        private const string MENU_STYLE_PATH = "Xeri/UI/ContextMenu/XeriContextMenuStyle";
        private const string SUBJECT = "ContextMenu";

        // ------------------------------------------------------------
        /// <summary>
        /// 메뉴 단계 UXML 레이아웃.
        /// </summary>
        // ------------------------------------------------------------
        internal static VisualTreeAsset LevelLayout => XeriUIResources.Require<VisualTreeAsset>(LEVEL_LAYOUT_PATH, SUBJECT);

        // ------------------------------------------------------------
        /// <summary>
        /// 명령 행 UXML 레이아웃.
        /// </summary>
        // ------------------------------------------------------------
        internal static VisualTreeAsset ActionLayout => XeriUIResources.Require<VisualTreeAsset>(ACTION_LAYOUT_PATH, SUBJECT);

        // ------------------------------------------------------------
        /// <summary>
        /// 메뉴 공통 USS.
        /// </summary>
        // ------------------------------------------------------------
        internal static StyleSheet MenuStyle => XeriUIResources.Require<StyleSheet>(MENU_STYLE_PATH, SUBJECT);

    #endregion

    }
}
