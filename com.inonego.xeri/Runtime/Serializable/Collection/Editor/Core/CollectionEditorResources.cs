/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : CollectionEditorResources.cs
수정일 : 2026-10-31

# 설명
Collection Editor의 View와 Window가 공유하는 UXML·USS를 Resources에서 로드한다.
리소스는 Editor/Resources/XeriUI/Collection에 두고 Unity의 에셋 수명주기에 맡긴다.
UXML과 USS는 Resources 경로가 겹치지 않도록 별도 이름으로 구분한다.
========================================================================= BLOCK_HEADER_END */

using System;

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

        private const string resourceRoot = "XeriUI/Collection/";

    #endregion

    #region 리소스 로드

        // ------------------------------------------------------------
        /// <summary>
        /// Collection Editor의 UXML을 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public static VisualTreeAsset LoadViewLayout()
        {
            return LoadRequired<VisualTreeAsset>("CollectionEditorView");
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Collection Editor의 USS를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public static StyleSheet LoadViewStyle()
        {
            return LoadRequired<StyleSheet>("CollectionEditorViewStyle");
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
            var asset = Resources.Load<T>(path);
            if (asset == null)
            {
                throw new InvalidOperationException($"Collection Editor 리소스를 찾을 수 없습니다: {path}");
            }

            return asset;
        }

    #endregion

    }
}
