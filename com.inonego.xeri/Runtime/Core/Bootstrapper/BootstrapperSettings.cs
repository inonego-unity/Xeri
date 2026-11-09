/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : BootstrapperSettings.cs
수정일 : 2026-11-03

# 설명
BootStrapper 실행에 필요한 씬 인덱스와 초기화 모듈 에셋 목록을 저장하는 설정 에셋.
설정 조회는 에셋을 생성하지 않으며, 에디터 설정 UI에서 명시적으로 생성한다.
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
    using inonego;
    using inonego.Xeri;
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
                    instance = Load();
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
        [SerializeField]
        [HelpBox("BootStrapper가 먼저 로드할 씬의 Build Settings 인덱스입니다.")]
        private XNullable<int> bootstrapperSceneIndex = default;

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
        [SerializeField]
        [HelpBox("BootStrapper 초기화가 끝난 뒤 로드할 실제 시작 씬의 Build Settings 인덱스입니다.")]
        private XNullable<int> sceneIndexToLoad = default;

        // ------------------------------------------------------------
        /// <summary>
        /// 실행할 초기화 모듈 목록.
        /// </summary>
        // ------------------------------------------------------------
        public IReadOnlyList<BootstrapperModuleAsset> Modules => modules;
        [SerializeField]
        [HelpBox("BootStrapper가 순서대로 실행할 초기화 모듈 에셋 목록입니다.")]
        private List<BootstrapperModuleAsset> modules = new();

    #endregion

    #region 설정 에셋 접근

        // ------------------------------------------------------------
        /// <summary>
        /// 기존 Resources 설정 에셋만 조회한다.
        /// </summary>
        // ------------------------------------------------------------
        private static BootstrapperSettings Load()
        {
            return Resources.Load<BootstrapperSettings>(ResourcesFileName);
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
