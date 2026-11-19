/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : CollectionEditorResources.cs
수정일 : 2026-11-19

# 설명
컬렉션 표(CollectionTable)와 편집 창이 쓰는 UXML·USS를 Resources에서 로드한다.
리소스는 Editor/Resources/Xeri/Collection에 두고 Unity의 에셋 수명주기에 맡긴다.
UXML과 USS는 Resources 경로가 겹치지 않도록 별도 이름으로 구분한다.
표가 많은 인스펙터에서 같은 리소스를 반복 조회하지 않도록 로드 결과를 보관하고, 에셋이 해제되면 다시 로드한다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UIElements;

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

        private const string resourceRoot = "Xeri/Collection/";
        private static readonly Dictionary<string, UnityEngine.Object> loaded = new();

    #endregion

    #region 리소스 로드

        // ------------------------------------------------------------
        /// <summary>
        /// 컬렉션 표의 UXML을 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public static VisualTreeAsset LoadTableLayout()
        {
            return LoadRequired<VisualTreeAsset>("CollectionTable");
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 컬렉션 표의 USS를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public static StyleSheet LoadTableStyle()
        {
            return LoadRequired<StyleSheet>("CollectionTableStyle");
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 상세 편집창의 UXML을 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public static VisualTreeAsset LoadEditWindowLayout()
        {
            return LoadRequired<VisualTreeAsset>("CollectionEditWindow");
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 상세 편집창의 USS를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public static StyleSheet LoadEditWindowStyle()
        {
            return LoadRequired<StyleSheet>("CollectionEditWindowStyle");
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 보조 입력창의 USS를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public static StyleSheet LoadUtilityWindowStyle()
        {
            return LoadRequired<StyleSheet>("CollectionUtilityWindow");
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 필수 리소스가 없으면 경로와 함께 오류를 보고한다.
        /// </summary>
        // ------------------------------------------------------------
        private static T LoadRequired<T>(string name)
        where T : UnityEngine.Object
        {
            var path = resourceRoot + name;

            // 재가져오기로 에셋이 해제되면 Unity null이 되므로 그때만 다시 로드한다.
            if (loaded.TryGetValue(path, out var cached) && cached is T cachedAsset && cachedAsset != null)
            {
                return cachedAsset;
            }

            var asset = Resources.Load<T>(path);
            if (asset == null)
            {
                throw new InvalidOperationException($"Collection Editor 리소스를 찾을 수 없습니다: {path}");
            }

            loaded[path] = asset;
            return asset;
        }

    #endregion

    }
}
