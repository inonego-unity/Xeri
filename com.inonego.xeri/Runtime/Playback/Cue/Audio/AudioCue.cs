/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : AudioCue.cs
수정일 : 2026-07-31

# 설명
오디오 Cue가 공통으로 사용하는 재생 설정을 정의한다.

# 적용 범위
시작 시간, 배치, 반복 실행과 Bus 정책은 상위 호출자 또는 후속 Audio System이 소유한다.
========================================================================= BLOCK_HEADER_END */

using System;

using UnityEngine;

namespace inonego.Xeri.Playback
{
    // ============================================================
    /// <summary>
    /// 오디오 Cue의 공통 재생 설정.
    /// </summary>
    // ============================================================
    public abstract class AudioCue : ScriptableObject, IPlaybackCue
    {
    #region 필드

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
                if (float.IsNaN(value) || float.IsInfinity(value) || value < 0.0f || value > 1.0f)
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
                if (float.IsNaN(value) || float.IsInfinity(value) || value < -3.0f || value > 3.0f)
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

    #endregion

    }
}
