/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : UnityAudioCuePlayer.cs
수정일 : 2026-09-05

# 설명
UnityAudioClipCue를 Pool에서 획득한 AudioSource voice로 즉시 또는 DSP 예약 실행하고 Playback을 갱신한다.

# 적용 범위
2D, 고정 위치 3D와 emitter 추적 3D 배치 및 2D 예약 시작을 지원한다.
Bus 출력 정책은 AudioManager가 계산하고 Player는 전달받은 초기 출력 설정을 voice에 적용한다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Audio;

using inonego;
using inonego.Xeri;
using inonego.Xeri.Pool;
using inonego.Xeri.Primitive;

namespace inonego.Xeri.Playback
{
    // ============================================================
    /// <summary>
    /// Unity AudioClip Cue Player.
    /// </summary>
    // ============================================================
    public sealed class UnityAudioCuePlayer : MonoBehaviour,
        ICuePlayer<NoCueBinding>,
        ICuePlayer<TransformBinding_Fixed>,
        ICuePlayer<TransformBinding_Tracked>
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

    #region Audio 재생

        // ------------------------------------------------------------
        /// <summary>
        /// UnityAudioClipCue를 이 backend가 처리할 수 있는지 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        internal bool SupportsCue(IPlaybackCue cue)
        {
            return cue is UnityAudioClipCue;
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// 지원 Audio Cue에서 이번 재생의 Variant를 하나 선택하고 재생 설정을 검증한다.
        /// </summary>
        // ----------------------------------------------------------------------
        internal UnityAudioClipCueVariant SelectVariant(AudioCue cue)
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

            var variant = audioCue.SelectVariant();
            ValidateVariant(variant);
            return variant;
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// Unity Audio Clip Cue Variant의 Clip·볼륨·Pitch·공간 재생 설정을 검증한다.
        /// </summary>
        // ----------------------------------------------------------------------
        private static void ValidateVariant(UnityAudioClipCueVariant variant)
        {
            if (variant == null)
            {
                throw new InvalidOperationException("Unity Audio Clip Cue Variant가 비어 있습니다.");
            }

            if (variant.Clip == null)
            {
                throw new InvalidOperationException("Unity Audio Clip Cue Variant에 AudioClip이 설정되지 않았습니다.");
            }

            if
            (
                !variant.Volume.IsFinite() ||
                variant.Volume < 0.0f ||
                variant.Volume > 1.0f
            )
            {
                throw new InvalidOperationException("Unity Audio Clip Cue Variant의 Volume이 유효하지 않습니다.");
            }

            if
            (
                !variant.Pitch.IsFinite() ||
                variant.Pitch < -3.0f ||
                variant.Pitch > 3.0f
            )
            {
                throw new InvalidOperationException("Unity Audio Clip Cue Variant의 Pitch가 유효하지 않습니다.");
            }

            if
            (
                !variant.SpatialBlend.IsFinite() ||
                variant.SpatialBlend < 0.0f ||
                variant.SpatialBlend > 1.0f ||
                !Enum.IsDefined(typeof(AudioRolloffMode), variant.RolloffMode) ||
                !variant.MinDistance.IsFinite() ||
                variant.MinDistance <= 0.0f ||
                !variant.MaxDistance.IsFinite() ||
                variant.MaxDistance < variant.MinDistance
            )
            {
                throw new InvalidOperationException("Unity Audio Clip Cue Variant의 공간 재생 설정이 유효하지 않습니다.");
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Audio Cue를 2D voice로 실행한다.
        /// </summary>
        // ------------------------------------------------------------
        public IAudioPlayback Play(AudioCue cue)
        {
            var variant = SelectVariant(cue);
            return PlayInternal
            (
                variant,
                isSpatial: false,
                Vector3.zero,
                transformBinding: null,
                output: null,
                variant.Volume,
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
            var variant = SelectVariant(cue);
            return PlayInternal
            (
                variant,
                isSpatial: true,
                position,
                transformBinding: null,
                output: null,
                variant.Volume,
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

            var variant = SelectVariant(cue);
            var binding = new TransformBinding_Tracked(emitter);
            return PlayInternal
            (
                variant,
                isSpatial: true,
                binding.World.Position,
                binding,
                output: null,
                variant.Volume,
                outputVolume: 1.0f
            );
        }

        // --------------------------------------------------------------------------------
        /// <summary>
        /// AudioManager가 선택한 Variant를 계산된 초기 볼륨과 출력 Group으로 2D 실행한다.
        /// </summary>
        // --------------------------------------------------------------------------------
        internal UnityAudioPlayback Play
        (
            UnityAudioClipCueVariant variant,
            float volume,
            AudioMixerGroup output,
            float outputVolume
        )
        {
            return PlayInternal
            (
                variant,
                isSpatial: false,
                Vector3.zero,
                transformBinding: null,
                output,
                volume,
                outputVolume
            );
        }

        // --------------------------------------------------------------------------------
        /// <summary>
        /// AudioManager가 선택한 Variant를 지정 DSP 시각에 2D 예약 실행한다.
        /// </summary>
        // --------------------------------------------------------------------------------
        internal UnityAudioPlayback PlayScheduled
        (
            UnityAudioClipCueVariant variant,
            double dspTime,
            float volume,
            AudioMixerGroup output,
            float outputVolume
        )
        {
            return PlayInternal
            (
                variant,
                isSpatial: false,
                Vector3.zero,
                transformBinding: null,
                output,
                volume,
                outputVolume,
                dspTime
            );
        }

        // --------------------------------------------------------------------------------
        /// <summary>
        /// AudioManager가 선택한 Variant를 지정 월드 위치에서 실행한다.
        /// </summary>
        // --------------------------------------------------------------------------------
        internal UnityAudioPlayback Play
        (
            UnityAudioClipCueVariant variant,
            Vector3 position,
            float volume,
            AudioMixerGroup output,
            float outputVolume
        )
        {
            return PlayInternal
            (
                variant,
                isSpatial: true,
                position,
                transformBinding: null,
                output,
                volume,
                outputVolume
            );
        }

        // --------------------------------------------------------------------------------
        /// <summary>
        /// AudioManager가 선택한 Variant를 emitter 위치에서 실행한다.
        /// </summary>
        // --------------------------------------------------------------------------------
        internal UnityAudioPlayback Play
        (
            UnityAudioClipCueVariant variant,
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

            var binding = new TransformBinding_Tracked(emitter);
            return Play
            (
                variant,
                in binding,
                volume,
                output,
                outputVolume
            );
        }

        // --------------------------------------------------------------------------------
        /// <summary>
        /// AudioManager가 선택한 Variant를 Tracked Transform Binding 위치에서 실행한다.
        /// </summary>
        // --------------------------------------------------------------------------------
        internal UnityAudioPlayback Play
        (
            UnityAudioClipCueVariant variant,
            in TransformBinding_Tracked binding,
            float volume,
            AudioMixerGroup output,
            float outputVolume
        )
        {
            if (!binding.IsValid)
            {
                throw new ArgumentException
                (
                    "Tracked Transform Binding의 Target과 Local TRS가 유효하지 않습니다.",
                    nameof(binding)
                );
            }

            return PlayInternal
            (
                variant,
                isSpatial: true,
                binding.World.Position,
                binding,
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
            UnityAudioClipCueVariant variant,
            bool isSpatial,
            Vector3 position,
            TransformBinding_Tracked? transformBinding,
            AudioMixerGroup output,
            float volume,
            float outputVolume,
            double scheduledStartDSPTime = double.NaN
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

            ValidateVariant(variant);

            if
            (
                !double.IsNaN(scheduledStartDSPTime) &&
                (!scheduledStartDSPTime.IsFinite() ||
                 scheduledStartDSPTime < 0.0)
            )
            {
                throw new ArgumentOutOfRangeException
                (
                    nameof(scheduledStartDSPTime),
                    "Scheduled DSP Time은 0 이상의 유한한 값이어야 합니다."
                );
            }

            var sourceLease = sourcePool.AcquireLease();

            try
            {
                var source = sourceLease.Value;

                // 재사용 voice가 이전 Cue의 상태를 이어받지 않도록 모든 변경 가능 설정을 덮어쓴다.
                source.playOnAwake = false;
                source.clip = variant.Clip;
                source.pitch = variant.Pitch;
                source.loop = variant.IsLooping;
                source.outputAudioMixerGroup = output;
                source.spatialBlend = isSpatial ? variant.SpatialBlend : 0.0f;
                source.rolloffMode = variant.RolloffMode;
                source.minDistance = variant.MinDistance;
                source.maxDistance = variant.MaxDistance;

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
                    variant.Pitch,
                    outputVolume,
                    transformBinding,
                    scheduledStartDSPTime
                );

                // 초기 개별·Bus 볼륨까지 설정된 voice만 실제 출력과 외부 Playback으로 공개한다.
                if (double.IsNaN(scheduledStartDSPTime))
                {
                    source.Play();
                }
                else
                {
                    source.PlayScheduled(scheduledStartDSPTime);
                }

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

    #region 인터페이스 구현

        // ------------------------------------------------------------
        /// <summary>
        /// Unity Audio Cue를 binding 없이 처리할 수 있는지 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        bool ICuePlayer<NoCueBinding>.CanPlay(IPlaybackCue cue, in NoCueBinding binding)
        {
            return SupportsCue(cue);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Unity Audio Cue를 2D로 실행한다.
        /// </summary>
        // ------------------------------------------------------------
        ICuePlayback ICuePlayer<NoCueBinding>.Play(IPlaybackCue cue, in NoCueBinding binding)
        {
            return Play(RequireAudioCue(cue));
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Unity Audio Cue를 Fixed Transform Binding으로 처리할 수 있는지 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        bool ICuePlayer<TransformBinding_Fixed>.CanPlay(IPlaybackCue cue, in TransformBinding_Fixed binding)
        {
            return SupportsCue(cue) && binding.IsValid;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Unity Audio Cue를 Binding의 월드 위치에서 실행한다.
        /// </summary>
        // ------------------------------------------------------------
        ICuePlayback ICuePlayer<TransformBinding_Fixed>.Play(IPlaybackCue cue, in TransformBinding_Fixed binding)
        {
            if (!binding.IsValid)
            {
                throw new ArgumentException
                (
                    "Fixed Transform Binding의 World TRS가 유효하지 않습니다.",
                    nameof(binding)
                );
            }

            return Play(RequireAudioCue(cue), binding.World.Position);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Unity Audio Cue를 Tracked Transform Binding으로 처리할 수 있는지 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        bool ICuePlayer<TransformBinding_Tracked>.CanPlay(IPlaybackCue cue, in TransformBinding_Tracked binding)
        {
            return SupportsCue(cue) && binding.IsValid;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Unity Audio Cue를 Binding Transform을 따라가도록 실행한다.
        /// </summary>
        // ------------------------------------------------------------
        ICuePlayback ICuePlayer<TransformBinding_Tracked>.Play(IPlaybackCue cue, in TransformBinding_Tracked binding)
        {
            if (!binding.IsValid)
            {
                throw new ArgumentException
                (
                    "Tracked Transform Binding의 Target과 Local TRS가 유효하지 않습니다.",
                    nameof(binding)
                );
            }

            var variant = SelectVariant(RequireAudioCue(cue));
            return PlayInternal
            (
                variant,
                isSpatial: true,
                binding.World.Position,
                binding,
                output: null,
                variant.Volume,
                outputVolume: 1.0f
            );
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 범용 Cue를 Audio Cue로 검증해 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        private static AudioCue RequireAudioCue(IPlaybackCue cue)
        {
            if (cue == null)
            {
                throw new ArgumentNullException(nameof(cue));
            }

            return cue as AudioCue ?? throw new ArgumentException
            (
                "UnityAudioCuePlayer는 AudioCue만 재생할 수 있습니다.",
                nameof(cue)
            );
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
            List<Exception> errors = null;

            for (var index = playbacks.Count - 1; index >= 0; index--)
            {
                try
                {
                    playbacks[index].Dispose();
                }
                catch (Exception exception)
                {
                    errors ??= new();
                    errors.Add(exception);
                }
            }

            playbacks.Clear();

            if (errors != null)
            {
                throw new AggregateException
                (
                    "Audio Player 비활성화 중 하나 이상의 Playback 정리가 실패했습니다.",
                    errors
                );
            }
        }

    #endregion

    }
}
