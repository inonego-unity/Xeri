/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TableGridResources.cs
수정일 : 2026-11-23

# 설명
TableGrid의 UXML·USS를 Resources에서 읽는다. Runtime·Editor 양쪽에서 AssetDatabase 없이 동작한다.

# 특이사항, 제약사항
로드·보관·없을 때 예외는 공용 XeriUIResources가 맡는다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UIElements;

namespace inonego.Xeri.UI.TableGrid
{
    // ============================================================
    /// <summary>
    /// TableGrid의 UXML·USS 자산.
    /// </summary>
    // ============================================================
    internal static class TableGridResources
    {

    #region 경로

        private const string LAYOUT_PATH = "Xeri/UI/TableGrid/TableGrid";
        private const string THEME_PATH = "Xeri/UI/TableGrid/TableGridTheme";
        private const string STYLE_PATH = "Xeri/UI/TableGrid/TableGridStyle";
        private const string SUBJECT = "TableGrid";
    #endregion

    #region 로드

        // ------------------------------------------------------------
        /// <summary>
        /// 헤더·본문·상태 줄 구조의 UXML.
        /// </summary>
        // ------------------------------------------------------------
        public static VisualTreeAsset Layout => XeriUIResources.Require<VisualTreeAsset>(LAYOUT_PATH, SUBJECT);

        // ------------------------------------------------------------
        /// <summary>
        /// 색·간격 변수를 정의하는 테마 USS.
        /// </summary>
        // ------------------------------------------------------------
        public static StyleSheet Theme => XeriUIResources.Require<StyleSheet>(THEME_PATH, SUBJECT);

        // ------------------------------------------------------------
        /// <summary>
        /// 표의 모양 규칙 USS.
        /// </summary>
        // ------------------------------------------------------------
        public static StyleSheet Style => XeriUIResources.Require<StyleSheet>(STYLE_PATH, SUBJECT);

    #endregion

    }
}
