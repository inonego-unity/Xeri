/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : GameUIBootstrapperModuleAsset.cs
수정일 : 2026-07-31

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
        /// <br/> Host Prefab과 Settings 참조를 확인하고 새 Host를 생성해 Runtime을 초기화한다.
        /// <br/> 실패 시 기존 Host를 건드리지 않고 이번에 생성한 Host만 제거한다.
        /// </summary>
        // ----------------------------------------------------------------------
        public override async Awaitable Init()
        {
            if (hostPrefab == null)
            {
                throw new InvalidOperationException("Game UI Host Prefab이 설정되지 않았습니다.");
            }

            if (settings == null)
            {
                throw new InvalidOperationException("Game UI Settings Asset이 설정되지 않았습니다.");
            }

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

                // 실제 인스턴스 구성과 Settings 계약은 Runtime 초기화 경계에서 한 번 검증한다.
                runtime.Initialize(settings);
            }
            catch
            {
                // 초기화에 실패한 Host가 Scene의 Runtime·EventSystem 구성을 막지 않도록 제거한다.
                instance.SetActive(false);
                Destroy(instance);
                throw;
            }
        }

    #endregion

    }
}
