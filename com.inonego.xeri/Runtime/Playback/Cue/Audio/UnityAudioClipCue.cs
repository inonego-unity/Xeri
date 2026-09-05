/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : UnityAudioClipCue.cs
수정일 : 2026-09-05

# 설명
UnityAudioClipCueAsset의 Audio Clip Variant를 구체 재생 리소스로 해석하는 runtime Cue를 제공한다.
Variant 선택 상태와 정책은 VariantCue 공통 기반이 소유하며 단일 Variant Cue도 같은 재생 경계를 사용한다.
========================================================================= BLOCK_HEADER_END */

using System;

namespace inonego.Xeri.Playback
{
    // ============================================================
    /// <summary>
    /// Unity AudioClip 기반 runtime Audio Cue.
    /// </summary>
    // ============================================================
    public sealed class UnityAudioClipCue : AudioCue
    {

    #region 필드

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> 이 runtime Cue를 생성한 authoring Asset.
        /// <br/> 단일 Variant 직접 구성에서는 null이다.
        /// </summary>
        // ----------------------------------------------------------------------
        public UnityAudioClipCueAsset Asset => asset;

        private readonly UnityAudioClipCueAsset asset = null;
        private readonly UnityAudioClipCueVariant standaloneVariant = null;

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// 프로그램에서 직접 구성한 단일 Variant runtime Cue를 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        public UnityAudioClipCue(UnityAudioClipCueVariant variant)
            : base(excludePrevious: false)
        {
            standaloneVariant = variant ?? throw new ArgumentNullException(nameof(variant));
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Asset의 Variant와 선택 정책을 사용하는 runtime Cue를 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        internal UnityAudioClipCue(UnityAudioClipCueAsset asset)
            : base(asset != null ? asset.ExcludePrevious : false)
        {
            this.asset = asset ?? throw new ArgumentNullException(nameof(asset));
        }

    #endregion

    #region Variant 선택

        // ------------------------------------------------------------
        /// <summary>
        /// 이번 재생에 사용할 Variant 하나를 선택한다.
        /// </summary>
        // ------------------------------------------------------------
        internal UnityAudioClipCueVariant SelectVariant()
        {
            // 직접 구성된 Cue는 선택 대상이 하나뿐이므로 그대로 반환한다.
            if (asset == null)
            {
                return standaloneVariant;
            }

            // Asset 기반 Cue는 최소 한 개 Variant를 authoring해야 선택을 진행할 수 있다.
            if (asset.VariantCount <= 0)
            {
                throw new InvalidOperationException
                (
                    $"Unity Audio Clip Cue Asset '{asset.name}'에 하나 이상의 Variant가 필요합니다."
                );
            }

            var index = SelectVariantIndex(asset.VariantCount);
            return asset.GetVariant(index) ?? throw new InvalidOperationException
            (
                $"Unity Audio Clip Cue Asset '{asset.name}'의 Variant {index}가 비어 있습니다."
            );
        }

    #endregion

    }
}
