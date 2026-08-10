/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : AudioBootstrapperModuleAsset.cs
수정일 : 2026-08-01

# 설명
기존 Xeri Bootstrapper에서 App 단위 Audio Host Prefab을 생성한다.
========================================================================= BLOCK_HEADER_END */

using System;

using UnityEngine;

using inonego.Xeri.Bootstrapper;

namespace inonego.Xeri.Playback
{
    // ============================================================
    /// <summary>
    /// AudioManager가 구성된 Audio Host를 생성하는 Bootstrapper Module Asset.
    /// </summary>
    // ============================================================
    [CreateAssetMenu
    (
        fileName = "Audio Bootstrapper Module",
        menuName = "Xeri/Bootstrapper/Audio Module"
    )]
    public sealed class AudioBootstrapperModuleAsset : BootstrapperModuleAsset
    {

    #region 필드

        [SerializeField]
        private GameObject hostPrefab = null;

    #endregion

    #region BootstrapperModuleAsset

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> AudioManager가 구성된 Host Prefab을 확인하고 새 Host를 생성한다.
        /// <br/> AudioManager와 UnityAudioCuePlayer는 자신의 Awake에서 등록과 Pool 초기화를 수행한다.
        /// </summary>
        // ----------------------------------------------------------------------
        public override async Awaitable Init()
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

            Instantiate(hostPrefab);
        }

    #endregion

    }
}
