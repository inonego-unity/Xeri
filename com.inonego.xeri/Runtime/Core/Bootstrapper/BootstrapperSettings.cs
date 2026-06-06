/* BLOCK_HEADER_BEGIN =======================================================================
파일명: BootstrapperSettings.cs
수정일: 2026-05-20

# 설명
BootStrapper 실행에 필요한 씬 인덱스와 초기화 모듈 에셋 목록을 저장하는 설정 에셋.
에디터에서는 Assets/Resources/BootstrapperSettings.asset 이 없으면 자동 생성한다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace inonego.Xeri.Bootstrapper
{
    using inonego.Xeri.Serializable;

    // ============================================================
    /// <summary>
    /// BootStrapper 실행 설정.
    /// </summary>
    // ============================================================
    public class BootstrapperSettings : ScriptableObject
    {

    #region 상수

        public const string ResourcesDirectory = "Assets/Resources";
        public const string ResourcesFileName = "BootstrapperSettings";
        public const string ResourcesAssetPath = ResourcesDirectory + "/" + ResourcesFileName + ".asset";

    #endregion

    #region 필드

        private static BootstrapperSettings instance;

        [SerializeField]
        [HelpBox("BootStrapper가 먼저 로드할 씬의 Build Settings 인덱스입니다.")]
        private XNullable<int> bootstrapperSceneIndex = default;

        [SerializeField]
        [HelpBox("BootStrapper 초기화가 끝난 뒤 로드할 실제 시작 씬의 Build Settings 인덱스입니다.")]
        private XNullable<int> sceneIndexToLoad = default;

        [SerializeField]
        [HelpBox("BootStrapper가 순서대로 실행할 초기화 모듈 에셋 목록입니다.")]
        private List<BootstrapperModuleAsset> modules = new();

    #endregion

    #region 프로퍼티

        // ------------------------------------------------------------
        /// <summary>
        /// BootStrapper 설정 인스턴스.
        /// </summary>
        // ------------------------------------------------------------
        public static BootstrapperSettings Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = LoadOrCreate();
                }

                return instance;
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// BootStrapper 씬 인덱스.
        /// </summary>
        // ------------------------------------------------------------
        public XNullable<int> BootstrapperSceneIndex
        {
            get => bootstrapperSceneIndex;
            set => bootstrapperSceneIndex = value;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 초기화 후 로드할 시작 씬 인덱스.
        /// </summary>
        // ------------------------------------------------------------
        public XNullable<int> SceneIndexToLoad
        {
            get => sceneIndexToLoad;
            set => sceneIndexToLoad = value;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 실행할 초기화 모듈 목록.
        /// </summary>
        // ------------------------------------------------------------
        public IReadOnlyList<BootstrapperModuleAsset> Modules => modules;

    #endregion

    #region 메서드

        // ------------------------------------------------------------
        /// <summary>
        /// Resources에서 설정을 로드하고, 에디터에서는 없을 때 에셋을 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        private static BootstrapperSettings LoadOrCreate()
        {
            var settings = Resources.Load<BootstrapperSettings>(ResourcesFileName);

        #if UNITY_EDITOR
            if (settings == null)
            {
                settings = CreateAsset();
            }
        #endif

            return settings;
        }

    #if UNITY_EDITOR

        // ------------------------------------------------------------
        /// <summary>
        /// BootStrapper 설정 에셋을 로드하거나 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        public static BootstrapperSettings CreateAsset()
        {
            var settings = AssetDatabase.LoadAssetAtPath<BootstrapperSettings>(ResourcesAssetPath);
            if (settings != null) return settings;

            MakeDirectory(ResourcesDirectory);

            settings = CreateInstance<BootstrapperSettings>();

            AssetDatabase.CreateAsset(settings, ResourcesAssetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            return settings;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// AssetDatabase 폴더를 재귀적으로 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        private static void MakeDirectory(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;

            var parent = Path.GetDirectoryName(path)?.Replace("\\", "/");
            var current = Path.GetFileName(path);

            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
            {
                MakeDirectory(parent);
            }

            AssetDatabase.CreateFolder(parent, current);
        }

    #endif

    #endregion

    }
}
