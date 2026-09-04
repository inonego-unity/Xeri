/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : AudioCue.cs
수정일 : 2026-09-04

# 설명
오디오 Cue가 공통으로 사용하는 runtime 재생 설정을 정의한다.

# 적용 범위
시작 시간과 런타임 배치는 상위 호출자가 소유하며 Cue는 기본 Bus와 재생 설정을 제공한다.
========================================================================= BLOCK_HEADER_END */

using System;

using UnityEngine;

using inonego;
using inonego.Xeri;
using inonego.Xeri.Primitive;

namespace inonego.Xeri.Playback
{
    // ============================================================
    /// <summary>
    /// 오디오 Cue의 공통 재생 설정.
    /// </summary>
    // ============================================================
    [Serializable]
    public abstract class AudioCue : IPlaybackCue
    {

    #region 필드

        // ------------------------------------------------------------
        /// <summary>
        /// Cue가 사용하는 기본 Audio Bus.
        /// </summary>
        // ------------------------------------------------------------
        public AudioBus Bus
        {
            get => bus;
            set
            {
                if (!Enum.IsDefined(typeof(AudioBus), value))
                {
                    throw new ArgumentOutOfRangeException(nameof(value), "유효한 Audio Bus가 아닙니다.");
                }

                bus = value;
            }
        }

        [SerializeField]
        private AudioBus bus = AudioBus.SFX;

        // ------------------------------------------------------------
        /// <summary>
        /// Cue의 기본 볼륨.
        /// </summary>
        // ------------------------------------------------------------
        public float Volume
        {
            get => volume;
            set
            {
                if (!value.IsFinite() || value < 0.0f || value > 1.0f)
                {
                    throw new ArgumentOutOfRangeException
                    (
                        nameof(value),
                        "Volume은 0 이상 1 이하의 유한한 값이어야 합니다."
                    );
                }

                volume = value;
            }
        }

        [SerializeField]
        [Range(0.0f, 1.0f)]
        private float volume = 1.0f;

        // ------------------------------------------------------------
        /// <summary>
        /// Cue의 기본 재생 Pitch. Unity AudioClip 범위인 -3 이상 3 이하를 사용한다.
        /// </summary>
        // ------------------------------------------------------------
        public float Pitch
        {
            get => pitch;
            set
            {
                if (!value.IsFinite() || value < -3.0f || value > 3.0f)
                {
                    throw new ArgumentOutOfRangeException
                    (
                        nameof(value),
                        "Pitch는 -3 이상 3 이하인 유한한 값이어야 합니다."
                    );
                }

                pitch = value;
            }
        }

        [SerializeField]
        [Range(-3.0f, 3.0f)]
        private float pitch = 1.0f;

        // ------------------------------------------------------------
        /// <summary>
        /// 한 Playback 안에서 Clip을 반복할지 여부.
        /// </summary>
        // ------------------------------------------------------------
        public bool IsLooping
        {
            get => isLooping;
            set => isLooping = value;
        }

        [SerializeField]
        private bool isLooping = false;

        // ------------------------------------------------------------
        /// <summary>
        /// 공간 재생에서 사용하는 2D와 3D의 혼합 비율.
        /// </summary>
        // ------------------------------------------------------------
        public float SpatialBlend
        {
            get => spatialBlend;
            set
            {
                if (!value.IsFinite() || value < 0.0f || value > 1.0f)
                {
                    throw new ArgumentOutOfRangeException
                    (
                        nameof(value),
                        "SpatialBlend는 0 이상 1 이하의 유한한 값이어야 합니다."
                    );
                }

                spatialBlend = value;
            }
        }

        [SerializeField]
        [Range(0.0f, 1.0f)]
        private float spatialBlend = 1.0f;

        // ------------------------------------------------------------
        /// <summary>
        /// 공간 재생의 거리 감쇠 방식.
        /// </summary>
        // ------------------------------------------------------------
        public AudioRolloffMode RolloffMode
        {
            get => rolloffMode;
            set
            {
                if (!Enum.IsDefined(typeof(AudioRolloffMode), value))
                {
                    throw new ArgumentOutOfRangeException(nameof(value), "유효한 Audio Rolloff Mode가 아닙니다.");
                }

                rolloffMode = value;
            }
        }

        [SerializeField]
        private AudioRolloffMode rolloffMode = AudioRolloffMode.Logarithmic;

        // ------------------------------------------------------------
        /// <summary>
        /// 공간 재생에서 최대 음량이 유지되는 거리.
        /// </summary>
        // ------------------------------------------------------------
        public float MinDistance
        {
            get => minDistance;
            set
            {
                if
                (
                    !value.IsFinite() ||
                    value <= 0.0f ||
                    value > maxDistance
                )
                {
                    throw new ArgumentOutOfRangeException
                    (
                        nameof(value),
                        "MinDistance는 0보다 크고 MaxDistance 이하여야 합니다."
                    );
                }

                minDistance = value;
            }
        }

        [SerializeField]
        [Min(0.0001f)]
        private float minDistance = 1.0f;

        // ------------------------------------------------------------
        /// <summary>
        /// 공간 재생의 거리 감쇠가 적용되는 최대 거리.
        /// </summary>
        // ------------------------------------------------------------
        public float MaxDistance
        {
            get => maxDistance;
            set
            {
                if
                (
                    !value.IsFinite() ||
                    value < minDistance
                )
                {
                    throw new ArgumentOutOfRangeException
                    (
                        nameof(value),
                        "MaxDistance는 MinDistance 이상의 유한한 값이어야 합니다."
                    );
                }

                maxDistance = value;
            }
        }

        [SerializeField]
        [Min(0.0001f)]
        private float maxDistance = 500.0f;

    #endregion

    }
}
