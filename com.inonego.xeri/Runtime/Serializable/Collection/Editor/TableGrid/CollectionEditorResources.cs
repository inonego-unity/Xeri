/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : CollectionEditorResources.cs
수정일 : 2026-11-23

# 설명
컬렉션 표(CollectionTableGrid)와 편집 창이 쓰는 UXML·USS를 Resources에서 로드한다.
리소스는 Editor/Resources/Xeri/Collection에 두고 Unity의 에셋 수명주기에 맡긴다.
UXML과 USS는 Resources 경로가 겹치지 않도록 별도 이름으로 구분한다.
로드 결과 보관과 없을 때 예외는 공용 XeriUIResources가 맡는다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UIElements;

using inonego;
using inonego.Xeri;
using inonego.Xeri.UI;

namespace inonego.Xeri.Serializable.Editor
{
    // ============================================================
    /// <summary>
    /// Collection Editor의 공통 UI 리소스를 로드한다.
    /// </summary>
    // ============================================================
    internal static class CollectionEditorResources
    {

    #region 필드

        private const string TABLE_GRID_LAYOUT_PATH = "Xeri/Collection/CollectionTableGrid";
        private const string TABLE_GRID_STYLE_PATH = "Xeri/Collection/CollectionTableGridStyle";
        private const string EDIT_WINDOW_LAYOUT_PATH = "Xeri/Collection/CollectionEditWindow";
        private const string EDIT_WINDOW_STYLE_PATH = "Xeri/Collection/CollectionEditWindowStyle";
        private const string UTILITY_WINDOW_STYLE_PATH = "Xeri/Collection/CollectionUtilityWindow";
        private const string SUBJECT = "Collection Editor";

    #endregion

    #region 리소스 로드

        // ------------------------------------------------------------
        /// <summary>
        /// 컬렉션 표 크롬(머리·검색·추가·본문 자리)의 UXML을 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public static VisualTreeAsset LoadTableGridLayout()
        {
            return XeriUIResources.Require<VisualTreeAsset>(TABLE_GRID_LAYOUT_PATH, SUBJECT);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 컬렉션 표의 USS를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public static StyleSheet LoadTableGridStyle()
        {
            return XeriUIResources.Require<StyleSheet>(TABLE_GRID_STYLE_PATH, SUBJECT);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 상세 편집창의 UXML을 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public static VisualTreeAsset LoadEditWindowLayout()
        {
            return XeriUIResources.Require<VisualTreeAsset>(EDIT_WINDOW_LAYOUT_PATH, SUBJECT);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 상세 편집창의 USS를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public static StyleSheet LoadEditWindowStyle()
        {
            return XeriUIResources.Require<StyleSheet>(EDIT_WINDOW_STYLE_PATH, SUBJECT);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 보조 입력창의 USS를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public static StyleSheet LoadUtilityWindowStyle()
        {
            return XeriUIResources.Require<StyleSheet>(UTILITY_WINDOW_STYLE_PATH, SUBJECT);
        }

    #endregion

    }
}
