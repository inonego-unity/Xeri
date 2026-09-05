/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : UnityParticleSystemCuePlayer.cs
수정일 : 2026-09-05

# 설명
UnityParticleSystemCue를 Pool에서 획득한 ParticleSystem으로 실행한다.

# 적용 범위
TransformBinding_Fixed과 TransformBinding_Tracked만 지원하며 binding 없는 임의 위치 fallback은 제공하지 않는다.
Prefab의 Renderer·Material은 자산 그대로 사용하고 Player가 Render Pipeline 자산을 생성하거나 교체하지 않는다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using inonego;
using inonego.Xeri;

namespace inonego.Xeri.Playback
{
    // ============================================================
    /// <summary>
    /// Unity ParticleSystem Cue Player.
    /// </summary>
    // ============================================================
    [DisallowMultipleComponent]
    public sealed class UnityParticleSystemCuePlayer : MonoBehaviour,
        ICuePlayer<TransformBinding_Fixed>,
        ICuePlayer<TransformBinding_Tracked>
    {

    #region 구성

        [SerializeField]
        private Transform poolRoot = null;

    #endregion

    #region 재생 상태

        private readonly List<UnityParticleSystemPlayback> playbacks = new();

    #endregion

    #region 인터페이스 구현

        // ------------------------------------------------------------
        /// <summary>
        /// 지정 Cue를 고정 World Transform에서 재생할 수 있는지 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        bool ICuePlayer<TransformBinding_Fixed>.CanPlay
        (
            IPlaybackCue cue,
            in TransformBinding_Fixed binding
        )
        {
            return cue is UnityParticleSystemCue && binding.IsValid;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Cue를 지정 World Transform에서 실행한다.
        /// </summary>
        // ------------------------------------------------------------
        ICuePlayback ICuePlayer<TransformBinding_Fixed>.Play
        (
            IPlaybackCue cue,
            in TransformBinding_Fixed binding
        )
        {
            if (!binding.IsValid)
            {
                throw new ArgumentException
                (
                    "Fixed Transform Binding의 World TRS가 유효하지 않습니다.",
                    nameof(binding)
                );
            }

            var particleCue = RequireCue(cue);
            var playback = AcquirePlayback
            (
                particleCue,
                transformBinding: null
            );

            try
            {
                var world = binding.World;
                playback.Particle.transform.SetPositionAndRotation
                (
                    world.Position,
                    world.Rotation
                );
                playback.Particle.transform.localScale = world.Scale;
                playback.Particle.Play(withChildren: true);
                playbacks.Add(playback);
                return playback;
            }
            catch
            {
                playback.Dispose();
                throw;
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 지정 Cue를 Transform 추적 재생할 수 있는지 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        bool ICuePlayer<TransformBinding_Tracked>.CanPlay
        (
            IPlaybackCue cue,
            in TransformBinding_Tracked binding
        )
        {
            return cue is UnityParticleSystemCue && binding.IsValid;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Cue를 지정 Transform을 따라가도록 실행한다.
        /// </summary>
        // ------------------------------------------------------------
        ICuePlayback ICuePlayer<TransformBinding_Tracked>.Play
        (
            IPlaybackCue cue,
            in TransformBinding_Tracked binding
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

            var particleCue = RequireCue(cue);
            var playback = AcquirePlayback
            (
                particleCue,
                binding
            );

            try
            {
                var world = binding.World;
                playback.Particle.transform.SetPositionAndRotation
                (
                    world.Position,
                    world.Rotation
                );
                playback.Particle.transform.localScale = world.Scale;
                playback.Particle.Play(withChildren: true);
                playbacks.Add(playback);
                return playback;
            }
            catch
            {
                playback.Dispose();
                throw;
            }
        }

    #endregion

    #region 재생 수명

        // ------------------------------------------------------------
        /// <summary>
        /// 모든 활성 ParticleSystem Playback을 즉시 종료한다.
        /// </summary>
        // ------------------------------------------------------------
        public void StopAll()
        {
            List<Exception> errors = null;

            // 역순 종료로 각 Playback이 자기 자신을 추적 목록에서 제거해도 순회를 보존한다.
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
                    "ParticleSystem Playback 종료 중 하나 이상의 정리가 실패했습니다.",
                    errors
                );
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Terminal Playback을 활성 추적 목록에서 제거한다.
        /// </summary>
        // ------------------------------------------------------------
        internal void ReleasePlayback(UnityParticleSystemPlayback playback)
        {
            playbacks.Remove(playback);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Runtime Cue가 선택한 Variant Pool에서 Lease를 획득해 Particle Playback을 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        private UnityParticleSystemPlayback AcquirePlayback
        (
            UnityParticleSystemCue cue,
            TransformBinding_Tracked? transformBinding
        )
        {
            // Variant 선택과 Pool 획득은 runtime Cue에 위임해 Player가 Pool 상태를 소유하지 않게 한다.
            var lease = cue.AcquireLease
            (
                transform,
                poolRoot,
                out var variant
            );
            var particle = lease.Value;

            try
            {
                // 재사용 인스턴스의 이전 방출 상태를 지우고 이번 Variant의 시간 정책만 적용한다.
                particle.Stop
                (
                    withChildren: true,
                    ParticleSystemStopBehavior.StopEmittingAndClear
                );
                var main = particle.main;
                main.useUnscaledTime = variant.UsesUnscaledTime;

                return new UnityParticleSystemPlayback
                (
                    this,
                    lease,
                    transformBinding
                );
            }
            catch
            {
                // 공개되지 못한 Playback의 Pool 소유권을 같은 실패 경계에서 즉시 반환한다.
                lease.Dispose();
                throw;
            }
        }

    #endregion

    #region 검증

        // ------------------------------------------------------------
        /// <summary>
        /// 범용 Cue를 UnityParticleSystemCue로 검증해 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        private static UnityParticleSystemCue RequireCue(IPlaybackCue cue)
        {
            if (cue == null)
            {
                throw new ArgumentNullException(nameof(cue));
            }

            return cue as UnityParticleSystemCue ?? throw new ArgumentException
            (
                "UnityParticleSystemCuePlayer는 UnityParticleSystemCue만 재생할 수 있습니다.",
                nameof(cue)
            );
        }

    #endregion

    #region Unity 생명주기

        // ------------------------------------------------------------
        /// <summary>
        /// 활성 ParticleSystem Playback의 Transform 추적과 자연 종료를 진행한다.
        /// </summary>
        // ------------------------------------------------------------
        private void LateUpdate()
        {
            for (var index = playbacks.Count - 1; index >= 0; index--)
            {
                playbacks[index].Tick();
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 비활성화에서 활성 ParticleSystem Playback을 모두 즉시 종료한다.
        /// </summary>
        // ------------------------------------------------------------
        private void OnDisable()
        {
            StopAll();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 파괴 시 남은 활성 ParticleSystem Playback을 정리한다.
        /// </summary>
        // ------------------------------------------------------------
        private void OnDestroy()
        {
            StopAll();
        }

    #endregion

    }
}
