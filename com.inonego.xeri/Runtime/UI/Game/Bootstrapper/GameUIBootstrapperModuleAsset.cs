/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : GameUIBootstrapperModuleAsset.cs
수정일 : 2026-08-24

# 설명
Xeri Bootstrapper에서 현재 Render Pipeline 출력 Adapter와 App 단위 Game UI Host를 조립하고 초기화한다.
========================================================================= BLOCK_HEADER_END */

using System;

using UnityEngine;

using inonego;
using inonego.Xeri;
using inonego.Xeri.Bootstrapper;

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

    #region Game UI 초기화

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

                // 기존 Xeri Bootstrapper 단계에서 현재 Render Pipeline 출력 Adapter를 자동 획득한다.
                var renderPipelineAdapter = GameUIRenderPipelineAdapterRegistry.Acquire();

                // Runtime이 Adapter 소유권을 넘겨받아 초기화 실패와 정상 종료에서 같은 경로로 정리한다.
                runtime.Initialize(settings, renderPipelineAdapter);
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
