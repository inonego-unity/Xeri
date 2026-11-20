/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : XeriContextMenuResources.cs
수정일 : 2026-11-18

# 설명
컨텍스트 메뉴가 쓰는 UXML·USS 리소스를 Resources에서 한 번만 불러와 모든 메뉴가 공유한다.
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

        private const string resourceRoot = "Xeri/UI/ContextMenu/";

        private static VisualTreeAsset levelLayout;
        private static VisualTreeAsset actionLayout;
        private static StyleSheet menuStyle;

        // ------------------------------------------------------------
        /// <summary>
        /// 메뉴 단계 UXML 레이아웃.
        /// </summary>
        // ------------------------------------------------------------
        internal static VisualTreeAsset LevelLayout
        {
            get
            {
                EnsureLoaded();
                return levelLayout;
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 명령 행 UXML 레이아웃.
        /// </summary>
        // ------------------------------------------------------------
        internal static VisualTreeAsset ActionLayout
        {
            get
            {
                EnsureLoaded();
                return actionLayout;
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 메뉴 공통 USS.
        /// </summary>
        // ------------------------------------------------------------
        internal static StyleSheet MenuStyle
        {
            get
            {
                EnsureLoaded();
                return menuStyle;
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 아직 불러오지 않은 리소스를 불러온다.
        /// </summary>
        // ------------------------------------------------------------
        private static void EnsureLoaded()
        {
            if (levelLayout == null)
            {
                levelLayout = Load<VisualTreeAsset>("XeriContextMenu");
            }

            if (actionLayout == null)
            {
                actionLayout = Load<VisualTreeAsset>("XeriContextMenuAction");
            }

            if (menuStyle == null)
            {
                menuStyle = Load<StyleSheet>("XeriContextMenuStyle");
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 필수 UI 리소스를 Resources에서 로드한다.
        /// </summary>
        // ------------------------------------------------------------
        private static T Load<T>(string name)
        where T : UnityEngine.Object
        {
            var path = $"{resourceRoot}{name}";
            var asset = Resources.Load<T>(path);

            if (asset == null)
            {
                throw new InvalidOperationException($"컨텍스트 메뉴 리소스를 찾을 수 없습니다: {path}");
            }

            return asset;
        }

    #endregion

    }
}
