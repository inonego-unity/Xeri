/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : UnityParticleSystemCuePlayer.cs
수정일 : 2026-08-19

# 설명
UnityParticleSystemCue를 Pool에서 획득한 ParticleSystem으로 실행한다.

# 적용 범위
WorldTransformBinding과 TransformBinding만 지원하며 binding 없는 임의 위치 fallback은 제공하지 않는다.
Prefab의 Renderer·Material은 자산 그대로 사용하고 Player가 Render Pipeline 자산을 생성하거나 교체하지 않는다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections.Generic;

using UnityEngine;

using inonego.Xeri;
using inonego.Xeri.Pool;

namespace inonego.Xeri.Playback
{
    // ============================================================
    /// <summary>
    /// Unity ParticleSystem Cue Player.
    /// </summary>
    // ============================================================
    [DisallowMultipleComponent]
    public sealed class UnityParticleSystemCuePlayer : MonoBehaviour,
        ICuePlayer<WorldTransformBinding>,
        ICuePlayer<TransformBinding>
    {

    #region 구성

        [SerializeField]
        private Transform poolRoot = null;

    #endregion

    #region Runtime 상태

        private readonly Dictionary<UnityParticleSystemCue, GOCompPool<ParticleSystem>> pools = new();
        private readonly List<UnityParticleSystemPlayback> playbacks = new();

    #endregion

    #region Cue Player 구현

        // ------------------------------------------------------------
        /// <summary>
        /// 지정 Cue를 고정 World Transform에서 재생할 수 있는지 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        bool ICuePlayer<WorldTransformBinding>.CanPlay
        (
            IPlaybackCue cue,
            in WorldTransformBinding binding
        )
        {
            return cue is UnityParticleSystemCue && binding.IsValid;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Cue를 지정 World Transform에서 실행한다.
        /// </summary>
        // ------------------------------------------------------------
        ICuePlayback ICuePlayer<WorldTransformBinding>.Play
        (
            IPlaybackCue cue,
            in WorldTransformBinding binding
        )
        {
            if (!binding.IsValid)
            {
                throw new ArgumentException
                (
                    "World Transform Binding의 위치·회전·스케일 값이 유효하지 않습니다.",
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
                playback.Particle.transform.SetPositionAndRotation
                (
                    binding.Position,
                    binding.Rotation
                );
                playback.Particle.transform.localScale = binding.Scale;
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
        bool ICuePlayer<TransformBinding>.CanPlay
        (
            IPlaybackCue cue,
            in TransformBinding binding
        )
        {
            return cue is UnityParticleSystemCue && binding.IsValid;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Cue를 지정 Transform을 따라가도록 실행한다.
        /// </summary>
        // ------------------------------------------------------------
        ICuePlayback ICuePlayer<TransformBinding>.Play
        (
            IPlaybackCue cue,
            in TransformBinding binding
        )
        {
            if (!binding.IsValid)
            {
                throw new ArgumentException
                (
                    "Transform Binding의 대상이 유효하지 않습니다.",
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
                playback.Particle.transform.SetPositionAndRotation
                (
                    binding.Position,
                    binding.Rotation
                );
                playback.Particle.transform.localScale = binding.Scale;
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

    #region Playback 수명

        // ------------------------------------------------------------
        /// <summary>
        /// Cue용 Pool에서 초기화된 Particle Playback을 획득한다.
        /// </summary>
        // ------------------------------------------------------------
        private UnityParticleSystemPlayback AcquirePlayback
        (
            UnityParticleSystemCue cue,
            TransformBinding? transformBinding
        )
        {
            var pool = GetOrCreatePool(cue);
            pool.Parent = transform;
            var particle = pool.Acquire(worldPositionStays: false);

            // 재사용 인스턴스의 이전 방출 상태만 제거하고 Renderer·Material은 Prefab 설정을 유지한다.
            particle.Stop
            (
                withChildren: true,
                ParticleSystemStopBehavior.StopEmittingAndClear
            );
            var main = particle.main;
            main.useUnscaledTime = cue.UsesUnscaledTime;

            return new UnityParticleSystemPlayback
            (
                this,
                cue,
                particle,
                transformBinding
            );
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Cue Prefab에 대응하는 ParticleSystem Pool을 조회하거나 최초 생성한다.
        /// </summary>
        // ------------------------------------------------------------
        private GOCompPool<ParticleSystem> GetOrCreatePool(UnityParticleSystemCue cue)
        {
            if (pools.TryGetValue(cue, out var existing))
            {
                return existing;
            }

            if (cue.Prefab == null)
            {
                throw new MissingReferenceException
                (
                    $"Unity Particle System Cue '{cue.name}'에 ParticleSystem Prefab이 필요합니다."
                );
            }

            var releasedRoot = new GameObject($"{cue.name}_Pool").transform;
            releasedRoot.SetParent(poolRoot != null ? poolRoot : transform, false);
            var provider = new PrefabGameObjectProvider
            (
                cue.Prefab.gameObject,
                transform
            );
            var created = new GOCompPool<ParticleSystem>(provider)
            {
                Parent = transform,
                Pool = releasedRoot,
            };
            pools.Add(cue, created);
            return created;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Terminal Playback의 Particle을 원래 Cue Pool에 반환하고 활성 추적에서 제거한다.
        /// </summary>
        // ------------------------------------------------------------
        internal void ReleasePlayback
        (
            UnityParticleSystemPlayback playback,
            UnityParticleSystemCue cue,
            ParticleSystem particle
        )
        {
            playbacks.Remove(playback);

            if (particle == null || cue == null) return;

            particle.Stop
            (
                withChildren: true,
                ParticleSystemStopBehavior.StopEmittingAndClear
            );

            if (pools.TryGetValue(cue, out var pool))
            {
                pool.Release
                (
                    particle,
                    pushToReleased: true,
                    worldPositionStays: false
                );
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 모든 활성 ParticleSystem Playback을 즉시 종료한다.
        /// </summary>
        // ------------------------------------------------------------
        public void StopAll()
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
                    "ParticleSystem Playback 종료 중 하나 이상의 정리가 실패했습니다.",
                    errors
                );
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
        /// 파괴 시 Cue별 Runtime Pool 참조를 정리한다.
        /// </summary>
        // ------------------------------------------------------------
        private void OnDestroy()
        {
            try
            {
                StopAll();
            }
            finally
            {
                pools.Clear();
            }
        }

    #endregion

    }
}
