/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : AudioBootstrapperModuleAsset.cs
수정일 : 2026-11-03

# 설명
Initial Scene 이전에 Audio Host Prefab을 생성하고 ApplicationScope가 종료 책임을 소유한다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Threading;
using System.Threading.Tasks;

using UnityEngine;

using inonego;
using inonego.Xeri;
using inonego.Xeri.Application;
using inonego.Xeri.Bootstrapper;

namespace inonego.Xeri.Playback
{
    // ======================================================================
    /// <summary>
    /// AudioManager가 구성된 Audio Host를 생성하는 Bootstrapper Module Asset.
    /// </summary>
    // ======================================================================
    [CreateAssetMenu
    (
        fileName = "Audio Bootstrapper Module",
        menuName = "Xeri/Bootstrapper/Audio Module"
    )]
    public sealed class AudioBootstrapperModuleAsset : BootstrapperModuleAsset
    {

    #region 실행 단계

        // ------------------------------------------------------------
        /// <summary>
        /// Audio Host가 생성될 Initial Scene 이전 phase.
        /// </summary>
        // ------------------------------------------------------------
        public override BootstrapperModulePhase Phase => BootstrapperModulePhase.BeforeInitialScene;

    #endregion

    #region 필드

        [SerializeField]
        private GameObject hostPrefab = null;

    #endregion

    #region Audio Host 초기화

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> AudioManager가 있는 Host Prefab을 확인하고 생성한다.
        /// <br/> 생성한 Host의 종료 책임을 ApplicationScope에 등록한다.
        /// </summary>
        // ------------------------------------------------------------
        public override Task StartAsync(ApplicationContext context, CancellationToken cancellationToken)
        {
            if (hostPrefab == null)
            {
                throw new InvalidOperationException("Audio Host Prefab이 설정되지 않았습니다.");
            }

            if (!hostPrefab.TryGetComponent<AudioManager>(out _))
            {
                throw new InvalidOperationException
                (
                    "Audio Host Prefab Root에 AudioManager가 없습니다."
                );
            }

            cancellationToken.ThrowIfCancellationRequested();

            var host = Instantiate(hostPrefab);
            var manager = host.GetComponent<AudioManager>();
            var ownership = new Lease(() => ReleaseHost(host, manager));

            try
            {
                context.Scope.Own(ownership);
            }
            catch
            {
                ownership.Dispose();
                throw;
            }

            // Awake의 슬롯 충돌로 Host가 등록되지 않은 경우 시작을 실패 처리한다.
            if (manager == null || !manager.IsRegistered)
            {
                throw new InvalidOperationException("Audio Host가 Singleton 슬롯에 등록되지 않았습니다.");
            }

            return Task.CompletedTask;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Singleton 슬롯을 먼저 해제한 뒤 Audio Host 파괴를 예약한다.
        /// </summary>
        // ------------------------------------------------------------
        private static void ReleaseHost(GameObject host, AudioManager manager)
        {
            try
            {
                if (manager != null)
                {
                    AudioManager.Unregister(manager);
                }
            }
            finally
            {
                Destroy(host);
            }
        }

    #endregion

    }
}
