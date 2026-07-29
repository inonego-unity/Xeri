/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : GameUIBootstrapperModuleAsset.cs
수정일 : 2026-07-29

# 설명
기존 Xeri Bootstrapper에서 App 단위 Game UI Host Prefab을 직접 생성하고 초기화한다.
========================================================================= BLOCK_HEADER_END */

using System;

using inonego.Xeri.Bootstrapper;

using UnityEngine;

namespace inonego.Xeri.UI.Game
{
    // ============================================================
    /// <summary>
    /// Game UI Runtime Host를 생성하는 Bootstrapper Module Asset.
    /// </summary>
    // ============================================================
    [CreateAssetMenu
    (
        fileName = "Game UI Bootstrapper Module",
        menuName = "Xeri/Bootstrapper/Game UI Module"
    )]
    public sealed class GameUIBootstrapperModuleAsset : BootstrapperModuleAsset
    {
    #region 필드

        [SerializeField]
        private GameObject hostPrefab = null;

        [SerializeField]
        private GameUISettingsAsset settings = null;

    #endregion

    #region BootstrapperModuleAsset

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> Host Prefab과 Settings를 검증하고 새 Host만 직접 생성해 Runtime을 초기화한다.
        /// <br/> 실패 시 기존 Host를 건드리지 않고 이번에 생성한 Host만 제거한다.
        /// </summary>
        // ----------------------------------------------------------------------
        public override async Awaitable Init()
        {
            Validate();

            var instance = Instantiate(hostPrefab);

            try
            {
                var runtime = instance.GetComponent<GameUIRuntime>();

                if (runtime == null)
                {
                    throw new InvalidOperationException
                    (
                        "생성한 Game UI Host Prefab Root에 GameUIRuntime이 없습니다."
                    );
                }

                if (!instance.activeInHierarchy)
                {
                    throw new InvalidOperationException("생성한 Game UI Host Root가 활성 상태가 아닙니다.");
                }

                if (!runtime.enabled)
                {
                    throw new InvalidOperationException("생성한 GameUIRuntime이 비활성 상태입니다.");
                }

                runtime.Initialize(settings);
            }
            catch
            {
                instance.SetActive(false);
                Destroy(instance);
                throw;
            }
        }

    #endregion

    #region 검증

        // ------------------------------------------------------------
        /// <summary>
        /// 생성 전 Host Prefab Root와 Settings 참조를 검증한다.
        /// </summary>
        // ------------------------------------------------------------
        private void Validate()
        {
            if (hostPrefab == null)
            {
                throw new InvalidOperationException("Game UI Host Prefab이 설정되지 않았습니다.");
            }

            if (settings == null)
            {
                throw new InvalidOperationException("Game UI Settings Asset이 설정되지 않았습니다.");
            }

            settings.Validate();

            if (!hostPrefab.activeSelf)
            {
                throw new InvalidOperationException("Game UI Host Prefab Root는 활성 상태여야 합니다.");
            }

            var runtime = hostPrefab.GetComponent<GameUIRuntime>();

            if (runtime == null)
            {
                throw new InvalidOperationException
                (
                    "Game UI Host Prefab Root에 GameUIRuntime이 필요합니다."
                );
            }

            if (!runtime.enabled)
            {
                throw new InvalidOperationException("Game UI Host Prefab의 GameUIRuntime이 비활성 상태입니다.");
            }
        }

    #endregion

    }
}
