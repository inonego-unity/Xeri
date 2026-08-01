/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : UnityAudioCuePlayer.cs
수정일 : 2026-08-01

# 설명
UnityAudioClipCue를 Pool에서 획득한 AudioSource voice로 실행하고 Playback을 갱신한다.

# 적용 범위
2D, 고정 위치 3D와 emitter 추적 3D 배치를 지원한다.
Bus 출력 정책은 AudioManager가 계산하고 Player는 전달받은 초기 출력 설정을 voice에 적용한다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Audio;

using inonego.Xeri.Pool;

namespace inonego.Xeri.Playback
{
    // ============================================================
    /// <summary>
    /// Unity AudioClip Cue Player.
    /// </summary>
    // ============================================================
    public sealed class UnityAudioCuePlayer : MonoBehaviour, ICuePlayer
    {
    #region 필드

        [SerializeField]
        private AudioSource sourcePrefab = null;

        [SerializeField]
        [Min(0)]
        private int initialVoiceCount = 0;

        private GOCompPool<AudioSource> sourcePool = null;
        private readonly List<UnityAudioPlayback> playbacks = new();

    #endregion

    #region 메서드

        // ------------------------------------------------------------
        /// <summary>
        /// UnityAudioClipCue를 처리할 수 있는지 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public bool CanPlay(IPlaybackCue cue)
        {
            return cue is UnityAudioClipCue;
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> UnityAudioClipCue를 2D voice로 실행한다.
        /// <br/> 생성한 Playback이 voice Lease의 반환을 소유한다.
        /// </summary>
        // ----------------------------------------------------------------------
        public ICuePlayback Play(IPlaybackCue cue)
        {
            if (cue == null)
            {
                throw new ArgumentNullException(nameof(cue));
            }

            if (cue is not UnityAudioClipCue audioCue)
            {
                throw new ArgumentException
                (
                    "UnityAudioCuePlayer는 UnityAudioClipCue만 재생할 수 있습니다.",
                    nameof(cue)
                );
            }

            return Play(audioCue);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Audio Cue를 2D voice로 실행한다.
        /// </summary>
        // ------------------------------------------------------------
        public IAudioPlayback Play(AudioCue cue)
        {
            return PlayInternal
            (
                cue,
                isSpatial: false,
                Vector3.zero,
                emitter: null,
                output: null,
                cue?.Volume ?? 0.0f,
                outputVolume: 1.0f
            );
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Audio Cue를 지정 월드 위치의 3D voice로 실행한다.
        /// </summary>
        // ------------------------------------------------------------
        public IAudioPlayback Play(AudioCue cue, Vector3 position)
        {
            return PlayInternal
            (
                cue,
                isSpatial: true,
                position,
                emitter: null,
                output: null,
                cue?.Volume ?? 0.0f,
                outputVolume: 1.0f
            );
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Audio Cue를 emitter의 월드 위치를 따라가는 3D voice로 실행한다.
        /// </summary>
        // ------------------------------------------------------------
        public IAudioPlayback Play(AudioCue cue, Transform emitter)
        {
            if (emitter == null)
            {
                throw new ArgumentNullException(nameof(emitter));
            }

            return PlayInternal
            (
                cue,
                isSpatial: true,
                emitter.position,
                emitter,
                output: null,
                cue?.Volume ?? 0.0f,
                outputVolume: 1.0f
            );
        }

        // --------------------------------------------------------------------------------
        /// <summary>
        /// AudioManager가 계산한 초기 볼륨과 출력 Group으로 Audio Cue를 2D 실행한다.
        /// </summary>
        // --------------------------------------------------------------------------------
        internal UnityAudioPlayback Play
        (
            AudioCue cue,
            float volume,
            AudioMixerGroup output,
            float outputVolume
        )
        {
            return PlayInternal
            (
                cue,
                isSpatial: false,
                Vector3.zero,
                emitter: null,
                output,
                volume,
                outputVolume
            );
        }

        // --------------------------------------------------------------------------------
        /// <summary>
        /// AudioManager가 계산한 초기 설정으로 Audio Cue를 지정 월드 위치에서 실행한다.
        /// </summary>
        // --------------------------------------------------------------------------------
        internal UnityAudioPlayback Play
        (
            AudioCue cue,
            Vector3 position,
            float volume,
            AudioMixerGroup output,
            float outputVolume
        )
        {
            return PlayInternal
            (
                cue,
                isSpatial: true,
                position,
                emitter: null,
                output,
                volume,
                outputVolume
            );
        }

        // --------------------------------------------------------------------------------
        /// <summary>
        /// AudioManager가 계산한 초기 설정으로 Audio Cue를 emitter 위치에서 실행한다.
        /// </summary>
        // --------------------------------------------------------------------------------
        internal UnityAudioPlayback Play
        (
            AudioCue cue,
            Transform emitter,
            float volume,
            AudioMixerGroup output,
            float outputVolume
        )
        {
            if (emitter == null)
            {
                throw new ArgumentNullException(nameof(emitter));
            }

            return PlayInternal
            (
                cue,
                isSpatial: true,
                emitter.position,
                emitter,
                output,
                volume,
                outputVolume
            );
        }

        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// <br/> 비활성 Host를 코드로 조립할 때 직렬화 입력과 같은 source 설정을 지정한다.
        /// <br/> 설정을 완료한 Host가 활성화되면 Awake에서 Pool을 초기화한다.
        /// </summary>
        // ----------------------------------------------------------------------------------------------------
        internal void SetSourceConfiguration(AudioSource sourcePrefab, int initialVoiceCount)
        {
            this.sourcePrefab = sourcePrefab;
            this.initialVoiceCount = initialVoiceCount;
        }

        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// <br/> source prefab을 사용하는 AudioSource Pool을 구성하고 선택한 초기 voice를 준비한다.
        /// <br/> 초기 voice는 직접 획득한 뒤 ReleaseAll로 한 번에 대기 상태로 전환한다.
        /// </summary>
        // ----------------------------------------------------------------------------------------------------
        private void InitializeSourcePool()
        {
            if (sourcePrefab == null)
            {
                throw new InvalidOperationException("UnityAudioCuePlayer에 AudioSource Prefab이 설정되지 않았습니다.");
            }

            if (initialVoiceCount < 0)
            {
                throw new InvalidOperationException("Initial Voice Count는 0 이상이어야 합니다.");
            }

            if (sourcePrefab.playOnAwake)
            {
                throw new InvalidOperationException("AudioSource Prefab의 Play On Awake는 비활성화되어야 합니다.");
            }

            var provider = new PrefabGameObjectProvider
            (
                sourcePrefab.gameObject,
                transform
            );

            sourcePool = new GOCompPool<AudioSource>(provider)
            {
                Pool = transform,
            };

            // 초기 동시 재생분을 먼저 획득하여 이후 첫 재생의 Instantiate 집중을 줄인다.
            for (var i = 0; i < initialVoiceCount; i++)
            {
                sourcePool.Acquire();
            }

            sourcePool.ReleaseAll();
        }

        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// <br/> Pool에서 voice Lease를 획득하고 이번 재생의 모든 AudioSource 설정을 적용한다.
        /// <br/> Playback 공개 전 실패하면 이번 호출에서 획득한 Lease만 반환한다.
        /// </summary>
        // ----------------------------------------------------------------------------------------------------
        private UnityAudioPlayback PlayInternal
        (
            AudioCue cue,
            bool isSpatial,
            Vector3 position,
            Transform emitter,
            AudioMixerGroup output,
            float volume,
            float outputVolume
        )
        {
            if (!isActiveAndEnabled)
            {
                throw new InvalidOperationException("활성화된 UnityAudioCuePlayer만 Cue를 재생할 수 있습니다.");
            }

            if (sourcePool == null)
            {
                throw new InvalidOperationException("UnityAudioCuePlayer의 AudioSource Pool이 초기화되지 않았습니다.");
            }

            if (cue == null)
            {
                throw new ArgumentNullException(nameof(cue));
            }

            if (cue is not UnityAudioClipCue audioCue)
            {
                throw new ArgumentException
                (
                    "UnityAudioCuePlayer는 UnityAudioClipCue만 재생할 수 있습니다.",
                    nameof(cue)
                );
            }

            if (audioCue.Clip == null)
            {
                throw new InvalidOperationException("Unity Audio Clip Cue에 AudioClip이 설정되지 않았습니다.");
            }

            if
            (
                float.IsNaN(audioCue.Volume) ||
                float.IsInfinity(audioCue.Volume) ||
                audioCue.Volume < 0.0f ||
                audioCue.Volume > 1.0f
            )
            {
                throw new InvalidOperationException("Unity Audio Clip Cue의 Volume이 유효하지 않습니다.");
            }

            if
            (
                float.IsNaN(audioCue.Pitch) ||
                float.IsInfinity(audioCue.Pitch) ||
                audioCue.Pitch < -3.0f ||
                audioCue.Pitch > 3.0f
            )
            {
                throw new InvalidOperationException("Unity Audio Clip Cue의 Pitch가 유효하지 않습니다.");
            }

            if
            (
                float.IsNaN(audioCue.SpatialBlend) ||
                float.IsInfinity(audioCue.SpatialBlend) ||
                audioCue.SpatialBlend < 0.0f ||
                audioCue.SpatialBlend > 1.0f ||
                !Enum.IsDefined(typeof(AudioRolloffMode), audioCue.RolloffMode) ||
                float.IsNaN(audioCue.MinDistance) ||
                float.IsInfinity(audioCue.MinDistance) ||
                audioCue.MinDistance <= 0.0f ||
                float.IsNaN(audioCue.MaxDistance) ||
                float.IsInfinity(audioCue.MaxDistance) ||
                audioCue.MaxDistance < audioCue.MinDistance
            )
            {
                throw new InvalidOperationException("Unity Audio Clip Cue의 공간 재생 설정이 유효하지 않습니다.");
            }

            var sourceLease = sourcePool.AcquireLease();

            try
            {
                var source = sourceLease.Value;

                // 재사용 voice가 이전 Cue의 상태를 이어받지 않도록 모든 변경 가능 설정을 덮어쓴다.
                source.playOnAwake = false;
                source.clip = audioCue.Clip;
                source.pitch = audioCue.Pitch;
                source.loop = audioCue.IsLooping;
                source.outputAudioMixerGroup = output;
                source.spatialBlend = isSpatial ? audioCue.SpatialBlend : 0.0f;
                source.rolloffMode = audioCue.RolloffMode;
                source.minDistance = audioCue.MinDistance;
                source.maxDistance = audioCue.MaxDistance;

                if (isSpatial)
                {
                    source.transform.position = position;
                }
                else
                {
                    source.transform.localPosition = Vector3.zero;
                }

                var playback = new UnityAudioPlayback
                (
                    sourceLease,
                    volume,
                    audioCue.Pitch,
                    outputVolume,
                    emitter
                );

                // 초기 개별·Bus 볼륨까지 설정된 voice만 실제 출력과 외부 Playback으로 공개한다.
                source.Play();
                playbacks.Add(playback);
                return playback;
            }
            catch
            {
                // 공개되지 않은 획득은 현재 호출의 Lease에서 바로 Pool로 반환한다.
                sourceLease.Dispose();
                throw;
            }
        }

    #endregion

    #region Unity 생명주기

        // ------------------------------------------------------------
        /// <summary>
        /// 직렬화되거나 사전에 지정된 source 설정으로 voice Pool을 초기화한다.
        /// </summary>
        // ------------------------------------------------------------
        private void Awake()
        {
            InitializeSourcePool();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Audio Playback의 emitter 위치, 자연 완료와 추적 제거를 진행한다.
        /// </summary>
        // ------------------------------------------------------------
        private void Update()
        {
            for (var i = playbacks.Count - 1; i >= 0; i--)
            {
                playbacks[i].Tick();

                if (playbacks[i].State != CuePlaybackState.Released) continue;

                playbacks.RemoveAt(i);
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Player가 비활성화되면 소유한 모든 Audio Playback을 즉시 종료한다.
        /// </summary>
        // ------------------------------------------------------------
        private void OnDisable()
        {
            for (var i = playbacks.Count - 1; i >= 0; i--)
            {
                playbacks[i].Dispose();
            }

            playbacks.Clear();
        }

    #endregion

    }
}
