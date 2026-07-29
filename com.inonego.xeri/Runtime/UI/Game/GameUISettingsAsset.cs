/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : GameUISettingsAsset.cs
수정일 : 2026-07-29

# 설명
Game UI Runtime backend, 기본 Profile, Scene Fade와 Input System의 순수 설정을 정의한다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections.Generic;

using UnityEngine;

namespace inonego.Xeri.UI.Game
{
    // ============================================================
    /// <summary>
    /// Game UI Runtime 조립 설정 Asset.
    /// </summary>
    // ============================================================
    [CreateAssetMenu
    (
        fileName = "Game UI Settings",
        menuName = "Xeri/UI/Game/Settings"
    )]
    public sealed class GameUISettingsAsset : ScriptableObject
    {
    #region 필드

        // ------------------------------------------------------------
        /// <summary>
        /// App 수명 기본 Layer Profile.
        /// </summary>
        // ------------------------------------------------------------
        public GameUIProfileAsset DefaultProfile => defaultProfile;

        [SerializeField]
        private GameUIProfileAsset defaultProfile = null;

        // ------------------------------------------------------------
        /// <summary>
        /// Scene Fade Overlay를 표시할 기본 Profile Layer ID.
        /// </summary>
        // ------------------------------------------------------------
        public string SceneFadeLayerID => sceneFadeLayerID;

        [SerializeField]
        private string sceneFadeLayerID = "";

        // ------------------------------------------------------------
        /// <summary>
        /// Scene Fade View를 공급하는 Provider.
        /// </summary>
        // ------------------------------------------------------------
        public IGameObjectProvider SceneFadeViewProvider => sceneFadeViewProvider;

        [SerializeReference]
        private IGameObjectProvider sceneFadeViewProvider = new PrefabGameObjectProvider();

        // ------------------------------------------------------------
        /// <summary>
        /// 기본 Scene Fade 색상.
        /// </summary>
        // ------------------------------------------------------------
        public Color DefaultFadeColor => defaultFadeColor;

        [SerializeField]
        private Color defaultFadeColor = Color.black;

        // ------------------------------------------------------------
        /// <summary>
        /// 기본 Scene Fade 시간.
        /// </summary>
        // ------------------------------------------------------------
        public float DefaultFadeDuration => defaultFadeDuration;

        [SerializeField]
        [Min(0.0f)]
        private float defaultFadeDuration = 0.25f;

        // ------------------------------------------------------------
        /// <summary>
        /// Input System UI Action Map 이름.
        /// </summary>
        // ------------------------------------------------------------
        public string UIActionMap => uiActionMap;

        [SerializeField]
        private string uiActionMap = "UI";

        // ------------------------------------------------------------
        /// <summary>
        /// Input System Gameplay Action Map 이름.
        /// </summary>
        // ------------------------------------------------------------
        public string GameplayActionMap => gameplayActionMap;

        [SerializeField]
        private string gameplayActionMap = "Player";

        // ------------------------------------------------------------
        /// <summary>
        /// Screen 종료 뒤 해제를 기다릴 UI Action 이름.
        /// </summary>
        // ------------------------------------------------------------
        public IReadOnlyList<string> ReleaseActionNames => releaseActionNames;

        [SerializeField]
        private string[] releaseActionNames = { "Cancel", "Submit", "Pause" };

    #endregion

    #region 메서드

        // ------------------------------------------------------------
        /// <summary>
        /// Runtime 조립에 필요한 순수 설정을 검증한다.
        /// </summary>
        // ------------------------------------------------------------
        internal void Validate()
        {
            if (defaultProfile == null)
            {
                throw new InvalidOperationException("Game UI 기본 Profile이 설정되지 않았습니다.");
            }

            if (string.IsNullOrWhiteSpace(sceneFadeLayerID))
            {
                throw new InvalidOperationException("Scene Fade Layer ID가 비어 있습니다.");
            }

            if (sceneFadeViewProvider == null)
            {
                throw new InvalidOperationException("Scene Fade View Provider가 설정되지 않았습니다.");
            }

            if (defaultFadeDuration < 0.0f)
            {
                throw new InvalidOperationException("Scene Fade 시간은 0 이상이어야 합니다.");
            }

            if (string.IsNullOrWhiteSpace(uiActionMap))
            {
                throw new InvalidOperationException("UI Action Map 이름이 비어 있습니다.");
            }

            if (string.IsNullOrWhiteSpace(gameplayActionMap))
            {
                throw new InvalidOperationException("Gameplay Action Map 이름이 비어 있습니다.");
            }

            if (string.Equals(uiActionMap, gameplayActionMap, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("UI와 Gameplay Action Map 이름은 서로 달라야 합니다.");
            }

            if (releaseActionNames == null || releaseActionNames.Length == 0)
            {
                throw new InvalidOperationException("입력 해제 Action 이름이 하나 이상 필요합니다.");
            }

            for (var i = 0; i < releaseActionNames.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(releaseActionNames[i]))
                {
                    throw new InvalidOperationException
                    (
                        $"입력 해제 Action 이름 {i}가 비어 있습니다."
                    );
                }
            }
        }

    #endregion

    }
}
